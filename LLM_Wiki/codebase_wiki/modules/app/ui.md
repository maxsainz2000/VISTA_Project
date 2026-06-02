---
type: layer-manifest
module: MerchSys.App
layer: UI
last-updated: 2026-06-02
---

# MerchSys.App — UI (Views)

This page details the WPF View implementations (XAML and code-behind) in the **MerchSys.App** module.

## Shell & Navigation

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Application.xaml`<br>`src/MerchSys.App/Application.xaml.vb` | `Application` | Main Application entry point. Handles Generic Host initialization, DI container building, manual schema bootstrapping via `MariaDbSchemaInitializer`, and startup theme loading (UX-01). | (none) |
| `src/MerchSys.App/MainWindow.xaml`<br>`src/MerchSys.App/MainWindow.xaml.vb` | `MainWindow` | Main application shell restructured to support the Master-Detail Activity Rail navigation. | `MainWindowViewModel` (Constructor Injection) |
| `src/MerchSys.App/ViewModels/MainWindowViewModel.vb` | `MainWindowViewModel` | MVVM hub for the shell. Manages `ActiveModule`, `ActiveModuleName`, role-aware `AppModule` collections, and navigation commands. | `IServiceProvider` (Constructor Injection) |
| `src/MerchSys.App/Models/NavigationItem.vb` | `NavigationItem`, `NavigationGroup` | POCO models representing navigation nodes and their parent groups. | (none) |
| `src/MerchSys.App/Models/AppModule.vb` | `AppModule` (Enum), `RailItem` | Represents master monolith modules and maps them to vertical rail icons, tooltips, and active states. | (none) |
| `src/MerchSys.App/Views/Shell/ActivityRail.xaml`<br>`src/MerchSys.App/Views/Shell/ActivityRail.xaml.vb` | `ActivityRail` | Far-left 60px vertical menu rendering the module selection icons (PUR, INV, POS, ACC, DEV). | `ActivityRailViewModel` (Constructor Injection) |
| `src/MerchSys.App/ViewModels/Shell/ActivityRailViewModel.vb` | `ActivityRailViewModel` | MVVM controller for the vertical navigation rail. Sourced with AppModule items and binds navigation swaps to `MainWindowViewModel`. | (none) |
| `src/MerchSys.App/Views/Shell/ModuleDetailPanel.xaml`<br>`src/MerchSys.App/Views/Shell/ModuleDetailPanel.xaml.vb` | `ModuleDetailPanel` | Adjacent 220px detail panel that switches sub-views dynamically based on the selected master module module. Anchors the connection health badge and logout action. | `MainWindowViewModel` (DataContext) |
| `src/MerchSys.App/Views/Shell/Modules/PurchasingPanel.xaml`<br>`.xaml.vb` | `PurchasingPanel` | UserControl holding sub-views inside the Purchasing module detail panel. | (Inherited) |
| `src/MerchSys.App/Views/Shell/Modules/InventoryPanel.xaml`<br>`.xaml.vb` | `InventoryPanel` | UserControl holding sub-views inside the Inventory module detail panel. | (Inherited) |
| `src/MerchSys.App/Views/Shell/Modules/PosPanel.xaml`<br>`.xaml.vb` | `PosPanel` | UserControl holding sub-views inside the POS module detail panel. | (Inherited) |
| `src/MerchSys.App/Views/Shell/Modules/AccountingPanel.xaml`<br>`.xaml.vb` | `AccountingPanel` | UserControl holding sub-views inside the Accounting module detail panel. | (Inherited) |
| `src/MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml`<br>`.xaml.vb` | `DeveloperToolsPanel` | UserControl holding sub-views inside the Developer Tools panel. | (Inherited) |
| `src/MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml`<br>`src/MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml.vb` | `ConnectionStatusIndicator` | Pill-shaped badge showing connection state (Green Online, Orange Reconnecting, Red Offline). | `ConnectionStatusViewModel` (Constructor Injection) |
| `src/MerchSys.App/ViewModels/Shell/ConnectionStatusViewModel.vb` | `ConnectionStatusViewModel` | VM for connection status indicator. | `IConnectionHealthMonitor` |
| `src/MerchSys.App/Behaviors/DisableOnOfflineBehavior.vb` | `DisableOnOfflineBehavior` | Attached behavior rendering `IsDisabledWhenOffline` DependencyProperty on buttons. | (none) |
| `src/MerchSys.App/Views/LoginView.xaml`<br>`src/MerchSys.App/Views/LoginView.xaml.vb` | `LoginView` | Standalone login window with authentication and first-login password change. | `LoginViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/OwnerDashboardView.xaml`<br>`src/MerchSys.App/Views/OwnerDashboardView.xaml.vb` | `OwnerDashboardView` | Owner Dashboard with KPI cards and plain-language interpretations. | `OwnerDashboardViewModel` (Constructor Injection) |
| `src/MerchSys.App/ViewModels/LoginViewModel.vb` | `LoginViewModel` | Manages login flow, error display, and password change logic. | `IAuthenticationService`, `LoginSessionService` (Constructor Injection) |
| `src/MerchSys.App/ViewModels/OwnerDashboardViewModel.vb` | `OwnerDashboardViewModel` | MVVM hub for the Owner dashboard. Sourced from all four modules to aggregate business metrics and display plain-language interpretations. | `ISessionService`, `IStockDashboardService`, `ILowStockAlertService`, `IExpiryTrackingService`, `IPurchaseOrderService`, `IVendorService`, `IAccountsPayableService`, `IDailySummaryService`, `IFinancialOverviewService`, `IIncomeStatementService` (Constructor Injection) |
| `src/MerchSys.App/Views/SessionTimeoutWarningView.xaml`<br>`src/MerchSys.App/Views/SessionTimeoutWarningView.xaml.vb` | `SessionTimeoutWarningView` | Modal countdown dialog for session inactivity. Prompts user to extend session or sign out; automatically logs out on timeout (OWASP DA2). | `SessionTimeoutWarningViewModel` (Transient) |
| `src/MerchSys.App/ViewModels/SessionTimeoutWarningViewModel.vb` | `SessionTimeoutWarningViewModel` | Countdown ViewModel. Formats remaining idle seconds as "M:SS" and exposes commands to stay signed in or sign out. | (none) |



## Accounting Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml`<br>`src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb` | `FinancialOverviewView` | Primary Accounting dashboard. Features KPI cards (including VAT Payable), a 6-month trend chart, Top Products list, and an alerts panel. | `FinancialOverviewViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml`<br>`src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml.vb` | `IncomeStatementView` | Income Statement (P&L) View. Features period switching (Monthly/Quarterly/Annual), a "What This Means" panel, and a detailed P&L layout with a per-product margins tab. | `IncomeStatementViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml`<br>`src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml.vb` | `SalesSummaryView` | Sales Summary View. Displays daily/weekly/monthly sales breakdowns by payment method with KPI summary cards and "What This Means" interpretation. | `SalesSummaryViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/VatReturnView.xaml`<br>`src/MerchSys.App/Views/Accounting/VatReturnView.xaml.vb` | `VatReturnView` | Manager-only BIR VAT reporting interface. Supports generation, locking, and export of Forms 2550M/Q and 2551Q. | `VatReturnViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/VatReliefReportView.xaml`<br>`src/MerchSys.App/Views/Accounting/VatReliefReportView.xaml.vb` | `VatReliefReportView` | Read-only compliance view displaying the monthly VAT Relief Report with side-by-side sales/purchases summaries, large color-coded Net VAT banner, "What This Means" strip, and trailing 12-month trend grid. | `VatReliefReportViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/TamperAuditReportView.xaml`<br>`src/MerchSys.App/Views/Accounting/TamperAuditReportView.xaml.vb` | `TamperAuditReportView` | Read-only compliance view allowing Manager and Owner roles to review filterable receipt tamper incidents. | `TamperAuditReportViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Accounting/Components/VatPayableTile.xaml`<br>`src/MerchSys.App/Views/Accounting/Components/VatPayableTile.xaml.vb` | `VatPayableTile` | Standalone KPI tile for VAT Payable / Percentage Tax. Inherits DataContext. | (Inherited from `FinancialOverviewView`) |

