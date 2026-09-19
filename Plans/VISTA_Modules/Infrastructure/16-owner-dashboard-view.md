---
module: MerchSys.Infrastructure
plan-id: INFRA-16
title: "Owner Dashboard & Read-Only View Enforcement"
depends-on: [INFRA-15, INT-02]
estimated-files: 6
---

# Owner Dashboard & Read-Only View Enforcement

## Context

The system plan (Section 7) defines the Owner role's access matrix:

| Feature Area | Manager | Owner |
|---|---|---|
| Purchasing — Create / Edit POs | Full Access | No Access |
| Purchasing — View PO Status | Full Access | Read-Only |
| Inventory — Record Adjustments / Shrinkage | Full Access | No Access |
| Inventory — View Stock Dashboard | Full Access | Read-Only |
| POS — Process Transactions | Full Access | No Access |
| POS — View Transaction History | Full Access | Read-Only |
| Accounting — View All Reports | Full Access | Read-Only |
| System Settings / User Management | Full Access | No Access |

The system plan also specifies an **Owner KPI Dashboard** (Section 6.4):

> **Purchasing KPIs:** Active vendor list, current PO status and pending deliveries.
> **Inventory KPIs:** Current stock on hand summary, low-stock item count.
> **Sales KPIs:** Daily/weekly revenue, transaction count, top-selling products.
> **Accounting KPIs:** Current period profit/loss, cash position, overdue AR total, upcoming AP due.

Currently, the `UserRole.Owner` enum value exists (in `SharedKernel/Enums/UserRole.vb`) and `MainWindowViewModel` already gates two items (VAT Settings, VAT Return) behind `UserRole.Manager`. However:

1. There is **no Owner-specific dashboard view** — the Owner would land on the same Stock Dashboard as the Manager.
2. The remaining navigation items are **not filtered** — the Owner currently sees all CRUD views (Sales Cart, Goods Receiving, Shrinkage, etc.) even though the system plan says "No Access."
3. CRUD views do **not disable** edit controls for the Owner role — if the Owner navigates to a view, they could potentially write data (DA5 violation).

INFRA-15 delivers the login mechanism that makes role-switching possible. This plan delivers the Owner's full experience: a dedicated dashboard, proper navigation filtering, and read-only enforcement on shared views.

## Prerequisites

- **INFRA-15** (Login Form & User Authentication) — `LoginSessionService`, `ISessionService.CurrentRole` reflects the authenticated user
- **INT-02** (Shell Navigation) — `MainWindowViewModel.BuildNavigationGroups()`, `NavigationItem`, `NavigationGroup`

## Wiki References

- `concepts/owasp-da-top10.md` — DA5 (Improper Authorization): Owner read-only enforced at data-access layer, not just UI
- `Sources/system_plan.md` — Section 6.4 (Owner KPI Dashboard), Section 7 (User Roles and Access Control)
- `concepts/plain-language-reporting.md` — "What This Means" boxes mandatory on all financial reports (the Owner dashboard KPIs should also include plain-language interpretation per the system plan)

## Deliverables

```
MerchSys.App/ViewModels/
└── OwnerDashboardViewModel.vb                           ' New

MerchSys.App/Views/
└── OwnerDashboardView.xaml + OwnerDashboardView.xaml.vb ' New

MerchSys.App/ViewModels/MainWindowViewModel.vb          ' Modified — Owner navigation filtering
MerchSys.App/Views/MainWindow.xaml                       ' Modified — role display in shell header

MerchSys.SharedKernel/Interfaces/ISessionService.vb     ' Modified — add IsAuthenticated (if not done by INFRA-15)
```

## Specification

### OwnerDashboardView

A single-page, read-only KPI dashboard displaying the four KPI groups specified in the system plan. The Owner opens the app and sees everything they need on one screen — no drilling into operational views.

**Layout** — a 2×2 card grid with a header row:

