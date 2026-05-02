---
module: MerchSys.Accounting
plan-id: ACC-02
title: "Accounting Data Access"
depends-on: [INFRA-03, ACC-01]
estimated-files: 6
---

# Accounting Data Access

## Context

Configures the `AccountingDbContext` with EF Core entity configurations for all Accounting entities and MediatR event handlers for receiving data from other modules.

## Prerequisites

- **INFRA-03** (Database Contexts) — `AccountingDbContext` shell exists
- **ACC-01** (Domain Models) — all Accounting entities exist

## Deliverables

```
MerchSys.Accounting/Data/
├── AccountingDbContext.vb                  ← Update: add DbSets
├── Configurations/
│   ├── FinancialPeriodConfiguration.vb
│   ├── RevenueRecordConfiguration.vb
│   ├── ExpenseRecordConfiguration.vb
│   └── FinancialSnapshotConfiguration.vb

MerchSys.Accounting/Handlers/
├── SaleCompletedAccountingHandler.vb       ← Records revenue + COGS
├── GoodsReceivedAccountingHandler.vb       ← Records AP expense
├── CreditPaymentAccountingHandler.vb       ← Reduces AR
└── ShrinkageAccountingHandler.vb           ← Records shrinkage expense
```

## Specification

### Table Names (Acc_ prefix)
`Acc_FinancialPeriods`, `Acc_RevenueRecords`, `Acc_ExpenseRecords`, `Acc_FinancialSnapshots`

### Key Configurations
- **RevenueRecord:** All monetary fields precision(18,2), Index on RecordDate, Index on ProductId
- **ExpenseRecord:** Amount precision(18,2), Category required max 50
- **FinancialSnapshot:** Index on SnapshotDate (unique)

### MediatR Handlers

**SaleCompletedAccountingHandler** (handles `SaleCompletedEvent`):
1. For each sale item, create a `RevenueRecord`
2. Query Inventory via MediatR (`GetInventoryValuationQuery`) to get COGS data, or use the event payload
3. Calculate `GrossProfit = NetAmount - COGS`
4. Create `ExpenseRecord` with Category="COGS"

**GoodsReceivedAccountingHandler** (handles `GoodsReceivedEvent`):
1. Create `ExpenseRecord` with Category="Purchase" for AP tracking
2. Record the total purchase cost

**CreditPaymentAccountingHandler** (handles `CreditPaymentEvent`):
1. Record the AR reduction

**ShrinkageAccountingHandler** (handles `ShrinkageRecordedEvent`):
1. Create `ExpenseRecord` with Category="Shrinkage" + the total value lost

## Acceptance Criteria

1. `dotnet build` succeeds
2. All tables use `Acc_` prefix
3. All 4 MediatR handlers registered and functional
4. Revenue records created per sale event
5. Expense records created for COGS, purchases, and shrinkage

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-02-summary.md`
