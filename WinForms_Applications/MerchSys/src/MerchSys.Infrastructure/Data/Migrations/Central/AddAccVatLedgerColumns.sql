-- =============================================================================
-- Migration  : AddAccVatLedgerColumns
-- Version    : 3
-- Applied by : PUR debug (PUR-14/15 — Goods Receiving Confirm Receipt failure)
-- Target     : merchsys_central.Acc_ExpenseRecords, merchsys_central.Acc_RevenueRecords
-- Depends on : INFRA-06 (0001_initial_schema.sql), ACC-10 (VAT ledger schema)
-- Description: Adds the BIR three-bucket VAT ledger columns that ACC-10 introduced on the
--              ExpenseRecord / RevenueRecord entities (via LedgerVatExtensions.vb partial
--              classes) but which were never added to the central tables. EF Core maps all
--              six columns by convention, so every INSERT lists them — including InputVat —
--              causing "Unknown column 'InputVat' in 'field list'" on Confirm Receipt.
--              Idempotent — safe to re-run.
-- =============================================================================

USE `merchsys_central`;

ALTER TABLE `Acc_ExpenseRecords`
    ADD COLUMN IF NOT EXISTS `VatableAmount`   DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `VatExemptAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `ZeroRatedAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `OutputVat`       DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `InputVat`        DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `VatTreatment`    INT            NOT NULL DEFAULT 0;

ALTER TABLE `Acc_RevenueRecords`
    ADD COLUMN IF NOT EXISTS `VatableAmount`   DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `VatExemptAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `ZeroRatedAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `OutputVat`       DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `InputVat`        DECIMAL(18, 4) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS `VatTreatment`    INT            NOT NULL DEFAULT 0;
