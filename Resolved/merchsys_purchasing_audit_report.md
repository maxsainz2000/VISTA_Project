# Code Audit Report: MerchSys.Purchasing

This report presents a thorough analysis of the `MerchSys.Purchasing` module. Several issues, ranging from critical thread-safety bugs that can cause race conditions under load to database framework bypasses, MVVM pattern deviations, and inefficient sequence generation logic, have been identified.

## ✅ Verification Addendum — 2026-06-11

Verified against source under `WPF_Applications/MerchSys/src/MerchSys.Purchasing/`. **Where this addendum conflicts with a finding's severity or fix, the addendum governs.**

> ⚠️ **Landmine — keep the raw ADO.NET** reader loops (documented EF Core 10 + VB.NET full-entity `ToListAsync()` empty-result workaround). Scalar/anonymous projections are safe.

| # | Reported | Verified verdict & action |
|---|---|---|
| 1 Class-level state | Critical | **Low (cleanup).** Per-process fields, not cross-laptop; serialized by the DbContext. Field→local cleanup. |
| 2 DB in dashboard/PO-list VMs | High | **Confirmed (MVVM violation).** `PurchaseOrderListViewModel` even re-implements the injected `_vendorService.GetAllAsync()` — replace with the service call. Move dashboard queries to a service (the monthly-trend GROUP BY is safe as an EF *projection*). |
| 3 PO-number generation | High | **Perf valid; race only half-fixed.** `.Select(p.OrderNumber).ToListAsync()` is a **scalar projection — it works** (not hit by the materializer bug). The proposed server-side `FirstOrDefault` fixes memory but **not** the cross-terminal race (two clients still read the same max). For real safety use a sequence table like `ReceiptIntegrityService.GetNextReceiptNumberAsync`. |
| 4 `DefaultUser="Manager"` clobbers session user | Medium | **Confirmed.** `BaseDbContext` unconditionally overwrites `CreatedBy/ModifiedBy` with `"Manager"`, discarding `_session.CurrentUsername` (e.g. `VatConfigurationWriter:154`). The "until auth is implemented" comment is stale — auth exists. Respect pre-set values or inject `ISessionService`. |
| 5 VAT event rounding | Medium | **Minor.** Real centavo drift from the pre-rounded unit cost; note the hardcoded `1.12D`. Pass exact line-level vatable/VAT values. |
| 6 Date-to-string param | Medium | **Confirmed** — systemic ISO-`"o"` cluster. |
| 7 Stale SQLite comment | Low | **Confirmed**, cosmetic. |

**Missed by this report:** the hardcoded `1.12D`/12% VAT assumption runs through `GoodsReceiptVatCalculator`, `GoodsReceivingViewModel`, and `GoodsReceiptLine` — broader than finding 5.

*Verified by Claude (Opus 4.8) on 2026-06-11. Original findings retained below for traceability.*

---

## Findings Summary Table

| Severity | Category | Component | Description |
| :--- | :--- | :--- | :--- |
| **Critical** | Thread Safety & Stability | Multiple Services: [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb), [GoodsReceivingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/GoodsReceivingService.vb), [PriceChangeService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PriceChangeService.vb), [PurchaseOrderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PurchaseOrderService.vb), [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb), [VendorService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/VendorService.vb) | Class-level state fields are used to store temporary query result lists and records, making services thread-unsafe under concurrent operations. |
| **High** | MVVM Pattern Deviation | ViewModels: [PurchasingDashboardViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/ViewModels/PurchasingDashboardViewModel.vb), [PurchaseOrderListViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb) | ViewModels bypass the service layer to open direct raw database connections (`MySqlConnection`), retrieve connection strings, and run database queries. |
| **High** | Performance & Concurrency | [PurchaseOrderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PurchaseOrderService.vb), [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb), [SequentialNumberGenerator.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Helpers/SequentialNumberGenerator.vb) | PO/GR sequence generation queries all order numbers into memory, causing scalability bottlenecks and concurrency race condition collisions. |
| **Medium** | Auditing & Integrity | [BaseDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/BaseDbContext.vb), Multiple Services | Central SaveChanges interceptor hardcodes `DefaultUser = "Manager"`, silently overwriting the caller-supplied session audit trail on insert and update. |
| **Medium** | Business Logic & Accuracy | [GoodsReceivingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/GoodsReceivingService.vb) | Rounding discrepancies between database VAT-exclusive sales/amounts and serialized event unit costs cause centavo reconciliation mismatches. |
| **Medium** | Stability & Bug | [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb) | Serializing `DateTime` parameters to ISO 8601 strings (`.ToString("o")`) instead of using native dates risks MySQL server format/culture crashes. |
| **Low** | Documentation Smell | [PurchasingDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Data/PurchasingDbContext.vb) | XML documentation comment claims database tables reside in a shared SQLite file, whereas the configurations and services use MySQL. |

