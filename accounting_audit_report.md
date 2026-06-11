# MerchSys.Accounting Codebase Audit Report

This audit report identifies critical issues, optimizations, and improvements in the `MerchSys.Accounting` module. The findings are categorized by severity and include direct code references, root cause analysis, and actionable remediation steps.

## ✅ Verification Addendum — 2026-06-11

Verified against source under `WPF_Applications/MerchSys/src/MerchSys.Accounting/`. Code quotes match, but two of the three findings need re-rating. **Where this addendum conflicts with a finding's severity or "Recommended Resolution," the addendum governs.**

> ⚠️ **Landmine — keep the raw ADO.NET.** The `MySqlConnection` reader loops here are the documented workaround for the EF Core 10 + VB.NET full-entity `ToListAsync()` empty-result bug (`LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`). Do not "modernise" them into `.Where(...)/.Include(...).ToListAsync()` — they return empty lists at runtime. Scalar/anonymous projections are safe.

| # | Reported | Verified verdict & action |
|---|---|---|
| 1 Class-level shared state | Critical | **Low (cleanup).** The fields are per-process — **not** shared across the 4 client laptops (separate processes). Services are root-resolved `Scoped` (effective singletons) but serialized by a non-reentrant DbContext, so no field race manifests. Field→local is safe (already proven by `CartService.GetTransactionHistoryPageAsync`). Cleanup, not a concurrency fix. |
| 2 SQLite `TEXT`/`INTEGER` + ISO dates | High | **Confirmed (High).** `TamperAuditEntry.Id`/`ReceiptId` are `Int64` mapped to `INTEGER`; `DetectedAt` is `TEXT` and queried with ISO-`"o"` strings (`ITamperAuditQueryService:49-50`) while the sibling `CountByKindAsync` uses native EF `DateTime`. Remove the `HasColumnType` overrides; pass native `DateTime` params. (ID-truncation scale risk is negligible here; the date-range correctness risk is the driver.) |
| 3 Manual connection management | Medium | **Skip (incorrect rationale).** These are read-only `SELECT`s with no open transaction; `SaveChanges` interceptors never run on reads. Only "extra pooled connection" is true (negligible), and the proposed reuse can raise "connection already in use." No change recommended. |

**Missed by this report:** (a) the ISO-date param anti-pattern (finding 2) is **systemic across every module** (~30 sites) — fix as one cross-module pass; (b) `FinancialOverviewViewModel` starts an AutoReset `System.Timers.Timer` without implementing `IDisposable` — the same VM/timer leak flagged in the Inventory report (INV-2), missed here.

*Verified by Claude (Opus 4.8) on 2026-06-11. Original findings retained below for traceability.*

---

## 1. [CRITICAL] Concurrency Risk: Class-Level Shared State in Async Services

### Affected Files
* [VatReportingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReportingService.vb) (Lines 28–30, 230, 376–377)
* [ITamperAuditQueryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/ITamperAuditQueryService.vb) (Line 27, 38)

### Problem Description
Both `VatReportingService` and `TamperAuditQueryService` are registered in the Dependency Injection container and contain stateful, class-level private fields to temporarily store collection query results during execution:
```vb
' VatReportingService.vb
Private _vatReturnList As List(Of VatReturn)
Private _revenueRecordList As List(Of RevenueRecord)
Private _expenseRecordList As List(Of ExpenseRecord)

' ITamperAuditQueryService.vb (TamperAuditQueryService class)
Private _tamperAuditList As List(Of TamperAuditEntry)
```
Inside their asynchronous query methods (e.g. `ListReturnsAsync`, `CollectLedgerDataAsync`, and `GetIncidentsAsync`), these fields are initialized (`New List(Of ...)`), populated via ADO.NET reader loops, and returned to the caller. 

Because service instances can be invoked concurrently (e.g., from overlapping asynchronous UI actions, concurrent background tasks, or parallel unit test suites), multiple threads executing these methods simultaneously will overwrite these class-level fields. This introduces race conditions, cross-thread data contamination (e.g., one user's query returns another user's records), or null reference crashes.

### Root Cause
These class-level fields were introduced as a misapplied workaround for the VB.NET + EF Core 10 `ToListAsync` materialization bug (documented in `efcore-vbnet-tolistasync-entity-empty.md`). The workaround prescribes writing results to a class field instead of a local variable when calling `ToListAsync` to prevent async state-machine lifter errors. 

However, since these methods bypass EF Core's materializer entirely by using **raw ADO.NET SQL commands**, they are **not affected** by the `ToListAsync` bug. Using class-level fields here is completely unnecessary and introduces severe concurrency vulnerabilities.

### Recommended Resolution
Remove the private fields from the class definitions and declare them as local variables within the respective methods. Since no `Await` statements are executed after the list population, there is no threat from the async compiler lifting issue.

```diff
' In VatReportingService.vb:
- Private _vatReturnList As List(Of VatReturn)
- Private _revenueRecordList As List(Of RevenueRecord)
- Private _expenseRecordList As List(Of ExpenseRecord)

  Public Async Function ListReturnsAsync(year As Integer?) As Task(Of IReadOnlyList(Of VatReturn)) _
      Implements IVatReportingService.ListReturnsAsync

+     Dim vatReturnList As New List(Of VatReturn)()
-     _vatReturnList = New List(Of VatReturn)()
      ...
-     Return _vatReturnList.AsReadOnly()
+     Return vatReturnList.AsReadOnly()
  End Function
```

