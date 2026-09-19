Imports System.Windows
Imports System.Windows.Media
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Presentation

Namespace Views.Shell

    Public Partial Class ConfirmationDialog
        Inherits Window

        Private ReadOnly _requiredToken As String

        Public Sub New(request As ConfirmationRequest)
            InitializeComponent()

            Me.Title = request.Title
            Me.TitleTextBlock.Text = request.Title
            Me.MessageTextBlock.Text = request.Message
            Me.ConfirmButton.Content = request.ConfirmButtonText

            If request.IsDestructive Then
                Me.ConfirmButton.Style = DirectCast(FindResource("DangerButtonStyle"), Style)
                Me.DialogIcon.Fill = DirectCast(FindResource("DangerBrush"), Brush)
            Else
                Me.ConfirmButton.Style = DirectCast(FindResource("PrimaryButtonStyle"), Style)
                Me.DialogIcon.Fill = DirectCast(FindResource("WarningBrush"), Brush)
            End If

            If Not String.IsNullOrEmpty(request.RequireTypedConfirmation) Then
                _requiredToken = request.RequireTypedConfirmation
                Me.InstructionTextBlock.Text = $"Type ""{_requiredToken}"" to confirm:"
                Me.TypedConfirmationArea.Visibility = Visibility.Visible
                Me.ConfirmButton.IsEnabled = False
                
                AddHandler Me.ConfirmationTextBox.TextChanged, AddressOf ConfirmationTextBox_TextChanged
                AddHandler Me.Loaded, Sub() Me.ConfirmationTextBox.Focus()
            Else
                Me.TypedConfirmationArea.Visibility = Visibility.Collapsed
                Me.ConfirmButton.IsEnabled = True
                AddHandler Me.Loaded, Sub() Me.ConfirmButton.Focus()
            End If
        End Sub

        Private Sub ConfirmationTextBox_TextChanged(sender As Object, e As Controls.TextChangedEventArgs)
            Me.ConfirmButton.IsEnabled = String.Equals(Me.ConfirmationTextBox.Text.Trim(), _requiredToken, StringComparison.OrdinalIgnoreCase)
        End Sub

        Private Sub ConfirmButton_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = True
            Close()
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = False
            Close()
        End Sub

    End Class

End Namespace