```
┌──────────────────────────────────────────────────────────┐
│  Welcome, [Owner Username]            [Log Out]          │
│  Villon Farm Supply — Owner Dashboard                    │
├────────────────────────────┬─────────────────────────────┤
│                            │                             │
│   📦 PURCHASING            │   📊 INVENTORY              │
│                            │                             │
│   Active Vendors: 12       │   Total SKUs: 48            │
│   Open POs: 3              │   Stock Value: ₱234,500     │
│   Pending Deliveries: 2    │   Low-Stock Items: 5 ⚠️     │
│   Overdue AP: ₱15,200      │   Expiring Soon: 3 🔴       │
│                            │                             │
│   "You have 3 purchase     │   "5 products are below     │
│    orders awaiting          │    minimum stock level.     │
│    delivery."               │    Check reorder            │
│                            │    suggestions."            │
├────────────────────────────┼─────────────────────────────┤
│                            │                             │
│   💰 SALES                 │   📈 ACCOUNTING             │
│                            │                             │
│   Today's Revenue: ₱12,400 │   Current Period P&L:       │
│   This Week: ₱67,800       │     Net Income: ₱45,200     │
│   Transactions Today: 23   │   Cash Position: ₱89,000    │
│   Top Product: Urea 50kg   │   Overdue AR: ₱8,500        │
│                            │   Upcoming AP: ₱22,000      │
│   "Sales are 15% above     │                             │
│    last week. Urea is      │   "Business is profitable   │
│    your best seller."      │    this period. ₱8,500 in   │
│                            │    customer credit is        │
│                            │    overdue."                │
└────────────────────────────┴─────────────────────────────┘
```

Each card includes a **"What This Means"** plain-language interpretation section, consistent with the system plan's mandatory requirement for all financial displays (A5).

### OwnerDashboardViewModel

```
Public Class OwnerDashboardViewModel
    Inherits ObservableObject

    Public Sub New(
        session As ISessionService,
        inventoryService As IStockDashboardService,
        purchasingService As IPurchaseOrderService,
        salesService As IDailySummaryService,
        accountingService As IFinancialOverviewService,
        apLedgerService As IAPLedgerService
    )
    End Sub

    ' ── Purchasing KPIs ──
    <ObservableProperty> Private _activeVendorCount As Integer
    <ObservableProperty> Private _openPurchaseOrderCount As Integer
    <ObservableProperty> Private _pendingDeliveryCount As Integer
    <ObservableProperty> Private _overdueApTotal As Decimal
    <ObservableProperty> Private _purchasingInterpretation As String

    ' ── Inventory KPIs ──
    <ObservableProperty> Private _totalSkuCount As Integer
    <ObservableProperty> Private _totalStockValue As Decimal
    <ObservableProperty> Private _lowStockItemCount As Integer
    <ObservableProperty> Private _expiringSoonCount As Integer
    <ObservableProperty> Private _inventoryInterpretation As String

    ' ── Sales KPIs ──
    <ObservableProperty> Private _todayRevenue As Decimal
    <ObservableProperty> Private _weekRevenue As Decimal
    <ObservableProperty> Private _todayTransactionCount As Integer
    <ObservableProperty> Private _topSellingProduct As String
    <ObservableProperty> Private _salesInterpretation As String

    ' ── Accounting KPIs ──
    <ObservableProperty> Private _currentPeriodNetIncome As Decimal
    <ObservableProperty> Private _cashPosition As Decimal
    <ObservableProperty> Private _overdueArTotal As Decimal
    <ObservableProperty> Private _upcomingApDue As Decimal
    <ObservableProperty> Private _accountingInterpretation As String

    ' ── State ──
    <ObservableProperty> Private _isLoading As Boolean
    <ObservableProperty> Private _ownerDisplayName As String
    <ObservableProperty> Private _lastRefreshedAt As DateTime

    <RelayCommand>
    Private Async Function RefreshAsync() As Task
        ' Load all KPIs from existing services
        ' Generate plain-language interpretations
    End Function

End Class
```

**Data sources:** The ViewModel reuses **existing services** — it does NOT create new data-access code. All four modules already have services that compute these metrics:

| KPI Group | Source Service | Already Exists? |
|---|---|---|
| Purchasing — Active vendors | `IVendorService` or vendor repository | Yes (PUR module) |
| Purchasing — Open POs | `IPurchaseOrderService` | Yes (PUR module) |
| Purchasing — Overdue AP | `IAPLedgerService` | Yes (PUR module) |
| Inventory — Stock summary | `IStockDashboardService` | Yes (INV module) |
| Inventory — Low-stock count | `ILowStockAlertService` | Yes (INV module) |
| Inventory — Expiring soon | `IExpiryTrackingService` | Yes (INV module) |
| Sales — Daily/weekly revenue | `IDailySummaryService` | Yes (POS module) |
| Sales — Top product | `IDailySummaryService` | Yes (POS module) |
| Accounting — Net income | `IFinancialOverviewService` | Yes (ACC module) |
| Accounting — Overdue AR | `IFinancialOverviewService` | Yes (ACC module) |
| Accounting — Upcoming AP | `IFinancialOverviewService` or `IAPLedgerService` | Yes (ACC/PUR module) |

If any service method is not publicly accessible (e.g., returns a ViewModel-specific shape), add a lightweight query method to the existing interface rather than duplicating data access. Document each addition in the implementation summary.

