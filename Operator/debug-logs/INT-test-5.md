---
test-id: INT-Test-5
checklist: INT-verification-checklist.md
branch: debug/INT-test-5
started: 2026-05-20T00:00
status: in-progress
---

# Debug Session — INT Test 5

## Problem Statement

Test 5 requires running the `EventChainVerificationHarness` from the Developer Tools menu.
The harness class exists (`MerchSys.App/Debug/EventChainVerificationHarness.vb`) and the
report writer exists (`MerchSys.App/Debug/EventChainReport.vb`), but the Developer Tools
panel (`DebugMenuView`) only has the VAT Schema Harness button. There is no button to
invoke the event chain harness.

## Starting State
- **Commit:** `bcb73f3`
- **Build status:** clean (0 errors, 0 warnings assumed from last known state)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.App/Views/Debug/DebugMenuExtensions.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.App/Debug/EventChainVerificationHarness.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.App/Debug/EventChainReport.vb`

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.App/Views/Debug/DebugMenuExtensions.vb` — the only file that needs a new button section

### Off-limits (do NOT touch)
- `SharedKernel/Events/*` — shared contracts
- `EventChainVerificationHarness.vb` — harness is complete; only needs wiring
- `EventChainReport.vb` — report writer is complete; only needs wiring
- All other module services/handlers

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Add a "Run Event Chain Harness" section and button to `DebugMenuView`,
  mirroring the VAT Schema Harness pattern. The button click handler instantiates
  `EventChainVerificationHarness(Nothing)` (IHost is stored but never used),
  calls both verify methods sequentially, writes the report, and shows a toast.
- **Changed:** `Views/Debug/DebugMenuExtensions.vb` — added event chain harness section
- **Build result:** (pending)
- **Runtime result:** (pending operator run)
- **Verdict:** (pending)
- **Action:** (pending)
