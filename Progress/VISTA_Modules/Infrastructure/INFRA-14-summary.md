---
module: Infrastructure
agent: claude-code
date: 2026-05-16
plan-ref: Plans/VISTA_Modules/Infrastructure/14-central-schema-receipt-alignment.md
status: completed
---

## Task Summary

Aligns the central MariaDB `Pos_OfficialReceipts` table with the local SQLite schema by adding the three columns introduced by POS-13/14/15 (`Status`, `IssuedAt`, `IntegrityHash`). Without this alignment the `SyncOrchestrator` (INFRA-12) would fail when pushing receipt rows to central MariaDB. Two deployment paths are provided: an idempotent ALTER TABLE script for existing installations and an updated `mariadb-init.sql` for fresh installs.

**Plan:** `[[14-central-schema-receipt-alignment]]`

## What Was Done

- Created `Plans/VISTA_Modules/Infrastructure/sql/mariadb-receipt-schema-alignment.sql` — idempotent ALTER TABLE statements with full header comment referencing INFRA-06, INFRA-08, POS-13/14/15 and trigger compatibility rationale
- Created `WPF_Applications/MerchSys/src/MerchSys.Infrastructure/Data/Migrations/Central/AlignReceiptSyncColumns.sql` — versioned migration script (version 1) containing the same ALTER TABLE statements for deployment tracking
- Modified `Plans/VISTA_Modules/Infrastructure/sql/mariadb-init.sql` — added `Status`, `IssuedAt`, `IntegrityHash` columns and `IX_OfficialReceipts_IssuedAt` index to the `Pos_OfficialReceipts` CREATE TABLE definition; bumped schema version to 1.1.0

## Trigger Compatibility Verification

The plan requested verification that INFRA-06's existing immutability triggers on `Pos_OfficialReceipts` remain correct after the column additions.

| Trigger | Type | Implementation | Effect after column addition |
|---|---|---|---|
| `trg_Pos_OfficialReceipts_NoUpdate` | BEFORE UPDATE | `SIGNAL SQLSTATE '45000'` (row-level, no column list) | Unchanged — still blocks all UPDATE unconditionally |
| `trg_Pos_OfficialReceipts_NoDelete` | BEFORE DELETE | `SIGNAL SQLSTATE '45000'` (row-level, no column list) | Unchanged — still blocks all DELETE unconditionally |

Both triggers fire on the row event, not on any specific column predicate. Adding `Status`, `IssuedAt`, and `IntegrityHash` is purely additive and has no effect on trigger behaviour. The blanket-block guarantee (stronger than a column-selective trigger) is preserved.

## Column Specification

| Column | Type | Nullable | Default | Origin |
|---|---|---|---|---|
| `Status` | `VARCHAR(20)` | NOT NULL | `'Issued'` | POS-13 — append-only lifecycle state |
| `IssuedAt` | `DATETIME(6)` | NOT NULL | `CURRENT_TIMESTAMP(6)` | POS-14 — BIR-mandated issuance timestamp |
| `IntegrityHash` | `VARCHAR(64)` | NULL | — | POS-15 — SHA-256 payload hash; nullable for pre-POS-13 rows |

`IntegrityHash` is nullable because receipts synced from before POS-13 will not carry a hash. The local `ComputeAndPersistAsync` runs locally; the central copy receives whatever value the local row holds.

## Deployment Order

| Step | Script | Purpose |
|---|---|---|
| 1 | `01-mariadb-init.sql` | Fresh install — creates all tables including updated `Pos_OfficialReceipts` with all three columns |
| 2 | `02-pos-receipt-integrity-triggers.sql` | INFRA-08 — `Pos_OfficialReceiptArchive` + triggers |
| 3 | `mariadb-receipt-schema-alignment.sql` | Existing install — adds the three columns idempotently |

Step 3 is only required for instances created before INFRA-14. Fresh installs via the updated `01-mariadb-init.sql` already include all columns; re-running the alignment script against a fresh install is safe (`IF NOT EXISTS` guards all statements).

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | N/A — SQL-only deliverable |
| Unit tests pass | N/A |
| Manual verification | N/A — requires live MariaDB 11.4.x instance |

## Issues Encountered

None. The plan was self-consistent and the existing trigger implementation (row-level SIGNAL, no column list) made the compatibility verification straightforward.

## What's Next

- [ ] Deploy `mariadb-receipt-schema-alignment.sql` against the staging/production MariaDB instance and verify `IF NOT EXISTS` idempotency by running it twice
- [ ] Wire `SyncOrchestrator` receipt push path (INFRA-12) and confirm a receipt row with `Status`, `IssuedAt`, and `IntegrityHash` populated inserts successfully into the aligned central table

## Codebase Wiki Discrepancies

The `codebase_wiki` did not need to be consulted (SQL-only plan, no VB.NET entities changed). No discrepancies observed.

## Cross-References

- Domain Wiki pages consulted: `[[offline-first-sync]]`, `[[bir-compliance]]`
- Agent Wiki entries consulted: none
- Plans consulted: `[[INFRA-06]]` (mariadb-init.sql), `[[INFRA-08]]` (receipt integrity triggers), `[[INFRA-12]]` (SyncOrchestrator), `[[POS-13]]`, `[[POS-14]]`, `[[POS-15]]` (SQLite column origins)