---

## 2. [HIGH] Date-Time Query Mismatch & Database Performance Issue (SQLite Leftover Configurations)

### Affected Files
* [TamperAuditEntryConfiguration.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Data/Configurations/TamperAuditEntryConfiguration.vb) (Lines 15–28)
* [ITamperAuditQueryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/ITamperAuditQueryService.vb) (Lines 49–50)
* [VatReportingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReportingService.vb) (Lines 378–379)
* [VatReliefReportService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReliefReportService.vb) (Lines 50–51)

### Problem Description
1. **Failing Date Queries:** In `TamperAuditQueryService.GetIncidentsAsync`, `VatReportingService.CollectLedgerDataAsync`, and `VatReliefReportService.BuildSummaryAsync`, `DateTime` parameters are converted to ISO 8601 strings with the `"o"` format (e.g., `"2026-06-11T18:27:41.0000000Z"`) before binding to the SQL query. However, standard MySQL dates use `"yyyy-MM-dd HH:mm:ss.ffffff"`.
   * Specifically for `TamperAuditLog`, the `DetectedAt` column is mapped explicitly as `HasColumnType("TEXT")`. Because the formats differ (ISO 8601 string vs MySQL default string date representation), string comparisons like `DetectedAt >= @fromUtc` behave unpredictably or fail to match records, resulting in empty returns.
   * For the other ledgers, comparing standard MySQL `DATETIME` columns with ISO 8601 strings containing `'T'` and `'Z'` characters causes driver-level parsing warnings or comparison failures.
2. **Primary Key/Foreign Key Truncation:** In `TamperAuditEntryConfiguration`, the entity's 64-bit `Long` properties (`Id` and `ReceiptId`) are mapped explicitly as `HasColumnType("INTEGER")`. In MySQL, `INTEGER` limits the value to 2.14 billion, leading to insert crashes when transaction volumes scale, even though the entity design expects `BIGINT`.
3. **Index/Performance Degradation:** Mapping string fields like `ReceiptNumber` and `TamperKind` as `TEXT` in MySQL ignores the `.HasMaxLength(50)` constraint, disables indexing optimizations, and degrades search performance on the composite index `IX_Acc_TamperAuditLog_DetectedAt_TamperKind`.

### Root Cause
These overrides are SQLite leftovers from initial development. MySQL/MariaDB database mappings were not fully normalized when the database architecture shifted.

### Recommended Resolution
1. Remove the SQLite-specific `HasColumnType("TEXT")` and `HasColumnType("INTEGER")` overrides from [TamperAuditEntryConfiguration.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Data/Configurations/TamperAuditEntryConfiguration.vb).
2. Pass native `DateTime` objects as command parameter values instead of formatted ISO strings:
```vb
' ITamperAuditQueryService.vb, VatReportingService.vb, VatReliefReportService.vb
giCmd.Parameters.Add(New MySqlParameter("@fromUtc", fromUtc))
giCmd.Parameters.Add(New MySqlParameter("@toUtc", toUtc))
```

---

## 3. [MEDIUM] Transaction & Interceptor Bypass: Direct Connection Management

### Affected Files
* [VatReportingService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReportingService.vb) (Lines 231–233, 380–382)
* [VatReliefReportService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/VatReliefReportService.vb) (Lines 52, 66–67)
* [ITamperAuditQueryService.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.Accounting/Services/ITamperAuditQueryService.vb) (Lines 39–41)

### Problem Description
To query raw SQL without triggering the EF Core materialization bug, these services extract the connection string and open new connection objects:
```vb
Dim ldConnStr = _db.Database.GetConnectionString()
Using ldConn As New MySqlConnection(ldConnStr)
    Await ldConn.OpenAsync()
```
Creating a completely separate connection:
1. **Bypasses Transactions:** If the current unit-of-work has an active transaction open on the DbContext, manual ADO.NET queries will run on a separate connection outside that transaction. This results in visual inconsistencies and isolation leaks.
2. **Bypasses Interceptors:** Multi-tenant routes, connection routing, or interceptors configured on the DbContext are bypassed.
3. **Connection Pool Overhead:** Establishing a separate connection adds resource overhead compared to reusing the active connection.

### Recommended Resolution
Reuse the DbContext's active database connection by casting it to `MySqlConnection`. Safely manage the connection state:

```vb
Dim conn = DirectCast(_db.Database.GetDbConnection(), MySqlConnector.MySqlConnection)
Dim openedHere = False
If conn.State <> ConnectionState.Open Then
    Await conn.OpenAsync()
    openedHere = True
End If
Try
    ' Execute commands using conn ...
Finally
    If openedHere Then
        Await conn.CloseAsync()
    End If
End Try
```
This ensures raw SQL commands execute within the active DbContext transaction scope.

---

## Summary of Findings

| Finding | Severity | Impact | Fix Confidence |
| :--- | :--- | :--- | :--- |
| **Class-level Shared State in Async Services** | **Critical** | Concurrency race conditions, data leaking between parallel requests, or crashes. | 100% |
| **Date-Time Format & Type Overrides Inconsistency** | **High** | Date-range queries fail to match records, ID truncation limits scale, index degradation. | 100% |
| **Manual Connection Management** | **Medium** | Bypasses transactions and EF DbContext interceptors; overhead on connection pool. | 100% |
