---
module: MerchSys.Integration
plan-id: INT-15
title: "ToListAsync remediation — Inventory + POS service methods"
depends-on: [INT-14]
estimated-files: 10
priority: critical
---

# INT-15: ToListAsync remediation — Inventory + POS service methods

## Context

The EF Core 10 + VB.NET bug documented at `agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` causes full-entity `ToListAsync()` queries to silently return empty lists at runtime — no exception, correct SQL, zero rows. The bug was first hit on `VendorService.GetAllAsync` and resolved there with a raw `SqliteConnection` workaround.

INT-14's triage checklist (`Operator/debug-logs/tolistasync-remediation-checklist.md`) enumerates every still-broken site. This plan resolves the Inventory and POS rows of that checklist.

**Why critical.** The affected methods back user-visible list screens — Stock Dashboard, Credit Account list, Cart history, Sales Returns, Expiry Monitor, Shrinkage history. In production today, those screens render empty even when the underlying tables contain rows. The defect is silent: the user assumes there is no data.

## Prerequisites

- **INT-14** — produces the authoritative checklist this plan consumes.
- **INFRA-18** — needed transitively via INT-14, and re-used at acceptance time to confirm the fixed sites no longer trigger Rule 3.

## Wiki References

- `agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` — the canonical fix shape (raw `SqliteConnection` + `reader.Read()` writing to a class field)
- `Operator/debug-logs/tolistasync-remediation-checklist.md` — scope (INT-15 rows only)
- The existing Vendor fix (commit referenced in the checklist) — the working precedent

## Deliverables

The exact file list comes from INT-14's checklist. Based on the verified true positives in `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md` section 2 Rule 3, the expected Inventory + POS surface is:

```
MerchSys.Inventory/Services/StockService.vb                ' Modified — GetCurrentStockAsync, GetStockBatchesAsync, and any sibling full-entity list
MerchSys.Inventory/Services/ExpiryTrackingService.vb       ' Modified
MerchSys.Inventory/Services/LowStockAlertService.vb        ' Modified
MerchSys.Inventory/Services/ShrinkageService.vb            ' Modified
MerchSys.Inventory/Services/InventoryAuditService.vb       ' Modified
MerchSys.Inventory/Services/VelocityService.vb             ' Modified — ClassifyAllProductsAsync (Include graph — see Implementation Notes)
MerchSys.POS/Services/CartService.vb                       ' Modified — GetTransactionHistoryAsync (full Transaction entity list)
MerchSys.POS/Services/CreditService.vb                     ' Modified — GetAllAccountsAsync, SearchAccountsAsync, plus the credit history calls
MerchSys.POS/Services/SalesReturnService.vb                ' Modified
MerchSys.POS/Services/ReceiptIntegrityService.vb           ' Modified
MerchSys.POS/Services/Archival/ReceiptArchivalService.vb   ' Modified — archival selection query
MerchSys.POS/Services/DailySummaryService.vb               ' Modified — only the full-entity reads; the anonymous projections are out of scope
MerchSys.POS/ViewModels/CreditManagementViewModel.vb       ' Modified — only if the call is a full-entity read (verify against checklist)
MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb ' Modified — same caveat
```

If a file listed above is not in INT-14's checklist, do not touch it. If a file is in the checklist but not listed above, treat the checklist as authoritative.

## Specification

### Per-method fix shape

For every row in INT-14's checklist assigned to INT-15 with `Fix complexity = simple` or `joined`, replicate the established Vendor pattern verbatim:

```vb
' Before — broken
Public Async Function GetSomethingAsync() As Task(Of List(Of T)) Implements I.GetSomethingAsync
    Return Await _db.SomethingSet _
        .Where(...) _
        .OrderBy(...) _
        .ToListAsync()
End Function

' After — fixed
Private _somethingList As List(Of T)

Public Async Function GetSomethingAsync() As Task(Of List(Of T)) Implements I.GetSomethingAsync
    _somethingList = New List(Of T)()
    Dim connStr = _db.Database.GetConnectionString()
    Using conn As New SqliteConnection(connStr)
        Await conn.OpenAsync()
        Using cmd = conn.CreateCommand()
            cmd.CommandText = "SELECT col1, col2, ... FROM <Pur_/Inv_/Pos_/Acc_>Something " &
                              "WHERE <WHERE-equivalent> ORDER BY <ORDER-equivalent>"
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    _somethingList.Add(New T With {
                        .Col1 = reader.GetInt32(0),
                        .Col2 = If(reader.IsDBNull(1), Nothing, reader.GetString(1)),
                        ...
                    })
                End While
            End Using
        End Using
    End Using
    Return _somethingList
End Function
```

The four non-negotiables from the wiki entry:

