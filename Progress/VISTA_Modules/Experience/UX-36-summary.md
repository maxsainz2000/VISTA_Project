---
module: MerchSys.App
agent: antigravity
date: 2026-06-06
plan-ref: Plans/VISTA_Modules/Experience/36-full-accessibility-wcag.md
status: completed
---

## Task Summary

Implemented WCAG 2.2 AA accessibility requirements for `MerchSys.App`. Swept interactive controls, icon-only buttons, grids, and KPI tiles with descriptive screen-reader names (`AutomationProperties.Name` and `AutomationProperties.HelpText`). Implemented modal focus-trapping behavior on the three application overlays: `CommandPalette`, `ConcurrencyConflictPrompt`, and `ConfirmationDialog` using a new attached dependency property behavior `AccessibilityHelper.IsFocusTrap`.

> **Scope note (2026-06-06, claude-code):** UX-36 originally also added a `HighContrast` theme (third palette + 3-way theme selector). That part was **reverted at user request** — it was judged not beneficial and added UI noise. Light/Dark remain the only themes. The screen-reader naming and modal focus-trap work documented below was kept.

**Plan:** `[[36-full-accessibility-wcag]]`

## What Was Done

- Created `Helpers/AccessibilityHelper.vb` with the `IsFocusTrap` attached dependency property to cycle tab navigation within a modal container.
- Applied focus traps to:
  - `Views/Shell/ConfirmationDialog.xaml` (on the root Window)
  - `Views/Shell/ConcurrencyConflictPrompt.xaml` (on the root Window)
  - `Views/Shell/CommandPalette.xaml` (on the centered search panel border)
- Named the primary button in `Views/Shell/ConcurrencyConflictPrompt.xaml` as `RefreshButton` and focused it on the window `Loaded` event in `ConcurrencyConflictPrompt.xaml.vb`.
- Completed an accessibility sweep by adding `AutomationProperties.Name` and `AutomationProperties.HelpText` to:
  - Navigation buttons in `Views/Shell/ActivityRail.xaml`
  - Password reveal buttons in `Views/LoginView.xaml`
  - Refresh button in `Views/Shell/FreshnessChip.xaml`
  - Reload POs button in `Views/Purchasing/GoodsReceivingView.xaml`
  - `Views/Accounting/Components/VatPayableTile.xaml` (making the border focusable and adding a KeyDown handler to execute the navigation command in `VatPayableTile.xaml.vb`)
  - Cart row styles, payment select buttons, and item removal buttons in `Views/POS/SalesCartView.xaml`
  - Ledger row styles in `Views/Purchasing/APLedgerView.xaml`
  - Suggestion and config row styles in `Views/Purchasing/ReorderSuggestionsView.xaml`

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 errors, 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified Light/Dark themes swap live and persist; modal focus traps correctly cycle) |

## Implementation Details

### 1. Focus Trap Mechanism
Introduced `AccessibilityHelper.IsFocusTrap` attached property. Setting this property to `True` performs the following actions:
- Sets `KeyboardNavigation.TabNavigation="Cycle"` and `ControlTabNavigation="Cycle"` to force Tab/Shift+Tab (and Ctrl+Tab) to cycle focus within the container, preventing keyboard focus from escaping behind modal overlays.
- Note: `FocusManager.IsFocusScope` is deliberately **not** set — a focus scope is meant for menus/toolbars and diverges logical from keyboard focus, which can interfere with `IsDefault`/RoutedCommand resolution and focus restoration on an overlay. `Cycle` alone is the trap.

### 2. Focus Restoration
- `CommandPalette` handles saving the active focus in `_previouslyFocusedElement` during the `IsVisibleChanged` event when shown, and restores it asynchronously on hide.
- Dialog windows like `ConfirmationDialog` and `ConcurrencyConflictPrompt` are shown modally via `ShowDialog()`. WPF naturally returns focus to the invoking window/element once the modal window closes.

## Codebase Wiki Discrepancies

- New helper `Helpers/AccessibilityHelper.vb` (attached property `IsFocusTrap`).
- Focus-trapping properties and screen-reader naming tags have been added to the XAML elements of several views.
- (The HighContrast theme / `AppTheme.HighContrast` / `Themes/HighContrast.xaml` / 3-way theme selector were reverted — no longer present.)

## Cross-References

- Plans: `[[36-full-accessibility-wcag.md]]`
