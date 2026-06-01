---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-15
plan-ref: Plans/VISTA_Modules/Accounting/13-vat-ledger-schema-verification.md
status: completed
---

## Task Summary

Implemented the VAT Ledger Schema Verification harness (ACC-13) — a `#If DEBUG`-gated pair of files that closes the three verification gaps flagged by the 2026-05-11 Accounting audit against the ACC-10 acceptance criteria.

**Plan:** `[[13-vat-ledger-schema-verification]]`

## What Was Done

- Created `src/MerchSys.Accounting/Debug/VatLedgerSchemaHarness.vb` — four-check schema verification harness targeting isolated `%TEMP%` SQLite databases.
- Created `src/MerchSys.Accounting/Debug/VatLedgerSchemaHarnessRunner.vb` — thin static entry point (`Module`) that instantiates the harness, collects the report, and writes Markdown to `%TEMP%\vat-ledger-schema-report-<timestamp>.md`.

## Sample CheckResult Entries (expected at runtime)

| Check | Expected Passed | Expected Detail |
|---|---|---|
| FreshDatabaseApplyResult | `True` | Tables OK. Unique index `IX_Acc_VatReturns_Year_Period_PeriodType_FormType_Active` confirmed (partial: FilingStatus!=3). DRIFT noted (see below). |
| ExistingDatabaseApplyResult | `True` | Idempotency OK. Seeded row preserved (count=1). 4 migration(s) each applied exactly once. |
| DuplicateFilingBlockedResult | `True` | Constraint enforced. SqliteErrorCode=19 (SQLITE_CONSTRAINT). First row preserved (committed count=1). |
| CascadeDeleteResult | `True` | EF Core cascade OK (orphan count=0). Schema-level cascade OK (raw SQL DELETE with foreign_keys=ON, orphan count=0). FK confirmed via PRAGMA. |

## Schema Drift Between ACC-10 Plan and Actual SQLite Schema

Two discrepancies exist between the ACC-10 plan description and the actual SQLite schema. These are informational — the schema is correct; the plan text was written before ACC-11 refined it.

| Aspect | ACC-10 Plan Description | Actual Schema (post-migrations) |
|---|---|---|
| Index name | `IX_Acc_VatReturns_PeriodYear_PeriodMonth_ReturnType` | `IX_Acc_VatReturns_Year_Period_PeriodType_FormType_Active` |
| Index column count | 3 columns (`PeriodYear`, `PeriodMonth`, `ReturnType`) | 4 columns (`Year`, `Period`, `PeriodType`, `FormType`) |
| Index type | Full unique index | Partial unique index `WHERE FilingStatus != 3` (added by `FixVatReturnAmendedIndex`) |
| Property names in entity | `PeriodYear`, `PeriodMonth`, `ReturnType` (string) | `Year`, `Period`, `FormType` (enum `VatReturnFormType`) |

The ACC-13 plan spec itself preserves the ACC-10 language. The harness was implemented against the actual schema, not the plan's description, so all four checks correctly target the live column and index names.

## PRAGMA Queries Used

| Query | Purpose | Columns Returned |
|---|---|---|
| `PRAGMA table_info('Acc_VatReturns')` | Enumerate columns created by migration | `cid, name, type, notnull, dflt_value, pk` |
| `PRAGMA table_info('Acc_VatReturnLines')` | Enumerate line table columns | same |
| `PRAGMA index_list('Acc_VatReturns')` | List all indexes with uniqueness flag | `seq, name, unique, origin, partial` |
| `PRAGMA foreign_key_list('Acc_VatReturnLines')` | Confirm `ON DELETE CASCADE` declaration | `id, seq, table, from, to, on_update, on_delete, match` |
| `SELECT MigrationId FROM __EFMigrationsHistory` | Verify each migration applied exactly once | `MigrationId` |

## Implementation Notes

- **No `IDbContextFactory` at runtime**: `AccountingDbContext` is registered via `AddDbContext` (not `AddDbContextFactory`) in `DatabaseConfig.vb`. The harness constructor accepts `IDbContextFactory(Of AccountingDbContext)` per spec for DI compatibility, but creates all scratch contexts via `DbContextOptionsBuilder` directly. The runner passes `Nothing` to the constructor since no check calls the factory.
- **Await-in-Catch restriction (BC36943)**: All checks follow the capture-then-await pattern — exceptions are stored in a local `runError` variable inside the `Try` block, and cleanup (scratch file deletion) runs synchronously after the `Try...End Try`.
- **Toast notifications**: `Notification.Wpf` is not available in `MerchSys.Accounting`. The runner outputs to `Console.WriteLine` with pass/fail count; the calling `MerchSys.App` layer is responsible for surfacing a toast after the runner returns.
- **Comment in With-block bug (BC30985)**: VB.NET does not allow inline `'` comments inside `New ... With { }` object initializers. One such comment was written and immediately caught by the build — moved outside the initializer block.
- **Partial index and Check 3**: The duplicate-filing constraint only fires when `FilingStatus != 3` (Amended). Both test rows use `FilingStatus.Generated (1)`, so the constraint applies correctly. Amended rows bypass the index by design (ACC-11 AmendReturnAsync).
- **Check 4 raw SQL isolation**: The raw SQL DELETE in Check 4 Part B uses a separate `SqliteConnection` (not the EF Core context's connection) to issue `PRAGMA foreign_keys = ON` without interfering with the context's connection state.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | ✅ Passed (4/4 schema checks passed via developer menu) |

## Issues Encountered

- **Comment inside object initializer (BC30985)**: A `'` comment placed on a line between `.FormType` and `.FilingStatus` inside a `New VatReturn With { }` block caused a cascade of 8 parse errors. VB.NET's object initializer parser does not support inline comments within the `{ }` body. Resolution: move comments to the line immediately before the `With { }` block.
  - Agent Wiki entry: not created (straightforward VB.NET syntax restriction, already documented in `feedback_vbnet_await_catch.md` pattern context).

## Codebase Wiki Discrepancies

- `codebase_wiki/modules/accounting/index.md` lists `plans-completed` without ACC-13 — expected, this is a new completion.
- No existing `Debug/` layer manifest exists in the codebase wiki for `MerchSys.Accounting`; the `VatTileSmokeHarness.vb` precedent is also not indexed there. Antigravity should add a `debug` layer entry covering both smoke and schema harnesses.

## What's Next

- [x] Wire `VatLedgerSchemaHarnessRunner.RunAndReportAsync` to a developer-only menu item in `MerchSys.App` (similar to how INT-13 wired the VAT tile smoke harness). *(completed in ACC-17)*
- [x] Run the harness once against the production dev database to produce an actual `CheckResult` output and confirm all four checks pass. *(completed/verified in Operator checklist)*
- [ ] Consider adding `IDbContextFactory(Of AccountingDbContext)` registration to `DatabaseConfig.AddModuleDbContexts` if future harnesses need factory-based multi-instance patterns. *(deferred/future architectural consideration)*

## Cross-References

- Domain Wiki pages consulted: `[[concepts/bir-compliance]]`, `[[concepts/modular-monolith]]`
- Agent Wiki entries consulted: `[[feedback_vbnet_await_catch]]`
- Plans consulted: `[[ACC-10]]` (schema origin), `[[ACC-11]]` (FixVatReturnAmendedIndex)
- Precedent harness: `MerchSys.Accounting/Debug/VatTileSmokeHarness.vb`, `MerchSys.POS/Debug/ReceiptSequenceHarnessReport.vb`
