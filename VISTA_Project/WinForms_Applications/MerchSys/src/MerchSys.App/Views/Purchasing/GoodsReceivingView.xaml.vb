Imports System.Windows.Controls
Imports MerchSys.Purchasing.ViewModels

Namespace Views.Purchasing

    ''' <summary>
    ''' Goods Receiving screen — manager selects a Submitted PO, edits received
    ''' quantities and expiry dates, notes discrepancies, and confirms receipt.
    ''' DataContext is set via constructor injection resolved from DI.
    ''' </summary>
    Partial Class GoodsReceivingView
        Inherits UserControl

        Public Sub New(viewModel As GoodsReceivingViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
