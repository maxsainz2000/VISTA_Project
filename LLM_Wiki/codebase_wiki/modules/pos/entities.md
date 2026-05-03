---
type: layer-manifest
module: MerchSys.POS
layer: Entities
last-updated: 2026-05-03
---

# MerchSys.POS — Entities

## EF Core Entities
| File Path | Class | Inherits | Key Properties | DB Table |
|---|---|---|---|---|
| `Entities/Transaction.vb` | `Transaction` | `SoftDeletableEntity` | `TransactionNumber`, `TotalAmount`, `PaymentMethod` | `Pos_Transactions` |
| `Entities/TransactionItem.vb` | `TransactionItem` | `AuditableEntity` | `TransactionId`, `ProductSku`, `Quantity`, `UnitPrice` | `Pos_TransactionItems` |
| `Entities/CreditAccount.vb` | `CreditAccount` | `SoftDeletableEntity` | `CustomerId`, `CustomerName`, `CreditLimit`, `Balance` | `Pos_CreditAccounts` |
| `Entities/CreditPayment.vb` | `CreditPayment` | `AuditableEntity` | `AccountId`, `Amount`, `PaymentDate` | `Pos_CreditPayments` |
| `Entities/SalesReturn.vb` | `SalesReturn` | `AuditableEntity` | `TransactionId`, `ReturnDate`, `Reason`, `RefundAmount` | `Pos_SalesReturns` |
| `Entities/SalesReturnItem.vb` | `SalesReturnItem` | `AuditableEntity` | `SalesReturnId`, `OriginalItemId`, `QuantityReturned` | `Pos_SalesReturnItems` |

## Value Objects / DTOs
| File Path | Class | Key Properties | Usage |
|---|---|---|---|
| `Entities/CartItem.vb` | `CartItem` | `ProductSku`, `ProductName`, `Quantity`, `UnitPrice` | In-memory cart representation |
| `Entities/DailySummary.vb` | `DailySummary` | `Date`, `TotalSales`, `TransactionCount` | DTO for summary reports |
