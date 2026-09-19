---
type: source-summary
title: "Accounting Module Academic Paper"
aliases: [Accounting Paper, MerchSys.Accounting Paper]
sources: [Sources/Accounting-Module_AcademicPaper.md]
related: [module-accounting, villon-farm-supply, fifo-costing, plain-language-reporting, iso-25010-2023, utaut2, descriptive-developmental-design]
last-updated: 2026-05-02
---

# Accounting Module Paper — Source Summary

**Raw source:** `Sources/Accounting-Module_AcademicPaper.md` (240 lines)

## Executive Summary

Academic paper (Chapters 1–3) for the MerchSys.Accounting module. Documents seven financial reporting problems (A1–A7) at [[villon-farm-supply|Villon Farm Supply]]. The defining feature is the mandatory [[plain-language-reporting|"What This Means"]] plain-language interpretation box on every report, addressing the manager's partial financial literacy (A5). The module aggregates data from Purchasing, Inventory, and POS to auto-generate financial reports.

## Problems Addressed

| ID | Problem | Impact |
|---|---|---|
| A1 | Manual bookkeeping by owner — no automation | Delayed, error-prone records |
| A2 | Reports requested via text/written summaries | Slow, unreliable information |
| A3 | No product-level gross profit margin tracking | Unprofitable products undetected |
| A4 | Cash flow shortfalls from credit-sale timing | Cannot restock or pay suppliers |
| A5 | Manager has partial financial literacy | Reports unused / misinterpreted |
| A6 | No automated early warning for slow sales, AP due dates, overdue AR | Problems detected too late |
| A7 | Financial position known only after-the-fact | Reactive management only |

## Core Features (V1)

1. **Financial Overview Dashboard** — KPIs: revenue, COGS, gross profit, AR outstanding, 6-month trend chart, top-selling products
2. **Merchandising-Format Income Statement** — Net Sales, COGS ([[fifo-costing|FIFO]]), Gross Profit, OpEx, Net Income; monthly/quarterly/annual views
3. **Sales Summary** — daily/weekly/monthly breakdown by payment method
4. **"What This Means" Boxes** — mandatory plain-language interpretation on every report tab (non-negotiable)
5. **FIFO-Based COGS** — batch-level cost tracking, oldest unreserved batch consumed first

## V2 Features (Planned, Not in Scope)

- Balance Sheet, Cash Flow Statement
- 10+ integration reports (Sales Journal, AR Aging, AP Aging, etc.)
- General Ledger, full Audit Trail

## Research Design

- **Type:** [[descriptive-developmental-design|Descriptive-Developmental]]
- **Respondents:** Manager + Owner (purposive sampling, n=2)
- **Locale:** Villon Farm Supply, Lanao del Norte
- **Evaluation:** [[iso-25010-2023|ISO 25010:2023]] + [[utaut2|UTAUT2]]

## Key Definitions

- **Plain-Language Interpretation** = mandatory "What This Means" feature translating financial figures into non-technical sentences
- **Merchandising-Format P&L** = Net Sales → COGS → Gross Profit → OpEx → Net Income
- **Gross Profit Margin** = (Gross Profit / Net Sales) × 100

## Delimitations

- V1 only: KPI dashboard, P&L, Sales Summary, "What This Means"
- Excludes Balance Sheet, Cash Flow, General Ledger (deferred to V2)
- No mobile/web deployment
- No integration with external accounting software (QuickBooks, Xero)

## Literature Highlights

- <30% of Mindanao micro-business owners can independently interpret their own financial statements (Dela Cruz & Santos, 2022)
- Plain-language financial summaries → significantly better decision-making (Chen & Wei, 2021)
- Automated accounting reduces manual error, accelerates reporting (Alao et al., 2024)
- FIFO produces most accurate COGS for merchandising businesses (Garcia & Lim, 2023)

## Source Citations

All content from `Sources/Accounting-Module_AcademicPaper.md`.