## Inventory Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml`<br>`src/MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml.vb` | `ExpiryMonitorView` | Expiry monitor dashboard for tracking soon-to-expire products. | `ExpiryMonitorViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Inventory/ProductManagementView.xaml`<br>`src/MerchSys.App/Views/Inventory/ProductManagementView.xaml.vb` | `ProductManagementView` | Product catalog management interface. | `ProductManagementViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Inventory/ProductPriceHistoryView.xaml`<br>`src/MerchSys.App/Views/Inventory/ProductPriceHistoryView.xaml.vb` | `ProductPriceHistoryView` | Read-only popup window showcasing a premium DataGrid ledger with color-coded price changes. | `ProductPriceHistoryViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Inventory/ShrinkageView.xaml`<br>`src/MerchSys.App/Views/Inventory/ShrinkageView.xaml.vb` | `ShrinkageView` | Shrinkage reporting and analysis interface. | `ShrinkageViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Inventory/StockDashboardView.xaml`<br>`src/MerchSys.App/Views/Inventory/StockDashboardView.xaml.vb` | `StockDashboardView` | Primary inventory tracking dashboard. Displays Retail Price, Avg Cost, and FIFO Cost side-by-side (INV-15). | `StockDashboardViewModel` (Constructor Injection) |

