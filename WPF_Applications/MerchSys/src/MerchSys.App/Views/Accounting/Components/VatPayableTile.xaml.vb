Namespace Views.Accounting.Components

    ''' <summary>
    ''' Standalone KPI tile for VAT Payable / Percentage Tax.
    ''' Inherits its DataContext from the parent <c>FinancialOverviewView</c>, which is bound to
    ''' <c>FinancialOverviewViewModel</c>.  The VAT-specific observable properties
    ''' (<c>VatPayable</c>, <c>VatPayableLabel</c>, <c>VatFilingDueDate</c>, <c>VatPayableSeverity</c>)
    ''' are added to that ViewModel via a partial-class extension in ACC-12.
    ''' Click navigation to <c>VatReturnView</c> is handled by <c>NavigateToVatReturnCommand</c>
    ''' on the ViewModel extension; the view itself is role-gated in ACC-11 (Manager-only).
    ''' </summary>
    Partial Public Class VatPayableTile

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
