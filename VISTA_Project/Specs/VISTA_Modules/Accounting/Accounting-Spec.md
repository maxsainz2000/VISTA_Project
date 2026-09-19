# Accounting Specification

## Feature: ACC-01

### Overview
Implemented the four core domain entities for the Accounting module as specified in ACC-01.
These entities provide the data foundation for financial reporting, KPI snapshots, and FIFO-based margin tracking.

### Requirements
- Deleted `Class1.vb` — scaffold placeholder removed
- **Entities/FinancialPeriod.vb**: summarised P&L per period type (Daily/Weekly/Monthly/Quarterly/Annual); inherits `AuditableEntity`
- **Entities/RevenueRecord.vb**: per-product revenue line created from `SaleCompletedEvent`; captures FIFO COGS and gross profit; uses `PaymentMethod` enum from SharedKernel; inherits `AuditableEntity`
- **Entities/ExpenseRecord.vb**: single expense posting from any source module (Purchasing/Inventory/POS); nullable `SourceReferenceId`; inherits `AuditableEntity`
- **Entities/FinancialSnapshot.vb**: point-in-time KPI cache (AR, AP, inventory value, today/MTD/YTD revenue); inherits `AuditableEntity`

## Feature: ACC-02

### Overview
Implemented the Accounting data access layer: updated `AccountingDbContext` with four DbSets, created four EF Core entity configurations, and created four MediatR event handlers for revenue and expense recording.

### Requirements
- **Data/AccountingDbContext.vb**: added DbSets for `FinancialPeriods`, `RevenueRecords`, `ExpenseRecords`, `FinancialSnapshots`
- **Data/Configurations/FinancialPeriodConfiguration.vb**: table `Acc_FinancialPeriods`, precision(18,2) on all monetary fields, precision(10,4) on GrossMarginPercent
- **Data/Configurations/RevenueRecordConfiguration.vb**: table `Acc_RevenueRecords`, precision(18,2) on all monetary fields, indexes on `RecordDate` and `ProductId`
- **Data/Configurations/ExpenseRecordConfiguration.vb**: table `Acc_ExpenseRecords`, precision(18,2) on Amount, Category max 50 required
- **Data/Configurations/FinancialSnapshotConfiguration.vb**: table `Acc_FinancialSnapshots`, precision(18,2) on all monetary fields, unique index on `SnapshotDate`
- **Handlers/SaleCompletedAccountingHandler.vb**: creates `RevenueRecord` + COGS `ExpenseRecord` per sale item
- **Handlers/GoodsReceivedAccountingHandler.vb**: creates "Purchase" `ExpenseRecord` per received line item
- **Handlers/CreditPaymentAccountingHandler.vb**: creates "AR Reduction" `ExpenseRecord` on credit repayment
- **Handlers/ShrinkageAccountingHandler.vb**: creates "Shrinkage" `ExpenseRecord` for inventory write-offs

## Feature: ACC-03

### Overview
Implemented the Financial Overview Service for the Accounting module — providing KPI aggregation, 6-month trend data, top-selling products, and AR/AP/Inventory balances for the Owner dashboard. Based on plan ACC-03.

### Requirements
- **Services/IFinancialOverviewService.vb**: interface defining `GetOverviewAsync()` and `RefreshSnapshotAsync()`, plus inline DTO classes: `FinancialOverviewDto`, `MonthlyTrendDto`, and `TopProductDto`
- **Services/FinancialOverviewService.vb**: concrete implementation injecting `AccountingDbContext`, `IMediator`, and `ILogger`

## Feature: ACC-04

### Overview
Implemented the merchandising-format Income Statement (P&L) service for the Accounting module. Provides monthly, quarterly, annual, and arbitrary date-range views of Net Sales → COGS → Gross Profit → Operating Expenses → Net Income, with FIFO-based COGS and per-product margin analysis.

### Requirements
- **Services/IIncomeStatementService.vb**: interface with `GenerateAsync`, `GenerateMonthlyAsync`, `GenerateQuarterlyAsync`, `GenerateAnnualAsync`, `GetPerProductMarginsAsync`; also defines `IncomeStatementDto` (merchandising P&L format) and `ProductMarginDto`
- **Services/IncomeStatementService.vb**: full implementation backed by `AccountingDbContext`; private `BuildStatementAsync` helper handles all aggregation; delegates period-label formatting to each public method

## Feature: ACC-05

### Overview
Implemented the Sales Summary Service — the formal accounting view of daily/weekly/monthly sales broken down by payment method. Distinct from the POS `DailySummaryService`; this service operates on `RevenueRecord` and `ExpenseRecord` data already captured by the Accounting handlers.

### Requirements
- **Services/ISalesSummaryService.vb**: Interface plus all DTOs: `AccountingSalesSummaryDto`, `PaymentBreakdownDto`, `DailySalesDto`, `PaymentTrendDto`
- **Services/SalesSummaryService.vb**: Full implementation

## Feature: ACC-06

### Overview
Implemented the "What This Means" plain-language interpretation engine for the Accounting module. This service translates raw financial figures from the three report DTOs into actionable, non-technical sentences for the store manager.

### Requirements
- **Services/IWhatThisMeansService.vb**: interface with 4 methods: `GenerateOverviewInterpretation`, `GenerateIncomeStatementInterpretation`, `GenerateSalesSummaryInterpretation`, `GenerateMarginAlert`
- **Services/WhatThisMeansService.vb**: template-based implementation; uses `StringBuilder` to compose multi-sentence paragraphs, formats all amounts as ₱N,NNN.NN, applies threshold-based conditional sentences (credit % warning at ≥30%, margin drop severity at ≥3%), derives previous-period comparison from `MonthlyTrend` list for Overview and from the optional `previousMargin` parameter for Income Statement

## Feature: ACC-07

### Overview
Implemented the Financial Overview Dashboard — the primary Accounting screen. Created the Presenter, Designer code view, and code-behind per plan ACC-07.

### Requirements
- **Presenters/FinancialOverviewPresenter.vb**: Presenter with 7 KPI properties, WhatThisMeansText, alert counts (OverdueARCount, OverdueAPCount, LowStockAlertCount), ObservableCollection of `TopProductDto` and `TrendBarItem`, 5-minute auto-refresh timer via `System.Timers.Timer` + SynchronizationContext, and `RefreshCommand` (AsyncRelayCommand).
- Created `TrendBarItem` class (within the Presenter file) — flat DTO for the bar chart with pre-computed `RevenueBarHeight`, `COGSBarHeight`, `GrossProfitBarHeight` (normalized to max revenue = 120px).
- **Views/Accounting/FinancialOverviewView.Designer code**: KPI card row (7 cards), "What This Means" box (light blue panel, 💡 icon, always visible, no collapse option), 6-month trend bar chart (WinForms primitives: ItemsControl + UniformGrid + Rectangles with VerticalAlignment=Bottom, tooltips showing exact values), Top Products DataGrid (10 rows, columns: Product / Units Sold / Revenue / COGS / Margin %), Alerts panel (3 cards with DataTrigger color-coding: green=OK, red=overdue AR, orange=overdue AP, yellow=low stock).
- **Views/Accounting/FinancialOverviewView.Designer code.vb**: code-behind with constructor injection of `FinancialOverviewPresenter`.

## Feature: ACC-08

### Overview
Implemented the Income Statement (P&L) View for the Accounting module. Creates a WinForms UserControl that displays the merchandising-format income statement with monthly/quarterly/annual period switching, a mandatory "What This Means" box, and a per-product margins DataGrid tab.

