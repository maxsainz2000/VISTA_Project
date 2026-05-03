---
type: layer-manifest
module: MerchSys.POS
layer: UI
last-updated: 2026-05-03
---

# MerchSys.POS — User Interface

## ViewModels and Views
| ViewModel (`ViewModels/`) | View (`MerchSys.App/Views/POS/`) | Key Commands / Logic |
|---|---|---|
| `SalesCartViewModel.vb` | `SalesCartView.xaml` | `AddToCartCommand`, `RemoveItemCommand`, `CheckoutCommand` |
| `CreditManagementViewModel.vb`| `CreditManagementView.xaml` | `SearchCustomerCommand`, `RecordPaymentCommand` |
| `TransactionHistoryViewModel.vb`| `TransactionHistoryView.xaml`| `LoadTransactionsCommand`, `ProcessReturnCommand` |
| `DailySummaryViewModel.vb` | `DailySummaryView.xaml` | `LoadCommand` (Daily/Weekly/Monthly) |
