---
test-id: POS-13-Test-2
checklist: POS-verification-checklist.md
branch: debug/POS-test-2
started: 2026-05-23T00:00
status: in-progress
---

# Debug Session — POS-13 Test 2

## Problem Statement

The checklist instructs the operator to invoke:
```
? Await Pos_SequenceConcurrencyHarness.RunAsync()
```
(no arguments) from the Immediate Window.

However, the harness method signature is:
```vb
Public Async Function RunAsync(connectionString As String) As Task
```
This call would fail with a compile error — missing required argument. The harness has no no-arg overload.

Additionally, a no-arg overload must NOT use the live `%LOCALAPPDATA%\MerchSys\merchsys.db` database, because 1000 parallel `GetNextReceiptNumberAsync` calls would advance the live `Pos_ReceiptSequences` counter by 1000, corrupting real receipt numbering. It must use a temp scratch SQLite file.

## Starting State
- **Commit:** `aeef4db`
- **Build status:** clean — 0 errors, 0 warnings
- **Relevant files:**
  - `WPF_Applications\MerchSys\src\MerchSys.POS\Tests\Pos.SequenceConcurrencyHarness.vb`

## Allowed Files
- `WPF_Applications\MerchSys\src\MerchSys.POS\Tests\Pos.SequenceConcurrencyHarness.vb` — the harness itself; add the no-arg overload here

### Off-limits (do NOT touch)
- `SharedKernel/Events/*` — shared contracts, not the bug source
- `ReceiptIntegrityService.vb` — not the bug source; the concurrency logic there is sound
- Other modules' services/handlers

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Add a no-arg `RunAsync()` overload inside the `#If DEBUG` block that creates a temp SQLite file, delegates to `RunAsync(connectionString)`, and cleans up afterward. This matches the checklist call signature without touching the live DB.
- **Changed:** `Pos.SequenceConcurrencyHarness.vb` — added no-arg overload
- **Build result:** TBD
- **Runtime result:** TBD
- **Verdict:** TBD
- **Action:** TBD

---

## Resolution

- **Status:** in-progress
- **Root cause:** No-arg overload missing from harness; original signature requires caller to supply a connection string.
- **Fix description:** Add `Public Async Function RunAsync() As Task` that creates a temp `.db` file, delegates to `RunAsync(connStr)`, and deletes the temp file in a Finally block.
- **Final commit:** TBD
- **Agent wiki entry needed?** no — straightforward overload addition
