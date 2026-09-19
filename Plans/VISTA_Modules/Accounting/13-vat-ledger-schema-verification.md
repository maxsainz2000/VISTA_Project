---
module: MerchSys.Accounting
plan-id: ACC-13
title: "VAT Ledger Schema Verification"
depends-on: [ACC-10]
estimated-files: 2
---

# VAT Ledger Schema Verification

## Context

ACC-10 introduced the `Acc_VatReturns` and `Acc_VatReturnLines` tables via the `AddVatLedgerColumns` migration, including a composite unique index on `(PeriodYear, PeriodMonth, ReturnType)` to prevent duplicate filings and a cascade-delete relationship from `VatReturn` to its lines. The 2026-05-11 Accounting audit flagged three verification gaps in the ACC-10 progress summary that were never closed:

1. The migration has not been confirmed to apply cleanly against both a **fresh** and an **existing** SQLite database.
2. The composite unique index has not been validated to actually block duplicate filings at runtime.
3. The cascade-delete behaviour from `VatReturn` to `VatReturnLines` has not been verified.

This plan exists to close those three gaps with a small, deterministic verification harness rather than leaving them as drift between the implementation and the acceptance criteria of ACC-10. No production schema is changed; this plan only adds verification artefacts.

## Prerequisites

- **ACC-10** (Accounting VAT Ledger Schema Extension) — `AddVatLedgerColumns` migration, `VatReturn`, `VatReturnLines`, `AccountingDbContext`
- **INFRA-03** (Database Contexts) — `AccountingDbContext` and SQLite provider registration
- **INFRA-04** (MediatR Event Bus) — not required at runtime, but the harness runs inside the same DI host

## Wiki References

- `concepts/bir-compliance.md` — BIR VAT return uniqueness rules (one filing per period per form type)
- `concepts/modular-monolith.md` — module-private `DbContext` boundary; harness must not reach into other modules

## Deliverables

```
MerchSys.Accounting/Debug/
├── VatLedgerSchemaHarness.vb
└── VatLedgerSchemaHarnessRunner.vb
```

Both files live under `MerchSys.Accounting/Debug/` to match the existing precedent set by POS-13's `Pos_SequenceConcurrencyHarness.vb`. No production code paths reference these files; they are invoked manually or by a developer-only menu item gated by `#If DEBUG`.

## Specification

### VatLedgerSchemaHarness

```
Public Class VatLedgerSchemaHarness

    Private ReadOnly _contextFactory As IDbContextFactory(Of AccountingDbContext)

    Public Sub New(contextFactory As IDbContextFactory(Of AccountingDbContext))
        _contextFactory = contextFactory
    End Sub

    Public Async Function RunAllAsync() As Task(Of VatLedgerSchemaReport)
End Class

Public Class VatLedgerSchemaReport
    Public Property FreshDatabaseApplyResult As CheckResult
    Public Property ExistingDatabaseApplyResult As CheckResult
    Public Property DuplicateFilingBlockedResult As CheckResult
    Public Property CascadeDeleteResult As CheckResult
End Class

Public Class CheckResult
    Public Property Passed As Boolean
    Public Property Detail As String
    Public Property DurationMs As Long
End Class
```

`RunAllAsync()` performs the four checks in order:

#### Check 1 — Fresh database apply
1. Create a scratch SQLite file path under `%TEMP%\vista-vat-schema-harness-<guid>.db`.
2. Build an `AccountingDbContext` against that path.
3. Call `Database.MigrateAsync()`.
4. Pass criterion: the `Acc_VatReturns` and `Acc_VatReturnLines` tables exist with the columns defined in ACC-10; the unique index `IX_Acc_VatReturns_PeriodYear_PeriodMonth_ReturnType` exists and is marked unique.
5. Inspect via `Database.SqlQueryRaw(Of TableInfoRow)("PRAGMA table_info('Acc_VatReturns')")` and `PRAGMA index_list('Acc_VatReturns')`.
6. Delete the scratch file in a `Finally`.

