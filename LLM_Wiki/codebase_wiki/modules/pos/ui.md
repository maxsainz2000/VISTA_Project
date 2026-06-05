---
type: layer-manifest
module: MerchSys.POS
layer: UI
last-updated: 2026-06-05
concurrency-guard: UX-14
---

# MerchSys.POS — User Interface

## ViewModels and Views
| ViewModel (`ViewModels/`) | View (`MerchSys.App/Views/POS/`) | Key Commands / Logic |
|---|---|---|
| `SalesCartViewModel.vb` | `SalesCartView.xaml` | `AddToCartCommand`, `RemoveItemCommand`, `CheckoutCommand`, `SearchAndAddProductCommand`, `HoldCartCommand`, `RecallCartCommand`; properties `HeldCartId` and `HasHeldCart` for held-cart swap; event `TransactionCompleted` triggered on checkout; concurrency conflict guarded via `ConcurrencyHelper.ExecuteWithConflictPromptAsync` (UX-14) |
| `CreditManagementViewModel.vb`| `CreditManagementView.xaml` | `SearchCustomerCommand`, `RecordPaymentCommand`; concurrency conflict guarded via `ConcurrencyHelper.ExecuteWithConflictPromptAsync` on `Pos_CreditAccounts` RowVersion (UX-14) |
| `TransactionHistoryViewModel.vb`| `TransactionHistoryView.xaml`| `LoadTransactionsCommand`, `ProcessReturnCommand`; `ProcessReturnAsync` guarded via `ConcurrencyHelper.ExecuteWithConflictPromptAsync` on `Inv_StockBatches` RowVersion (UX-14) |
| `DailySummaryViewModel.vb` | `DailySummaryView.xaml` | `LoadCommand` (Daily/Weekly/Monthly) |
| `VatSettingsViewModel.vb` | `VatSettingsView.xaml` | `SaveCommand`, `ReloadCommand`; includes manager-only TIN validation logic; concurrency conflict guarded via `ConcurrencyHelper.ExecuteWithConflictPromptAsync` (UX-14) |
