#If DEBUG Then

Imports System.IO
Imports System.Threading
Imports MediatR
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.Accounting.Enums
Imports MerchSys.Accounting.Services
Imports MerchSys.Accounting.Services.Insights
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Queries

Namespace Debug

    ''' <summary>
    ''' End-to-end smoke test for the VAT Payable tile pipeline (ACC-14).
    ''' Builds a scratch SQLite database, seeds one month of synthetic VAT ledger data
    ''' (VatableSales ₱100,000 / OutputVat ₱12,000 / InputVat ₱3,000 ⇒ payable ₱9,000),
    ''' resolves <see cref="IFinancialOverviewService"/> via a minimal isolated DI container,
    ''' and asserts that the decorated service returns the correct DTO values.
    ''' </summary>
    Public Class VatTileSmokeHarness

        ''' <summary>
        ''' Shared entry point — callable from the VS Immediate Window as
        ''' <c>? Await VatTileSmokeHarness.RunAsync(host)</c>.
        ''' </summary>
        Public Shared Async Function RunAsync(host As IHost) As Task(Of VatTileSmokeReport)
            Dim scratchPath = Path.Combine(
                Path.GetTempPath(),
                $"vista-vat-tile-smoke-{Guid.NewGuid():N}.db")

            Dim report As New VatTileSmokeReport()
            Dim smokeProvider As ServiceProvider = Nothing
            Dim runError As Exception = Nothing

            Try
                ' ── Build isolated DI container ───────────────────────────────────────
                Dim services As New ServiceCollection()

                services.AddLogging()

                services.AddDbContext(Of AccountingDbContext)(
                    Sub(o) o.UseSqlite($"Data Source={scratchPath}"),
                    ServiceLifetime.Scoped)

                ' SmokeSyncableRepository delegates SaveChangesWithJournalAsync to the
                ' underlying DbContext so VatReportingService writes persist to the scratch DB
                ' without requiring SyncJournalDbContext (not available in the isolated container).
                services.AddScoped(Of ISyncableRepository(Of AccountingDbContext))(
                    Function(sp) New SmokeSyncableRepository(
                        sp.GetRequiredService(Of AccountingDbContext)()))

                ' Scan this assembly: picks up SmokeVatConfigHandler (returns IsVatRegistered=True)
                ' and all Accounting notification handlers (unused in the harness, silently idle).
                services.AddMediatR(Sub(cfg)
                                        cfg.RegisterServicesFromAssembly(
                                            GetType(SmokeVatConfigHandler).Assembly)
                                    End Sub)

                services.AddScoped(Of IVatReportingService, VatReportingService)()
                services.AddScoped(Of FinancialOverviewService)()
                services.AddScoped(Of IKpiProvider, VatPayableKpiProvider)()
                services.AddScoped(Of IFinancialInsightProvider, VatPayableInsightProvider)()
                services.AddScoped(Of IWhatThisMeansService, WhatThisMeansService)()
                services.AddScoped(Of IFinancialOverviewService)(
                    Function(sp) New VatEnrichedFinancialOverviewService(
                        sp.GetRequiredService(Of FinancialOverviewService)(),
                        sp.GetServices(Of IKpiProvider)()))

                smokeProvider = services.BuildServiceProvider()

                ' ── Create scratch schema via raw SQL ─────────────────────────────────
                ' MigrateAsync() silently skips VB.NET migration classes in EF Core 10
                ' (see agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md).
                ' Raw SQL mirrors the exact DDL produced by migrations 1–3.
                SetupScratchSmokeSchema(scratchPath)

                ' ── Seed synthetic VAT ledger data ────────────────────────────────────
                ' VatableSales=₱100,000 | OutputVat=₱12,000 | InputVat=₱3,000 ⇒ payable=₱9,000
                Dim now = DateTime.UtcNow
                Dim today = now.Date

                Using scope = smokeProvider.CreateScope()
                    Dim db = scope.ServiceProvider.GetRequiredService(Of AccountingDbContext)()

                    db.RevenueRecords.Add(New RevenueRecord With {
                        .RecordDate = today,
                        .SourceTransactionId = 1,
                        .PaymentMethod = PaymentMethod.Cash,
                        .GrossAmount = 112000D,
                        .DiscountAmount = 0D,
                        .NetAmount = 112000D,
                        .VatAmount = 12000D,
                        .ProductId = 1,
                        .ProductName = "Smoke Test Product",
                        .QuantitySold = 100,
                        .COGS = 0D,
                        .GrossProfit = 112000D,
                        .VatableAmount = 100000D,
                        .VatExemptAmount = 0D,
                        .ZeroRatedAmount = 0D,
                        .OutputVat = 12000D,
                        .InputVat = 0D,
                        .VatTreatment = VatTreatment.Vatable,
                        .CreatedBy = "smoke",
                        .CreatedAt = now,
                        .ModifiedBy = "smoke",
                        .ModifiedAt = now
                    })

                    db.ExpenseRecords.Add(New ExpenseRecord With {
                        .RecordDate = today,
                        .Category = "COGS",
                        .Description = "Smoke test purchase",
                        .Amount = 28000D,
                        .SourceModule = "Purchasing",
                        .SourceReferenceId = Nothing,
                        .VatableAmount = 25000D,
                        .VatExemptAmount = 0D,
                        .ZeroRatedAmount = 0D,
                        .OutputVat = 0D,
                        .InputVat = 3000D,
                        .VatTreatment = VatTreatment.Vatable,
                        .CreatedBy = "smoke",
                        .CreatedAt = now,
                        .ModifiedBy = "smoke",
                        .ModifiedAt = now
                    })

                    Await db.SaveChangesAsync()
                End Using

                ' ── Resolve and call the decorated service ────────────────────────────
                Using scope = smokeProvider.CreateScope()
                    Dim overviewService = scope.ServiceProvider.GetRequiredService(Of IFinancialOverviewService)()
                    Dim dto = Await overviewService.GetOverviewAsync()

                    report.SeededVatableSales = 100000D
                    report.SeededOutputVat = 12000D
                    report.SeededInputVat = 3000D
                    report.ComputedVatPayable = dto.VatPayable
                    report.TileSeverity = dto.VatPayableSeverity
                    report.DaysUntilDeadline = If(dto.VatFilingDueDate.HasValue,
                        (dto.VatFilingDueDate.Value.Date - DateTime.UtcNow.Date).Days,
                        -1)
                End Using

            Catch ex As Exception
                runError = ex
            End Try

            ' ── Cleanup scratch DB (after all Awaits are done, per feedback_vbnet_await_catch.md) ──
            smokeProvider?.Dispose()
            If File.Exists(scratchPath) Then
                Try
                    File.Delete(scratchPath)
                Catch
                End Try
            End If

            If runError IsNot Nothing Then
                Throw New InvalidOperationException(
                    $"VatTileSmokeHarness failed: {runError.Message}", runError)
            End If

            ' ── Navigation route check (INT-13 wiring) ───────────────────────────────
            ' Reflection avoids a direct reference to MerchSys.App from MerchSys.Accounting.
            ' VB.NET root namespace (MerchSys.App) is part of the fully-qualified type name.
            ' DI registration of VatReturnView proves INT-13's wiring is consumable.
            Dim vatReturnViewType = Type.GetType("MerchSys.App.Views.Accounting.VatReturnView, MerchSys.App")
            Dim roleCheckNote As String
            If vatReturnViewType IsNot Nothing Then
                Dim resolvedView = host.Services.GetService(vatReturnViewType)
                report.NavigationRouteFound = resolvedView IsNot Nothing
                roleCheckNote = If(report.NavigationRouteFound,
                    "VatReturnView resolved from DI (VatReturnView registered as Transient — INT-13 confirmed).",
                    "VatReturnView NOT resolved from DI — registration missing.")
            Else
                report.NavigationRouteFound = False
                roleCheckNote = "VatReturnView type not found via reflection (MerchSys.App assembly not loaded?)."
            End If

            ' ── Write Markdown report ─────────────────────────────────────────────────
            Dim reportPath = Path.Combine(
                Path.GetTempPath(),
                $"vat-tile-smoke-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md")
            Await WriteReportAsync(report, roleCheckNote, reportPath)

            Return report
        End Function

        Private Shared Async Function WriteReportAsync(report As VatTileSmokeReport,
                                                        roleCheckNote As String,
                                                        reportPath As String) As Task
            Dim lines As New List(Of String) From {
                "# VAT Tile Smoke Report",
                "",
                $"**Run:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                "",
                "## Seeded Data",
                "| Metric | Value |",
                "|---|---|",
                $"| Vatable Sales | ₱{report.SeededVatableSales:N2} |",
                $"| Output VAT | ₱{report.SeededOutputVat:N2} |",
                $"| Input VAT | ₱{report.SeededInputVat:N2} |",
                $"| Expected Payable | ₱{report.SeededOutputVat - report.SeededInputVat:N2} |",
                "",
                "## Computed Results",
                "| Metric | Value |",
                "|---|---|",
                $"| ComputedVatPayable | ₱{report.ComputedVatPayable:N2} |",
                $"| TileSeverity | {report.TileSeverity} |",
                $"| DaysUntilDeadline | {report.DaysUntilDeadline} |",
                $"| NavigationRouteFound | {report.NavigationRouteFound} |",
                "",
                "## Assertions",
                "| Check | Result |",
                "|---|---|",
                $"| VatPayable = 9000 | {If(report.ComputedVatPayable = 9000D, "✅ PASS", "❌ FAIL")} |",
                $"| NavigationRouteFound | {If(report.NavigationRouteFound, "✅ PASS", "❌ FAIL")} |",
                "",
                "## Role Check",
                roleCheckNote
            }
            Await File.WriteAllLinesAsync(reportPath, lines)
        End Function

        ''' <summary>
        ''' Creates all Accounting module tables on a scratch SQLite database using raw SQL,
        ''' combining migrations 1 (InitialAccounting), 2 (AddVatLedgerColumns), and
        ''' 3 (FixVatReturnAmendedIndex).  Idempotent via IF NOT EXISTS guards.
        ''' Required because EF Core 10 cannot discover VB.NET migration classes via MigrateAsync().
        ''' </summary>
        Private Shared Sub SetupScratchSmokeSchema(scratchPath As String)
            Using conn As New SqliteConnection($"Data Source={scratchPath}")
                conn.Open()

                ' Migration 1 — base tables
                Exec(conn,
                    "CREATE TABLE IF NOT EXISTS ""Acc_FinancialPeriods"" (" &
                    """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_FinancialPeriods"" PRIMARY KEY AUTOINCREMENT, " &
                    """PeriodType"" TEXT NOT NULL, " &
                    """StartDate"" TEXT NOT NULL, " &
                    """EndDate"" TEXT NOT NULL, " &
                    """TotalRevenue"" TEXT NOT NULL, " &
                    """TotalCOGS"" TEXT NOT NULL, " &
                    """GrossProfit"" TEXT NOT NULL, " &
                    """GrossMarginPercent"" TEXT NOT NULL, " &
                    """TotalExpenses"" TEXT NOT NULL, " &
                    """NetIncome"" TEXT NOT NULL, " &
                    """IsClosed"" INTEGER NOT NULL, " &
                    """CreatedBy"" TEXT NULL, " &
                    """CreatedAt"" TEXT NOT NULL, " &
                    """ModifiedBy"" TEXT NULL, " &
                    """ModifiedAt"" TEXT NULL" &
                    ")")

                ' Acc_RevenueRecords — base columns + VAT columns from migration 2
                Exec(conn,
                    "CREATE TABLE IF NOT EXISTS ""Acc_RevenueRecords"" (" &
                    """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_RevenueRecords"" PRIMARY KEY AUTOINCREMENT, " &
                    """RecordDate"" TEXT NOT NULL, " &
                    """SourceTransactionId"" INTEGER NOT NULL, " &
                    """PaymentMethod"" INTEGER NOT NULL, " &
                    """GrossAmount"" TEXT NOT NULL, " &
                    """DiscountAmount"" TEXT NOT NULL, " &
                    """NetAmount"" TEXT NOT NULL, " &
                    """VatAmount"" TEXT NOT NULL, " &
                    """ProductId"" INTEGER NOT NULL, " &
                    """ProductName"" TEXT NOT NULL, " &
                    """QuantitySold"" INTEGER NOT NULL, " &
                    """COGS"" TEXT NOT NULL, " &
                    """GrossProfit"" TEXT NOT NULL, " &
                    """VatableAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """VatExemptAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """ZeroRatedAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """OutputVat"" TEXT NOT NULL DEFAULT '0', " &
                    """InputVat"" TEXT NOT NULL DEFAULT '0', " &
                    """VatTreatment"" INTEGER NOT NULL DEFAULT 0, " &
                    """CreatedBy"" TEXT NULL, " &
                    """CreatedAt"" TEXT NOT NULL, " &
                    """ModifiedBy"" TEXT NULL, " &
                    """ModifiedAt"" TEXT NULL" &
                    ")")

                ' Acc_ExpenseRecords — base columns + VAT columns from migration 2
                Exec(conn,
                    "CREATE TABLE IF NOT EXISTS ""Acc_ExpenseRecords"" (" &
                    """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_ExpenseRecords"" PRIMARY KEY AUTOINCREMENT, " &
                    """RecordDate"" TEXT NOT NULL, " &
                    """Category"" TEXT NOT NULL, " &
                    """Description"" TEXT NULL, " &
                    """Amount"" TEXT NOT NULL, " &
                    """SourceModule"" TEXT NOT NULL, " &
                    """SourceReferenceId"" INTEGER NULL, " &
                    """VatableAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """VatExemptAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """ZeroRatedAmount"" TEXT NOT NULL DEFAULT '0', " &
                    """OutputVat"" TEXT NOT NULL DEFAULT '0', " &
                    """InputVat"" TEXT NOT NULL DEFAULT '0', " &
                    """VatTreatment"" INTEGER NOT NULL DEFAULT 0, " &
                    """CreatedBy"" TEXT NULL, " &
                    """CreatedAt"" TEXT NOT NULL, " &
                    """ModifiedBy"" TEXT NULL, " &
                    """ModifiedAt"" TEXT NULL" &
                    ")")

                Exec(conn,
                    "CREATE TABLE IF NOT EXISTS ""Acc_FinancialSnapshots"" (" &
                    """Id"" INTEGER NOT NULL CONSTRAINT ""PK_Acc_FinancialSnapshots"" PRIMARY KEY AUTOINCREMENT, " &
                    """SnapshotDate"" TEXT NOT NULL, " &
                    """TotalAR"" TEXT NOT NULL, " &
                    """TotalAP"" TEXT NOT NULL, " &
                    """InventoryValue"" TEXT NOT NULL, " &
                    """TodayRevenue"" TEXT NOT NULL, " &
                    """MonthToDateRevenue"" TEXT NOT NULL, " &
                    """YearToDateRevenue"" TEXT NOT NULL, " &
                    """CreatedBy"" TEXT NULL, " &
                    """CreatedAt"" TEXT NOT NULL, " &
                    """ModifiedBy"" TEXT NULL, " &
                    """ModifiedAt"" TEXT NULL" &
                    ")")

                ' Migration 2 — VAT return tables
                Exec(conn,
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

                Exec(conn,
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

                ' Migration 3 — partial unique index (supersedes migration 2's full index)
                Exec(conn,
                    "CREATE UNIQUE INDEX IF NOT EXISTS " &
                    """IX_Acc_VatReturns_Year_Period_PeriodType_FormType_Active"" " &
                    "ON ""Acc_VatReturns"" (""Year"", ""Period"", ""PeriodType"", ""FormType"") " &
                    "WHERE ""FilingStatus"" != 3")

                Exec(conn,
                    "CREATE INDEX IF NOT EXISTS ""IX_Acc_RevenueRecords_RecordDate"" " &
                    "ON ""Acc_RevenueRecords"" (""RecordDate"")")
                Exec(conn,
                    "CREATE INDEX IF NOT EXISTS ""IX_Acc_RevenueRecords_ProductId"" " &
                    "ON ""Acc_RevenueRecords"" (""ProductId"")")
                Exec(conn,
                    "CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Acc_FinancialSnapshots_SnapshotDate"" " &
                    "ON ""Acc_FinancialSnapshots"" (""SnapshotDate"")")
                Exec(conn,
                    "CREATE INDEX IF NOT EXISTS ""IX_Acc_VatReturnLines_VatReturnId"" " &
                    "ON ""Acc_VatReturnLines"" (""VatReturnId"")")
            End Using
        End Sub

        Private Shared Sub Exec(conn As SqliteConnection, sql As String)
            Using cmd = conn.CreateCommand()
                cmd.CommandText = sql
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        ''' <summary>
        ''' Returns <c>IsVatRegistered = True, Tin = "999-999-999-000"</c> without hitting the
        ''' POS database.  Stands in for the production <c>GetVatConfigurationQueryHandler</c>
        ''' in <c>MerchSys.POS</c>.
        ''' </summary>
        Private Class SmokeVatConfigHandler
            Implements IRequestHandler(Of GetVatConfigurationQuery, GetVatConfigurationResult)

            Public Function Handle(request As GetVatConfigurationQuery,
                                   cancellationToken As CancellationToken) As Task(Of GetVatConfigurationResult) _
                Implements IRequestHandler(Of GetVatConfigurationQuery, GetVatConfigurationResult).Handle

                Return Task.FromResult(New GetVatConfigurationResult With {
                    .IsVatRegistered = True,
                    .VatRate = 0.12D,
                    .NonVatPercentageTaxRate = 0.03D
                })
            End Function

        End Class

        ''' <summary>
        ''' Minimal ISyncableRepository stub for the isolated harness DI container.
        ''' Delegates SaveChangesWithJournalAsync to the underlying DbContext so that
        ''' VatReportingService can persist generated VAT returns to the scratch database.
        ''' SyncJournalDbContext is not available in the isolated container and is not needed here.
        ''' </summary>
        Private Class SmokeSyncableRepository
            Implements ISyncableRepository(Of AccountingDbContext)

            Private ReadOnly _db As AccountingDbContext

            Public Sub New(db As AccountingDbContext)
                _db = db
            End Sub

            Public Function SaveChangesWithJournalAsync(cancellationToken As CancellationToken) As Task(Of Integer) _
                Implements ISyncableRepository(Of AccountingDbContext).SaveChangesWithJournalAsync
                Return _db.SaveChangesAsync(cancellationToken)
            End Function

            Public Function GetTrackedChangeDescriptors() As IReadOnlyList(Of SyncJournalDescriptor) _
                Implements ISyncableRepository(Of AccountingDbContext).GetTrackedChangeDescriptors
                Return New List(Of SyncJournalDescriptor)().AsReadOnly()
            End Function

        End Class

    End Class

    Public Class VatTileSmokeReport
        Public Property SeededVatableSales As Decimal
        Public Property SeededOutputVat As Decimal
        Public Property SeededInputVat As Decimal
        Public Property ComputedVatPayable As Decimal
        Public Property TileSeverity As KpiSeverity
        Public Property DaysUntilDeadline As Integer
        Public Property NavigationRouteFound As Boolean
    End Class

End Namespace

#End If
