---
type: source-summary
title: "Purchasing Module Academic Paper"
aliases: [Purchasing Paper, MerchSys.Purchasing Paper]
sources: [Sources/Purchasing-Module_AcademicPaper.md]
related: [module-purchasing, villon-farm-supply, fifo-costing, reorder-suggestion-engine, iso-25010-2023, utaut2, descriptive-developmental-design, tacurong-city]
last-updated: 2026-05-02
---

# Purchasing Module Paper — Source Summary

**Raw source:** `Sources/Purchasing-Module_AcademicPaper.md` (266 lines)

## Executive Summary

Academic paper (Chapters 1–3) for the MerchSys.Purchasing module. Documents six purchasing problems (P1–P6) at [[villon-farm-supply|Villon Farm Supply]], proposes a Windows Forms desktop solution with vendor directory, PO lifecycle, AP tracking, price change detection, and [[reorder-suggestion-engine|reorder suggestion engine]]. Evaluated against [[iso-25010-2023|ISO 25010:2023]] and [[utaut2|UTAUT2]].

## Problems Addressed

| ID | Problem | Impact |
|---|---|---|
| P1 | Reorder by gut feel — no data threshold | Stockouts and overordering |
| P2 | Fully manual purchasing — no audit trail | No accountability |
| P3 | Manual retail price updates on supplier price change | Incorrect margins |
| P4 | No formal AP ledger — tracked by supplier visits | Missed payments |
| P5 | Price volatility → incorrect COGS | Profitability distortion |
| P6 | No seasonal demand model | Peak-season stockouts |

## Core Features (from IPO Process)

1. **Vendor Directory** — contact details, pricing history, lead time, multi-supplier support
2. **Purchase Order Management** — Draft → Submitted → Received → Verified lifecycle
3. **Goods Receiving Verification** — quantity/condition check, expiry capture, discrepancy flagging
4. **AP Tracking** — credit terms, due dates, outstanding balances
5. **Price Change Detection** — alerts manager to review retail price before selling
6. **Reorder Suggestion Engine** — based on stock level, threshold, velocity, lead time, seasonal flags

## Research Design

- **Type:** [[descriptive-developmental-design|Descriptive-Developmental]]
- **Respondents:** Manager + Owner (purposive sampling, n=2)
- **Locale:** [[tacurong-city|Tacurong City]], Sultan Kudarat
- **Evaluation:** [[iso-25010-2023|ISO 25010:2023]] (8 characteristics, 5-pt Likert) + [[utaut2|UTAUT2]] (User Acceptance, Ease of Use/Usefulness)

## Key Definitions

- **Reorder Point** = avg daily demand × lead time + safety stock
- **Lead Time** = days from PO placement to goods receipt
- **PO Lifecycle** = Draft → Submitted → Received → Verified → Closed

## Delimitations

- Excludes Inventory, POS, Accounting modules
- No mobile/web deployment
- No EDI or supplier system integration
- No ML/AI forecasting — uses statistical methods only

## Literature Highlights

- CPFR model (Hill et al., 2018) — collaborative procurement planning
- EzStock (Lim et al., 2025) — purpose-built inventory for small business
- Digital transformation yields 23% cycle time reduction, 31% order accuracy improvement (Rane et al., 2024)
- Simple statistical methods outperform gut-feel even with limited data (Syntetos et al., 2016)

## Source Citations

All content from `Sources/Purchasing-Module_AcademicPaper.md`.
