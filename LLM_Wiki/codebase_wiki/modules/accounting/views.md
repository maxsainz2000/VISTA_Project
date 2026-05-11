---
type: layer-manifest
module: MerchSys.Accounting
layer: Views
last-updated: 2026-05-06
---

# MerchSys.Accounting — Views

This page details the Views for the **MerchSys.Accounting** module.

## UI Components

| File Path | View | Code-Behind | Key Responsibilities |
|---|---|---|---|
| `src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml` | `FinancialOverviewView` | `.xaml.vb` | Dashboard displaying KPIs, 6-month trends, top products, and actionable alerts. |
| `src/MerchSys.App/Views/Accounting/IncomeStatementView.xaml` | `IncomeStatementView` | `.xaml.vb` | Displays the P&L statement supporting monthly, quarterly, and annual periods. |
| `src/MerchSys.App/Views/Accounting/SalesSummaryView.xaml` | `SalesSummaryView` | `.xaml.vb` | Shows sales breakdown by payment method and daily performance over selected periods. |
| `src/MerchSys.App/Views/Accounting/VatReturnView.xaml` | `VatReturnView` | `.xaml.vb` | Manager-only BIR VAT reporting interface. Supports generation, locking, and export of Forms 2550M/Q and 2551Q. |
| `src/MerchSys.App/Views/Accounting/Components/VatPayableTile.xaml` | `VatPayableTile` | `.xaml.vb` | KPI tile for the dashboard displaying current VAT liability and filing deadline with severity-based styling. |
| `src/MerchSys.Accounting/Debug/VatTileSmokeHarness.vb` | `VatTileSmokeHarness` | (Harness) | `#If DEBUG`-gated E2E smoke harness for VAT tile logic; seeds scratch DB and verifies computed payable values. |
