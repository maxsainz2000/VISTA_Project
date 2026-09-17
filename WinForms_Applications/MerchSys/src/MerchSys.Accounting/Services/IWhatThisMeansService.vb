Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Interface IWhatThisMeansService
        Function GenerateOverviewInterpretation(data As FinancialOverviewDto) As String
        Function GenerateIncomeStatementInterpretation(data As IncomeStatementDto, Optional previousMargin As Decimal = -1D) As String
        Function GenerateSalesSummaryInterpretation(data As AccountingSalesSummaryDto) As String
        Function GenerateMarginAlert(currentMargin As Decimal, previousMargin As Decimal) As String

        Function GetOverviewSeverity(data As FinancialOverviewDto) As InsightSeverity
        Function GetIncomeStatementSeverity(data As IncomeStatementDto, Optional previousMargin As Decimal = -1D) As InsightSeverity
        Function GetSalesSummarySeverity(data As AccountingSalesSummaryDto) As InsightSeverity
    End Interface

End Namespace
