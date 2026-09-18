---
type: source-summary
title: "Villon Interview Populated"
aliases: [Villon Interview, Manager Interview, Interview Summary]
sources: [Sources/Villon_Interview_Populated.md]
related: [villon-farm-supply, module-purchasing, module-inventory, module-pos, module-accounting, utang-credit-system, expiry-date-tracking, plain-language-reporting, reorder-suggestion-engine, bir-compliance]
last-updated: 2026-06-01
---

# Villon Interview Populated — Source Summary

**Raw source:** `Sources/Villon_Interview_Populated.md` (361 lines)

## Executive Summary

This document summarizes the interview with the Manager of **Villon Farm Supply** regarding the pre-system operational realities, pain points, and specific requirements across all four core VISTA modules. The interview validates the academic paper baselines, provides concrete parameters (such as the ~50 SKU catalog scale and Sunday market day), and confirms critical system rules (such as zero-tolerance credit blocking and batch-level FIFO inventory valuation). 

---

## Problems & Operational Realities

The interview documents major manual workflows and operational pain points that align with the VISTA system's problem statements:

| ID | Operational Reality / Pain Point | VISTA Traceability |
|---|---|---|
| **Purchasing** | • Reordering is determined solely by gut feel and visual shelf checks (Q1).<br>• Ordering is completely manual via phone/text or in-person visits (Q2, Q3).<br>• High supplier price volatility is the major frustration; currently updates retail prices manually (Q9).<br>• Accounts payable (AP) is tracked informally based on when suppliers visit rather than an AP ledger (Q7). | Maps to **P1**, **P2**, **P3**, **P4**, **P5** |
| **Inventory** | • Stock levels are monitored via a physical record book updated per transaction (Q10).<br>• Full physical counts are performed weekly and take 1 to 3 hours (Q10).<br>• Stockout incidents occur due to high customer demand and delayed supplier deliveries, risking lost sales and customer attrition (Q17).<br>• Total monetary value of stock on hand is currently unknown and never systematically calculated (Q18). | Maps to **I1**, **I2**, **I3**, **I5**, **I6** |
| **POS / Sales** | • Typical sale is manual; credit (utang) transactions are logged in a physical credit ledger (Q20).<br>• Extended credit has no formal limit, but a strict "no additional credit if balance exists" rule is manually enforced (Q22).<br>• Cashiering errors and price disputes occur, resolved by manual book/receipt lookups (Q27). | Maps to **S1**, **S2**, **S4** |
| **Accounting** | • Bookkeeping is personally handled manually by the store owner (Q29).<br>• Reports are requested daily/weekly and delivered informally via text/chat messages (Q30).<br>• Product-level gross profit margin is not systematically tracked (Q31).<br>• Cash flow gaps occur when large credit sales delay cash collection while supplier bills fall due (Q32). | Maps to **A1**, **A2**, **A3**, **A4** |

---

## Feature Validations & System Requirements

The manager's responses provide strong, direct validation for the planned VISTA feature set:

1. **FIFO Costing (Q13):** The manager explicitly selected **FIFO (First In, First Out)** as the costing method to value sold items, confirming the batch-level costing model.
2. **Zero-Tolerance Credit Blocking (Q22):** The manager enforces a strict policy that no customer can incur new credit if they have an outstanding unpaid balance. This matches the system's hard credit blocking rule.
3. **Expiry Date Tracking (Q12):** Expiry tracking is confirmed as a requirement for **Pesticides/Chemicals**, **Seeds**, and **Feeds**. **Fertilizers** are implicitly validated as shelf-stable, matching the exclusion of fertilizers from expiry requirements.
4. **Plain-Language Reporting (Q33, Q34):** The manager has partial financial literacy and requests monthly reports written in plain language highlighting:
   - Actual monthly profit or loss (earnings check).
   - Cash flow status (liquidity).
   - Inventory movement (what sold, what remained, what to replenish).
5. **Reorder Engine & Alerts (Q8, Q15, Q16, Q28):** Low-stock alerts for fast-moving items are identified as the most valuable warning. The engine must incorporate the **Palay crop calendar** (high-demand planting seasons) for fertilizers/pesticides, and recognize a lead time of "a few days" for supplier deliveries.
6. **Payment Methods (Q21):** Confirms supported payment modes: Cash, GCash/e-wallet, Bank Transfer, and Credit (Utang).
7. **BIR Compliance (Q25, Q36):** Confirms the store is BIR-registered, issues sequential Official Receipts, retains records long-term, and has previously undergone a BIR audit.

---

## Key Operational Parameters

- **SKU Scale:** Approximately 50 distinct products (categorized by pesticides, seeds, feeds, fertilizers) (Q11).
- **Busiest Day:** **Sunday** (the local market day) (Q26).
- **Fastest-Moving Items:** UNO animal feeds for pigs (consistent monthly seller) and agricultural chemicals (seasonal spikes) (Q16).
- **Credit Collection:** Overdue credit is collected when the customer visits the store, or by personal home visits by the manager (Q22).

---

## Source Citations

All facts, workflows, and configurations are traced directly to the raw questionnaire in `Sources/Villon_Interview_Populated.md`.
