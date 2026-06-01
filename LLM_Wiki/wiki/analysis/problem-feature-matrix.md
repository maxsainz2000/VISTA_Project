---
type: analysis
title: "Problem-Feature Traceability Matrix"
aliases: [traceability matrix, problem-to-feature map]
sources: [Sources/system_plan.md, Sources/Purchasing-Module_AcademicPaper.md, Sources/Inventory-Module_AcademicPaper.md, Sources/POS-Module_AcademicPaper.md, Sources/Accounting-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, module-purchasing, module-inventory, module-pos, module-accounting]
last-updated: 2026-06-01
---

# Problem-Feature Traceability Matrix

Complete mapping of all 25 identified problems to their VISTA solutions.

> [!NOTE]
> **Real-World Empirical Validation:** The completed manager interview (`Sources/Villon_Interview_Populated.md`) serves as a 100% harmonious real-world validation source for all 25 problem-to-feature pairs listed below, confirming that these academic and theoretical specifications mirror actual operational pain points and constraints at Villon Farm Supply.


## Purchasing Module (P1–P6)

| ID | Problem | Feature | Status |
|---|---|---|---|
| P1 | Gut-feel reorders | [[reorder-suggestion-engine\|Reorder Suggestion Engine]] | Planned |
| P2 | No PO audit trail | PO Lifecycle with status tracking | Planned |
| P3 | Manual retail price updates | Price Change Detection & Alerts | Planned |
| P4 | No AP ledger | AP Tracking with due dates | Planned |
| P5 | Price volatility → bad COGS | Batch-level cost recording | Planned |
| P6 | No seasonal demand model | Seasonal flags in reorder engine | Planned |

## Inventory Module (I1–I7)

| ID | Problem | Feature | Status |
|---|---|---|---|
| I1 | Manual ledger only | Real-time digital stock records | Planned |
| I2 | Weekly count 1–3 hrs | Auto-updated on receive/sale | Planned |
| I3 | No dashboard | Real-Time Stock Dashboard | Planned |
| I4 | No expiry tracking | [[expiry-date-tracking\|Batch-level Expiry Monitoring]] | Planned |
| I5 | Unknown inventory value | Live [[fifo-costing\|FIFO]] Valuation | Planned |
| I6 | Demand-spike stockouts | Predictive Stockout Estimation | Planned |
| I7 | No fast/slow product view | Product Velocity Classification | Planned |

## POS Module (S1–S5)

| ID | Problem | Feature | Status |
|---|---|---|---|
| S1 | No digital sales record | Cart-based TX interface + history | Planned |
| S2 | Informal utang ledger | Digital [[utang-credit-system\|credit accounts]] | Planned |
| S3 | No BIR OR numbering | [[bir-compliance\|OR-YYYY-XXXX]] auto receipts | Planned |
| S4 | No credit blocking | Hard block when balance > 0 | Planned |
| S5 | No daily summary | Automated daily sales summary | Planned |

## Accounting Module (A1–A7)

| ID | Problem | Feature | Status |
|---|---|---|---|
| A1 | Manual bookkeeping | Auto-generated reports from module data | Planned |
| A2 | Reports via text/summaries | Direct dashboard access | Planned |
| A3 | No product-level margins | Gross profit per product (FIFO) | Planned |
| A4 | Cash flow shortfalls | AR/AP visibility + alerts (V2 full) | Planned |
| A5 | Partial financial literacy | [[plain-language-reporting\|"What This Means"]] boxes | Planned |
| A6 | No automated warnings | Overdue AR, AP due dates, slow sales | Planned |
| A7 | Financial position after-the-fact | Real-time Financial Overview | Planned |

## Summary

- **Total Problems:** 25
- **Total Features:** 25 (1:1 mapping)
- **Status:** All Planned — none yet implemented

## Source References

- [[wiki/sources/system-plan|System Plan]] — complete problem list
- Individual module papers — detailed requirements per problem
- [[wiki/sources/villon-interview-populated|Villon Interview]] — operational validation of all 25 problem-feature pairs

