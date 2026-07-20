---
type: error-fix
module: Infrastructure
agent: antigravity
date: 2026-07-20
tags: [ef-core, mariadb, migration, sequence]
error-code: DbUpdateException
severity: runtime-error
---

# Unseeded Sequence Tables

## Problem

The POS Sales Cart and Purchasing Order flows failed completely on checkout/finalization with `DbUpdateException` (`Duplicate entry 'TX-2026-0001' for key 'IX_Pos_SalesTransactions_TransactionNumber'`). The application successfully committed a new sequence number but threw a fatal error attempting to save the actual transaction or purchase order.

## Root Cause

In INT-22, the system's generation logic for transaction numbers and purchase orders was hardened to use concurrency-safe sequence tables (`Pos_TransactionSequences` and `Pur_OrderSequences`), implemented via `0007_sequence_tables.sql`.

However, migration 0007 only created the tables; it did not seed them with the current max values from the existing live tables (`Pos_SalesTransactions` and `Pur_PurchaseOrders`). 

Because they were empty, `CartService.GenerateTransactionNumberAsync` initialized `NextValue` to `1`. The service successfully committed `TX-2026-0001` in its own inner transaction, but `FinalizeAsync` failed to insert the transaction into `Pos_SalesTransactions` because `TX-2026-0001` already existed from prior usage. This caused a "catch-up" scenario where the user would experience exactly N checkout crashes (where N is the number of existing transactions) before the sequence generator finally surpassed the existing records.

## Fix

A new forward migration `0009_seed_sequence_tables.sql` was created to dynamically seed both sequence tables with the current max sequence numbers from their respective live tables using an `ON DUPLICATE KEY UPDATE` script:

```sql
INSERT INTO `Pos_TransactionSequences` (`Year`, `NextValue`, `CreatedBy`, `CreatedAt`, `RowVersion`)
SELECT 
    YEAR(CURRENT_DATE),
    COALESCE(MAX(CAST(SUBSTRING_INDEX(`TransactionNumber`, '-', -1) AS UNSIGNED)), 0),
    'System', 
    UTC_TIMESTAMP(6),
    CURRENT_TIMESTAMP(6)
FROM `Pos_SalesTransactions`
WHERE YEAR(`TransactionDate`) = YEAR(CURRENT_DATE)
ON DUPLICATE KEY UPDATE 
    `NextValue` = GREATEST(`NextValue`, VALUES(`NextValue`));
```

## Prevention

- **Always seed new generator tables:** When replacing an aggregation-based generator (like `CountAsync() + 1`) with an explicit database sequence table on a live system, the migration script MUST extract the current max value and seed the new table.
- **Isolate Generator Commits:** The EF Core `DbUpdateException` duplicate key catch-retry loop is robust for concurrent clients trying to create the initial `Year` row, but it cannot protect against the *outer* transaction failing on the live entity insertion.

## Related

- Domain Wiki: `[[centralized-database-architecture]]`
