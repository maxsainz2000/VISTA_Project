---
type: source-summary
title: "Inventory Module Academic Paper"
aliases: [Inventory Paper, MerchSys.Inventory Paper]
sources: [Sources/Inventory-Module_AcademicPaper.md]
related: [module-inventory, villon-farm-supply, fifo-costing, expiry-date-tracking, iso-25010-2023, utaut2, descriptive-developmental-design]
last-updated: 2026-05-02
---

# Inventory Module Paper — Source Summary

**Raw source:** `Sources/Inventory-Module_AcademicPaper.md` (218 lines)

## Executive Summary

Academic paper (Chapters 1–3) for the MerchSys.Inventory module. Documents seven inventory problems (I1–I7) at [[villon-farm-supply|Villon Farm Supply]], proposes a real-time stock dashboard with FIFO costing, expiry tracking, low-stock alerts, and predictive stockout estimation. The Inventory Module is the **central hub** — receives data from Purchasing (goods received) and feeds data to POS (stock deductions).

## Problems Addressed

| ID | Problem | Impact |
|---|---|---|
| I1 | Stock tracked only in physical record book | Data lag, transcription errors |
| I2 | Weekly full count takes 1–3 hours | Operational downtime |
| I3 | No real-time dashboard | Manager operates with delayed info |
| I4 | Expiry dates not tracked systematically | Expired goods sold or written off |
| I5 | Total inventory value unknown | No basis for financial planning |
| I6 | Stockouts from demand spikes | Lost revenue, customer defection |
| I7 | No fast/slow-moving product visibility | Suboptimal ordering |

## Core Features

1. **Real-Time Stock Dashboard** — all products, current qty, low-stock status, inflow/outflow, total value
2. **Automated Low-Stock Alerts** — triggers at manager-defined minimum threshold
3. **[[expiry-date-tracking|Expiry Date Tracking]]** — batch-level for pesticides, seeds, feeds
4. **[[fifo-costing|FIFO Costing]]** — oldest batch cost consumed first per sale
5. **Inventory Valuation** — real-time total monetary value using FIFO
6. **Shrinkage Recording** — damage, expiry write-off, admin discrepancy with financial impact
7. **Predictive Stockout Estimate** — days-until-stockout based on avg daily velocity

## Research Design

- **Type:** [[descriptive-developmental-design|Descriptive-Developmental]]
- **Respondents:** Manager + Owner (purposive sampling, n=2)
- **Evaluation:** [[iso-25010-2023|ISO 25010:2023]] + [[utaut2|UTAUT2]]

## Key Definitions

- **Stock on Hand** = auto-updated on every receive and sale
- **Minimum Threshold** = manager-set trigger for low-stock alert
- **Batch-Level Records** = qty, unit cost, receipt date, expiry date per purchase batch
- **Shrinkage** = inventory loss from theft, damage, spoilage, or admin errors

## Delimitations

- Excludes Purchasing, POS, Accounting modules (but interfaces with them)
- No barcode/RFID scanning
- No mobile/web deployment
- Single-business evaluation (n=2)

## Literature Highlights

- Digital inventory → 25% reduction in stockouts, 30% improvement in accuracy (Muller, 2024)
- Automated alerts reduce stockout rates by up to 40% (Syntetos et al., 2022)
- Predictive estimation → 15% service level improvement (Wang & Hu, 2023)
- FIFO aligns physical flow with cost flow for perishable goods (Horngren et al., 2021)

## Source Citations

All content from `Sources/Inventory-Module_AcademicPaper.md`.
