Imports MerchSys.Accounting.Entities

Namespace Services

    ''' <summary>
    ''' Generates, files, and amends BIR VAT returns from the Accounting ledger.
    ''' Three form types are supported:
    ''' <list type="bullet">
    '''   <item>Form 2550M — monthly VAT return (VAT-registered businesses only)</item>
    '''   <item>Form 2550Q — quarterly VAT return (VAT-registered businesses only)</item>
    '''   <item>Form 2551Q — quarterly 3% percentage tax (non-VAT businesses only)</item>
    ''' </list>
    ''' Filing deadlines (from <c>concepts/bir-compliance.md</c>):
    ''' <list type="bullet">
    '''   <item>Form 2550M: 20th of the following month (manual), 25th (eFPS)</item>
    '''   <item>Form 2550Q: 25th of the month after the quarter</item>
    '''   <item>Form 2551Q: 25th of the month after the quarter</item>
    ''' </list>
    ''' See also: ACC-10 (schema), POS-14 (event publisher), INFRA-07 (event contracts).
    ''' </summary>
    Public Interface IVatReportingService

        ''' <summary>
        ''' Generates Form 2550M for the given calendar year and month (1–12).
        ''' Throws <see cref="InvalidOperationException"/> if <c>IsVatRegistered = False</c>.
        ''' Throws <see cref="Exceptions.VatReturnLockedException"/> if the period is already filed.
        ''' </summary>
        Function GenerateMonthlyVatReturnAsync(year As Integer, month As Integer) As Task(Of VatReturn)

        ''' <summary>
        ''' Generates Form 2550Q for the given year and BIR fiscal quarter (1–4).
        ''' Quarter windows: Q1=Jan–Mar, Q2=Apr–Jun, Q3=Jul–Sep, Q4=Oct–Dec.
        ''' Throws <see cref="InvalidOperationException"/> if <c>IsVatRegistered = False</c>.
        ''' </summary>
        Function GenerateQuarterlyVatReturnAsync(year As Integer, quarter As Integer) As Task(Of VatReturn)

        ''' <summary>
        ''' Generates Form 2551Q for the given year and BIR fiscal quarter (1–4).
        ''' Tax = TotalGrossReceipts × <c>NonVatPercentageTaxRate</c> (default 3%).
        ''' Throws <see cref="InvalidOperationException"/> if <c>IsVatRegistered = True</c>.
        ''' </summary>
        Function GenerateNonVatPercentageTaxAsync(year As Integer, quarter As Integer) As Task(Of VatReturn)

        ''' <summary>Returns a single <see cref="VatReturn"/> by primary key, with its <c>Lines</c> collection.</summary>
        Function GetReturnAsync(returnId As Integer) As Task(Of VatReturn)

        ''' <summary>Lists all <see cref="VatReturn"/> headers for the given year, or all years if <paramref name="year"/> is Nothing.</summary>
        Function ListReturnsAsync(year As Integer?) As Task(Of IReadOnlyList(Of VatReturn))

        ''' <summary>
        ''' Transitions <c>FilingStatus</c> from <c>Generated</c> to <c>Filed</c>.
        ''' Sets <c>FiledAt = UtcNow</c> and <c>FiledBy</c> to the supplied user identifier.
        ''' Throws if the return is not in <c>Generated</c> status.
        ''' </summary>
        Function FileReturnAsync(returnId As Integer, filedBy As String) As Task

        ''' <summary>
        ''' Creates a new <see cref="VatReturn"/> row with <c>FilingStatus = Amended</c>
        ''' recomputed from current ledger state.  The original <c>Filed</c> row is left
        ''' untouched as the BIR audit trail.  Only <c>Filed</c> returns may be amended.
        ''' </summary>
        Function AmendReturnAsync(returnId As Integer) As Task(Of VatReturn)

    End Interface

End Namespace
