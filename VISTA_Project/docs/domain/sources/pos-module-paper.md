---
type: source-summary
title: "POS Module Academic Paper"
aliases: [POS Paper, MerchSys.POS Paper, Point of Sale Paper]
sources: [Sources/POS-Module_AcademicPaper.md]
related: [module-pos, villon-farm-supply, utang-credit-system, bir-compliance, iso-25010-2023, utaut2, descriptive-developmental-design, tacurong-city]
last-updated: 2026-05-02
---

# POS Module Paper — Source Summary

**Raw source:** `Sources/POS-Module_AcademicPaper.md` (214 lines)

## Executive Summary

Academic paper (Chapters 1–3) for the MerchSys.POS module. Documents five sales transaction problems (S1–S5) at [[villon-farm-supply|Villon Farm Supply]]. Proposes a cart-based digital POS with multi-payment support, [[utang-credit-system|credit (utang) management]] with hard blocking, [[bir-compliance|BIR-compliant OR generation]], and automated daily sales summaries. Located in [[tacurong-city|Tacurong City]], Sultan Kudarat.

## Problems Addressed

| ID | Problem | Impact |
|---|---|---|
| S1 | Sales recorded in manual receipt book — no digital record | Unverifiable transactions |
| S2 | Credit/utang tracked informally — balances lost or disputed | Uncollected receivables |
| S3 | No official receipt numbering system for BIR | Compliance risk |
| S4 | No mechanism to block repeat credit for unpaid customers | Stacking bad debt |
| S5 | No automated daily sales summary | Decision-making without data |

## Core Features

1. **Cart-Based Transaction Interface** — product, qty, price, discount, payment method, customer
2. **Multi-Payment Support** — Cash, GCash/e-wallet, Bank Transfer, Credit/Utang
3. **Official Receipt Generation** — OR-YYYY-XXXX format, BIR-compliant, auto-sequential
4. **Credit Customer Management** — digital accounts, running balances, payment history
5. **Credit Blocking** — hard block when outstanding balance > 0 (non-negotiable)
6. **Discount Management** — bulk buyer and regular customer discounts with authorization
7. **Sales Return & Exchange** — reason captured, inventory restocked, original TX linked
8. **VAT-Ready Structure** — configurable for both VAT (12%) and non-VAT
9. **Daily Sales Summary** — total sales, TX count, payment method breakdown
10. **AR Collection Flags** — overdue credit accounts flagged for follow-up

## Research Design

- **Type:** [[descriptive-developmental-design|Descriptive-Developmental]]
- **Respondents:** Manager + Owner (purposive sampling, n=2)
- **Locale:** [[tacurong-city|Tacurong City]], Sultan Kudarat
- **Evaluation:** [[iso-25010-2023|ISO 25010:2023]] (8 characteristics) + [[utaut2|UTAUT2]] (User Acceptance, Ease of Use/Usefulness)

## Key Definitions

- **Utang** = Philippine informal credit where farmers buy on credit, pay after harvest
- **Credit Blocking** = system enforces zero-tolerance on stacking unpaid credit
- **OR-YYYY-XXXX** = Official Receipt format per BIR requirements
- **VAT-Ready** = accommodates both VAT-registered (12%) and non-VAT scenarios

## Delimitations

- Excludes Purchasing, Inventory, Accounting modules
- No online/e-commerce or mobile POS
- No external payment gateway integration (records only)
- No actual electronic fund transfers — recording only
- Credit blocking is strictly enforced (no partial credit)

## Literature Highlights

- POS software market valued at USD 10.07B (Grand View Research, 2023)
- 60–70% of rural agricultural retail involves credit/utang (Llanto & Badiola, 2022)
- Automated credit blocking reduces bad debt without harming relationships (Mendoza & Cruz, 2024)
- POS adoption barriers: cost, tech literacy, "manual is sufficient" perception (Bauzon, 2025)
- Manual AR ledgers have 15–20% discrepancy vs actual (Garcia & Santos, 2023)

## Source Citations

All content from `Sources/POS-Module_AcademicPaper.md`.
