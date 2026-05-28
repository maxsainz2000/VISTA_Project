---
name: sqlite-trigger-no-temp-reference
type: error-fix
module: MerchSys.App
agent: claude-code
date: 2026-05-15
tags: [sqlite, triggers, temp-table, BC30456, runtime-error, archival]
status: historical
historical-as-of: 2026-05-28
historical-reason: SQLite removed from architecture per system_plan_amendment_2026-05-28.md
---

> **⚠️ Historical as of 2026-05-28.** SQLite is being removed from the architecture (see `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md`). This error and its fix only apply to legacy SQLite code that has not yet been deleted. Do not write new SQLite triggers.

## Error

```
Microsoft.Data.Sqlite.SqliteException
Message=SQLite Error 1: 'trigger pos_receipts_no_delete cannot reference objects in database temp'.
```

Thrown from `DatabaseInitializer.ApplyAddReceiptIntegrityArchive` at startup when applying the
migration that creates the `pos_receipts_no_delete` trigger.

## Root Cause

SQLite does not allow trigger WHEN clauses (or trigger bodies) to reference objects in the
`temp` database. The original trigger read from `temp.archival_session`:

```sql
CREATE TRIGGER pos_receipts_no_delete
BEFORE DELETE ON "Pos_OfficialReceipts"
WHEN (SELECT COALESCE((SELECT value FROM temp.archival_session
      WHERE key = 'archival_in_progress'), 0) = 0)
BEGIN SELECT RAISE(ABORT, 'BIR-immutable'); END
```

The intent was to use a session-scoped TEMP TABLE so the archival session flag would auto-clear
on connection close (crash-safe). SQLite rejects this entirely — regardless of whether the
temp table exists at trigger-creation time.

## Fix

Replace `temp.archival_session` with a persistent table `Pos_ArchivalSession(key, value, expires_at)`
in the main database. Use a TTL column to preserve crash-safety: if the archival service
terminates mid-batch, the flag row's `expires_at` expires and the trigger re-engages.

### DatabaseInitializer.vb — `ApplyAddReceiptIntegrityArchive`

```vb
' Create the persistent session-flag table before the trigger.
Exec(conn,
    "CREATE TABLE IF NOT EXISTS ""Pos_ArchivalSession"" (" &
    """key"" TEXT NOT NULL PRIMARY KEY, " &
    """value"" INTEGER NOT NULL, " &
    """expires_at"" TEXT NOT NULL" &
    ")")

' Trigger reads Pos_ArchivalSession with TTL check.
Exec(conn, "DROP TRIGGER IF EXISTS pos_receipts_no_delete")
Exec(conn,
    "CREATE TRIGGER IF NOT EXISTS pos_receipts_no_delete " &
    "BEFORE DELETE ON ""Pos_OfficialReceipts"" " &
    "WHEN (SELECT COALESCE((SELECT value FROM ""Pos_ArchivalSession"" " &
    "WHERE key = 'archival_in_progress' AND expires_at > datetime('now')), 0) = 0) " &
    "BEGIN " &
    "SELECT RAISE(ABORT, 'BIR-immutable'); " &
    "END")
```

### ReceiptArchivalService.vb — `RunBatchAsync`

Remove the `CREATE TEMP TABLE` call. Replace the flag insert/clear with:

```vb
' Set flag with 5-minute TTL
Await db.Database.ExecuteSqlRawAsync(
    "INSERT OR REPLACE INTO ""Pos_ArchivalSession"" (key, value, expires_at) " &
    "VALUES ('archival_in_progress', 1, datetime('now', '+5 minutes'))",
    cancellationToken)

' ... perform deletes ...

' Clear flag
Await db.Database.ExecuteSqlRawAsync(
    "DELETE FROM ""Pos_ArchivalSession"" WHERE key = 'archival_in_progress'",
    cancellationToken)
```

## Rules

- **Never reference `temp.*` inside a SQLite trigger** — not in WHEN clauses, not in the
  trigger body. SQLite raises "cannot reference objects in database temp" unconditionally.
- For connection-scoped flags that triggers need to read, use a persistent table with a TTL
  column instead of TEMP TABLE. A 5-minute TTL is sufficient for batch operations.
- The `Pos_ArchivalSession` table is safe to query from triggers and is crash-safe via TTL.

## Related

- First encountered: INT-12 runtime harness execution, startup crash (2026-05-15)
- Affects: `DatabaseInitializer.ApplyAddReceiptIntegrityArchive`, `ReceiptArchivalService.RunBatchAsync`
