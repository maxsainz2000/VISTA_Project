---
module: MerchSys.App
plan-id: INFRA-30
title: "Master-Detail Rail Sidebar Refactor"
depends-on: []
estimated-files: 12
priority: medium
amendment-ref: AMD-2026-05-28-01
---

# INFRA-30: Master-Detail Rail Sidebar Refactor

## Context

The current sidebar exposes all 22 navigation items in one long flat list (5 POS + 6 Purchasing + 4 Inventory + 6 Accounting + 1 Dev Tools, per the manager verification checklist Part 2). Cognitive load grows with each module added.

Per the 2026-05-28 design decision, the sidebar is replaced by a **master-detail rail**: a narrow vertical rail of 4 module icons (Purchasing / Inventory / POS / Accounting) on the far left; selecting one swaps the detail panel to show only that module's sub-views. Inspired by VS Code's activity bar and Outlook's module switcher.

This plan is **UI-only** and **independent of the SQLite-removal track** — can run in parallel with INFRA-23 → INFRA-29.

## Prerequisites

- None. (Can run in parallel with the SQLite-removal track.)

## Wiki References

- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` — sidebar revision is mentioned in the project log entry but not in the amendment body
- `Operator/manager-verification-checklist.md` Part 2 — current sidebar inventory (22 items, 5 groups including Dev Tools)

## Deliverables

```
MerchSys.App/Views/Shell/MainWindow.xaml                        ' MOD — replace single sidebar with rail + detail panel grid
MerchSys.App/Views/Shell/ActivityRail.xaml                      ' NEW — 4 module icons (+ Dev Tools in Debug builds)
MerchSys.App/Views/Shell/ActivityRail.xaml.vb                   ' NEW
MerchSys.App/Views/Shell/ModuleDetailPanel.xaml                 ' NEW — ContentControl that swaps per active module
MerchSys.App/Views/Shell/ModuleDetailPanel.xaml.vb              ' NEW

MerchSys.App/Views/Shell/Modules/PurchasingPanel.xaml           ' NEW — Purchasing nav items only
MerchSys.App/Views/Shell/Modules/InventoryPanel.xaml            ' NEW
MerchSys.App/Views/Shell/Modules/PosPanel.xaml                  ' NEW
MerchSys.App/Views/Shell/Modules/AccountingPanel.xaml           ' NEW
MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml       ' NEW (Debug only)

MerchSys.App/ViewModels/MainWindowViewModel.vb                  ' MOD — ActiveModule property + module-switch command
MerchSys.App/ViewModels/Shell/ActivityRailViewModel.vb          ' NEW
MerchSys.App/ViewModels/Shell/ModulePanelViewModel.vb           ' NEW base for the four module panel VMs (or extend existing)
```

## Specification

### Layout

```
┌──┬─────────────────┬────────────────────────────────────────┐
│  │ Purchasing      │                                        │
│ P│ ──────────────  │                                        │
│ I│ ▸ Purchase Ords │                                        │
│ S│ ▸ Goods Recvg   │           Active View                  │
│ A│ ▸ Vendor Dir    │       (StockDashboardView,             │
│  │ ▸ Acc. Payable  │        PurchaseOrdersView, etc.)       │
│ D│ ▸ Reorder Sug.  │                                        │
│  │ ▸ Vendor Cat.   │                                        │
│  │                 │                                        │
└──┴─────────────────┴────────────────────────────────────────┘
 60px   220px              remaining width
 Rail   Module Detail      Content
```

- **Rail (60px)** — 4 icons stacked top-to-bottom (Purchasing / Inventory / POS / Accounting). Active module's icon shows a left accent bar (`#2980B9`, 3px). In Debug builds, a 5th icon for Developer Tools appears at the bottom.
- **Module Detail Panel (220px)** — shows the active module's sub-views as a vertical list of selectable items.
- **Content (remaining)** — the actual view (Stock Dashboard, Purchase Orders, etc.), unchanged from today.

### Default landing

- **Manager** lands on **Inventory → Stock Dashboard** (preserved from today).
- **Owner** lands on **Accounting → Financial Overview** (preserved from today).
- "Default landing" is implemented by selecting the rail icon AND the sub-view item on startup, per role.

### Active-module persistence

