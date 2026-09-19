---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-04
tags: [wpf, xaml, mvvm, confirmation, dialogs, danger-styling]
---

# Pattern: Shared Confirmation & Destructive-Action Dialogs (WPF/VB.NET)

This pattern documents the design, routing, additive confirm-gating, danger styling, and optional typed-token matching for the application-wide Confirmation Dialog.

## Context

Irreversible, destructive, or financial actions (deletes, submissions, returns) must be guarded with a confirmation prompt to prevent accidental data loss or irreversible state transitions.
1. **Mirror Concurrency Prompt**: Follow the proven presenter pattern used for optimistic concurrency conflicts (`IConflictPresenter`), using dispatcher marshaling to cleanly display a modal `Window` and return a boolean task result.
2. **Keyboard Navigation Support**: Confirm dialogs must support standard accessibility (ESC closes/cancels, Enter confirms via `IsCancel` and `IsDefault` properties).
3. **Danger vs Accent Brushes**: Highlight destructive actions using `DangerBrush` (red) on the confirm button and warning icon, and routine irreversible actions (like submissions) using `PrimaryButtonStyle` (blue/accent) and `WarningBrush` (orange).
4. **Typed Token Confirmation**: Major destructive actions (e.g. deleting a vendor with database references) must utilize a "type to confirm" text box, enabling the confirm button only when the user types the exact token (case-insensitively).
5. **Additive Gating**: The confirm gate is placed at the top of the command handler method, keeping existing `CanExecute` validation and optimistic concurrency helpers completely untouched.

## The Pattern

### 1. Presenter Interface and Request DTO
Define a clean, WPF-free interface in `MerchSys.SharedKernel.Interfaces`:

```vb
Public Class ConfirmationRequest
    Public Property Title As String
    Public Property Message As String
    Public Property ConfirmButtonText As String
    Public Property IsDestructive As Boolean
    Public Property RequireTypedConfirmation As String

    Public Sub New(title As String, message As String, confirmButtonText As String, isDestructive As Boolean, Optional requireTypedConfirmation As String = Nothing)
        Me.Title = title
        Me.Message = message
        Me.ConfirmButtonText = confirmButtonText
        Me.IsDestructive = isDestructive
        Me.RequireTypedConfirmation = requireTypedConfirmation
    End Sub
End Class

Public Interface IConfirmationPresenter
    Function PromptAsync(request As ConfirmationRequest) As Task(Of Boolean)
End Interface
```

### 2. Concrete Presenter and Dialog Dispatching
Implement the presenter in `MerchSys.App.Services` to marshal the UI thread cleanly:

```vb
Public Class DefaultConfirmationPresenter
    Implements IConfirmationPresenter

    Public Function PromptAsync(request As ConfirmationRequest) As Task(Of Boolean) Implements IConfirmationPresenter.PromptAsync
        Dim tcs As New TaskCompletionSource(Of Boolean)()

        Application.Current.Dispatcher.Invoke(Sub()
            Try
                Dim dialog As New Views.Shell.ConfirmationDialog(request)
                dialog.Owner = Application.Current.MainWindow
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner
                Dim result = dialog.ShowDialog()
                tcs.SetResult(result.HasValue AndAlso result.Value)
            Catch ex As Exception
                tcs.SetException(ex)
            End Try
        End Sub)

        Return tcs.Task
    End Function
End Class
```

### 3. Dialog UI & Code-Behind
In the dialog XAML, wire `IsCancel="True"` (Cancel) and `IsDefault="True"` (Confirm). In code-behind, load properties dynamically and monitor text-matching changes:

```vb
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
```

### 4. Additive Gating in Command Handlers
Always inject `IConfirmationPresenter` and place the gate at the very top of the command handler:

```vb
Private Async Function DeleteSelectedAsync() As Task
    If SelectedVendor Is Nothing Then Return
    Dim vendorName = SelectedVendor.Name

    ' Additive confirm gate placed before setting IsBusy or initiating database transactions
    Dim req As New ConfirmationRequest("Delete Vendor", $"This will permanently delete the vendor '{vendorName}'.", "_Delete", True, vendorName)
    If Not Await _confirmationPresenter.PromptAsync(req) Then Return

    IsBusy = True
    Try
        ' Existing mutation logic goes here...
```

## Why It Works

- **Consistent Layout**: Placing Cancel on the left and Confirm on the right avoids visual confusion.
- **WPF DialogResult**: Setting `DialogResult = True` on click cleanly closes `ShowDialog()` and returns control to the awaiting presenter.
- **Dispatcher Safety**: Marshaling calls through `Dispatcher.Invoke` prevents exceptions when viewmodels trigger prompts from background threads.
- **Keyboard Floor Integrity**: Using `IsCancel` and `IsDefault` ensures that ESC and Enter bubble up to the dialog window actions naturally.

## Related

- Domain Wiki: `[[wpf-vista-state-feedback]]`, `[[wpf-vista-keyboard-focus]]`
- Progress Summary: `[[UX-20-summary]]`
