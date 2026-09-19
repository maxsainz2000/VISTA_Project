---
type: error-fix
module: Infrastructure
agent: antigravity
date: 2026-05-29
tags: [ef-core, mariadb, sqlite, vb-net, sql-dialect, post-pivot-regression, runtime-error]
error-code: MySqlException-1064
severity: runtime-error
---

## Problem

After the 2026-05-28 pivot from SQLite to centralized MariaDB, raw SQL statements executed using EF Core's `ExecuteSqlRawAsync` or `ExecuteSqlInterpolatedAsync` threw syntax errors or behaved as no-ops. For example, in `ReceiptArchivalService.RunBatchAsync`, the following error occurs on runtime database execution:

```
MySqlConnector.MySqlException (0x80004005): You have an error in your SQL syntax; check the manual that corresponds to your MariaDB server version for the right syntax to use...
```

## Root Cause

During the SQLite-era, raw SQL statements were written using:
1. SQLite-specific dialect features, such as `INSERT OR REPLACE` and datetime functions (e.g. `datetime('now', '+5 minutes')`).
2. Double-quoted identifiers (e.g. `"Pos_ArchivalSession"`), which are parsed as double-quoted string literals in MariaDB because `ANSI_QUOTES` is not enabled in the server's `sql_mode`.

MariaDB requires:
1. `ON DUPLICATE KEY UPDATE` or `REPLACE INTO` syntax for upserts.
2. Backticks (`` ` ``) for database, table, and column identifiers instead of double quotes, especially when using reserved keywords like `` `key` `` or `` `value` ``.
3. MariaDB-native datetime functions like `DATE_ADD(NOW(6), INTERVAL 5 MINUTE)`.

## Fix

We updated all raw SQL queries in `ReceiptArchivalService.vb` to conform to the MariaDB dialect.

```vb
' Before (SQLite - broken on MariaDB)
Await db.Database.ExecuteSqlRawAsync(
    "INSERT OR REPLACE INTO ""Pos_ArchivalSession"" (key, value, expires_at) " &
    "VALUES ('archival_in_progress', 1, datetime('now', '+5 minutes'))",
    cancellationToken)

Await db.Database.ExecuteSqlRawAsync(
    "DELETE FROM ""Pos_ReceiptIntegrity"" WHERE ""ReceiptId"" = {0}",
    {item.Receipt.Id},
    cancellationToken)

' After (MariaDB - fixed)
Await db.Database.ExecuteSqlRawAsync(
    "INSERT INTO `Pos_ArchivalSession` (`key`, `value`, `expires_at`) " &
    "VALUES ('archival_in_progress', 1, DATE_ADD(NOW(6), INTERVAL 5 MINUTE)) " &
    "ON DUPLICATE KEY UPDATE `value` = 1, `expires_at` = DATE_ADD(NOW(6), INTERVAL 5 MINUTE)",
    cancellationToken)

Await db.Database.ExecuteSqlRawAsync(
    "DELETE FROM `Pos_ReceiptIntegrity` WHERE `ReceiptId` = {0}",
    {item.Receipt.Id},
    cancellationToken)
```

## Prevention

- **Quote using Backticks:** Always use backticks (`` ` ``) rather than double quotes (`""`) for table and column identifiers in all raw SQL statements targetting MariaDB.
- **Escape Reserved Keywords:** Columns like `key`, `value`, `order`, etc. are reserved or semi-reserved in MariaDB. They must be enclosed in backticks (e.g., `` `key` ``).
- **Convert SQLite Functions:** Replace any SQLite date/time helper functions like `datetime('now', ...)` with MariaDB equivalents such as `NOW(6)` and `DATE_ADD()`.
- **Rewrite Upserts:** Replace `INSERT OR REPLACE` with `INSERT ... ON DUPLICATE KEY UPDATE` to avoid unnecessary row deletions/re-insertions.

## Related

- `[[mariadb-pure-client-server-architecture]]`
- `[[efcore-vbnet-tolistasync-entity-empty]]`
