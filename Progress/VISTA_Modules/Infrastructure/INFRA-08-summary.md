---
module: Infrastructure
agent: claude-code
date: 2026-05-11
plan-ref: Plans/VISTA_Modules/Infrastructure/08-mariadb-receipt-integrity-triggers.md
status: completed
---

## Task Summary

Closes the MariaDB tamper-evidence gap identified in the 2026-05-11 Infrastructure and POS audits. The POS-13 SQLite triggers enforce append-only semantics locally; this plan adds the central-server mirror. Audit of `01-mariadb-init.sql` revealed partial coverage already exists — see "Discovered State" below.

**Plan:** `[[08-mariadb-receipt-integrity-triggers]]`

## What Was Done

- Created `Plans/VISTA_Modules/Infrastructure/sql/02-pos-receipt-integrity-triggers.sql` — schema delta: `Pos_OfficialReceiptArchive` table + two immutability triggers
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Resources/Sql/ReceiptIntegrityTriggerVerification.sql` — diagnostic query bundle (trigger inventory + 5 negative-path probes)
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Resources/Sql/` directory (did not previously exist)

## Discovered State of mariadb-init.sql (INFRA-06 Overlap)

Reading `01-mariadb-init.sql` before implementing revealed that INFRA-06 **already installed** blanket immutability triggers for two of the three tables this plan targeted:

| Table | Existing trigger (INFRA-06) | Action |
|---|---|---|
| `Pos_ReceiptIntegrity` | `trg_Pos_ReceiptIntegrity_NoUpdate` / `trg_Pos_ReceiptIntegrity_NoDelete` | Blanket SIGNAL 45000 on any UPDATE or DELETE |
| `Pos_OfficialReceipts` | `trg_Pos_OfficialReceipts_NoUpdate` / `trg_Pos_OfficialReceipts_NoDelete` | Blanket SIGNAL 45000 on any UPDATE or DELETE |
| `Pos_OfficialReceiptArchive` | **None — table absent from mariadb-init.sql** | Gap closed by this plan |

Conclusion: `Pos_ReceiptIntegrity` and `Pos_OfficialReceipts` are already protected. Adding duplicate triggers for those tables would be redundant; no new triggers were created for them.

## Schema Discrepancy — Pos_OfficialReceipts (central vs. SQLite)

The INFRA-08 plan spec calls for a status-aware `BEFORE UPDATE` trigger on `Pos_OfficialReceipts` that checks `OLD.Status = 'Issued'` and guards columns `ReceiptNumber`, `IssuedAt`, `TotalAmount`, `IntegrityHash`. These columns do **not** exist in the central MariaDB `Pos_OfficialReceipts` table:

| Column (plan spec) | Present in MariaDB? |
|---|---|
| `Status` | No |
| `IssuedAt` | No (MariaDB has `IssueDate`) |
| `IntegrityHash` | No |
| `ReceiptNumber` | Yes |
| `TotalAmount` | Yes |

The existing blanket trigger (`trg_Pos_OfficialReceipts_NoUpdate`) blocks ALL updates unconditionally, which is a strictly stronger guarantee than the status-conditional SQLite trigger. No schema change or new trigger is needed; the blanket trigger satisfies the compliance goal.

**Codebase wiki discrepancy to flag:** The central `Pos_OfficialReceipts` schema differs from the SQLite entity defined in POS-13 (`Status`, `IssuedAt`, `IntegrityHash` absent centrally). This may require a schema alignment pass when the sync worker (INFRA-05) is wired to `Pos_OfficialReceipts`.

## SQL SECURITY INVOKER — MariaDB Limitation

The plan specifies `SQL SECURITY INVOKER` on all triggers. In MariaDB 11.4, the `SQL SECURITY` clause is **only valid for stored routines (functions/procedures)**, not for triggers. Triggers always execute in the definer's security context; the clause is not recognized and would cause a syntax error.

Mitigation applied:
- Triggers use `DEFINER = CURRENT_USER`, binding them to the admin account that applies the script (not a root superuser), which limits the privilege surface.
- The primary access control — `merchsys_sync_role` holds `INSERT` but not `UPDATE` or `DELETE` on `Pos_*` tables — is the operative defence; this is already in `01-mariadb-init.sql`.
- This limitation is not a security regression; it is a documentation gap in the plan.

## Trigger SQL