**Plain-language interpretation generation:**

Each card's interpretation string is computed from the KPI values using simple conditional logic:

```
' Inventory example:
If LowStockItemCount > 0 Then
    InventoryInterpretation = $"{LowStockItemCount} products are below minimum stock level. Check reorder suggestions."
Else
    InventoryInterpretation = "All products are above minimum stock level."
End If
```

Follow the "What This Means" convention from the existing `FinancialOverviewService` and `IncomeStatementService`. The interpretation must use plain language suitable for a reader with partial financial literacy (A5).

### Auto-Refresh

The dashboard auto-refreshes every 60 seconds using a `DispatcherTimer` (same pattern as `SyncStatusIndicatorViewModel` from INFRA-10). This ensures the Owner sees near-real-time data without manual refresh.

A manual "Refresh" button is also provided at the top of the dashboard.

The timer is started when the view is loaded and stopped/disposed when the view is unloaded or the user logs out.

### Owner Navigation Filtering

Modify `MainWindowViewModel.BuildNavigationGroups()` to show **different sidebar items** based on `ISessionService.CurrentRole`:

**Manager sees (current — unchanged):**
- Point of Sale: Sales Cart, Credit Management, Transaction History, Daily Summary, VAT Settings
- Purchasing: Purchase Orders, Goods Receiving, Vendor Directory, Accounts Payable, Reorder Suggestions
- Inventory: Stock Dashboard, Product Management, Expiry Monitor, Shrinkage
- Accounting: Financial Overview, Income Statement, Sales Summary, Tamper Audit Report, VAT Return (BIR)
- Developer Tools (DEBUG only)

**Owner sees:**
- Owner Dashboard (landing page — the new `OwnerDashboardView`)
- Read-Only Views:
  - POS: Transaction History (read-only)
  - Purchasing: Purchase Orders (read-only), Accounts Payable (read-only)
  - Inventory: Stock Dashboard (read-only)
  - Accounting: Financial Overview, Income Statement, Sales Summary

**Owner does NOT see:**
- Sales Cart, Credit Management, Daily Summary, VAT Settings
- Goods Receiving, Vendor Directory, Reorder Suggestions
- Product Management, Expiry Monitor, Shrinkage
- Tamper Audit Report, VAT Return (BIR)
- Developer Tools

This is derived directly from the system plan Section 7 access matrix. The Manager column says "Full Access" everywhere; the Owner column says "Read-Only" on view/report screens and "No Access" on CRUD/operational screens.

**Implementation approach:**

Introduce a `RequiredRole` property on `NavigationItem`:

```
Public Class NavigationItem
    ' ... existing properties ...
    Public Property MinimumRole As UserRole?               ' Nothing = visible to all roles
End Class
```

In `BuildNavigationGroups()`, tag each item with `MinimumRole`. Then filter:

```
' In each Build*NavItems() method:
items = items.Where(Function(i) i.MinimumRole Is Nothing OrElse
                                 _session.CurrentRole >= i.MinimumRole).ToList()
```

Alternatively, since there are only two roles with very different views, a simpler approach is to use the existing `If _session.CurrentRole = UserRole.Manager Then` pattern (already used for VAT Settings and VAT Return) and extend it to all operational items. The implementer should choose the cleaner approach and document the decision.

### Default Landing Page Per Role

Modify `MainWindowViewModel.NavigateToDefault()`:

```
Public Sub NavigateToDefault()
    If _session.CurrentRole = UserRole.Owner Then
        ' Navigate to OwnerDashboardView
        Dim ownerItem = NavigationGroups.SelectMany(Function(g) g.Items) _
            .FirstOrDefault(Function(i) i.ViewType = GetType(Views.OwnerDashboardView))
        If ownerItem IsNot Nothing Then Navigate(ownerItem)
    Else
        ' Existing Manager default: Stock Dashboard
        Dim defaultItem = NavigationGroups.SelectMany(Function(g) g.Items) _
            .FirstOrDefault(Function(i) i.ViewType = GetType(Views.Inventory.StockDashboardView))
        If defaultItem IsNot Nothing Then Navigate(defaultItem)
    End If
End Sub
```

### Read-Only Enforcement on Shared Views

Views that appear in both Manager and Owner navigation (Transaction History, Purchase Orders, Accounts Payable, Stock Dashboard, Financial Overview, Income Statement, Sales Summary) must **disable all write controls** when the Owner is logged in.

**Pattern:** Each shared view's ViewModel checks `ISessionService.CurrentRole` and exposes a `CanEdit` property:

```
Public ReadOnly Property CanEdit As Boolean
    Get
        Return _session.CurrentRole = UserRole.Manager
    End Get
End Property
```

