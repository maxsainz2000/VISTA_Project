Namespace Services.Insights

    ''' <summary>
    ''' Generates a plain-language VAT / Percentage Tax filing reminder for the "What This Means"
    ''' engine.  Reads the VAT extension fields on <see cref="FinancialOverviewDto"/> populated by
    ''' <see cref="VatEnrichedFinancialOverviewService"/> and emits one of three sentences:
    ''' <list type="bullet">
    '''   <item><b>Info</b> — "You owe ₱X in VAT for [period]. File by [date]."</item>
    '''   <item><b>Warning</b> — "VAT filing for [period] is due in N days — ₱X to remit."</item>
    '''   <item><b>Critical</b> — "VAT for [period] is overdue. ₱X should have been filed by [date]."</item>
    ''' </list>
    ''' Returns <c>Nothing</c> when the VAT amount is zero or the due date is not set.
    ''' </summary>
    Public Class VatPayableInsightProvider
        Implements IFinancialInsightProvider

        Public Function GenerateInsight(dto As FinancialOverviewDto) As String _
            Implements IFinancialInsightProvider.GenerateInsight

            If dto.VatPayable = 0D OrElse Not dto.VatFilingDueDate.HasValue Then Return Nothing
            If String.IsNullOrEmpty(dto.VatPeriodDescription) Then Return Nothing

            Dim taxLabel = If(dto.IsVatRegistered, "VAT", dto.VatPayableLabel)
            Dim amountStr = $"₱{dto.VatPayable:N2}"
            Dim period = dto.VatPeriodDescription
            Dim dueDateStr = dto.VatFilingDueDate.Value.ToString("MMMM d")
            Dim daysUntil = (dto.VatFilingDueDate.Value.Date - DateTime.UtcNow.Date).Days

            Select Case dto.VatPayableSeverity
                Case KpiSeverity.Critical
                    If daysUntil < 0 Then
                        Return $"{taxLabel} for {period} is overdue. {amountStr} should have been filed by {dueDateStr}."
                    Else
                        Return $"{taxLabel} filing for {period} is due in {daysUntil} day{If(daysUntil = 1, "", "s")} — {amountStr} to remit."
                    End If

                Case KpiSeverity.Warning
                    Return $"{taxLabel} filing for {period} is due in {daysUntil} day{If(daysUntil = 1, "", "s")} — {amountStr} to remit."

                Case Else
                    Return $"You owe {amountStr} in {taxLabel} for {period}. File by {dueDateStr}."
            End Select
        End Function

    End Class

End Namespace
