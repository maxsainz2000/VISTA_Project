#If DEBUG Then

Imports System.IO
Imports System.Threading
Imports MediatR
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

        Public Async Function RunAsync(host As IHost) As Task(Of VatTileSmokeReport)
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

                ' ── Migrate scratch schema ────────────────────────────────────────────
                Using scope = smokeProvider.CreateScope()
                    Dim db = scope.ServiceProvider.GetRequiredService(Of AccountingDbContext)()
                    Await db.Database.MigrateAsync()
                End Using

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
            ' DI registration of VatReturnView proves INT-13's wiring is consumable.
            Dim vatReturnViewType = Type.GetType("Views.Accounting.VatReturnView, MerchSys.App")
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
