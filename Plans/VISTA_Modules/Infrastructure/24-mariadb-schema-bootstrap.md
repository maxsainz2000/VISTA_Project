---
module: MerchSys.Infrastructure
plan-id: INFRA-24
title: "MariaDB Schema Bootstrap (Replaces SQLite DatabaseInitializer)"
depends-on: []
estimated-files: 8
priority: critical
amendment-ref: AMD-2026-05-28-01
---

# INFRA-24: MariaDB Schema Bootstrap

## Context

The 2026-05-28 amendment removes SQLite and the per-module SQLite `DatabaseInitializer`. The centralized MariaDB instance must instead carry the full schema for all four modules (`Pur_*`, `Inv_*`, `Pos_*`, `Acc_*`).

Today the MariaDB schema is hand-maintained (INFRA-22 explicitly noted "no `mariadb-init.sql` exists in the repo"). That is no longer acceptable when MariaDB is the **only** DB — every client laptop must hit a known schema version. This plan introduces a startup-time bootstrap that applies versioned DDL scripts using raw `MySqlConnector` (bypassing the EF Core 10 ↔ VB.NET migration discovery bug — see `agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md`).

This plan is **independent of INFRA-23**: schema bootstrap uses raw `MySqlConnector`, not EF Core, so it works regardless of which EF Core provider is chosen for the module DbContexts.

## Prerequisites

- None directly. Runs in parallel with INFRA-23.
- Reads: existing local SQLite `DatabaseInitializer` (`MerchSys.App/Data/DatabaseInitializer.vb`) and every module's `Migrations/` folder for the SQLite DDL — both are the source of truth for the schema to translate.

## Wiki References

- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md`
- `LLM_Wiki/agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md` — raw-connection bootstrap pattern (now using `MySqlConnection` instead of `SqliteConnection`)
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md`

## Deliverables

```
MerchSys.Infrastructure/Data/Migrations/Central/
├── 0001_initial_schema.sql                    ' NEW — all CREATE TABLE IF NOT EXISTS, indexes, FKs
├── 0002_seed_reference_data.sql               ' NEW — 4 categories, 3 vendors, 3 credit accounts, 20 products
└── README.md                                  ' NEW — naming convention, idempotency rules

MerchSys.App/Data/
└── MariaDbSchemaInitializer.vb                ' NEW — startup bootstrap, replaces SQLite DatabaseInitializer

MerchSys.App/
└── App.xaml.vb (or Application_Startup.vb)    ' MOD — call MariaDbSchemaInitializer.Initialize before _host.Build()

MerchSys.App/MerchSys.App.vbproj               ' MOD — add the .sql files as embedded resources
```

The SQLite `DatabaseInitializer.vb` and all `MerchSys.<Module>/Migrations/` folders are **not** deleted in this plan — they are torn out in INFRA-25 + INFRA-27.

## Specification

### Schema scripts

`0001_initial_schema.sql` contains every `CREATE TABLE IF NOT EXISTS` for every table currently in the SQLite schema, ported to MariaDB syntax. Conventions:

