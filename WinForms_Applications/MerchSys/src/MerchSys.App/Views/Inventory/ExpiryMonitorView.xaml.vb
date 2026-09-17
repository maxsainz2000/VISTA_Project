Imports System.Windows
Imports System.Text.RegularExpressions
Imports System.Windows.Controls
Imports System.Windows.Input
Imports MerchSys.Inventory.ViewModels

Namespace Views.Inventory

    ''' <summary>
    ''' Expiry Monitor screen — near-expiry alerts, expired batch list, and write-off action.
    ''' DataContext is set via constructor injection resolved from DI.
    ''' Write-off confirmation is handled here to keep the ViewModel free of UI dependencies.
    ''' </summary>
    Partial Class ExpiryMonitorView
        Inherits UserControl

        Public Sub New(viewModel As ExpiryMonitorViewModel)
            InitializeComponent()
            DataContext = viewModel
        End Sub

        Private Sub WriteOffButton_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            If btn Is Nothing Then Return

            Dim row = TryCast(btn.Tag, ExpiryRowItem)
            If row Is Nothing Then Return

            Dim result = MessageBox.Show(
                $"Write off {row.QtyRemaining} unit(s) of ""{row.ProductName}"" (Batch #{row.BatchId})?" &
                Environment.NewLine & Environment.NewLine &
                $"Total value: ₱{row.Value:N2} will be recorded as shrinkage." &
                Environment.NewLine & "This action cannot be undone.",
                "Confirm Write-Off",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning)

            If result <> MessageBoxResult.Yes Then Return

            Dim vm = TryCast(DataContext, ExpiryMonitorViewModel)
            If vm IsNot Nothing Then
                Dispatcher.InvokeAsync(Async Function()
                                           Await vm.WriteOffCommand.ExecuteAsync(row)
                                       End Function)
            End If
        End Sub

        Private Sub ThresholdBox_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
            e.Handled = Not Regex.IsMatch(e.Text, "^\d$")
        End Sub

        Private Sub ThresholdUp_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = TryCast(DataContext, ExpiryMonitorViewModel)
            If vm IsNot Nothing Then vm.DaysThreshold += 1
        End Sub

        Private Sub ThresholdDown_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = TryCast(DataContext, ExpiryMonitorViewModel)
            If vm IsNot Nothing Then vm.DaysThreshold -= 1
        End Sub

        Private Sub OnUnloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            Dim vm = TryCast(DataContext, IDisposable)
            If vm IsNot Nothing Then vm.Dispose()
        End Sub

    End Class

End Namespace
