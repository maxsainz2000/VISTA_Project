---
type: schema-map
last-updated: 2026-05-28
---

# Database Schema Mapping

This page maps the EF Core entities across all modules to their central MariaDB tables.

## MerchSys.POS (`Pos_` prefix)
| Entity | DB Table | Key Constraints |
|---|---|---|
| `CreditAccount` | `Pos_CreditAccounts` | PK `Id`, Index on `IsBlocked` |
| `SalesTransaction` | `Pos_SalesTransactions` | PK `Id`, Unique Index on `TransactionNumber`, Index on `TransactionDate`, Index on `CreditAccountId` |
| `SalesTransactionLine` | `Pos_SalesTransactionLines` | PK `Id`, Index on `TransactionId` |
| `OfficialReceipt` | `Pos_OfficialReceipts` | PK `Id`, Unique Index on `ReceiptNumber`, Unique Index on `TransactionId` |
| `CreditPayment` | `Pos_CreditPayments` | PK `Id`, Index on `CreditAccountId` |
| `SalesReturn` | `Pos_SalesReturns` | PK `Id`, Index on `OriginalTransactionId` |
| `ReceiptIntegrity` | `Pos_ReceiptIntegrity` | PK `Id`, Unique Index on `ReceiptId`, FK to `Pos_OfficialReceipts` |
| `ReceiptSequence` | `Pos_ReceiptSequence` | PK `Id`, Unique Index on `Year`, RowVersion token |
| `OfficialReceiptArchive` | `Pos_OfficialReceiptArchive` | PK `Id`, Index on `OriginalReceiptId` |
| `ReceiptIntegrityArchive` | `Pos_ReceiptIntegrityArchive` | PK `Id`, Index on `ReceiptId` |
| `VatConfiguration` | `Pos_VatConfiguration` | PK `Id`, Seed row Id=1 enforced by constraint |
| N/A | `Pos_ArchivalSession` | PK `key`, stores session-aware trigger flags with TTL (INT-12) |

## MerchSys.Purchasing (`Pur_` prefix)
| Entity | DB Table | Key Constraints |
|---|---|---|
| `Vendor` | `Pur_Vendors` | PK `Id`, Unique Index on `Name` |
| `PurchaseOrder` | `Pur_PurchaseOrders` | PK `Id`, Unique Index on `OrderNumber`, Index on `VendorId` |
| `PurchaseOrderLine` | `Pur_PurchaseOrderLines` | PK `Id`, Index on `PurchaseOrderId` |
| `GoodsReceipt` | `Pur_GoodsReceipts` | PK `Id`, Unique Index on `ReceiptNumber`, Index on `PurchaseOrderId` |
| `GoodsReceiptLine` | `Pur_GoodsReceiptLines` | PK `Id`, Index on `GoodsReceiptId` |
| `AccountsPayableEntry` | `Pur_AccountsPayable` | PK `Id`, Unique Index on `PurchaseOrderId`, Index on `VendorId`+`IsPaid` |
| `ReorderConfig` | `Pur_ReorderConfigs` | PK `Id`, Unique Index on `ProductId`, Index on `IsActive` |
| `ReorderSuggestion` | `Pur_ReorderSuggestions` | PK `Id`, Index on `Status`, Index on `ProductId`+`Status` |
| `PriceChangeAlert` | `Pur_PriceChangeAlerts` | PK `Id`, Index on `IsAcknowledged`, Index on `ProductId` |
| `VendorProduct` | `Pur_VendorProducts` | PK `Id`, Unique Index `(VendorId, ProductId)` where `IsDeleted = 0`, FK `VendorId` |

## MerchSys.Inventory (`Inv_` prefix)
| Entity | DB Table | Key Constraints |
|---|---|---|
| `ProductCategory` | `Inv_ProductCategories` | PK `Id`, Unique Index on `Name` |
| `Product` | `Inv_Products` | PK `Id`, Unique Index on `Sku`, FK `CategoryId` -> `Inv_ProductCategories` |
| `StockBatch` | `Inv_StockBatches` | PK `Id`, Index on `ProductId`+`ReceiptDate` |
| `ShrinkageRecord` | `Inv_ShrinkageRecords` | PK `Id` |
| `StockAlertConfig` | `Inv_StockAlertConfigs` | PK `Id` |
| `StockMovement` | `Inv_StockMovements` | PK `Id`, Composite Index (`ProductId`, `OccurredAt`) |
| `StockAuditRecord` | `Inv_StockAuditRecords` | PK `Id`, FK `ProductId` -> `Inv_Products`, Index on `AuditedAt` |
| `ProductPriceHistory` | `Inv_ProductPriceHistory` | PK `Id`, Composite Index `(ProductId, ChangedAt DESC)`, FK `ProductId` |
| `SaleCogsRecord` | `Inv_SaleCogs` | PK `Id`, Composite Index `(TransactionId, ProductId)`, Index on `BatchId`, FK `BatchId` -> `Inv_StockBatches` |

## MerchSys.Accounting (`Acc_` prefix)
| Entity | DB Table | Key Constraints |
|---|---|---|
| `FinancialPeriod` | `Acc_FinancialPeriods` | PK `Id` |
| `RevenueRecord` | `Acc_RevenueRecords` | PK `Id`, Index on `RecordDate`, Index on `ProductId` |
| `ExpenseRecord` | `Acc_ExpenseRecords` | PK `Id` |
| `FinancialSnapshot` | `Acc_FinancialSnapshots` | PK `Id`, Unique Index on `SnapshotDate` |
| `VatReturn` | `Acc_VatReturns` | PK `Id`, Partial Unique Index on `(Year, Period, PeriodType, FormType)` WHERE `FilingStatus != 3` (Amended) |
| `VatReturnLine` | `Acc_VatReturnLines` | PK `Id`, FK `VatReturnId`, Index on `(SourceModule, SourceTable, SourceRowId)` |
| `TamperAuditEntry` | `Acc_TamperAuditLog` | PK `Id`, Composite Index `(DetectedAt, TamperKind)`, Immutability triggers (No UPDATE/DELETE) |

> [!NOTE]
> **Ledger VAT Columns:** `Acc_RevenueRecords` and `Acc_ExpenseRecords` tables carry six additional columns for BIR compliance: `VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, `OutputVat`, `InputVat`, and `VatTreatment`. These were added via migration `AddVatLedgerColumns` (ACC-10).

> [!IMPORTANT]
> **MariaDB Append-Only Triggers (INFRA-08):** To ensure end-to-end tamper-evidence, the following MariaDB tables on the central server are protected by `BEFORE UPDATE` and `BEFORE DELETE` triggers that raise SQLSTATE `45000`:
> - `Pos_ReceiptIntegrity`
> - `Pos_OfficialReceipts`
> - `Pos_OfficialReceiptArchive`
> - These triggers enforce immutability for issued/synced rows, mirroring the SQLite local enforcement (POS-13).

> [!NOTE]
> **Optimistic Concurrency Row Versions (INFRA-26):** Eight mutable central tables (`Inv_StockBatches`, `Inv_Products`, `Pur_AccountsPayable`, `Pur_PurchaseOrders`, `Pos_CreditAccounts`, `Pos_SalesTransactions`, `Pur_Vendors`, `Inv_ProductCategories`) carry a `RowVersion` column configured as a native MariaDB `TIMESTAMP(6)` database-managed optimistic concurrency token.


## System Accounts (`Sys_` prefix)
| Entity | DB Table | Key Constraints |
|---|---|---|
| `UserAccount` | `Sys_UserAccounts` | PK `Id`, Unique Index on `Username` |
