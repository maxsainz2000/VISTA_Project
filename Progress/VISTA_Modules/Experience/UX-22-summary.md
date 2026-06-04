---
module: MerchSys.App
agent: claude-code
date: 2026-06-05
plan-ref: Plans/VISTA_Modules/Experience/22-tooltips-affordance.md
status: completed
---

## Task Summary

Implemented UX-22 — tooltips and affordance for every icon-only control across VISTA. Added a shared implicit `ToolTip` style to `Components.xaml` and attached tooltips to the three remaining icon-only controls: the password-visibility eye buttons in `LoginView`, the remove-line X button in `SalesCartView`, and the dark-mode toggle switch in `ModuleDetailPanel`.

**Plan:** `[[22-tooltips-affordance]]`

## What Was Done

- Modified `src/MerchSys.App/Themes/Components.xaml` — added implicit `ToolTip` style (tokenized background, foreground, border, radius, Inter font) with `ControlTemplate` override for `RadiusSmall` corners
- Modified `src/MerchSys.App/Views/LoginView.xaml` — added `ToolTip="Show / hide password"` to the current-password eye button; `ToolTip="Show / hide new password"` to the new-password eye button
- Modified `src/MerchSys.App/Views/POS/SalesCartView.xaml` — added `ToolTip="Remove from cart"` to the X-mark remove-line button inside the cart DataGrid template
- Modified `src/MerchSys.App/Views/Shell/ModuleDetailPanel.xaml` — added `ToolTip="Toggle dark mode"` to the `ThemeToggle` ToggleButton

## Coverage Table

| Surface | Control | Tooltip text | Text source |
|---|---|---|---|
| Activity Rail | Module buttons (PUR / INV / POS / ACC / DEV) | "Purchasing  (Ctrl+1)" etc. | `RailItem.ToolTipText` in `ActivityRailViewModel.BuildRailItems` — pre-existing ✅ |
| Activity Rail | "V" identity mark | "VISTA — Villon Integrated Supply and Trade Application" | Hardcoded in ActivityRail.xaml — pre-existing ✅ |
| Login | Password field eye toggle | "Show / hide password" | Wording matches the action |
| Login | New-password field eye toggle | "Show / hide new password" | Wording matches the action |
| POS Sales Cart | Remove-line button (X icon, DataGrid) | "Remove from cart" | Wording matches the command mnemonic |
| Shell sidebar | Dark mode toggle switch | "Toggle dark mode" | Wording matches the adjacent "Dark Mode" label |
| Sparkline bars | Individual bars (Rectangle) | Bound value (currency) | `{Binding Value, StringFormat=...}` — pre-existing ✅ |
| Stock Dashboard | Retail Price / Avg Cost / FIFO Cost column headers | Explanatory descriptions | Tooltips on column labels — pre-existing ✅ |
| Goods Receiving | Reload-POs refresh button | "Reload submitted POs" | Inline in GoodsReceivingView.xaml — pre-existing ✅ |
| Connection status | Pill badge | "Online" / "Reconnecting" / "Offline" text visible | Text label + color = no tooltip needed |
| Delta indicator | Arrow + percentage | Percentage text visible alongside arrow | Text label = no tooltip needed |
| All toolbar buttons | Text-labeled buttons (Refresh, Add, Edit, etc.) | N/A | Text labels = no redundant tooltip |

## Controls confirmed NOT icon-only (no tooltip added)

- All action buttons in PurchaseOrderListView, VendorDirectoryView, APLedgerView, ExpiryMonitorView, ShrinkageView, CreditManagementView, TransactionHistoryView — all carry `Content` text labels
- OwnerDashboard Refresh button — carries "Refresh" text label + icon
- ReorderSuggestionsView Generate / Refresh buttons — carry `AccessText` labels
- ErrorStatePanel "Retry" button — text label
- ModuleDetailPanel "Log Out" button — text label
- ConnectionStatusIndicator "Retry" button — text label

## Shared Tooltip Style

Added an implicit `TargetType="{x:Type ToolTip}"` style in `Components.xaml`:
- `Background = SurfaceBrush` (theme-reactive)
- `Foreground = TextPrimaryBrush` (theme-reactive)
- `BorderBrush = SeparatorBrush` (theme-reactive)
- `BorderThickness = 1`
- `Padding = 8,5`
- `FontFamily = AppFontFamily` (Inter)
- `FontSize = FontSizeCaption`
- `ControlTemplate` override for `CornerRadius = RadiusSmall` rounded corners

No inline hex, all `DynamicResource`. Style is merged at `[4]` in `Application.xaml` after the theme palette, so tokens resolve at runtime.

## Bound-Tooltip-in-Template Handling

The Activity Rail `ToolTip="{Binding ToolTipText}"` binds inside a `DataTemplate` on an `ItemsControl`. This works correctly because the binding source is the `RailItem` DataContext, which IS accessible to the `Button` element inside the template. No `PlacementTarget` / `RelativeSource` workaround was needed here because the `ToolTip` attribute is set directly on the `Button` (not inside a `ToolTip` element's own content). The binding was already in place and verified to show the correct screen name.

## P1 Automation-Name Candidates

The following tooltip strings are candidates for reuse as `AutomationProperties.Name` in the future Pro P1 accessibility pass:
- Rail buttons: `ToolTipText` values ("Purchasing (Ctrl+1)", etc.)
- Eye buttons: "Show / hide password", "Show / hide new password"
- Remove button: "Remove from cart"
- Theme toggle: "Toggle dark mode"

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | Pending — hover all icon-only controls in both themes to confirm tooltip chrome and correct text |

## Issues Encountered

None. The tooltip style and per-control attributes compiled cleanly.

## What's Next

- [ ] Manual hover test in both Light and Dark themes: verify every icon-only control (rail buttons, eye buttons, cart remove, theme toggle) shows a correctly styled, readable tooltip
- [ ] Verify the shared tooltip chrome recolors on theme toggle (SurfaceBrush switches from white to dark surface)

## Cross-References

- Domain Wiki pages consulted: none
- Agent Wiki entries consulted: `[[wpf-vista-iconography]]`, `[[wpf-vista-theming-conventions]]`, `[[wpf-setter-targets-clr-property-not-dependencyproperty]]`
- New pattern created: `[[wpf-vista-tooltips]]`
