---
type: schema-map
last-updated: 2026-05-05
---

# Database Schema Mapping

This page maps the EF Core entities across all modules to their SQLite/MariaDB tables.

## MerchSys.POS (`Pos_` prefix)
| Entity | DB Table | Key Constraints |
|---|---|---|
| `Transaction` | `Pos_Transactions` | PK `Id`, Index on `TransactionNumber` |
| `TransactionItem` | `Pos_TransactionItems` | PK `Id`, FK `TransactionId` -> `Pos_Transactions` |
| `CreditAccount` | `Pos_CreditAccounts` | PK `Id`, Index on `CustomerId` |
| `CreditPayment` | `Pos_CreditPayments` | PK `Id`, FK `AccountId` -> `Pos_CreditAccounts` |
| `SalesReturn` | `Pos_SalesReturns` | PK `Id`, FK `TransactionId` -> `Pos_Transactions` |
| `SalesReturnItem` | `Pos_SalesReturnItems` | PK `Id`, FK `SalesReturnId` -> `Pos_SalesReturns` |

## MerchSys.Purchasing (`Pur_` prefix)
| Entity | DB Table | Key Constraints |
|---|---|---|
| `Vendor` | `Pur_Vendors` | PK `Id`, Index on `Name` |
| `PurchaseOrder` | `Pur_PurchaseOrders` | PK `Id`, FK `VendorId` -> `Pur_Vendors`, Index on `OrderNumber` |
| `PurchaseOrderLine` | `Pur_PurchaseOrderLines` | PK `Id`, FK `PurchaseOrderId` -> `Pur_PurchaseOrders` |
| `GoodsReceipt` | `Pur_GoodsReceipts` | PK `Id`, FK `PurchaseOrderId` -> `Pur_PurchaseOrders`, Index on `ReceiptNumber` |
| `GoodsReceiptLine` | `Pur_GoodsReceiptLines` | PK `Id`, FK `GoodsReceiptId` -> `Pur_GoodsReceipts` |
| `AccountsPayableEntry` | `Pur_AccountsPayable` | PK `Id`, FK `PurchaseOrderId` -> `Pur_PurchaseOrders`, FK `VendorId` -> `Pur_Vendors`, Index on `VendorId`+`IsPaid` |
| `ReorderConfig` | `Pur_ReorderConfigs` | PK `Id`, Unique Index on `ProductId`, nullable FK `PreferredVendorId` -> `Pur_Vendors` (SetNull) |
| `ReorderSuggestion` | `Pur_ReorderSuggestions` | PK `Id`, Composite Index on `(ProductId, Status)` |
| `PriceChangeAlert` | `Pur_PriceChangeAlerts` | PK `Id`, Index on `IsAcknowledged`, Index on `ProductId`, precision(18,4) on cost/percent columns |

## MerchSys.Inventory (`Inv_` prefix)
*(To be populated during Inventory implementation)*

## MerchSys.Accounting (`Acc_` prefix)
| Entity | DB Table | Key Constraints |
|---|---|---|
| `FinancialPeriod` | `Acc_FinancialPeriods` | PK `Id`, Index on `StartDate`+`EndDate` |
| `RevenueRecord` | `Acc_RevenueRecords` | PK `Id`, Index on `ProductId`, Index on `RecordDate` |
| `ExpenseRecord` | `Acc_ExpenseRecords` | PK `Id`, Index on `SourceModule` |
| `FinancialSnapshot` | `Acc_FinancialSnapshots` | PK `Id`, Index on `SnapshotDate` |
