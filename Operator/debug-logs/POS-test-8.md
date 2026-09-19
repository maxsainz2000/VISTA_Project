---
test-id: POS-16-Test-8
checklist: POS-verification-checklist.md
branch: debug/POS-test-8
started: 2026-05-23T00:00
status: resolved
---

# Debug Session — POS-16 Test 8

## Problem Statement

Verify that the BIR immutability trigger on `Pos_OfficialReceipts` blocks direct DELETE statements, returning a `BIR-immutable` error.

**Test steps:**
1. Open DB Browser for SQLite (or sqlite3 CLI).
2. Run: `DELETE FROM Pos_OfficialReceipts WHERE Id = <any id>;`
3. Expected: DELETE fails with `BIR-immutable` trigger error.

## Starting State
- **Commit:** `1333cf2`
- **Build status:** clean
- **Relevant files:**
  - `Pos_OfficialReceipts` table (trigger: `pos_receipts_no_delete`)

## Allowed Files
N/A — verification-only test, no code changes needed.

### Off-limits (do NOT touch)
N/A

---

## Attempt Log

### Attempt 1 — Verification run (no fix needed)
- **Hypothesis:** Trigger `pos_receipts_no_delete` should fire and raise `BIR-immutable` on any DELETE outside an archival session.
- **Changed:** none
- **Test command:**
  ```
  sqlite3 $db "DELETE FROM Pos_OfficialReceipts WHERE Id = 211;"
  ```
- **Runtime result:**
  ```
  Error in 2nd command line argument: BIR-immutable
  Exit code: 1
  ```
- **Verdict:** ✅ fixed (trigger already works as expected)
- **Action:** no commit needed

---

## Resolution

- **Status:** resolved
- **Root cause:** N/A — trigger was already correctly implemented.
- **Fix description:** No fix required. The `pos_receipts_no_delete` BEFORE DELETE trigger on `Pos_OfficialReceipts` calls `RAISE(ABORT, 'BIR-immutable')` when no archival session is active. Tested with `Id = 211`, DELETE failed with the expected error.
- **Trigger definition:**
  ```sql
  CREATE TRIGGER pos_receipts_no_delete
  BEFORE DELETE ON "Pos_OfficialReceipts"
  WHEN (SELECT COALESCE(
      (SELECT value FROM "Pos_ArchivalSession"
       WHERE key = 'archival_in_progress' AND expires_at > datetime('now')), 0) = 0)
  BEGIN SELECT RAISE(ABORT, 'BIR-immutable'); END
  ```
- **Final commit:** no new commit (no code changed)
- **Agent wiki entry needed?** no
