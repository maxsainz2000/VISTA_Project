---
module: MerchSys.Infrastructure
plan-id: INFRA-14
title: "Central Schema Alignment for Receipt Sync"
depends-on: [INFRA-06, INFRA-08]
estimated-files: 2
priority: medium
---

# Central Schema Alignment for Receipt Sync

## Context

INFRA-08 delivered MariaDB receipt integrity triggers for the central `Pos_OfficialReceipts` table. However, the 2026-05-15 Infrastructure audit notes a **schema alignment gap**: the local SQLite `Pos_OfficialReceipts` table gained `Status`, `IssuedAt`, and `IntegrityHash` columns through POS-13/14/15 migrations, but the central MariaDB schema (defined in INFRA-06's `mariadb-init.sql`) does not yet include these columns.

Without alignment, the `SyncOrchestrator` (INFRA-12) will fail when attempting to push receipts with these columns to the central schema.

## Prerequisites

- **INFRA-06** (MariaDB Central Schema) — `mariadb-init.sql`, central `Pos_OfficialReceipts` table definition
- **INFRA-08** (MariaDB Receipt Integrity Triggers) — existing triggers on central receipt tables

## Wiki References

- `concepts/offline-first-sync.md` — Local and central schemas must be aligned for sync

## Deliverables

```
Plans/VISTA_Modules/Infrastructure/sql/
└── mariadb-receipt-schema-alignment.sql        ' New — ALTER TABLE statements

MerchSys.Infrastructure/Data/Migrations/Central/
└── AlignReceiptSyncColumns.sql                 ' New — versioned migration script
```

## Specification

### ALTER TABLE Statements

```sql
ALTER TABLE Pos_OfficialReceipts
    ADD COLUMN IF NOT EXISTS `Status` VARCHAR(20) NOT NULL DEFAULT 'Issued',
    ADD COLUMN IF NOT EXISTS `IssuedAt` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    ADD COLUMN IF NOT EXISTS `IntegrityHash` VARCHAR(64) NULL;

ALTER TABLE Pos_OfficialReceipts
    ADD INDEX IF NOT EXISTS `IX_OfficialReceipts_IssuedAt` (`IssuedAt`);
```

### mariadb-init.sql Update

Update the `CREATE TABLE Pos_OfficialReceipts` definition in `mariadb-init.sql` to include the three new columns in the base schema for fresh installations. The `ALTER TABLE` script above handles existing installations.

### Trigger Compatibility

Verify that INFRA-08's existing triggers (`trg_Pos_ReceiptIntegrity_NoUpdate`, `trg_Pos_ReceiptIntegrity_NoDelete`) still function correctly after the column additions. The triggers should not reference specific column lists — they apply to the row level. Confirm and document.

## Implementation Notes

- `IF NOT EXISTS` ensures idempotency — the script can be re-run safely.
- `IntegrityHash` is nullable because receipts synced from before POS-13 won't have hashes. The local `ComputeAndPersistAsync` runs locally; the central copy receives whatever the local has.
- The `Status` column default of `'Issued'` matches the POS domain model's initial state.
- `DATETIME(6)` for microsecond precision, matching SQLite's datetime storage.

## Acceptance Criteria

1. `ALTER TABLE` script applies cleanly against an existing MariaDB 11.4.x instance with INFRA-06 schema.
2. `ALTER TABLE` script is idempotent (re-running produces no errors).
3. Fresh `mariadb-init.sql` creates the table with all three columns included.
4. INFRA-08 triggers continue to function correctly (UPDATE/DELETE still blocked).
5. `SyncOrchestrator` can push a receipt row with `Status`, `IssuedAt`, and `IntegrityHash` populated.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-14-summary.md` using `Progress/_template.md`.

### Documentation
- Header comment in the SQL script referencing INFRA-06, INFRA-08, POS-13, and the column origin.
