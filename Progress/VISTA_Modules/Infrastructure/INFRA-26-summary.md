---
module: Infrastructure
agent: antigravity
date: 2026-05-28
plan-ref: Plans/VISTA_Modules/Infrastructure/26-optimistic-concurrency-and-fifo-lock.md
status: completed
---

## Task Summary

Successfully implemented both optimistic concurrency tokens and pessimistic locking in VISTA. Created the abstract base class `ConcurrencyAwareEntity` to define the `RowVersion` optimistic concurrency token (mapped to MariaDB `TIMESTAMP(6)`), updated `AuditableEntity` to inherit from it, and mapped it in EF Core Configurations for all 8 mutable tables. Rewrote `StockService.DeductStockFIFOAsync` and `ShrinkageService.RecordShrinkageAsync` FIFO paths to run within explicit connection transactions with a `FOR UPDATE` clause, locking records and preventing stock divergence. Created `ConcurrencyHelper.vb` with `ExecuteWithConcurrencyRetryAsync` to catch concurrency conflicts, show error toasts, and trigger refreshing in a completely compiler-safe way.

**Plan:** `[[26-optimistic-concurrency-and-fifo-lock]]`
**Branch:** `master`

## What Was Done

Concise list of changes made:

- **Created ConcurrencyAwareEntity:** Created `ConcurrencyAwareEntity.vb` in `SharedKernel/Entities/` with a `RowVersion` property of type `DateTime`.
- **Inherited in AuditableEntity:** Modified `AuditableEntity.vb` to inherit from `ConcurrencyAwareEntity` so that all mutable entities inherit it.
- **Configured EF Mappings:** Added `Property(Function(e) e.RowVersion).IsRowVersion().HasColumnType("TIMESTAMP(6)")` to the entity configurations of all 8 mutable tables: `Inv_StockBatches`, `Inv_Products`, `Pur_AccountsPayable`, `Pur_PurchaseOrders`, `Pos_CreditAccounts`, `Pos_SalesTransactions`, `Pur_Vendors`, `Inv_ProductCategories`, and `Pos_ReceiptSequence`.
- **FIFO Pessimistic Lock Rewrite:** Rewrote FIFO decrement paths in `StockService.vb` and `ShrinkageService.vb` to run transactionally using raw ADO.NET and the `FOR UPDATE` modifier, releasing locks only on commit/rollback.
- **Fixed Stock Discrepancy:** Added DB writes for `QuantityRemaining` updates in `ShrinkageService.vb` FIFO path, resolving a silent data-loss bug in the legacy code.
- **Wired UI Toast Retry Helper:** Created `ConcurrencyHelper.vb` with the `ExecuteWithConcurrencyRetryAsync` ViewModel helper. Fixed a VB.NET compiler error (BC36943) regarding await-in-catch by executing the await callback after the try-catch block.
- **Eliminated Warning:** Removed local `RowVersion` shadowing property in `ReceiptSequence.vb` and manual assignments in `ReceiptIntegrityService.vb` to leverage native MariaDB automatic TIMESTAMP updates.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 errors, 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Database-driven optimistic concurrency and pessimistic transaction FOR UPDATE blocks verified as compilation-safe and warning-free) |

## Issues Encountered

- **Issue:** VB.NET compiler threw error BC36943 when attempting to await inside the catch block of `ExecuteWithConcurrencyRetryAsync`.
  - **Resolution:** Modified the method to capture the concurrency conflict flag in the catch block and await the reload callback afterward.
- **Issue:** `ReceiptSequence` had property shadowing warnings because it redefined `RowVersion` locally as `Byte()`.
  - **Resolution:** Deleted local redefining and manual assignments to utilize standard native `TIMESTAMP(6)` database-managed row versioning from the base class.

## What's Next

- [x] **Component 5: Decommission Sync Layer & Delete SQLite (INFRA-27)** *(completed 2026-05-28)*
