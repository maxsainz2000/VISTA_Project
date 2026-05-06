---
type: layer-manifest
module: MerchSys.Accounting
layer: Services
last-updated: 2026-05-06
---

# MerchSys.Accounting — Services

This page details the Service implementations for the **MerchSys.Accounting** module.

## Core Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Accounting/Services/IFinancialOverviewService.vb`<br>`src/MerchSys.Accounting/Services/FinancialOverviewService.vb` | `IFinancialOverviewService`<br>`FinancialOverviewService` | Financial Overview Service. Provides KPI aggregation (Revenue MTD/YTD), 6-month revenue/margin trends, top-selling products, and AR/AP/Inventory balances for the dashboard. `RefreshSnapshotAsync` recomputes revenue KPIs, fetches Inventory valuation via MediatR, and upserts a daily `FinancialSnapshot`. |
| `src/MerchSys.Accounting/Services/IIncomeStatementService.vb`<br>`src/MerchSys.Accounting/Services/IncomeStatementService.vb` | `IIncomeStatementService`<br>`IncomeStatementService` | Income Statement Service. Implements merchandising-format P&L (Net Sales → COGS → Gross Profit → OpEx → Net Income). Supports monthly, quarterly, annual, and custom date range views using FIFO-based COGS and provides per-product margin analysis. |
| `src/MerchSys.Accounting/Services/ISalesSummaryService.vb`<br>`src/MerchSys.Accounting/Services/SalesSummaryService.vb` | `ISalesSummaryService`<br>`SalesSummaryService` | Sales Summary Service. Implements formal accounting view of daily/weekly/monthly sales breakdown by payment method. Operates on RevenueRecord and ExpenseRecord data captured by accounting handlers. |
