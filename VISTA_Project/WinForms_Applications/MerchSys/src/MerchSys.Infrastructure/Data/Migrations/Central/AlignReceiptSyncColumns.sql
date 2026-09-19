-- =============================================================================
-- Migration  : AlignReceiptSyncColumns
-- Version    : 1
-- Applied by : INFRA-14 (Central Schema Alignment for Receipt Sync)
-- Target     : merchsys_central.Pos_OfficialReceipts (MariaDB 11.4.x)
-- Depends on : INFRA-06 (mariadb-init.sql v1.0.0), INFRA-08 (receipt triggers)
-- Column origins:
--   Status        — POS-13
--   IssuedAt      — POS-14
--   IntegrityHash — POS-15
-- =============================================================================

USE `merchsys_central`;

ALTER TABLE `Pos_OfficialReceipts`
    ADD COLUMN IF NOT EXISTS `Status`        VARCHAR(20) NOT NULL DEFAULT 'Issued',
    ADD COLUMN IF NOT EXISTS `IssuedAt`      DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    ADD COLUMN IF NOT EXISTS `IntegrityHash` VARCHAR(64) NULL;

ALTER TABLE `Pos_OfficialReceipts`
    ADD INDEX IF NOT EXISTS `IX_OfficialReceipts_IssuedAt` (`IssuedAt`);
