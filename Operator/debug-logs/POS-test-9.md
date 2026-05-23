---
test-id: POS-16-Test-9
checklist: POS-verification-checklist.md
branch: debug/POS-test-9
started: 2026-05-23T00:00
status: resolved
---

# Debug Session — POS-16 Test 9

## Problem Statement

Verify that both archive tables (`Pos_OfficialReceiptArchive`, `Pos_ReceiptIntegrityArchive`)
reject UPDATE and DELETE at the database trigger level, returning `BIR-archive-immutable`.

**Test steps:**
1. Try `UPDATE Pos_OfficialReceiptArchive SET Status = 'test' WHERE Id = <id>;`
2. Try `DELETE FROM Pos_OfficialReceiptArchive WHERE Id = <id>;`
3. Repeat both for `Pos_ReceiptIntegrityArchive`.
4. Expected: all four statements fail with `BIR-archive-immutable`.

## Starting State
- **Commit:** `13ff755`
- **Build status:** clean
- **Relevant objects:**
  - Trigger `pos_receipt_archive_no_update` on `Pos_OfficialReceiptArchive`
  - Trigger `pos_receipt_archive_no_delete` on `Pos_OfficialReceiptArchive`
  - Trigger `pos_integrity_archive_no_update` on `Pos_ReceiptIntegrityArchive`
  - Trigger `pos_integrity_archive_no_delete` on `Pos_ReceiptIntegrityArchive`
- **Note:** Both archive tables were empty. One test row was inserted into each
  before firing the blocked operations. Because DELETE is itself blocked,
  those rows are permanent (by design — this proves the trigger works).

## Allowed Files
N/A — verification-only test, no code changes needed.

### Off-limits (do NOT touch)
N/A

---

## Attempt Log

### Attempt 1 — Verification run (no fix needed)

Triggers confirmed present:
```sql
CREATE TRIGGER pos_receipt_archive_no_update  BEFORE UPDATE ON "Pos_OfficialReceiptArchive"  BEGIN SELECT RAISE(ABORT, 'BIR-archive-immutable'); END
CREATE TRIGGER pos_receipt_archive_no_delete  BEFORE DELETE ON "Pos_OfficialReceiptArchive"  BEGIN SELECT RAISE(ABORT, 'BIR-archive-immutable'); END
CREATE TRIGGER pos_integrity_archive_no_update BEFORE UPDATE ON "Pos_ReceiptIntegrityArchive" BEGIN SELECT RAISE(ABORT, 'BIR-archive-immutable'); END
CREATE TRIGGER pos_integrity_archive_no_delete BEFORE DELETE ON "Pos_ReceiptIntegrityArchive" BEGIN SELECT RAISE(ABORT, 'BIR-archive-immutable'); END
```

Test results (test row Id = 1 in both tables):

| # | Operation | Table | Exit | Error |
|---|-----------|-------|------|-------|
| 1 | UPDATE | Pos_OfficialReceiptArchive | 1 | BIR-archive-immutable |
| 2 | DELETE | Pos_OfficialReceiptArchive | 1 | BIR-archive-immutable |
| 3 | UPDATE | Pos_ReceiptIntegrityArchive | 1 | BIR-archive-immutable |
| 4 | DELETE | Pos_ReceiptIntegrityArchive | 1 | BIR-archive-immutable |

Rows remained intact after all 4 attempts (row count = 1 each).

- **Verdict:** ✅ all 4 triggers working as expected
- **Action:** no commit needed

---

## Resolution

- **Status:** resolved
- **Root cause:** N/A — all triggers already correctly implemented.
- **Fix description:** No fix required. All 4 BEFORE UPDATE/DELETE triggers on both
  archive tables call `RAISE(ABORT, 'BIR-archive-immutable')` unconditionally.
  Unlike the live table trigger (`pos_receipts_no_delete`), these carry no archival-session
  exception — archive records can never be modified or removed by anyone.
- **Final commit:** no new commit (no code changed)
- **Agent wiki entry needed?** no
