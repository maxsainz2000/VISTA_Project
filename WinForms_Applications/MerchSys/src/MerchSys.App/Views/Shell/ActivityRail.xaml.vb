Imports MerchSys.App.ViewModels.Shell

Namespace Views.Shell

    ''' <summary>
    ''' 60px vertical Activity Rail showing module icon buttons.
    ''' DataContext is ActivityRailViewModel resolved via DI.
    ''' </summary>
    Partial Class ActivityRail
        Inherits System.Windows.Controls.UserControl

        Public Sub New(viewModel As ActivityRailViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

    End Class

End Namespace