---

## Detailed Findings & Proposed Diffs

### 1. Thread-Safety Vulnerabilities via Class-Level State Fields (Critical)

> [!WARNING]
> **Severe Risk of Concurrent Data Corruption and Runtime Exceptions**  
> Storing database query results and entity collection buffers in class-level instance fields inside scoped services introduces severe race conditions. If multiple async tasks are spawned concurrently (e.g., parallel dashboard refreshes or background reorder checking), these fields will overlap and overwrite each other, causing data leakage, concurrency failures, or incorrect return payloads.

#### Root Cause
In [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb#L14-L16):
```vb
        Private ReadOnly _db As PurchasingDbContext
        Private _apList As List(Of AccountsPayableEntry)
        Private _grListForAp As List(Of GoodsReceipt)
```
These fields are initialized and populated directly within the execution of async query methods (e.g. `GetAllOutstandingAsync`, `GetByVendorAsync`, `GetOverdueAsync`, `GetAllAsync`). Under load, concurrent calls to these methods will overwrite `_apList` or `_grListForAp`, causing a thread-safety breach. 

This pattern is repeated in multiple service classes in the purchasing module:
* [GoodsReceivingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/GoodsReceivingService.vb#L25): `_grListForPO`
* [PriceChangeService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PriceChangeService.vb#L14): `_priceAlertList`
* [PurchaseOrderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PurchaseOrderService.vb#L16-L18): `_poList`, `_poLineList`, `_poVendorList`
* [ReorderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/ReorderService.vb#L19-L20): `_reorderSuggestionList`, `_reorderConfigList`
* [VendorService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/VendorService.vb#L14-L15): `_vendorList`, `_grListVendorHistory`

All of these fields are only utilized within the scope of their respective method calls. They should be local variables, not class fields.

#### Proposed Correction (Example for `AccountsPayableService.vb`)
```diff
     Public Class AccountsPayableService
         Implements IAccountsPayableService
 
         Private ReadOnly _db As PurchasingDbContext
-        Private _apList As List(Of AccountsPayableEntry)
-        Private _grListForAp As List(Of GoodsReceipt)
 
         Public Sub New(db As PurchasingDbContext)
```
*(Remove the class fields and define them locally by adding `Dim` inside the respective query methods. Apply this cleanup to all listed services.)*

---

### 2. Database Access inside Presentation Layer / MVVM Violations (High)

> [!CAUTION]
> **Violation of MVVM Separation of Concerns**  
> ViewModels must not directly manage database connections, retrieve connection strings, or execute raw SQL commands. Doing so couples the presentation layer directly to the database engine, prevents clean testing, and degrades maintainability.

#### Root Cause
1. In [PurchasingDashboardViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/ViewModels/PurchasingDashboardViewModel.vb#L388-L410):
   ```vb
   Private Async Function LoadMonthlyTrendAsync(connStr As String) As Task(Of List(Of TrendBarItem))
       Dim trendList As New List(Of TrendBarItem)()
       ...
       Using conn As New MySqlConnection(connStr)
           Await conn.OpenAsync()
           Using cmd = conn.CreateCommand()
               cmd.CommandText = "SELECT YEAR(OrderDate) as Yr, ..."
   ```
   The Dashboard ViewModel directly retrieves the connection string from the EF Core DbContext, opens raw `MySqlConnection` blocks, and performs reader parsing loops.
2. In [PurchaseOrderListViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb#L330-L355):
   ```vb
   ' EF Core 10 VB.NET ToListAsync() silently returns empty for full entity queries.
   ' Load vendors via a fresh MySqlConnection to bypass EF's materializer entirely.
   _vendorList = New List(Of Vendor)()
   Dim connStr = _db.Database.GetConnectionString()
   Using conn As New MySqlConnection(connStr)
       ...
   ```
   Even though `_vendorService` is injected, the ViewModel bypasses it and queries the database using ADO.NET directly. Since `IVendorService.GetAllAsync()` already encapsulates this raw query workaround, this direct database query is completely redundant.

#### Proposed Correction
Move the database queries from `PurchasingDashboardViewModel` into a new dashboard service layer (e.g., `IPurchasingDashboardService`), or place them inside `IPurchaseOrderService`/`IVendorService`. Modify `PurchaseOrderListViewModel` to retrieve vendors cleanly through the service:
```diff
-                Using conn As New MySqlConnection(connStr)
-                    ' ... Raw ADO.NET loading logic ...
-                End Using
+                _vendorList = Await _vendorService.GetAllAsync()
```

---

### 3. Inefficient and Fragile Unique Order Number Generation (High)

> [!WARNING]
> **Performance Bottleneck and Concurrency Save Failures**  
> Loading the entire set of order numbers into memory to compute the next sequential value represents a major scalability hazard. As the database grows to thousands of records, this causes high memory consumption and network latency. Furthermore, it creates a race condition: if two terminals attempt to create a draft PO concurrently, they will get the same max number and one will crash upon saving due to unique index constraints.

#### Root Cause
In [PurchaseOrderService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/PurchaseOrderService.vb#L31-L34):
```vb
            Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
                IgnoreQueryFilters().
                Select(Function(p) p.OrderNumber).
                ToListAsync()
```
(A similar pattern is found in `ReorderService.AcceptSuggestionAsync` and `GoodsReceivingService.ReceiveGoodsAsync`).

#### Proposed Correction
Directly retrieve only the maximum sequence number for the current prefix and year using a server-side query:
```diff
-            Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
-                IgnoreQueryFilters().
-                Select(Function(p) p.OrderNumber).
-                ToListAsync()
-            Dim orderNumber As String = SequentialNumberGenerator.Generate("PO", year, existingNumbers)
+            Dim pattern As String = $"PO-{year}-"
+            Dim maxOrderNumber As String = Await _db.PurchaseOrders.
+                IgnoreQueryFilters().
+                Where(Function(p) p.OrderNumber.StartsWith(pattern)).
+                OrderByDescending(Function(p) p.OrderNumber).
+                Select(Function(p) p.OrderNumber).
+                FirstOrDefaultAsync()
+            
+            Dim maxSeq As Integer = 0
+            If maxOrderNumber IsNot Nothing Then
+                Integer.TryParse(maxOrderNumber.Substring(pattern.Length), maxSeq)
+            End If
+            Dim orderNumber As String = $"{pattern}{(maxSeq + 1).ToString("D4")}"
```

---

### 4. Overwriting of Caller-Supplied Audit Information (Medium)

> [!NOTE]
> **Audit Trail Integrity Smell**  
> The central `BaseDbContext.SaveChangesAsync` interceptor automatically overwrites audit tracking properties (`CreatedBy`, `ModifiedBy`) on all `IAuditable` entities with the hardcoded string `"Manager"`. Any service attempt to audit actions to specific session users (e.g., developers, specific managers, or "System") is lost.

#### Root Cause
In [BaseDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/BaseDbContext.vb#L84-L95):
```vb
                    Case EntityState.Added
                        If TypeOf dbEntry.Entity Is IAuditable Then
                            Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
                            auditable.CreatedAt = now
                            auditable.CreatedBy = DefaultUser
...
```
This forces the creator/modifier to always be `"Manager"`, discarding values explicitly populated in `VendorProductService.AddCatalogEntryAsync` (using `_session.CurrentUsername`) and `GoodsReceivingService.ReceiveGoodsAsync` (using `_session.CurrentUsername` or `"System"`).

#### Proposed Correction
Modify `BaseDbContext.SaveChangesAsync` to respect already-populated audit parameters, or inject the `ISessionService` into the DbContext:
```diff
                     Case EntityState.Added
                         If TypeOf dbEntry.Entity Is IAuditable Then
                             Dim auditable = DirectCast(dbEntry.Entity, IAuditable)
                             auditable.CreatedAt = now
-                            auditable.CreatedBy = DefaultUser
+                            If String.IsNullOrEmpty(auditable.CreatedBy) Then
+                                auditable.CreatedBy = DefaultUser
+                            End If
```

---

### 5. Rounding Mismatch in VAT Event Publication (Medium)

> [!WARNING]
> **Centavo Rounding Discrepancies in Event Subscribers**  
> Publishing a pre-rounded unit cost on VAT events, rather than the exact vatable/input values, causes minor calculation mismatches. When subscribers multiply the rounded unit cost by the quantity, the total will deviate from the physical invoice total by several centavos.

#### Root Cause
In [GoodsReceivingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/GoodsReceivingService.vb#L182-L194):
```vb
            For Each grLine In receipt.Lines
                Dim unitCostExcl As Decimal = If(grLine.VatClassification = VatTreatment.Vatable,
                    Math.Round(grLine.UnitCost / 1.12D, 4),
                    grLine.UnitCost)
                vatEvent.Items.Add(New GoodsReceivedWithVatEvent.GoodsReceivedItemWithVat With {
                    ...
                    .UnitCost = Math.Round(unitCostExcl, 2),
                    .InputVat = grLine.VatAmount
                })
```
If `UnitCost = 10.55` and `QuantityReceived = 100`, the DB records `VatableSales = 941.96` and `VatAmount = 113.04` (Sum = `1055.00`). But the event publishes `.UnitCost = 9.42`, leading a subscriber to calculate `100 * 9.42 = 942.00` (which, with `.InputVat` of `113.04`, equals `1055.04` — a 4-centavo mismatch).

#### Proposed Correction
Pass the exact line-level `VatableSales` and `VatAmount` fields directly in the event, or provide the precise non-rounded unit cost (`unitCostExcl` without rounding to 2 decimal places) so that decimal precision is maintained.

---

### 6. Fragile Date-to-String Parameter Formatting (Medium)

> [!WARNING]
> **Risk of Driver and Culture Failures**  
> Formatting dates as ISO 8601 strings (`.ToString("o")`) when registering database parameters is highly fragile and risks format errors depending on server culture/regional settings.

#### Root Cause
In [AccountsPayableService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Services/AccountsPayableService.vb#L186):
```vb
                    odCmd.Parameters.Add(New MySqlParameter("@today", DateTime.UtcNow.Date.ToString("o")))
```

#### Proposed Correction
Pass the native `.NET DateTime` object directly. The MySQL driver is designed to handle parameters of this type natively:
```diff
-                    odCmd.Parameters.Add(New MySqlParameter("@today", DateTime.UtcNow.Date.ToString("o")))
+                    odCmd.Parameters.Add(New MySqlParameter("@today", DateTime.UtcNow.Date))
```

---

### 7. Contradictory/Stale XML Comments (Low)

#### Root Cause
In [PurchasingDbContext.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Purchasing/Data/PurchasingDbContext.vb#L9-L10):
```vb
    ''' All tables in this context use the <c>Pur_</c> prefix to prevent naming collisions
    ''' with other modules in the shared <c>merchsys.db</c> SQLite file.
```
The codebase uses a MySQL engine (`MySqlConnection`), mapping columns to database-specific properties like `TIMESTAMP(6)` column types. The reference to SQLite is misleading.

#### Proposed Correction
Update the DbContext comments to reflect that the context connects to MySQL/MariaDB.