- Engine: `ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`
- PK: `INT NOT NULL AUTO_INCREMENT`
- Money: `DECIMAL(18, 4)` (same precision as SQLite REAL with declared precision)
- Timestamps: `DATETIME(6)` for microsecond precision (matches INFRA-14's pattern)
- Booleans: `TINYINT(1) NOT NULL DEFAULT 0`
- Soft-delete flag: `IsDeleted TINYINT(1) NOT NULL DEFAULT 0`
- Audit columns: `CreatedBy VARCHAR(64) NOT NULL`, `CreatedAt DATETIME(6) NOT NULL`, `ModifiedBy VARCHAR(64) NULL`, `ModifiedAt DATETIME(6) NULL`
- **Optimistic concurrency token (NEW, per INFRA-27):** every mutable table gets `RowVersion TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)`. Append-only tables (`Inv_StockMovements`, `Inv_SaleCogs`, `Pos_ReceiptIntegrity`, `Acc_TamperAuditLog`, etc.) omit this column.
- FKs preserved with `ON DELETE RESTRICT`
- Indexes ported one-for-one from SQLite

`0002_seed_reference_data.sql` uses `INSERT IGNORE` (MariaDB equivalent of SQLite `INSERT OR IGNORE`) for idempotent seeding of:
- 4 `Inv_ProductCategories` (Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds)
- 3 `Pur_Vendors`
- 3 `Pos_CreditAccounts`
- 20 `Inv_Products`

Reuse the exact values from the current SQLite seed in `DatabaseInitializer.vb`.

### `__EFMigrationsHistory` substitute

The previous SQLite design populated `__EFMigrationsHistory` to satisfy EF CLI introspection. EF CLI is unused for MariaDB schema. Instead, create `__SchemaMigrations` (single source of truth):

```sql
CREATE TABLE IF NOT EXISTS `__SchemaMigrations` (
    `ScriptName` VARCHAR(128) NOT NULL,
    `AppliedAt`  DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `Sha256`     VARCHAR(64)  NOT NULL,
    PRIMARY KEY (`ScriptName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

`Sha256` stores the hash of the script content as applied — drift detection for the next plan to ship.

### `MariaDbSchemaInitializer.vb`

```vb
Public Module MariaDbSchemaInitializer
    Public Sub Initialize(connectionString As String, logger As ILogger)
        ' 1. Open MySqlConnection
        ' 2. Ensure __SchemaMigrations exists
        ' 3. Enumerate embedded .sql resources in lexicographic order (0001_, 0002_, ...)
        ' 4. For each: if name NOT IN __SchemaMigrations, execute; record (name, sha256)
        ' 5. If name IS IN __SchemaMigrations but sha256 differs → log critical error and ABORT app
        ' 6. Close
    End Sub
End Module
```

Critical rules:
- Scripts must be **idempotent** (`CREATE TABLE IF NOT EXISTS`, `INSERT IGNORE`).
- Scripts are executed **once per app launch** at the start of `Application_Startup`, before `_host.Build()`.
- Hash drift = ABORT, not silent skip. A schema script changed under our feet means the DB and the app disagree about reality.
- Use `MySqlConnector.MySqlConnection` (the same package the sync layer used). Do **not** add Pomelo or the EF Core MariaDB provider as a dependency of `MerchSys.App` for this purpose.
- All script execution wrapped in a single transaction per script file. On failure, transaction rolls back, exception bubbles, app exits.

### Embedded resources

Scripts ship as embedded `.sql` resources in `MerchSys.App.dll`. `.vbproj` entry:

```xml
<ItemGroup>
  <EmbeddedResource Include="..\..\src\MerchSys.Infrastructure\Data\Migrations\Central\*.sql" />
</ItemGroup>
```

This guarantees clients always have the exact schema the binary expects — no separate `.sql` file deployment.

### Bootstrap call site

In `Application_Startup`, immediately after reading `appsettings.json`'s connection string:

```vb
Dim connStr = Configuration("ConnectionStrings:MerchSysCentral")
Try
    MariaDbSchemaInitializer.Initialize(connStr, _logger)
Catch ex As Exception
    ' Cannot continue without a known schema. Show fatal error window and exit.
    MessageBox.Show($"Cannot initialize database schema: {ex.Message}", "VISTA — Fatal", MessageBoxButton.OK, MessageBoxImage.Error)
    Shutdown(1)
    Return
End Try
```

## Acceptance Criteria

1. `0001_initial_schema.sql` applied against an empty `merchsys_central` produces every table currently in local SQLite. `SHOW TABLES` reconciliation passes.
2. Column types, FKs, indexes match the SQLite shape (per-table comparison documented in the progress summary).
3. `RowVersion` column exists on every mutable table per the INFRA-27 list. Append-only tables do not have it.
4. `__SchemaMigrations` contains rows for `0001_initial_schema.sql` and `0002_seed_reference_data.sql` after first launch.
5. Re-launching the app produces no schema changes (idempotent — `__SchemaMigrations` already has the entries; scripts skipped).
6. Tampering with a script content (changing one line and re-launching) causes the initializer to abort with a "schema drift detected" error and the app exits.
7. Seed counts on a fresh DB: 4 categories, 3 vendors, 3 credit accounts, 20 products.
8. Build clean: 0 errors / 0 warnings.

## Out of Scope (Defer)

- Tearing out the SQLite `DatabaseInitializer` and per-module `Migrations/` folders. That is INFRA-25.
- Wiring up the application's module `DbContext`s to MariaDB. That is INFRA-26 (post-INFRA-23 provider decision).
- Schema *change* migrations after the initial bootstrap. The naming convention (`NNNN_<description>.sql`) supports this; the first change-migration will be authored when needed.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-24-summary.md` per `Progress/_template.md`. Include:

- Per-table comparison of SQLite vs MariaDB column types/FKs/indexes (the audit table).
- `SHOW TABLES` output from a fresh MariaDB instance after bootstrap.
- `SELECT * FROM __SchemaMigrations` after first launch and after second launch (proving idempotency).
- A demonstration of drift detection: change one byte in a script, re-launch, capture the abort log.
- Seed-row counts.

### Documentation

- `MerchSys.Infrastructure/Data/Migrations/Central/README.md` documents the naming convention (`NNNN_<description>.sql`), the idempotency rules, the hash-drift abort policy, and the rule that scripts are append-only (never edit a shipped script — author a new numbered script for any change).
- Header comments on each `.sql` script reference INFRA-24 and the source SQLite migration (if any).
- XML doc on `MariaDbSchemaInitializer.Initialize` documenting the abort-on-drift contract.
