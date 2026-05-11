---
type: layer-manifest
module: MerchSys.POS
layer: Services
last-updated: 2026-05-11
---

# MerchSys.POS — Services

## Core Services
| File Path | Interface & Implementation | Key Methods | Dependencies (DI) |
|---|---|---|---|
| `Services/CartService.vb` | `ICartService` / `CartService` | `CreateCartAsync()`, `AddLineAsync()`, `UpdateLineQuantityAsync()`, `RemoveLineAsync()`, `ApplyLineDiscountAsync()`, `FinalizeAsync()`, `VoidTransactionAsync()`, `GetTransactionHistoryAsync()` | `POSDbContext`, `IReceiptService`, `IConfiguration` |
| `Services/PaymentService.vb`| `IPaymentService` / `PaymentService` | `ProcessPaymentAsync()` | |
| `Services/CreditService.vb` | `ICreditService` / `CreditService` | `CreateAccountAsync()`, `GetAccountAsync()`, `GetAllAccountsAsync()`, `SearchAccountsAsync()`, `CanExtendCreditAsync()`, `ChargeCreditAsync()`, `RecordPaymentAsync()`, `GetPaymentHistoryAsync()`, `GetTotalOutstandingAsync()`, `GetOverdueAccountsAsync()` | |
| `Services/ReceiptService.vb`| `IReceiptService` / `ReceiptService` | `GenerateReceiptAsync()`, `GetReceiptAsync()`, `GetReceiptByTransactionAsync()`, `PrintReceiptAsync()` | `POSDbContext`, `IConfiguration`, `IReceiptIntegrityService` (POS-15) |
| `Services/SalesReturnService.vb` | `ISalesReturnService` / `SalesReturnService` | `ProcessReturnAsync()`, `GetReturnsForTransactionAsync()`, `GetReturnHistoryAsync()`, `GetTransactionIdsWithReturnsAsync()` | |
| `Services/DailySummaryService.vb` | `IDailySummaryService` / `DailySummaryService` | `GetDailySummaryAsync()`, `GetWeeklySummaryAsync()`, `GetMonthlySummaryAsync()` | |
| `src/MerchSys.POS/Services/IReceiptIntegrityService.vb`<br>`src/MerchSys.POS/Services/ReceiptIntegrityService.vb` | `IReceiptIntegrityService`<br>`ReceiptIntegrityService` | BIR-compliant hash chain generation, receipt validation, and gap-free sequence management. | `POSDbContext`, `IConfiguration`, `IMediator` |
| `src/MerchSys.POS/Services/IVatCalculator.vb`<br>`src/MerchSys.POS/Services/VatCalculator.vb` | `IVatCalculator`<br>`VatCalculator` | Three-bucket VAT decomposition (Vatable, Exempt, Zero-Rated) for BIR compliance. | |
| `src/MerchSys.POS/Services/VatConfigurationLoader.vb` | `VatConfigurationLoader` | Singleton cache for VAT settings with double-checked lazy loading and invalidation support. | `IServiceScopeFactory` |
| `src/MerchSys.POS/Services/VatAwareReceiptService.vb` | `VatAwareReceiptService` | Decorator for `ReceiptService` that calculates and persists VAT breakdown for transactions. | `ReceiptService`, `IVatCalculator`, `VatConfigurationLoader` |
 
+## Debug & Utilities
+| File Path | Class | Description |
+|---|---|---|
+| `src/MerchSys.POS/Debug/ReceiptSequenceHarnessReport.vb` | `ReceiptSequenceHarnessReport` | Debug-only runner for `Pos_SequenceConcurrencyHarness`; validates concurrency-safe receipt numbering against a scratch SQLite DB. |
+
