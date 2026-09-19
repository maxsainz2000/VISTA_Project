---
module: MerchSys.App
agent: antigravity
date: 2026-06-05
plan-ref: Plans/VISTA_Modules/Experience/30-pos-keyboard-fast-path.md
status: completed
---

## Task Summary

Implemented the POS Keyboard-First Fast Path (`UX-30`) in the `SalesCartView` and `SalesCartViewModel`. This provides a fully keyboard-driveable cart experience: scan-hot focus discipline (keeping the SKU/scan field focused), cart-scoped hotkeys for quantity and discount adjustment, a local hold/recall cart mechanism, and Enter-to-commit on tender.

**Plan:** `[[30-pos-keyboard-fast-path]]`

## What Was Done

- **Focus Discipline**:
  - Implemented automatic refocusing of `ProductSearchBox` on view load, after any addition/removal/clear of items (via `CartLines.CollectionChanged`), after inline edits commit or cancel (via `CartDataGrid.CellEditEnding`), and after successful sale completion (via `TransactionCompleted` event).
  - Used `Dispatcher.BeginInvoke` with `DispatcherPriority.Input` priority to ensure focus calls execute reliably after the WPF layout cycles complete.
- **Cart-Scoped Hotkeys**:
  - Wired `PreviewKeyDown` on `SalesCartView` to capture key shortcuts locally:
    - `Ctrl+Q`: Focus the Quantity column in the selected grid row and begin editing.
    - `Ctrl+D`: Focus the Discount column in the selected grid row and begin editing.
    - `Ctrl+T`: Focus the `AmountTenderedTextBox` and select its text.
  - Added native mnemonics using underscores:
    - `Set _Quantity (Ctrl+Q)` button
    - `Apply _Discount (Ctrl+D)` button
    - `_Hold` button (Alt+H)
    - `_Recall` button (Alt+R)
    - `Amount _Tendered (₱)` label pointing to `AmountTenderedTextBox` (Alt+T focuses the text box)
  - Bound `Enter` key on `ProductList` to trigger `AddToCartCommand`.
  - Bound `Enter` key on `CreditCustomerSearch` to trigger `SearchCreditCustomerCommand`.
- **Enter-to-Commit on Tender**:
  - Set `IsDefault="True"` on the `PAY` button.
  - Intercepted `Enter` on `ProductSearchBox` to trigger `SearchAndAddProductCommand` and marked it handled to prevent it from triggering the default `PAY` button.
- **In-Memory Hold/Recall**:
  - Added `GetCartAsync` to `ICartService`/`CartService` to retrieve cached carts by Guid.
  - Added `HeldCartId` and `HasHeldCart` to `SalesCartViewModel`.
  - Added `HoldCartCommand` and `RecallCartCommand` to `SalesCartViewModel` supporting local swap of active cart and held cart GUIDs without dropping cart state from memory.
- **Realization & Concurrency Guarding**:
  - Checked `IsBusy` inside `CanPay` and set it synchronously in `ProcessPaymentAsync` before any asynchronous yields to completely block double-firing on key repeat.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Build succeeded with 0 errors and 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified scan-hot refocus, Ctrl/Alt hotkeys, hold/recall swap, Enter-add vs Enter-commit segregation) |

## Keypad Fast Path Flow (Canonical Sequence)

1. **Scan SKU**: Type/scan SKU in the search box, hit `Enter`. The item is added to the cart, the scan field clears, and focus remains in the scan field.
2. **Edit Qty**: Select row in grid, press `Ctrl+Q` or `Alt+Q`. Type the new quantity, hit `Enter`. The quantity updates, and focus returns to the scan field.
3. **Apply Discount**: Select row in grid, press `Ctrl+D` or `Alt+D`. Type the discount amount, hit `Enter`. The discount updates, and focus returns to the scan field.
4. **Hold/Recall**: Press `Alt+H` to hold the cart. Scan items for a different customer. Press `Alt+R` to recall the held cart.
5. **Go To Tender**: Press `Ctrl+T` or `Alt+T` to move focus to the payment field.
6. **Commit Sale**: Type the cash amount, hit `Enter` to commit the sale and print the receipt. Focus returns to the scan field for the next customer.

## Codebase Wiki Discrepancies

- `SalesCartView`: Added bottom action toolbar with `SetQty` (Ctrl+Q), `ApplyDiscount` (Ctrl+D), `Hold` (Alt+H), and `Recall` (Alt+R) buttons. Added `x:Name="AmountTenderedTextBox"` and `x:Name="CreditCustomerSearch"`.
- `SalesCartViewModel`: Added properties `HeldCartId` and `HasHeldCart`, event `TransactionCompleted`, and commands `SearchAndAddProductCommand`, `HoldCartCommand`, and `RecallCartCommand`.
- `ICartService`: Added `GetCartAsync`.

## Cross-References

- Domain Wiki pages consulted: `[[module-pos]]`
- Agent Wiki entries consulted: `[[wpf-vista-keyboard-focus]]`
