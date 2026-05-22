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
- **Hypothesis:** The harness is fully wired (button exists, nav group exists, DI registered). Run the app and execute the harness via the Developer Tools menu to verify it passes all four checks.
- **Changed:** (none — verification run)
- **Build result:** clean (0 errors, 0 warnings)
- **Runtime result:** pending operator test
- **Verdict:** pending
- **Action:** pending

---

## Resolution

- **Status:** in-progress
- **Root cause:** n/a
- **Fix description:** n/a
- **Final commit:** n/a
- **Agent wiki entry needed?** n/a
