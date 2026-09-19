-- Migration: INT-21 Acc_TamperAuditLog column-type alignment
-- Brings the tamper-audit table in line with the EF model after the SQLite-era
-- column types were removed from TamperAuditEntryConfiguration:
--   * Id / ReceiptId : INT  -> BIGINT  (Int64 in the entity)
--   * ExpectedValue / ActualValue : TEXT -> VARCHAR(512)
--
-- This is a NEW forward migration rather than an edit to 0001_initial_schema.sql:
-- the schema initializer SHA-256-hashes every applied script and aborts on drift,
-- so applied migrations must never be modified in place.
-- MODIFY COLUMN is naturally idempotent; the migration runner applies it once.

ALTER TABLE `Acc_TamperAuditLog`
    MODIFY COLUMN `Id` BIGINT NOT NULL AUTO_INCREMENT,
    MODIFY COLUMN `ReceiptId` BIGINT NOT NULL,
    MODIFY COLUMN `ExpectedValue` VARCHAR(512) NULL,
    MODIFY COLUMN `ActualValue` VARCHAR(512) NULL;
