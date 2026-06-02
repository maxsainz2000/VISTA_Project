Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.App.Services.Theming

Namespace Views.Shell.Modules
    Partial Class DeveloperToolsPanel
        Inherits System.Windows.Controls.UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub ToggleTheme_Click(sender As Object, e As System.Windows.RoutedEventArgs)
            Try
                Dim themeService = DebugHostHolder.CurrentHost.Services.GetRequiredService(Of IThemeService)()
                themeService.Toggle()
            Catch ex As System.Exception
                System.Windows.MessageBox.Show($"Failed to toggle theme: {ex.Message}", "Theming Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
            End Try
        End Sub

    End Class
End Namespace
