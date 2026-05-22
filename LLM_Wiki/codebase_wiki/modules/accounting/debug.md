---
type: layer-manifest
module: MerchSys.Accounting
layer: debug
last-updated: 2026-05-22
---

# Accounting — Debug Layer

This layer contains `#If DEBUG`-gated harnesses and runners used for integration testing, schema verification, and developer-only diagnostic tools. These components are excluded from production builds.

## Components

| File | Type | Description |
|---|---|---|
| `Debug/VatLedgerSchemaHarness.vb` | Harness | Multi-check schema verification targeting isolated SQLite databases. Verifies migrations, constraints, and cascades. |
| `Debug/VatLedgerSchemaHarnessRunner.vb` | Runner | Entry point that instantiates the schema harness and generates Markdown reports in `%TEMP%`. |
| `Debug/VatTileSmokeHarness.vb` | Harness | Smoke test for the VAT dashboard tile, verifying KPI calculation logic against transient data. |

## Implementation Patterns

- **Conditional Compilation**: All files in this directory are wrapped in `#If DEBUG ... #End If`.
- **Isolated Databases**: Schema harnesses use `%TEMP%` located SQLite databases to avoid interfering with developer or production data.
- **Markdown Reporting**: Results are typically output as Markdown files for easy review in the IDE or external viewers.
- **BC36943 Pattern**: Uses capture-then-await in `Try...Catch` blocks to handle cleanup of temporary files regardless of success/failure.

## Verification Queries (SQLite)

The following queries are used by the schema harness to verify the live database state:

| Query | Purpose |
|---|---|
| `PRAGMA table_info('Acc_VatReturns')` | Enumerate columns created by migrations. |
| `PRAGMA index_list('Acc_VatReturns')` | List all indexes with uniqueness and partial flags. |
| `PRAGMA foreign_key_list('Acc_VatReturnLines')` | Confirm `ON DELETE CASCADE` declaration. |
| `SELECT MigrationId FROM __EFMigrationsHistory` | Verify each migration applied exactly once. |
