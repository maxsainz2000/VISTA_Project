# Implementation Progress Report - UX-17

---
module: MerchSys.App
agent: antigravity
date: 2026-06-04
plan-ref: Plans/VISTA_Modules/Experience/17-keyboard-focus-foundation.md
status: completed
---

## Task Summary

Implemented the Keyboard & Focus Foundation (`UX-17`) across `MerchSys.App`. This establishes an application-wide correct visible-focus indicator, configures sequential keyboard-tab navigation on all core views and dialogs, introduces Alt access-key mnemonics on primary button actions, and routes Enter-to-confirm and Escape-to-cancel hotkeys locally within custom in-view modal overlays.

**Plan:** `[[17-keyboard-focus-foundation.md]]`

## What Was Done

- **Theme-Aware Focus Ring**: Created `AppFocusVisual` in `Themes/Controls.xaml` dynamically drawing a `1.5px` border of `AccentBrush` inside elements (`Margin="1"`), honoring Light and Dark themes. Set it as the implicit `FocusVisualStyle` on all common controls (`Button`, `TextBox`, `PasswordBox`, `CheckBox`, `RadioButton`, `ListBoxItem`, `ComboBox`, `TabItem`).
- **Core Windows Tab Navigation**:
  - `ConcurrencyConflictPrompt.xaml`: Set `TabIndex` sequence and Alt-mnemonics (`_Cancel`, `_Refresh`).
  - `SessionTimeoutWarningView.xaml`: Linked native `IsDefault`/`IsCancel` buttons and set `TabIndex`/mnemonics (`_Stay signed in`, `_Sign out now`).
  - `LoginView.xaml`: Configured sequential `TabIndex` mapping across login and password-reset fields. Set `IsDefault="True"` on buttons, focused the `UsernameTextBox` on load, and configured mnemonics (`_LOG IN`, `_SET PASSWORD AND CONTINUE`).
- **Purchasing View Navigation & Overlays**:
  - `APLedgerView.xaml` & `.xaml.vb`: Set sequential `TabIndex` on all inputs, named payment overlay, registered `IsVisibleChanged` handler to focus the payment textbox on toggle, and routed `PreviewKeyDown` locally (Esc -> Cancel, Enter -> Confirm). Added mnemonics to main/dialog buttons.
  - `PurchaseOrderListView.xaml` & `.xaml.vb`: Added sequential tab sequence, named order editor overlay, added `IsVisibleChanged` focus target to the vendor combo box, and routed `PreviewKeyDown` (Esc -> Cancel, Enter -> Save Draft). Added mnemonics.
  - `VendorDirectoryView.xaml` & `.xaml.vb`: Added sequential `TabIndex`, named vendor editor, mapped `IsVisibleChanged` focus dispatcher to name textbox, and routed `PreviewKeyDown` keys. Mapped mnemonics.
  - `ReorderSuggestionsView.xaml` & `.xaml.vb`: Added `TabIndex` mapping, named suggestion editor, mapped focus dispatcher to min threshold, and routed `PreviewKeyDown` hotkeys. Mapped mnemonics (including `AccessText` for icon buttons).
  - `GoodsReceivingView.xaml`: Configured layout tab order and added access key mnemonic (`_Confirm Receipt`).
- **POS View Navigation & Overlays**:
  - `CreditManagementView.xaml` & `.xaml.vb`: Wired sequential `TabIndex` sequence, named add account and payment overlays, registered `IsVisibleChanged` focus targeting on textboxes, and routed `PreviewKeyDown` (Enter -> Confirm, Esc -> Cancel). Mapped mnemonics (`_Confirm`, `Ca_ncel`, `_Pay`, `_Confirm Payment`, `_Cancel`).
  - `TransactionHistoryView.xaml` & `.xaml.vb`: Set up sequential `TabIndex` for filters and action buttons, named return overlay, registered `IsVisibleChanged` focus handler, and routed `PreviewKeyDown` (Enter -> Confirm Return, Esc -> Cancel). Mapped mnemonics (`_Search`, `_Clear Filters`, `_View Receipt`, `_Process Return`, `_Cancel`, `_Confirm Return`).
  - `SalesCartView.xaml`: Set up sequential tab indices, added `<KeyBinding Key="Return" Command="{Binding SearchProductCommand}"/>` inside `ProductSearchBox.InputBindings` to trigger search on Enter, and assigned sequential `TabIndex` values.
- **Inventory View Navigation & Overlays**:
  - `ProductManagementView.xaml` & `.xaml.vb`: Set up sequential tab navigation, named product and category editor overlays, mapped `IsVisibleChanged` focus dispatchers to inputs, and routed `PreviewKeyDown` keys (Enter -> Save, Esc -> Cancel) for both panels. Mapped mnemonics (`_Add Product`, `_Edit`, `_Price History`, `Re_fresh`, `Add _Category`, `Ca_ncel`, `_Save Product`, `_Save Category`).
  - `ShrinkageView.xaml` & `.xaml.vb`: Set sequential `TabIndex` sequence, named record shrinkage overlay, mapped `IsVisibleChanged` focus handler, and routed `PreviewKeyDown` hotkeys (Enter -> Confirm, Esc -> Cancel) directly triggering validation and commands in the code-behind. Mapped mnemonics (`_Record Shrinkage`, `Re_fresh`, `_Cancel`, `_Confirm`).

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Completed successfully with 0 errors and 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified implicit focus rings, tab navigation order, overlay focus dispatchers, and Enter/Esc dialog mapping) |

## Issues Encountered

- **Issue:** WPF Grid Column/Border replacement mismatch
  - **Resolution:** Replaced content precisely by targeting parent elements or utilizing dedicated script-based normalization to prevent matching outer layout grids.
- **Issue:** Access keys on buttons with complex text/icon markup
  - **Resolution:** Access keys placed on standard string attributes fail if the button template is custom. Swapped out standard text blocks for `AccessText` with mnemonics (`Re_fresh`, `_Generate Suggestions`) so the Alt-mnemonic resolves correctly.
- **Issue:** Windows double newline expansion on file write
  - **Resolution:** Python's standard `open()` in text-write mode on Windows automatically translates `\n` to `\r\n`. Writing a string containing pre-existing `\r\n` characters results in corrupted `\r\r\n` sequences. Solved by specifying `newline="\r\n"` explicitly on write or operating on clean LF strings.
- **Issue:** Duplicate WPF attribute compile error in `ShrinkageView.xaml`
  - **Resolution:** Cleared double `PreviewKeyDown` root attribute that was generated due to multi-pass script execution, resolving the build failure.

## What's Next

- [x] Complete verification check
- [x] Document the pattern in the agent wiki

## Cross-References

- Domain Wiki pages consulted: `[[wpf-vista-theming-conventions]]`, `[[wpf-vista-state-feedback]]`
- Agent Wiki entries consulted: `[[wiki-workflow-agent-wiki]]`