### `trg_pos_official_receipt_archive_block_update`

```sql
CREATE DEFINER = CURRENT_USER TRIGGER `trg_pos_official_receipt_archive_block_update`
BEFORE UPDATE ON `Pos_OfficialReceiptArchive`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Pos_OfficialReceiptArchive is append-only; UPDATE blocked.';
END
```

SQLSTATE: `45000` — user-defined exception (not `23000` which masks as constraint violation).

### `trg_pos_official_receipt_archive_block_delete`

```sql
CREATE DEFINER = CURRENT_USER TRIGGER `trg_pos_official_receipt_archive_block_delete`
BEFORE DELETE ON `Pos_OfficialReceiptArchive`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Pos_OfficialReceiptArchive is append-only; DELETE blocked.';
END
```

SQLSTATE: `45000`.

## Deployment Order

| Step | Command | Notes |
|---|---|---|
| 1 | `mysql -u <admin> -p merchsys_central < 01-mariadb-init.sql` | Creates all tables + existing triggers |
| 2 | `mysql -u <admin> -p merchsys_central < 02-pos-receipt-integrity-triggers.sql` | Creates `Pos_OfficialReceiptArchive` + its triggers |
| 3 | `mysql -u <admin> -p merchsys_central < ReceiptIntegrityTriggerVerification.sql` | Deployment checklist verification |

Step 2 must be run by the same admin account used in Step 1 so that `DEFINER = CURRENT_USER` resolves to the same user. Step 3 is optional but recommended after every deployment; all five probes should produce SQLSTATE 45000 errors.

## Negative-Path Probe SQLSTATE Reference

| Probe | Table | Operation | Expected SQLSTATE | Expected message |
|---|---|---|---|---|
| 1 | `Pos_ReceiptIntegrity` | UPDATE | 45000 | Updates to Pos_ReceiptIntegrity are prohibited (BIR compliance) |
| 2 | `Pos_ReceiptIntegrity` | DELETE | 45000 | Deletions from Pos_ReceiptIntegrity are prohibited (BIR compliance) |
| 3 | `Pos_OfficialReceipts` | UPDATE | 45000 | Updates to Pos_OfficialReceipts are prohibited (BIR compliance) |
| 4 | `Pos_OfficialReceiptArchive` | UPDATE | 45000 | Pos_OfficialReceiptArchive is append-only; UPDATE blocked. |
| 5 | `Pos_OfficialReceiptArchive` | DELETE | 45000 | Pos_OfficialReceiptArchive is append-only; DELETE blocked. |

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | N/A — SQL-only deliverable |
| Unit tests pass | N/A |
| Manual verification | N/A — requires live MariaDB 11.4.x instance |

## Issues Encountered

- **Issue:** Plan calls for `SQL SECURITY INVOKER` on triggers, which is not valid MariaDB syntax.
  - **Resolution:** Used `DEFINER = CURRENT_USER` as the best available alternative; documented limitation in this summary. No Agent Wiki entry created (SQL dialect limitation, not a code bug).

- **Issue:** Plan specifies status-aware trigger for `Pos_OfficialReceipts` referencing `Status` and `IntegrityHash` columns absent from the central schema.
  - **Resolution:** Existing blanket trigger provides stronger protection. Discrepancy logged above. Central schema may need alignment with POS-13 SQLite schema in a future pass.

- **Issue:** `Pos_ReceiptIntegrity` and `Pos_OfficialReceipts` triggers already existed in `01-mariadb-init.sql` — the audit that prompted INFRA-08 was stale.
  - **Resolution:** No duplicate triggers added. Gap confirmed to be only `Pos_OfficialReceiptArchive`.

## What's Next

- [ ] Schema alignment pass: add `Status`, `IssuedAt`, `IntegrityHash` columns to central `Pos_OfficialReceipts` if the sync worker requires them (coordinate with INFRA-05 sync wiring)
- [ ] Future remediation: replace the INFRA-06 triggers (`trg_Pos_ReceiptIntegrity_NoUpdate`, etc.) with versions using `DEFINER = <admin>` (not anonymous default) once a formal DB admin account is established

## Cross-References

- Domain Wiki pages consulted: `[[bir-compliance]]`, `[[client-server-wpf]]`
- Plans consulted: `[[INFRA-06]]` (mariadb-init.sql), `[[POS-13]]` (SQLite trigger reference)
