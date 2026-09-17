Imports System.Windows.Controls
Imports MerchSys.POS.ViewModels

Namespace Views.POS

    Partial Class VatSettingsView
        Inherits UserControl

        Private ReadOnly _viewModel As VatSettingsViewModel

        Public Sub New(viewModel As VatSettingsViewModel)
            InitializeComponent()
            _viewModel = viewModel
            DataContext = viewModel
        End Sub

        Private Async Sub UserControl_Loaded(sender As Object, e As System.Windows.RoutedEventArgs) Handles Me.Loaded
            Await _viewModel.ReloadAsync()
        End Sub

    End Class

End Namespace