- Active module persists for the session (in-memory). Does not persist across restarts in this plan (could be added later via user prefs).

### Icons

Use Segoe Fluent Icons (built into Windows 11) or a vector library already in NuGet (`Material.Icons.WPF` if not already referenced). One glyph per module:

| Module | Glyph hint |
|---|---|
| Purchasing | shopping cart / clipboard |
| Inventory | boxes / warehouse |
| POS | cash register / receipt |
| Accounting | chart-bar / ledger |
| Developer Tools | wrench (Debug builds only) |

Each icon has a tooltip with the module name.

### Sub-view item rendering

Within a module panel, items render as plain text buttons with a chevron `▸` left of label. Active sub-view is highlighted with `#D6EAF8` background and `#2980B9` left border (matching the existing selection-highlight style from the Stock Dashboard tests).

### Role-aware visibility

Module panels honor existing Manager/Owner role filtering:
- **Manager** sees all sub-views in all four modules.
- **Owner** sees read-only KPI views: Accounting → Financial Overview, Sales Summary, Income Statement, VAT Relief Report; Inventory → Stock Dashboard (read-only); etc. The exact Owner sub-view list comes from the existing role mapping — do not change it in this plan.

### Keyboard shortcuts

- `Ctrl+1` Purchasing
- `Ctrl+2` Inventory
- `Ctrl+3` POS
- `Ctrl+4` Accounting
- `Ctrl+0` Developer Tools (Debug only)

Document in `MainWindow.xaml` via `KeyBinding` entries.

### Migration of existing nav code

The current sidebar is likely a `StackPanel` or `ListBox` in `MainWindow.xaml` driven by `MainWindowViewModel.NavigationItems` (a flat collection). Refactor to:
- `MainWindowViewModel.ActiveModule` (enum: Purchasing/Inventory/POS/Accounting/DeveloperTools)
- Each module has its own ObservableCollection of nav items.
- `ActivityRailViewModel.SelectModuleCommand(moduleEnum)` mutates `ActiveModule`.
- `ModuleDetailPanel` uses a `DataTemplateSelector` (or a `ContentControl` with bound `ContentTemplate`) to render the active module's panel.

### Sync with ConnectionStatusIndicator (INFRA-28)

The activity rail must coexist with the connection status indicator from INFRA-28. Suggested placement: indicator stays in the shell header (top), rail occupies the left edge full-height.

If INFRA-28 hasn't shipped when this plan ships, leave a placeholder slot in `MainWindow.xaml` with an inline `' INFRA-28: ConnectionStatusIndicator goes here ` comment.

## Acceptance Criteria

1. Launching the app as `manager` shows the activity rail with 4 icons (5 in Debug), Inventory icon active, Stock Dashboard rendered.
2. Clicking the Purchasing icon swaps the detail panel to show only the 6 Purchasing sub-views. Inventory items are no longer visible.
3. Sub-view list updates the content area correctly when clicked.
4. Active sub-view highlight matches `#D6EAF8` + `#2980B9` left border.
5. Owner login shows only Owner-accessible items per existing role mapping; no new items leak.
6. `Ctrl+1` through `Ctrl+4` (and `Ctrl+0` in Debug) switch modules.
7. Tooltip on each rail icon displays the module name.
8. Build: 0 errors / 0 warnings.

## Out of Scope (Defer)

- Cross-session persistence of active module (in-memory only this plan).
- Customizable rail order or hidden modules.
- Hover-to-preview submenu (a la VS Code activity bar minimap). Out of scope.
- Mobile/responsive layouts.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-30-summary.md` per `Progress/_template.md`. Include:

- Screenshots: rail collapsed view + each of the 4 module panels expanded.
- Confirmation that role-aware visibility still matches the manager verification checklist Part 2 expectations.
- Confirmation that no existing view was deleted (only navigation reorganized).
- Keyboard-shortcut transcript.

### Documentation

- `MainWindowViewModel.vb` updated XML doc on `ActiveModule` property.
- Inline comments in `MainWindow.xaml` documenting the 3-column grid layout (Rail / Module Detail / Content).
- Update `Operator/manager-verification-checklist.md` Part 2 to reflect the new master-detail navigation pattern — re-describe how to verify each module's sub-views in the new UI.
