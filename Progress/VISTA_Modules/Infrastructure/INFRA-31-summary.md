---
module: Infrastructure
agent: antigravity
date: 2026-05-29
plan-ref: Plans/VISTA_Modules/Infrastructure/31-rowversion-mapping-correction.md
status: completed
---

## Task Summary

This report documents the completed implementation of **INFRA-31: RowVersion Mapping Correction**.
It restores the design scope of **INFRA-26** optimistic concurrency control by ensuring that `RowVersion` is only treated as a mapped column where the database table physically possesses it (Group A), while being completely ignored for all append-only and child tables (Group B).

**Plan:** `[[31-rowversion-mapping-correction.md]]`
**Branch:** `debug/PUR-savedraft-rowversion` (reconciled and finalized)

## What Was Done

- Modified `WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Data/BaseDbContext.vb`
  - Registered `IgnoreNonTokenRowVersionConvention` (a nested `IModelFinalizingConvention` class) inside `ConfigureConventions`.
  - Added XML documentation for the custom convention explaining the rule.
- Modified the 5 missing Group A entity configurations to add explicit `.IsRowVersion()` mapping:
  - `ReorderConfigConfiguration.vb` (Purchasing module)
  - `StockAlertConfigConfiguration.vb` (Inventory module)
  - `VatConfigurationMap.vb` (POS module)
  - `FinancialPeriodConfiguration.vb` (Accounting module)
  - `VatReturnMap.vb` (Accounting module)
- Modified `PurchaseOrderLineConfiguration.vb` (Purchasing module):
  - Removed the redundant, local hotfix `builder.Ignore(Function(l) l.RowVersion)` to keep a single, centralized convention-based mechanism.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Completed successfully) |
| Concurrency check | ✅ (INFRA-26 AC #2 does not regress) |
| Manual verification | ✅ (Group B tables no longer generate RowVersion column in SQL) |

### Group A and B Classifications
1. **Group A (Real Concurrency-Token tables - Mapped):** `Product`, `ProductCategory`, `StockBatch`, `CreditAccount`, `ReceiptSequence`, `SalesTransaction`, `VendorProduct`, `Vendor`, `PurchaseOrder`, `AccountsPayableEntry`, `ReorderConfig`, `StockAlertConfig`, `VatConfiguration`, `FinancialPeriod`, `VatReturn`.
2. **UserAccount Determination:** Audited the `UserAccount` entity. It does not carry a `RowVersion` property or inherit `AuditableEntity`, so it naturally does not require EF mapping. The DB default continues to fill the column correctly.
3. **Group B (Non-concurrency tables - Ignored):** Centrally ignored via the finalizing convention. Includes: `PurchaseOrderLine`, `GoodsReceipt`, `GoodsReceiptLine`, `ReorderSuggestion`, `PriceChangeAlert`, `StockMovement`, `ShrinkageRecord`, `StockAuditRecord`, `SalesTransactionLine`, `SalesReturn`, `OfficialReceipt`, `ReceiptIntegrity`, `ReceiptIntegrityArchive`, `OfficialReceiptArchive`, `CreditPayment`, `ExpenseRecord`, `RevenueRecord`, `FinancialSnapshot`, `VatReturnLine`.

## Issues Encountered

None. The EF Core 10 `IModelFinalizingConvention` API proved fully supported and highly elegant under VB.NET, avoiding the fallback reordering mechanism.

## What's Next

- Verify runtime behavior on MariaDB to confirm that no `Unknown column 'RowVersion'` is generated in inserts or updates for Group B tables.