### Requirements
- **Presenters/IncomeStatementPresenter.vb**: Presenter with:
- `IncomeStatementPeriodType` enum (Monthly, Quarterly, Annual)
- Period toggle via `IsMonthly`, `IsQuarterly`, `IsAnnual` bool properties backed by a private `PeriodType` property; setting any to `True` triggers a reload
- Period selectors: `SelectedYear` (int), `SelectedMonth` (int 1–12), `SelectedQuarterLabel` (string "Q1"–"Q4" mapping to private `_selectedQuarter` int)
- `AvailableYears` (current year –4 to current, descending), `AvailableMonths` (1–12), `AvailableQuarterLabels` ("Q1"–"Q4")
- P&L display string properties (`NetSalesDisplay`, `COGSDisplay`, `GrossProfitDisplay`, `OperatingExpensesDisplay`, `ShrinkageLossDisplay`, `NetIncomeDisplay`) pre-formatted as `₱X,XXX.XX` for positive amounts and `(₱X,XXX.XX)` for deductions/negatives via `FormatAmount` / `FormatDeduction` private helpers
- `GrossMarginPercent` and `NetMarginPercent` as Decimal (Designer code uses StringFormat for percent display)
- `WhatThisMeansText` (String) bound to `IWhatThisMeansService.GenerateIncomeStatementInterpretation` with previous-period margin comparison
- `ProductMargins As ObservableCollection(Of ProductMarginDto)` loaded and sorted by `GrossMarginPercent` ascending (lowest margin first)
- `LoadCommand As AsyncRelayCommand` and `IsBusy As Boolean` for loading state
- Previous-period fetch: monthly → previous calendar month, quarterly → previous quarter (wraps year), annual → previous year
- **Views/Accounting/IncomeStatementView.Designer code**: UserControl with:
- Toolbar: title + period toggle RadioButtons (Monthly / Quarterly / Annual) + conditional Month ComboBox (visible when Monthly) + Quarter ComboBox "Q1"–"Q4" (visible when Quarterly) + Year ComboBox (always visible) + Loading indicator + Refresh button
- "What This Means" box: light-blue panel with 💡 icon, `WhatThisMeansText` binding, always visible (above the TabControl so it persists across tab switches)
- **Income Statement tab**: 13-row Grid layout — Net Sales, Less: COGS, separator, Gross Profit (with %), Less: Operating Expenses, Shrinkage Loss (indented), separator, Net Income (with %). All amounts bind to pre-formatted string properties. `Courier New` font on amount column for monospaced alignment.
- **Per-Product Margins tab**: DataGrid with columns ProductName, Revenue, COGS, Gross Profit, Margin %, Units Sold. `StringFormat='₱{0:N2}'` for currency; `CanUserSortColumns="True"` for interactive re-sorting; items delivered pre-sorted by margin ascending from Presenter.
- **Views/Accounting/IncomeStatementView.Designer code.vb**: code-behind with constructor injection of `IncomeStatementPresenter`.

## Feature: ACC-09

### Overview
Implemented the Sales Summary View — daily/weekly/monthly breakdown by payment method with mandatory "What This Means" interpretation and credit warning logic.

