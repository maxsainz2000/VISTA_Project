---
type: pattern
module: POS
agent: antigravity
date: 2026-06-05
tags: [wpf, xaml, keyboard-focus, fast-path, focus-discipline, mvvm]
---

# WPF POS Keyboard-First Fast Path Design Pattern

This pattern establishes a robust design structure for building keyboard-first fast paths in high-frequency WPF forms (such as sales carts) which require mouse-free scanning, editing, tendering, and local transaction caching.

## Context

High-speed cashiers require completely mouse-free interaction to optimize transaction throughput:
1. **Focus Persistence**: Focus must remain locked to the primary SKU/barcode scan field during the entire flow, refocussing after every add-item, edit, removal, clear, or completion.
2. **Inline Grid Editing**: Operators must be able to focus and edit line details (like quantity or discount) using hotkeys, then commit changes and immediately return to scanning.
3. **Hotkeys Scoping**: Hotkeys must be scoped to the active view to avoid colliding with shell-wide global shortcuts.
4. **Enter-key Disambiguation**: The `Enter` key must be mapped contextually (e.g., searching/adding items when typing a barcode vs. committing the transaction when tendering).
5. **Double-Commit Guarding**: The checkout action must be guarded against rapid key repeat or double clicks.
6. **Local Cache / Hold-Recall**: Cashiers must be able to put a customer's cart on hold to serve someone else and then recall it later, entirely via keyboard actions.

## The Pattern

### 1. Scan-Hot Focus Discipline

Maintain a "hot" input field by listening to collection change events on the bindable list (add/remove/clear) and cell-editing commits/cancellations. Dispatch focus asynchronously via the WPF thread queue at `Input` priority to ensure focus is not stolen by grid selection shifts, toast popups, or layout changes.

```vb
Private Sub CartLines_CollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
    Dispatcher.BeginInvoke(Sub()
                               ProductSearchBox.Focus()
                           End Sub, System.Windows.Threading.DispatcherPriority.Input)
End Sub

Private Sub CartDataGrid_CellEditEnding(sender As Object, e As DataGridCellEditEndingEventArgs)
    If e.EditAction = DataGridEditAction.Commit Then
        Dispatcher.InvokeAsync(Async Function()
                                   Await vm.UpdateQuantityCommand.ExecuteAsync(line)
                                   ProductSearchBox.Focus()
                               End Function)
    Else
        Dispatcher.InvokeAsync(Sub()
                                   ProductSearchBox.Focus()
                               End Sub)
    End If
End Sub
```

### 2. Contextual Enter-Key Routing (Disambiguation)

- Set `IsDefault="True"` on the primary checkout (`PAY`) button.
- Intercept the `Enter` key on the scan/SKU input box by hooking its local `KeyDown` event, executing the add-to-cart command, and marking `e.Handled = True`. This blocks the event from bubbling up to trigger the default `IsDefault` checkout button.

```xml
<TextBox x:Name="ProductSearchBox" KeyDown="ProductSearchBox_KeyDown" TabIndex="1"/>
<Button x:Name="PayButton" Content="PAY" Command="{Binding PayCommand}" IsDefault="True" TabIndex="14"/>
```

```vb
Private Sub ProductSearchBox_KeyDown(sender As Object, e As KeyEventArgs)
    If e.Key = Key.Enter Then
        Dim vm = TryCast(DataContext, SalesCartViewModel)
        If vm IsNot Nothing Then
            Dispatcher.InvokeAsync(Async Function()
                                       Await vm.SearchAndAddProductCommand.ExecuteAsync(Nothing)
                                       ProductSearchBox.Focus()
                                   End Function)
            e.Handled = True
        End If
    End If
End Sub
```

### 3. Cart-Scoped PreviewKeyDown Hotkeys

To edit lines and navigate the cart cleanly from the keyboard without global shortcut collisions:
- Hook the `PreviewKeyDown` event at the root `UserControl` level.
- Trigger programmatic grid cell editing or input focus, and mark `e.Handled = True`.

```vb
Private Sub SalesCartView_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
    If (Keyboard.Modifiers And ModifierKeys.Control) = ModifierKeys.Control Then
        If e.Key = Key.Q Then
            FocusCartGridCell("Qty")
            e.Handled = True
        ElseIf e.Key = Key.D Then
            FocusCartGridCell("Discount")
            e.Handled = True
        ElseIf e.Key = Key.T Then
            AmountTenderedTextBox.Focus()
            AmountTenderedTextBox.SelectAll()
            e.Handled = True
        End If
    End If
End Sub

Private Sub FocusCartGridCell(columnName As String)
    If CartDataGrid.SelectedItem Is Nothing AndAlso CartDataGrid.Items.Count > 0 Then
        CartDataGrid.SelectedIndex = 0
    End If
    If CartDataGrid.SelectedItem IsNot Nothing Then
        Dim selectedItem = CartDataGrid.SelectedItem
        CartDataGrid.Focus()
        Dim column = CartDataGrid.Columns.FirstOrDefault(Function(c) c.Header.ToString() = columnName)
        If column IsNot Nothing Then
            CartDataGrid.CurrentCell = New DataGridCellInfo(selectedItem, column)
            CartDataGrid.BeginEdit()
        End If
    End If
End Sub
```

### 4. Double-Commit & Concurrency Guarding

Check a synchronous `IsBusy` flag inside the VM's checkout method command guard (`CanPay`) before any asynchronous yields (the first `Await` statement). This stops the command from executing multiple times on subsequent Enter keys while the database write is in progress.

```vb
Public ReadOnly Property CanPay As Boolean
    Get
        If IsBusy Then Return False
        If CartLines.Count = 0 Then Return False
        If SelectedPaymentMethod = PaymentMethod.Cash AndAlso AmountTendered < GrandTotal Then Return False
        Return True
    End Get
End Property

Private Async Function ProcessPaymentAsync() As Task
    If Not CanPay Then Return
    IsBusy = True ' Set synchronously before any Await
    Try
        ' finalization database logic...
    Finally
        IsBusy = False
    End Try
End Function
```

### 5. Local Hold/Recall Cache Swap

Support temporary transaction storage by keeping held carts in-memory in `CartService` under their Guid and swapping the `_currentCartId` locally on the VM:

```vb
Private Async Function HoldCartAsync() As Task
    If CartLines.Count = 0 Then Return
    HeldCartId = _currentCartId
    Await StartNewTransactionAsync()
End Function

Private Async Function RecallCartAsync() As Task
    If Not HasHeldCart Then Return
    Dim tempId = _currentCartId
    _currentCartId = HeldCartId
    HeldCartId = tempId
    Dim cart = Await _cartService.GetCartAsync(_currentCartId)
    SyncCartLines(cart)
End Function
```

## Canonical Keypad Flow

1. **Scan SKU**: Type/scan SKU in the scan field, hit `Enter` (Adds item to cart, clears field, focuses field).
2. **Adjust Qty**: Select line, press `Ctrl+Q` or `Alt+Q` (Focuses Qty column, opens editor). Type quantity, press `Enter` (Commits quantity, focuses field).
3. **Add Discount**: Select line, press `Ctrl+D` or `Alt+D` (Focuses Discount column, opens editor). Type amount, press `Enter` (Commits discount, focuses field).
4. **Go to Tender**: Press `Ctrl+T` or `Alt+T` (Focuses Amount Tendered, selects all text).
5. **Commit Sale**: Type the cash amount, press `Enter` (Commits payment, prints receipt, starts a new cart, focuses scan field).
