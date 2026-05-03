---
type: schema-map
last-updated: 2026-05-03
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
*(To be populated during Purchasing implementation)*

## MerchSys.Inventory (`Inv_` prefix)
*(To be populated during Inventory implementation)*

## MerchSys.Accounting (`Acc_` prefix)
*(To be populated during Accounting implementation)*
