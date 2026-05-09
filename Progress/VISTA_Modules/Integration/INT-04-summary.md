---
module: Integration
agent: claude-code
date: 2026-05-09
plan-ref: Plans/VISTA_Modules/Integration/04-ef-migrations.md
status: completed
---

# INT-04: EF Core Migrations & Data Layer Finalization

## Task Summary

Generated manual EF Core migrations for all four module DbContexts (Purchasing, Inventory, POS, Accounting), created model snapshots, added `IDesignTimeDbContextFactory` implementations for CLI discoverability, and implemented a `DatabaseInitializer` workaround for EF Core 10's inability to discover VB.NET migration classes at runtime.

**Plan:** `Plans/VISTA_Modules/Integration/04-ef-migrations.md`

## What Was Done

- Created `src/MerchSys.Purchasing/Data/PurchasingDbContextFactory.vb` — `IDesignTimeDbContextFactory(Of PurchasingDbContext)` for EF CLI design-time support
- Created `src/MerchSys.Inventory/Data/InventoryDbContextFactory.vb` — same pattern for Inventory
- Created `src/MerchSys.POS/Data/POSDbContextFactory.vb` — same pattern for POS
- Created `src/MerchSys.Accounting/Data/AccountingDbContextFactory.vb` — same pattern for Accounting
- Created `src/MerchSys.Purchasing/Migrations/20260507100001_InitialPurchasing.vb` — manual migration for 9 Purchasing tables + 3 vendor seed rows
- Created `src/MerchSys.Purchasing/Migrations/PurchasingDbContextModelSnapshot.vb` — model snapshot for future migration diffs
- Created `src/MerchSys.Inventory/Migrations/20260507100002_InitialInventory.vb` — manual migration for 5 Inventory tables + 4 categories + 20 product seed rows
- Created `src/MerchSys.Inventory/Migrations/InventoryDbContextModelSnapshot.vb` — model snapshot
- Created `src/MerchSys.POS/Migrations/20260507100003_InitialPOS.vb` — manual migration for 6 POS tables + 3 credit account seed rows
- Created `src/MerchSys.POS/Migrations/POSDbContextModelSnapshot.vb` — model snapshot
- Created `src/MerchSys.Accounting/Migrations/20260507100004_InitialAccounting.vb` — manual migration for 4 Accounting tables (no seed data)
- Created `src/MerchSys.Accounting/Migrations/AccountingDbContextModelSnapshot.vb` — model snapshot
- Created `src/MerchSys.App/Data/DatabaseInitializer.vb` — ADO.NET initializer that applies all 4 migrations idempotently on app startup (workaround for EF CLI VB.NET bug)
- Modified `src/MerchSys.App/Application.xaml.vb` — calls `DatabaseInitializer.Initialize()` before the main window is shown
- Added `Microsoft.EntityFrameworkCore.Design` v10.0.7 to `MerchSys.App.vbproj` (required for EF CLI startup project)
- Installed `dotnet-ef` 10.0.7 global tool

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors, 0 warnings |
| `dotnet ef migrations add` commands | ❌ Blocked — EF Core 10 CLI does not support VB.NET code generation (migrations written manually) |
| `dotnet ef database update` commands | ❌ Blocked — EF CLI discovers 0 migration classes in VB.NET assembly (see Issues below) |
| `DatabaseInitializer` compiles correctly | ✅ |
| Database created at app startup | ✅ (via `DatabaseInitializer` workaround, applied on first run) |

## Issues Encountered

- **Issue:** `dotnet ef migrations add` fails with "The project language 'VB' isn't supported by the built-in IMigrationsCodeGenerator service"
  - **Resolution:** The community package `EntityFrameworkCore.VisualBasic` (v8.0.0) only supports EF Core 8 and has a `Microsoft.CodeAnalysis.Common` version conflict with EF 10. Migrations were written manually in VB.NET using `migrationBuilder.Sql()` for raw DDL and DML.

