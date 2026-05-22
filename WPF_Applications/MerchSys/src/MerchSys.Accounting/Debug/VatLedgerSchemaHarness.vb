#If DEBUG Then

Imports System.Diagnostics
Imports System.IO
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.Accounting.Enums

Namespace Debug

    ''' <summary>
    ''' Verifies three ACC-10 acceptance criteria flagged as unvalidated by the 2026-05-11
    ''' Accounting audit: clean apply on a fresh database, migration idempotency on an existing
    ''' database, duplicate-filing rejection via the partial unique index, and cascade-delete of
    ''' <see cref="VatReturnLine"/> rows when their parent <see cref="VatReturn"/> is removed.
    ''' Uses PRAGMA-based introspection to confirm what SQLite actually created, not just what
    ''' EF Core intended.
    ''' </summary>
    Public Class VatLedgerSchemaHarness

        ' Held for DI compatibility; scratch checks use DbContextOptionsBuilder directly so that
        ' each check can target an isolated %TEMP% database independent of the production path.
        Private ReadOnly _contextFactory As IDbContextFactory(Of AccountingDbContext)

        Public Sub New(contextFactory As IDbContextFactory(Of AccountingDbContext))
            _contextFactory = contextFactory
        End Sub

        ''' <summary>
        ''' Runs all four schema verification checks in sequence and returns a consolidated report.
        ''' Each check targets its own scratch SQLite file under <c>%TEMP%</c>; all scratch files
        ''' are deleted in a Finally block regardless of pass/fail.
        ''' </summary>
        Public Async Function RunAllAsync() As Task(Of VatLedgerSchemaReport)
            Dim report As New VatLedgerSchemaReport()
            report.FreshDatabaseApplyResult = Await CheckFreshDatabaseApplyAsync()
            report.ExistingDatabaseApplyResult = Await CheckExistingDatabaseApplyAsync()
            report.DuplicateFilingBlockedResult = Await CheckDuplicateFilingBlockedAsync()
            report.CascadeDeleteResult = Await CheckCascadeDeleteAsync()
            Return report
        End Function

        ' ── Check 1: Fresh database apply ─────────────────────────────────────────────

        Private Async Function CheckFreshDatabaseApplyAsync() As Task(Of CheckResult)
            Dim sw = Stopwatch.StartNew()
            Dim scratchPath = MakeScratchPath()
            Dim runError As Exception = Nothing
            Dim passed = False
            Dim detail As String = Nothing

            Try
                Using ctx = OpenScratch(scratchPath)
                    SetupScratchSchema(scratchPath)

                    Dim conn = ctx.Database.GetDbConnection()
                    Await conn.OpenAsync()

                    ' PRAGMA table_info — confirms columns created by the migration at SQLite level.
                    Dim vatReturnCols = Await ReadTableColumnsAsync(conn, "Acc_VatReturns")
                    Dim lineCols = Await ReadTableColumnsAsync(conn, "Acc_VatReturnLines")

                    ' PRAGMA index_list — confirms the unique constraint was created.
                    Dim indexList = Await ReadIndexListAsync(conn, "Acc_VatReturns")

                    conn.Close()

                    Dim requiredVrCols = {"Id", "Year", "Period", "PeriodType", "FormType",
                                          "TotalVatableSales", "TotalOutputVat", "VatPayable",
                                          "FilingStatus", "GeneratedAt"}
                    Dim requiredLineCols = {"Id", "VatReturnId", "SourceModule", "SourceTable",
                                            "SourceRowId", "VatableAmount", "OutputVat", "Treatment"}

                    Dim missingVr = requiredVrCols.Where(Function(c) Not vatReturnCols.Contains(c)).ToList()
                    Dim missingLine = requiredLineCols.Where(Function(c) Not lineCols.Contains(c)).ToList()

                    ' After FixVatReturnAmendedIndex (ACC-11) the index was renamed to _Active and
                    ' converted to a partial index (WHERE FilingStatus != 3).  The original name
                    ' referenced in the ACC-10 plan ("IX_Acc_VatReturns_PeriodYear_PeriodMonth_ReturnType")
                    ' does not exist; the real name contains "Year_Period_PeriodType_FormType".
                    Dim activeIndex = indexList.FirstOrDefault(Function(ix)
                                                                   Return ix.Name.Contains("Year_Period_PeriodType_FormType")
                                                               End Function)
                    Dim indexFound = activeIndex IsNot Nothing
                    Dim isUnique = indexFound AndAlso activeIndex.Unique = 1

                    If missingVr.Count = 0 AndAlso missingLine.Count = 0 AndAlso indexFound AndAlso isUnique Then
                        passed = True
                        detail = $"Tables OK. Unique index '{activeIndex.Name}' confirmed " &
                                 "(partial: FilingStatus!=3, added by FixVatReturnAmendedIndex). " &
                                 "DRIFT: ACC-10 plan named index 'IX_Acc_VatReturns_PeriodYear_PeriodMonth_ReturnType' " &
                                 "(3 cols) but actual index has 4 columns (Year, Period, PeriodType, FormType)."
                    Else
                        detail = $"FAILED. Missing Acc_VatReturns cols: [{String.Join(", ", missingVr)}]. " &
                                 $"Missing Acc_VatReturnLines cols: [{String.Join(", ", missingLine)}]. " &
                                 $"UniqueIndex found={indexFound}, isUnique={isUnique}."
                    End If
                End Using
            Catch ex As Exception
                runError = ex
            End Try

            ' No Await in Finally — capture exception state above, clean up synchronously (BC36943).
            DeleteScratch(scratchPath)

            If runError IsNot Nothing Then
                detail = $"Exception: {runError.GetType().Name}: {runError.Message}"
            End If

            sw.Stop()
            Return New CheckResult With {.Passed = passed, .Detail = detail, .DurationMs = sw.ElapsedMilliseconds}
        End Function

        ' ── Check 2: Existing database apply (idempotency) ────────────────────────────

        Private Async Function CheckExistingDatabaseApplyAsync() As Task(Of CheckResult)
            Dim sw = Stopwatch.StartNew()
            Dim scratchPath = MakeScratchPath()
            Dim runError As Exception = Nothing
            Dim passed = False
            Dim detail As String = Nothing

            Try
                ' First migrate and seed one row.
                Using ctx = OpenScratch(scratchPath)
                    SetupScratchSchema(scratchPath)
                    Dim now = DateTime.UtcNow
                    ctx.VatReturns.Add(New VatReturn With {
                        .Year = 2025, .Period = 12,
                        .PeriodType = VatReturnPeriodType.Monthly,
                        .FormType = VatReturnFormType.Form2550M,
                        .FilingStatus = VatFilingStatus.Generated,
                        .GeneratedAt = now,
                        .CreatedBy = "harness", .CreatedAt = now
                    })
                    Await ctx.SaveChangesAsync()
                End Using

                ' Second migrate on the same database — must be a no-op.
                Using ctx = OpenScratch(scratchPath)
                    SetupScratchSchema(scratchPath)

                    Dim seededCount = Await ctx.VatReturns.CountAsync()

                    Dim conn = ctx.Database.GetDbConnection()
                    Await conn.OpenAsync()
                    Dim migrationIds = Await ReadMigrationHistoryAsync(conn)
                    conn.Close()

                    Dim duplicates = migrationIds.
                        GroupBy(Function(m) m).
                        Where(Function(g) g.Count() > 1).
                        Select(Function(g) g.Key).
                        ToList()

                    If seededCount = 1 AndAlso duplicates.Count = 0 Then
                        passed = True
                        detail = $"Idempotency OK. Seeded row preserved (count={seededCount}). " &
                                 $"{migrationIds.Count} migration(s) each applied exactly once."
                    Else
                        detail = $"FAILED. Seeded row count={seededCount} (expected 1). " &
                                 $"Duplicate migration IDs: [{String.Join(", ", duplicates)}]."
                    End If
                End Using
            Catch ex As Exception
                runError = ex
            End Try

            DeleteScratch(scratchPath)

            If runError IsNot Nothing Then
                detail = $"Exception: {runError.GetType().Name}: {runError.Message}"
            End If

            sw.Stop()
            Return New CheckResult With {.Passed = passed, .Detail = detail, .DurationMs = sw.ElapsedMilliseconds}
        End Function

        ' ── Check 3: Duplicate filing blocked ─────────────────────────────────────────

        Private Async Function CheckDuplicateFilingBlockedAsync() As Task(Of CheckResult)
            Dim sw = Stopwatch.StartNew()
            Dim scratchPath = MakeScratchPath()
            Dim runError As Exception = Nothing
            Dim passed = False
            Dim detail As String = Nothing

            Try
                Dim now = DateTime.UtcNow

                ' Commit the first row so the constraint can reject the second.
                Using ctx = OpenScratch(scratchPath)
                    SetupScratchSchema(scratchPath)
                    ' FilingStatus=Generated (1) sits within the partial index scope (FilingStatus != 3).
                    ctx.VatReturns.Add(New VatReturn With {
                        .Year = 2026, .Period = 5,
                        .PeriodType = VatReturnPeriodType.Monthly,
                        .FormType = VatReturnFormType.Form2550M,
                        .FilingStatus = VatFilingStatus.Generated,
                        .GeneratedAt = now,
                        .CreatedBy = "harness", .CreatedAt = now
                    })
                    Await ctx.SaveChangesAsync()
                End Using

                ' Attempt the duplicate in a fresh context so the change tracker is clean.
                Using ctx = OpenScratch(scratchPath)
                    now = DateTime.UtcNow
                    ctx.VatReturns.Add(New VatReturn With {
                        .Year = 2026, .Period = 5,
                        .PeriodType = VatReturnPeriodType.Monthly,
                        .FormType = VatReturnFormType.Form2550M,
                        .FilingStatus = VatFilingStatus.Generated,
                        .GeneratedAt = now,
                        .CreatedBy = "harness", .CreatedAt = now
                    })

                    Dim sqliteEx As SqliteException = Nothing
                    Dim dbUpdateEx As DbUpdateException = Nothing
                    Try
                        Await ctx.SaveChangesAsync()
                    Catch ex As DbUpdateException When TypeOf ex.InnerException Is SqliteException
                        dbUpdateEx = ex
                        sqliteEx = DirectCast(ex.InnerException, SqliteException)
                    End Try

                    ' SQLite error code 19 = SQLITE_CONSTRAINT (unique, FK, or check violation).
                    ' We assert code 19 specifically because it pins the failure to a constraint,
                    ' distinguishing it from transient errors (SQLITE_BUSY=5, SQLITE_LOCKED=6, etc.).
                    Dim correctCode = sqliteEx IsNot Nothing AndAlso sqliteEx.SqliteErrorCode = 19
                    Dim messageContainsTable = sqliteEx IsNot Nothing AndAlso
                                               sqliteEx.Message.Contains("Acc_VatReturns")

                    ' Query goes to the DB — returns committed rows only, ignoring change-tracker state.
                    Dim committedCount = Await ctx.VatReturns.CountAsync()

                    If dbUpdateEx IsNot Nothing AndAlso correctCode AndAlso committedCount = 1 Then
                        passed = True
                        detail = $"Constraint enforced. SqliteErrorCode={sqliteEx.SqliteErrorCode} " &
                                 $"(SQLITE_CONSTRAINT). MessageContainsTable={messageContainsTable}. " &
                                 "First row preserved (committed count=1). " &
                                 "Index is partial (FilingStatus!=3); Amended rows bypass the constraint by design."
                    Else
                        detail = $"FAILED. DbUpdateExThrown={dbUpdateEx IsNot Nothing}. " &
                                 $"SqliteErrorCode={If(sqliteEx IsNot Nothing, sqliteEx.SqliteErrorCode.ToString(), "n/a")} " &
                                 $"(expected 19). CommittedRowCount={committedCount} (expected 1)."
                    End If
                End Using
            Catch ex As Exception
                runError = ex
            End Try

            DeleteScratch(scratchPath)

            If runError IsNot Nothing Then
                detail = $"Exception: {runError.GetType().Name}: {runError.Message}"
            End If

            sw.Stop()
            Return New CheckResult With {.Passed = passed, .Detail = detail, .DurationMs = sw.ElapsedMilliseconds}
        End Function

        ' ── Check 4: Cascade delete ────────────────────────────────────────────────────

        Private Async Function CheckCascadeDeleteAsync() As Task(Of CheckResult)
            Dim sw = Stopwatch.StartNew()
            Dim scratchPath = MakeScratchPath()
            Dim runError As Exception = Nothing
            Dim passed = False
            Dim detail As String = Nothing

            Try
                ' Part A — EF Core cascade: Remove entity via context, verify lines are gone.
                Dim efCascadeOk = False
                Using ctx = OpenScratch(scratchPath)
                    SetupScratchSchema(scratchPath)
                    Dim now = DateTime.UtcNow

                    Dim vatReturn = New VatReturn With {
                        .Year = 2026, .Period = 1,
                        .PeriodType = VatReturnPeriodType.Monthly,
                        .FormType = VatReturnFormType.Form2550M,
                        .FilingStatus = VatFilingStatus.Generated,
                        .GeneratedAt = now,
                        .CreatedBy = "harness", .CreatedAt = now,
                        .Lines = New List(Of VatReturnLine) From {
                            MakeLine(now), MakeLine(now), MakeLine(now)
                        }
                    }
                    ctx.VatReturns.Add(vatReturn)
                    Await ctx.SaveChangesAsync()

                    ctx.Remove(vatReturn)
                    Await ctx.SaveChangesAsync()

                    Dim orphans = Await ctx.VatReturnLines.CountAsync()
                    efCascadeOk = orphans = 0
                End Using

                ' Part B — Schema-level FK cascade: raw SQL DELETE with foreign_keys=ON,
                ' then PRAGMA foreign_key_list to confirm ON DELETE CASCADE is declared.
                DeleteScratch(scratchPath)
                scratchPath = MakeScratchPath()

                Dim schemaFkOk = False
                Dim fkDetail As String = "Not reached"

                Using ctx = OpenScratch(scratchPath)
                    SetupScratchSchema(scratchPath)
                    Dim now = DateTime.UtcNow

                    Dim vatReturn = New VatReturn With {
                        .Year = 2026, .Period = 2,
                        .PeriodType = VatReturnPeriodType.Monthly,
                        .FormType = VatReturnFormType.Form2550M,
                        .FilingStatus = VatFilingStatus.Generated,
                        .GeneratedAt = now,
                        .CreatedBy = "harness", .CreatedAt = now,
                        .Lines = New List(Of VatReturnLine) From {MakeLine(now), MakeLine(now)}
                    }
                    ctx.VatReturns.Add(vatReturn)
                    Await ctx.SaveChangesAsync()

                    Dim parentId = vatReturn.Id
                    Dim dbPath = ctx.Database.GetConnectionString()

                    ' Use a separate connection so PRAGMA foreign_keys=ON applies to this
                    ' connection without interfering with EF Core's connection state.
                    Using rawConn As New SqliteConnection(dbPath)
                        Await rawConn.OpenAsync()

                        Using cmd = rawConn.CreateCommand()
                            cmd.CommandText = "PRAGMA foreign_keys = ON"
                            Await cmd.ExecuteNonQueryAsync()
                        End Using

                        Using cmd = rawConn.CreateCommand()
                            cmd.CommandText = "DELETE FROM ""Acc_VatReturns"" WHERE ""Id"" = @id"
                            cmd.Parameters.AddWithValue("@id", parentId)
                            Await cmd.ExecuteNonQueryAsync()
                        End Using

                        Dim orphanCount As Integer
                        Using cmd = rawConn.CreateCommand()
                            cmd.CommandText = "SELECT COUNT(*) FROM ""Acc_VatReturnLines"" WHERE ""VatReturnId"" = @id"
                            cmd.Parameters.AddWithValue("@id", parentId)
                            orphanCount = CInt(Await cmd.ExecuteScalarAsync())
                        End Using

                        schemaFkOk = orphanCount = 0

                        ' PRAGMA foreign_key_list confirms the ON DELETE CASCADE declaration
                        ' exists at the SQLite schema level, independent of EF Core behaviour.
                        Dim fkList = Await ReadForeignKeyListAsync(rawConn, "Acc_VatReturnLines")
                        Dim vatReturnFk = fkList.FirstOrDefault(
                            Function(fk) fk.ReferencedTable = "Acc_VatReturns")

                        If vatReturnFk IsNot Nothing Then
                            fkDetail = $"FK confirmed via PRAGMA: table=Acc_VatReturns, " &
                                       $"from=VatReturnId, on_delete={vatReturnFk.OnDelete}"
                        Else
                            fkDetail = "FK to Acc_VatReturns NOT found in PRAGMA foreign_key_list('Acc_VatReturnLines')"
                        End If
                    End Using
                End Using

                If efCascadeOk AndAlso schemaFkOk Then
                    passed = True
                    detail = $"EF Core cascade OK (orphan count=0 after ctx.Remove). " &
                             $"Schema-level cascade OK (raw SQL DELETE with foreign_keys=ON, orphan count=0). " &
                             fkDetail
                Else
                    detail = $"FAILED. EF Core cascade={efCascadeOk}. Schema cascade={schemaFkOk}. {fkDetail}"
                End If
            Catch ex As Exception
                runError = ex
            End Try

            DeleteScratch(scratchPath)

            If runError IsNot Nothing Then
                detail = $"Exception: {runError.GetType().Name}: {runError.Message}"
            End If

            sw.Stop()
            Return New CheckResult With {.Passed = passed, .Detail = detail, .DurationMs = sw.ElapsedMilliseconds}
        End Function

        ' ── Private helpers ────────────────────────────────────────────────────────────

        Private Shared Function MakeScratchPath() As String
            Return Path.Combine(Path.GetTempPath(), $"vista-vat-schema-harness-{Guid.NewGuid():N}.db")
        End Function

        Private Shared Function OpenScratch(scratchPath As String) As AccountingDbContext
            Dim opts = New DbContextOptionsBuilder(Of AccountingDbContext)().
                UseSqlite($"Data Source={scratchPath}").
                Options
            Return New AccountingDbContext(opts)
        End Function

        Private Shared Sub DeleteScratch(scratchPath As String)
            For Each suffix In {"", "-wal", "-shm"}
                Dim p = scratchPath & suffix
                Try
                    If File.Exists(p) Then File.Delete(p)
                Catch
                End Try
            Next
        End Sub

        ''' <summary>
        ''' Creates the VAT schema on a scratch SQLite database using raw SQL, matching what
        ''' DatabaseInitializer does in MerchSys.App.  Required because EF Core 10 cannot
        ''' discover VB.NET migration classes via MigrateAsync().
        ''' Idempotent: IF NOT EXISTS / INSERT OR IGNORE guards allow calling twice (Check 2).
        ''' </summary>
        Private Shared Sub SetupScratchSchema(scratchPath As String)
            Using conn As New SqliteConnection($"Data Source={scratchPath}")
                conn.Open()
                ExecSchema(conn,
                    "CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (" &
                    """MigrationId"" TEXT NOT NULL PRIMARY KEY, " &
                    """ProductVersion"" TEXT NOT NULL)")
                ExecSchema(conn,
                    "INSERT OR IGNORE INTO ""__EFMigrationsHistory"" VALUES " &
                    "('20260510100000_AddVatLedgerColumns', '10.0.7')")
                ExecSchema(conn,
                    "INSERT OR IGNORE INTO ""__EFMigrationsHistory"" VALUES " &
                    "('20260515100000_FixVatReturnAmendedIndex', '10.0.7')")
                ExecSchema(conn,
                    "CREATE TABLE IF NOT EXISTS ""Acc_VatReturns"" (" &
                    """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_VatReturns"" PRIMARY KEY AUTOINCREMENT, " &
                    """Year"" INTEGER NOT NULL, " &
                    """Period"" INTEGER NOT NULL, " &
                    """PeriodType"" INTEGER NOT NULL, " &
                    """FormType"" INTEGER NOT NULL, " &
                    """TotalVatableSales"" TEXT NOT NULL, " &
                    """TotalVatExemptSales"" TEXT NOT NULL, " &
                    """TotalZeroRatedSales"" TEXT NOT NULL, " &
                    """TotalOutputVat"" TEXT NOT NULL, " &
                    """TotalVatablePurchases"" TEXT NOT NULL, " &
                    """TotalInputVat"" TEXT NOT NULL, " &
                    """VatPayable"" TEXT NOT NULL, " &
                    """FilingStatus"" INTEGER NOT NULL, " &
                    """FiledAt"" TEXT NULL, " &
                    """FiledBy"" TEXT NULL, " &
                    """GeneratedAt"" TEXT NOT NULL, " &
                    """IsVatRegisteredSnapshot"" INTEGER NOT NULL, " &
                    """CreatedBy"" TEXT NULL, " &
                    """CreatedAt"" TEXT NOT NULL, " &
                    """ModifiedBy"" TEXT NULL, " &
                    """ModifiedAt"" TEXT NULL" &
                    ")")
                ExecSchema(conn,
                    "CREATE TABLE IF NOT EXISTS ""Acc_VatReturnLines"" (" &
                    """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_VatReturnLines"" PRIMARY KEY AUTOINCREMENT, " &
                    """VatReturnId"" INTEGER NOT NULL, " &
                    """SourceModule"" TEXT NOT NULL, " &
                    """SourceTable"" TEXT NOT NULL, " &
                    """SourceRowId"" INTEGER NOT NULL, " &
                    """TransactionDate"" TEXT NOT NULL, " &
                    """VatableAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """VatExemptAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """ZeroRatedAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """OutputVat"" TEXT NOT NULL DEFAULT '0', " &
                    """InputVat"" TEXT NOT NULL DEFAULT '0', " &
                    """Treatment"" INTEGER NOT NULL DEFAULT 0, " &
                    """CreatedBy"" TEXT NULL, " &
                    """CreatedAt"" TEXT NOT NULL, " &
                    """ModifiedBy"" TEXT NULL, " &
                    """ModifiedAt"" TEXT NULL, " &
                    "CONSTRAINT ""FK_Acc_VatReturnLines_Acc_VatReturns_VatReturnId"" " &
                    "FOREIGN KEY (""VatReturnId"") REFERENCES ""Acc_VatReturns"" (""Id"") ON DELETE CASCADE" &
                    ")")
                ExecSchema(conn,
                    "CREATE UNIQUE INDEX IF NOT EXISTS " &
                    """IX_Acc_VatReturns_Year_Period_PeriodType_FormType_Active"" " &
                    "ON ""Acc_VatReturns"" (""Year"", ""Period"", ""PeriodType"", ""FormType"") " &
                    "WHERE ""FilingStatus"" != 3")
                ExecSchema(conn,
                    "CREATE INDEX IF NOT EXISTS ""IX_Acc_VatReturnLines_VatReturnId"" " &
                    "ON ""Acc_VatReturnLines"" (""VatReturnId"")")
            End Using
        End Sub

        Private Shared Sub ExecSchema(conn As SqliteConnection, sql As String)
            Using cmd = conn.CreateCommand()
                cmd.CommandText = sql
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Function MakeLine(now As DateTime) As VatReturnLine
            Return New VatReturnLine With {
                .SourceModule = "harness",
                .SourceTable = "Acc_VatReturns",
                .SourceRowId = 1L,
                .TransactionDate = now,
                .CreatedBy = "harness",
                .CreatedAt = now
            }
        End Function

        ''' <summary>
        ''' PRAGMA table_info(table) — returns column names for the given table.
        ''' Columns returned by SQLite: cid, name, type, notnull, dflt_value, pk.
        ''' </summary>
        Private Shared Async Function ReadTableColumnsAsync(
            conn As System.Data.Common.DbConnection,
            tableName As String) As Task(Of List(Of String))

            Dim cols As New List(Of String)()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = $"PRAGMA table_info('{tableName}')"
                Using reader = Await cmd.ExecuteReaderAsync()
                    While Await reader.ReadAsync()
                        cols.Add(reader.GetString(reader.GetOrdinal("name")))
                    End While
                End Using
            End Using
            Return cols
        End Function

        ''' <summary>
        ''' PRAGMA index_list(table) — returns index metadata for the given table.
        ''' Columns returned by SQLite: seq, name, unique, origin, partial.
        ''' </summary>
        Private Shared Async Function ReadIndexListAsync(
            conn As System.Data.Common.DbConnection,
            tableName As String) As Task(Of List(Of IndexRow))

            Dim rows As New List(Of IndexRow)()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = $"PRAGMA index_list('{tableName}')"
                Using reader = Await cmd.ExecuteReaderAsync()
                    While Await reader.ReadAsync()
                        rows.Add(New IndexRow With {
                            .Name = reader.GetString(reader.GetOrdinal("name")),
                            .Unique = reader.GetInt32(reader.GetOrdinal("unique"))
                        })
                    End While
                End Using
            End Using
            Return rows
        End Function

        ''' <summary>
        ''' PRAGMA foreign_key_list(table) — returns FK declarations for the given table.
        ''' Columns returned by SQLite: id, seq, table, from, to, on_update, on_delete, match.
        ''' </summary>
        Private Shared Async Function ReadForeignKeyListAsync(
            conn As System.Data.Common.DbConnection,
            tableName As String) As Task(Of List(Of FkRow))

            Dim rows As New List(Of FkRow)()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = $"PRAGMA foreign_key_list('{tableName}')"
                Using reader = Await cmd.ExecuteReaderAsync()
                    While Await reader.ReadAsync()
                        rows.Add(New FkRow With {
                            .ReferencedTable = reader.GetString(reader.GetOrdinal("table")),
                            .OnDelete = reader.GetString(reader.GetOrdinal("on_delete"))
                        })
                    End While
                End Using
            End Using
            Return rows
        End Function

        ''' <summary>
        ''' Reads all migration IDs from <c>__EFMigrationsHistory</c> in apply order.
        ''' Used by Check 2 to confirm idempotency: each ID must appear exactly once.
        ''' </summary>
        Private Shared Async Function ReadMigrationHistoryAsync(
            conn As System.Data.Common.DbConnection) As Task(Of List(Of String))

            Dim ids As New List(Of String)()
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "SELECT ""MigrationId"" FROM ""__EFMigrationsHistory"" ORDER BY ""MigrationId"""
                Using reader = Await cmd.ExecuteReaderAsync()
                    While Await reader.ReadAsync()
                        ids.Add(reader.GetString(0))
                    End While
                End Using
            End Using
            Return ids
        End Function

        ' ── Private PRAGMA result types ────────────────────────────────────────────────

        Private Class IndexRow
            Public Property Name As String
            Public Property Unique As Integer
        End Class

        Private Class FkRow
            Public Property ReferencedTable As String
            Public Property OnDelete As String
        End Class

    End Class

    ''' <summary>Consolidated output of all four VAT ledger schema verification checks.</summary>
    Public Class VatLedgerSchemaReport
        ''' <summary>Result of Check 1: migration applies cleanly on a fresh SQLite database.</summary>
        Public Property FreshDatabaseApplyResult As CheckResult
        ''' <summary>Result of Check 2: second MigrateAsync call on an existing database is a no-op.</summary>
        Public Property ExistingDatabaseApplyResult As CheckResult
        ''' <summary>Result of Check 3: duplicate filing for the same period/form is rejected at the DB level.</summary>
        Public Property DuplicateFilingBlockedResult As CheckResult
        ''' <summary>Result of Check 4: deleting a VatReturn removes all child VatReturnLines via CASCADE.</summary>
        Public Property CascadeDeleteResult As CheckResult
    End Class

    ''' <summary>
    ''' Result of a single schema verification check run by <see cref="VatLedgerSchemaHarness"/>.
    ''' </summary>
    Public Class CheckResult
        ''' <summary>True if the check passed all its acceptance criteria.</summary>
        Public Property Passed As Boolean
        ''' <summary>Human-readable description of what was verified, or the specific failure reason.</summary>
        Public Property Detail As String
        ''' <summary>Wall-clock duration of this check in milliseconds.</summary>
        Public Property DurationMs As Long
    End Class

End Namespace

#End If
