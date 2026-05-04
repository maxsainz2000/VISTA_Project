Imports System.Text.RegularExpressions
Imports System.Windows.Controls
Imports System.Windows.Input
Imports MerchSys.Inventory.ViewModels

Namespace Views.Inventory

    ''' <summary>
    ''' Shrinkage screen — history grid with date/product/reason filters, record dialog overlay.
    ''' DataContext is set via constructor injection resolved from DI.
    ''' Confirmation dialog and DatePicker sync are handled here to keep the ViewModel UI-free.
    ''' </summary>
    Partial Class ShrinkageView
        Inherits UserControl

        Public Sub New(viewModel As ShrinkageViewModel)
            InitializeComponent()
            DataContext = viewModel
            StartDatePicker.SelectedDate = viewModel.FilterStartDate
            EndDatePicker.SelectedDate = viewModel.FilterEndDate
        End Sub

        ' ─── Filter event handlers ────────────────────────────────────────────────

        Private Sub StartDatePicker_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim vm = TryCast(DataContext, ShrinkageViewModel)
            If vm Is Nothing Then Return
            Dim dp = TryCast(sender, DatePicker)
            If dp IsNot Nothing AndAlso dp.SelectedDate.HasValue Then
                vm.FilterStartDate = dp.SelectedDate.Value
            End If
        End Sub

        Private Sub EndDatePicker_SelectedDateChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim vm = TryCast(DataContext, ShrinkageViewModel)
            If vm Is Nothing Then Return
            Dim dp = TryCast(sender, DatePicker)
            If dp IsNot Nothing AndAlso dp.SelectedDate.HasValue Then
                vm.FilterEndDate = dp.SelectedDate.Value
            End If
        End Sub

        Private Sub ReasonFilterCombo_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim vm = TryCast(DataContext, ShrinkageViewModel)
            If vm Is Nothing Then Return
            Dim cb = TryCast(sender, ComboBox)
            If cb?.SelectedItem IsNot Nothing Then
                Dim item = TryCast(cb.SelectedItem, ComboBoxItem)
                vm.FilterReason = If(item?.Tag?.ToString(), String.Empty)
            End If
        End Sub

        ' ─── Dialog event handlers ────────────────────────────────────────────────

        Private Sub CancelDialogButton_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = TryCast(DataContext, ShrinkageViewModel)
            vm?.CancelDialogCommand.Execute(Nothing)
        End Sub

        Private Sub ConfirmRecordButton_Click(sender As Object, e As RoutedEventArgs)
            Dim vm = TryCast(DataContext, ShrinkageViewModel)
            If vm Is Nothing Then Return

            Dim validMsg = vm.ValidateDialog()
            If Not String.IsNullOrEmpty(validMsg) Then
                MessageBox.Show(validMsg, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim productName = vm.DialogSelectedProduct.ProductName
            Dim qty = vm.DialogQuantity
            Dim reason = vm.DialogReason

            Dim result = MessageBox.Show(
                $"Record {qty} unit(s) of ""{productName}"" as shrinkage?" &
                Environment.NewLine & Environment.NewLine &
                $"Reason: {reason}" & Environment.NewLine &
                "This action cannot be undone.",
                "Confirm Shrinkage",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning)

            If result <> MessageBoxResult.Yes Then Return

            Dispatcher.InvokeAsync(Async Function()
                                       Await vm.ExecuteRecordCommand.ExecuteAsync(Nothing)
                                   End Function)
        End Sub

        Private Sub QuantityBox_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
            e.Handled = Not Regex.IsMatch(e.Text, "^\d$")
        End Sub

    End Class

End Namespace
