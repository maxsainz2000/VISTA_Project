---
module: MerchSys.Infrastructure
plan-id: INFRA-08
title: "MariaDB Receipt Integrity Triggers"
depends-on: [INFRA-06, POS-13]
estimated-files: 2
---

# MariaDB Receipt Integrity Triggers

## Context

INFRA-06 established the central MariaDB schema (`mariadb-init.sql`) for the sync target. POS-13 introduced the local SQLite tables `Pos_ReceiptIntegrity` and `Pos_OfficialReceiptArchive` along with SQLite triggers that enforce **append-only / no-update / no-delete** semantics on receipt integrity rows — a BIR tamper-evidence requirement.

The 2026-05-11 Infrastructure and POS audits both flagged the same gap: the MariaDB equivalent of those tamper triggers is **missing** from `mariadb-init.sql`. As written today, a row that lands in the central MariaDB `Pos_ReceiptIntegrity` table via the sync worker can be silently updated or deleted on the central server, which defeats the entire tamper-evidence chain end-to-end.

This plan closes the gap by adding the MariaDB-equivalent triggers and adjusting the central schema to mirror the SQLite immutability guarantees. The SQLite side (POS-13) is not touched.

## Prerequisites

- **INFRA-06** (MariaDB Central Schema & Reconciliation) — `mariadb-init.sql`, `Pos_ReceiptIntegrity` and `Pos_OfficialReceiptArchive` table definitions on the central server
- **POS-13** (BIR Tamper-Proof Receipt Retention & Sequence) — reference SQLite triggers for parity
- **INFRA-05** (Sync Worker) — receipt rows arrive at the central server via the sync journal; triggers must not break legitimate INSERTs from the sync worker

## Wiki References

- `concepts/bir-compliance.md` — Section on Official Receipt immutability and 10-year retention
- `concepts/client-server-wpf.md` — Sync direction is local → central (push only); central never writes back to local
- `analysis/cross-module-data-flow.md` — `ReceiptTamperDetectedEvent` flow

## Deliverables

```
Plans/VISTA_Modules/Infrastructure/sql/
└── 02-pos-receipt-integrity-triggers.sql      ' New, applied after 01-mariadb-init.sql

WPF_Applications/MerchSys/src/MerchSys.App/Resources/Sql/
└── ReceiptIntegrityTriggerVerification.sql    ' Diagnostic query bundle
```

The first file is the schema delta. The second file is a small bundle of diagnostic SELECTs against `INFORMATION_SCHEMA.TRIGGERS` used by the deployment checklist to confirm the triggers landed. No VB code is added.

## Specification

### Trigger set on `Pos_ReceiptIntegrity`

Mirror the SQLite triggers from POS-13. The MariaDB versions must:

1. **Block UPDATE** on any row in `Pos_ReceiptIntegrity` regardless of the column touched.
   - `BEFORE UPDATE` trigger that calls `SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Pos_ReceiptIntegrity is append-only; UPDATE blocked.'`
