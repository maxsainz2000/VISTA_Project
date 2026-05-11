-- =============================================================================
-- VISTA — Receipt Integrity Trigger Verification
-- File      : ReceiptIntegrityTriggerVerification.sql
-- Plan      : INFRA-08 (MariaDB Receipt Integrity Triggers)
-- Purpose   : Deployment-checklist diagnostic bundle.  Run against
--             merchsys_central after applying both init files to confirm that
--             immutability triggers are present and behave correctly.
--
-- Usage
-- -----
--   mysql -u <admin> -p merchsys_central < ReceiptIntegrityTriggerVerification.sql
--
-- Expected outcome
-- ----------------
--   Section 1 — trigger inventory:
--     Returns at least 6 rows across the three tables (2 per table).
--     All rows show ACTION_TIMING = 'BEFORE'.
--
--   Probe 1 — UPDATE on Pos_ReceiptIntegrity:
--     Blocked by trg_Pos_ReceiptIntegrity_NoUpdate (INFRA-06).
--     Expected error: SQLSTATE 45000,
--     "Updates to Pos_ReceiptIntegrity are prohibited (BIR compliance)"
--
--   Probe 2 — DELETE on Pos_ReceiptIntegrity:
--     Blocked by trg_Pos_ReceiptIntegrity_NoDelete (INFRA-06).
--     Expected error: SQLSTATE 45000,
--     "Deletions from Pos_ReceiptIntegrity are prohibited (BIR compliance)"
--
--   Probe 3 — UPDATE on Pos_OfficialReceipts:
--     Blocked by trg_Pos_OfficialReceipts_NoUpdate (INFRA-06).
--     Note: the central schema has no Status column; the blanket trigger
--     blocks ALL updates regardless of receipt state, which is a stricter
--     guarantee than the status-aware SQLite trigger in POS-13.
--     Expected error: SQLSTATE 45000,
--     "Updates to Pos_OfficialReceipts are prohibited (BIR compliance)"
--
--   Probe 4 — UPDATE on Pos_OfficialReceiptArchive:
--     Blocked by trg_pos_official_receipt_archive_block_update (INFRA-08).
--     Expected error: SQLSTATE 45000,
--     "Pos_OfficialReceiptArchive is append-only; UPDATE blocked."
--
--   Probe 5 — DELETE on Pos_OfficialReceiptArchive:
--     Blocked by trg_pos_official_receipt_archive_block_delete (INFRA-08).
--     Expected error: SQLSTATE 45000,
--     "Pos_OfficialReceiptArchive is append-only; DELETE blocked."
-- =============================================================================

USE `merchsys_central`;


-- =============================================================================
-- Section 1: Trigger inventory
-- Operator: verify at least 2 rows per table, all ACTION_TIMING = 'BEFORE'.
-- =============================================================================

SELECT
    EVENT_OBJECT_TABLE   AS `Table`,
    TRIGGER_NAME         AS `Trigger`,
    EVENT_MANIPULATION   AS `Event`,
    ACTION_TIMING        AS `Timing`,
    DEFINER              AS `Definer`
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA = DATABASE()
  AND EVENT_OBJECT_TABLE IN (
      'Pos_ReceiptIntegrity',
      'Pos_OfficialReceipts',
      'Pos_OfficialReceiptArchive'
  )
ORDER BY EVENT_OBJECT_TABLE, EVENT_MANIPULATION;


-- =============================================================================
-- Section 2: Negative-path probes
-- Each probe runs inside BEGIN ... ROLLBACK so no data is written.
-- A successful probe means the database raised an error (operation blocked).
-- An operator running this bundle should see an error for each probe.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Probe 1: UPDATE on Pos_ReceiptIntegrity must fail (SQLSTATE 45000)
-- Expected: "Updates to Pos_ReceiptIntegrity are prohibited (BIR compliance)"
-- ---------------------------------------------------------------------------
BEGIN;
    UPDATE `Pos_ReceiptIntegrity`
    SET    `ChecksumHash` = 'tampered'
    WHERE  `Id` = -1;   -- row does not exist; trigger fires before WHERE evaluation
ROLLBACK;

-- ---------------------------------------------------------------------------
-- Probe 2: DELETE on Pos_ReceiptIntegrity must fail (SQLSTATE 45000)
-- Expected: "Deletions from Pos_ReceiptIntegrity are prohibited (BIR compliance)"
-- ---------------------------------------------------------------------------
BEGIN;
    DELETE FROM `Pos_ReceiptIntegrity`
    WHERE  `Id` = -1;
ROLLBACK;

-- ---------------------------------------------------------------------------
-- Probe 3: UPDATE on Pos_OfficialReceipts must fail (SQLSTATE 45000)
-- The central schema has no Status column; the blanket trigger blocks all
-- updates unconditionally (stricter than the POS-13 SQLite status-aware gate).
-- Expected: "Updates to Pos_OfficialReceipts are prohibited (BIR compliance)"
-- ---------------------------------------------------------------------------
BEGIN;
    UPDATE `Pos_OfficialReceipts`
    SET    `ReceiptNumber` = 'TAMPERED'
    WHERE  `Id` = -1;
ROLLBACK;

-- ---------------------------------------------------------------------------
-- Probe 4: UPDATE on Pos_OfficialReceiptArchive must fail (SQLSTATE 45000)
-- Expected: "Pos_OfficialReceiptArchive is append-only; UPDATE blocked."
-- ---------------------------------------------------------------------------
BEGIN;
    UPDATE `Pos_OfficialReceiptArchive`
    SET    `ReceiptNumber` = 'TAMPERED'
    WHERE  `Id` = -1;
ROLLBACK;

-- ---------------------------------------------------------------------------
-- Probe 5: DELETE on Pos_OfficialReceiptArchive must fail (SQLSTATE 45000)
-- Expected: "Pos_OfficialReceiptArchive is append-only; DELETE blocked."
-- ---------------------------------------------------------------------------
BEGIN;
    DELETE FROM `Pos_OfficialReceiptArchive`
    WHERE  `Id` = -1;
ROLLBACK;
