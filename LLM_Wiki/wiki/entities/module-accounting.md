---
type: entity
title: "Accounting Module"
aliases: [MerchSys.Accounting, Accounting]
sources: [Sources/system_plan.md, Sources/Accounting-Module_AcademicPaper.md]
related: [villon-farm-supply, fifo-costing, plain-language-reporting, module-purchasing, module-inventory, module-pos]
last-updated: 2026-05-02
---

# Accounting Module (MerchSys.Accounting)

Aggregates data from Purchasing, Inventory, and POS to auto-generate financial reports. Every report tab carries a mandatory [[plain-language-reporting|"What This Means"]] interpretation box.

## Problems → Features

| Problem | Feature |
|---|---|
| A1 — Manual bookkeeping | Auto-generated reports from live module data |
| A2 — Reports via text/summaries | Direct dashboard access for Owner |
| A3 — No product-level margin tracking | Gross profit per product (FIFO-based) |
| A4 — Cash flow shortfalls | AR/AP visibility + early warning alerts (V2 full) |
| A5 — Partial financial literacy | [[plain-language-reporting\|"What This Means"]] boxes (mandatory) |
| A6 — No automated warnings | Overdue AR, AP due dates, slow sales alerts |
| A7 — Financial position after-the-fact only | Real-time Financial Overview dashboard |

## V1 Scope

1. **Financial Overview Dashboard** — KPIs, 6-month trend, top products
2. **Merchandising-Format Income Statement** — monthly/quarterly/annual
3. **Sales Summary** — daily/weekly/monthly by payment method
4. **"What This Means" Boxes** — every tab, non-negotiable

## V2 Scope (Planned)

- Balance Sheet, Cash Flow Statement
- Sales Journal, AR Aging, AP Aging
- General Ledger, full Audit Trail

## Data Flow

```
Purchasing → Accounting (AP, purchase costs)
Inventory  → Accounting (COGS via FIFO, valuations)
POS        → Accounting (revenue, AR, payment methods)
```

## User Roles

- **Manager:** View all V1 reports + "What This Means" guidance
- **Owner:** View all V1 reports (primary consumer of Financial Overview)

## Namespace

`MerchSys.Accounting`

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture, problem list
- [[wiki/sources/accounting-module-paper|Accounting Paper]] — detailed requirements, V1/V2 scope
