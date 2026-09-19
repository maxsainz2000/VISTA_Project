---
type: layer-manifest
module: MerchSys.POS
layer: Entities
last-updated: 2026-05-22
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
| `Entities/ReceiptIntegrity.vb` | `ReceiptIntegrity` | `AuditableEntity` | `ReceiptId`, `IntegrityHash`, `PreviousHash`. Marked `<NoSync>`. | `Pos_ReceiptIntegrity` |
| `Entities/ReceiptSequence.vb` | `ReceiptSequence` | `AuditableEntity` | `Year`, `NextValue`, `RowVersion` | `Pos_ReceiptSequence` |
| `Entities/OfficialReceiptArchive.vb` | `OfficialReceiptArchive` | N/A | `ArchivedAt`, `ArchivedHash` | `Pos_OfficialReceiptArchive` |
| `Entities/VatConfiguration.vb` | `VatConfiguration` | `AuditableEntity` | `IsVatRegistered`, `VatRate`, `TIN`, `EffectiveFrom` | `Pos_VatConfiguration` |
| `Entities/ReceiptIntegrityArchive.vb` | `ReceiptIntegrityArchive` | N/A | `ReceiptId`, `IntegrityHash`, `ArchivedAt`, `ArchivedByService` | `Pos_ReceiptIntegrityArchive` |

## Entity Extensions

| File Path | Class | Type | Key Properties / Description |
|---|---|---|---|
| `Entities/Extensions/SalesTransactionLineVatExtension.vb` | `SalesTransactionLine` | Partial Extension | Adds per-line VAT breakdown columns: `Treatment`, `VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, and `OutputVat` for BIR compliance. |
| `Entities/Extensions/SalesTransactionVatExtension.vb` | `SalesTransaction` | Partial Extension | Adds three-bucket VAT totals (`VatableSales`, `VatExemptSales`, `ZeroRatedSales`) and snapshots for VAT rate and registration status. |

## Value Objects / DTOs
- **VAT Extensions:** `SalesTransaction` and `SalesTransactionLine` have partial class extensions in `Entities/Extensions/` adding BIR-required VAT decomposition fields.
