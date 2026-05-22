---
test-id: ACC-Test-9
checklist: ACC-verification-checklist.md
branch: debug/ACC-test-9
started: 2026-05-22T14:00
status: in-progress
---

# Debug Session — ACC Test 9

## Problem Statement

Test 9 asks the operator to run:
```
? Await VatTileSmokeHarness.RunAsync(host)
```
in the Visual Studio Immediate Window and verify:
- `ComputedVatPayable = 9000`
- `NavigationRouteFound = True`

The `VatTileSmokeHarness` class exists but has never been run. Pre-analysis found 4 bugs:

1. `Await db.Database.MigrateAsync()` — EF Core 10 VB.NET migration discovery bug; tables never created → seed data throws SQLite "no such table" error.
2. `ISyncableRepository(Of AccountingDbContext)` not registered in the isolated DI container → `FinancialOverviewService` and `VatReportingService` cannot be constructed.
3. `RunAsync` is an instance method — the test calls it as `VatTileSmokeHarness.RunAsync(host)` (shared/static call syntax) → VB.NET compile/runtime error.
4. `Type.GetType("Views.Accounting.VatReturnView, MerchSys.App")` — missing root namespace prefix; should be `"MerchSys.App.Views.Accounting.VatReturnView, MerchSys.App"` → `NavigationRouteFound` always `False`.

## Starting State
- **Commit:** `1cca7c6`
- **Build status:** clean (0 errors, 0 warnings assumed from prior sessions)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.Accounting/Debug/VatTileSmokeHarness.vb`

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.Accounting/Debug/VatTileSmokeHarness.vb` — all 4 bugs are here

### Off-limits (do NOT touch)
- `SharedKernel/` — shared contracts
- Other modules' services/handlers
- `FinancialOverviewService.vb` — bug is in the harness, not the service
- `VatReportingService.vb` — same reason

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Fix all 4 bugs in `VatTileSmokeHarness.vb`:
  1. Replace `MigrateAsync` with raw-SQL `SetupScratchSmokeSchema` (mirrors the pattern from `VatLedgerSchemaHarness`).
  2. Add a `SmokeSyncableRepository` nested class that implements `ISyncableRepository(Of AccountingDbContext)` and delegates `SaveChangesWithJournalAsync` to `db.SaveChangesAsync()`. Register it in the isolated DI.
  3. Make `RunAsync` a `Shared` method.
  4. Fix reflection type name to `"MerchSys.App.Views.Accounting.VatReturnView, MerchSys.App"`.
- **Changed:** `WPF_Applications/MerchSys/src/MerchSys.Accounting/Debug/VatTileSmokeHarness.vb` — all 4 bugs fixed in one edit (all in same file, all required for harness to function)
- **Build result:** ✅ clean — 0 errors, 0 warnings
- **Runtime result:** Operator must run `? Await VatTileSmokeHarness.RunAsync(host)` in VS Immediate Window to confirm `ComputedVatPayable = 9000` and `NavigationRouteFound = True`
- **Verdict:** ✅ fixed (build clean; runtime verification pending operator)
- **Action:** committed as `ce01032`, merged to master

---

### Attempt 2
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:** ✅ / ❌ / ⚠️
- **Action:** committed / reverted

---

### Attempt 3
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:** ✅ / ❌ / ⚠️
- **Action:** committed / reverted

---

### Attempt 4
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:** ✅ / ❌ / ⚠️
- **Action:** committed / reverted

---

### Attempt 5
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:** ✅ / ❌ / ⚠️
- **Action:** committed / reverted

> **⛔ HARD STOP** — If all 5 attempts failed, STOP here. Write the resolution section below and escalate to the operator.

---

## Resolution

<!-- Fill this in when the bug is fixed OR when you hit the 5-attempt limit -->

- **Status:** resolved (build verified; operator runtime check pending)
- **Root cause:** `VatTileSmokeHarness` was written but never run; contained 4 bugs: (1) `MigrateAsync` silently skips VB.NET migrations so tables are never created; (2) `ISyncableRepository(Of AccountingDbContext)` not registered in isolated DI; (3) `RunAsync` was an instance method, not `Shared`; (4) reflection type name missing root namespace prefix.
- **Fix description:** Replaced `MigrateAsync` with `SetupScratchSmokeSchema` raw SQL; added `SmokeSyncableRepository` nested class and DI registration; made `RunAsync` `Shared`; fixed `Type.GetType` string to include `MerchSys.App.` prefix.
- **Final commit:** `ce01032`
- **Agent wiki entry needed?** no — bugs 1 and 4 are already covered by `efcore10-vbnet-migration-discovery-bug.md` and `wpf-mainwindow-not-shell-window.md`. Bug 2 (missing DI registration for isolated harness) is a one-off harness authoring error.