1. Fresh `New SqliteConnection(connStr)` — not `_db.Database.GetDbConnection()`.
2. Synchronous `cmd.ExecuteReader()` + `reader.Read()` — not the `Async` variants.
3. Results written to a **class field**, not a local `Dim x As New List(Of T)`.
4. `Imports Microsoft.Data.Sqlite` added if not already present.

### Per-method fix shape — `Include` graphs

For rows marked `Fix complexity = graph`, the same raw-`SqliteConnection` workaround applies, but the body must:

1. Issue one SQL statement per included relationship (or one JOIN statement).
2. Build the parent entities first, indexed by primary key in a `Dictionary(Of Integer, T)`.
3. Iterate the child rows and assign them into the parent's navigation collection.

Specific known graph methods:

- `VelocityService.ClassifyAllProductsAsync` — Products + `Include(Category)` + `Include(StockBatches)` + `Include(ShrinkageRecords)`. Split into four SELECTs: products, categories (lookup by FK), batches, shrinkage records. Reassemble.
- `StockService.GetCurrentStockAsync` — Products + `Include(StockBatches)`. Two SELECTs.

If a `graph` method's fix exceeds ~80 lines of SQL plumbing, stop and **defer it to a follow-up plan** — record the deferral in this plan's summary with a justification.

### Out of scope

- Anonymous-type projections, scalar projections, `GroupBy + Select(New With { ... })` chains. The wiki excludes these. INT-14's checklist must already have filtered them out; double-check by reading the chain before rewriting.
- `DailySummaryService` projection queries — these are out of scope. Only the full-entity reads in `DailySummaryService` (if any) belong here.
- Anything outside Inventory/POS modules — that is INT-16.

### Verification

For each modified method, perform one smoke check by running the app and exercising the screen that consumes it. Confirm at least one row renders where the underlying table has rows. Record the result inline in INT-14's checklist (`Fix applied = yes`, `Verified = yes/<screen name>`).

If a method cannot be exercised because its screen is not yet wired (e.g., archival background job), a SQL-level verification suffices: query `merchsys.db` directly with `sqlite3` to confirm rows exist, then add a one-liner Debug.WriteLine inside the fixed method (gated by `#If DEBUG`) to confirm the list count at runtime. Remove the Debug.WriteLine before commit.

## Implementation Notes

- **Class-field naming.** Use `_<entityName>List` — `_vendorList`, `_stockBatchList`, `_creditAccountList`. Match the existing Vendor fix exactly.
- **DI scope.** The class fields hold the most recent query result for the lifetime of the service instance. Service registrations in `MerchSys.App` are scoped per request — fine. If you discover a service registered `Singleton`, change the registration to `Scoped` rather than refactoring the fix shape. The wiki entry is explicit that the class-field pattern is required, not optional.
- **VAT/audit columns.** Several flagged entities have `IsDeleted`, `CreatedAt`, `ModifiedAt` columns. Include the same `WHERE IsDeleted = 0` filter the original EF query used. Do not silently broaden the query.
- **Transactions.** None of these methods write data; they only read. Do not wrap the SQLite connection in a transaction.
- **Parameterisation.** Any predicate value that came from a method parameter (e.g., `searchTerm`, `customerId`) must be passed as a `SqliteParameter`, never concatenated into the `CommandText`. The wiki Vendor example used a static query; search variants do not — add parameters explicitly.

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` — 0 errors, 0 warnings.
2. Every Inventory + POS row in `tolistasync-remediation-checklist.md` has `Fix applied = yes` and `Verified = <screen or sqlite check>`.
3. Re-running the INFRA-18 corrected Rule 3 detector against the live codebase produces zero hits in `MerchSys.Inventory.*` and `MerchSys.POS.*` files for sites in this batch.
4. Stock Dashboard, Credit Account list, Cart Transaction History, Sales Returns list, Expiry Monitor, and Shrinkage list all render rows when the underlying tables contain rows.
5. Any `Fix complexity = graph` deferral is recorded in the summary with the file, method, line, and a one-paragraph justification for deferring to a follow-up plan.
6. No method in this batch had its `WHERE` clause silently broadened or narrowed during the rewrite.

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-15-summary.md` using `Progress/_template.md`. Include:

- The updated checklist diff (which rows flipped to `Fix applied = yes`).
- For each `graph` method: the SQL JOIN strategy used, plus the parent-key dictionary scheme.
- Any deferred rows, with the follow-up plan ID once that plan exists.
- The before/after Rule 3 hit counts for Inventory + POS modules.

## Post-Completion Notes

If verification surfaces any flagged method whose underlying screen has wider runtime bugs (DI gaps, missing migrations), record those in this summary and create follow-up plans rather than expanding INT-15's scope.
