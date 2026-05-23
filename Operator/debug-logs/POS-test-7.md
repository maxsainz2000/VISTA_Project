---
test-id: POS-16-Test-7
checklist: POS-verification-checklist.md
branch: debug/POS-test-7
started: 2026-05-23T00:00
status: in-progress
---

# Debug Session — POS-16 Test 7

## Problem Statement

Verify that `ReceiptArchivalService` rolls back cleanly when the archive-insert step fails,
leaving the live `Pos_OfficialReceipts` table completely untouched.

The service transaction order is:
1. Query eligible receipts
2. `db.OfficialReceiptArchives.Add(...)` + `SaveChangesAsync()` — INSERT into archive
3. Set `Pos_ArchivalSession` flag
4. DELETE source rows from live tables
5. Clear session flag → Commit

If step 2 fails (archive table missing), the catch block calls `RollbackAsync()` and
re-throws. Steps 3–5 never execute, so no rows are deleted from the live table.

Approach: add `RunRollbackTestAsync()` — seeds 20 expired receipts, **drops**
`Pos_OfficialReceiptArchive` to force the insert to fail, runs archival (expects an
exception), then verifies `Pos_OfficialReceipts` still has 20 rows.

## Starting State
- **Commit:** `22a921e`
- **Build status:** clean — 0 errors, 0 warnings
- **Relevant files:**
  - `WPF_Applications\MerchSys\src\MerchSys.POS\Debug\ReceiptArchivalHarness.vb`
  - `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb`

## Allowed Files
- `WPF_Applications\MerchSys\src\MerchSys.POS\Debug\ReceiptArchivalHarness.vb` — add rollback test
- `WPF_Applications\MerchSys\src\MerchSys.App\Views\Debug\DebugMenuExtensions.vb` — add Dev menu button

### Off-limits (do NOT touch)
- `SharedKernel/Events/*`
- Other modules' services/handlers
- `ReceiptArchivalService.vb` unless rollback logic is wrong

---

## Attempt Log

### Attempt 1
- **Hypothesis:** Add `RunRollbackTestAsync()` — seeds 20 expired receipts, drops
  `Pos_OfficialReceiptArchive` to force a SaveChanges failure, runs archival (catching the
  expected exception), then asserts LiveRemaining=20 (rollback kept live table intact).
- **Changed:**
  - `ReceiptArchivalHarness.vb` — add `RunRollbackTestAsync()`
  - `DebugMenuExtensions.vb` — add "Run Rollback Test" button
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

---

### Attempt 2
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

---

### Attempt 3
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

---

### Attempt 4
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

---

### Attempt 5
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:**
- **Action:**

> **⛔ HARD STOP** — If all 5 attempts failed, STOP here.

---

## Resolution

- **Status:** in-progress
- **Root cause:**
- **Fix description:**
- **Final commit:**
- **Agent wiki entry needed?**
