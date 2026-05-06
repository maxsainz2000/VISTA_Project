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
| `Entities/SalesTransaction.vb` | `SalesTransaction` | `SoftDeletableEntity` | `TransactionNumber`, `TotalAmount`, `PaymentMethod` | `Pos_SalesTransactions` |
| `Entities/SalesTransactionLine.vb` | `SalesTransactionLine` | `AuditableEntity` | `TransactionId`, `ProductId`, `Quantity`, `UnitPrice` | `Pos_SalesTransactionLines` |
| `Entities/CreditAccount.vb` | `CreditAccount` | `SoftDeletableEntity` | `CustomerId`, `CustomerName`, `CreditLimit`, `Balance` | `Pos_CreditAccounts` |
| `Entities/CreditPayment.vb` | `CreditPayment` | `AuditableEntity` | `AccountId`, `Amount`, `PaymentDate` | `Pos_CreditPayments` |
| `Entities/SalesReturn.vb` | `SalesReturn` | `AuditableEntity` | `OriginalTransactionId`, `ReturnDate`, `Reason`, `RefundAmount` | `Pos_SalesReturns` |
| `Entities/OfficialReceipt.vb` | `OfficialReceipt` | `AuditableEntity` | `ReceiptNumber`, `IssueDate`, `TotalAmount` | `Pos_OfficialReceipts` |

## Value Objects / DTOs
None documented for this layer.
