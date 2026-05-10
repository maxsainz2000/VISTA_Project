---
type: layer-manifest
module: MerchSys.POS
layer: Services
last-updated: 2026-05-10
---

# MerchSys.POS — Services

## Core Services
| File Path | Interface & Implementation | Key Methods | Dependencies (DI) |
|---|---|---|---|
| `Services/CartService.vb` | `ICartService` / `CartService` | `CreateCartAsync()`, `AddLineAsync()`, `UpdateLineQuantityAsync()`, `RemoveLineAsync()`, `ApplyLineDiscountAsync()`, `FinalizeAsync()`, `VoidTransactionAsync()`, `GetTransactionHistoryAsync()` | `POSDbContext`, `IReceiptService`, `IConfiguration` |
| `Services/PaymentService.vb`| `IPaymentService` / `PaymentService` | `ProcessPaymentAsync()` | |
| `Services/CreditService.vb` | `ICreditService` / `CreditService` | `CreateAccountAsync()`, `GetAccountAsync()`, `GetAllAccountsAsync()`, `SearchAccountsAsync()`, `CanExtendCreditAsync()`, `ChargeCreditAsync()`, `RecordPaymentAsync()`, `GetPaymentHistoryAsync()`, `GetTotalOutstandingAsync()`, `GetOverdueAccountsAsync()` | |
| `Services/ReceiptService.vb`| `IReceiptService` / `ReceiptService` | `GenerateReceiptAsync()`, `GetReceiptAsync()`, `GetReceiptByTransactionAsync()`, `PrintReceiptAsync()` | |
| `Services/SalesReturnService.vb` | `ISalesReturnService` / `SalesReturnService` | `ProcessReturnAsync()`, `GetReturnsForTransactionAsync()`, `GetReturnHistoryAsync()`, `GetTransactionIdsWithReturnsAsync()` | |
| `Services/DailySummaryService.vb` | `IDailySummaryService` / `DailySummaryService` | `GetDailySummaryAsync()`, `GetWeeklySummaryAsync()`, `GetMonthlySummaryAsync()` | |

