---
type: layer-manifest
module: MerchSys.Accounting
layer: Views
last-updated: 2026-06-07
---

# MerchSys.Accounting — Views

This page details the Views for the **MerchSys.Accounting** module.

## UI Components

| File Path | View | Code-Behind | Key Responsibilities |
|---|---|---|---|
| `src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml` | `FinancialOverviewView` | `.xaml.vb` | Dashboard displaying KPIs, 6-month trends, top products, and actionable alerts. |
| `src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml` | `IncomeStatementView` | `.xaml.vb` | Displays the P&L statement supporting monthly, quarterly, and annual periods. Added CSV and PDF (Preview) export toolbar actions (UX-42). |
| `src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml` | `SalesSummaryView` | `.xaml.vb` | Shows sales breakdown by payment method and daily performance over selected periods. |
| `src/MerchSys.App/Views/Accounting/VatReturnView.xaml` | `VatReturnView` | `.xaml.vb` | Manager-only BIR VAT reporting interface. Supports generation, locking, and export of Forms 2550M/Q and 2551Q. DataGrid virtualization (Recycling) enabled (UX-37). |
| `src/MerchSys.App/Views/Accounting/VatReliefReportView.xaml` | `VatReliefReportView` | `.xaml.vb` | Read-only compliance view displaying the monthly VAT Relief Report with side-by-side sales/purchases summaries, large color-coded Net VAT banner, "What This Means" strip, and trailing 12-month trend grid. Added CSV and PDF (Preview) export toolbar actions (UX-42). |
| `src/MerchSys.App/Views/Accounting/Components/VatPayableTile.xaml` | `VatPayableTile` | `.xaml.vb` | KPI tile for the dashboard displaying current VAT liability and filing deadline with severity-based styling. |
| `src/MerchSys.App/Views/Accounting/TamperAuditReportView.xaml` | `TamperAuditReportView` | `.xaml.vb` | Compliance view allowing Manager and Owner roles to review filterable receipt tamper incidents, with added options to export data to CSV and PDF formats via code-behind `SaveFileDialog` click handlers. Mounts `FilterSummaryBar` showing live "{shown} of {total}" count and active `FilterChipItem` chips (UX-31). Keeps busy spinner overlay instead of skeleton loader since it is a monolithic compliance report view (UX-31). Configured with logical TabIndex mapping and Alt mnemonics (UX-32). DataGrid virtualization (Recycling) enabled (UX-37). |
