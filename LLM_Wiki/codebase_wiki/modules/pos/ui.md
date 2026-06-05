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
| `CreditManagementViewModel.vb`| `CreditManagementView.xaml` | `SearchCustomerCommand`, `RecordPaymentCommand`; concurrency conflict guarded via `ConcurrencyHelper.ExecuteWithConflictPromptAsync` on `Pos_CreditAccounts` RowVersion (UX-14). Implements `IFreshnessAware` and stamps `LastLoadedAt`. Mounts `FilterSummaryBar`, `FreshnessChip`, coordinated `SkeletonPanel` (Rows), and `BusyOverlay` (UX-31). |
| `TransactionHistoryViewModel.vb`| `TransactionHistoryView.xaml`| `LoadTransactionsCommand`, `ProcessReturnCommand`; `ProcessReturnAsync` guarded via `ConcurrencyHelper.ExecuteWithConflictPromptAsync` on `Inv_StockBatches` RowVersion (UX-14). Mounts `FilterSummaryBar`, `FreshnessChip`, coordinated `SkeletonPanel` (Rows), `BusyOverlay`, and informational `EmptyStatePanel` (UX-31 & UX-32). |
| `DailySummaryViewModel.vb` | `DailySummaryView.xaml` | `LoadCommand` (Daily/Weekly/Monthly). Implements `IFreshnessAware` and stamps `LastLoadedAt`. Mounts `FreshnessChip`, coordinated `SkeletonPanel` (Rows), and `BusyOverlay` (UX-31). Configured with logical TabIndex mapping, Alt mnemonics (`_Daily`, `_Weekly`, `_Monthly`, `_Refresh`), and Enter key handler in code-behind triggers reloading (UX-32). |
| `VatSettingsViewModel.vb` | `VatSettingsView.xaml` | `SaveCommand`, `ReloadCommand`; includes manager-only TIN validation logic; concurrency conflict guarded via `ConcurrencyHelper.ExecuteWithConflictPromptAsync` (UX-14). Configured with logical TabIndex mapping, Alt mnemonics (`_Reload`, `_Save`); Save button has `IsDefault="True"`, TIN text box handles Enter without conflict (UX-32). |
