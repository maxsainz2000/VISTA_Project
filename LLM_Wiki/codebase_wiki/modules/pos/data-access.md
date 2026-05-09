---
type: layer-manifest
module: MerchSys.POS
layer: Data Access
last-updated: 2026-05-09
---

# MerchSys.POS — Data Access

This page details the Data Access layer for the **MerchSys.POS** module. Provides manual migration files for VB.NET support.

## DbContext
- **Class:** `POSDbContext`
- **Base:** `AppDbContext` (Inherited from SharedKernel)
- **Primary Responsibility:** Orchestrates persistence for sales transactions, credit accounts, and official receipts.

## Files and Classes

| File Path | Class / Interface | Responsibilities / Notes |
|---|---|---|
| `src/MerchSys.POS/Data/POSDbContext.vb` | `POSDbContext` | Entry point for persistence; exposes DbSets for `CreditAccounts`, `SalesTransactions`, `SalesTransactionLines`, `OfficialReceipts`, `CreditPayments`, `SalesReturns`. |
| `src/MerchSys.POS/Data/POSDbContextFactory.vb` | `POSDbContextFactory` | `IDesignTimeDbContextFactory(Of POSDbContext)` implementation for EF CLI design-time support. |
| `src/MerchSys.POS/Migrations/20260507100003_InitialPOS.vb` | `InitialPOS` | Manual EF Core migration (Sqlite) for 6 POS tables and seed data. |
| `src/MerchSys.POS/Migrations/POSDbContextModelSnapshot.vb` | `POSDbContextModelSnapshot` | EF Core model snapshot for the POS module. |
