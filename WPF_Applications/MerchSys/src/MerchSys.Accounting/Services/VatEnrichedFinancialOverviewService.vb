Imports MerchSys.Accounting.Entities

Namespace Services

    ''' <summary>
    ''' Decorator over <see cref="FinancialOverviewService"/> (ACC-03) that enriches the returned
    ''' <see cref="FinancialOverviewDto"/> with KPI data supplied by registered <see cref="IKpiProvider"/>
    ''' implementations, without modifying the ACC-03 source file.
    ''' The decorator is registered in the composition root after the concrete service so that DI
    ''' resolves the inner service directly and wraps it here.
    ''' </summary>
    Public Class VatEnrichedFinancialOverviewService
        Implements IFinancialOverviewService

        Private ReadOnly _inner As FinancialOverviewService
        Private ReadOnly _kpiProviders As IEnumerable(Of IKpiProvider)

        Public Sub New(inner As FinancialOverviewService,
                       kpiProviders As IEnumerable(Of IKpiProvider))
            _inner = inner
            _kpiProviders = kpiProviders
        End Sub

        ''' <summary>
        ''' Calls the inner service, then invokes each <see cref="IKpiProvider"/> and maps its result
        ''' onto the extension properties of <see cref="FinancialOverviewDto"/> (declared in
        ''' <c>FinancialOverviewVatExtension.vb</c>).
        ''' </summary>
        Public Async Function GetOverviewAsync() As Task(Of FinancialOverviewDto) Implements IFinancialOverviewService.GetOverviewAsync
            Dim dto = Await _inner.GetOverviewAsync()
            Dim asOfDate = DateTime.UtcNow

            For Each provider In _kpiProviders
                Dim kpi = Await provider.ProvideAsync(asOfDate)
                If kpi Is Nothing Then Continue For

                Select Case kpi.Key
                    Case "VatPayable"
                        dto.VatPayable = kpi.Amount
                        dto.VatPayableLabel = kpi.DisplayLabel
                        dto.VatPayableSeverity = kpi.Severity
                        dto.VatFilingDueDate = kpi.DueDate
                        dto.IsVatRegistered = (kpi.DisplayLabel = "VAT Payable")
                        If kpi.DueDate.HasValue Then
                            Dim filingMonth = If(dto.IsVatRegistered,
                                New DateTime(asOfDate.Year, asOfDate.Month, 1),
                                Nothing)
                            dto.VatPeriodDescription = If(dto.IsVatRegistered,
                                New DateTime(asOfDate.Year, asOfDate.Month, 1).ToString("MMMM yyyy"),
                                $"Q{(asOfDate.Month - 1) \ 3 + 1} {asOfDate.Year}")
                        End If
                End Select
            Next

            Return dto
        End Function

        Public Function RefreshSnapshotAsync() As Task(Of FinancialSnapshot) Implements IFinancialOverviewService.RefreshSnapshotAsync
            Return _inner.RefreshSnapshotAsync()
        End Function

    End Class

End Namespace
