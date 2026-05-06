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