#### Check 2 — Existing database apply (idempotency)
1. Reuse the same scratch path; migrate once.
2. Seed one `VatReturn` row.
3. Call `Database.MigrateAsync()` a second time.
4. Pass criterion: no exception thrown; the seeded row still exists; `__EFMigrationsHistory` shows the migration applied exactly once.

#### Check 3 — Duplicate filing blocked
1. On a fresh scratch DB.
2. Insert `VatReturn { PeriodYear=2026, PeriodMonth=5, ReturnType="2550M" }`.
3. Attempt to insert a second row with the same composite key.
4. Pass criterion: `SaveChangesAsync` throws `DbUpdateException` whose inner exception is a `SqliteException` with `SqliteErrorCode = 19` (constraint) and message containing `Acc_VatReturns` and one of the indexed columns.
5. The first row remains in the table; the second is not persisted.

#### Check 4 — Cascade delete
1. On a fresh scratch DB.
2. Insert one `VatReturn` with three `VatReturnLines`.
3. Remove the `VatReturn` via `Context.Remove(vatReturn)` followed by `SaveChangesAsync`.
4. Pass criterion: `Acc_VatReturnLines` row count for that parent is `0`; the parent row is gone.
5. Repeat with raw SQL `DELETE FROM Acc_VatReturns WHERE Id = @id` to verify the FK is declared `ON DELETE CASCADE` at the schema level, not just by EF Core's in-memory cascade. Inspect via `PRAGMA foreign_key_list('Acc_VatReturnLines')`.

### VatLedgerSchemaHarnessRunner

A thin static entry point invoked from a developer-only menu item. Writes a Markdown summary to `%TEMP%\vat-ledger-schema-report-<timestamp>.md` and surfaces a `Notification.Wpf` toast with the pass/fail count. Does not throw on failure; the report is the deliverable.

```
Public Module VatLedgerSchemaHarnessRunner
    Public Async Function RunAndReportAsync(host As IHost) As Task
End Module
```

## Implementation Notes

- The harness creates and disposes its own `AccountingDbContext` instances via `IDbContextFactory(Of AccountingDbContext)` so the production application-scoped context is never mutated.
- All scratch databases live under `%TEMP%` and are cleaned up in `Finally` blocks; a stale lock should not block subsequent runs.
- The PRAGMA-based introspection is preferred over EF Core model snapshot inspection because it verifies what SQLite actually created, not what EF *intended* to create.
- The harness does **not** test the MariaDB equivalent — that is the responsibility of INFRA-06's `mariadb-init.sql` verification (tracked separately in the Infrastructure audit).
- Per project policy (CLAUDE.md), no test project is created; this is a debug harness, not an xUnit suite.
- The `#If DEBUG` gate is mandatory on the runner; release builds must not ship a one-click "wipe my dev DB" entry point.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. Both files exist under `MerchSys.Accounting/Debug/` and are excluded from release builds via `#If DEBUG`.
3. `VatLedgerSchemaHarness.RunAllAsync()` returns a `VatLedgerSchemaReport` with four populated `CheckResult` entries.
4. Each check captures its duration in milliseconds.
5. Check 3's failure path correctly distinguishes a unique-constraint violation from any other `DbUpdateException` (asserts SQLite error code 19, not just any exception).
6. Check 4 verifies the FK declaration via `PRAGMA foreign_key_list`, not only the EF Core delete behaviour.
7. The runner produces a Markdown report at the documented path and emits a toast notification.
8. The harness leaves no scratch `.db` files behind on a clean run.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-13-summary.md` using `Progress/_template.md`. Include the four `CheckResult` entries from a sample harness run, and note any drift between the ACC-10 migration as written and the actual SQLite schema.

### Documentation
- XML doc comments on `VatLedgerSchemaHarness` and `CheckResult` describing what each check protects against.
- Inline comment in Check 3 explaining why SQLite error code 19 is the specific assertion target.
- A short section in the implementation summary listing the exact PRAGMA queries used, for future agents auditing schema state.
