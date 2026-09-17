Imports System.Windows
Imports MerchSys.Inventory.ViewModels

Namespace Views.Inventory

    Public Partial Class ProductPriceHistoryView
        Inherits Window

        Public Sub New(viewModel As ProductPriceHistoryViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
            Close()
        End Sub

    End Class

End Namespace
