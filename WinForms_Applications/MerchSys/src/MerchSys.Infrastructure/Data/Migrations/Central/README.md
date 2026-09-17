# VISTA Centralized MariaDB Migrations

This directory contains versioned DDL and seed scripts for the centralized MariaDB database.

## Naming Conventions
- Every script must follow the pattern `NNNN_description.sql`, where `NNNN` is a zero-padded 4-digit sequential integer (e.g., `0001_initial_schema.sql`).
- Scripts are enumerated and executed in lexicographical order.

## Strict Rules
1. **Append-Only:** Once a script is deployed or committed, it **MUST NEVER** be edited or removed. Any subsequent database schema changes must be written as a new sequential script (e.g., `0003_add_new_table.sql`).
2. **Idempotency:** All statements must be idempotent. Use `CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`, and `INSERT IGNORE` so that re-executing a script does not produce errors or duplicate data.
3. **Drift Detection (Sha256):** The startup initializer `MariaDbSchemaInitializer` records the SHA-256 hash of each applied script in the `__SchemaMigrations` table. If a script's contents are altered after execution, the startup initializer **ABORTS** the application launch immediately to prevent schema drift and database corruption.

## Script Guidelines
- Always use `ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci` for table declarations.
- Use `DATETIME(6)` for microsecond precision timestamps.
- Use `TINYINT(1)` for boolean variables.
- Use `DECIMAL(18, 4)` for monetary or fractional values.
- All mutable tables must declare a `RowVersion TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6)` column for optimistic concurrency. Append-only tables (like logs or journals) should omit this column.