XAML binds `IsEnabled` on buttons, text inputs, and action commands:

```xml
<Button Content="Save" IsEnabled="{Binding CanEdit}" Command="{Binding SaveCommand}" />
```

This is **UI-layer enforcement only**. Per DA5, the data-access layer must also reject writes from Owner sessions. However, the existing repository/service layer does not yet enforce role-based write restrictions. This plan adds the UI enforcement; a separate follow-up plan should address data-access-layer enforcement to fully satisfy DA5.

> **Note:** The existing `MainWindowViewModel` role gates already prevent the Owner from seeing operational CRUD views. Combined with `CanEdit = False` on shared views, the Owner cannot reach any write path through normal UI interaction. Full data-layer enforcement is documented as a "What's Next" item.

### Shell Header — Role Display

Add a small user/role indicator to the `MainWindow.xaml` shell header (top-right area, near the Log Out button added by INFRA-15):

```
┌──────────────────────────────────────────────────────┐
│  VISTA                              owner (Owner)  🚪│
│  ─────────────────────────────────────────────────── │
│  [Sidebar]  │  [Content Area]                        │
```

Bound to `ISessionService.CurrentUsername` and `ISessionService.CurrentRole`. This gives the operator immediate visual confirmation of which role they are testing.

## Implementation Notes

- **No new data-access code.** The OwnerDashboardViewModel calls existing service methods. If a needed query doesn't exist (e.g., "count of open POs"), add a single method to the existing interface/service rather than creating a new service. Document each addition.
- **"What This Means" is mandatory.** Per the system plan and academic paper A5, every financial figure displayed to the Owner must include a plain-language interpretation. This is non-negotiable. The interpretations should be computed in the ViewModel, not in the View code-behind.
- **No CRUD views for Owner.** The Owner's sidebar navigation does not include any view that allows creating, editing, or deleting records. If the Owner can see a view, all edit controls must be disabled.
- **Existing role gates are preserved.** The `If _session.CurrentRole = UserRole.Manager Then` blocks already in `BuildPosNavItems()` and `BuildAccountingNavItems()` remain and are extended to cover additional items.
- **Responsive layout.** The 2×2 KPI card grid should adapt to smaller window sizes. Use WPF's `UniformGrid` or `WrapPanel` so cards stack vertically when the window is narrow.
- Per the feedback memory: VB.NET `Await` is forbidden in `Catch`/`Finally`. The ViewModel's `RefreshAsync` must handle errors after the `Try` block.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. Logging in as Owner shows `OwnerDashboardView` as the landing page (not Stock Dashboard).
3. Logging in as Manager still shows Stock Dashboard as the landing page.
4. Owner's sidebar navigation shows only: Owner Dashboard, Transaction History, Purchase Orders, Accounts Payable, Stock Dashboard, Financial Overview, Income Statement, Sales Summary.
5. Owner's sidebar does NOT show: Sales Cart, Credit Management, Daily Summary, VAT Settings, Goods Receiving, Vendor Directory, Reorder Suggestions, Product Management, Expiry Monitor, Shrinkage, Tamper Audit Report, VAT Return (BIR).
6. Owner Dashboard displays KPIs from all four modules: Purchasing, Inventory, Sales, Accounting.
7. Each KPI card includes a plain-language "What This Means" interpretation section.
8. Owner Dashboard auto-refreshes every 60 seconds.
9. On shared views (Transaction History, Stock Dashboard, Financial Overview, etc.), edit/save buttons are disabled when logged in as Owner.
10. The shell header displays the current username and role.
11. Switching from Owner to Manager via logout/login changes the sidebar and landing page in the same session.
12. **ACC checklist Test 8** passes: Owner sees the VAT Payable tile on Financial Overview but clicking it does nothing (no navigation to VatReturnView).
13. **POS checklist Test 13** passes: Manager sees "VAT Settings" in the sidebar; Owner does NOT.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-16-summary.md` using `Progress/_template.md`. Include:

- The final Owner sidebar navigation list (which views are included/excluded).
- Which existing service methods were reused vs. newly added for the Owner Dashboard KPIs.
- The pattern used for read-only enforcement on shared views (`CanEdit` property).
- A note about DA5 partial compliance: UI enforcement done, data-layer enforcement deferred.
- The "What This Means" interpretation logic for each KPI card.

### Documentation
- XML doc on `OwnerDashboardViewModel` describing the four KPI groups and their data sources.
- XML doc on the `CanEdit` property pattern explaining its relationship to DA5.
- Inline comment in `MainWindowViewModel` explaining the Owner navigation filtering rationale and citing the system plan Section 7.
