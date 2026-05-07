---
module: Integration
plan-id: INT-02
title: "Shell Navigation & View Wiring"
depends-on: [INT-01]
estimated-files: 4
---

# Shell Navigation & View Wiring

## Context

All 16 module Views (XAML + code-behind) were created during module implementation, but none are reachable from the `MainWindow` navigation sidebar. The application currently launches to a blank shell. This plan wires every View into the navigation system so that users can access all module screens at runtime.

**Audit sources:** `Pending_Tasks/Purchasing-audit-2026-05-07.md`, `Pending_Tasks/Inventory-audit-2026-05-07.md`, `Pending_Tasks/POS-audit-2026-05-07.md`, `Pending_Tasks/Accounting-audit-2026-05-07.md`

## Prerequisites

- INT-01 (App Composition Root) — all ViewModels and services must be DI-registered before views can be instantiated via navigation.

## Wiki References

- `LLM_Wiki/wiki/concepts/modular-monolith.md` — UI pattern (MVVM)
- `LLM_Wiki/wiki/concepts/client-server-wpf.md` — WPF shell architecture

## Deliverables

### 1. Navigation Service

Implement a navigation service (or enhance the existing `MainWindow` code-behind) that:
- Resolves Views from the DI container by type
- Displays the selected View in the main content area
- Highlights the active navigation item in the sidebar
- Supports back-navigation (optional, if sidebar makes this unnecessary)

### 2. MainWindow Sidebar Navigation

Update `MainWindow.xaml` to include a sidebar with navigation entries grouped by module:

```
── Point of Sale
   ├── Sales Cart               → SalesCartView
   ├── Credit Management        → CreditManagementView
   ├── Transaction History      → TransactionHistoryView
   └── Daily Summary            → DailySummaryView

── Purchasing
   ├── Purchase Orders          → PurchaseOrderListView
   ├── Goods Receiving          → GoodsReceivingView
   ├── Vendor Directory         → VendorDirectoryView
   ├── Accounts Payable         → APLedgerView
   └── Reorder Suggestions      → ReorderSuggestionsView

── Inventory
   ├── Stock Dashboard          → StockDashboardView
   ├── Product Management       → ProductManagementView
   ├── Expiry Monitor           → ExpiryMonitorView
   └── Shrinkage                → ShrinkageView

── Accounting
   ├── Financial Overview       → FinancialOverviewView
   ├── Income Statement         → IncomeStatementView
   └── Sales Summary            → SalesSummaryView
```

### 3. Notification.Wpf Integration

Wire `Notification.Wpf`'s `NotificationManager` so that toast notifications appear correctly. PUR-10 flagged that `Notification.Wpf` toast feedback was deferred — ensure the `NotificationManager` is initialized in `Application.xaml.vb` or the host builder and injectable where needed.

### 4. Default Landing View

Set a sensible default view when the application starts (e.g., `StockDashboardView` or `FinancialOverviewView`). The user should see something meaningful on launch rather than a blank content area.

## Implementation Notes

- The sidebar navigation structure should follow MVVM: sidebar binds to a collection of `NavigationItem` objects, each with a display name, icon hint, and target View type.
- Views live in `MerchSys.App/Views/` — they are XAML files with `DataContext` set to the corresponding ViewModel resolved from DI.
- Module grouping in the sidebar should use visual separators or expandable groups.
- Consider role-based visibility in a future plan (Manager vs. Owner) — for now, show all items.

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` completes with **0 errors, 0 warnings**
2. All 16 Views are reachable from the sidebar navigation
3. Clicking a navigation item loads the corresponding View in the content area
4. The active navigation item is visually highlighted
5. Application launches to a default View (not a blank screen)
6. `Notification.Wpf` toast notifications can fire from the `NotificationManager`

## Output Requirements

### Implementation Summary
After completing all code, create a progress report at:
```
Progress/VISTA_Modules/Integration/INT-02-summary.md
```
Using the template structure from `Progress/_template.md`. Include:
- All files created/modified with full paths
- Navigation structure implemented
- Default landing view chosen
- Build status confirmation
- Any deviations from this plan
