---
type: layer-manifest
module: MerchSys.App
layer: UI
last-updated: 2026-05-11
---

# MerchSys.App — UI (Views)

This page details the WPF View implementations (XAML and code-behind) in the **MerchSys.App** module.

## Shell & Navigation

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Application.xaml`<br>`src/MerchSys.App/Application.xaml.vb` | `Application` | Main Application entry point. Handles Generic Host initialization, DI container building (including `ISessionService`), and manual database migration via `DatabaseInitializer`. | (none) |
| `src/MerchSys.App/MainWindow.xaml`<br>`src/MerchSys.App/MainWindow.xaml.vb` | `MainWindow` | Main application shell with grouped sidebar navigation and a dynamic content area. | `MainWindowViewModel` (Constructor Injection) |
| `src/MerchSys.App/ViewModels/MainWindowViewModel.vb` | `MainWindowViewModel` | MVVM hub for the shell. Manages `NavigationGroups`, `CurrentView` state, and navigation commands. | `IServiceProvider` (Constructor Injection) |
| `src/MerchSys.App/Models/NavigationItem.vb` | `NavigationItem`, `NavigationGroup` | POCO models representing navigation nodes and their parent groups. `NavigationItem` is observable for `IsActive` state. | (none) |

## Accounting Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml`<br>`src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb` | `FinancialOverviewView` | Primary Accounting dashboard. Features KPI cards (including VAT Payable), a 6-month trend chart, Top Products list, and an alerts panel. | `FinancialOverviewViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml`<br>`src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml.vb` | `IncomeStatementView` | Income Statement (P&L) View. Features period switching (Monthly/Quarterly/Annual), a "What This Means" panel, and a detailed P&L layout with a per-product margins tab. | `IncomeStatementViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml`<br>`src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml.vb` | `SalesSummaryView` | Sales Summary View. Displays daily/weekly/monthly sales breakdowns by payment method with KPI summary cards and "What This Means" interpretation. | `SalesSummaryViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/VatReturnView.xaml`<br>`src/MerchSys.App/Views/Accounting/VatReturnView.xaml.vb` | `VatReturnView` | Manager-only BIR VAT reporting interface. Supports generation, locking, and export of Forms 2550M/Q and 2551Q. | `VatReturnViewModel` (Constructor Injection) |

## Inventory Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml`<br>`src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml.vb` | `ExpiryMonitorView` | Expiry monitor dashboard for tracking soon-to-expire products. | `ExpiryMonitorViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Inventory/ProductManagementView.xaml`<br>`src/MerchSys.App/Views/Inventory/ProductManagementView.xaml.vb` | `ProductManagementView` | Product catalog management interface. | `ProductManagementViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Inventory/ShrinkageView.xaml`<br>`src/MerchSys.App/Views/Inventory/ShrinkageView.xaml.vb` | `ShrinkageView` | Shrinkage reporting and analysis interface. | `ShrinkageViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Inventory/StockDashboardView.xaml`<br>`src/MerchSys.App/Views/Inventory/StockDashboardView.xaml.vb` | `StockDashboardView` | Primary inventory tracking dashboard. | `StockDashboardViewModel` (Constructor Injection) |

## POS Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/POS/CreditManagementView.xaml`<br>`src/MerchSys.App/Views/POS/CreditManagementView.xaml.vb` | `CreditManagementView` | Customer credit and balance management. | `CreditManagementViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/POS/DailySummaryView.xaml`<br>`src/MerchSys.App/Views/POS/DailySummaryView.xaml.vb` | `DailySummaryView` | Daily cash drawer and sales summary tracking. | `DailySummaryViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/POS/SalesCartView.xaml`<br>`src/MerchSys.App/Views/POS/SalesCartView.xaml.vb` | `SalesCartView` | Primary point-of-sale checkout interface. | `SalesCartViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/POS/TransactionHistoryView.xaml`<br>`src/MerchSys.App/Views/POS/TransactionHistoryView.xaml.vb` | `TransactionHistoryView` | Past transactions viewer with refund processing. | `TransactionHistoryViewModel` (Constructor Injection) |

## Purchasing Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/Purchasing/APLedgerView.xaml`<br>`src/MerchSys.App/Views/Purchasing/APLedgerView.xaml.vb` | `APLedgerView` | Accounts payable ledger for vendor invoices. | `APLedgerViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/GoodsReceivingView.xaml`<br>`src/MerchSys.App/Views/Purchasing/GoodsReceivingView.xaml.vb` | `GoodsReceivingView` | Goods receiving interface for inbound shipments. | `GoodsReceivingViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml`<br>`src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml.vb` | `PurchaseOrderListView` | Active and historical purchase orders tracking. | `PurchaseOrderListViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/ReorderSuggestionsView.xaml`<br>`src/MerchSys.App/Views/Purchasing/ReorderSuggestionsView.xaml.vb` | `ReorderSuggestionsView` | Automated reorder suggestions based on velocity. | `ReorderSuggestionsViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/VendorDirectoryView.xaml`<br>`src/MerchSys.App/Views/Purchasing/VendorDirectoryView.xaml.vb` | `VendorDirectoryView` | Vendor catalog and contact management. | `VendorListViewModel` (Constructor Injection) |
## SQL Resources

| File Path | Description |
|---|---|
| `src/MerchSys.App/Resources/Sql/ReceiptIntegrityTriggerVerification.sql` | Diagnostic bundle for MariaDB triggers. Includes trigger inventory and five negative-path probes (BIR compliance). |
