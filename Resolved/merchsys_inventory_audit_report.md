# Code Audit Report: MerchSys.Inventory

This report presents a thorough analysis of the `MerchSys.Inventory` module. Several issues, ranging from critical thread-safety bugs that can cause race conditions under load to resource leaks, MVVM pattern deviations, and database framework bypasses, have been identified.

## ✅ Verification Addendum — 2026-06-11

Verified against source under `WPF_Applications/MerchSys/src/MerchSys.Inventory/`. Finding 4's proposed fix is **dangerous**. **Where this addendum conflicts with a finding's severity or fix, the addendum governs.**

> ⚠️ **Landmine — keep the raw ADO.NET.** The raw `MySqlConnection` reader loops (and the FIFO `SELECT ... FOR UPDATE`) are the documented workaround for the EF Core 10 + VB.NET full-entity `ToListAsync()` empty-result bug and the architecture's row-locking requirement. Converting them to `.ToListAsync()`/`FromSqlRaw(...).ToListAsync()` returns empty lists at runtime.

| # | Reported | Verified verdict & action |
|---|---|---|
| 1 Class-level state | Critical | **Low (cleanup).** Per-process fields, not shared across client laptops; serialized by the DbContext. Field→local is safe cleanup, not a concurrency fix. |
| 2 Timer/IDisposable leak | High | **Confirmed.** `StockDashboardViewModel` + `ExpiryMonitorViewModel` start AutoReset `System.Timers.Timer`s and don't implement `IDisposable`. Implement `IDisposable` (stop+dispose timer, detach handler) and dispose on `View.Unloaded`. **Also fix `FinancialOverviewViewModel` (Accounting) — same leak, missed by both reports.** |
| 3 DB access in ProductManagementViewModel | High | **Confirmed (MVVM violation).** Move the query into a service — but the service must keep raw ADO.NET / scalar projection, **not** full-entity `ToListAsync()`. |
| 4 Raw ADO / change-tracker / N+1 | High | **Fix is dangerous — do not implement.** FIFO raw-ADO + `FOR UPDATE` is mandated by CLAUDE.md; the proposed `FromSqlRaw(...).ToListAsync()` empties the result and breaks every checkout. Keep raw ADO.NET. Only valid sub-point: the raw `UPDATE` sets `ModifiedAt` but not `ModifiedBy`. |
| 5 Expiry threshold inconsistency | Medium | **Minor.** `StockService` uses exact `UtcNow` consistently; the mismatch is vs `StockDashboardService`/`ExpiryTrackingService` (`UtcNow.Date`). Harmonize on one convention. Edge-case only (the exact expiry day). |
| 6 Date-to-string params | Medium | **Confirmed** — part of the systemic ISO-`"o"` cluster. Native-`DateTime` fix is safe. |
| 7 Stale SQLite comment | Low | **Confirmed**, cosmetic. (More instances exist: `POSDbContext`, `InventoryDbContext`, `PurchasingDbContext`, etc.) |

*Verified by Claude (Opus 4.8) on 2026-06-11. Original findings retained below for traceability.*

---

## Findings Summary Table