2. **Block DELETE** on any row in `Pos_ReceiptIntegrity` outside an explicit archival path.
   - `BEFORE DELETE` trigger with the same `SIGNAL` pattern, message `'Pos_ReceiptIntegrity is append-only; DELETE blocked.'`
   - There is no archival path on the central server for integrity rows — the SQLite archival job (POS-13's `Pos_OfficialReceiptArchive`) operates locally; central rows live forever.
3. **Permit INSERT** unconditionally. The sync worker inserts these rows; the trigger set must not interfere.

Naming convention: `trg_pos_receipt_integrity_<action>` (e.g., `trg_pos_receipt_integrity_block_update`). Use `CREATE TRIGGER` with explicit `DEFINER = CURRENT_USER` and `SQL SECURITY INVOKER` to avoid privilege escalation surprises.

### Trigger set on `Pos_OfficialReceipts`

POS-13 also installs SQLite triggers on `Pos_OfficialReceipts` itself that block UPDATE of the `ReceiptNumber`, `IssuedAt`, `TotalAmount`, and `IntegrityHash` columns once a row is in `Issued` state. Mirror these:

1. `BEFORE UPDATE` trigger that compares `OLD.Status` and the four guarded columns. If `OLD.Status = 'Issued'` and any guarded column differs, `SIGNAL SQLSTATE '45000'` with message `'Issued receipt fields are immutable.'`
2. `BEFORE DELETE` trigger blocking removal of `Issued`-state rows regardless of source. Message: `'Issued receipts cannot be deleted from central ledger.'`
3. Permit transitions from `Draft` → `Issued`, but reject any other `Status` mutation on an `Issued` row.

### Trigger set on `Pos_OfficialReceiptArchive`

For consistency with the local SQLite archive (which is itself append-only after a row lands), block UPDATE and DELETE on this table on the central side. INSERT-only.

### Diagnostic queries

`ReceiptIntegrityTriggerVerification.sql` contains:

```sql
SELECT TRIGGER_NAME, EVENT_MANIPULATION, ACTION_TIMING
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA = DATABASE()
  AND EVENT_OBJECT_TABLE IN (
    'Pos_ReceiptIntegrity',
    'Pos_OfficialReceipts',
    'Pos_OfficialReceiptArchive'
  )
ORDER BY EVENT_OBJECT_TABLE, EVENT_MANIPULATION;
```

Plus three negative-path probes wrapped in `BEGIN ... ROLLBACK`:

```sql
-- Probe 1: UPDATE on Pos_ReceiptIntegrity should fail with SQLSTATE 45000
-- Probe 2: DELETE on Pos_ReceiptIntegrity should fail with SQLSTATE 45000
-- Probe 3: UPDATE of ReceiptNumber on an Issued receipt should fail
```

Each probe is commented with the expected SQLSTATE and message text so an operator running the bundle can confirm pass/fail by eye.

## Implementation Notes

- The `Plans/VISTA_Modules/Infrastructure/sql/` directory already exists (per the `ls` output of the parent folder, which includes a `sql` subdirectory) — place the new `.sql` file there alongside `01-mariadb-init.sql` (or whatever INFRA-06 named it). If the existing file uses a different prefix, match it; do not invent a new convention.
- Triggers must be installed by the **same** apply pipeline as `mariadb-init.sql` — i.e., the deployment runbook must apply both files in order. Document this in the implementation summary.
- The `SIGNAL SQLSTATE '45000'` pattern is the MariaDB / MySQL idiomatic equivalent of SQLite's `RAISE(ABORT, ...)`. Use `45000` (unhandled user-defined exception) — not `23000` (integrity constraint), which would mask as a normal constraint violation.
- Use `SQL SECURITY INVOKER`, not `DEFINER`, so the trigger executes with the caller's privileges. The sync worker user must have INSERT but not UPDATE/DELETE on these tables; if the trigger ran as `DEFINER = root`, an attacker who hijacked the sync worker could effectively bypass row-level intent.
- Do **not** drop existing triggers blindly with `DROP TRIGGER IF EXISTS` followed by `CREATE TRIGGER` in a single file without a transaction — MariaDB DDL is not transactional, and a half-applied trigger set is worse than the absent state. Use `CREATE TRIGGER` only; if the operator needs to reapply, they explicitly drop first via runbook.
- The audit notes that `mariadb-init.sql` already contains "POS receipt triggers" — confirm scope. This plan adds the **immutability** triggers specifically; it does not duplicate any existing INSERT-time validation triggers.
- No SQLite changes. The POS-13 SQLite triggers stay as the source of truth for local enforcement; this plan is the mirror image on the central side.

## Acceptance Criteria

1. New SQL file applies cleanly against a fresh MariaDB 11.4.x instance immediately after `01-mariadb-init.sql`.
2. `SELECT * FROM INFORMATION_SCHEMA.TRIGGERS WHERE EVENT_OBJECT_TABLE = 'Pos_ReceiptIntegrity'` returns at least two rows (BEFORE UPDATE, BEFORE DELETE) with `ACTION_TIMING = 'BEFORE'`.
3. Attempting `UPDATE Pos_ReceiptIntegrity SET ReceiptNumber = '...' WHERE Id = 1` raises SQLSTATE `45000` with the documented message.
4. Attempting `DELETE FROM Pos_ReceiptIntegrity WHERE Id = 1` raises SQLSTATE `45000`.
5. `INSERT INTO Pos_ReceiptIntegrity (...) VALUES (...)` succeeds — sync inserts are not blocked.
6. Attempting `UPDATE Pos_OfficialReceipts SET ReceiptNumber = '...' WHERE Status = 'Issued'` raises SQLSTATE `45000`.
7. Updating a `Draft`-state receipt to set `Status = 'Issued'` succeeds (the immutability gate only engages once `Status = 'Issued'`).
8. Triggers run with `SQL SECURITY INVOKER`.
9. The diagnostic SQL bundle returns the expected trigger inventory and the three negative-path probes all fail with SQLSTATE `45000`.
10. No source-file edits to `mariadb-init.sql`; new triggers are in a separate `.sql` file applied after it.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-08-summary.md` using `Progress/_template.md`. Capture the exact SQL of each trigger, the SQLSTATE for each negative-path probe, and a note on the deployment ordering requirement (init script first, then triggers).

### Documentation
- Header comment in `02-pos-receipt-integrity-triggers.sql` summarising the BIR motivation and pointing at `concepts/bir-compliance.md`.
- A short "Deployment Order" section in the implementation summary that lists the SQL files to apply and in what order, suitable to be lifted into a future runbook.
- A note in the summary describing the discovered state of `mariadb-init.sql`'s existing POS triggers (if any) to confirm there is no overlap.
