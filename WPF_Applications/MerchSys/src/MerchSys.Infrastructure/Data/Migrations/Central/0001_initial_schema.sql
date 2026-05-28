-- VISTA Centralized MariaDB Initialization Schema
-- Migration ID: 0001_initial_schema.sql
-- Reference Plans: INFRA-24

CREATE TABLE IF NOT EXISTS `__SchemaMigrations` (
    `ScriptName` VARCHAR(128) NOT NULL,
    `AppliedAt`  DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `Sha256`     VARCHAR(64)  NOT NULL,
    PRIMARY KEY (`ScriptName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 1. Purchasing Module
CREATE TABLE IF NOT EXISTS `Pur_Vendors` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(255) NOT NULL,
    `ContactPerson` VARCHAR(255) NOT NULL,
    `Phone` VARCHAR(64) NOT NULL,
    `Email` VARCHAR(255) NULL,
    `Address` VARCHAR(512) NOT NULL,
    `DefaultLeadTimeDays` INT NOT NULL,
    `Notes` TEXT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeletedBy` VARCHAR(64) NULL,
    `DeletedAt` DATETIME(6) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pur_Vendors_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_PurchaseOrders` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `OrderNumber` VARCHAR(128) NOT NULL,
    `VendorId` INT NOT NULL,
    `Status` INT NOT NULL,
    `OrderDate` DATETIME(6) NOT NULL,
    `ExpectedDeliveryDate` DATETIME(6) NULL,
    `TotalAmount` DECIMAL(18, 4) NOT NULL,
    `Notes` TEXT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeletedBy` VARCHAR(64) NULL,
    `DeletedAt` DATETIME(6) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pur_PurchaseOrders_OrderNumber` (`OrderNumber`),
    KEY `IX_Pur_PurchaseOrders_VendorId` (`VendorId`),
    CONSTRAINT `FK_Pur_PurchaseOrders_VendorId` FOREIGN KEY (`VendorId`) REFERENCES `Pur_Vendors` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_PurchaseOrderLines` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `PurchaseOrderId` INT NOT NULL,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `QuantityOrdered` INT NOT NULL,
    `UnitCost` DECIMAL(18, 4) NOT NULL,
    `LineTotal` DECIMAL(18, 4) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Pur_PurchaseOrderLines_PurchaseOrderId` (`PurchaseOrderId`),
    CONSTRAINT `FK_Pur_PurchaseOrderLines_PurchaseOrderId` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `Pur_PurchaseOrders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_GoodsReceipts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `PurchaseOrderId` INT NOT NULL,
    `ReceiptNumber` VARCHAR(128) NOT NULL,
    `ReceivedDate` DATETIME(6) NOT NULL,
    `ReceivedBy` VARCHAR(64) NULL,
    `Notes` TEXT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pur_GoodsReceipts_ReceiptNumber` (`ReceiptNumber`),
    KEY `IX_Pur_GoodsReceipts_PurchaseOrderId` (`PurchaseOrderId`),
    CONSTRAINT `FK_Pur_GoodsReceipts_PurchaseOrderId` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `Pur_PurchaseOrders` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_GoodsReceiptLines` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `GoodsReceiptId` INT NOT NULL,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `QuantityOrdered` INT NOT NULL,
    `QuantityReceived` INT NOT NULL,
    `UnitCost` DECIMAL(18, 4) NOT NULL,
    `ExpiryDate` DATETIME(6) NULL,
    `HasDiscrepancy` TINYINT(1) NOT NULL DEFAULT 0,
    `DiscrepancyNotes` TEXT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `VatClassification` INT NOT NULL DEFAULT 0,
    `VatAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `VatableSales` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    PRIMARY KEY (`Id`),
    KEY `IX_Pur_GoodsReceiptLines_GoodsReceiptId` (`GoodsReceiptId`),
    CONSTRAINT `FK_Pur_GoodsReceiptLines_GoodsReceiptId` FOREIGN KEY (`GoodsReceiptId`) REFERENCES `Pur_GoodsReceipts` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_AccountsPayable` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `PurchaseOrderId` INT NOT NULL,
    `VendorId` INT NOT NULL,
    `InvoiceNumber` VARCHAR(128) NULL,
    `InvoiceDate` DATETIME(6) NOT NULL,
    `DueDate` DATETIME(6) NOT NULL,
    `TotalAmount` DECIMAL(18, 4) NOT NULL,
    `AmountPaid` DECIMAL(18, 4) NOT NULL,
    `Balance` DECIMAL(18, 4) NOT NULL,
    `IsPaid` TINYINT(1) NOT NULL DEFAULT 0,
    `Notes` TEXT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pur_AccountsPayable_PurchaseOrderId` (`PurchaseOrderId`),
    KEY `IX_Pur_AccountsPayable_VendorId_IsPaid` (`VendorId`, `IsPaid`),
    CONSTRAINT `FK_Pur_AccountsPayable_PurchaseOrderId` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `Pur_PurchaseOrders` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Pur_AccountsPayable_VendorId` FOREIGN KEY (`VendorId`) REFERENCES `Pur_Vendors` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_ReorderConfigs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `PreferredVendorId` INT NULL,
    `MinimumThreshold` INT NOT NULL,
    `SafetyStock` INT NOT NULL,
    `DefaultOrderQuantity` INT NOT NULL,
    `LeadTimeDays` INT NOT NULL,
    `IsSeasonalItem` TINYINT(1) NOT NULL DEFAULT 0,
    `SeasonalMultiplier` DECIMAL(18, 4) NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pur_ReorderConfigs_ProductId` (`ProductId`),
    KEY `IX_Pur_ReorderConfigs_IsActive` (`IsActive`),
    CONSTRAINT `FK_Pur_ReorderConfigs_PreferredVendorId` FOREIGN KEY (`PreferredVendorId`) REFERENCES `Pur_Vendors` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_ReorderSuggestions` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `CurrentStock` INT NOT NULL,
    `ReorderPoint` INT NOT NULL,
    `SuggestedQuantity` INT NOT NULL,
    `PreferredVendorId` INT NULL,
    `PreferredVendorName` VARCHAR(255) NULL,
    `EstimatedLeadTimeDays` INT NOT NULL,
    `IsSeasonalAdjusted` TINYINT(1) NOT NULL DEFAULT 0,
    `Status` VARCHAR(64) NOT NULL,
    `ConvertedToPOId` INT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Pur_ReorderSuggestions_Status` (`Status`),
    KEY `IX_Pur_ReorderSuggestions_ProductId_Status` (`ProductId`, `Status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_PriceChangeAlerts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `VendorId` INT NOT NULL,
    `VendorName` VARCHAR(255) NOT NULL,
    `PreviousUnitCost` DECIMAL(18, 4) NOT NULL,
    `NewUnitCost` DECIMAL(18, 4) NOT NULL,
    `ChangePercent` DECIMAL(18, 4) NOT NULL,
    `ChangeDirection` VARCHAR(32) NOT NULL,
    `GoodsReceiptId` INT NOT NULL,
    `IsAcknowledged` TINYINT(1) NOT NULL DEFAULT 0,
    `AcknowledgedAt` DATETIME(6) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Pur_PriceChangeAlerts_IsAcknowledged` (`IsAcknowledged`),
    KEY `IX_Pur_PriceChangeAlerts_ProductId` (`ProductId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pur_VendorProducts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `VendorId` INT NOT NULL,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `LastUnitCost` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `Notes` TEXT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeletedBy` VARCHAR(64) NULL,
    `DeletedAt` DATETIME(6) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `UX_Pur_VendorProducts_Vendor_Product` (`VendorId`, `ProductId`) USING BTREE,
    CONSTRAINT `FK_Pur_VendorProducts_VendorId` FOREIGN KEY (`VendorId`) REFERENCES `Pur_Vendors` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 2. Inventory Module
CREATE TABLE IF NOT EXISTS `Inv_ProductCategories` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(255) NOT NULL,
    `Description` TEXT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeletedBy` VARCHAR(64) NULL,
    `DeletedAt` DATETIME(6) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Inv_ProductCategories_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_Products` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(255) NOT NULL,
    `Sku` VARCHAR(128) NOT NULL,
    `CategoryId` INT NOT NULL,
    `Description` TEXT NULL,
    `RetailPrice` DECIMAL(18, 4) NOT NULL,
    `Unit` VARCHAR(64) NULL,
    `HasExpiry` TINYINT(1) NOT NULL DEFAULT 0,
    `MinimumThreshold` INT NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeletedBy` VARCHAR(64) NULL,
    `DeletedAt` DATETIME(6) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Inv_Products_Sku` (`Sku`),
    CONSTRAINT `FK_Inv_Products_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Inv_ProductCategories` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_StockBatches` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `QuantityReceived` INT NOT NULL,
    `QuantityRemaining` INT NOT NULL,
    `UnitCost` DECIMAL(18, 4) NOT NULL,
    `ReceiptDate` DATETIME(6) NOT NULL,
    `ExpiryDate` DATETIME(6) NULL,
    `SourcePurchaseOrderId` INT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    KEY `IX_Inv_StockBatches_ProductId_ReceiptDate` (`ProductId`, `ReceiptDate`),
    CONSTRAINT `FK_Inv_StockBatches_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Inv_Products` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_StockMovements` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `MovementType` VARCHAR(64) NOT NULL,
    `Quantity` INT NOT NULL,
    `OccurredAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Inv_StockMovements_ProductId_OccurredAt` (`ProductId`, `OccurredAt`),
    CONSTRAINT `FK_Inv_StockMovements_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Inv_Products` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_ShrinkageRecords` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `StockBatchId` INT NULL,
    `QuantityLost` INT NOT NULL,
    `UnitCost` DECIMAL(18, 4) NOT NULL,
    `TotalValue` DECIMAL(18, 4) NOT NULL,
    `Reason` VARCHAR(255) NOT NULL,
    `Notes` TEXT NULL,
    `RecordedDate` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Inv_ShrinkageRecords_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Inv_Products` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Inv_ShrinkageRecords_StockBatchId` FOREIGN KEY (`StockBatchId`) REFERENCES `Inv_StockBatches` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_StockAlertConfigs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `MinimumThreshold` INT NOT NULL,
    `ExpiryAlertDays` INT NOT NULL,
    `IsAlertEnabled` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Inv_StockAlertConfigs_ProductId` (`ProductId`),
    CONSTRAINT `FK_Inv_StockAlertConfigs_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Inv_Products` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_StockAuditRecords` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `ExpectedQuantity` INT NOT NULL,
    `PhysicalCount` INT NOT NULL,
    `Variance` INT NOT NULL,
    `Reason` VARCHAR(255) NOT NULL,
    `Notes` TEXT NULL,
    `PerformedBy` VARCHAR(64) NOT NULL,
    `AuditedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Inv_StockAuditRecords_ProductId` (`ProductId`),
    CONSTRAINT `FK_Inv_StockAuditRecords_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Inv_Products` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Inv_ProductPriceHistory` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ProductId` INT NOT NULL,
    `OldPrice` DECIMAL(18, 4) NOT NULL,
    `NewPrice` DECIMAL(18, 4) NOT NULL,
    `ChangedAt` DATETIME(6) NOT NULL,
    `ChangedBy` VARCHAR(64) NOT NULL,
    `Reason` TEXT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Inv_ProductPriceHistory_ProductId_ChangedAt` (`ProductId`, `ChangedAt` DESC),
    CONSTRAINT `FK_Inv_ProductPriceHistory_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Inv_Products` (`Id`) ON DELETE RESTRICT
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
    KEY `IX_Inv_SaleCogs_Tx_Product` (`TransactionId`, `ProductId`),
    KEY `IX_Inv_SaleCogs_Batch` (`BatchId`),
    CONSTRAINT `FK_Inv_SaleCogs_BatchId` FOREIGN KEY (`BatchId`) REFERENCES `Inv_StockBatches` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 3. POS Module
CREATE TABLE IF NOT EXISTS `Pos_CreditAccounts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `CustomerName` VARCHAR(255) NOT NULL,
    `Phone` VARCHAR(64) NULL,
    `Address` VARCHAR(512) NULL,
    `CurrentBalance` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `TotalCreditExtended` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `TotalPaymentsReceived` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `IsBlocked` TINYINT(1) NOT NULL DEFAULT 0,
    `LastTransactionDate` DATETIME(6) NULL,
    `Notes` TEXT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeletedBy` VARCHAR(64) NULL,
    `DeletedAt` DATETIME(6) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    KEY `IX_Pos_CreditAccounts_IsBlocked` (`IsBlocked`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_SalesTransactions` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `TransactionNumber` VARCHAR(128) NOT NULL,
    `TransactionDate` DATETIME(6) NOT NULL,
    `CustomerId` INT NULL,
    `CustomerName` VARCHAR(255) NULL,
    `PaymentMethod` INT NOT NULL,
    `SubTotal` DECIMAL(18, 4) NOT NULL,
    `DiscountAmount` DECIMAL(18, 4) NOT NULL,
    `VatAmount` DECIMAL(18, 4) NOT NULL,
    `TotalAmount` DECIMAL(18, 4) NOT NULL,
    `AmountTendered` DECIMAL(18, 4) NOT NULL,
    `ChangeAmount` DECIMAL(18, 4) NOT NULL,
    `IsVoided` TINYINT(1) NOT NULL DEFAULT 0,
    `VoidReason` TEXT NULL,
    `CreditAccountId` INT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeletedBy` VARCHAR(64) NULL,
    `DeletedAt` DATETIME(6) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `VatableSales` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `VatExemptSales` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `ZeroRatedSales` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `VatRateSnapshot` DECIMAL(18, 4) NOT NULL DEFAULT 0.1200,
    `IsVatRegisteredSnapshot` TINYINT(1) NOT NULL DEFAULT 0,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pos_SalesTransactions_TransactionNumber` (`TransactionNumber`),
    KEY `IX_Pos_SalesTransactions_TransactionDate` (`TransactionDate`),
    KEY `IX_Pos_SalesTransactions_CreditAccountId` (`CreditAccountId`),
    CONSTRAINT `FK_Pos_SalesTransactions_CreditAccountId` FOREIGN KEY (`CreditAccountId`) REFERENCES `Pos_CreditAccounts` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_SalesTransactionLines` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `TransactionId` INT NOT NULL,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `Quantity` INT NOT NULL,
    `UnitPrice` DECIMAL(18, 4) NOT NULL,
    `DiscountAmount` DECIMAL(18, 4) NOT NULL,
    `LineTotal` DECIMAL(18, 4) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `Treatment` INT NOT NULL DEFAULT 0,
    `VatableAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `VatExemptAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `ZeroRatedAmount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `OutputVat` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    PRIMARY KEY (`Id`),
    KEY `IX_Pos_SalesTransactionLines_TransactionId` (`TransactionId`),
    CONSTRAINT `FK_Pos_SalesTransactionLines_TransactionId` FOREIGN KEY (`TransactionId`) REFERENCES `Pos_SalesTransactions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_OfficialReceipts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `TransactionId` INT NOT NULL,
    `ReceiptNumber` VARCHAR(128) NOT NULL,
    `BusinessName` VARCHAR(255) NOT NULL,
    `BusinessAddress` VARCHAR(512) NULL,
    `BusinessTIN` VARCHAR(64) NULL,
    `IssueDate` DATETIME(6) NOT NULL,
    `Items` TEXT NULL,
    `TotalAmount` DECIMAL(18, 4) NOT NULL,
    `VatAmount` DECIMAL(18, 4) NOT NULL,
    `IsVatRegistered` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pos_OfficialReceipts_ReceiptNumber` (`ReceiptNumber`),
    UNIQUE KEY `IX_Pos_OfficialReceipts_TransactionId` (`TransactionId`),
    CONSTRAINT `FK_Pos_OfficialReceipts_TransactionId` FOREIGN KEY (`TransactionId`) REFERENCES `Pos_SalesTransactions` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_CreditPayments` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `CreditAccountId` INT NOT NULL,
    `PaymentAmount` DECIMAL(18, 4) NOT NULL,
    `PaymentDate` DATETIME(6) NOT NULL,
    `PaymentMethod` INT NOT NULL,
    `Notes` TEXT NULL,
    `ReceivedBy` VARCHAR(64) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Pos_CreditPayments_CreditAccountId` (`CreditAccountId`),
    CONSTRAINT `FK_Pos_CreditPayments_CreditAccountId` FOREIGN KEY (`CreditAccountId`) REFERENCES `Pos_CreditAccounts` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_SalesReturns` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `OriginalTransactionId` INT NOT NULL,
    `ReturnDate` DATETIME(6) NOT NULL,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `QuantityReturned` INT NOT NULL,
    `UnitPrice` DECIMAL(18, 4) NOT NULL,
    `RefundAmount` DECIMAL(18, 4) NOT NULL,
    `Reason` VARCHAR(255) NOT NULL,
    `IsRestocked` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Pos_SalesReturns_OriginalTransactionId` (`OriginalTransactionId`),
    CONSTRAINT `FK_Pos_SalesReturns_OriginalTransactionId` FOREIGN KEY (`OriginalTransactionId`) REFERENCES `Pos_SalesTransactions` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_ReceiptSequence` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Year` INT NOT NULL,
    `NextValue` INT NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pos_ReceiptSequence_Year` (`Year`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_ReceiptIntegrity` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `ReceiptId` INT NOT NULL,
    `IntegrityHash` VARCHAR(256) NOT NULL,
    `PreviousHash` VARCHAR(256) NOT NULL,
    `RetentionExpiresAt` DATETIME(6) NOT NULL,
    `IsImmutable` TINYINT(1) NOT NULL DEFAULT 1,
    `HashAlgorithm` VARCHAR(64) NOT NULL,
    `CanonicalPayload` LONGTEXT NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Pos_ReceiptIntegrity_ReceiptId` (`ReceiptId`),
    KEY `IX_Pos_ReceiptIntegrity_RetentionExpiresAt` (`RetentionExpiresAt`),
    CONSTRAINT `FK_Pos_ReceiptIntegrity_ReceiptId` FOREIGN KEY (`ReceiptId`) REFERENCES `Pos_OfficialReceipts` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_OfficialReceiptArchive` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `OriginalReceiptId` INT NOT NULL,
    `TransactionId` INT NOT NULL,
    `ReceiptNumber` VARCHAR(128) NOT NULL,
    `BusinessName` VARCHAR(255) NOT NULL,
    `BusinessAddress` VARCHAR(512) NULL,
    `BusinessTIN` VARCHAR(64) NULL,
    `IssueDate` DATETIME(6) NOT NULL,
    `Items` TEXT NULL,
    `TotalAmount` DECIMAL(18, 4) NOT NULL,
    `VatAmount` DECIMAL(18, 4) NOT NULL,
    `IsVatRegistered` TINYINT(1) NOT NULL DEFAULT 0,
    `ArchivedAt` DATETIME(6) NOT NULL,
    `ArchivedHash` VARCHAR(256) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Pos_OfficialReceiptArchive_OriginalReceiptId` (`OriginalReceiptId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_ReceiptIntegrityArchive` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `OriginalIntegrityId` INT NOT NULL,
    `ReceiptId` INT NOT NULL,
    `IntegrityHash` VARCHAR(256) NOT NULL,
    `PreviousHash` VARCHAR(256) NOT NULL,
    `RetentionExpiresAt` DATETIME(6) NOT NULL,
    `IsImmutable` TINYINT(1) NOT NULL DEFAULT 1,
    `HashAlgorithm` VARCHAR(64) NOT NULL,
    `CanonicalPayload` LONGTEXT NOT NULL,
    `ArchivedAt` DATETIME(6) NOT NULL,
    `ArchivedByService` VARCHAR(128) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Pos_ReceiptIntegrityArchive_ReceiptId` (`ReceiptId`),
    KEY `IX_Pos_ReceiptIntegrityArchive_OriginalIntegrityId` (`OriginalIntegrityId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_ArchivalSession` (
    `key` VARCHAR(128) NOT NULL,
    `value` INT NOT NULL,
    `expires_at` DATETIME(6) NOT NULL,
    PRIMARY KEY (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Pos_VatConfiguration` (
    `Id` INT NOT NULL,
    `IsVatRegistered` TINYINT(1) NOT NULL DEFAULT 0,
    `VatRate` DECIMAL(18, 4) NOT NULL,
    `NonVatPercentageTaxRate` DECIMAL(18, 4) NOT NULL,
    `EffectiveFrom` DATETIME(6) NOT NULL,
    `BusinessTIN` VARCHAR(64) NULL,
    `BusinessName` VARCHAR(255) NULL,
    `BusinessAddress` VARCHAR(512) NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    CONSTRAINT `CK_Pos_VatConfiguration_SingleRow` CHECK (`Id` = 1)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 4. Accounting Module
CREATE TABLE IF NOT EXISTS `Acc_FinancialPeriods` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `PeriodType` VARCHAR(64) NOT NULL,
    `StartDate` DATETIME(6) NOT NULL,
    `EndDate` DATETIME(6) NOT NULL,
    `TotalRevenue` DECIMAL(18, 4) NOT NULL,
    `TotalCOGS` DECIMAL(18, 4) NOT NULL,
    `GrossProfit` DECIMAL(18, 4) NOT NULL,
    `GrossMarginPercent` DECIMAL(18, 4) NOT NULL,
    `TotalExpenses` DECIMAL(18, 4) NOT NULL,
    `NetIncome` DECIMAL(18, 4) NOT NULL,
    `IsClosed` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_RevenueRecords` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `RecordDate` DATETIME(6) NOT NULL,
    `SourceTransactionId` INT NOT NULL,
    `PaymentMethod` INT NOT NULL,
    `GrossAmount` DECIMAL(18, 4) NOT NULL,
    `DiscountAmount` DECIMAL(18, 4) NOT NULL,
    `NetAmount` DECIMAL(18, 4) NOT NULL,
    `VatAmount` DECIMAL(18, 4) NOT NULL,
    `ProductId` INT NOT NULL,
    `ProductName` VARCHAR(255) NOT NULL,
    `QuantitySold` INT NOT NULL,
    `COGS` DECIMAL(18, 4) NOT NULL,
    `GrossProfit` DECIMAL(18, 4) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Acc_RevenueRecords_RecordDate` (`RecordDate`),
    KEY `IX_Acc_RevenueRecords_ProductId` (`ProductId`),
    CONSTRAINT `FK_Acc_RevenueRecords_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Inv_Products` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_ExpenseRecords` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `RecordDate` DATETIME(6) NOT NULL,
    `Category` VARCHAR(128) NOT NULL,
    `Description` TEXT NULL,
    `Amount` DECIMAL(18, 4) NOT NULL,
    `SourceModule` VARCHAR(128) NOT NULL,
    `SourceReferenceId` INT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_FinancialSnapshots` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `SnapshotDate` DATETIME(6) NOT NULL,
    `TotalAR` DECIMAL(18, 4) NOT NULL,
    `TotalAP` DECIMAL(18, 4) NOT NULL,
    `InventoryValue` DECIMAL(18, 4) NOT NULL,
    `TodayRevenue` DECIMAL(18, 4) NOT NULL,
    `MonthToDateRevenue` DECIMAL(18, 4) NOT NULL,
    `YearToDateRevenue` DECIMAL(18, 4) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Acc_FinancialSnapshots_SnapshotDate` (`SnapshotDate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_TamperAuditLog` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `DetectedAt` DATETIME(6) NOT NULL,
    `ReceiptId` INT NOT NULL,
    `ReceiptNumber` VARCHAR(128) NOT NULL,
    `TamperKind` VARCHAR(128) NOT NULL,
    `DetectedByService` VARCHAR(255) NOT NULL,
    `ExpectedValue` TEXT NULL,
    `ActualValue` TEXT NULL,
    `AdditionalContextJson` LONGTEXT NULL,
    `MachineName` VARCHAR(255) NOT NULL,
    `OperatingUser` VARCHAR(255) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Acc_TamperAuditLog_DetectedAt_TamperKind` (`DetectedAt`, `TamperKind`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_VatReturns` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Year` INT NOT NULL,
    `Period` INT NOT NULL,
    `PeriodType` INT NOT NULL,
    `FormType` INT NOT NULL,
    `FilingStatus` INT NOT NULL,
    `FilingDate` DATETIME(6) NULL,
    `VatableSales` DECIMAL(18, 4) NOT NULL,
    `VatExemptSales` DECIMAL(18, 4) NOT NULL,
    `ZeroRatedSales` DECIMAL(18, 4) NOT NULL,
    `OutputVat` DECIMAL(18, 4) NOT NULL,
    `VatablePurchases` DECIMAL(18, 4) NOT NULL,
    `VatExemptPurchases` DECIMAL(18, 4) NOT NULL,
    `ZeroRatedPurchases` DECIMAL(18, 4) NOT NULL,
    `InputVat` DECIMAL(18, 4) NOT NULL,
    `VatPayable` DECIMAL(18, 4) NOT NULL,
    `VatRefundable` DECIMAL(18, 4) NOT NULL,
    `IsAmended` TINYINT(1) NOT NULL DEFAULT 0,
    `AmendedReturnId` INT NULL,
    `VatRateSnapshot` DECIMAL(18, 4) NOT NULL,
    `IsVatRegisteredSnapshot` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Acc_VatReturns_Year_Period_PeriodType_FormType_Active` (`Year`, `Period`, `PeriodType`, `FormType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `Acc_VatReturnLines` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `VatReturnId` INT NOT NULL,
    `SourceModule` VARCHAR(128) NOT NULL,
    `SourceTable` VARCHAR(128) NOT NULL,
    `SourceRowId` INT NOT NULL,
    `TransactionDate` DATETIME(6) NOT NULL,
    `VatableAmount` DECIMAL(18, 4) NOT NULL,
    `VatExemptAmount` DECIMAL(18, 4) NOT NULL,
    `ZeroRatedAmount` DECIMAL(18, 4) NOT NULL,
    `OutputVat` DECIMAL(18, 4) NOT NULL,
    `InputVat` DECIMAL(18, 4) NOT NULL,
    `Treatment` INT NOT NULL,
    `CreatedBy` VARCHAR(64) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedBy` VARCHAR(64) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_Acc_VatReturnLines_VatReturnId` (`VatReturnId`),
    KEY `IX_Acc_VatReturnLines_SourceModule_SourceTable_SourceRowId` (`SourceModule`, `SourceTable`, `SourceRowId`),
    CONSTRAINT `FK_Acc_VatReturnLines_VatReturnId` FOREIGN KEY (`VatReturnId`) REFERENCES `Acc_VatReturns` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- 5. System/Auth Module
CREATE TABLE IF NOT EXISTS `Sys_UserAccounts` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `Username` VARCHAR(255) NOT NULL,
    `PasswordHash` VARCHAR(255) NOT NULL,
    `Role` INT NOT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `FailedLoginAttempts` INT NOT NULL DEFAULT 0,
    `LockedUntil` DATETIME(6) NULL,
    `LastPasswordChangeAt` DATETIME(6) NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `RowVersion` TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Sys_UserAccounts_Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. BIR Archival Triggers & Immutability Rules
DELIMITER //

CREATE TRIGGER IF NOT EXISTS `tr_acc_tamper_no_update`
BEFORE UPDATE ON `Acc_TamperAuditLog`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'tamper-audit-immutable';
END //

CREATE TRIGGER IF NOT EXISTS `tr_acc_tamper_no_delete`
BEFORE DELETE ON `Acc_TamperAuditLog`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'tamper-audit-immutable';
END //

CREATE TRIGGER IF NOT EXISTS `tr_pos_integrity_archive_no_update`
BEFORE UPDATE ON `Pos_ReceiptIntegrityArchive`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BIR-archive-immutable';
END //

CREATE TRIGGER IF NOT EXISTS `tr_pos_integrity_archive_no_delete`
BEFORE DELETE ON `Pos_ReceiptIntegrityArchive`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BIR-archive-immutable';
END //

CREATE TRIGGER IF NOT EXISTS `tr_pos_receipt_archive_no_update`
BEFORE UPDATE ON `Pos_OfficialReceiptArchive`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BIR-archive-immutable';
END //

CREATE TRIGGER IF NOT EXISTS `tr_pos_receipt_archive_no_delete`
BEFORE DELETE ON `Pos_OfficialReceiptArchive`
FOR EACH ROW
BEGIN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BIR-archive-immutable';
END //

CREATE TRIGGER IF NOT EXISTS `tr_pos_receipts_no_delete`
BEFORE DELETE ON `Pos_OfficialReceipts`
FOR EACH ROW
BEGIN
    DECLARE is_active INT DEFAULT 0;
    
    SELECT COUNT(*) INTO is_active
    FROM `Pos_ArchivalSession`
    WHERE `key` = 'archival_in_progress' AND `expires_at` > CURRENT_TIMESTAMP(6);
    
    IF is_active = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BIR-immutable';
    END IF;
END //

DELIMITER ;