| Severity | Category | Component | Description |
| :--- | :--- | :--- | :--- |
| **Critical** | Thread Safety & Stability | Multiple Services: [StockService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockService.vb), [ShrinkageService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/ShrinkageService.vb), [ExpiryTrackingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/ExpiryTrackingService.vb), [LowStockAlertService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/LowStockAlertService.vb), [InventoryAuditService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/InventoryAuditService.vb), [GetProductCatalogQueryHandler.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Handlers/GetProductCatalogQueryHandler.vb) | Class-level state fields are used to store temporary query result lists, making services thread-unsafe under concurrent operations. |
| **High** | Memory & Resource Leak | [ExpiryMonitorViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/ExpiryMonitorViewModel.vb), [StockDashboardViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/StockDashboardViewModel.vb) | Un-disposed `System.Timers.Timer` instances leak ViewModels and DB context instances permanently in memory. |
| **High** | MVVM Pattern Deviation | [ProductManagementViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb) | Presentation layer directly references MySQL connection strings, opens connections, and runs raw database SELECT queries. |
| **High** | Design Smell & Robustness | Multiple Components | Bypassing Entity Framework Core with raw ADO.NET query dependencies, manual N+1 table joining, and fragile column-index mapping. |
| **Medium** | Stability & Bug | [StockBatch.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Entities/StockBatch.vb), Multiple Services | Discrepant expiry checking logic between midnight today (`Date`) and exact time (`UtcNow`) causes products to block checkout but display as normal stock. |
| **Medium** | Stability & Bug | Multiple Services | Fragile date-to-string parameter serialization in raw SQL queries (`today.ToString("o")`) instead of passing native .NET DateTime parameters. |
| **Low** | Documentation Smell | [GetProductsForCatalogQueryHandler.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Handlers/GetProductsForCatalogQueryHandler.vb) | Stale/contradictory XML comment claiming the handler queries a SQLite database, whereas the code instantiates a MySQL/MariaDB connection. |

---

## Detailed Findings & Proposed Diffs

### 1. Thread-Safety Vulnerabilities via Class-Level State Fields (Critical)

> [!WARNING]
> **Severe Risk of Concurrent Data Corruption and Runtime Crashes**  
> Storing query results in class-level instance fields inside services registered as Scoped or Singleton introduces severe race conditions when multiple terminals check out concurrently. If two checkouts occur at the same time, the lists will be overwritten, causing incorrect quantities to be deducted, transaction exceptions, or data corruption.

