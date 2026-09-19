---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-27
plan-ref: Plans/VISTA_Modules/Accounting/22-revenue-record-consolidation.md
status: completed
---

## Task Summary

Consolidated writing of `Acc_RevenueRecords` to eliminate the duplicate writer race condition between `SaleCompletedAccountingHandler` (legacy) and `SaleCompletedWithVatHandler` (VAT). The legacy handler has been deleted, and the VAT handler was renamed to `SaleRevenueHandler` and modified to act as the single authoritative writer for revenue and COGS expenses for POS transactions, preserving proper idempotency and VAT handling.

**Plan:** `[[22-revenue-record-consolidation.md]]`

## What Was Done

- **Deleted:** `WPF_Applications/MerchSys/src/MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` — legacy duplicate writer. Grep searches confirm no other source file references it.
- **Created:** `WPF_Applications/MerchSys/src/MerchSys.Accounting/Handlers/SaleRevenueHandler.vb` (Renamed and modified from `SaleCompletedWithVatHandler.vb`) — consolidated authoritative writer.
- **Modified:** `SaleRevenueHandler.vb` to absorb COGS `ExpenseRecord` writes, including strict idempotency checks keyed by `(Category="COGS", SourceModule="POS", SourceReferenceId=TransactionId)` and description patterns to ensure safety on POS transaction re-publishes.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Completed successfully with 0 errors and 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified namespace scanning automatically resolves MediatR handler rename/removal) |

## SQLite Verification (Before/After)

- Before plan implementation, fresh test sales created duplicate `Acc_RevenueRecords` rows (one from legacy handler and one from VAT handler's `Else` branch).
- Running the query post-plan:
```sql
SELECT SourceTransactionId, ProductId, COUNT(*) FROM Acc_RevenueRecords GROUP BY SourceTransactionId, ProductId HAVING COUNT(*) > 1;
```
Yields: `0 rows returned` (zero duplicates).

## Codebase Wiki Discrepancy

Flagged for the next automated Antigravity wiki-sync to update `LLM_Wiki/codebase_wiki/modules/accounting/handlers.md`:
- `SaleCompletedAccountingHandler` (Deleted)
- `SaleCompletedWithVatHandler` (Deleted/Renamed)
- `SaleRevenueHandler` (New Handler)

## ACC-21 Handoff

The per-batch COGS logic in `SaleRevenueHandler.vb` has been updated under ACC-21 to query `GetSaleCogsBreakdownQuery` instead of the old FIFO cost approximation. The code block from `ACC-22` handoff was resolved during the subsequent step:
```vb
' Query the exact per-batch FIFO COGS breakdown from Inventory
Dim breakdown = Await _mediator.Send(
    New GetSaleCogsBreakdownQuery() With {
        .TransactionId = notification.TransactionId,
        .ProductId = item.ProductId
    },
    cancellationToken)
```

## Cross-References

- Domain Wiki pages consulted: `[[modular-monolith.md]]`, `[[bir-compliance.md]]`
- Agent Wiki entries consulted: `[[efcore10-vbnet-migration-discovery-bug.md]]`
