---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/07-shrinkage-recording.md
status: completed
---

## Task Summary

Implemented inventory shrinkage recording — damage, spoilage, expiry write-off, and administrative discrepancies. Each record captures the financial impact and publishes a `ShrinkageRecordedEvent` to Accounting via MediatR.

**Plan:** `[[07-shrinkage-recording]]`

## What Was Done

- Created `src/MerchSys.Inventory/Services/IShrinkageService.vb` — defines `IShrinkageService` with `RecordShrinkageAsync`, `GetShrinkageHistoryAsync`, and `GetTotalShrinkageValueAsync`
- Created `src/MerchSys.Inventory/Services/ShrinkageService.vb` — full implementation with batch-targeted or FIFO deduction, reason validation, MediatR event publish, and audit logging

## Architecture Notes

### Deduction Strategy
`RecordShrinkageAsync` supports two deduction paths:
1. **Batch-targeted** (`batchId` provided): deducts from the specified `StockBatch` directly. Throws if batch not found for product or if batch has insufficient remaining stock.
2. **FIFO** (`batchId = Nothing`): queries all batches with `QuantityRemaining > 0` ordered by `ReceiptDate` ascending. Unlike `StockService.DeductStockFIFOAsync`, this does **not** exclude expired batches — shrinkage may legitimately target already-expired stock (e.g., recording damage discovered on expired units before a formal write-off).

### Multi-Batch FIFO and `ShrinkageRecord` Per Batch
When FIFO spans multiple batches, one `ShrinkageRecord` is created per batch portion consumed (consistent with `ExpiryTrackingService.WriteOffExpiredBatchAsync`). The method returns the first record created. History queries return all records.

### MediatR Event Aggregation
A single `ShrinkageRecordedEvent` is published per `RecordShrinkageAsync` call regardless of how many batches were consumed. For multi-batch FIFO, `UnitCost` is the weighted average (`TotalValue / TotalQty`) and `QuantityLost`/`TotalValue` are the aggregated totals. This gives Accounting one atomic event per staff-initiated shrinkage action.

### Reason Validation
Allowed values enforced via a module-level `ValidReasons` array: `"Damage"`, `"Spoilage"`, `"Expiry"`, `"Admin Error"`. `ArgumentException` is thrown for any other value.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None.

## What's Next

- Register `IShrinkageService` / `ShrinkageService` in `MerchSys.App` DI (Scoped)
- Wire `CheckAndGenerateAlertsAsync` call into a `ShrinkageRecordedEvent` handler (INV-06 note)
- Implement Accounting handler for `ShrinkageRecordedEvent`

## Cross-References

- Domain Wiki pages consulted: `[[inventory-module-paper]]`, `[[cross-module-data-flow]]`
- Agent Wiki entries consulted: none
