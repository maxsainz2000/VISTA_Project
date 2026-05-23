---
test-id: ACC-Test-6
checklist: ACC-verification-checklist.md
branch: debug/ACC-test-6
started: 2026-05-22T00:00
status: in-progress
---

# Debug Session — ACC Test 6

## Problem Statement

ACC-13 — VAT Ledger Schema Verification Harness.
Test 6: Run the schema harness from the developer menu.

The checkbox `[ ] Schema harness runs and all four checks pass` has not yet been verified.
Goal: launch the app in Debug, navigate to Developer Tools, click "Run VAT Schema Harness",
and confirm a MessageBox appears + a .md report is written to %TEMP% with all four checks passing.

## Starting State
- **Commit:** `d169972`
- **Build status:** clean — 0 errors, 0 warnings
- **Relevant files:**
  - `WPF_Applications\MerchSys\src\MerchSys.Accounting\Debug\VatLedgerSchemaHarness.vb` — four schema checks
  - `WPF_Applications\MerchSys\src\MerchSys.Accounting\Debug\VatLedgerSchemaHarnessRunner.vb` — runner / report writer
  - `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb` — button + click handler
  - `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` — nav group registration
  - `WPF_Applications\MerchSys\src\MerchSys.App\Startup\DebugServiceRegistration.vb` — DI registration

## Allowed Files
- `WPF_Applications\MerchSys\src\MerchSys.Accounting\Debug\VatLedgerSchemaHarness.vb` — harness logic
- `WPF_Applications\MerchSys\src\MerchSys.Accounting\Debug\VatLedgerSchemaHarnessRunner.vb` — runner
- `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb` — button wiring
- `WPF_Applications\MerchSys\src\MerchSys.App\ViewModels\MainWindowViewModel.vb` — nav registration
- `WPF_Applications\MerchSys\src\MerchSys.App\Startup\DebugServiceRegistration.vb` — DI registration

### Off-limits (do NOT touch)
- `SharedKernel/Events/*` — shared contracts
- Other modules' services/handlers

---

## Attempt Log

### Attempt 1
- **Hypothesis:** The harness is fully wired (button exists, nav group exists, DI registered). Run the app and execute the harness via the Developer Tools menu to confirm all four checks pass.
- **Changed:** (none — initial verification run to get baseline)
- **Build result:** clean (0 errors, 0 warnings)
- **Runtime result:** 0/4 checks failed. Check 1: all columns missing from both tables. Checks 2–4: DbUpdateException on insert. Root cause: `MigrateAsync()` on scratch AccountingDbContext does nothing — EF Core 10 cannot discover VB.NET migration classes (known bug).
- **Verdict:** ❌ failed
- **Action:** identified root cause; proceeded to Attempt 2

---

### Attempt 2
- **Hypothesis:** Replace `Await ctx.Database.MigrateAsync()` in `VatLedgerSchemaHarness` (6 call sites) with `SetupScratchSchema(scratchPath)` — a new raw-SQL helper that mirrors `DatabaseInitializer.ApplyVatLedgerColumns` + `ApplyFixVatReturnAmendedIndex`, using IF NOT EXISTS / INSERT OR IGNORE for idempotency (Check 2).
- **Changed:** `WPF_Applications\MerchSys\src\MerchSys.Accounting\Debug\VatLedgerSchemaHarness.vb` — replaced 6× `MigrateAsync()` calls; added `SetupScratchSchema` and `ExecSchema` helpers
- **Build result:** clean (0 errors, 0 warnings)
- **Runtime result:** 4/4 checks passed. Check 1: tables + partial unique index confirmed. Check 2: idempotency OK (2 migrations × 1 each). Check 3: SQLITE_CONSTRAINT code 19, committed count=1. Check 4: EF cascade + schema-level FK CASCADE both confirmed.
- **Verdict:** ✅ fixed
- **Action:** committed as `1cca7c6`

---

## Resolution

- **Status:** resolved
- **Root cause:** `MigrateAsync()` on a scratch `AccountingDbContext` silently creates empty tables (no columns) because EF Core 10 cannot discover VB.NET migration classes. This is the same bug documented in `efcore10-vbnet-migration-discovery-bug.md`. The harness was written using `MigrateAsync()` but the rest of the app uses raw SQL via `DatabaseInitializer`.
- **Fix description:** Added `SetupScratchSchema(scratchPath As String)` private helper that creates the VAT schema via raw `SqliteConnection` SQL, exactly matching what `DatabaseInitializer` does. All 6 `MigrateAsync()` call sites replaced.
- **Final commit:** `1cca7c6`
- **Agent wiki entry needed?** yes — `efcore-vbnet-migrateAsync-scratch-db` (extends existing pattern)