## POS Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/POS/CreditManagementView.xaml`<br>`src/MerchSys.App/Views/POS/CreditManagementView.xaml.vb` | `CreditManagementView` | Customer credit and balance management. | `CreditManagementViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/POS/DailySummaryView.xaml`<br>`src/MerchSys.App/Views/POS/DailySummaryView.xaml.vb` | `DailySummaryView` | Daily cash drawer and sales summary tracking. | `DailySummaryViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/POS/SalesCartView.xaml`<br>`src/MerchSys.App/Views/POS/SalesCartView.xaml.vb` | `SalesCartView` | Primary point-of-sale checkout interface. | `SalesCartViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/POS/TransactionHistoryView.xaml`<br>`src/MerchSys.App/Views/POS/TransactionHistoryView.xaml.vb` | `TransactionHistoryView` | Past transactions viewer with refund processing. | `TransactionHistoryViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/POS/VatSettingsView.xaml`<br>`src/MerchSys.App/Views/POS/VatSettingsView.xaml.vb` | `VatSettingsView` | Two-column BIR registration form for managing VAT rates, TIN, and business info. Manager-only access. | `VatSettingsViewModel` (Constructor Injection) |

## Purchasing Views

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/Purchasing/APLedgerView.xaml`<br>`src/MerchSys.App/Views/Purchasing/APLedgerView.xaml.vb` | `APLedgerView` | Accounts payable ledger for vendor invoices. | `APLedgerViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/GoodsReceivingView.xaml`<br>`src/MerchSys.App/Views/Purchasing/GoodsReceivingView.xaml.vb` | `GoodsReceivingView` | Goods receiving interface for inbound shipments. | `GoodsReceivingViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml`<br>`src/MerchSys.App/Views/Purchasing/PurchaseOrderListView.xaml.vb` | `PurchaseOrderListView` | Active and historical purchase orders tracking. | `PurchaseOrderListViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/ReorderSuggestionsView.xaml`<br>`src/MerchSys.App/Views/Purchasing/ReorderSuggestionsView.xaml.vb` | `ReorderSuggestionsView` | Automated reorder suggestions based on velocity. | `ReorderSuggestionsViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/VendorCatalogView.xaml`<br>`src/MerchSys.App/Views/Purchasing/VendorCatalogView.xaml.vb` | `VendorCatalogView` | Master-detail catalog editor interface allowing Managers to manage vendor products. | `VendorCatalogViewModel` (Constructor Injection) |
| `src/MerchSys.App/Views/Purchasing/VendorDirectoryView.xaml`<br>`src/MerchSys.App/Views/Purchasing/VendorDirectoryView.xaml.vb` | `VendorDirectoryView` | Vendor catalog and contact management. | `VendorListViewModel` (Constructor Injection) |
## SQL Resources

| File Path | Description |
|---|---|
| `src/MerchSys.App/Resources/Sql/ReceiptIntegrityTriggerVerification.sql` | Diagnostic bundle for MariaDB triggers. Includes trigger inventory and five negative-path probes (BIR compliance). |

## Debug & Developer Tools

| File Path | Class | Description | DataContext / Injection |
|---|---|---|---|
| `src/MerchSys.App/Views/Debug/DebugMenuExtensions.vb` | `DebugMenuView` | Code-only `#If DEBUG`-gated UserControl that renders the developer debug panel. | (none) |

## Themes & Styles

| File Path | Description |
|---|---|
| `src/MerchSys.App/Themes/Tokens.xaml` | Theme-agnostic structure tokens including Inter font family configurations, corner radii, margins, borders, type scales, and drop shadow effects. |
| `src/MerchSys.App/Themes/Light.xaml` | Light color palette keys and values, defining color-parity brushes like `WindowBackgroundBrush`, `SurfaceBrush`, etc. |
| `src/MerchSys.App/Themes/Dark.xaml` | Dark color palette keys and values, defining color-parity brushes (e.g. `SelectionBackgroundBrush` with 28% alpha opacity). |