#### Root Cause
In [StockService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockService.vb#L35-L37):
```vb
        Private _batchesForFIFO As List(Of StockBatch)
        Private _batchesForProduct As List(Of StockBatch)
        Private _productStockList As List(Of Product)
```
These fields are initialized and populated directly inside methods:
```vb
        Public Async Function DeductStockFIFOAsync(...) As Task(...)
            _batchesForFIFO = New List(Of StockBatch)()
            ...
```
This pattern is repeated in multiple classes:
* `ShrinkageService.vb`: `_batchesForShrinkage` and `_shrinkageHistoryList`
* `ExpiryTrackingService.vb`: `_nearExpiryBatchList` and `_expiredBatchList`
* `LowStockAlertService.vb`: `_alertConfigList`
* `InventoryAuditService.vb`: `_auditHistoryList`
* `GetProductCatalogQueryHandler.vb`: `_catalogProductList`

All of these fields are only utilized within the scope of their respective method calls. They should be local variables, not class fields.

#### Proposed Correction (Example for `StockService.vb`)
```diff
     Public Class StockService
         Implements IStockService
 
         Private ReadOnly _db As InventoryDbContext
         Private ReadOnly _logger As ILogger(Of StockService)
-        Private _batchesForFIFO As List(Of StockBatch)
-        Private _batchesForProduct As List(Of StockBatch)
-        Private _productStockList As List(Of Product)
 
         Public Sub New(db As InventoryDbContext,
                        logger As ILogger(Of StockService))
```
*(Remove the fields and define them locally inside `DeductStockFIFOAsync`, `GetCurrentStockAsync`, and `GetStockBatchesAsync` by adding `Dim` to the variables.)*

---

### 2. ViewModel Timer and DB Connection Leaks (High)

> [!CAUTION]
> **Severe Memory and Connection Leak**  
> Standard `System.Timers.Timer` instances run on background threadpool threads. Because the view models do not implement `IDisposable` to stop and dispose of these timers, the garbage collector can never clean up the view models or their injected DB contexts. Every navigation to the Stock Dashboard or Expiry Monitor leaks a ViewModel instance and keeps active database connections open forever.

#### Root Cause
In [ExpiryMonitorViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/ExpiryMonitorViewModel.vb#L83-L85) (and similarly in `StockDashboardViewModel.vb`):
```vb
            _refreshTimer = New System.Timers.Timer(60_000) With {.AutoReset = True}
            AddHandler _refreshTimer.Elapsed, AddressOf OnRefreshTick
            _refreshTimer.Start()
```
Because the class does not implement `IDisposable`, the handler is never detached, the timer is never stopped, and the VM is permanently leaked in memory.

#### Proposed Correction (Example for `ExpiryMonitorViewModel.vb`)
```diff
     Public Class ExpiryMonitorViewModel
         Inherits ObservableObject
-        Implements IFreshnessAware
+        Implements IFreshnessAware, IDisposable
...
+        Public Sub Dispose() Implements IDisposable.Dispose
+            If _refreshTimer IsNot Nothing Then
+                _refreshTimer.Stop()
+                RemoveHandler _refreshTimer.Elapsed, AddressOf OnRefreshTick
+                _refreshTimer.Dispose()
+            End If
+        End Sub
```
*(Apply the same `IDisposable` implementation to `StockDashboardViewModel.vb`.)*

---

### 3. Database Access inside Presentation Layer (High)

> [!CAUTION]
> **Violation of MVVM Separation of Concerns**  
> ViewModels must not directly manage database connections, load connection strings, or execute raw SQL. Doing so couples the presentation layer directly to the database engine, prevents clean testing, and degrades maintainability.

#### Root Cause
In [ProductManagementViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb#L563-L565):
```vb
                Dim pmConnStr = _db.Database.GetConnectionString()
                Using pmConn As New MySqlConnection(pmConnStr)
                    Await pmConn.OpenAsync()
```
The view model is executing direct raw ADO.NET SQL commands to load products and categories.

#### Proposed Correction
Move the query logic into a service method in `IStockService` (or a dedicated product management service) and use Entity Framework Core instead of raw ADO.NET inside the ViewModel.

---

### 4. Fragile Raw ADO.NET Queries, Change Tracker Desync, and N+1 Joining (High)

> 🔴 **VERIFICATION (2026-06-11) — the proposed `FromSqlRaw(...).ToListAsync()` fix breaks checkout; do NOT implement.** The FIFO `SELECT ... FOR UPDATE` raw-ADO path is mandated by the architecture, and full-entity `ToListAsync()` silently returns empty in VB.NET + EF Core 10 — FIFO would find no batches and every sale would throw InsufficientStock. Keep the raw ADO.NET. Only valid sub-point: the raw `UPDATE` sets `ModifiedAt` but not `ModifiedBy`. See the Verification Addendum at the top.

> [!WARNING]
> **Risk of Silent Data Corruption, Stale Cache, and Test Failure**  
> Bypassing Entity Framework Core using direct raw ADO.NET queries (`MySqlConnection`) in business logic services causes multiple severe issues:
> 1. **Change Tracker Desync**: Modifying rows via direct `UPDATE` SQL commands in `StockService` and `ShrinkageService` bypasses the EF Core DbContext change tracker. Stale values of remaining quantities will be served if entities are queried subsequently within the same scope.
> 2. **Bypassing Auditing**: Automatic population of properties like `ModifiedBy` is bypassed because EF's save interceptors are never triggered.
> 3. **Manual N+1 joining**: Code manually runs separate raw SELECT commands for products, categories, batches, and shrinkages, stitching them together in memory using string joins and dictionary mappings.
> 4. **Testing Inhibition**: Hardcoding `MySqlConnection` blocks in-memory and SQLite unit testing.

#### Root Cause
In [VelocityService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/VelocityService.vb#L26-L121) (and in `StockService.vb`, `ShrinkageService.vb`, `StockDashboardService.vb`, `ExpiryTrackingService.vb`, and the Handlers):
The services manually construct a raw `MySqlConnection` and write manual query parsing loops like:
```vb
.Id = cReader.GetInt32(0)
.Name = cReader.GetString(1)
```

#### Proposed Correction
Refactor these direct database calls to clean, database-agnostic EF Core LINQ queries. If database locking (`FOR UPDATE`) is required for transactional safety, use EF's `FromSqlRaw` to load locked entities into the change tracker:
```vb
Dim batches = Await _db.StockBatches _
    .FromSqlRaw("SELECT * FROM Inv_StockBatches WHERE ProductId = {0} AND QuantityRemaining > 0 AND (ExpiryDate IS NULL OR ExpiryDate >= {1}) ORDER BY ReceiptDate ASC, Id ASC FOR UPDATE", productId, now) _
    .ToListAsync()
```
Modify the properties in memory, and call `Await _db.SaveChangesAsync()` to ensure concurrency tokens are validated and auditing remains intact.

For read queries, replace manual joins with clean LINQ eager loading:
```vb
Return Await _db.Products _
    .Include(Function(p) p.Category) _
    .Include(Function(p) p.StockBatches) _
    .Include(Function(p) p.ShrinkageRecords) _
    .Where(Function(p) Not p.IsDeleted AndAlso p.IsActive) _
    .ToListAsync()
```

---

### 5. Inconsistent Expiry Date Checking Logic (Medium)

> [!NOTE]
> **Business Logic Inconsistency**  
> The checkout engine treats a batch that expires today as immediately expired (preventing checkout). However, the dashboard and reports treat it as active/normal stock, causing a mismatch where stock is shown as available on the shelf but is unsellable.

#### Root Cause
1. In `StockService.DeductStockFIFOAsync`, checking is performed against exact current time `DateTime.UtcNow`:
   ```vb
   "AND (ExpiryDate IS NULL OR ExpiryDate >= @now)"
   ```
2. In `StockDashboardService.vb` and `ExpiryTrackingService.vb`, checking is done against midnight today `DateTime.UtcNow.Date`:
   ```vb
   Dim today As DateTime = DateTime.UtcNow.Date
   ```

#### Proposed Correction
Harmonize the date comparisons. Align the checkout engine (`StockService`) and reports to consistently evaluate against `DateTime.UtcNow.Date` or consistent UTC midnight thresholds.

---

### 6. Fragile Date-to-String Parameter Formatting (Medium)

> [!WARNING]
> **Risk of Driver and Culture Failures**  
> Serializing `DateTime` objects to ISO 8601 strings (`.ToString("o")`) or `"yyyy-MM-dd HH:mm:ss"` format before adding them to SQL parameters is highly fragile and risks format errors depending on server region settings.

#### Root Cause
In [ExpiryTrackingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/ExpiryTrackingService.vb#L50-L51):
```vb
                    neCmd.Parameters.Add(New MySqlParameter("@today", today.ToString("o")))
                    neCmd.Parameters.Add(New MySqlParameter("@threshold", thresholdDate.ToString("o")))
```

#### Proposed Correction
Pass the native `.NET DateTime` objects directly. The MySQL driver converts them correctly without string formatting overhead:
```diff
-                    neCmd.Parameters.Add(New MySqlParameter("@today", today.ToString("o")))
-                    neCmd.Parameters.Add(New MySqlParameter("@threshold", thresholdDate.ToString("o")))
+                    neCmd.Parameters.Add(New MySqlParameter("@today", today))
+                    neCmd.Parameters.Add(New MySqlParameter("@threshold", thresholdDate))
```

---

### 7. Contradictory/Stale XML Comments (Low)

#### Root Cause
In [GetProductsForCatalogQueryHandler.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Inventory/Handlers/GetProductsForCatalogQueryHandler.vb#L11-L13):
```vb
    ''' Handles GetProductsForCatalogQuery sent from cross-module catalog editors.
    ''' Queries active products directly from SQLite database to avoid EF Core VB.NET discovery bugs.
    ''' </summary>
```
The query uses `MySqlConnection` (targeting MySQL/MariaDB in production), making the reference to "SQLite database" misleading.

#### Proposed Correction
Update the comments to specify that the query targets the shared MariaDB/MySQL database.
