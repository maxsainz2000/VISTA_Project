---
module: MerchSys.Integration
plan-id: INT-23
title: "ViewModel data-access layering + timer disposal"
depends-on: [INT-18]
estimated-files: 11
priority: medium
---

# INT-23: ViewModel data-access layering + timer disposal

## Context

Two verified MVVM-hygiene findings:

- **INV-3 / POS-3 / PUR-2 — ViewModels open raw `MySqlConnection`s.** Four ViewModels reach past the service layer to run SQL directly: `ProductManagementViewModel`, `CreditManagementViewModel`, `PurchasingDashboardViewModel`, `PurchaseOrderListViewModel`. This couples the presentation layer to the DB engine.
- **INV-2 — leaking refresh timers.** `StockDashboardViewModel`, `ExpiryMonitorViewModel`, and (missed by the reports) `FinancialOverviewViewModel` start AutoReset `System.Timers.Timer`s without implementing `IDisposable`, so the VM and its injected services/DbContext can never be collected and the timer keeps firing DB queries.

**Layering rule for this batch:** move the query *into a service*, but the service keeps the existing data-access shape — **raw ADO.NET for full-entity loads** (the `ToListAsync` workaround), or an **EF projection** for aggregate/scalar results (GROUP BY trend, KPI sums are safe because projections are not hit by the materialisation bug). Do **not** convert full-entity loads to `ToListAsync()`.

## Prerequisites

- **INT-18** — remediation index.

## Wiki References

- `LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` — projections safe, full entities not.
- `LLM_Wiki/wiki/concepts/modular-monolith.md` — ViewModels in module libraries, no DB in the VM.

## Deliverables

```
' --- Data-access layering ---
MerchSys.Inventory/Services/IStockService.vb + StockService.vb           ' Modified — product/category load method
MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb              ' Modified — call service, drop MySqlConnection
MerchSys.POS/Services/ICreditService.vb + CreditService.vb               ' Modified — GetCreditTransactionsAsync
MerchSys.POS/ViewModels/CreditManagementViewModel.vb                     ' Modified — call service, drop MySqlConnection
MerchSys.Purchasing/Services/IPurchasingDashboardService.vb (+impl)      ' New — dashboard queries
MerchSys.Purchasing/ViewModels/PurchasingDashboardViewModel.vb           ' Modified — call service, drop MySqlConnection
MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb             ' Modified — use injected _vendorService.GetAllAsync()
MerchSys.App/Startup/*ServiceRegistration / Application.xaml.vb          ' Modified — register new service(s)

' --- Timer disposal ---
MerchSys.Inventory/ViewModels/StockDashboardViewModel.vb                 ' Modified — Implements IDisposable
MerchSys.Inventory/ViewModels/ExpiryMonitorViewModel.vb                  ' Modified — Implements IDisposable
MerchSys.Accounting/ViewModels/FinancialOverviewViewModel.vb             ' Modified — Implements IDisposable
MerchSys.App/Views/**/(StockDashboard|ExpiryMonitor|FinancialOverview)View.xaml.vb  ' Modified — dispose VM on Unloaded
```

## Specification

### Part 1 — data-access layering

For each ViewModel that opens a `MySqlConnection`:

1. Add a method to the owning **service interface + implementation** that returns the data the VM needs (a DTO or entity list). Move the exact SQL/reader loop into the service unchanged — keep raw ADO.NET for full-entity loads; use an EF **projection** (`.Select(Function(x) New ...With{...}).ToListAsync()` or `.GroupBy(...).Select(...)`) only for aggregate/scalar shapes.
2. Replace the VM's connection/reader block with a call to the injected service.
3. Remove now-unused `Imports MySqlConnector` and any `_db.Database.GetConnectionString()` use from the VM.

Per-VM notes:

- **`ProductManagementViewModel`** (`_db.Database.GetConnectionString()` at ~563) — add e.g. `IStockService.GetProductsWithCategoriesAsync()` (raw ADO, full entities) and call it.
- **`CreditManagementViewModel`** (~460) — add `ICreditService.GetCreditTransactionsAsync(accountId)` returning `List(Of CreditTransactionItem)`; the VM maps straight into `CreditTransactions`.
- **`PurchasingDashboardViewModel`** (~308/393/438) — create `IPurchasingDashboardService` with the monthly-trend and KPI queries. The trend (`GROUP BY YEAR/MONTH`) is an **EF projection** and is safe; register the service in DI.
- **`PurchaseOrderListViewModel`** (~333) — this VM already injects `_vendorService` and then redundantly re-queries vendors via raw ADO. Delete the raw block and call `Await _vendorService.GetAllAsync()`.

> `OwnerDashboardViewModel` also opens a `MySqlConnection` for its trend chart (App). It is **optional** in this batch; if included, follow the same pattern (move to a service/projection). Note the decision in the summary.

### Part 2 — timer disposal (INV-2 + FinancialOverview)

For `StockDashboardViewModel`, `ExpiryMonitorViewModel`, and `FinancialOverviewViewModel`:

1. Add `Implements IDisposable` (alongside any existing interface such as `IFreshnessAware`).
2. Implement:

```vb
Public Sub Dispose() Implements IDisposable.Dispose
    If _refreshTimer IsNot Nothing Then
        _refreshTimer.Stop()
        RemoveHandler _refreshTimer.Elapsed, AddressOf OnRefreshTick
        _refreshTimer.Dispose()
    End If
End Sub
```

3. Ensure the corresponding **View** disposes the VM on `Unloaded` (mirror the existing `OwnerDashboardView` pattern — it already does this for its `DispatcherTimer`). If a View has no `Unloaded` handler, add one that casts `DataContext` to `IDisposable` and calls `Dispose()`.

> Confirm whether each View already has an `Unloaded`/cleanup handler before adding one, to avoid double-dispose. Guard `Dispose` to be idempotent.

## Implementation Notes

- This is the meatiest batch. Do the timer disposal (Part 2 — mechanical, low-risk) first, then the layering (Part 1).
- Keep service method signatures minimal and return existing DTOs/entities where possible to limit ripple.
- The new `IPurchasingDashboardService` must be registered in DI; verify the dashboard still loads.
- Build after each VM/service pair (`dotnet build MerchSys.slnx`). Per `CLAUDE.md`, document non-trivial build errors in the summary and stop.
- Do not introduce full-entity `ToListAsync()` in any moved query.

## Acceptance Criteria

1. None of the four flagged ViewModels (`ProductManagement`, `CreditManagement`, `PurchasingDashboard`, `PurchaseOrderList`) references `MySqlConnection` or `GetConnectionString()`; each calls a service.
2. `PurchaseOrderListViewModel` uses `_vendorService.GetAllAsync()` (redundant raw query removed).
3. Moved full-entity loads use raw ADO.NET; aggregate queries use EF projections — no full-entity `ToListAsync()` introduced.
4. `StockDashboardViewModel`, `ExpiryMonitorViewModel`, and `FinancialOverviewViewModel` implement `IDisposable` (stop+detach+dispose timer) and are disposed on `View.Unloaded`.
5. New services are registered in DI; the affected screens load and refresh.
6. `dotnet build MerchSys.slnx` — target 0 errors, 0 warnings (or documented per protocol).

## Output Requirements

Create a progress report at `Progress/VISTA_Modules/Integration/INT-23-summary.md` using `Progress/_template.md`. List the service methods added, the VMs cleaned, the timer-disposal wiring per View, and whether `OwnerDashboardViewModel` was included.