- **Issue:** `dotnet ef database update` reports "No migrations were found in assembly 'MerchSys.Purchasing'" even though the migration class (with `[Migration]` attribute and `Inherits Migration`) is correctly compiled in the DLL (confirmed via ILSpy decompilation).
  - **Root cause:** EF Core 10's design-time migration scanner (`GetConstructibleTypes()`) does not discover migration classes from VB.NET compiled assemblies when invoked through the CLI tool chain. The exact internal cause is unclear — the compiled IL is correct (`public class InitialPurchasing : Migration` with `[Migration("...")]` attribute) but EF's assembly scanner returns empty. Suspected EF Core 10 regression with VB.NET assemblies.
  - **Resolution:** Created `src/MerchSys.App/Data/DatabaseInitializer.vb` — a standalone ADO.NET module using `Microsoft.Data.Sqlite.SqliteConnection` that applies all 4 migrations idempotently using `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`, and `INSERT OR IGNORE INTO`. Called from `Application_Startup` in `Application.xaml.vb` before the main window is shown. Populates `__EFMigrationsHistory` correctly so future EF migration runs (if the discovery bug is fixed) will recognize the baseline.
  - **Agent Wiki entry:** `efcore-vbnet-migration-discovery-bug` (to be filed)

- **Issue:** `EntityFrameworkCore.VisualBasic` v8.0.0 package caused `NU1107` version conflict (`Microsoft.CodeAnalysis.Common` needs 4.5.0 vs 5.0.0 required by EF 10 analyzers)
  - **Resolution:** Removed the package. Not needed since migrations are written manually.

## Database Schema Summary

All 24 tables created across 4 modules:

| Module | Tables |
|--------|--------|
| Purchasing (Pur_) | Pur_Vendors, Pur_PurchaseOrders, Pur_PurchaseOrderLines, Pur_GoodsReceipts, Pur_GoodsReceiptLines, Pur_AccountsPayable, Pur_ReorderConfigs, Pur_ReorderSuggestions, Pur_PriceChangeAlerts |
| Inventory (Inv_) | Inv_ProductCategories, Inv_Products, Inv_StockBatches, Inv_ShrinkageRecords, Inv_StockAlertConfigs |
| POS (Pos_) | Pos_CreditAccounts, Pos_SalesTransactions, Pos_SalesTransactionLines, Pos_OfficialReceipts, Pos_CreditPayments, Pos_SalesReturns |
| Accounting (Acc_) | Acc_FinancialPeriods, Acc_RevenueRecords, Acc_ExpenseRecords, Acc_FinancialSnapshots |

All tables have `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` audit columns. Financial and inventory tables have `IsDeleted`, `DeletedBy`, `DeletedAt` for soft-delete. Table prefixes are correct (`Pur_`, `Inv_`, `Pos_`, `Acc_`).

## Seed Data

| Module | Seed Records |
|--------|-------------|
| Purchasing | 3 vendors (AgriChem Supplies, FarmFresh Seeds Corp., Golden Feeds Trading) |
| Inventory | 4 product categories, 20 products (5 fertilizers, 5 pesticides, 5 seeds, 5 animal feeds) |
| POS | 3 credit accounts (Juan Dela Cruz, Maria Santos, Pedro Reyes) |
| Accounting | None |

## What's Next

- [x] Log EF Core 10 VB.NET migration discovery bug in `LLM_Wiki/agent_wiki/errors/` for future agent awareness *(completed — logged at `agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md`)*
- [ ] When EF Core fixes VB.NET migration discovery: run `dotnet ef database update` for all 4 modules to validate the manual migration files apply cleanly *(genuine — tracked by INT-06)*
- [x] INT-05: Final integration and smoke testing *(completed — INT-05 delivered)*

## Codebase Wiki Discrepancies

None observed.

## Cross-References

- Domain Wiki pages consulted: `LLM_Wiki/wiki/analysis/tech-stack-reference.md`, `LLM_Wiki/wiki/concepts/modular-monolith.md`
- Agent Wiki entries consulted: None (first time hitting this specific EF/VB.NET issue)
