# Pattern: WPF VISTA Notification Actions & Undo

Conventions and design patterns for adding optional action buttons (such as "Undo" or "View") to toast notifications in WPF, specifically for reversible soft-deletes and voids.

## Core Rules

1. **Backward-Compatible Service Contract**: Extend `INotificationService` methods using an `Optional action As NotificationAction = Nothing` parameter so that all existing informational call sites continue compiling and behaving unchanged.
2. **Reversals Route Through Module Services**: Soft-delete undo writes must route through the owning module's command/service/repository path (respecting optimistic-concurrency tokens, updating ModifiedBy/ModifiedAt audit columns, and raising MediatR integration events). Never write directly to the DB from the App layer.
3. **Time-Boxed closures**: The undo callback is time-limited to matching toast lifetimes (typically 5–8 seconds). Verify expiration using a timestamp closure.
4. **Idempotent Lambdas**: Prevent double execution (and double-restoration) by checking a local `hasUndone` flag inside the callback closure.
5. **No `Await` in `Catch`/`Finally` (BC36943)**: VB.NET does not support `Await` statements inside `Catch` or `Finally` blocks. Capture error states in the `Catch` block, then await any notification or feedback calls *after* the `Try-Catch` block.
6. **Role Guarding**: Action buttons must only appear on notifications for users authorized to perform that action (e.g., Manager/Developer). Toasts remain informational for read-only roles (e.g., Owner).

## Implementation Pattern

### 1. Contract Definition (SharedKernel)

```vb
Namespace Interfaces

    Public Class NotificationAction
        Public Property Label As String
        Public Property Callback As Action

        Public Sub New(label As String, callback As Action)
            Me.Label = label
            Me.Callback = callback
        End Sub
    End Class

    Public Interface INotificationService
        Sub ShowSuccess(message As String, Optional action As NotificationAction = Nothing)
        Sub ShowError(message As String, Optional action As NotificationAction = Nothing)
        Sub ShowInfo(message As String, Optional action As NotificationAction = Nothing)
        Sub ShowWarning(message As String, Optional action As NotificationAction = Nothing)
    End Interface

End Namespace
```

### 2. Service Implementation (App)

```vb
Public Class DefaultNotificationService
    Implements INotificationService

    Public Sub ShowSuccess(message As String, Optional action As NotificationAction = Nothing) Implements INotificationService.ShowSuccess
        Dim content = New NotificationContent() With {
            .Title = "Success",
            .Message = message,
            .Type = NotificationType.Success
        }
        If action IsNot Nothing Then
            content.LeftButtonContent = action.Label
            content.LeftButtonAction = action.Callback
        End If
        _toastManager.Show(content)
    End Sub
    ' ... similar for ShowError, ShowInfo, ShowWarning ...
End Class
```

### 3. ViewModel Usage (Idempotent, Time-Boxed Closure)

```vb
Private Async Function DeleteSelectedAsync() As Task
    If SelectedItem Is Nothing Then Return
    Dim itemId = SelectedItem.Id
    Dim itemName = SelectedItem.Name

    Dim req As New ConfirmationRequest("Delete", $"Confirm delete '{itemName}'?", "_Delete", True)
    If Not Await _confirmationPresenter.PromptAsync(req) Then Return

    IsBusy = True
    Try
        Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
            Async Function() Await _service.DeleteAsync(itemId),
            AddressOf LoadDataAsync,
            _conflictPresenter)

        If saved Then
            Await LoadDataAsync()

            ' Closure variables for time-boxing and idempotency
            Dim hasUndone As Boolean = False
            Dim deleteTime = DateTime.UtcNow
            Dim undoCallback = Async Sub()
                                   If hasUndone Then Return
                                   If (DateTime.UtcNow - deleteTime).TotalSeconds > 8.0 Then
                                       _notifications.ShowWarning("Undo window has expired.")
                                       Return
                                   End If
                                   hasUndone = True

                                   Dim success = False
                                   Dim conflict = False
                                   Dim errMsg = String.Empty
                                   Try
                                       success = Await _service.RestoreAsync(itemId)
                                   Catch dbEx As Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
                                       conflict = True
                                   Catch ex As Exception
                                       errMsg = ex.Message
                                   End Try

                                   If conflict Then
                                       _notifications.ShowError("Could not undo — data was changed elsewhere.")
                                   ElseIf Not String.IsNullOrEmpty(errMsg) Then
                                       _notifications.ShowError($"Restore failed: {errMsg}")
                                   ElseIf success Then
                                       Await LoadDataAsync()
                                       _notifications.ShowSuccess($"'{itemName}' restored.")
                                   Else
                                       _notifications.ShowError("Could not undo.")
                                   End If
                               End Sub

            Dim undoAction = New NotificationAction("Undo", undoCallback)
            _notifications.ShowSuccess($"'{itemName}' deleted.", undoAction)
        End If
    Catch ex As Exception
        StatusMessage = $"Delete failed: {ex.Message}"
    Finally
        IsBusy = False
    End Try
End Function
```

## Detector Contract

```regex
(?s)New\s+NotificationAction\(.*?,.*?Async\s+Sub\(\).*?DateTime\.UtcNow.*?TotalSeconds\s*>\s*8\.0.*?Catch\s+dbEx\s+As\s+Microsoft\.EntityFrameworkCore\.DbUpdateConcurrencyException)
```
