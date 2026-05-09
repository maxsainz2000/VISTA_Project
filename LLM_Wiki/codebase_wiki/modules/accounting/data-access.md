---
type: layer-manifest
module: MerchSys.Accounting
layer: Data Access
last-updated: 2026-05-09
---

# MerchSys.Accounting — Data Access

This page details the Data Access layer for the **MerchSys.Accounting** module. Provides manual migration files for VB.NET support.

## DbContext
- **Class:** `AccountingDbContext`
- **Base:** `AppDbContext` (Inherited from SharedKernel)
- **Primary Responsibility:** Orchestrates persistence for financial records and P&L snapshots.

## Configurations and DbSets

| File Path | Entity / Table | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Accounting/Data/AccountingDbContext.vb` | `AccountingDbContext` | Entry point for persistence; exposes DbSets for all accounting entities. |
| `src/MerchSys.Accounting/Data/AccountingDbContextFactory.vb` | `AccountingDbContextFactory` | `IDesignTimeDbContextFactory(Of AccountingDbContext)` implementation for EF CLI design-time support. |
| `src/MerchSys.Accounting/Data/Configurations/FinancialPeriodConfiguration.vb` | `FinancialPeriod` / `Acc_FinancialPeriods` | Configures P&L summaries with decimal precision (18,2) and GrossMargin (10,4). |
| `src/MerchSys.Accounting/Data/Configurations/RevenueRecordConfiguration.vb` | `RevenueRecord` / `Acc_RevenueRecords` | Configures revenue tracking; includes indexes on `ProductId` and `RecordDate`. |
| `src/MerchSys.Accounting/Data/Configurations/ExpenseRecordConfiguration.vb` | `ExpenseRecord` / `Acc_ExpenseRecords` | Configures expense records; enforces required Category with max length 50. |
| `src/MerchSys.Accounting/Data/Configurations/FinancialSnapshotConfiguration.vb` | `FinancialSnapshot` / `Acc_FinancialSnapshots` | Configures KPI snapshots with unique index on `SnapshotDate`. |
| `src/MerchSys.Accounting/Migrations/20260507100004_InitialAccounting.vb` | `InitialAccounting` | Manual EF Core migration (Sqlite) for 4 Accounting tables. |
| `src/MerchSys.Accounting/Migrations/AccountingDbContextModelSnapshot.vb` | `AccountingDbContextModelSnapshot` | EF Core model snapshot for the Accounting module. |

