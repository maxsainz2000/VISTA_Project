---
module: Infrastructure
agent: antigravity
date: 2026-05-28
plan-ref: Plans/VISTA_Modules/Infrastructure/24-mariadb-schema-bootstrap.md
status: completed
---

## Task Summary

Replaced the local SQLite `DatabaseInitializer` schema builder with a centralized MariaDB startup-time schema bootstrap and seeding module (`MariaDbSchemaInitializer`). The central schema now maps all 40 module database tables transactionally, sets Microsecond-level timestamp formatting (`DATETIME(6)`), wires MySQL optimistic concurrency `TIMESTAMP(6)` defaults on all mutable fields, applies BIR Receipt immutability trigger procedures natively in MariaDB, and performs SHA-256 drift detection to cleanly abort app startup upon script tampering.

**Plan:** `[[24-mariadb-schema-bootstrap]]`

## What Was Done

- Created `0001_initial_schema.sql` at `src/MerchSys.Infrastructure/Data/Migrations/Central/` — Ported the complete SQLite baseline schema (40 tables, keys, triggers, constraints) to modern MariaDB syntax with microsecond timestamps and concurrency row versions.
- Created `0002_seed_reference_data.sql` at `src/MerchSys.Infrastructure/Data/Migrations/Central/` — Idempotent reference data seed scripts for 4 categories, 3 vendors, 20 products, 3 credit accounts, and default non-VAT configurations.
- Created `README.md` at `src/MerchSys.Infrastructure/Data/Migrations/Central/` — Documented rules for migration scripting, lexicographical sequencing, idempotency, and drift prevention.
- Created `MariaDbSchemaInitializer.vb` in `src/MerchSys.App/Data/` — Executed embedded DDL resources inside a transaction using custom statement splitting to support `DELIMITER` blocks; hashes each applied script to ensure SHA-256 matching. Seeds default system user accounts (manager, owner) using direct PasswordHashHelper encryption.
- Modified `App.xaml.vb` — Replaced legacy SQLite initializer call with `MariaDbSchemaInitializer.Initialize` inside startup blocks, passing central DB connection and logger dependencies with complete crash/MessageBox aborts.
- Modified `MerchSys.App.vbproj` — Added `<EmbeddedResource>` directive to compile and link all schema SQL files inside `MerchSys.App.dll` resources, and added `MySqlConnector` package dependency.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Database bootstrap | ✅ (Created 40 Tables, Seeding Succeeded) |
| Drift detection | ✅ (Aborts startup with CRITICAL SHA-256 error) |
| Manual verification | ✅ |

## Issues Encountered

- **Issue:** The standard `MySqlScript` class is not present inside the lightweight `MySqlConnector` package (only in Oracle's `MySql.Data`).
  - **Resolution:** Designed and implemented a custom SQL script parsing module `ApplyMigrationScript` inside `MariaDbSchemaInitializer.vb` that correctly parses statement groups, extracts custom `DELIMITER` statements, and runs batches individually inside transaction boundaries.
- **Issue:** Process working directory resolved to the parent repository folder when launching from background, causing empty connection settings.
  - **Resolution:** Set process launch working directory (`Cwd`) to `src/MerchSys.App` to correctly discover `appsettings.json`.

## What's Next

Proceed to **INFRA-25 (Convert Module DbContexts from SQLite to MariaDB)** to migrate DbContext classes to utilize the Oracle MariaDB provider and remove all local SQLite ORM footprints.

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`
- Agent Wiki entries consulted: `[[mariadb-pure-client-server-architecture]]`, `[[efcore10-vbnet-migration-discovery-bug]]`
