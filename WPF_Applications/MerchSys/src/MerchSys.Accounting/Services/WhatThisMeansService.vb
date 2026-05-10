Imports MerchSys.Accounting.Services.Insights

Namespace Services

    Public Class WhatThisMeansService
        Implements IWhatThisMeansService

        Private Const CreditWarningThreshold As Decimal = 30D
        Private Const MarginDropWarningThreshold As Decimal = 3D

        Private ReadOnly _insightProviders As IEnumerable(Of IFinancialInsightProvider)

        Public Sub New(insightProviders As IEnumerable(Of IFinancialInsightProvider))
            _insightProviders = insightProviders
        End Sub

        Public Function GenerateOverviewInterpretation(data As FinancialOverviewDto) As String _
            Implements IWhatThisMeansService.GenerateOverviewInterpretation

            Dim sb As New System.Text.StringBuilder()

            sb.AppendLine($"Your store earned {FormatPeso(data.TodayRevenue)} today from {data.TodayTransactions} transaction{Plural(data.TodayTransactions)}.")

            Dim previousMonthMtd = GetPreviousMonthMtdRevenue(data.MonthlyTrend)
            If previousMonthMtd > 0 AndAlso data.MonthToDateRevenue > 0 Then
                Dim changePercent = Math.Round(((data.MonthToDateRevenue - previousMonthMtd) / previousMonthMtd) * 100D, 1)
                Dim direction = If(changePercent >= 0, "higher", "lower")
                sb.AppendLine($"Month-to-date revenue is {FormatPeso(data.MonthToDateRevenue)}, which is {Math.Abs(changePercent)}% {direction} than last month at this point.")
            Else
                sb.AppendLine($"Month-to-date revenue is {FormatPeso(data.MonthToDateRevenue)}.")
            End If

            sb.AppendLine($"You have {FormatPeso(data.TotalAR)} in outstanding credit (utang) from customers and {FormatPeso(data.TotalAP)} in unpaid supplier bills.")

            If data.OverdueARCount > 0 Then
                sb.AppendLine($"Warning: {data.OverdueARCount} customer account{Plural(data.OverdueARCount)} {HaveHas(data.OverdueARCount)} overdue balances. Follow up to avoid bad debt.")
            End If

            If data.LowStockAlertCount > 0 Then
                sb.AppendLine($"You have {data.LowStockAlertCount} product{Plural(data.LowStockAlertCount)} running low on stock. Consider reordering soon.")
            End If

            For Each provider In _insightProviders
                Dim insight = provider.GenerateInsight(data)
                If Not String.IsNullOrEmpty(insight) Then
                    sb.AppendLine(insight)
                End If
            Next

            Return sb.ToString().Trim()
        End Function

        Public Function GenerateIncomeStatementInterpretation(data As IncomeStatementDto, Optional previousMargin As Decimal = -1D) As String _
            Implements IWhatThisMeansService.GenerateIncomeStatementInterpretation

            Dim sb As New System.Text.StringBuilder()

            Dim marginPesos = If(data.GrossMarginPercent > 0, Math.Round(data.GrossMarginPercent, 2), 0D)
            sb.AppendLine($"For {data.PeriodDescription}, your gross profit margin is {Math.Round(data.GrossMarginPercent, 1)}%.")
            sb.AppendLine($"For every ₱100 of sales, your business keeps ₱{Math.Round(marginPesos, 2)} after paying for the cost of goods.")

            If previousMargin >= 0 Then
                Dim diff = Math.Round(data.GrossMarginPercent - previousMargin, 1)
                If diff < 0 Then
                    sb.AppendLine($"This is lower than the previous period ({Math.Round(previousMargin, 1)}%), which may be caused by supplier price increases. Consider reviewing your retail prices.")
                ElseIf diff > 0 Then
                    sb.AppendLine($"This is an improvement from the previous period ({Math.Round(previousMargin, 1)}%). Your pricing or cost management is working well.")
                Else
                    sb.AppendLine($"Your margin is stable compared to the previous period ({Math.Round(previousMargin, 1)}%).")
                End If
            End If

            If data.NetIncome < 0 Then
                sb.AppendLine($"Your business recorded a net loss of {FormatPeso(Math.Abs(data.NetIncome))} this period. Review operating expenses and see if any costs can be reduced.")
            ElseIf data.NetIncome > 0 Then
                sb.AppendLine($"After operating expenses, your net income is {FormatPeso(data.NetIncome)}.")
            End If

            If data.ShrinkageLoss > 0 Then
                sb.AppendLine($"Shrinkage (spoilage or damage) cost your business {FormatPeso(data.ShrinkageLoss)} this period. Monitor inventory handling to reduce losses.")
            End If

            Return sb.ToString().Trim()
        End Function

        Public Function GenerateSalesSummaryInterpretation(data As AccountingSalesSummaryDto) As String _
            Implements IWhatThisMeansService.GenerateSalesSummaryInterpretation

            Dim sb As New System.Text.StringBuilder()

            sb.AppendLine($"Total sales for {data.PeriodDescription} were {FormatPeso(data.TotalNetSales)} from {data.TransactionCount} transaction{Plural(data.TransactionCount)}.")

            If data.TotalDiscounts > 0 Then
                sb.AppendLine($"Discounts totalled {FormatPeso(data.TotalDiscounts)}, and returns were {FormatPeso(data.TotalReturns)}.")
            End If

            Dim creditEntry = data.PaymentBreakdown?.Find(Function(p) p.PaymentMethod.Equals("Credit", StringComparison.OrdinalIgnoreCase))
            If creditEntry IsNot Nothing AndAlso creditEntry.Percentage >= CreditWarningThreshold Then
                sb.AppendLine($"{Math.Round(creditEntry.Percentage, 1)}% of sales were on credit (utang). This is higher than normal — monitor your accounts receivable closely.")
            End If

            Dim topMethod = GetTopPaymentMethod(data.PaymentBreakdown)
            If topMethod IsNot Nothing Then
                sb.AppendLine($"Your busiest payment method was {topMethod.PaymentMethod} with {topMethod.TransactionCount} transaction{Plural(topMethod.TransactionCount)}.")
            End If

            Return sb.ToString().Trim()
        End Function

        Public Function GenerateMarginAlert(currentMargin As Decimal, previousMargin As Decimal) As String _
            Implements IWhatThisMeansService.GenerateMarginAlert

            Dim diff = Math.Round(previousMargin - currentMargin, 1)
            Dim sb As New System.Text.StringBuilder()

            sb.AppendLine($"⚠ Your gross margin dropped from {Math.Round(previousMargin, 1)}% to {Math.Round(currentMargin, 1)}% — a {diff}% decline.")

            If diff >= MarginDropWarningThreshold Then
                sb.AppendLine("This is a significant drop. Consider checking recent supplier price changes and reviewing your retail pricing immediately.")
            Else
                sb.AppendLine("Consider checking recent supplier price changes to identify the cause.")
            End If

            Return sb.ToString().Trim()
        End Function

        Private Shared Function FormatPeso(amount As Decimal) As String
            Return $"₱{amount:N2}"
        End Function

        Private Shared Function Plural(count As Integer) As String
            Return If(count = 1, "", "s")
        End Function

        Private Shared Function HaveHas(count As Integer) As String
            Return If(count = 1, "has", "have")
        End Function

        Private Shared Function GetPreviousMonthMtdRevenue(trend As List(Of MonthlyTrendDto)) As Decimal
            If trend Is Nothing OrElse trend.Count < 2 Then Return 0D
            Return trend(trend.Count - 2).Revenue
        End Function

        Private Shared Function GetTopPaymentMethod(breakdown As List(Of PaymentBreakdownDto)) As PaymentBreakdownDto
            If breakdown Is Nothing OrElse breakdown.Count = 0 Then Return Nothing
            Dim top As PaymentBreakdownDto = Nothing
            For Each entry In breakdown
                If top Is Nothing OrElse entry.TransactionCount > top.TransactionCount Then
                    top = entry
                End If
            Next
            Return top
        End Function

    End Class

End Namespace
