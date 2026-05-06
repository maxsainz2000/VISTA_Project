---
type: layer-manifest
module: MerchSys.Accounting
layer: Entities
last-updated: 2026-05-06
---
7: 
8: # MerchSys.Accounting — Entities
9: 
10: This page details the Entities for the **MerchSys.Accounting** module.
11: 
12: ## Files and Classes
13: 
14: | File Path | Class / Interface | Base / Implements | Key Members / Responsibilities |
15: |---|---|---|---|
16: | `src/MerchSys.Accounting/Entities/FinancialPeriod.vb` | `FinancialPeriod` | `AuditableEntity` | Summarized P&L per period type (Daily/Weekly/Monthly/Quarterly/Annual). |
17: | `src/MerchSys.Accounting/Entities/RevenueRecord.vb` | `RevenueRecord` | `AuditableEntity` | Captured from POS events; tracks FIFO COGS and per-product gross profit. |
18: | `src/MerchSys.Accounting/Entities/ExpenseRecord.vb` | `ExpenseRecord` | `AuditableEntity` | Captures postings from all source modules (COGS, shrinkage, operating expenses). |
19: | `src/MerchSys.Accounting/Entities/FinancialSnapshot.vb` | `FinancialSnapshot` | `AuditableEntity` | Point-in-time KPI cache for AR, AP, and inventory valuation. |
