---
type: pattern
module: MerchSys.POS
agent: antigravity
date: 2026-06-05
tags: [wpf, vb-net, mvvm, user-experience]
---

## Context

When implementing user-facing actions in MerchSys that modify or delete data, it is critical to prevent accidental execution of destructive actions. Some actions are reversible, while others are irreversible. To ensure a consistent and safe user experience, we categorize these actions and apply appropriate guardrails: Confirmation dialogs, Undo toasts, or both.

## The Pattern

We classify destructive actions into two types and apply matching guardrail levels:

1. **Reversible Destructive Actions (e.g., Soft-deletes, Toggling Active States)**
   - **Guardrail**: **Undo Notification Toast** (with or without Confirmation).
   - **Pattern**: Perform the action immediately in a concurrency-safe manner, then trigger a time-boxed success toast notification that includes an "Undo" action callback. If clicked within a ~8-second window, the callback restores the previous state.
   
2. **Irreversible or High-Materiality Actions (e.g., Recording inventory shrinkage, Voiding transactions)**
   - **Guardrail**: **Confirmation Gating** (optionally with Typed-Text Confirmation for high-materiality or BIR-sensitive actions).
   - **Pattern**: Before modifying any data, prompt the user with a modal confirmation dialog using `IConfirmationPresenter.PromptAsync`. For extremely critical operations (like voids or inventory adjustments), require the user to type a specific word (e.g. `"VOID"`, `"RECORD"`) to unlock the confirm button.

### Code Example: Undo on Reversible Action (VB.NET)
```vb
Private Async Function ToggleActiveAsync(row As ProductManagementRowItem) As Task
    If row Is Nothing Then Return

    Dim product = Await _db.Products.FindAsync(row.ProductId)
    If product Is Nothing Then Return

    Dim wasActive As Boolean = product.IsActive
    Dim productId = product.Id
    Dim productName = product.Name

    IsBusy = True
    Try
        Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
            Async Function()
                product.IsActive = Not product.IsActive
                Await _db.SaveChangesAsync()
            End Function,
            AddressOf LoadDataAsync,
            _conflictPresenter)

        If saved Then
            Await LoadDataAsync()

            If wasActive Then
                Dim hasUndone As Boolean = False
                Dim actionTime = DateTime.UtcNow
                Dim undoCallback = Async Sub()
                                       If hasUndone Then Return
                                       If (DateTime.UtcNow - actionTime).TotalSeconds > 8.0 Then
                                           _notifications.ShowWarning("Undo window has expired.")
                                           Return
                                       End If
                                       hasUndone = True

                                       Dim success = False
                                       Dim conflict = False
                                       Try
                                           Dim restoreSaved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                                               Async Function()
                                                   Dim p = Await _db.Products.FindAsync(productId)
                                                   If p IsNot Nothing Then
                                                       p.IsActive = True
                                                       Await _db.SaveChangesAsync()
                                                       success = True
                                                   End If
                                               End Function,
                                               AddressOf LoadDataAsync,
                                               _conflictPresenter)
                                       Catch dbEx As DbUpdateConcurrencyException
                                           conflict = True
                                       End Try

                                       If conflict Then
                                           _notifications.ShowError("Could not undo — data was changed elsewhere.")
                                       ElseIf success Then
                                           Await LoadDataAsync()
                                           _notifications.ShowSuccess($"Product '{productName}' reactivated.")
                                       Else
                                           _notifications.ShowError("Could not undo.")
                                       End If
                                   End Sub

                Dim undoAction = New NotificationAction("Undo", undoCallback)
                _notifications.ShowSuccess($"Product '{productName}' deactivated.", undoAction)
            End If
        End If
    Finally
        IsBusy = False
    End Try
End Function
```

### Code Example: Typed Confirmation on Irreversible Action (VB.NET)
```vb
Dim req As New ConfirmationRequest("Void Transaction", $"This will void transaction '{txNum}'. This action is irreversible.", "_Void", True, "VOID")
If Not Await _confirmationPresenter.PromptAsync(req) Then Return
```

## Why It Works

- **Low-Friction Safety**: Reversible actions do not interrupt the user's flow with annoying dialog boxes. Instead, the Undo toast follows the "user control and freedom" heuristic (NN/g), allowing errors to be corrected post-hoc.
- **High-Friction Gating**: For irreversible and audit-sensitive actions (like voiding a BIR transaction), typed confirmation forces the user to pause, read, and type the command deliberately, mitigating slip errors.
- **Time-Boxed & Idempotent**: To prevent race conditions or late actions, the undo callback is bounded to ~8 seconds and tracks a `hasUndone` flag to guarantee it only executes once.

## Rules

- **Never Neither**: Every destructive/data-altering action in WPF views MUST have either a Confirmation Dialog, an Undo Toast, or both.
- **Time-Boxed Window**: Always enforce the ~8-second expiration logic and idempotency checks in the Undo handler.
- **Role Verification**: Ensure write actions verify manager/developer permissions via `CanEdit` check before enabling actions.
- **Concurrency Integration**: Always wrap state-changing database operations in `ConcurrencyHelper.ExecuteWithConflictPromptAsync` to handle concurrency conflicts gracefully.

## Related

- `[[notification-actions-undo]]`
- `[[concurrency-conflict-consolidation]]`
- `[[confirmation-dialogs]]`
