---
type: layer-manifest
module: MerchSys.POS
layer: Services
last-updated: 2026-05-29
---

# MerchSys.POS — Services

## Core Services
| File Path | Interface & Implementation | Key Methods | Dependencies (DI) |
|---|---|---|---|
| `src/MerchSys.POS/Services/ICartService.vb`<br>`src/MerchSys.POS/Services/CartService.vb` | `ICartService`<br>`CartService` | `CreateCartAsync()`, `AddLineAsync()`, `UpdateLineQuantityAsync()`, `RemoveLineAsync()`, `ApplyLineDiscountAsync()`, `FinalizeAsync()`, `VoidTransactionAsync()`, `GetTransactionHistoryAsync()` | `POSDbContext`, `IReceiptService`, `IConfiguration` |
| `src/MerchSys.POS/Services/IPaymentService.vb`<br>`src/MerchSys.POS/Services/PaymentService.vb`| `IPaymentService`<br>`PaymentService` | `ProcessPaymentAsync()` | |
| `src/MerchSys.POS/Services/ICreditService.vb`<br>`src/MerchSys.POS/Services/CreditService.vb` | `ICreditService`<br>`CreditService` | `CreateAccountAsync()`, `GetAccountAsync()`, `GetAllAccountsAsync()`, `SearchAccountsAsync()`, `CanExtendCreditAsync()`, `ChargeCreditAsync()`, `RecordPaymentAsync()`, `GetPaymentHistoryAsync()`, `GetTotalOutstandingAsync()`, `GetOverdueAccountsAsync()` | |
| `src/MerchSys.POS/Services/IReceiptService.vb`<br>`src/MerchSys.POS/Services/ReceiptService.vb`| `IReceiptService`<br>`ReceiptService` | `GenerateReceiptAsync()`, `GetReceiptAsync()`, `GetReceiptByTransactionAsync()`, `PrintReceiptAsync()` | `POSDbContext`, `IConfiguration`, `IReceiptIntegrityService` (POS-15), `IReceiptBodyComposer` (POS-18), `IReceiptRenderer` (POS-19) |
| `src/MerchSys.POS/Services/ISalesReturnService.vb`<br>`src/MerchSys.POS/Services/SalesReturnService.vb` | `ISalesReturnService`<br>`SalesReturnService` | `ProcessReturnAsync()`, `GetReturnsForTransactionAsync()`, `GetReturnHistoryAsync()`, `GetTransactionIdsWithReturnsAsync()` | |
| `src/MerchSys.POS/Services/IDailySummaryService.vb`<br>`src/MerchSys.POS/Services/DailySummaryService.vb` | `IDailySummaryService`<br>`DailySummaryService` | `GetDailySummaryAsync()`, `GetWeeklySummaryAsync()`, `GetMonthlySummaryAsync()` | |
| `src/MerchSys.POS/Services/IReceiptIntegrityService.vb`<br>`src/MerchSys.POS/Services/ReceiptIntegrityService.vb` | `IReceiptIntegrityService`<br>`ReceiptIntegrityService` | BIR-compliant hash chain generation, receipt validation, and gap-free sequence management. | `POSDbContext`, `IConfiguration`, `IMediator` |
| `src/MerchSys.POS/Services/IVatCalculator.vb`<br>`src/MerchSys.POS/Services/VatCalculator.vb` | `IVatCalculator`<br>`VatCalculator` | Three-bucket VAT decomposition (Vatable, Exempt, Zero-Rated) for BIR compliance. | |
| `src/MerchSys.POS/Services/VatConfigurationLoader.vb` | `VatConfigurationLoader` | Singleton cache for VAT settings with double-checked lazy loading and invalidation support. | `IServiceScopeFactory` |
| `src/MerchSys.POS/Services/IVatConfigurationWriter.vb` | `IVatConfigurationWriter`<br>`VatConfigurationWriter` | Manager-only service for updating VAT registration settings, TIN, and rates; triggers cache invalidation. | `POSDbContext`, `VatConfigurationLoader`, `IMediator` |
| `src/MerchSys.POS/Services/VatAwareReceiptService.vb` | `VatAwareReceiptService` | Decorator for `ReceiptService` that calculates and persists VAT breakdown for transactions. | `ReceiptService`, `IVatCalculator`, `VatConfigurationLoader` |
| `src/MerchSys.POS/Services/ReceiptFormatting/IReceiptBodyComposer.vb`<br>`src/MerchSys.POS/Services/ReceiptFormatting/BirCompliantReceiptBodyComposer.vb` | `IReceiptBodyComposer`<br>`BirCompliantReceiptBodyComposer` | Composes BIR-compliant receipt body text with three-bucket VAT disclosure (VATable, Exempt, Zero-Rated) and Output VAT. | `POSDbContext`, `VatConfigurationLoader` |
| `src/MerchSys.POS/Services/ReceiptRendering/IReceiptRenderer.vb`<br>`src/MerchSys.POS/Services/ReceiptRendering/ReceiptRenderTarget.vb`<br>`src/MerchSys.POS/Services/ReceiptRendering/ConsoleReceiptRenderer.vb`<br>`src/MerchSys.POS/Services/ReceiptRendering/PdfReceiptRenderer.vb`<br>`src/MerchSys.POS/Services/ReceiptRendering/ReceiptPdfOptions.vb` | `IReceiptRenderer`<br>`ReceiptRenderTarget`<br>`ConsoleReceiptRenderer`<br>`PdfReceiptRenderer`<br>`ReceiptPdfOptions` | Pluggable rendering layer for BIR-compliant Official Receipts. Outputs to Console (default) or monospace A5 portrait PDF written to disk. Implements defensive overwrite guards to prevent modifying generated receipts. | `IOptions(Of ReceiptPdfOptions)`, `ILogger(Of PdfReceiptRenderer)` |
| `src/MerchSys.POS/Services/Archival/IReceiptArchivalService.vb`<br>`src/MerchSys.POS/Services/Archival/ReceiptArchivalOptions.vb`<br>`src/MerchSys.POS/Services/Archival/ReceiptArchivalService.vb` | `IReceiptArchivalService`<br>`ReceiptArchivalOptions`<br>`ReceiptArchivalService` | Background service for transactional archival of expired receipts and integrity logs to cold storage. Uses `Pos_ArchivalSession` table for session-variable-aware database triggers (INT-12). | `IServiceScopeFactory`, `POSDbContext`, `IOptions(Of ReceiptArchivalOptions)` |

