-- =============================================================================
-- Migration  : AddInvSaleCogs
-- Version    : 2
-- Applied by : INFRA-22 (Central Schema Alignment for Inv_SaleCogs)
-- Target     : merchsys_central.Inv_SaleCogs (MariaDB 11.4.x)
-- Depends on : INFRA-06 (mariadb-init.sql v1.0.0), ACC-21 (local Inv_SaleCogs)
-- Description: Central MariaDB schema for Inv_SaleCogs (per-batch FIFO COGS ledger).
--              Companion to ACC-21's local SQLite migration 20260527120000_AddInvSaleCogs.vb.
--              Idempotent — safe to re-run.
-- =============================================================================

USE `merchsys_central`;

CREATE TABLE IF NOT EXISTS `Inv_SaleCogs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `TransactionId` INT NOT NULL,
    `ProductId` INT NOT NULL,
    `BatchId` INT NOT NULL,
    `QuantityDeducted` INT NOT NULL,
    `UnitCost` DECIMAL(18, 4) NOT NULL,
    `Cogs` DECIMAL(18, 4) NOT NULL,
    `DeductedAt` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Inv_SaleCogs_Tx_Product` (`TransactionId`, `ProductId`),
    INDEX `IX_Inv_SaleCogs_Batch` (`BatchId`),
    CONSTRAINT `FK_Inv_SaleCogs_Inv_StockBatches_BatchId`
        FOREIGN KEY (`BatchId`) REFERENCES `Inv_StockBatches` (`Id`)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
