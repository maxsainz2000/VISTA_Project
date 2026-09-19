-- =============================================================================
-- VISTA MariaDB — POS Receipt Integrity Triggers (Schema Delta)
-- File         : 02-pos-receipt-integrity-triggers.sql
-- Plan         : INFRA-08 (MariaDB Receipt Integrity Triggers)
-- Target engine: MariaDB 11.4.x LTS
-- Apply after  : 01-mariadb-init.sql  (schema delta — do NOT apply standalone)
-- BIR reference: LLM_Wiki/wiki/concepts/bir-compliance.md
--                NIRC §113 (issuance), §235 (10-year preservation, tamper-proof)
--
-- Purpose
-- -------
-- Closes the tamper-evidence gap on the central MariaDB sync target identified
-- in the 2026-05-11 Infrastructure and POS audits.  POS-13 enforces append-only
-- semantics locally via an EF SaveChangesInterceptor and SQLite triggers.  This
-- file mirrors those guarantees on the central server, ensuring that receipt rows
-- synced from the POS client cannot be silently mutated or removed centrally.
--
-- Discovered state of 01-mariadb-init.sql (INFRA-06)
-- ---------------------------------------------------
-- 01-mariadb-init.sql already contains blanket BEFORE UPDATE / BEFORE DELETE
-- triggers for Pos_ReceiptIntegrity and Pos_OfficialReceipts:
--   trg_Pos_ReceiptIntegrity_NoUpdate   / trg_Pos_ReceiptIntegrity_NoDelete
--   trg_Pos_OfficialReceipts_NoUpdate   / trg_Pos_OfficialReceipts_NoDelete
-- These provide the required immutability for those two tables.  This file does
-- NOT add duplicate triggers for them; doing so would be redundant and would
-- complicate the trigger inventory without adding protection.
--
-- The Pos_OfficialReceiptArchive table is entirely absent from 01-mariadb-init.sql
-- — that is the primary gap this file closes.
--
-- Note on SQL SECURITY INVOKER
-- ----------------------------
-- The INFRA-08 plan calls for SQL SECURITY INVOKER on all triggers.  In MariaDB,
-- the SQL SECURITY clause is only valid for stored routines (functions/procedures),
-- not for triggers.  Triggers always execute in the definer's security context.
-- The mitigation applied here is DEFINER = CURRENT_USER, which binds the trigger
-- to the admin account that applies this script rather than a root superuser.
-- The primary privilege guard — that the sync role (merchsys_sync_role) holds
-- INSERT but not UPDATE or DELETE on Pos_* tables — is enforced in
-- 01-mariadb-init.sql.  See INFRA-08-summary.md for the full remediation note.
--
-- Deployment Order
-- ----------------
--   Step 1:  mysql -u <admin> -p merchsys_central < 01-mariadb-init.sql
--   Step 2:  mysql -u <admin> -p merchsys_central < 02-pos-receipt-integrity-triggers.sql
--
-- Both files must be applied by the same admin account in the above order.
-- 02-pos-receipt-integrity-triggers.sql has no effect if applied before Step 1
-- because Pos_OfficialReceiptArchive's trigger references the table created here.
-- =============================================================================

USE `merchsys_central`;


-- =============================================================================
-- Pos_OfficialReceiptArchive
-- Cold-storage mirror of Pos_OfficialReceipts.  Receipts are COPIED here after
-- RetentionExpiresAt + ArchivePolicy:GraceDays; the source row in
-- Pos_OfficialReceipts is never removed (BIR 10-year retention).
-- Schema mirrors Pos_OfficialReceipts plus ArchivedAt and ArchivedHash.
-- Append-only: once a row is archived it is permanent.
-- =============================================================================

CREATE TABLE IF NOT EXISTS `Pos_OfficialReceiptArchive` (
    `Id`               INT           NOT NULL,
    `OriginalId`       INT           NOT NULL   COMMENT 'PK of the source Pos_OfficialReceipts row',
    `TransactionId`    INT           NOT NULL,
    `ReceiptNumber`    VARCHAR(50)   NOT NULL,
    `BusinessName`     VARCHAR(255)  NOT NULL,
    `BusinessAddress`  VARCHAR(500)  NOT NULL,
    `BusinessTIN`      VARCHAR(50)   NOT NULL,
    `IssueDate`        DATETIME(6)   NOT NULL,
    `Items`            TEXT          NULL,
    `TotalAmount`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `VatAmount`        DECIMAL(18,4) NOT NULL DEFAULT 0,
    `IsVatRegistered`  TINYINT(1)    NOT NULL DEFAULT 0,
    `ArchivedAt`       DATETIME(6)   NOT NULL,
    `ArchivedHash`     VARCHAR(64)   NOT NULL   COMMENT 'SHA-256 of the canonical receipt payload at archive time',
    `CreatedBy`        VARCHAR(100)  NOT NULL,
    `CreatedAt`        DATETIME(6)   NOT NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Pos_OfficialReceiptArchive_OriginalId`     (`OriginalId`),
    UNIQUE INDEX `UIX_Pos_OfficialReceiptArchive_ReceiptNumber`  (`ReceiptNumber`),
    INDEX `IX_Pos_OfficialReceiptArchive_IssueDate`              (`IssueDate`),
    INDEX `IX_Pos_OfficialReceiptArchive_ArchivedAt`             (`ArchivedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='BIR 10-year cold storage. Append-only. Mirrors Pos_OfficialReceipts.';


-- =============================================================================
-- Immutability triggers — Pos_OfficialReceiptArchive
-- Archive rows must be permanent once inserted.  Blocking UPDATE and DELETE at
-- the DB layer provides defence-in-depth alongside the application-level policy.
--
-- DEFINER = CURRENT_USER: trigger is owned by the admin account that applies
-- this script, not a root superuser (reduces privilege-escalation surface).
-- SQL SECURITY INVOKER is not a valid trigger attribute in MariaDB 11.4;
-- privilege separation is enforced via the sync role grants in 01-mariadb-init.sql.
-- =============================================================================

DELIMITER $$

CREATE DEFINER = CURRENT_USER TRIGGER `trg_pos_official_receipt_archive_block_update`
BEFORE UPDATE ON `Pos_OfficialReceiptArchive`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Pos_OfficialReceiptArchive is append-only; UPDATE blocked.';
END$$

CREATE DEFINER = CURRENT_USER TRIGGER `trg_pos_official_receipt_archive_block_delete`
BEFORE DELETE ON `Pos_OfficialReceiptArchive`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Pos_OfficialReceiptArchive is append-only; DELETE blocked.';
END$$

DELIMITER ;
