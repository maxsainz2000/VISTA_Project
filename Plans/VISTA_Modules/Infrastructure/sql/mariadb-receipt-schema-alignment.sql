-- =============================================================================
-- VISTA MariaDB — Central Receipt Schema Alignment
-- File         : mariadb-receipt-schema-alignment.sql
-- Plan         : INFRA-14 (Central Schema Alignment for Receipt Sync)
-- Target engine: MariaDB 11.4.x LTS
-- Apply after  : 01-mariadb-init.sql (INFRA-06), 02-pos-receipt-integrity-triggers.sql (INFRA-08)
--
-- Purpose
-- -------
-- Adds Status, IssuedAt, and IntegrityHash to the central Pos_OfficialReceipts
-- table so that the SyncOrchestrator (INFRA-12) can push receipt rows from the
-- local SQLite schema (which gained these columns via POS-13 / POS-14 / POS-15)
-- to the central MariaDB sync target without column mismatch errors.
--
-- Column origins:
--   Status        — POS-13 (append-only lifecycle state, domain model initial = 'Issued')
--   IssuedAt      — POS-14 (BIR-mandated issuance timestamp, microsecond precision)
--   IntegrityHash — POS-15 (SHA-256 of canonical receipt payload; nullable because
--                           receipts synced from before POS-13 do not carry a hash)
--
-- Trigger compatibility (INFRA-06 / INFRA-08)
-- -------------------------------------------
-- The existing triggers trg_Pos_OfficialReceipts_NoUpdate and
-- trg_Pos_OfficialReceipts_NoDelete (installed by INFRA-06 mariadb-init.sql)
-- operate at the ROW level via SIGNAL SQLSTATE '45000'. They do not reference
-- any specific column list, so adding columns to the table has no effect on their
-- behaviour. Both triggers continue to block all UPDATE and DELETE operations on
-- Pos_OfficialReceipts unconditionally after this script is applied.
--
-- Idempotency
-- -----------
-- All statements use IF NOT EXISTS so the script can be re-run safely against
-- an instance where the columns already exist (e.g. fresh install via updated
-- mariadb-init.sql). No errors will be raised on a second application.
--
-- Deployment Order
-- ----------------
--   Step 1: mysql -u <admin> -p merchsys_central < 01-mariadb-init.sql
--   Step 2: mysql -u <admin> -p merchsys_central < 02-pos-receipt-integrity-triggers.sql
--   Step 3: mysql -u <admin> -p merchsys_central < mariadb-receipt-schema-alignment.sql
-- =============================================================================

USE `merchsys_central`;

-- Add the three sync-required columns to Pos_OfficialReceipts.
-- Each ADD COLUMN is guarded by IF NOT EXISTS for idempotency.
ALTER TABLE `Pos_OfficialReceipts`
    ADD COLUMN IF NOT EXISTS `Status`        VARCHAR(20) NOT NULL DEFAULT 'Issued',
    ADD COLUMN IF NOT EXISTS `IssuedAt`      DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    ADD COLUMN IF NOT EXISTS `IntegrityHash` VARCHAR(64) NULL;

-- Supporting index on IssuedAt to match the local SQLite index pattern and to
-- support time-range queries on the central receipt table.
ALTER TABLE `Pos_OfficialReceipts`
    ADD INDEX IF NOT EXISTS `IX_OfficialReceipts_IssuedAt` (`IssuedAt`);
