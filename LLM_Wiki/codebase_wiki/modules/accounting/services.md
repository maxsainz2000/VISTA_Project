---
type: layer-manifest
module: MerchSys.Accounting
layer: Services
last-updated: 2026-05-27
---

# MerchSys.Accounting — Services

This page details the Service implementations for the **MerchSys.Accounting** module.

## Core Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Accounting/Services/IFinancialOverviewService.vb`<br>`src/MerchSys.Accounting/Services/FinancialOverviewService.vb` | `IFinancialOverviewService`<br>`FinancialOverviewService` | Financial Overview Service. Provides KPI aggregation (Revenue MTD/YTD), 6-month revenue/margin trends, top-selling products, and AR/AP/Inventory balances for the dashboard. `RefreshSnapshotAsync` recomputes revenue KPIs, fetches Inventory valuation via MediatR, and upserts a daily `FinancialSnapshot`. |
| `src/MerchSys.Accounting/Services/VatEnrichedFinancialOverviewService.vb` | `IFinancialOverviewService`<br>`VatEnrichedFinancialOverviewService` | Decorator for `FinancialOverviewService`. Enriches the dashboard DTO with VAT-specific KPIs and alerts using registered `IKpiProvider` implementations without modifying the core service. |
| `src/MerchSys.Accounting/Services/IKpiProvider.vb`<br>`src/MerchSys.Accounting/Services/VatPayableKpiProvider.vb` | `IKpiProvider`<br>`VatPayableKpiProvider` | KPI Provider abstraction. `VatPayableKpiProvider` computes current VAT liability, handles filing locks, and determines severity (Neutral/Amber/Red) based on the BIR deadline. |
| `src/MerchSys.Accounting/Services/IIncomeStatementService.vb`<br>`src/MerchSys.Accounting/Services/IncomeStatementService.vb` | `IIncomeStatementService`<br>`IncomeStatementService` | Income Statement Service. Implements merchandising-format P&L (Net Sales → COGS → Gross Profit → OpEx → Net Income). Supports monthly, quarterly, annual, and custom date range views using FIFO-based COGS and provides per-product margin analysis. |
| `src/MerchSys.Accounting/Services/ISalesSummaryService.vb`<br>`src/MerchSys.Accounting/Services/SalesSummaryService.vb` | `ISalesSummaryService`<br>`SalesSummaryService` | Sales Summary Service. Implements formal accounting view of daily/weekly/monthly sales breakdown by payment method. Operates on RevenueRecord and ExpenseRecord data captured by accounting handlers. |
| `src/MerchSys.Accounting/Services/IWhatThisMeansService.vb`<br>`src/MerchSys.Accounting/Services/WhatThisMeansService.vb` | `IWhatThisMeansService`<br>`WhatThisMeansService` | Plain-language interpretation engine. Translates raw financial DTOs into actionable sentences. Now supports extensible `IFinancialInsightProvider` registration to append modular insights (e.g., VAT status) to dashboard summaries. |
| `src/MerchSys.Accounting/Services/Insights/IFinancialInsightProvider.vb`<br>`src/MerchSys.Accounting/Services/Insights/VatPayableInsightProvider.vb` | `IFinancialInsightProvider`<br>`VatPayableInsightProvider` | Insight Provider abstraction. `VatPayableInsightProvider` generates human-readable sentences regarding VAT filing status and liability severity. |
| `src/MerchSys.Accounting/Services/IVatReportingService.vb`<br>`src/MerchSys.Accounting/Services/VatReportingService.vb` | `IVatReportingService`<br>`VatReportingService` | BIR VAT Reporting pipeline. Generates, files, and amends BIR Forms 2550M, 2550Q, and 2551Q. Aggregates three-bucket VAT totals (Sales, Goods, Services) and enforces filing locks. |
| `src/MerchSys.Accounting/Services/IVatReliefReportService.vb`<br>`src/MerchSys.Accounting/Services/VatReliefReportService.vb` | `IVatReliefReportService`<br>`VatReliefReportService` | BIR VAT Relief Report service. Aggregates monthly three-bucket VAT totals across sales and purchases directly from `Acc_RevenueRecords` and `Acc_ExpenseRecords`. Supports chronological multi-month trends via `GetTrailingMonthsAsync`. Uses raw `MySqlConnection` to bypass EF Core 10 + VB.NET `ToListAsync` empty-result bug (see `agent_wiki/errors/efcore10-vbnet-tolistasync-empty.md`). |
| `src/MerchSys.Accounting/Services/VatReturnExporter.vb` | `IVatReturnExporter`<br>`VatReturnExporter` | BIR Form exporter. Generates CSV for electronic filing and plain-text PDF templates for human-readable output using placeholder substitution. (Note: Interface `IVatReturnExporter` is declared in the same file). |
| `src/MerchSys.Accounting/Services/ITamperAuditQueryService.vb` | `ITamperAuditQueryService`<br>`TamperAuditQueryService` | Provides read-side access to the tamper audit ledger. Returns incidents ordered by detection date and provides in-memory grouping for incident counts by kind to avoid EF translation edge cases. |
| `src/MerchSys.Accounting/Services/Reporting/ITamperReportExporter.vb`<br>`src/MerchSys.Accounting/Services/Reporting/TamperReportExporter.vb` | `ITamperReportExporter`<br>`TamperReportExporter` | Tamper report exporter. Generates BIR compliance-friendly CSV (RFC 4180 with UTF-8 BOM) and professional A4 PDF reports (QuestPDF) of tamper incidents with strict no-overwrite protection. Supports the strongly-typed `TamperReportExportOptions` model. |
| (Resource) | `Reports/Templates/*.template` | Embedded plain-text templates for BIR Forms 2550M, 2550Q, and 2551Q used by the exporter. |
