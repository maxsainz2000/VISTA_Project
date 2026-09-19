---
module: MerchSys.Integration
plan-id: INT-20
title: "Service field→local cleanup + small correctness fixes"
depends-on: [INT-18, INT-19]
estimated-files: 14
priority: medium
---

# INT-20: Service field→local cleanup + small correctness fixes

## Context

This batch clears the low-risk hygiene findings verified in **INT-18**: the class-level query-buffer fields (re-rated from "Critical concurrency" to cleanup — see INT-18 fact #2), plus four small, independent correctness fixes that do not warrant their own plans.

None of these change query mechanics or touch the `ToListAsync` workaround. The field→local change is proven safe by `CartService.GetTransactionHistoryPageAsync`, which already uses a local `Dim` list declared before an `Await`.

Sequence **after INT-19** so the date-parameter edits land first; the field→local pass is then the last edit per service file.

## Prerequisites

- **INT-18** — remediation index.
- **INT-19** — native-DateTime params (shares several service files).

## Wiki References

- `LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` — note the "write to a class field" guidance there is about EF materialisation; it does **not** require these raw-ADO buffers to be fields. Local lists populated by a synchronous `reader.Read()` loop and returned immediately are safe (see `CartService.GetTransactionHistoryPageAsync`).
- `CLAUDE.md` — testing-phase rules.

## Deliverables

```
' --- Part A: query-buffer field -> local ---
MerchSys.Accounting/Services/VatReportingService.vb            ' Modified — _vatReturnList, _revenueRecordList, _expenseRecordList
MerchSys.Accounting/Services/ITamperAuditQueryService.vb       ' Modified — _tamperAuditList
MerchSys.Inventory/Services/StockService.vb                    ' Modified — _batchesForFIFO, _batchesForProduct, _productStockList (+ Part E)
MerchSys.Inventory/Services/ShrinkageService.vb                ' Modified — _batchesForShrinkage, _shrinkageHistoryList
MerchSys.Inventory/Services/ExpiryTrackingService.vb           ' Modified — _nearExpiryBatchList, _expiredBatchList
MerchSys.Inventory/Services/LowStockAlertService.vb            ' Modified — _alertConfigList
MerchSys.Inventory/Services/InventoryAuditService.vb           ' Modified — _auditHistoryList
MerchSys.Inventory/Handlers/GetProductCatalogQueryHandler.vb   ' Modified — _catalogProductList
MerchSys.POS/Services/CartService.vb                           ' Modified — _txHistoryList
MerchSys.POS/Services/ReceiptIntegrityService.vb              ' Modified — _integrityChainList
MerchSys.POS/Services/CreditService.vb                         ' Modified — _creditAccountList
MerchSys.POS/Services/SalesReturnService.vb                    ' Modified — buffer field(s)
MerchSys.POS/Services/DailySummaryService.vb                   ' Modified — buffer field(s)
MerchSys.Purchasing/Services/AccountsPayableService.vb         ' Modified — _apList, _grListForAp
MerchSys.Purchasing/Services/GoodsReceivingService.vb          ' Modified — _grListForPO
MerchSys.Purchasing/Services/PriceChangeService.vb             ' Modified — _priceAlertList
MerchSys.Purchasing/Services/PurchaseOrderService.vb           ' Modified — _poList, _poLineList, _poVendorList
MerchSys.Purchasing/Services/ReorderService.vb                 ' Modified — _reorderSuggestionList, _reorderConfigList
MerchSys.Purchasing/Services/VendorService.vb                  ' Modified — _vendorList, _grListVendorHistory

' --- Part B: VatConfigurationLoader lock ---
MerchSys.POS/Services/VatConfigurationLoader.vb                ' Modified — Invalidate() lock primitive

' --- Part C: ConnectionHealthMonitor CTS dispose ---
MerchSys.App/Services/ConnectionHealthMonitor.vb               ' Modified — [Stop]/Dispose

' --- Part D: stale SQLite doc comments ---
MerchSys.POS/Data/POSDbContext.vb                              ' Modified — comment
MerchSys.Inventory/Data/InventoryDbContext.vb                  ' Modified — comment
MerchSys.Purchasing/Data/PurchasingDbContext.vb                ' Modified — comment
MerchSys.Inventory/Handlers/GetProductsForCatalogQueryHandler.vb ' Modified — comment
```

(Use the actual field names present in each file; the list above reflects the audit reports and may include a field that was already removed — skip any that no longer exist and note it.)

## Specification

### Part A — query-buffer field → local

For each listed private `List(Of T)` field that is **only** assigned (`= New List(...)`), populated by a `reader.Read()` loop, and returned within a single method:

1. Delete the field declaration.
2. At the assignment site inside the method, declare it as a local: `Dim xList As New List(Of T)()`.
3. Update the in-method references and the `Return`.

Constraints:

- **Do not** touch `CartService._carts` (a deliberate `Shared ReadOnly ConcurrentDictionary` — process-wide cart store, correctly synchronised).
- **Do not** alter the raw ADO.NET reader loops, SELECT text, or the `_db`/`_context` fields.
- Where a method already copies the field to a local before use (e.g., `StockService.DeductStockFIFOAsync` does `Dim batches = _batchesForFIFO`), collapse to a single local.
- A field used by **more than one** method (rare) must become a local in **each** method, not be shared.

### Part B — `VatConfigurationLoader.Invalidate()` lock

`GetAsync()` guards the cache with `Await _lock.WaitAsync()` (a `SemaphoreSlim`), but `Invalidate()` uses `SyncLock _lock` (a monitor lock on the semaphore object) — the two do not mutually exclude. Make `Invalidate()` use the same semaphore:

```vb
Public Sub Invalidate()
    _lock.Wait()
    Try
        _cached = Nothing
    Finally
        _lock.Release()
    End Try
End Sub
```

### Part C — `ConnectionHealthMonitor` CTS disposal

`[Stop]()` sets `_cts = Nothing` before `Dispose()` runs, so `_cts?.Dispose()` is a no-op and the `CancellationTokenSource` is never disposed. Dispose inside `[Stop]`:

```vb
Public Sub [Stop]() Implements IConnectionHealthMonitor.[Stop]
    If _cts IsNot Nothing Then
        _cts.Cancel()
        _cts.Dispose()
        _cts = Nothing
    End If
End Sub

Public Sub Dispose() Implements IDisposable.Dispose
    [Stop]()
    _retrySignal.Dispose()
End Sub
```

### Part D — stale SQLite doc comments

Update XML/inline comments that describe the data store as a "shared `merchsys.db` SQLite file" or "SQLite database" to reference the centralized MariaDB instance. Comment-only edits — no code. (Leave comments inside the legacy SQLite/sync scaffolding scheduled for removal in INFRA-23→30; scope here is the four files listed.)

### Part E — FIFO `ModifiedBy` (the one valid INV-4 sub-point)

In `StockService.DeductStockFIFOAsync`, the raw `UPDATE Inv_StockBatches SET QuantityRemaining=@qty, ModifiedAt=@now ...` sets `ModifiedAt` but not `ModifiedBy`. Add `ModifiedBy = @modifiedBy` to the `UPDATE` and bind a value. Use `"System"` if no session user is readily available in this service (the FIFO path runs under the sale pipeline); do **not** introduce a new session dependency here if it complicates the constructor — `"System"` is acceptable and consistent with other batch-write paths. Keep the rest of the FIFO raw-ADO + `FOR UPDATE` path exactly as is.

## Implementation Notes

- This plan is mechanical. The only behavioural changes are Parts B, C, and E; Parts A and D are non-behavioural.
- Build per file (`dotnet build MerchSys.slnx`). Per `CLAUDE.md`, document any non-trivial build error in the summary and stop rather than deep-troubleshooting.
- A removed field may still be referenced by a `Friend`/`Shared` helper — grep each field name across the file before deleting to be sure all references move into the method.

## Acceptance Criteria

1. Every listed query-buffer field is gone; each affected method uses a local list and returns it.
2. `CartService._carts` is untouched.
3. `VatConfigurationLoader.Invalidate()` uses `_lock.Wait()/Release()`; `ConnectionHealthMonitor` disposes its CTS inside `[Stop]`.
4. The four stale SQLite comments now reference MariaDB.
5. `StockService` FIFO `UPDATE` sets `ModifiedBy`; the `FOR UPDATE` transaction is otherwise unchanged.
6. No raw SELECT text or query logic changed; no EF `ToListAsync` introduced.
7. `dotnet build MerchSys.slnx` — target 0 errors, 0 warnings (or documented per protocol).

## Output Requirements

Create a progress report at `Progress/VISTA_Modules/Integration/INT-20-summary.md` using `Progress/_template.md`. List the fields converted per file, note any field that no longer existed, and record the Part B/C/E behavioural changes.
