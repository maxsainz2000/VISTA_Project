---
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-05
plan-ref: Plans/VISTA_Modules/Purchasing/06-ap-tracking.md
status: completed
---

## Task Summary

Implemented the Accounts Payable tracking service layer for the Purchasing module. Provides creation, payment recording, and querying of AP entries against vendor invoices.

**Plan:** `[[06-ap-tracking]]`

## What Was Done

- Created `src/MerchSys.Purchasing/Services/IAccountsPayableService.vb` — interface defining six AP operations
- Created `src/MerchSys.Purchasing/Services/AccountsPayableService.vb` — full implementation

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None. Build succeeded with 0 errors, 0 warnings on first attempt.

## Implementation Notes

**`CreateFromPurchaseOrderAsync`:** Derives `TotalAmount` by loading all `GoodsReceipts` (with lines) for the given PO and summing `QuantityReceived × UnitCost` across all receipt lines — reflecting actual received goods rather than the original PO total. Guards against duplicate AP entries per PO.

**`RecordPaymentAsync`:** Validates `amount > 0` and `amount ≤ Balance` before accumulating. Uses `entry.TotalAmount - entry.AmountPaid` for balance recalculation to avoid floating-point drift. Sets `IsPaid = True` when `Balance = 0`.

**`GetOverdueAsync`:** Captures `DateTime.UtcNow.Date` into a local variable before the LINQ query; this avoids an EF Core translation issue with `.Date` property inside a Where clause.

**`GetTotalOutstandingAsync`:** Guards with `AnyAsync` before calling `SumAsync` to avoid an empty-set sum returning a nullable result.

All query methods include `Vendor` and `PurchaseOrder` navigation via `Include` so callers get fully populated entities.

## What's Next

- [ ] PUR-07 and beyond (reorder engine, ViewModels)
- [ ] DI registration of `IAccountsPayableService` / `AccountsPayableService` in `MerchSys.App` (deferred to a consolidation plan)

## Cross-References

- Domain Wiki pages consulted: `entities/module-purchasing.md`, `sources/purchasing-module-paper.md`
- Agent Wiki entries consulted: `[[vbnet-lambda-param-shadows-local-variable]]`, `[[vbnet-leading-dot-fluent-chains]]`
