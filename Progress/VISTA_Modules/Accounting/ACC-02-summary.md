---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-06
plan-ref: Plans/VISTA_Modules/Accounting/02-data-access.md
status: completed
---

## Task Summary

Implemented the Accounting data access layer: updated `AccountingDbContext` with four DbSets, created four EF Core entity configurations, and created four MediatR event handlers for revenue and expense recording.

**Plan:** `[[02-data-access]]`

## What Was Done

- Modified `src/MerchSys.Accounting/Data/AccountingDbContext.vb` — added DbSets for `FinancialPeriods`, `RevenueRecords`, `ExpenseRecords`, `FinancialSnapshots`
- Created `src/MerchSys.Accounting/Data/Configurations/FinancialPeriodConfiguration.vb` — table `Acc_FinancialPeriods`, precision(18,2) on all monetary fields, precision(10,4) on GrossMarginPercent
- Created `src/MerchSys.Accounting/Data/Configurations/RevenueRecordConfiguration.vb` — table `Acc_RevenueRecords`, precision(18,2) on all monetary fields, indexes on `RecordDate` and `ProductId`
- Created `src/MerchSys.Accounting/Data/Configurations/ExpenseRecordConfiguration.vb` — table `Acc_ExpenseRecords`, precision(18,2) on Amount, Category max 50 required
- Created `src/MerchSys.Accounting/Data/Configurations/FinancialSnapshotConfiguration.vb` — table `Acc_FinancialSnapshots`, precision(18,2) on all monetary fields, unique index on `SnapshotDate`
- Created `src/MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` — creates `RevenueRecord` + COGS `ExpenseRecord` per sale item
- Created `src/MerchSys.Accounting/Handlers/GoodsReceivedAccountingHandler.vb` — creates "Purchase" `ExpenseRecord` per received line item
- Created `src/MerchSys.Accounting/Handlers/CreditPaymentAccountingHandler.vb` — creates "AR Reduction" `ExpenseRecord` on credit repayment
- Created `src/MerchSys.Accounting/Handlers/ShrinkageAccountingHandler.vb` — creates "Shrinkage" `ExpenseRecord` for inventory write-offs

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

- **COGS not in event payload:** `SaleCompletedEvent.SaleItem` carries `UnitPrice`, `Quantity`, and `DiscountAmount` but no unit cost. `RevenueRecord.COGS` and the paired COGS `ExpenseRecord.Amount` are both recorded as `0` for now. `GrossProfit` equals `NetAmount` until a per-product cost query is available from Inventory.
  - **Resolution:** Noted as a known limitation; no architecture change required at this stage. COGS can be backfilled when the Accounting service layer (ACC-03) introduces the inventory valuation query path.

## What's Next

- [ ] ACC-03: Accounting Services — implement `IAccountingService` for period aggregation and snapshot refresh
- [ ] ACC-04: Accounting ViewModels and Views — KPI dashboard and plain-language summaries

## Cross-References

- Domain Wiki pages consulted: none
- Agent Wiki entries consulted: none
- Codebase Wiki pages consulted: `[[modules/accounting/index]]`, `[[modules/accounting/entities]]`, `[[modules/shared-kernel/events-queries]]`, `[[modules/inventory/data-access]]`
