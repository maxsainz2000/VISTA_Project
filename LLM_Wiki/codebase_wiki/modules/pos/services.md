---
type: layer-manifest
module: MerchSys.POS
layer: Services
last-updated: 2026-05-03
---

# MerchSys.POS — Services

## Core Services
| File Path | Interface & Implementation | Key Methods |
|---|---|---|
| `Services/CartService.vb` | `ICartService` / `CartService` | `AddItem()`, `RemoveItem()`, `UpdateQuantity()`, `GetCart()`, `ClearCart()` |
| `Services/PaymentService.vb`| `IPaymentService` / `PaymentService` | `ProcessPaymentAsync()`, `ValidatePayment()` |
| `Services/CreditService.vb` | `ICreditService` / `CreditService` | `GetAccountAsync()`, `ProcessCreditSaleAsync()`, `RecordPaymentAsync()` |
| `Services/ReceiptService.vb`| `IReceiptService` / `ReceiptService` | `GenerateReceiptAsync()`, `ReprintReceiptAsync()` |
| `Services/SalesReturnService.vb` | `ISalesReturnService` / `SalesReturnService` | `ProcessReturnAsync()`, `ValidateReturnEligibility()` |
| `Services/DailySummaryService.vb` | `IDailySummaryService` / `DailySummaryService` | `GetDailySummaryAsync()`, `GetPeriodSummaryAsync()` |
