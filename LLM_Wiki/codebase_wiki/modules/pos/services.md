---
type: layer-manifest
module: MerchSys.POS
layer: Services
last-updated: 2026-05-07
---

# MerchSys.POS — Services

## Core Services
| File Path | Interface & Implementation | Key Methods | Dependencies (DI) |
|---|---|---|---|
| `Services/CartService.vb` | `ICartService` / `CartService` | `CreateCartAsync()`, `AddLineAsync()`, `UpdateLineQuantityAsync()`, `RemoveLineAsync()`, `FinalizeAsync()`, `VoidTransactionAsync()` | `POSDbContext`, `IReceiptService`, `IConfiguration` |
| `Services/PaymentService.vb`| `IPaymentService` / `PaymentService` | `ProcessPaymentAsync()`, `ValidatePayment()` | |
| `Services/CreditService.vb` | `ICreditService` / `CreditService` | `GetAccountAsync()`, `ProcessCreditSaleAsync()`, `RecordPaymentAsync()` | |
| `Services/ReceiptService.vb`| `IReceiptService` / `ReceiptService` | `GenerateReceiptAsync()`, `ReprintReceiptAsync()` | |
| `Services/SalesReturnService.vb` | `ISalesReturnService` / `SalesReturnService` | `ProcessReturnAsync()`, `ValidateReturnEligibility()` | |
| `Services/DailySummaryService.vb` | `IDailySummaryService` / `DailySummaryService` | `GetDailySummaryAsync()`, `GetPeriodSummaryAsync()` | |