### Requirements
- **Presenters/SalesSummaryPresenter.vb**: Presenter with `SalesSummaryPeriodType` enum (Daily/Weekly/Monthly), period toggle bool properties (`IsDaily`, `IsWeekly`, `IsMonthly`), visibility helpers (`ShowDatePicker`, `ShowMonthSelectors`, `ShowDailyBreakdown`), 4 KPI card properties (`TotalNetSalesDisplay`, `TransactionCount`, `AvgTransactionValueDisplay`, `TotalReturnsDisplay`), payment breakdown footer properties (`TotalGrossSalesDisplay`, `TotalTxCountDisplay`), `HasCreditWarning` flag (triggered when credit >= 30%), `WhatThisMeansText`, `ObservableCollection(Of PaymentBreakdownDto)`, `ObservableCollection(Of DailySalesDto)`, and `AsyncRelayCommand LoadCommand`. Weekly period anchors to Sunday of the selected date via `AddDays(-(CInt(DayOfWeek)))`.
- **Views/Accounting/SalesSummaryView.Designer code**: Toolbar with Daily/Weekly/Monthly RadioButton toggles; `DatePicker` for Daily/Weekly (visible via `ShowDatePicker`); Month+Year ComboBoxes for Monthly (visible via `ShowMonthSelectors`); mandatory "What This Means" box with `DataTrigger` on `HasCreditWarning` changing background from light blue (#EBF5FB) to light yellow (#FEF9E7) and showing a "⚠ Credit Alert — High AR Exposure" badge; 4 KPI summary cards in a 4-column Grid; payment method breakdown as an `ItemsControl` with custom header/footer rows (footer shows totals from Presenter); daily breakdown `DataGrid` (Gross Sales / Discounts / Returns / Net Sales / Transactions per day) with `Visibility` bound to `ShowDailyBreakdown`.
- **Views/Accounting/SalesSummaryView.Designer code.vb**: code-behind with constructor injection of `SalesSummaryPresenter`.

## Feature: ACC-10

### Overview
Implemented the Accounting VAT Ledger Schema Extension (ACC-10). Added BIR three-bucket VAT columns to existing ledger entities via partial-class extensions, introduced `VatReturn` and `VatReturnLine` entities with full EF configuration, and shipped a manual migration (`AddVatLedgerColumns`) with non-destructive backfill. Original ACC-01 and ACC-02 source files are unchanged.

### Requirements
- **Enums/VatReturnPeriodType.vb**: `Monthly / Quarterly` enum
- **Enums/VatReturnFormType.vb**: `Form2550M / Form2550Q / Form2551Q` enum
- **Enums/VatFilingStatus.vb**: `Draft / Generated / Filed / Amended` enum
- **Entities/Extensions/LedgerVatExtensions.vb**: partial-class extensions adding 6 VAT columns (`VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, `OutputVat`, `InputVat`, `VatTreatment`) to `RevenueRecord` and `ExpenseRecord`
- **Entities/VatReturn.vb**: filing-period header entity (`Acc_VatReturns`)
- **Entities/VatReturnLine.vb**: source-document line entity (`Acc_VatReturnLines`)
- **Data/Configurations/VatReturnMap.vb**: EF Fluent API configuration for `VatReturn` (composite unique index, cascade-delete to lines) and `VatReturnLine` (two indexes)
- **Data/AccountingDbContextVatExtension.vb**: partial-class extension adding `VatReturns` and `VatReturnLines` DbSets without modifying `AccountingDbContext.vb`
- **Migrations/20260510100000_AddVatLedgerColumns.vb**: manual migration with `ALTER TABLE … ADD COLUMN`, `CREATE TABLE`, backfill `UPDATE`, and `Down` using `DROP TABLE` + `ALTER TABLE … DROP COLUMN`
- Updated `Migrations/AccountingDbContextModelSnapshot.vb` — added VAT columns to `ExpenseRecord` and `RevenueRecord` entity blocks; added `VatReturn` and `VatReturnLine` entity blocks with FK relationship and navigation

## Feature: ACC-11

### Overview
Implemented the full BIR VAT Reporting Service pipeline for ACC-11. This includes MediatR handlers that consume VAT-enriched events, `IVatReportingService` that generates/files/amends BIR Form 2550M (monthly VAT), Form 2550Q (quarterly VAT), and Form 2551Q (quarterly 3% percentage tax), a `VatReturnExporter` for CSV and plain-text PDF export, and a Manager-only WinForms view to preview, lock, and export returns.

### Requirements
**New files — SharedKernel:**
- **MerchSys.SharedKernel/Queries/GetVatConfigurationQuery.vb**: cross-module MediatR query for POS VatConfiguration; follows GetProductCostQuery pattern
- **MerchSys.SharedKernel/Queries/GetVatConfigurationResult.vb**: result DTO: `IsVatRegistered`, `VatRate`, `NonVatPercentageTaxRate`
**New files — MerchSys.POS:**
- **MerchSys.POS/Handlers/GetVatConfigurationQueryHandler.vb**: reads `POSDbContext.VatConfigurations` AsNoTracking; returns safe defaults with warning if row is missing
**New files — MerchSys.Accounting:**
- **MerchSys.Accounting/Exceptions/VatReturnLockedException.vb**: inherits `InvalidOperationException`; properties: `ReturnId`, `Year`, `Period`, `FormType`
- **MerchSys.Accounting/Handlers/SaleCompletedWithVatHandler.vb**: handles `SaleCompletedWithVatEvent`; idempotency on `(SourceTransactionId, ProductId)`; updates VAT columns if record exists, creates full record otherwise
- **MerchSys.Accounting/Handlers/GoodsReceivedWithVatHandler.vb**: handles `GoodsReceivedWithVatEvent`; idempotency via `(SourceModule, SourceReferenceId.HasValue + .Value, Description.Contains)`; populates three-bucket VAT on ExpenseRecord
- **MerchSys.Accounting/Services/IVatReportingService.vb**: interface with Generate/Get/List/File/Amend methods
- **MerchSys.Accounting/Services/VatReportingService.vb**: full implementation; uses IMediator to get VatConfiguration cross-module; private `LedgerData` class aggregates three-bucket totals; `GuardAndClearExistingAsync` enforces lock on Filed returns; `BuildVatReturn` maps all BIR line numbers (2550M: 1-23, 2550Q: 1-28, 2551Q: 1-14); banker's rounding throughout
- **MerchSys.Accounting/Services/VatReturnExporter.vb**: both `IVatReturnExporter` interface and `VatReturnExporter` class; CSV export enumerates all form-specific BIR lines; PDF export reads embedded `.template` resource with `{{Placeholder}}` substitution, falls back to built-in string if resource missing; returns `MemoryStream`
- **MerchSys.Accounting/Presenters/VatReturnPresenter.vb**: `VatReturnLineRow`, `ExportReadyEventArgs`, full Presenter with IsForm2550M/Q/R boolean properties, `UpdateWhatThisMeans()`, `AsyncRelayCommand` for Generate/File/Amend/ExportCSV/ExportPDF; export commands raise `ExportReady` event (keeps Presenter in class library without WinForms reference)
- **MerchSys.Accounting/Migrations/20260515100000_FixVatReturnAmendedIndex.vb**: drops full unique index on `(Year, Period, PeriodType, FormType)`; recreates as partial unique index `WHERE FilingStatus != 3` to allow Amended rows without constraint violation
**New files — MerchSys.App:**
- **MerchSys.App/Views/Accounting/VatReturnView.Designer code**: Manager-only UserControl; toolbar, "What This Means" strip, Year/Period ComboBoxes, form-type RadioButtons bound to IsForm2550M/Q, summary KPI cards (5 columns), Generate/File/Amend/ExportCSV/ExportPDF buttons, Lines DataGrid
- **MerchSys.App/Views/Accounting/VatReturnView.Designer code.vb**: code-behind subscribes to `ExportReady`; handles `SaveFileDialog` and writes stream to chosen path
**New files — Templates:**
- **MerchSys.Accounting/Reports/Templates/Form2550M.template**: plain-text BIR Form 2550M layout with `{{Placeholder}}` substitution markers
- **MerchSys.Accounting/Reports/Templates/Form2550Q.template**: quarterly VAT form template
- **MerchSys.Accounting/Reports/Templates/Form2551Q.template**: non-VAT percentage tax form template
**Modified files:**
- **MerchSys.Accounting/MerchSys.Accounting.vbproj**: added `<EmbeddedResource>` entries for all three `.template` files
- **MerchSys.App/Data/DatabaseInitializer.vb**: added `ApplyVatLedgerColumns` (creates `Acc_VatReturns`, `Acc_VatReturnLines`, adds VAT columns to Revenue/Expense tables, backfills zeros) and `ApplyFixVatReturnAmendedIndex` (partial unique index migration) — fills the gap left by ACC-10 which defined the EF migration file but never wired it into the initializer
- **MerchSys.App/Application.Designer code.vb**: registered `IVatReportingService`, `IVatReturnExporter`, `VatReturnPresenter`, `VatReturnView` with DI
- **MerchSys.App/Presenters/MainWindowPresenter.vb**: extracted `BuildAccountingNavItems()`; injects `ISessionService`; VAT Return nav item gated on `UserRole.Manager`
- **MerchSys.POS/Services/VatConfigurationLoader.vb**: added missing `Imports System.Threading` (pre-existing BC30002 from ACC-10)

## Feature: ACC-12

### Overview
Implemented the VAT Payable KPI widget for the Financial Overview Dashboard (ACC-12).
Added a `VatPayableKpiProvider` behind an `IKpiProvider` abstraction, a decorator
`VatEnrichedFinancialOverviewService` that enriches the ACC-03 DTO without touching its
source file, partial-class extensions on both `FinancialOverviewDto` and
`FinancialOverviewPresenter`, a "What This Means" insight provider, and a standalone
`VatPayableTile.Designer code` UserControl.

### Requirements
**New files — MerchSys.Accounting:**
- **Services/IKpiProvider.vb**: `IKpiProvider` interface, `KpiValue` (with `DueDate As DateTime?` extension beyond plan spec to avoid fragile string parsing), `KpiSeverity` enum
- **Services/VatPayableKpiProvider.vb**: full implementation; handles `VatReturnLockedException` by falling back to `ListReturnsAsync` (Await-in-Catch pattern avoided per VB.NET BC36943 restriction — captured bool flag before re-await)
- **Services/VatEnrichedFinancialOverviewService.vb**: decorator; calls inner service then each `IKpiProvider`; maps `"VatPayable"` key to DTO VAT extension fields
- **Services/Insights/IFinancialInsightProvider.vb**: `IFinancialInsightProvider` interface
- **Services/Insights/VatPayableInsightProvider.vb**: generates Info/Warning/Critical insight sentences from the enriched DTO
- **Presenters/Extensions/FinancialOverviewVatExtension.vb**: two partial classes:
- `Partial Public Class FinancialOverviewDto` (Services namespace) — adds `VatPayable`, `VatPayableLabel`, `VatFilingDueDate`, `VatPayableSeverity`, `IsVatRegistered`, `VatPeriodDescription`
- `Partial Public Class FinancialOverviewPresenter` (Presenters namespace) — adds six observable VAT properties, `IsVatWarning`/`IsVatCritical` read-only helpers, `NavigateToVatReturnCommand`, `NavigateToVatReturnRequested` event, and an `OnPropertyChanged` override that triggers `ApplyVatDataFromServiceAsync` on every IsBusy True→False transition (hooking into the existing refresh cycle without modifying the ACC-07 file)
**New files — MerchSys.App:**
- **Views/Accounting/Components/VatPayableTile.Designer code**: standalone `UserControl` with severity DataTriggers (neutral/amber/red), peso amount, label, and due-date secondary line; inherits parent DataContext
- **Views/Accounting/Components/VatPayableTile.Designer code.vb**: minimal code-behind (`InitializeComponent` only; DataContext from parent)
**Modified files:**
- `Services/WhatThisMeansService.vb` — added constructor accepting `IEnumerable(Of IFinancialInsightProvider)`; `GenerateOverviewInterpretation` now appends each provider's sentence after the standard text
- `MerchSys.App/Application.Designer code.vb` — replaced single `IFinancialOverviewService` registration with decorator pattern (`FinancialOverviewService` registered as concrete, `IFinancialOverviewService` resolved via factory that wraps it in `VatEnrichedFinancialOverviewService`); added `IKpiProvider`, `IFinancialInsightProvider`, and `VatPayableTile` registrations
**Unchanged (verified via `git diff`):**
- `Services/IFinancialOverviewService.vb` (ACC-03)
- `Services/FinancialOverviewService.vb` (ACC-03)
- `Presenters/FinancialOverviewPresenter.vb` (ACC-07)
- `Views/Accounting/FinancialOverviewView.Designer code` (ACC-07)

## Feature: ACC-13

### Overview
Implemented the VAT Ledger Schema Verification harness (ACC-13) — a `#If DEBUG`-gated pair of files that closes the three verification gaps flagged by the 2026-05-11 Accounting audit against the ACC-10 acceptance criteria.

### Requirements
- **Debug/VatLedgerSchemaHarness.vb**: four-check schema verification harness targeting isolated `%TEMP%` SQLite databases.
- **Debug/VatLedgerSchemaHarnessRunner.vb**: thin static entry point (`Module`) that instantiates the harness, collects the report, and writes Markdown to `%TEMP%\vat-ledger-schema-report-<timestamp>.md`.

## Feature: ACC-14

### Overview
Implemented ACC-14: VAT Tile Integration into Financial Overview. Placed `VatPayableTile` in `FinancialOverviewView.Designer code`, wired the navigation handler in the code-behind using the type-based `NavigationItem` lookup pattern, and produced `VatTileSmokeHarness.vb` for end-to-end verification.

### Requirements
- **Views/Accounting/FinancialOverviewView.Designer code**: added `xmlns:vatTiles` namespace, 8th `ColumnDefinition Width="*"`, removed `Margin="0"` override from Inventory Value tile, added `<vatTiles:VatPayableTile Grid.Column="7" Margin="0"/>` as the new last KPI tile
- **Views/Accounting/FinancialOverviewView.Designer code.vb**: added `System.Linq` and `MerchSys.App.Presenters` imports; wired `AddHandler Presenter.NavigateToVatReturnRequested, AddressOf OnNavigateToVatReturnRequested` in constructor; implemented `OnNavigateToVatReturnRequested` using type-based `NavigationItem` resolution from `MainWindowPresenter.NavigationGroups`
- **Debug/VatTileSmokeHarness.vb**: `#If DEBUG`-gated harness; builds scratch AccountingDbContext in `%TEMP%`; seeds `RevenueRecord` (VatableAmount=₱100,000, OutputVat=₱12,000) and `ExpenseRecord` (InputVat=₱3,000); resolves `IFinancialOverviewService` from isolated ServiceCollection; asserts `ComputedVatPayable = 9000`; navigation check via reflection on host's `IServiceProvider`; writes Markdown report to `%TEMP%\vat-tile-smoke-report-<timestamp>.md`
- **MerchSys.Accounting.vbproj**: added `Microsoft.Extensions.DependencyInjection 10.0.7` and `Microsoft.Extensions.Hosting.Abstractions 10.0.7` (required by harness for `ServiceCollection`, `BuildServiceProvider()`, and `IHost`)

## Feature: ACC-15

### Overview
Implemented the Receipt Tamper Audit Handler for ACC-15. Added a durable, append-only audit ledger (`Acc_TamperAuditLog`) and a MediatR handler that consumes `ReceiptTamperDetectedEvent` from POS-13, persisting every tamper incident with full context. Also delivered a read-side query service interface for future reporting plans.

### Requirements
- **MerchSys.Accounting/Entities/TamperAuditEntry.vb**: new append-only entity; deviates from standard audit-column convention (no `IsDeleted`, no `ModifiedBy`/`ModifiedAt`); deviation documented in XML doc comment
- **MerchSys.Accounting/Data/AccountingDbContextTamperExtension.vb**: partial class adding `TamperAuditEntries` DbSet without modifying ACC-02 source
- **MerchSys.Accounting/Data/Configurations/TamperAuditEntryConfiguration.vb**: EF Core `IEntityTypeConfiguration`; configures table name `Acc_TamperAuditLog`, nullable columns, and composite index `IX_Acc_TamperAuditLog_DetectedAt_TamperKind`
- **MerchSys.Accounting/Migrations/20260516100000_AddTamperAuditLog.vb**: migration with `CREATE TABLE`, composite index, and two SQLite immutability triggers (UPDATE-blocking, DELETE-blocking); cross-references INFRA-08 for MariaDB equivalents
- Updated `MerchSys.Accounting/Migrations/AccountingDbContextModelSnapshot.vb` — added `TamperAuditEntry` entity block
- **MerchSys.Accounting/Handlers/ReceiptTamperDetectedHandler.vb**: `INotificationHandler(Of ReceiptTamperDetectedEvent)`; sink-only, rethrows on save failure after logging Critical; captures `Environment.MachineName` and `WindowsIdentity.GetCurrent()?.Name`
- **MerchSys.Accounting/Services/ITamperAuditQueryService.vb**: interface + `TamperAuditQueryService` implementation in same file; `GetIncidentsAsync` returns rows ordered by `DetectedAt DESC`; `CountByKindAsync` materializes TamperKind strings then groups in memory to avoid EF translation edge cases
- Updated `MerchSys.App/Data/DatabaseInitializer.vb` — added `ApplyIfPending` call + `ApplyTamperAuditLog` method with idempotent `CREATE TABLE IF NOT EXISTS`, index, and `CREATE TRIGGER IF NOT EXISTS`
- Updated `MerchSys.App/Application.Designer code.vb` — added `services.AddScoped(Of ITamperAuditQueryService, TamperAuditQueryService)()`; handler is auto-registered via MediatR assembly scanning (`RegisterServicesFromAssembly` on `AccountingDbContext` assembly), no double registration

## Feature: ACC-16

### Overview
Implemented ACC-16: VatPayableTile Financial Overview Placement & Navigation Wiring.

Upon inspection, the core functional deliverables of ACC-16 were already implemented by ACC-14
(`14-vat-tile-integration.md`), which placed the tile and wired the navigation handler ahead of
this plan executing. ACC-16's remaining output requirement (the Designer code documentation comment) was
added in this session.

### Requirements
- **Views/Accounting/FinancialOverviewView.Designer code**: added four-line inline
comment above `<vatTiles:VatPayableTile>` documenting the tile's DataContext source
(`VatEnrichedFinancialOverviewService` decorator), click-navigation chain, and type-based
NavigationItem lookup

## Feature: ACC-17

### Overview
Implemented ACC-17: Schema Verification Harness Dev-Menu Integration. Wired the
`VatLedgerSchemaHarnessRunner` (ACC-13) to a developer-only sidebar navigation entry so the
harness can be invoked from the running application without manual code changes.

### Requirements
- **Views/Debug/DebugMenuExtensions.vb**: code-only `UserControl`
(`DebugMenuView`) that renders the developer debug panel. Contains the `#If DEBUG`-gated
`RunVatSchemaHarness_Click` handler, which calls
`VatLedgerSchemaHarnessRunner.RunAndReportAsync(Nothing)` and surfaces a `MessageBox` on
completion (or on error). BC36943 compliant: `Await` is inside `Try`, not `Catch`.
- **Startup/DebugServiceRegistration.vb**: `#If DEBUG`-gated
`DebugServiceRegistration` Module with `AddDebugServices` extension method that registers
`DebugMenuView` as Transient in DI.
- **Application.Designer code.vb**: added `#If DEBUG` block calling
`services.AddDebugServices()` after the Shell registrations.
- **Presenters/MainWindowPresenter.vb**: refactored
`BuildNavigationGroups` to assign to a local variable before returning, then appends a
"Developer Tools" `NavigationGroup` with a "Run VAT Schema Harness" `NavigationItem`
(pointing to `DebugMenuView`) inside a `#If DEBUG` block.

## Feature: ACC-18

### Overview
Implemented the Tamper Audit Report UI — a read-only compliance view allowing Manager and Owner roles to review receipt tamper incidents. Consumes `ITamperAuditQueryService` (delivered in ACC-15) and displays incidents in a filterable DataGrid with hash truncation, colour-coded severity, and an empty-state banner.

### Requirements
- **Presenters/TamperAuditReportPresenter.vb**: Presenter with `TamperAuditEntryDto` nested class, date-range filter (default last 30 days), `LoadCommand` (AsyncRelayCommand), and `HasNoEntries`/`HasEntries` observable properties for conditional visibility
- **Views/Accounting/TamperAuditReportView.Designer code**: DataGrid-based view with 6 columns (Date/Time, Receipt Number, Expected Hash, Actual Hash, Severity, Details); hash columns use `ExpectedHashShort`/`ActualHashShort` (12-char truncation) in the cell and full value in ToolTip; Severity styled red for Critical; empty-state banner bound to `HasNoEntries`
- **Views/Accounting/TamperAuditReportView.Designer code.vb**: Constructor-injection code-behind
- **Presenters/MainWindowPresenter.vb**: Added "Tamper Audit Report" `NavigationItem` to `BuildAccountingNavItems()` (unconditional — accessible to both Manager and Owner)
- **Application.Designer code.vb**: Registered `TamperAuditReportPresenter` (Transient) and `Views.Accounting.TamperAuditReportView` (Transient) in the DI container

## Feature: ACC-19

### Overview
Implemented the BIR VAT Relief Report (ACC-19) — a lightweight, month-scoped summary view that aggregates the three-bucket VAT totals from the revenue and expense ledgers without the full per-line BIR form layout delivered by ACC-11.

### Requirements
- **Services/IVatReliefReportService.vb**: defines `VatReliefSummary` DTO and `IVatReliefReportService` interface with `GetMonthlySummaryAsync` and `GetTrailingMonthsAsync`
- **Services/VatReliefReportService.vb**: implementation using raw `SqliteConnection` + synchronous `reader.Read()` loop (EF Core 10 VB.NET workaround); connection string obtained via `_db.Database.GetConnectionString()` mirroring `VatReportingService`
- **Presenters/VatReliefReportPresenter.vb**: MVP Presenter with `LoadCommand`/`RefreshCommand`, trailing-months `ObservableCollection`, and `WhatThisMeans` builder using `CultureInfo("en-PH")`
- **Views/Accounting/VatReliefReportView.Designer code**: read-only view: period selector toolbar, Net VAT Payable banner (colour-coded Red/Green/Grey), two side-by-side summary cards (Sales and Purchases), trailing 12-month DataGrid
- **Views/Accounting/VatReliefReportView.Designer code.vb**: minimal code-behind; Presenter injected via constructor
- **Application.Designer code.vb**: added `AddScoped(Of IVatReliefReportService, VatReliefReportService)`, `AddTransient(Of VatReliefReportPresenter)`, and `AddTransient(Of Views.Accounting.VatReliefReportView)`
- **Presenters/MainWindowPresenter.vb**: added "VAT Relief Report" nav item to `BuildAccountingNavItems` (Manager + Owner) and to `BuildOwnerNavigationGroups` Accounting section
- Removed item #1 (BIR VAT Relief Report) from `Plans/Future/deferred-features-backlog.md` and updated `last-synced`

## Feature: ACC-20

### Overview
Implemented the **Tamper Incident Report — CSV / PDF Export (ACC-20)** feature. This allows BIR auditors and company owners/managers to export all persisted tamper audit log events within a given date range. Both CSV and PDF formats are generated directly from the view, utilizing QuestPDF for PDF creation and standard BCL StreamWriter for RFC 4180-compliant CSV generation.

### Requirements
- **SharedKernel changes**:
- Expanded `GetVatConfigurationResult.vb` to include `BusinessName`, `BusinessAddress`, and `BusinessTIN` properties.
- **POS changes**:
- Modified `GetVatConfigurationQueryHandler.vb` to populate the new properties from the `VatConfiguration` singleton entity.
- **Accounting changes**:
- Added package reference for `QuestPDF` version `2026.5.0` to `MerchSys.Accounting.vbproj`.
- Created `ITamperReportExporter.vb` defining the export abstraction.
- Created `TamperReportFormat.vb` enum defining Csv and Pdf.
- Created `TamperReportExportOptions.vb` config class.
- Created `TamperReportExporter.vb` implementing ITamperReportExporter.
- Modified `TamperAuditReportPresenter.vb` to inject exporter and options, and expose `ExportCsvCommand` and `ExportPdfCommand` relay commands.
- **App/Views changes**:
- Registered the scoped exporter service, bound settings configuration, and declared the QuestPDF Community license in `Application.Designer code.vb` under Accounting registrations.
- Added "Export to CSV" and "Export to PDF" buttons in `TamperAuditReportView.Designer code`.
- Implemented the file dialog click handlers and environment suggested path resolver in `TamperAuditReportView.Designer code.vb`.
- Added `Accounting:TamperReport:Export` configurations in `appsettings.json`.

## Feature: ACC-21

### Overview
Implemented per-batch FIFO COGS accuracy by persisting exact FIFO deduction details from Inventory to a new ledger `Inv_SaleCogs` during stock deductions. Created a new MediatR query `GetSaleCogsBreakdownQuery` to retrieve the details, and updated the Accounting `SaleRevenueHandler` to sum exact per-batch costs, eliminating costing errors and phantom losses/profits.

### Requirements
- **Created:** `Entities/SaleCogsRecord.vb` — represents per-batch audit records.
- **Created:** `Data/Configurations/SaleCogsRecordConfiguration.vb` — configuration for `Inv_SaleCogs` table and indexes.
- **Modified:** `InventoryDbContext.vb` to register the new `SaleCogsRecords` DbSet.
- **Created:** `Migrations/20260527120000_AddInvSaleCogs.vb` — manual VB.NET schema migration class.
- **Modified:** `DatabaseInitializer.vb` to apply raw SQL table creation at startup.
- **Modified:** `SaleCompletedHandler.vb` in Inventory to persist deductions to `Inv_SaleCogs` with idempotency guards.
- **Created:** `Queries/GetSaleCogsBreakdownQuery.vb` — MediatR query and result DTO.
- **Created:** `Handlers/GetSaleCogsBreakdownQueryHandler.vb` — Projects directly to bypass EF Core 10 `ToListAsync` entity bug.
- **Modified:** `SaleRevenueHandler.vb` in Accounting to query exact per-batch COGS, sum it, and fall back defensively to the oldest FIFO batch cost.

## Feature: ACC-22

### Overview
Consolidated writing of `Acc_RevenueRecords` to eliminate the duplicate writer race condition between `SaleCompletedAccountingHandler` (legacy) and `SaleCompletedWithVatHandler` (VAT). The legacy handler has been deleted, and the VAT handler was renamed to `SaleRevenueHandler` and modified to act as the single authoritative writer for revenue and COGS expenses for POS transactions, preserving proper idempotency and VAT handling.

### Requirements
- **Deleted:** `Handlers/SaleCompletedAccountingHandler.vb` — legacy duplicate writer. Grep searches confirm no other source file references it.
- **Created:** `Handlers/SaleRevenueHandler.vb` (Renamed and modified from `SaleCompletedWithVatHandler.vb`) — consolidated authoritative writer.
- **Modified:** `SaleRevenueHandler.vb` to absorb COGS `ExpenseRecord` writes, including strict idempotency checks keyed by `(Category="COGS", SourceModule="POS", SourceReferenceId=TransactionId)` and description patterns to ensure safety on POS transaction re-publishes.

## Feature: ACC-23

### Overview
Replaced the plain-text "Export PDF" output on three accounting views (Income Statement, VAT Relief Report, VAT Return) with proper A4 PDF documents generated by QuestPDF. The shared `ReportPreviewWindow` was redesigned from a plain-text `TextBox` to an image-based PDF page previewer with "Save As PDF" and "Print" actions.

### Requirements
- **Created:** `Services/Reporting/ReportPdfResult.vb` — shared DTO carrying `PdfBytes`, `PageImages`, and `SuggestedFileName` between exporter services and the preview window.
- **Created:** `Services/Reporting/IIncomeStatementPdfExporter.vb` — interface for Income Statement PDF generation.
- **Created:** `Services/Reporting/IncomeStatementPdfExporter.vb` — QuestPDF implementation rendering A4 portrait document with business header, comparative P&L table, product breakdown, and paginated footer. Resolves business identity via `GetVatConfigurationQuery`.
- **Created:** `Services/Reporting/IVatReliefPdfExporter.vb` — interface for VAT Relief Report PDF generation.
- **Created:** `Services/Reporting/VatReliefPdfExporter.vb` — QuestPDF implementation rendering Sales/Purchases summaries, Net VAT Payable, interpretation text, and trailing 12-month trend table.
- **Modified:** `Services/VatReturnExporter.vb` — rewrote `ExportPdfAsync` from plain-text template rendering to QuestPDF `Document.Create()` with a structured BIR form line-number table. Added `BuildCsvLineItems` helper and `VatReturnLineItem` private DTO.
- **Modified:** `Presenters/VatReturnPresenter.vb` — changed file extension from `.pdf.txt` to `.pdf` and dialog filter from text files to `"PDF files (*.pdf)|*.pdf"`.
- **Modified:** `Views/Shell/ReportPreviewWindow.Designer code` — replaced `TextBox` with `ItemsControl` of page images inside `ScrollViewer` with inline `DropShadowEffect` for paper-like appearance.
- **Modified:** `Views/Shell/ReportPreviewWindow.Designer code.vb` — new constructor accepts `ReportPdfResult`. Converts page PNG byte arrays to `BitmapImage` sources for preview. "Save As PDF..." writes actual PDF bytes. "Print" renders page images via `PrintDialog.PrintVisual`.
- **Modified:** `Views/Accounting/IncomeStatementView.Designer code.vb` — injected `IIncomeStatementPdfExporter` via constructor. `ExportPdf_Click` generates PDF and opens preview window. Removed old `BuildIncomeStatementReport` plain-text method.
- **Modified:** `Views/Accounting/VatReliefReportView.Designer code.vb` — injected `IVatReliefPdfExporter` via constructor. `ExportPdf_Click` generates PDF and opens preview window. Removed old `BuildVatReliefReport` plain-text method.
- **Modified:** `Application.Designer code.vb` — added two Scoped DI registrations: `IIncomeStatementPdfExporter → IncomeStatementPdfExporter` and `IVatReliefPdfExporter → VatReliefPdfExporter`.



## Verification (from ACC-verification-checklist.md)

---
module: Accounting
source: Accounting-audit-2026-06-01.md
originally-generated: 2026-05-17
last-synced: 2026-06-01
---

# Operator Verification Checklist — Accounting

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 18 Accounting plans are completed. These are the remaining acceptance tests.
> 
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
>
> **Login required (INFRA-15):** The app now shows a login screen on launch.
> Unless a test specifically says to log in as Owner, log in as `manager`.
> Default password: `Vista2026!` (first login will prompt you to change it).

### Key file locations

| What | Path |
|------|------|
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| Database config | `WinForms_Applications\MerchSys\src\MerchSys.App\Data\DatabaseConfig.vb` |
| FinancialOverviewView (Designer code) | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.Designer code` |
| FinancialOverviewView (code-behind) | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.Designer code.vb` |
| VatPayableTile | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\Accounting\Components\VatPayableTile.Designer code.vb` |
| FinancialOverviewVatExtension | `WinForms_Applications\MerchSys\src\MerchSys.Accounting\Presenters\Extensions\FinancialOverviewVatExtension.vb` |
| Concurrency harness | `WinForms_Applications\MerchSys\src\MerchSys.POS\Tests\Pos.SequenceConcurrencyHarness.vb` |

---

## ACC-10 — VAT Ledger Schema Extension

### Test 1: Fresh database migration

**What to do:**
1. Go to `%LOCALAPPDATA%\MerchSys\` in File Explorer.
2. Rename `merchsys.db` to `merchsys.db.bak` (this is your backup).
3. Open the solution in Visual Studio and press **F5** to launch the app in Debug mode. The app will create a new database on startup.
4. Wait for the app to fully load.

**What you should see:**
- The app starts without errors.
- No crash or migration error messages appear.
- A new `merchsys.db` file appears in `%LOCALAPPDATA%\MerchSys\`.

- [X] Fresh database migration works

---

### Test 2: Existing database migration

**What to do:**
1. If you backed up in Test 1, restore it: delete the new `merchsys.db` and rename `merchsys.db.bak` back to `merchsys.db`.
2. Press **F5** to launch the app in Debug mode.

**What you should see:**
- The app starts without errors.
- No crash or migration error messages appear.
- Your old data is still there.

- [X] Existing database migration works

---

### Test 3: Duplicate VAT return filing is blocked

**What to do:**
1. Open DB Browser for SQLite (or similar tool).
2. Open the database file at `%LOCALAPPDATA%\MerchSys\merchsys.db`.
3. Find the `Acc_VatReturns` table.
4. Try to insert two rows that have the **same** `PeriodStart`, `PeriodEnd`, and `FormType` values.

**What you should see:**
- The first row inserts fine.
- The second row fails with a unique constraint error.

- [X] Duplicate VAT return filing is blocked

---

### Test 4: Deleting a VAT return also deletes its lines

**What to do:**
1. Open DB Browser for SQLite.
2. Open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
3. Find a row in `Acc_VatReturns` that has child rows in `Acc_VatReturnLines`.
4. Note the `Id` of the VAT return.
5. Delete that VAT return row.
6. Check `Acc_VatReturnLines` for rows with that same `VatReturnId`.

**What you should see:**
- All the child rows in `Acc_VatReturnLines` are automatically deleted when you delete the parent.

- [X] Cascade delete from VatReturn to VatReturnLines works

---

## ACC-12 — VAT Payable KPI Tile

### Test 5: VAT tile shows correct amount

**What to do:**
1. Make sure there is VAT ledger data in the database for the current month. (If there is none, complete a few sales first, or seed data manually in `Acc_VatReturnLines`.)
2. Launch the app and log in as `manager` (use your changed password, or `Vista2026!` if first run — you will be prompted to change it).
3. Navigate to the **Financial Overview** screen.
4. Find the VAT Payable tile among the KPI cards.

> **View file:** `WinForms_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.Designer code`
> **Tile file:** `WinForms_Applications\MerchSys\src\MerchSys.App\Views\Accounting\Components\VatPayableTile.Designer code.vb`

**What you should see:**
- The tile displays a peso amount that matches the sum of VAT for the current period.
- The tile colour changes based on how close the BIR deadline is:
  - Green = plenty of time remaining
  - Yellow/Orange = deadline is approaching
  - Red = deadline is very close or past

- [X] VAT tile shows correct amount and colour — "Percentage Tax" ₱306.00 Due Jul 25, white border (Info severity, deadline 65+ days away)

---

## ACC-13 — VAT Ledger Schema Verification Harness

### Test 6: Run the schema harness from the developer menu

**What to do:**
1. Build the solution in **Debug** configuration (Ctrl+Shift+B).
2. Press **F5** to launch the app.
3. In the menu bar, click **Developer Tools** (or look for a "Dev" menu).
4. Click **Run VAT Schema Harness** (or similar button text).
5. Wait a few seconds for it to finish.

**What you should see:**
- A message box pops up telling you the harness is done.
- A `.md` (Markdown) report file appears in your `%TEMP%` folder (usually `C:\Users\<you>\AppData\Local\Temp\`).
- Open that report. It should show all four checks with a **Pass** status.

- [X] Schema harness runs and all four checks pass — fixed: MigrateAsync() silently skips VB.NET migrations on scratch DB; replaced with SetupScratchSchema() raw-SQL helper mirroring DatabaseInitializer

---

## ACC-14 + ACC-16 — VAT Tile in Financial Overview (combined)

> ACC-14 and ACC-16 have the same acceptance tests. Completing these once covers both plans.

### Test 7: Manager can see and click the VAT tile

**What to do:**
1. Press **F5** to launch the app.
2. Log in with username `manager` and your password.
3. Navigate to the **Financial Overview** screen.
4. Look for the VAT Payable tile (it should be among the KPI cards at the top).
5. Click the VAT Payable tile.

> **Navigation handler:** `WinForms_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.Designer code.vb` — look for `NavigateToVatReturnRequested`

**What you should see:**
- The tile is visible on the Financial Overview.
- Clicking it takes you to the **VAT Return View** screen.

- [X] Manager sees VAT tile and clicking it opens VatReturnView — fixed: Application.Current.MainWindow was LoginView (first shown); handler now iterates Application.Current.Windows to find the shell

---

### Test 8: Owner can see the tile but cannot navigate

**What to do:**
1. Launch the app.
2. Log out if currently logged in (click **Log Out** in the sidebar).
3. Log in with username `owner` and your password.
4. Navigate to the **Financial Overview** screen.
5. Look for the VAT Payable tile.
6. Click the VAT Payable tile.

**What you should see:**
- The tile is visible on the Financial Overview.
- Clicking it does **nothing** (no navigation happens). The Owner role is restricted from accessing the VAT Return View.

- [X] Owner sees VAT tile but clicking does nothing — VAT Return nav item absent from Owner nav groups; FirstOrDefault returns Nothing, navigation suppressed

---

### Test 9: VAT tile smoke harness

**What to do:**
1. Build the solution in **Debug** configuration.
2. Press **F5** to launch the app.
3. Open the **Immediate Window** in Visual Studio (Debug → Windows → Immediate).
4. Type: `? Await VatTileSmokeHarness.RunAsync(host)` and press Enter.
5. Read the output.

> **VAT extension logic:** `WinForms_Applications\MerchSys\src\MerchSys.Accounting\Presenters\Extensions\FinancialOverviewVatExtension.vb`

**What you should see:**
- `ComputedVatPayable = 9000` (or whatever the expected test value is)
- `NavigationRouteFound = True`

- [X] VatTileSmokeHarness passes — ComputedVatPayable = ₱9,000.00 ✅, NavigationRouteFound = True ✅ — via Dev menu button (Immediate Window approach broken by VS hot-reload + Private _host not in scope as 'host')

---

## ACC-17 — Schema Harness Dev-Menu Button

> This overlaps with Test 6 above. If you already did Test 6, just confirm the button exists.

### Test 10: Dev menu button exists and works

**What to do:**
1. Build in **Debug** configuration and launch the app (F5).
2. Go to **Developer Tools** in the menu.
3. Click the **Run VAT Schema Harness** button.

**What you should see:**
- A `MessageBox` pops up confirming the harness ran.
- A `.md` report file appears in `%TEMP%`.

- [X] Dev menu button exists and produces harness report — report confirmed in %TEMP%: 4/4 checks passed (vat-ledger-schema-report-20260522-132501.md)


### Future / Backlog Item

## 4. IDbContextFactory Registration for Harnesses

**Module:** Accounting / Infrastructure
**Source:** ACC-13 What's Next
**Description:** Consider adding `IDbContextFactory(Of AccountingDbContext)` registration to `DatabaseConfig.AddModuleDbContexts` so that future verification harnesses can use factory-based multi-instance patterns instead of the single-instance `DbContext`.
**Why deferred:** The current harness (ACC-13) works fine with the existing pattern. This is only needed if future harnesses require concurrent database access.
**Depends on:** ACC-13 (VAT Ledger Schema Verification — completed).

---


### Future / Backlog Item

## 5. MariaDB Immutability Triggers for Acc_TamperAuditLog

**Status:** CLOSED — completed natively in central initial schema migration 0001 (triggers `tr_acc_tamper_no_update` and `tr_acc_tamper_no_delete` are active on `Acc_TamperAuditLog`).
**Module:** Accounting / Infrastructure
**Source:** ACC-15 What's Next
**Description:** The `Acc_TamperAuditLog` table has SQLite immutability triggers (deployed by ACC-15), but no MariaDB equivalents for the central replica. This is a SQL-only task similar to INFRA-08 (which added MariaDB triggers for POS tables, but not Acc_* tables).
**Why deferred:** The central MariaDB deployment is not yet live, and a formal DB admin account hasn't been established yet.
**Depends on:** ACC-15 (completed), INFRA-08 (completed), DB admin account (not yet created).

---


### Future / Backlog Item

## 18. Stock Valuation, COGS Accuracy, and Duplicate Revenue Record Bugs

**Module:** Inventory / Accounting / POS (cross-cutting)
**Source:** User Observation (2026-05-27) — hands-on testing with multi-vendor purchasing
**Status:** CLOSED — completed via INV-15, ACC-21, and ACC-22. Fully verified under Test 7 of the Manager checklist (split-batch POS checkout and FIFO COGS).
- **18a → INV-15** `Plans/VISTA_Modules/Inventory/15-stock-dashboard-cost-column.md` (UI cost visibility — completed)
- **18c → ACC-22** `Plans/VISTA_Modules/Accounting/22-revenue-record-consolidation.md` (eliminate duplicate-writer race — completed)
- **18b → ACC-21** `Plans/VISTA_Modules/Accounting/21-per-batch-cogs-accuracy.md` (per-batch FIFO COGS via new `Inv_SaleCogs` ledger — completed)

### Reproduction Steps

1. **Vendor Product Catalog tab:** Added "Ammonium Sulfate" (ProductId=3) to all three vendors with different prices:
   - AgriChem Supplies (VendorId=1): ₱1,100/unit
   - FarmFresh Seed Corp. (VendorId=2): ₱1,000/unit
   - Golden Feeds Trading (VendorId=3): ₱1,200/unit
2. **Purchase Orders tab:** Created 3 POs (PO-2026-0001 to PO-2026-0003), one per vendor, each ordering 10 units.
3. **Goods Receiving tab:** Confirmed all 3 POs → 3 `StockBatch` rows created (Batch 1 = ₱1,200, Batch 2 = ₱1,100, Batch 3 = ₱1,000). Total stock: 30 units, total cost: ₱33,000.
4. **Stock Dashboard tab:** Stock quantity was correct (30 units), but the "Price" column showed ₱1,100 — the `RetailPrice` from `Inv_Products` set in the Product Management tab, not the actual per-batch purchase cost.
5. **Sales Cart tab:** Sold all 30 units in a single transaction at ₱1,100/unit (the `RetailPrice`). Revenue = ₱33,000.
6. **Expected:** Some profit or at least break-even, since the average purchase cost was ₱1,100/unit (= ₱33,000/30).
7. **Actual:** Two `RevenueRecord` rows were created with conflicting COGS values, and one shows a ₱3,000 *loss*.

### Root Causes (verified via database inspection)

#### 18a. Stock Dashboard "Price" column shows `RetailPrice`, not actual purchase cost

The `StockDashboardService.GetDashboardDataAsync()` correctly computes `StockValue` using FIFO `UnitCost` from `Inv_StockBatches` (line 116: `b.QuantityRemaining * b.UnitCost`), but the `ProductSummaryDto.RetailPrice` field is sourced from `Inv_Products.RetailPrice` — the selling price set in Product Management, **not** the purchase cost. The dashboard's "Price" column therefore doesn't reflect what was actually paid to vendors.

**DB evidence:**
- `Inv_Products` (ProductId=3): `RetailPrice` = 1100
- `Inv_StockBatches`: Batch 1 `UnitCost` = 1200, Batch 2 `UnitCost` = 1100, Batch 3 `UnitCost` = 1000

**Impact:** User cannot tell the actual purchase cost per product from the Stock Dashboard. The `StockValue` sum card is correct (it uses batch `UnitCost`), but the per-row "Price" column is misleading because it shows the retail selling price, not the cost.

**Fix scope:** The dashboard could show a **weighted-average cost** column alongside `RetailPrice`, computed as `SUM(QuantityRemaining × UnitCost) / SUM(QuantityRemaining)` across non-expired batches. Alternatively, show the FIFO cost (oldest batch's `UnitCost`), matching what Accounting uses for COGS.

#### 18b. COGS uses only the FIFO-oldest batch's `UnitCost` for the *entire* sale quantity

`SaleCompletedAccountingHandler.Handle()` calls `GetProductCostQuery` to get the COGS unit cost. The handler (`GetProductCostQueryHandler`) returns the `UnitCost` of the **single** oldest non-expired batch with remaining stock, then multiplies it by the full `item.Quantity`:

```
costResult.FifoUnitCost * item.Quantity   ← line 45, SaleCompletedAccountingHandler.vb
```

This is incorrect when a sale quantity spans multiple FIFO batches at different costs. The `SaleCompletedHandler` in Inventory *correctly* deducts stock across multiple batches via `DeductStockFIFOAsync()` and returns per-batch `FIFODeductionResult.COGS` values, but Accounting **ignores this** and re-queries cost independently using `GetProductCostQuery`, which only returns a single unit cost.

**DB evidence:**
- `Acc_RevenueRecords` (Id=1): `COGS` = 36000, `GrossProfit` = -3000 (used ₱1,200 × 30 = ₱36,000 — the oldest batch price, despite only 10 of those 30 units came from that batch).
- Correct COGS should be: (10 × ₱1,200) + (10 × ₱1,100) + (10 × ₱1,000) = ₱33,000, yielding ₱0 profit (break-even since `RetailPrice` = average cost = ₱1,100).

**Impact:** **Financial statements show phantom losses (or inflated profits, depending on batch price ordering)**. The reported COGS is wrong any time a sale spans multiple batches with different unit costs. This is an accounting accuracy bug — not cosmetic.

**Fix scope:** The Accounting handler should receive the actual per-batch COGS breakdown from the Inventory module's FIFO deduction engine (the `FIFODeductionResult` list from `StockService.DeductStockFIFOAsync`) instead of independently re-querying a single unit cost. Options:
1. Include the per-batch COGS breakdown directly in the `SaleCompletedEvent` (requires the Inventory handler to run first and pass data downstream).
2. Replace `GetProductCostQuery` with a new query that sums COGS across the batches that were *actually consumed* during this specific sale deduction.

#### 18c. Duplicate `RevenueRecord` rows — race between legacy and VAT accounting handlers

Two `Acc_RevenueRecords` exist for the same `(SourceTransactionId=1, ProductId=3)`:
- Record Id=1: `COGS` = 36000, `GrossProfit` = -3000 (created by `SaleCompletedAccountingHandler`)
- Record Id=2: `COGS` = 0, `GrossProfit` = 33000 (created by `SaleCompletedWithVatHandler` in its `Else` branch)

The `SaleCompletedWithVatHandler` is designed to be idempotent: it first queries for an existing `RevenueRecord` matching `(SourceTransactionId, ProductId)` and updates VAT columns if found. However, **the existing record was not found** — the VAT handler's `Else` branch executed, creating a second record. This happened because:
- `SaleCompletedAccountingHandler` was called first and added the record to the EF `DbContext`, but `SaveChangesWithJournalAsync` may not have flushed before `SaleCompletedWithVatHandler.Handle()` ran its `FirstOrDefaultAsync` query on the **same** `DbContext` instance — resulting in the query not finding the record.
- The second record has `COGS = 0` because by the time `GetProductCostQuery` ran for it, all 30 units had already been deducted by the Inventory handler, leaving zero remaining stock.

**DB evidence:**
- `Acc_ExpenseRecords` (Id=4): `Category` = "COGS", `Amount` = 0 (the COGS expense from the second handler)
- Only one `Pos_SalesTransactions` row exists (Id=1), confirming only one sale happened, but two revenue records exist.

**Impact:** Financial reports double-count revenue, and one of the two records has zero COGS. The duplicate must be prevented.

**Fix scope:** The `SaleCompletedAccountingHandler` should call `SaveChangesWithJournalAsync` *before* the VAT handler runs, so the `FirstOrDefaultAsync` in the VAT handler sees the committed record. Alternatively, combine both handlers into a single handler, or ensure handler ordering via MediatR pipeline configuration.

### Dependencies
- 18a: Independent — UI/display change only.
- 18b: Requires rethinking the COGS data flow between Inventory's FIFO deduction engine and Accounting's revenue recording. May require changes to `SaleCompletedEvent` or a new cross-module query.
- 18c: Requires fixing MediatR handler ordering or merging the two accounting handlers. Related to the dual-event design from POS-14.

### Source Document Evidence

The bugs documented above violate requirements and promises stated in the original project source documents. The following passages confirm that **per-batch COGS accuracy** is not a nice-to-have — it is a core design commitment.

**system_plan.md (§6.2 — Inventory Module):**
> "FIFO Costing: Oldest batch cost used first when valuing sold items, per client confirmation. **Batch-level purchase records maintained for accurate COGS calculation.**"

**system_plan.md (§9 — Database Design Principles):**
> "FIFO Batch-Level Records: Purchase price recorded at the batch/lot level per product to support accurate FIFO COGS calculation. **Oldest unreserved batch cost consumed first on each sale.**"

**system_plan.md (§10/§11 — Risk Register, Risk P5, rated High/High):**
> "Price Volatility & Costing Error: Retail price not updated after supplier cost change — confirmed ongoing problem — **leading to incorrect COGS and distorted profit margins.**"
>
> Mitigation: "FIFO batch-level costing ensures historical sales are not retroactively affected by new purchase prices."

Bug 18b is the *exact manifestation* of P5's predicted failure mode: when a single product is purchased from multiple vendors at different prices, COGS is calculated using only the oldest batch's `UnitCost` for the entire quantity, producing distorted profit margins.

**Inventory-Module_AcademicPaper.md (§2 — FIFO Costing and Inventory Valuation Methods):**
> "When a sale occurs, the system deducts units from the oldest batch first, **calculating the cost of goods sold based on that batch's unit cost.** This approach ensures that the FIFO assumption is applied precisely at the batch level, producing accurate cost-of-goods-sold figures." — (Kieso et al., 2023)

The Inventory module (`StockService.DeductStockFIFOAsync`) correctly implements this: it walks batches oldest-first and computes per-batch COGS in `FIFODeductionResult`. The bug is that Accounting ignores this result and re-queries a single unit cost via `GetProductCostQuery`.

**Accounting-Module_AcademicPaper.md (§1.5 — Scope, V1 Features):**
> "The cost of goods sold will be calculated using the FIFO (First-In, First-Out) costing method, as confirmed by the client, with costs recorded at the batch and lot level per product at the time of receiving goods. **The oldest unreserved batch cost will be consumed first on each sale** to ensure that reported COGS accurately reflects the cost of goods actually sold."

**Accounting-Module_AcademicPaper.md (§2 — Theme 3, FIFO Costing):**
> "Without disciplined batch-level cost tracking, the FIFO method degrades into an approximation that may not accurately reflect the actual flow of costs through inventory." — (Garcia & Lim, 2023)
>
> "Without precise product-level costing, businesses cannot determine which products generate the highest margins, which are unprofitable, and where pricing adjustments are needed." — (Villanueva & Reyes, 2022)

**Purchasing-Module_AcademicPaper.md (§1.2 — Conceptual Framework):**
> P5 identifies "price volatility that causes COGS distortions" as an independent variable the module must address.

**Conclusion:** Bug 18b (single-batch COGS for multi-batch sales) directly contradicts the documented design intent of batch-level FIFO COGS. Bugs 18a and 18c are consequential — 18a prevents the user from *seeing* the cost discrepancy before a sale, and 18c corrupts the accounting output after a sale. Together, these three bugs undermine the system's stated goal of "producing COGS figures that accurately represent the cost of goods actually sold" and its P5 risk mitigation.

---


### Future / Backlog Item

## 21. Merchandising-Format Balance Sheet & Cash Flow Statement

**Module:** Accounting & Financial Reporting
**Source:** `Accounting-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Implement the formal merchandising Balance Sheet (detailing Assets at FIFO merchandise cost, Accounts Payable liabilities, and Owner's Equity) and a cash-basis Cash Flow Statement to track actual cash movements. The Cash Flow Statement is critical for anticipating Lanao del Norte's local market cycles where credit sales delay cash inflows while supplier invoices fall due.
**Why deferred:** Deferred to V2 scope per the academic study constraints. The V1 scope was deliberately bounded to the P&L statement, Sales summaries, and VAT reporting dashboards to match the manager's immediate financial literacy constraints.

---


### Future / Backlog Item

## 22. General Ledger and Integration Journals (Sales, AR Aging, AP Aging)

**Module:** Accounting & Financial Reporting
**Source:** `Accounting-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Implement formal accounting ledger structures, including the master General Ledger, Sales Journal, Accounts Receivable (AR) Aging reports (categorizing outstanding customer credit by 30/60/90 days), and Accounts Payable (AP) Aging reports.
**Why deferred:** Highly visual dashboard cards (e.g. outstanding AP alerts, overdue AR collection flags) provide immediate management utility. Comprehensive multi-row ledger grids are deferred to the V2 auditing phase.

---

