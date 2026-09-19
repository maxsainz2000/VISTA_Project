---
type: error-fix
module: Infrastructure
agent: claude-code
date: 2026-06-11
tags: [mariadb, schema-migration, schema-drift, sha256, startup, ddl, idempotent-migrations, runtime-error]
error-code: SchemaDrift
severity: runtime-error
---

## Problem

After INT-21 edited the column types of `Acc_TamperAuditLog` directly inside `0001_initial_schema.sql`, the app aborted at startup on any database that had already been initialized:

```
VISTA — Fatal
Cannot initialize database schema: FATAL: Schema drift detected in
script '0001_initial_schema.sql'. Aborting startup.
```

The build was 0/0 and a fresh database would have worked — the failure only hits **existing** installs.

## Root Cause

`MariaDbSchemaInitializer` (`MerchSys.App/Data/MariaDbSchemaInitializer.vb`) is a hash-guarded migration runner. For every embedded `*.sql` script it computes a SHA-256 of the file content and compares it to the hash stored in `__SchemaMigrations` when the script was first applied:

```vb
If dbHash <> currentHash Then
    Throw New InvalidOperationException(
        $"FATAL: Schema drift detected in script '{fileName}'. Aborting startup.")
```

This is a deliberate **anti-tamper** feature. Any byte change to a script that was already applied is treated as tampering and aborts launch. So editing `0001` in place — even a legitimate column-type fix — guarantees a fatal drift on every existing DB. (The INT-21 summary's plan to run a manual `ALTER` afterward was moot: the app crashes before it can reach a console.)

## Fix

Never modify an applied migration. **Revert** the edited script to its original content (restoring the recorded hash) and add the change as a **new forward migration**:

1. `git checkout <pre-commit> -- 0001_initial_schema.sql` — restore byte-for-byte, hash matches again.
2. New `0008_tamper_audit_column_types.sql`:

```sql
ALTER TABLE `Acc_TamperAuditLog`
    MODIFY COLUMN `Id` BIGINT NOT NULL AUTO_INCREMENT,
    MODIFY COLUMN `ReceiptId` BIGINT NOT NULL,
    MODIFY COLUMN `ExpectedValue` VARCHAR(512) NULL,
    MODIFY COLUMN `ActualValue` VARCHAR(512) NULL;
```

Numeric-prefixed scripts sort before the `Add*` scripts (the runner does `OrderBy(resourceName)`), and the `Central\*.sql` wildcard in `MerchSys.App.vbproj` auto-embeds new files — so `0008` is picked up with no project edit. This converges both fresh installs (0001 creates, 0008 aligns) and existing DBs (0001 untouched, 0008 applied as pending) with no manual DB surgery.

## Prevention

- **An applied migration is immutable.** To change schema, always add the next-numbered `NNNN_*.sql`. This is the same discipline the runner enforces at runtime.
- Make new migrations idempotent where practical (`CREATE TABLE IF NOT EXISTS`, `MODIFY COLUMN`) so re-running against a partially-migrated DB is safe.
- When a fix touches a table's DDL, ask "was the creating script already applied?" If yes (anything in `0001`–prior), it needs a new forward script, not an in-place edit.
- This applies equally to EF-config/column-type alignments: change the EF mapping **and** ship a forward `ALTER` migration; don't rewrite the original `CREATE TABLE`.

## Related

- `[[efcore-vat-ledger-columns-missing-central-schema]]` — other post-pivot central-schema alignment via a forward script
- `[[mariadb-raw-sql-sqlite-dialect-leakage]]` — sibling SQLite→MariaDB DDL cleanup
- Domain: `[[centralized-database-architecture]]`
