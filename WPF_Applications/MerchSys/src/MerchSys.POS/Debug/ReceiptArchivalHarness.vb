#If DEBUG Then
Imports System.IO
Imports System.Threading
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Logging.Abstractions
Imports Microsoft.Extensions.Options
Imports Microsoft.Extensions.Primitives
Imports MerchSys.POS.Data
Imports MerchSys.POS.Services.Archival

Namespace Debug

    Public Class ReceiptArchivalHarnessResult
        Public Property ReceiptsMoved As Integer
        Public Property LiveRemaining As Integer
        Public Property ArchiveCount As Integer
        Public Property ElapsedMs As Long
        Public Property Passed As Boolean
        Public Property Notes As String
    End Class

    ''' <summary>
    ''' Debug-only harness for POS-16 Test 4.
    ''' Seeds 100 expired + 100 in-window receipts in a scratch SQLite database,
    ''' runs <see cref="ReceiptArchivalService.ArchiveEligibleAsync"/>, and verifies
    ''' that exactly 100 receipts were moved to the archive table.
    ''' </summary>
    Public Module ReceiptArchivalHarness

        Public Async Function RunAsync() As Task(Of ReceiptArchivalHarnessResult)
            Dim harnessResult As New ReceiptArchivalHarnessResult()
            Dim dbPath = Path.Combine(Path.GetTempPath(), $"vista-archival-harness-{Guid.NewGuid():N}.db")
            Dim connStr = $"Data Source={dbPath}"
            Dim sw = System.Diagnostics.Stopwatch.StartNew()

            ' ── 1. Create EF schema ───────────────────────────────────────────
            Dim efOptions = New DbContextOptionsBuilder(Of POSDbContext)().UseSqlite(connStr).Options
            Using ctx = New POSDbContext(efOptions)
                Await ctx.Database.EnsureCreatedAsync()
            End Using

            ' Pos_ArchivalSession is created by DatabaseInitializer, not EF Core.
            ' Create it here so the service can set and clear the session flag.
            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TABLE IF NOT EXISTS ""Pos_ArchivalSession"" (" &
                        """key"" TEXT NOT NULL PRIMARY KEY, " &
                        """value"" INTEGER NOT NULL, " &
                        """expires_at"" TEXT NOT NULL)"
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            ' ── 2. Seed test data via raw SQL (bypasses ImmutableReceiptInterceptor) ──
            ' Expired batch (1–100): RetentionExpiresAt in the past → eligible for archival.
            ' In-window batch (101–200): RetentionExpiresAt in the future → must not be archived.
            Dim nowStr = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")

            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()

                Dim txSql =
                    "INSERT INTO Pos_SalesTransactions " &
                    "(TransactionNumber, TransactionDate, PaymentMethod, SubTotal, DiscountAmount, " &
                    "VatAmount, TotalAmount, AmountTendered, ChangeAmount, IsVoided, IsDeleted, " &
                    "CreatedAt, VatableSales, VatExemptSales, ZeroRatedSales, " &
                    "VatRateSnapshot, IsVatRegisteredSnapshot) " &
                    "VALUES (@num, '2015-01-15T00:00:00', 0, 100, 0, 0, 100, 100, 0, 0, 0, " &
                    "@now, 0, 0, 0, 0.12, 0)"

                Dim receiptSql =
                    "INSERT INTO Pos_OfficialReceipts " &
                    "(TransactionId, ReceiptNumber, BusinessName, IssueDate, " &
                    "TotalAmount, VatAmount, IsVatRegistered, CreatedAt) " &
                    "VALUES (@txId, @rnum, 'Villon Farm Supply', '2015-01-15T00:00:00', " &
                    "100, 0, 0, @now)"

                Dim integritySql =
                    "INSERT INTO Pos_ReceiptIntegrity " &
                    "(ReceiptId, IntegrityHash, PreviousHash, RetentionExpiresAt, IsImmutable, " &
                    "HashAlgorithm, CanonicalPayload, CreatedAt) " &
                    "VALUES (@rid, @hash, '', @retExp, 1, 'SHA-256-v1', '{}', @now)"

                For i = 1 To 200
                    Dim isExpired = i <= 100
                    Dim prefix = If(isExpired, "EXP", "WIN")
                    Dim retExp = If(isExpired, "2025-01-01T00:00:00", "2035-01-01T00:00:00")

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = txSql
                        cmd.Parameters.AddWithValue("@num", $"TX-TEST-{prefix}-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim txId As Long
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT last_insert_rowid()"
                        txId = CType(cmd.ExecuteScalar(), Long)
                    End Using

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = receiptSql
                        cmd.Parameters.AddWithValue("@txId", txId)
                        cmd.Parameters.AddWithValue("@rnum", $"OR-TEST-{prefix}-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim receiptId As Long
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT last_insert_rowid()"
                        receiptId = CType(cmd.ExecuteScalar(), Long)
                    End Using

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = integritySql
                        cmd.Parameters.AddWithValue("@rid", receiptId)
                        cmd.Parameters.AddWithValue("@hash", $"testhash-{prefix}-{i:D4}")
                        cmd.Parameters.AddWithValue("@retExp", retExp)
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End Using

            ' ── 3. Build minimal DI and run archival ─────────────────────────
            Dim services As New ServiceCollection()
            services.AddDbContext(Of POSDbContext)(
                Sub(o) o.UseSqlite(connStr),
                ServiceLifetime.Scoped)
            Dim provider = services.BuildServiceProvider()

            Dim scopeFactory = provider.GetRequiredService(Of IServiceScopeFactory)()
            Dim archivalOpts = Microsoft.Extensions.Options.Options.Create(
                New ReceiptArchivalOptions() With {.Enabled = True, .BatchSize = 500})
            Dim archivalConfig As IConfiguration = New EmptyConfiguration()

            Dim svc As New ReceiptArchivalService(
                scopeFactory,
                archivalOpts,
                NullLogger(Of ReceiptArchivalService).Instance,
                archivalConfig)

            Dim batchResult As ReceiptArchivalBatchResult = Nothing
            Dim runEx As Exception = Nothing
            Try
                batchResult = Await svc.ArchiveEligibleAsync(DateTime.UtcNow, 500, CancellationToken.None)
            Catch ex As Exception
                runEx = ex
            End Try

            ' ── 4. Verify counts ─────────────────────────────────────────────
            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT COUNT(*) FROM Pos_OfficialReceipts"
                    harnessResult.LiveRemaining = CInt(cmd.ExecuteScalar())
                End Using
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT COUNT(*) FROM Pos_OfficialReceiptArchive"
                    harnessResult.ArchiveCount = CInt(cmd.ExecuteScalar())
                End Using
            End Using

            sw.Stop()
            harnessResult.ElapsedMs = sw.ElapsedMilliseconds
            harnessResult.ReceiptsMoved = If(batchResult IsNot Nothing, batchResult.ReceiptsMoved, 0)
            harnessResult.Passed = (harnessResult.ReceiptsMoved = 100 AndAlso
                                    harnessResult.LiveRemaining = 100 AndAlso
                                    harnessResult.ArchiveCount = 100)

            Dim exNote = If(runEx IsNot Nothing, $" Exception={runEx.GetType().Name}: {runEx.Message}", "")
            harnessResult.Notes =
                $"ReceiptsMoved={harnessResult.ReceiptsMoved}, " &
                $"LiveRemaining={harnessResult.LiveRemaining}, " &
                $"ArchiveCount={harnessResult.ArchiveCount}, " &
                $"ElapsedMs={harnessResult.ElapsedMs}" & exNote

            ' ── 5. Cleanup scratch DB ────────────────────────────────────────
            Dim cleanupEx As Exception = Nothing
            Try
                If File.Exists(dbPath) Then File.Delete(dbPath)
                If File.Exists(dbPath & "-wal") Then File.Delete(dbPath & "-wal")
                If File.Exists(dbPath & "-shm") Then File.Delete(dbPath & "-shm")
            Catch ex As Exception
                cleanupEx = ex
            End Try

            If cleanupEx IsNot Nothing Then
                harnessResult.Notes &= $" (cleanup warning: {cleanupEx.Message})"
            End If

            System.Console.WriteLine(
                $"[ReceiptArchivalHarness] {If(harnessResult.Passed, "PASS", "FAIL")} — {harnessResult.Notes}")

            Return harnessResult
        End Function

        ''' <summary>
        ''' Batch-size variant for POS-16 Test 5.
        ''' Seeds 100 expired receipts, runs archival with batchSize=50.
        ''' Pass criteria: ReceiptsMoved=50 AND HadMoreEligible=True.
        ''' </summary>
        Public Async Function RunBatchSizeTestAsync() As Task(Of ReceiptArchivalHarnessResult)
            Dim harnessResult As New ReceiptArchivalHarnessResult()
            Dim dbPath = Path.Combine(Path.GetTempPath(), $"vista-archival-batch-harness-{Guid.NewGuid():N}.db")
            Dim connStr = $"Data Source={dbPath}"
            Dim sw = System.Diagnostics.Stopwatch.StartNew()

            ' ── 1. Create schema ─────────────────────────────────────────────
            Dim efOptions = New DbContextOptionsBuilder(Of POSDbContext)().UseSqlite(connStr).Options
            Using ctx = New POSDbContext(efOptions)
                Await ctx.Database.EnsureCreatedAsync()
            End Using

            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TABLE IF NOT EXISTS ""Pos_ArchivalSession"" (" &
                        """key"" TEXT NOT NULL PRIMARY KEY, " &
                        """value"" INTEGER NOT NULL, " &
                        """expires_at"" TEXT NOT NULL)"
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            ' ── 2. Seed 100 expired receipts only ────────────────────────────
            Dim nowStr = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()

                Dim txSql =
                    "INSERT INTO Pos_SalesTransactions " &
                    "(TransactionNumber, TransactionDate, PaymentMethod, SubTotal, DiscountAmount, " &
                    "VatAmount, TotalAmount, AmountTendered, ChangeAmount, IsVoided, IsDeleted, " &
                    "CreatedAt, VatableSales, VatExemptSales, ZeroRatedSales, " &
                    "VatRateSnapshot, IsVatRegisteredSnapshot) " &
                    "VALUES (@num, '2015-01-15T00:00:00', 0, 100, 0, 0, 100, 100, 0, 0, 0, " &
                    "@now, 0, 0, 0, 0.12, 0)"

                Dim receiptSql =
                    "INSERT INTO Pos_OfficialReceipts " &
                    "(TransactionId, ReceiptNumber, BusinessName, IssueDate, " &
                    "TotalAmount, VatAmount, IsVatRegistered, CreatedAt) " &
                    "VALUES (@txId, @rnum, 'Villon Farm Supply', '2015-01-15T00:00:00', " &
                    "100, 0, 0, @now)"

                Dim integritySql =
                    "INSERT INTO Pos_ReceiptIntegrity " &
                    "(ReceiptId, IntegrityHash, PreviousHash, RetentionExpiresAt, IsImmutable, " &
                    "HashAlgorithm, CanonicalPayload, CreatedAt) " &
                    "VALUES (@rid, @hash, '', '2025-01-01T00:00:00', 1, 'SHA-256-v1', '{}', @now)"

                For i = 1 To 100
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = txSql
                        cmd.Parameters.AddWithValue("@num", $"TX-BATCH-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim txId As Long
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT last_insert_rowid()"
                        txId = CType(cmd.ExecuteScalar(), Long)
                    End Using

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = receiptSql
                        cmd.Parameters.AddWithValue("@txId", txId)
                        cmd.Parameters.AddWithValue("@rnum", $"OR-BATCH-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim receiptId As Long
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT last_insert_rowid()"
                        receiptId = CType(cmd.ExecuteScalar(), Long)
                    End Using

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = integritySql
                        cmd.Parameters.AddWithValue("@rid", receiptId)
                        cmd.Parameters.AddWithValue("@hash", $"batchhash-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End Using

            ' ── 3. Run archival with batchSize=50 ────────────────────────────
            Dim services As New ServiceCollection()
            services.AddDbContext(Of POSDbContext)(
                Sub(o) o.UseSqlite(connStr),
                ServiceLifetime.Scoped)
            Dim provider = services.BuildServiceProvider()

            Dim scopeFactory = provider.GetRequiredService(Of IServiceScopeFactory)()
            Dim archivalOpts = Microsoft.Extensions.Options.Options.Create(
                New ReceiptArchivalOptions() With {.Enabled = True, .BatchSize = 50})
            Dim archivalConfig As IConfiguration = New EmptyConfiguration()

            Dim svc As New ReceiptArchivalService(
                scopeFactory,
                archivalOpts,
                NullLogger(Of ReceiptArchivalService).Instance,
                archivalConfig)

            Dim batchResult As ReceiptArchivalBatchResult = Nothing
            Dim runEx As Exception = Nothing
            Try
                batchResult = Await svc.ArchiveEligibleAsync(DateTime.UtcNow, 50, CancellationToken.None)
            Catch ex As Exception
                runEx = ex
            End Try

            ' ── 4. Verify counts ─────────────────────────────────────────────
            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT COUNT(*) FROM Pos_OfficialReceipts"
                    harnessResult.LiveRemaining = CInt(cmd.ExecuteScalar())
                End Using
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT COUNT(*) FROM Pos_OfficialReceiptArchive"
                    harnessResult.ArchiveCount = CInt(cmd.ExecuteScalar())
                End Using
            End Using

            sw.Stop()
            harnessResult.ElapsedMs = sw.ElapsedMilliseconds
            harnessResult.ReceiptsMoved = If(batchResult IsNot Nothing, batchResult.ReceiptsMoved, 0)
            Dim hadMore = If(batchResult IsNot Nothing, batchResult.HadMoreEligible, False)

            harnessResult.Passed = (harnessResult.ReceiptsMoved = 50 AndAlso
                                    hadMore AndAlso
                                    harnessResult.LiveRemaining = 50 AndAlso
                                    harnessResult.ArchiveCount = 50)

            Dim exNote = If(runEx IsNot Nothing, $" Exception={runEx.GetType().Name}: {runEx.Message}", "")
            harnessResult.Notes =
                $"ReceiptsMoved={harnessResult.ReceiptsMoved}, " &
                $"HadMoreEligible={hadMore}, " &
                $"LiveRemaining={harnessResult.LiveRemaining}, " &
                $"ArchiveCount={harnessResult.ArchiveCount}, " &
                $"ElapsedMs={harnessResult.ElapsedMs}" & exNote

            Dim cleanupEx As Exception = Nothing
            Try
                If File.Exists(dbPath) Then File.Delete(dbPath)
                If File.Exists(dbPath & "-wal") Then File.Delete(dbPath & "-wal")
                If File.Exists(dbPath & "-shm") Then File.Delete(dbPath & "-shm")
            Catch ex As Exception
                cleanupEx = ex
            End Try

            If cleanupEx IsNot Nothing Then
                harnessResult.Notes &= $" (cleanup warning: {cleanupEx.Message})"
            End If

            System.Console.WriteLine(
                $"[ReceiptArchivalHarness.BatchSize] {If(harnessResult.Passed, "PASS", "FAIL")} — {harnessResult.Notes}")

            Return harnessResult
        End Function

        ''' <summary>
        ''' Fiscal-year guard variant for POS-16 Test 6.
        ''' Seeds 20 receipts with IssueDate in the current year (2026) and RetentionExpiresAt
        ''' in the past. Pass criteria: ReceiptsMoved=0 (fiscal-year guard blocks archival).
        ''' </summary>
        Public Async Function RunFiscalYearGuardTestAsync() As Task(Of ReceiptArchivalHarnessResult)
            Dim harnessResult As New ReceiptArchivalHarnessResult()
            Dim dbPath = Path.Combine(Path.GetTempPath(), $"vista-archival-fiscal-harness-{Guid.NewGuid():N}.db")
            Dim connStr = $"Data Source={dbPath}"
            Dim sw = System.Diagnostics.Stopwatch.StartNew()

            ' ── 1. Create schema ─────────────────────────────────────────────
            Dim efOptions = New DbContextOptionsBuilder(Of POSDbContext)().UseSqlite(connStr).Options
            Using ctx = New POSDbContext(efOptions)
                Await ctx.Database.EnsureCreatedAsync()
            End Using

            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TABLE IF NOT EXISTS ""Pos_ArchivalSession"" (" &
                        """key"" TEXT NOT NULL PRIMARY KEY, " &
                        """value"" INTEGER NOT NULL, " &
                        """expires_at"" TEXT NOT NULL)"
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            ' ── 2. Seed 20 current-year receipts with past RetentionExpiresAt ─
            ' IssueDate.Year = 2026 (current fiscal year) — must NOT be archived
            ' even though RetentionExpiresAt is in the past.
            Dim nowStr = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            Dim currentYear = DateTime.UtcNow.Year
            Dim issueDateStr = $"{currentYear}-03-15T00:00:00"

            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()

                Dim txSql =
                    "INSERT INTO Pos_SalesTransactions " &
                    "(TransactionNumber, TransactionDate, PaymentMethod, SubTotal, DiscountAmount, " &
                    "VatAmount, TotalAmount, AmountTendered, ChangeAmount, IsVoided, IsDeleted, " &
                    "CreatedAt, VatableSales, VatExemptSales, ZeroRatedSales, " &
                    "VatRateSnapshot, IsVatRegisteredSnapshot) " &
                    "VALUES (@num, @issueDate, 0, 100, 0, 0, 100, 100, 0, 0, 0, " &
                    "@now, 0, 0, 0, 0.12, 0)"

                Dim receiptSql =
                    "INSERT INTO Pos_OfficialReceipts " &
                    "(TransactionId, ReceiptNumber, BusinessName, IssueDate, " &
                    "TotalAmount, VatAmount, IsVatRegistered, CreatedAt) " &
                    "VALUES (@txId, @rnum, 'Villon Farm Supply', @issueDate, 100, 0, 0, @now)"

                Dim integritySql =
                    "INSERT INTO Pos_ReceiptIntegrity " &
                    "(ReceiptId, IntegrityHash, PreviousHash, RetentionExpiresAt, IsImmutable, " &
                    "HashAlgorithm, CanonicalPayload, CreatedAt) " &
                    "VALUES (@rid, @hash, '', '2025-01-01T00:00:00', 1, 'SHA-256-v1', '{}', @now)"

                For i = 1 To 20
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = txSql
                        cmd.Parameters.AddWithValue("@num", $"TX-FISCAL-{i:D4}")
                        cmd.Parameters.AddWithValue("@issueDate", issueDateStr)
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim txId As Long
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT last_insert_rowid()"
                        txId = CType(cmd.ExecuteScalar(), Long)
                    End Using

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = receiptSql
                        cmd.Parameters.AddWithValue("@txId", txId)
                        cmd.Parameters.AddWithValue("@rnum", $"OR-FISCAL-{i:D4}")
                        cmd.Parameters.AddWithValue("@issueDate", issueDateStr)
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim receiptId As Long
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT last_insert_rowid()"
                        receiptId = CType(cmd.ExecuteScalar(), Long)
                    End Using

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = integritySql
                        cmd.Parameters.AddWithValue("@rid", receiptId)
                        cmd.Parameters.AddWithValue("@hash", $"fiscalhash-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End Using

            ' ── 3. Run archival ───────────────────────────────────────────────
            Dim services As New ServiceCollection()
            services.AddDbContext(Of POSDbContext)(
                Sub(o) o.UseSqlite(connStr),
                ServiceLifetime.Scoped)
            Dim provider = services.BuildServiceProvider()

            Dim scopeFactory = provider.GetRequiredService(Of IServiceScopeFactory)()
            Dim archivalOpts = Microsoft.Extensions.Options.Options.Create(
                New ReceiptArchivalOptions() With {.Enabled = True, .BatchSize = 500})
            Dim archivalConfig As IConfiguration = New EmptyConfiguration()

            Dim svc As New ReceiptArchivalService(
                scopeFactory,
                archivalOpts,
                NullLogger(Of ReceiptArchivalService).Instance,
                archivalConfig)

            Dim batchResult As ReceiptArchivalBatchResult = Nothing
            Dim runEx As Exception = Nothing
            Try
                batchResult = Await svc.ArchiveEligibleAsync(DateTime.UtcNow, 500, CancellationToken.None)
            Catch ex As Exception
                runEx = ex
            End Try

            ' ── 4. Verify counts ─────────────────────────────────────────────
            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT COUNT(*) FROM Pos_OfficialReceipts"
                    harnessResult.LiveRemaining = CInt(cmd.ExecuteScalar())
                End Using
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT COUNT(*) FROM Pos_OfficialReceiptArchive"
                    harnessResult.ArchiveCount = CInt(cmd.ExecuteScalar())
                End Using
            End Using

            sw.Stop()
            harnessResult.ElapsedMs = sw.ElapsedMilliseconds
            harnessResult.ReceiptsMoved = If(batchResult IsNot Nothing, batchResult.ReceiptsMoved, 0)

            ' Pass: nothing archived, all 20 still in live table
            harnessResult.Passed = (harnessResult.ReceiptsMoved = 0 AndAlso
                                    harnessResult.LiveRemaining = 20 AndAlso
                                    harnessResult.ArchiveCount = 0)

            Dim exNote = If(runEx IsNot Nothing, $" Exception={runEx.GetType().Name}: {runEx.Message}", "")
            harnessResult.Notes =
                $"ReceiptsMoved={harnessResult.ReceiptsMoved} (expected 0), " &
                $"LiveRemaining={harnessResult.LiveRemaining} (expected 20), " &
                $"ArchiveCount={harnessResult.ArchiveCount} (expected 0), " &
                $"IssueDate.Year={currentYear}, ElapsedMs={harnessResult.ElapsedMs}" & exNote

            Dim cleanupEx As Exception = Nothing
            Try
                If File.Exists(dbPath) Then File.Delete(dbPath)
                If File.Exists(dbPath & "-wal") Then File.Delete(dbPath & "-wal")
                If File.Exists(dbPath & "-shm") Then File.Delete(dbPath & "-shm")
            Catch ex As Exception
                cleanupEx = ex
            End Try

            If cleanupEx IsNot Nothing Then
                harnessResult.Notes &= $" (cleanup warning: {cleanupEx.Message})"
            End If

            System.Console.WriteLine(
                $"[ReceiptArchivalHarness.FiscalYearGuard] {If(harnessResult.Passed, "PASS", "FAIL")} — {harnessResult.Notes}")

            Return harnessResult
        End Function

        ''' <summary>
        ''' Rollback variant for POS-16 Test 7.
        ''' Seeds 20 expired receipts, drops Pos_OfficialReceiptArchive to force the
        ''' archive-insert to fail, then verifies the live table is untouched (LiveRemaining=20).
        ''' ArchiveEligibleAsync is expected to throw — that is correct behaviour.
        ''' </summary>
        Public Async Function RunRollbackTestAsync() As Task(Of ReceiptArchivalHarnessResult)
            Dim harnessResult As New ReceiptArchivalHarnessResult()
            Dim dbPath = Path.Combine(Path.GetTempPath(), $"vista-archival-rollback-harness-{Guid.NewGuid():N}.db")
            Dim connStr = $"Data Source={dbPath}"
            Dim sw = System.Diagnostics.Stopwatch.StartNew()

            ' ── 1. Create schema ─────────────────────────────────────────────
            Dim efOptions = New DbContextOptionsBuilder(Of POSDbContext)().UseSqlite(connStr).Options
            Using ctx = New POSDbContext(efOptions)
                Await ctx.Database.EnsureCreatedAsync()
            End Using

            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TABLE IF NOT EXISTS ""Pos_ArchivalSession"" (" &
                        """key"" TEXT NOT NULL PRIMARY KEY, " &
                        """value"" INTEGER NOT NULL, " &
                        """expires_at"" TEXT NOT NULL)"
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            ' ── 2. Seed 20 expired receipts ──────────────────────────────────
            Dim nowStr = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")

            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()

                Dim txSql =
                    "INSERT INTO Pos_SalesTransactions " &
                    "(TransactionNumber, TransactionDate, PaymentMethod, SubTotal, DiscountAmount, " &
                    "VatAmount, TotalAmount, AmountTendered, ChangeAmount, IsVoided, IsDeleted, " &
                    "CreatedAt, VatableSales, VatExemptSales, ZeroRatedSales, " &
                    "VatRateSnapshot, IsVatRegisteredSnapshot) " &
                    "VALUES (@num, '2015-01-15T00:00:00', 0, 100, 0, 0, 100, 100, 0, 0, 0, " &
                    "@now, 0, 0, 0, 0.12, 0)"

                Dim receiptSql =
                    "INSERT INTO Pos_OfficialReceipts " &
                    "(TransactionId, ReceiptNumber, BusinessName, IssueDate, " &
                    "TotalAmount, VatAmount, IsVatRegistered, CreatedAt) " &
                    "VALUES (@txId, @rnum, 'Villon Farm Supply', '2015-01-15T00:00:00', " &
                    "100, 0, 0, @now)"

                Dim integritySql =
                    "INSERT INTO Pos_ReceiptIntegrity " &
                    "(ReceiptId, IntegrityHash, PreviousHash, RetentionExpiresAt, IsImmutable, " &
                    "HashAlgorithm, CanonicalPayload, CreatedAt) " &
                    "VALUES (@rid, @hash, '', '2025-01-01T00:00:00', 1, 'SHA-256-v1', '{}', @now)"

                For i = 1 To 20
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = txSql
                        cmd.Parameters.AddWithValue("@num", $"TX-ROLLBACK-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim txId As Long
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT last_insert_rowid()"
                        txId = CType(cmd.ExecuteScalar(), Long)
                    End Using

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = receiptSql
                        cmd.Parameters.AddWithValue("@txId", txId)
                        cmd.Parameters.AddWithValue("@rnum", $"OR-ROLLBACK-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using

                    Dim receiptId As Long
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT last_insert_rowid()"
                        receiptId = CType(cmd.ExecuteScalar(), Long)
                    End Using

                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = integritySql
                        cmd.Parameters.AddWithValue("@rid", receiptId)
                        cmd.Parameters.AddWithValue("@hash", $"rollbackhash-{i:D4}")
                        cmd.Parameters.AddWithValue("@now", nowStr)
                        cmd.ExecuteNonQuery()
                    End Using
                Next

                ' ── 3. Drop the archive table to force the insert to fail ────
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "DROP TABLE IF EXISTS ""Pos_OfficialReceiptArchive"""
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            ' ── 4. Run archival — expect an exception ─────────────────────────
            Dim services As New ServiceCollection()
            services.AddDbContext(Of POSDbContext)(
                Sub(o) o.UseSqlite(connStr),
                ServiceLifetime.Scoped)
            Dim provider = services.BuildServiceProvider()

            Dim scopeFactory = provider.GetRequiredService(Of IServiceScopeFactory)()
            Dim archivalOpts = Microsoft.Extensions.Options.Options.Create(
                New ReceiptArchivalOptions() With {.Enabled = True, .BatchSize = 500})

            Dim svc As New ReceiptArchivalService(
                scopeFactory,
                archivalOpts,
                NullLogger(Of ReceiptArchivalService).Instance,
                New EmptyConfiguration())

            Dim thrownEx As Exception = Nothing
            Try
                Await svc.ArchiveEligibleAsync(DateTime.UtcNow, 500, CancellationToken.None)
            Catch ex As Exception
                thrownEx = ex
            End Try

            ' ── 5. Verify live table is intact after rollback ─────────────────
            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT COUNT(*) FROM Pos_OfficialReceipts"
                    harnessResult.LiveRemaining = CInt(cmd.ExecuteScalar())
                End Using
            End Using

            sw.Stop()
            harnessResult.ElapsedMs = sw.ElapsedMilliseconds
            harnessResult.ReceiptsMoved = 0
            harnessResult.ArchiveCount = 0

            ' Pass: service threw (expected), live table still has all 20 rows
            Dim exceptionThrown = thrownEx IsNot Nothing
            harnessResult.Passed = (exceptionThrown AndAlso harnessResult.LiveRemaining = 20)

            harnessResult.Notes =
                $"ExceptionThrown={exceptionThrown} (expected True), " &
                $"ExceptionType={If(thrownEx IsNot Nothing, thrownEx.GetType().Name, "none")}, " &
                $"LiveRemaining={harnessResult.LiveRemaining} (expected 20), " &
                $"ElapsedMs={harnessResult.ElapsedMs}"

            Dim cleanupEx As Exception = Nothing
            Try
                If File.Exists(dbPath) Then File.Delete(dbPath)
                If File.Exists(dbPath & "-wal") Then File.Delete(dbPath & "-wal")
                If File.Exists(dbPath & "-shm") Then File.Delete(dbPath & "-shm")
            Catch ex As Exception
                cleanupEx = ex
            End Try

            If cleanupEx IsNot Nothing Then
                harnessResult.Notes &= $" (cleanup warning: {cleanupEx.Message})"
            End If

            System.Console.WriteLine(
                $"[ReceiptArchivalHarness.Rollback] {If(harnessResult.Passed, "PASS", "FAIL")} — {harnessResult.Notes}")

            Return harnessResult
        End Function

    End Module

    ''' <summary>Returns Nothing for every key. Provides graceDays=0 to ReceiptArchivalService.</summary>
    Friend Class EmptyConfiguration
        Implements IConfiguration

        Default Public Property Item(key As String) As String Implements IConfiguration.Item
            Get
                Return Nothing
            End Get
            Set(value As String)
            End Set
        End Property

        Public Function GetSection(key As String) As IConfigurationSection Implements IConfiguration.GetSection
            Return New EmptyConfigurationSection(key)
        End Function

        Public Function GetChildren() As IEnumerable(Of IConfigurationSection) Implements IConfiguration.GetChildren
            Return Enumerable.Empty(Of IConfigurationSection)()
        End Function

        Public Function GetReloadToken() As IChangeToken Implements IConfiguration.GetReloadToken
            Return New CancellationChangeToken(CancellationToken.None)
        End Function
    End Class

    Friend Class EmptyConfigurationSection
        Implements IConfigurationSection

        Private ReadOnly _key As String

        Public Sub New(sectionKey As String)
            _key = sectionKey
        End Sub

        Default Public Property Item(key As String) As String Implements IConfiguration.Item
            Get
                Return Nothing
            End Get
            Set(value As String)
            End Set
        End Property

        Public ReadOnly Property Key As String Implements IConfigurationSection.Key
            Get
                Return _key
            End Get
        End Property

        Public ReadOnly Property Path As String Implements IConfigurationSection.Path
            Get
                Return _key
            End Get
        End Property

        Public Property Value As String Implements IConfigurationSection.Value
            Get
                Return Nothing
            End Get
            Set(value As String)
            End Set
        End Property

        Public Function GetSection(sectionKey As String) As IConfigurationSection Implements IConfiguration.GetSection
            Return New EmptyConfigurationSection(sectionKey)
        End Function

        Public Function GetChildren() As IEnumerable(Of IConfigurationSection) Implements IConfiguration.GetChildren
            Return Enumerable.Empty(Of IConfigurationSection)()
        End Function

        Public Function GetReloadToken() As IChangeToken Implements IConfiguration.GetReloadToken
            Return New CancellationChangeToken(CancellationToken.None)
        End Function
    End Class

End Namespace
#End If
