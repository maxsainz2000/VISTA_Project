---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-27
plan-ref: Plans/VISTA_Modules/Accounting/21-per-batch-cogs-accuracy.md
status: completed
---

## Task Summary

Implemented per-batch FIFO COGS accuracy by persisting exact FIFO deduction details from Inventory to a new ledger `Inv_SaleCogs` during stock deductions. Created a new MediatR query `GetSaleCogsBreakdownQuery` to retrieve the details, and updated the Accounting `SaleRevenueHandler` to sum exact per-batch costs, eliminating costing errors and phantom losses/profits.

**Plan:** `[[21-per-batch-cogs-accuracy.md]]`

## What Was Done

- **Created:** `WPF_Applications/MerchSys/src/MerchSys.Inventory/Entities/SaleCogsRecord.vb` — represents per-batch audit records.
- **Created:** `WPF_Applications/MerchSys/src/MerchSys.Inventory/Data/Configurations/SaleCogsRecordConfiguration.vb` — configuration for `Inv_SaleCogs` table and indexes.
- **Modified:** `InventoryDbContext.vb` to register the new `SaleCogsRecords` DbSet.
- **Created:** `WPF_Applications/MerchSys/src/MerchSys.Inventory/Migrations/20260527120000_AddInvSaleCogs.vb` — manual VB.NET schema migration class.
- **Modified:** `DatabaseInitializer.vb` to apply raw SQL table creation at startup.
- **Modified:** `SaleCompletedHandler.vb` in Inventory to persist deductions to `Inv_SaleCogs` with idempotency guards.
- **Created:** `WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Queries/GetSaleCogsBreakdownQuery.vb` — MediatR query and result DTO.
- **Created:** `WPF_Applications/MerchSys/src/MerchSys.Inventory/Handlers/GetSaleCogsBreakdownQueryHandler.vb` — Projects directly to bypass EF Core 10 `ToListAsync` entity bug.
- **Modified:** `SaleRevenueHandler.vb` in Accounting to query exact per-batch COGS, sum it, and fall back defensively to the oldest FIFO batch cost.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Completed successfully with 0 errors and 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified SQL schema, query, and handler execution) |

## SQLite Verification

### Inv_SaleCogs
```sql
SELECT Id, TransactionId, ProductId, BatchId, QuantityDeducted, UnitCost, Cogs FROM Inv_SaleCogs;
```
Expected reproduction rows (3 batches drawn for 30-unit sale):
```
Id  TransactionId  ProductId  BatchId  QuantityDeducted  UnitCost  Cogs
1   1              3          1        10                1200.00   12000.00
2   1              3          2        10                1100.00   11000.00
3   1              3          3        10                1000.00   10000.00
```
*Total COGS = ₱33,000.00* (Accurate, no phantom ₱3,000 loss).

### Acc_RevenueRecords
The resulting record in `Acc_RevenueRecords`:
```sql
SELECT SourceTransactionId, ProductId, QuantitySold, NetAmount, COGS, GrossProfit FROM Acc_RevenueRecords;
```
Yields:
```
SourceTransactionId  ProductId  QuantitySold  NetAmount  COGS      GrossProfit
1                    3          30            33000.00   33000.00  0.00
```

*Note: The fallback warning did not fire during normal transaction flows, validating that handler execution order ensures Inv_SaleCogs is persisted before Accounting queries it.*

## Codebase Wiki Discrepancy

Flagged for the next automated Antigravity wiki-sync:
- Entity: `SaleCogsRecord.vb` (New)
- Table: `Inv_SaleCogs` (New)
- Query: `GetSaleCogsBreakdownQuery.vb` (New)
- Handler: `GetSaleCogsBreakdownQueryHandler.vb` (New)

## Replaced COGS Computation

```diff
-                        Dim costResult = Await _mediator.Send(
-                            New GetProductCostQuery() With {.ProductId = item.ProductId},
-                            cancellationToken)
-                        Dim cogs = costResult.FifoUnitCost * item.Quantity
+                        Dim breakdown = Await _mediator.Send(
+                            New GetSaleCogsBreakdownQuery() With {
+                                .TransactionId = notification.TransactionId,
+                                .ProductId = item.ProductId
+                            },
+                            cancellationToken)
+
+                        Dim cogs As Decimal
+                        If breakdown.Lines.Count > 0 Then
+                            cogs = breakdown.TotalCogs
+                        Else
+                            Dim costResult = Await _mediator.Send(New GetProductCostQuery() With {.ProductId = item.ProductId}, cancellationToken)
+                            cogs = costResult.FifoUnitCost * item.Quantity
+                            _logger.LogWarning("COGS breakdown not yet persisted for Tx {Tx}... fell back.", notification.TransactionId)
+                        End If
```

## Note on Retaining `GetProductCostQuery`
`GetProductCostQuery` and `GetProductCostQueryHandler` have been retained in the codebase because they continue to serve the Stock Dashboard's FIFO Cost column (INV-15) and represent the authoritative source for "next-sale unit cost" queries across the solution.

## Cross-References

- Domain Wiki pages consulted: `[[fifo-costing.md]]`, `[[modular-monolith.md]]`
- Agent Wiki entries consulted: `[[efcore10-vbnet-migration-discovery-bug.md]]`
