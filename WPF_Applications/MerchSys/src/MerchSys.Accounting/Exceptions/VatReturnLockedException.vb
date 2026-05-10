Imports MerchSys.Accounting.Enums

Namespace Exceptions

    ''' <summary>
    ''' Thrown by <c>VatReportingService</c> when an attempt is made to regenerate a
    ''' VAT return that has already been filed with BIR.  Filed returns are immutable
    ''' at the service layer; use <c>AmendReturnAsync</c> instead.
    ''' See: <c>concepts/bir-compliance.md</c> — BIR amendment workflow.
    ''' </summary>
    Public Class VatReturnLockedException
        Inherits InvalidOperationException

        ''' <summary>PK of the locked <c>VatReturn</c> row.</summary>
        Public ReadOnly Property ReturnId As Integer

        ''' <summary>Calendar year of the locked return.</summary>
        Public ReadOnly Property Year As Integer

        ''' <summary>Period number (1–12 for monthly, 1–4 for quarterly).</summary>
        Public ReadOnly Property Period As Integer

        ''' <summary>BIR form type of the locked return.</summary>
        Public ReadOnly Property FormType As VatReturnFormType

        Public Sub New(returnId As Integer, year As Integer, period As Integer, formType As VatReturnFormType)
            MyBase.New($"VAT return #{returnId} for {formType} {year}/P{period} has already been filed with BIR and cannot be regenerated. Use AmendReturnAsync to correct a filed return.")
            Me.ReturnId = returnId
            Me.Year = year
            Me.Period = period
            Me.FormType = formType
        End Sub

    End Class

End Namespace
