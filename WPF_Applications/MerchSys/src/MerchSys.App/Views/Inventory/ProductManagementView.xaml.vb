Imports System.Windows.Controls
Imports MerchSys.Inventory.ViewModels

Namespace Views.Inventory

    ''' <summary>
    ''' Product Management screen — Manager-only CRUD for products and categories.
    ''' DataContext is set via constructor injection resolved from DI.
    ''' </summary>
    Partial Class ProductManagementView
        Inherits UserControl

        Public Sub New(viewModel As ProductManagementViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub SearchBox_KeyDown(sender As Object, e As System.Windows.Input.KeyEventArgs) _
            Handles SearchBox.KeyDown

            If e.Key = System.Windows.Input.Key.Escape Then
                Dim vm = TryCast(DataContext, ProductManagementViewModel)
                If vm IsNot Nothing Then vm.SearchText = String.Empty
            End If
        End Sub

    End Class

End Namespace
