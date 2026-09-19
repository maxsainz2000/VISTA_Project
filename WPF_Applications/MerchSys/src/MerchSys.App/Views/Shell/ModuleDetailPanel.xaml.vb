Imports MerchSys.App.ViewModels

Namespace Views.Shell

    ''' <summary>
    ''' 220px detail panel showing session info, active module name, sub-view nav items,
    ''' connection status badge, and Log Out button.
    ''' DataContext is MainWindowViewModel (bound to SelectModuleCommand, NavigateCommand, etc.).
    ''' </summary>
    Partial Class ModuleDetailPanel
        Inherits System.Windows.Controls.UserControl

        Public Sub New(mainViewModel As MainWindowViewModel,
                       connectionIndicator As ConnectionStatusIndicator)
            InitializeComponent()
            DataContext = mainViewModel
            ConnectionStatusSlot.Content = connectionIndicator
        End Sub

    End Class

End Namespace
