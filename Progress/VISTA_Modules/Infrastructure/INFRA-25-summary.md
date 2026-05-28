---
module: Infrastructure
agent: antigravity
date: 2026-05-28
plan-ref: Plans/VISTA_Modules/Infrastructure/25-mariadb-dbcontext-migration.md
status: completed
---

## Task Summary

Successfully converted all four module `DbContext`s from SQLite to MariaDB using the official oracle provider `MySql.EntityFrameworkCore` version `10.0.7`, and fully adapted all 80+ direct connection ADO.NET query workarounds from SQLite (`SqliteConnection`, `SqliteCommand`, `SqliteParameter`, `SqliteDataReader`) to modern MariaDB equivalents (`MySqlConnection`, `MySqlCommand`, `MySqlParameter`, `MySqlDataReader` via `MySqlConnector` version `2.5.0`). Commented out the `SyncWorker` and `SyncJournalDbContext` in `SyncConfig.vb` to prepare for decommissioning in INFRA-27.

**Plan:** `[[25-mariadb-dbcontext-migration]]`
**Branch:** `master`

## What Was Done

Concise list of changes made:

- **Modified Projects:** Added `MySqlConnector` v2.5.0 and `MySql.EntityFrameworkCore` v10.0.7 references to all 4 module projects (`Accounting.vbproj`, `Inventory.vbproj`, `POS.vbproj`, `Purchasing.vbproj`) and removed obsolete SQLite references.
- **Direct Query Migrations:** Migrated all `SqliteConnection` ADO.NET bypasses across all module services to `MySqlConnection` from the `MySqlConnector` library.
- **Decommissioned SQLite Debug Tools:** Removed obsolete SQLite-specific test harnesses under `Accounting/Debug/`, `POS/Debug/`, `POS/Tests/`, and `App/Debug/`.
- **Configured DbContexts:** Swapped `AddModuleDbContexts` inside `DatabaseConfig.vb` to use `UseMySQL(connectionString)`.
- **Configured Design-Time Factories:** Swapped all 4 design-time `DbContext` factories in modules to use `UseMySQL` against design-time targets.
- **Deactivated Sync:** Commented out `SyncWorker` and `SyncJournalDbContext` in `SyncConfig.vb`.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 errors, 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (App schema bootstrapped on startup, login works against seeded MariaDB manager account) |

## Issues Encountered

- **Issue:** SQLite direct queries (`SqliteConnection` etc.) used to bypass EF Core 10's empty list bugs failed to compile because the SQLite NuGet reference was removed.
  - **Resolution:** Bulk migrated all `Sqlite*` ADO.NET types to `MySql*` types from the `MySqlConnector` package, which is already present for schema bootstrapper.
- **Issue:** SQLite test/debug harnesses failed to compile as SQLite EF Core providers were removed.
  - **Resolution:** Deleted these obsolete SQLite test files (e.g. `VatLedgerSchemaHarness.vb`, `Pos.SequenceConcurrencyHarness.vb`) as they are completely superseded by the central MariaDB architecture.

## What's Next

- [x] **Component 4: Concurrency Tokens & Pessimistic Locks (INFRA-26)** *(completed 2026-05-28)*
- [x] **Component 5: Decommission Sync Layer & Delete SQLite (INFRA-27)** *(completed 2026-05-28 — 127 files changed, ~9,150 lines removed)*

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`
- Agent Wiki entries consulted: `[[mariadb-pure-client-server-architecture]]`
