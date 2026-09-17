Imports MerchSys.App.ViewModels.Shell

Namespace Views.Shell

    ''' <summary>
    ''' Connection status pill badge displayed in the shell sidebar.
    ''' DataContext is the ConnectionStatusViewModel resolved via DI.
    ''' </summary>
    Partial Class ConnectionStatusIndicator
        Inherits System.Windows.Controls.UserControl

        Public Sub New(viewModel As ConnectionStatusViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
