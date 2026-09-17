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
            AddHandler Me.TileBorder.KeyDown, AddressOf OnTileKeyDown
        End Sub

        Private Sub OnTileKeyDown(sender As Object, e As Input.KeyEventArgs)
            If e.Key = Input.Key.Enter OrElse e.Key = Input.Key.Space Then
                Dim dc = Me.DataContext
                If dc IsNot Nothing Then
                    Dim cmdProp = dc.GetType().GetProperty("NavigateToVatReturnCommand")
                    If cmdProp IsNot Nothing Then
                        Dim cmd = TryCast(cmdProp.GetValue(dc), System.Windows.Input.ICommand)
                        If cmd IsNot Nothing AndAlso cmd.CanExecute(Nothing) Then
                            cmd.Execute(Nothing)
                            e.Handled = True
                        End If
                    End If
                End If
            End If
        End Sub

    End Class

End Namespace
