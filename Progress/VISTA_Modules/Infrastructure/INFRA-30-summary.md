---
module: Infrastructure
agent: claude-code
date: 2026-05-28
plan-ref: Plans/VISTA_Modules/Infrastructure/30-master-detail-rail-sidebar.md
status: completed
---

## Task Summary

Replaced the 22-item flat sidebar with a Master-Detail Activity Rail layout (INFRA-30). A 60px icon rail on the far left holds 4 module icons (PUR / INV / POS / ACC, plus DEV in Debug builds). Selecting an icon swaps the adjacent 220px Module Detail Panel to show only that module's sub-views. The content area (Col 2) is unchanged.

**Plan:** `[[30-master-detail-rail-sidebar]]`

## Layout

```
┌──────┬─────────────────────┬────────────────────────────────────────┐
│      │ Purchasing           │                                        │
│ PUR  │ ──────────────────── │                                        │
│ INV  │ ▸ Purchase Orders    │                                        │
│ POS  │ ▸ Goods Receiving    │         Active Content View            │
│ ACC  │ ▸ Vendor Directory   │    (StockDashboardView, etc.)          │
│      │ ▸ Vendor Catalog     │                                        │
│ DEV* │ ▸ Accounts Payable   │                                        │
│      │ ▸ Reorder Suggestions│                                        │
│      │ ──────────────────── │                                        │
│      │ [connection badge]   │                                        │
│      │ Log Out              │                                        │
└──────┴─────────────────────┴────────────────────────────────────────┘
 60px        220px                       remaining
 Rail        Module Detail Panel         Content
*DEV = Debug builds + Manager role only
```

## What Was Done

**New files:**
- `Models/AppModule.vb` — `AppModule` enum (Purchasing/Inventory/POS/Accounting/DeveloperTools), `RailItem` ObservableObject (ModuleId, Abbreviation, ToolTipText, IsActive)
- `ViewModels/Shell/ActivityRailViewModel.vb` — builds RailItems, delegates SelectModuleCommand to MainWindowViewModel, subscribes to ActiveModule changes to sync IsActive state
- `Views/Shell/ActivityRail.xaml` + `.vb` — 60px vertical rail; DI-injected ActivityRailViewModel; left accent bar (#2980B9, 3px) + dark background (#243342) on active icon
- `Views/Shell/ModuleDetailPanel.xaml` + `.vb` — 220px panel; DataContext = MainWindowViewModel; shows ActiveModuleName header, 5 DataTrigger-gated ItemsControls (one per module), ConnectionStatusSlot, Log Out button
- `Views/Shell/Modules/PurchasingPanel.xaml` + `.vb` — minimal UserControl binding to `PurchasingItems`
- `Views/Shell/Modules/InventoryPanel.xaml` + `.vb`
- `Views/Shell/Modules/PosPanel.xaml` + `.vb`
- `Views/Shell/Modules/AccountingPanel.xaml` + `.vb`
- `Views/Shell/Modules/DeveloperToolsPanel.xaml` + `.vb`

**Modified files:**
- `Models/NavigationItem.vb` — no change (AppModule.vb is a new file in same namespace)
- `ViewModels/MainWindowViewModel.vb` — added `ActiveModule`, `ActiveModuleName`, per-module item collections (`PurchasingItems`, `InventoryItems`, `PosItems`, `AccountingItems`, `DeveloperToolsItems`), `SelectModuleCommand`, `RebuildModuleCollections()`; role-aware navigation preserved
- `Views/Shell/MainWindow.xaml` — restructured to 3-column grid; `Window.InputBindings` for Ctrl+1–4 and Ctrl+0
- `Views/Shell/MainWindow.xaml.vb` — constructor now accepts `ActivityRail` + `ModuleDetailPanel` (removed `ConnectionStatusIndicator` — now owned by `ModuleDetailPanel`)
- `Application.xaml.vb` — registered `ActivityRailViewModel`, `ActivityRail`, `ModuleDetailPanel` as Singleton

## Keyboard Shortcuts

| Shortcut | Module |
|---|---|
| Ctrl+1 | Purchasing |
| Ctrl+2 | Inventory |
| Ctrl+3 | POS |
| Ctrl+4 | Accounting |
| Ctrl+0 | Developer Tools (Debug + Manager only) |

## Role-Aware Visibility

| Module | Manager sub-views | Owner sub-views |
|---|---|---|
| Purchasing | 6 items | 2 items (Orders, AP only) |
| Inventory | 4 items | 1 item (Stock Dashboard only) |
| POS | 5 items (incl. VAT Settings) | 1 item (Transaction History only) |
| Accounting | 6 items (incl. Tamper Audit, VAT Return) | 4 items (read-only KPIs) |
| Dev Tools | 1 item (Debug builds only) | Not visible |

Matches the INFRA-30 plan's acceptance criteria for Owner role — no additional items leak compared to the previous Owner navigation.

## Design Decisions

- **ModulePanelViewModel** — used "extend existing" approach per plan. Module panels bind directly to the inherited DataContext (MainWindowViewModel) rather than per-module ViewModels, since all panels are structurally identical (ItemsControl + nav item buttons).
- **ConnectionStatusSlot ownership** — moved from `MainWindow.xaml.vb` to `ModuleDetailPanel.xaml.vb`. ModuleDetailPanel receives `ConnectionStatusIndicator` via constructor injection.
- **`Module` keyword trap** — `RailItem.Module` renamed to `RailItem.ModuleId` to avoid BC30183 VB.NET reserved keyword conflict. See CLAUDE.md Build Traps.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A (testing phase separate) |

## Issues Encountered

- **Issue:** `Button.Style` set twice (attribute + child element) in `ActivityRail.xaml` → MC3024 error.
  - **Resolution:** Removed the `Style="{StaticResource RailButtonStyle}"` attribute; kept only `Button.Style` child element with `BasedOn="{StaticResource RailButtonStyle}"`.
- **Issue:** `RailItem.Module As AppModule` → BC30183 (`Module` is a VB.NET keyword).
  - **Resolution:** Renamed to `RailItem.ModuleId`. Updated all XAML bindings and VM references.

## What's Next

All INFRA-23 through INFRA-30 components are now complete. Remaining work:
- [ ] Operator: run the verification plan from `implementation_plan.md` (manual testing checklist)
- [ ] Apply `behaviors:DisableOnOfflineBehavior.IsDisabledWhenOffline="True"` to mutation buttons during testing phase
- [ ] Quarterly backup restore drill (first date ~3 months after deployment)

## Cross-References

- Domain Wiki pages consulted: `[[system_plan_amendment_2026-05-28]]`
- Agent Wiki entries consulted: `[[mariadb-pure-client-server-architecture]]`
