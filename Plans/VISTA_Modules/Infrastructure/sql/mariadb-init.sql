-- =============================================================================
-- VISTA MariaDB Central Schema — mariadb-init.sql
-- Schema version : 1.1.0  (INFRA-14: added Status, IssuedAt, IntegrityHash to Pos_OfficialReceipts)
-- Target engine  : MariaDB 11.4.x LTS
-- Created by     : INFRA-06 (MariaDB Central Schema & Reconciliation)
-- Description    : DDL for all synced tables mirroring the local SQLite 3NF
--                  structure.  Append-only tables (Pos_ BIR/financial and
--                  Acc_*) have BEFORE UPDATE and BEFORE DELETE triggers that
--                  SIGNAL SQLSTATE '45000' to enforce immutability at the DB
--                  layer (defence in depth alongside client-side policy).
-- Usage          : Run as a MariaDB admin user against a fresh instance.
--                  The sync user (merchsys_sync) is granted at the end.
--                  Password placeholder — replace before use; never commit
--                  real credentials.
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `merchsys_central`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `merchsys_central`;

-- =============================================================================
-- Purchasing  (Pur_*)  — LastWriteWins
-- =============================================================================

CREATE TABLE IF NOT EXISTS `Pur_Vendors` (
    `Id`                   INT          NOT NULL,
    `Name`                 VARCHAR(255) NOT NULL,
    `ContactPerson`        VARCHAR(255) NULL,
    `Phone`                VARCHAR(50)  NULL,
    `Email`                VARCHAR(255) NULL,
    `Address`              VARCHAR(500) NULL,
    `DefaultLeadTimeDays`  INT          NOT NULL DEFAULT 0,
    `Notes`                VARCHAR(1000) NULL,
    `IsDeleted`            TINYINT(1)   NOT NULL DEFAULT 0,
    `DeletedBy`            VARCHAR(100) NULL,
    `DeletedAt`            DATETIME(6)  NULL,
    `CreatedBy`            VARCHAR(100) NOT NULL,
    `CreatedAt`            DATETIME(6)  NOT NULL,
    `ModifiedBy`           VARCHAR(100) NULL,
    `ModifiedAt`           DATETIME(6)  NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pur_Vendors_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_PurchaseOrders` (
    `Id`                     INT          NOT NULL,
    `OrderNumber`            VARCHAR(50)  NOT NULL,
    `VendorId`               INT          NOT NULL,
    `Status`                 INT          NOT NULL DEFAULT 0,
    `OrderDate`              DATETIME(6)  NOT NULL,
    `ExpectedDeliveryDate`   DATETIME(6)  NULL,
    `TotalAmount`            DECIMAL(18,4) NOT NULL DEFAULT 0,
    `Notes`                  VARCHAR(1000) NULL,
    `IsDeleted`              TINYINT(1)   NOT NULL DEFAULT 0,
    `DeletedBy`              VARCHAR(100) NULL,
    `DeletedAt`              DATETIME(6)  NULL,
    `CreatedBy`              VARCHAR(100) NOT NULL,
    `CreatedAt`              DATETIME(6)  NOT NULL,
    `ModifiedBy`             VARCHAR(100) NULL,
    `ModifiedAt`             DATETIME(6)  NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Pur_PurchaseOrders_OrderNumber` (`OrderNumber`),
    INDEX `IX_Pur_PurchaseOrders_VendorId` (`VendorId`),
    INDEX `IX_Pur_PurchaseOrders_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_PurchaseOrderLines` (
    `Id`               INT           NOT NULL,
    `PurchaseOrderId`  INT           NOT NULL,
    `ProductId`        INT           NOT NULL,
    `ProductName`      VARCHAR(255)  NOT NULL,
    `QuantityOrdered`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `UnitCost`         DECIMAL(18,4) NOT NULL DEFAULT 0,
    `LineTotal`        DECIMAL(18,4) NOT NULL DEFAULT 0,
    `CreatedBy`        VARCHAR(100)  NOT NULL,
    `CreatedAt`        DATETIME(6)   NOT NULL,
    `ModifiedBy`       VARCHAR(100)  NULL,
    `ModifiedAt`       DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pur_PurchaseOrderLines_PurchaseOrderId` (`PurchaseOrderId`),
    INDEX `IX_Pur_PurchaseOrderLines_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_GoodsReceipts` (
    `Id`               INT          NOT NULL,
    `PurchaseOrderId`  INT          NOT NULL,
    `ReceiptNumber`    VARCHAR(50)  NOT NULL,
    `ReceivedDate`     DATETIME(6)  NOT NULL,
    `ReceivedBy`       VARCHAR(100) NOT NULL,
    `Notes`            VARCHAR(1000) NULL,
    `CreatedBy`        VARCHAR(100) NOT NULL,
    `CreatedAt`        DATETIME(6)  NOT NULL,
    `ModifiedBy`       VARCHAR(100) NULL,
    `ModifiedAt`       DATETIME(6)  NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Pur_GoodsReceipts_ReceiptNumber` (`ReceiptNumber`),
    INDEX `IX_Pur_GoodsReceipts_PurchaseOrderId` (`PurchaseOrderId`),
    INDEX `IX_Pur_GoodsReceipts_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_GoodsReceiptLines` (
    `Id`                INT           NOT NULL,
    `GoodsReceiptId`    INT           NOT NULL,
    `ProductId`         INT           NOT NULL,
    `ProductName`       VARCHAR(255)  NOT NULL,
    `QuantityOrdered`   DECIMAL(18,4) NOT NULL DEFAULT 0,
    `QuantityReceived`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `UnitCost`          DECIMAL(18,4) NOT NULL DEFAULT 0,
    `ExpiryDate`        DATETIME(6)   NULL,
    `HasDiscrepancy`    TINYINT(1)    NOT NULL DEFAULT 0,
    `DiscrepancyNotes`  VARCHAR(500)  NULL,
    `CreatedBy`         VARCHAR(100)  NOT NULL,
    `CreatedAt`         DATETIME(6)   NOT NULL,
    `ModifiedBy`        VARCHAR(100)  NULL,
    `ModifiedAt`        DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pur_GoodsReceiptLines_GoodsReceiptId` (`GoodsReceiptId`),
    INDEX `IX_Pur_GoodsReceiptLines_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_AccountsPayable` (
    `Id`               INT           NOT NULL,
    `PurchaseOrderId`  INT           NOT NULL,
    `VendorId`         INT           NOT NULL,
    `InvoiceNumber`    VARCHAR(100)  NULL,
    `InvoiceDate`      DATETIME(6)   NOT NULL,
    `DueDate`          DATETIME(6)   NOT NULL,
    `TotalAmount`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `AmountPaid`       DECIMAL(18,4) NOT NULL DEFAULT 0,
    `Balance`          DECIMAL(18,4) NOT NULL DEFAULT 0,
    `IsPaid`           TINYINT(1)    NOT NULL DEFAULT 0,
    `Notes`            VARCHAR(1000) NULL,
    `CreatedBy`        VARCHAR(100)  NOT NULL,
    `CreatedAt`        DATETIME(6)   NOT NULL,
    `ModifiedBy`       VARCHAR(100)  NULL,
    `ModifiedAt`       DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Pur_AccountsPayable_PurchaseOrderId` (`PurchaseOrderId`),
    INDEX `IX_Pur_AccountsPayable_VendorId_IsPaid` (`VendorId`, `IsPaid`),
    INDEX `IX_Pur_AccountsPayable_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_ReorderConfigs` (
    `Id`                    INT           NOT NULL,
    `ProductId`             INT           NOT NULL,
    `ProductName`           VARCHAR(255)  NOT NULL,
    `PreferredVendorId`     INT           NULL,
    `MinimumThreshold`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `SafetyStock`           DECIMAL(18,4) NOT NULL DEFAULT 0,
    `DefaultOrderQuantity`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `LeadTimeDays`          INT           NOT NULL DEFAULT 0,
    `IsSeasonalItem`        TINYINT(1)    NOT NULL DEFAULT 0,
    `SeasonalMultiplier`    DECIMAL(18,4) NOT NULL DEFAULT 1,
    `IsActive`              TINYINT(1)    NOT NULL DEFAULT 1,
    `CreatedBy`             VARCHAR(100)  NOT NULL,
    `CreatedAt`             DATETIME(6)   NOT NULL,
    `ModifiedBy`            VARCHAR(100)  NULL,
    `ModifiedAt`            DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Pur_ReorderConfigs_ProductId` (`ProductId`),
    INDEX `IX_Pur_ReorderConfigs_IsActive` (`IsActive`),
    INDEX `IX_Pur_ReorderConfigs_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_ReorderSuggestions` (
    `Id`                      INT           NOT NULL,
    `ProductId`               INT           NOT NULL,
    `ProductName`             VARCHAR(255)  NOT NULL,
    `CurrentStock`            DECIMAL(18,4) NOT NULL DEFAULT 0,
    `ReorderPoint`            DECIMAL(18,4) NOT NULL DEFAULT 0,
    `SuggestedQuantity`       DECIMAL(18,4) NOT NULL DEFAULT 0,
    `PreferredVendorId`       INT           NULL,
    `PreferredVendorName`     VARCHAR(255)  NULL,
    `EstimatedLeadTimeDays`   INT           NOT NULL DEFAULT 0,
    `IsSeasonalAdjusted`      TINYINT(1)    NOT NULL DEFAULT 0,
    `Status`                  VARCHAR(50)   NOT NULL DEFAULT 'Pending',
    `ConvertedToPOId`         INT           NULL,
    `CreatedBy`               VARCHAR(100)  NOT NULL,
    `CreatedAt`               DATETIME(6)   NOT NULL,
    `ModifiedBy`              VARCHAR(100)  NULL,
    `ModifiedAt`              DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pur_ReorderSuggestions_Status` (`Status`),
    INDEX `IX_Pur_ReorderSuggestions_ProductId_Status` (`ProductId`, `Status`),
    INDEX `IX_Pur_ReorderSuggestions_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_PriceChangeAlerts` (
    `Id`                INT           NOT NULL,
    `ProductId`         INT           NOT NULL,
    `ProductName`       VARCHAR(255)  NOT NULL,
    `VendorId`          INT           NOT NULL,
    `VendorName`        VARCHAR(255)  NOT NULL,
    `PreviousUnitCost`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `NewUnitCost`       DECIMAL(18,4) NOT NULL DEFAULT 0,
    `ChangePercent`     DECIMAL(18,4) NOT NULL DEFAULT 0,
    `ChangeDirection`   VARCHAR(20)   NOT NULL,
    `GoodsReceiptId`    INT           NOT NULL,
    `IsAcknowledged`    TINYINT(1)    NOT NULL DEFAULT 0,
    `AcknowledgedAt`    DATETIME(6)   NULL,
    `CreatedBy`         VARCHAR(100)  NOT NULL,
    `CreatedAt`         DATETIME(6)   NOT NULL,
    `ModifiedBy`        VARCHAR(100)  NULL,
    `ModifiedAt`        DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pur_PriceChangeAlerts_IsAcknowledged` (`IsAcknowledged`),
    INDEX `IX_Pur_PriceChangeAlerts_ProductId` (`ProductId`),
    INDEX `IX_Pur_PriceChangeAlerts_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- =============================================================================
-- Inventory  (Inv_*)  — LastWriteWins
-- =============================================================================

CREATE TABLE IF NOT EXISTS `Inv_ProductCategories` (
    `Id`           INT          NOT NULL,
    `Name`         VARCHAR(255) NOT NULL,
    `Description`  VARCHAR(500) NULL,
    `IsDeleted`    TINYINT(1)   NOT NULL DEFAULT 0,
    `DeletedBy`    VARCHAR(100) NULL,
    `DeletedAt`    DATETIME(6)  NULL,
    `CreatedBy`    VARCHAR(100) NOT NULL,
    `CreatedAt`    DATETIME(6)  NOT NULL,
    `ModifiedBy`   VARCHAR(100) NULL,
    `ModifiedAt`   DATETIME(6)  NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Inv_ProductCategories_Name` (`Name`),
    INDEX `IX_Inv_ProductCategories_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_Products` (
    `Id`                INT           NOT NULL,
    `Sku`               VARCHAR(100)  NOT NULL,
    `Name`              VARCHAR(255)  NOT NULL,
    `CategoryId`        INT           NOT NULL,
    `RetailPrice`       DECIMAL(18,4) NOT NULL DEFAULT 0,
    `Unit`              VARCHAR(50)   NOT NULL,
    `HasExpiry`         TINYINT(1)    NOT NULL DEFAULT 0,
    `MinimumThreshold`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `IsDeleted`         TINYINT(1)    NOT NULL DEFAULT 0,
    `DeletedBy`         VARCHAR(100)  NULL,
    `DeletedAt`         DATETIME(6)   NULL,
    `CreatedBy`         VARCHAR(100)  NOT NULL,
    `CreatedAt`         DATETIME(6)   NOT NULL,
    `ModifiedBy`        VARCHAR(100)  NULL,
    `ModifiedAt`        DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Inv_Products_Sku` (`Sku`),
    INDEX `IX_Inv_Products_CategoryId` (`CategoryId`),
    INDEX `IX_Inv_Products_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_StockBatches` (
    `Id`                 INT           NOT NULL,
    `ProductId`          INT           NOT NULL,
    `QuantityReceived`   DECIMAL(18,4) NOT NULL DEFAULT 0,
    `QuantityRemaining`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `UnitCost`           DECIMAL(18,4) NOT NULL DEFAULT 0,
    `ReceiptDate`        DATETIME(6)   NOT NULL,
    `ExpiryDate`         DATETIME(6)   NULL,
    `IsExpired`          TINYINT(1)    NOT NULL DEFAULT 0,
    `IsFullyConsumed`    TINYINT(1)    NOT NULL DEFAULT 0,
    `CreatedBy`          VARCHAR(100)  NOT NULL,
    `CreatedAt`          DATETIME(6)   NOT NULL,
    `ModifiedBy`         VARCHAR(100)  NULL,
    `ModifiedAt`         DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Inv_StockBatches_ProductId_ReceiptDate` (`ProductId`, `ReceiptDate`),
    INDEX `IX_Inv_StockBatches_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_ShrinkageRecords` (
    `Id`            INT           NOT NULL,
    `ProductId`     INT           NOT NULL,
    `Reason`        VARCHAR(500)  NOT NULL,
    `QuantityLost`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `UnitCost`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `TotalValue`    DECIMAL(18,4) NOT NULL DEFAULT 0,
    `CreatedBy`     VARCHAR(100)  NOT NULL,
    `CreatedAt`     DATETIME(6)   NOT NULL,
    `ModifiedBy`    VARCHAR(100)  NULL,
    `ModifiedAt`    DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Inv_ShrinkageRecords_ProductId` (`ProductId`),
    INDEX `IX_Inv_ShrinkageRecords_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_StockAlertConfigs` (
    `Id`                INT           NOT NULL,
    `ProductId`         INT           NOT NULL,
    `MinimumThreshold`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `ExpiryAlertDays`   INT           NOT NULL DEFAULT 30,
    `IsAlertEnabled`    TINYINT(1)    NOT NULL DEFAULT 1,
    `CreatedBy`         VARCHAR(100)  NOT NULL,
    `CreatedAt`         DATETIME(6)   NOT NULL,
    `ModifiedBy`        VARCHAR(100)  NULL,
    `ModifiedAt`        DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Inv_StockAlertConfigs_ProductId` (`ProductId`),
    INDEX `IX_Inv_StockAlertConfigs_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_StockMovements` (
    `Id`            INT           NOT NULL,
    `ProductId`     INT           NOT NULL,
    `MovementType`  INT           NOT NULL,
    `Quantity`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `OccurredAt`    DATETIME(6)   NOT NULL,
    `CreatedBy`     VARCHAR(100)  NOT NULL,
    `CreatedAt`     DATETIME(6)   NOT NULL,
    `ModifiedBy`    VARCHAR(100)  NULL,
    `ModifiedAt`    DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Inv_StockMovements_ProductId_OccurredAt` (`ProductId`, `OccurredAt`),
    INDEX `IX_Inv_StockMovements_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_StockAuditRecords` (
    `Id`                INT           NOT NULL,
    `ProductId`         INT           NOT NULL,
    `ExpectedQuantity`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `PhysicalCount`     DECIMAL(18,4) NOT NULL DEFAULT 0,
    `Variance`          DECIMAL(18,4) NOT NULL DEFAULT 0,
    `Reason`            VARCHAR(500)  NOT NULL,
    `Notes`             VARCHAR(1000) NULL,
    `PerformedBy`       VARCHAR(100)  NOT NULL,
    `AuditedAt`         DATETIME(6)   NOT NULL,
    `CreatedBy`         VARCHAR(100)  NOT NULL,
    `CreatedAt`         DATETIME(6)   NOT NULL,
    `ModifiedBy`        VARCHAR(100)  NULL,
    `ModifiedAt`        DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Inv_StockAuditRecords_ProductId` (`ProductId`),
    INDEX `IX_Inv_StockAuditRecords_AuditedAt` (`AuditedAt`),
    INDEX `IX_Inv_StockAuditRecords_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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





-- =============================================================================
-- POS  (Pos_*)  — Mixed: financial tables AppendOnly, others LastWriteWins
-- =============================================================================

CREATE TABLE IF NOT EXISTS `Pos_CreditAccounts` (
    `Id`           INT           NOT NULL,
    `CustomerName` VARCHAR(255)  NOT NULL,
    `CreditLimit`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `Balance`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `IsBlocked`    TINYINT(1)    NOT NULL DEFAULT 0,
    `IsDeleted`    TINYINT(1)    NOT NULL DEFAULT 0,
    `DeletedBy`    VARCHAR(100)  NULL,
    `DeletedAt`    DATETIME(6)   NULL,
    `CreatedBy`    VARCHAR(100)  NOT NULL,
    `CreatedAt`    DATETIME(6)   NOT NULL,
    `ModifiedBy`   VARCHAR(100)  NULL,
    `ModifiedAt`   DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pos_CreditAccounts_IsBlocked` (`IsBlocked`),
    INDEX `IX_Pos_CreditAccounts_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_SalesTransactions` (
    `Id`               INT           NOT NULL,
    `TransactionNumber` VARCHAR(50)  NOT NULL,
    `TransactionDate`  DATETIME(6)   NOT NULL,
    `CustomerId`       INT           NULL,
    `CustomerName`     VARCHAR(255)  NULL,
    `PaymentMethod`    INT           NOT NULL DEFAULT 0,
    `SubTotal`         DECIMAL(18,4) NOT NULL DEFAULT 0,
    `DiscountAmount`   DECIMAL(18,4) NOT NULL DEFAULT 0,
    `VatAmount`        DECIMAL(18,4) NOT NULL DEFAULT 0,
    `TotalAmount`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `AmountTendered`   DECIMAL(18,4) NOT NULL DEFAULT 0,
    `ChangeAmount`     DECIMAL(18,4) NOT NULL DEFAULT 0,
    `IsVoided`         TINYINT(1)    NOT NULL DEFAULT 0,
    `VoidReason`       VARCHAR(500)  NULL,
    `IsDeleted`        TINYINT(1)    NOT NULL DEFAULT 0,
    `DeletedBy`        VARCHAR(100)  NULL,
    `DeletedAt`        DATETIME(6)   NULL,
    `CreatedBy`        VARCHAR(100)  NOT NULL,
    `CreatedAt`        DATETIME(6)   NOT NULL,
    `ModifiedBy`       VARCHAR(100)  NULL,
    `ModifiedAt`       DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Pos_SalesTransactions_TransactionNumber` (`TransactionNumber`),
    INDEX `IX_Pos_SalesTransactions_TransactionDate` (`TransactionDate`),
    INDEX `IX_Pos_SalesTransactions_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_SalesTransactionLines` (
    `Id`             INT           NOT NULL,
    `TransactionId`  INT           NOT NULL,
    `ProductId`      INT           NOT NULL,
    `ProductName`    VARCHAR(255)  NOT NULL,
    `Quantity`       DECIMAL(18,4) NOT NULL DEFAULT 0,
    `UnitPrice`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `LineTotal`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `CreatedBy`      VARCHAR(100)  NOT NULL,
    `CreatedAt`      DATETIME(6)   NOT NULL,
    `ModifiedBy`     VARCHAR(100)  NULL,
    `ModifiedAt`     DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pos_SalesTransactionLines_TransactionId` (`TransactionId`),
    INDEX `IX_Pos_SalesTransactionLines_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- AppendOnly — BIR mandated (Official Receipts)
-- INFRA-14: Status, IssuedAt, IntegrityHash added to align with local SQLite schema
--           (columns originated in POS-13 / POS-14 / POS-15).
--           Existing installations: apply mariadb-receipt-schema-alignment.sql instead.
CREATE TABLE IF NOT EXISTS `Pos_OfficialReceipts` (
    `Id`               INT           NOT NULL,
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
    `Status`           VARCHAR(20)   NOT NULL DEFAULT 'Issued',
    `IssuedAt`         DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `IntegrityHash`    VARCHAR(64)   NULL,
    `CreatedBy`        VARCHAR(100)  NOT NULL,
    `CreatedAt`        DATETIME(6)   NOT NULL,
    `ModifiedBy`       VARCHAR(100)  NULL,
    `ModifiedAt`       DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Pos_OfficialReceipts_ReceiptNumber` (`ReceiptNumber`),
    UNIQUE INDEX `UIX_Pos_OfficialReceipts_TransactionId` (`TransactionId`),
    INDEX `IX_OfficialReceipts_IssuedAt` (`IssuedAt`),
    INDEX `IX_Pos_OfficialReceipts_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- AppendOnly — BIR mandated (Credit Payments)
CREATE TABLE IF NOT EXISTS `Pos_CreditPayments` (
    `Id`               INT           NOT NULL,
    `CreditAccountId`  INT           NOT NULL,
    `PaymentAmount`    DECIMAL(18,4) NOT NULL DEFAULT 0,
    `PaymentDate`      DATETIME(6)   NOT NULL,
    `PaymentMethod`    INT           NOT NULL DEFAULT 0,
    `Notes`            VARCHAR(500)  NULL,
    `ReceivedBy`       VARCHAR(100)  NOT NULL,
    `CreatedBy`        VARCHAR(100)  NOT NULL,
    `CreatedAt`        DATETIME(6)   NOT NULL,
    `ModifiedBy`       VARCHAR(100)  NULL,
    `ModifiedAt`       DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pos_CreditPayments_CreditAccountId` (`CreditAccountId`),
    INDEX `IX_Pos_CreditPayments_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_SalesReturns` (
    `Id`                      INT           NOT NULL,
    `OriginalTransactionId`   INT           NOT NULL,
    `ReturnDate`              DATETIME(6)   NOT NULL,
    `Reason`                  VARCHAR(500)  NOT NULL,
    `RefundAmount`            DECIMAL(18,4) NOT NULL DEFAULT 0,
    `CreatedBy`               VARCHAR(100)  NOT NULL,
    `CreatedAt`               DATETIME(6)   NOT NULL,
    `ModifiedBy`              VARCHAR(100)  NULL,
    `ModifiedAt`              DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pos_SalesReturns_OriginalTransactionId` (`OriginalTransactionId`),
    INDEX `IX_Pos_SalesReturns_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- AppendOnly — receipt integrity audit log (reserved for future POS implementation)
CREATE TABLE IF NOT EXISTS `Pos_ReceiptIntegrity` (
    `Id`             INT          NOT NULL,
    `TransactionId`  INT          NOT NULL,
    `ReceiptId`      INT          NOT NULL,
    `ChecksumHash`   VARCHAR(64)  NOT NULL,
    `ValidatedAt`    DATETIME(6)  NOT NULL,
    `CreatedBy`      VARCHAR(100) NOT NULL,
    `CreatedAt`      DATETIME(6)  NOT NULL,
    `ModifiedBy`     VARCHAR(100) NULL,
    `ModifiedAt`     DATETIME(6)  NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Pos_ReceiptIntegrity_TransactionId` (`TransactionId`),
    INDEX `IX_Pos_ReceiptIntegrity_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- =============================================================================
-- Accounting  (Acc_*)  — AppendOnly (ledger rows are immutable once posted)
-- =============================================================================

CREATE TABLE IF NOT EXISTS `Acc_FinancialPeriods` (
    `Id`                  INT           NOT NULL,
    `PeriodType`          VARCHAR(20)   NOT NULL,
    `StartDate`           DATETIME(6)   NOT NULL,
    `EndDate`             DATETIME(6)   NOT NULL,
    `TotalRevenue`        DECIMAL(18,4) NOT NULL DEFAULT 0,
    `TotalCOGS`           DECIMAL(18,4) NOT NULL DEFAULT 0,
    `GrossProfit`         DECIMAL(18,4) NOT NULL DEFAULT 0,
    `GrossMarginPercent`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `TotalExpenses`       DECIMAL(18,4) NOT NULL DEFAULT 0,
    `NetIncome`           DECIMAL(18,4) NOT NULL DEFAULT 0,
    `IsClosed`            TINYINT(1)    NOT NULL DEFAULT 0,
    `CreatedBy`           VARCHAR(100)  NOT NULL,
    `CreatedAt`           DATETIME(6)   NOT NULL,
    `ModifiedBy`          VARCHAR(100)  NULL,
    `ModifiedAt`          DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Acc_FinancialPeriods_StartDate` (`StartDate`),
    INDEX `IX_Acc_FinancialPeriods_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_RevenueRecords` (
    `Id`                  INT           NOT NULL,
    `RecordDate`          DATETIME(6)   NOT NULL,
    `SourceTransactionId` INT           NOT NULL,
    `PaymentMethod`       INT           NOT NULL DEFAULT 0,
    `GrossAmount`         DECIMAL(18,4) NOT NULL DEFAULT 0,
    `DiscountAmount`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `NetAmount`           DECIMAL(18,4) NOT NULL DEFAULT 0,
    `VatAmount`           DECIMAL(18,4) NOT NULL DEFAULT 0,
    `ProductId`           INT           NOT NULL,
    `ProductName`         VARCHAR(255)  NOT NULL,
    `QuantitySold`        INT           NOT NULL DEFAULT 0,
    `COGS`                DECIMAL(18,4) NOT NULL DEFAULT 0,
    `GrossProfit`         DECIMAL(18,4) NOT NULL DEFAULT 0,
    `CreatedBy`           VARCHAR(100)  NOT NULL,
    `CreatedAt`           DATETIME(6)   NOT NULL,
    `ModifiedBy`          VARCHAR(100)  NULL,
    `ModifiedAt`          DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Acc_RevenueRecords_RecordDate` (`RecordDate`),
    INDEX `IX_Acc_RevenueRecords_ProductId` (`ProductId`),
    INDEX `IX_Acc_RevenueRecords_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_ExpenseRecords` (
    `Id`                 INT           NOT NULL,
    `RecordDate`         DATETIME(6)   NOT NULL,
    `Category`           VARCHAR(50)   NOT NULL,
    `Description`        VARCHAR(500)  NOT NULL,
    `Amount`             DECIMAL(18,4) NOT NULL DEFAULT 0,
    `SourceModule`       VARCHAR(50)   NOT NULL,
    `SourceReferenceId`  INT           NULL,
    `CreatedBy`          VARCHAR(100)  NOT NULL,
    `CreatedAt`          DATETIME(6)   NOT NULL,
    `ModifiedBy`         VARCHAR(100)  NULL,
    `ModifiedAt`         DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Acc_ExpenseRecords_RecordDate` (`RecordDate`),
    INDEX `IX_Acc_ExpenseRecords_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_FinancialSnapshots` (
    `Id`                  INT           NOT NULL,
    `SnapshotDate`        DATETIME(6)   NOT NULL,
    `TotalAR`             DECIMAL(18,4) NOT NULL DEFAULT 0,
    `TotalAP`             DECIMAL(18,4) NOT NULL DEFAULT 0,
    `InventoryValue`      DECIMAL(18,4) NOT NULL DEFAULT 0,
    `TodayRevenue`        DECIMAL(18,4) NOT NULL DEFAULT 0,
    `MonthToDateRevenue`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `YearToDateRevenue`   DECIMAL(18,4) NOT NULL DEFAULT 0,
    `CreatedBy`           VARCHAR(100)  NOT NULL,
    `CreatedAt`           DATETIME(6)   NOT NULL,
    `ModifiedBy`          VARCHAR(100)  NULL,
    `ModifiedAt`          DATETIME(6)   NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `UIX_Acc_FinancialSnapshots_SnapshotDate` (`SnapshotDate`),
    INDEX `IX_Acc_FinancialSnapshots_ModifiedAt` (`ModifiedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- =============================================================================
-- Immutability triggers — AppendOnly tables
-- SQLSTATE '45000' is the user-defined exception class in MariaDB.
-- Triggered on: Pos_OfficialReceipts, Pos_ReceiptIntegrity, Pos_CreditPayments,
--               Acc_FinancialPeriods, Acc_RevenueRecords, Acc_ExpenseRecords,
--               Acc_FinancialSnapshots
-- =============================================================================

DELIMITER $$

-- Pos_OfficialReceipts
CREATE OR REPLACE TRIGGER trg_Pos_OfficialReceipts_NoUpdate
BEFORE UPDATE ON `Pos_OfficialReceipts`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Updates to Pos_OfficialReceipts are prohibited (BIR compliance)';
END$$

CREATE OR REPLACE TRIGGER trg_Pos_OfficialReceipts_NoDelete
BEFORE DELETE ON `Pos_OfficialReceipts`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Deletions from Pos_OfficialReceipts are prohibited (BIR compliance)';
END$$

-- Pos_ReceiptIntegrity
CREATE OR REPLACE TRIGGER trg_Pos_ReceiptIntegrity_NoUpdate
BEFORE UPDATE ON `Pos_ReceiptIntegrity`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Updates to Pos_ReceiptIntegrity are prohibited (BIR compliance)';
END$$

CREATE OR REPLACE TRIGGER trg_Pos_ReceiptIntegrity_NoDelete
BEFORE DELETE ON `Pos_ReceiptIntegrity`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Deletions from Pos_ReceiptIntegrity are prohibited (BIR compliance)';
END$$

-- Pos_CreditPayments
CREATE OR REPLACE TRIGGER trg_Pos_CreditPayments_NoUpdate
BEFORE UPDATE ON `Pos_CreditPayments`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Updates to Pos_CreditPayments are prohibited (BIR compliance)';
END$$

CREATE OR REPLACE TRIGGER trg_Pos_CreditPayments_NoDelete
BEFORE DELETE ON `Pos_CreditPayments`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Deletions from Pos_CreditPayments are prohibited (BIR compliance)';
END$$

-- Acc_FinancialPeriods
CREATE OR REPLACE TRIGGER trg_Acc_FinancialPeriods_NoUpdate
BEFORE UPDATE ON `Acc_FinancialPeriods`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Updates to Acc_FinancialPeriods are prohibited (ledger immutability)';
END$$

CREATE OR REPLACE TRIGGER trg_Acc_FinancialPeriods_NoDelete
BEFORE DELETE ON `Acc_FinancialPeriods`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Deletions from Acc_FinancialPeriods are prohibited (ledger immutability)';
END$$

-- Acc_RevenueRecords
CREATE OR REPLACE TRIGGER trg_Acc_RevenueRecords_NoUpdate
BEFORE UPDATE ON `Acc_RevenueRecords`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Updates to Acc_RevenueRecords are prohibited (ledger immutability)';
END$$

CREATE OR REPLACE TRIGGER trg_Acc_RevenueRecords_NoDelete
BEFORE DELETE ON `Acc_RevenueRecords`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Deletions from Acc_RevenueRecords are prohibited (ledger immutability)';
END$$

-- Acc_ExpenseRecords
CREATE OR REPLACE TRIGGER trg_Acc_ExpenseRecords_NoUpdate
BEFORE UPDATE ON `Acc_ExpenseRecords`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Updates to Acc_ExpenseRecords are prohibited (ledger immutability)';
END$$

CREATE OR REPLACE TRIGGER trg_Acc_ExpenseRecords_NoDelete
BEFORE DELETE ON `Acc_ExpenseRecords`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Deletions from Acc_ExpenseRecords are prohibited (ledger immutability)';
END$$

-- Acc_FinancialSnapshots
CREATE OR REPLACE TRIGGER trg_Acc_FinancialSnapshots_NoUpdate
BEFORE UPDATE ON `Acc_FinancialSnapshots`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Updates to Acc_FinancialSnapshots are prohibited (ledger immutability)';
END$$

CREATE OR REPLACE TRIGGER trg_Acc_FinancialSnapshots_NoDelete
BEFORE DELETE ON `Acc_FinancialSnapshots`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Deletions from Acc_FinancialSnapshots are prohibited (ledger immutability)';
END$$

DELIMITER ;


-- =============================================================================
-- Access control — sync user role
-- Replace 'CHANGE_ME' with the real password; never commit the real password.
-- =============================================================================

CREATE ROLE IF NOT EXISTS `merchsys_sync_role`;

GRANT SELECT, INSERT, UPDATE ON `merchsys_central`.* TO `merchsys_sync_role`;
-- No DELETE grant; append-only and soft-delete policies are enforced above.

CREATE USER IF NOT EXISTS `merchsys_sync`@`%` IDENTIFIED BY 'CHANGE_ME';
GRANT `merchsys_sync_role` TO `merchsys_sync`@`%`;
SET DEFAULT ROLE `merchsys_sync_role` FOR `merchsys_sync`@`%`;

FLUSH PRIVILEGES;
