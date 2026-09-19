-- Migration: INT-22 Concurrency-safe sequence tables
-- Creates Pos_TransactionSequences and Pur_OrderSequences

CREATE TABLE IF NOT EXISTS `Pos_TransactionSequences` (
    `Year` INT NOT NULL,
    `NextValue` INT NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Year`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_OrderSequences` (
    `SeqKey` VARCHAR(16) NOT NULL,
    `NextValue` INT NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`SeqKey`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
