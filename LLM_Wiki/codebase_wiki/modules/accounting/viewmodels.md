---
type: layer-manifest
module: MerchSys.Accounting
layer: ViewModels
last-updated: 2026-05-27
---

# MerchSys.Accounting — ViewModels

This page details the ViewModel implementations for the **MerchSys.Accounting** module.

## Core ViewModels

| File Path | Class | Key Responsibilities | Dependencies (DI) |
|---|---|---|---|
| `src/MerchSys.Accounting/ViewModels/FinancialOverviewViewModel.vb` | `FinancialOverviewViewModel` | ViewModel for the Financial Overview Dashboard. Aggregates KPIs, 6-month trends, top products, and alerts. Includes a 5-minute auto-refresh timer, a `RefreshCommand`, and `NavigateToVatReturnRequested` event for shell navigation. | `IFinancialOverviewService`, `IWhatThisMeansService` |
| `src/MerchSys.Accounting/ViewModels/IncomeStatementViewModel.vb` | `IncomeStatementViewModel` | ViewModel for the Income Statement (P&L) view. Supports Monthly, Quarterly, and Annual periods. Provides formatted display strings for accounting lines, "What This Means" interpretation, and per-product margin breakdown. | `IIncomeStatementService`, `IWhatThisMeansService` |
| `src/MerchSys.Accounting/ViewModels/SalesSummaryViewModel.vb` | `SalesSummaryViewModel` | ViewModel for the Sales Summary View. Provides Daily/Weekly/Monthly breakdowns by payment method, KPI summary cards, and mandatory interpretation logic. | `ISalesSummaryService`, `IWhatThisMeansService` |
| `src/MerchSys.Accounting/ViewModels/VatReturnViewModel.vb` | `VatReturnViewModel` | ViewModel for BIR VAT reporting. Manages form state (2550M/Q, 2551Q), triggers generation/filing/amendment, and raises `ExportReady` events for the View to handle file I/O. | `IVatReportingService`, `IVatReturnExporter`, `IWhatThisMeansService` |
| `src/MerchSys.Accounting/ViewModels/VatReliefReportViewModel.vb` | `VatReliefReportViewModel` | ViewModel for the monthly VAT Relief Report. Manages loading monthly aggregates and trailing-month trends, and generates local plain-language explanations of Net VAT payable/credit status. | `IVatReliefReportService` |
| `src/MerchSys.Accounting/ViewModels/TamperAuditReportViewModel.vb` | `TamperAuditReportViewModel` | ViewModel for the tamper audit report. Coordinates loading filterable tamper incident lists from the query service, tracks incident distribution statistics for Manager review, and exposes `ExportCsvCommand` and `ExportPdfCommand` for BIR compliance exporting. | `ITamperAuditQueryService`, `ITamperReportExporter`, `IOptions(Of TamperReportExportOptions)` |

## Support Classes

| File Path | Class | Description |
|---|---|---|
| `src/MerchSys.Accounting/ViewModels/FinancialOverviewViewModel.vb` | `TrendBarItem` | Flat item for the 6-month trend bar chart. Contains pre-computed bar heights for UI normalization. |
| `src/MerchSys.Accounting/ViewModels/IncomeStatementViewModel.vb` | `IncomeStatementPeriodType` | Enum: `Monthly`, `Quarterly`, `Annual`. |
| `src/MerchSys.Accounting/ViewModels/SalesSummaryViewModel.vb` | `SalesSummaryPeriodType` | Enum: `Daily`, `Weekly`, `Monthly`. |
| `src/MerchSys.Accounting/ViewModels/VatReturnViewModel.vb` | `VatReturnLineRow` | DTO for DataGrid display of VAT return line items. |
| `src/MerchSys.Accounting/ViewModels/VatReturnViewModel.vb` | `ExportReadyEventArgs` | Custom event args containing the `MemoryStream` and suggested filename for exports. |
| `src/MerchSys.Accounting/ViewModels/Extensions/FinancialOverviewVatExtension.vb` | `FinancialOverviewViewModel` | Partial class extension. Adds observable VAT properties and hooks into the `OnPropertyChanged` (IsBusy) cycle to trigger VAT data enrichment from the service. |
