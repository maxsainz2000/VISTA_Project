Namespace Services

    Public Interface IWhatThisMeansService
        Function GenerateOverviewInterpretation(data As FinancialOverviewDto) As String
        Function GenerateIncomeStatementInterpretation(data As IncomeStatementDto, Optional previousMargin As Decimal = -1D) As String
        Function GenerateSalesSummaryInterpretation(data As AccountingSalesSummaryDto) As String
        Function GenerateMarginAlert(currentMargin As Decimal, previousMargin As Decimal) As String
    End Interface

End Namespace
