---
type: concept
title: "FIFO Costing"
aliases: [First-In First-Out, FIFO, batch-level costing]
sources: [Sources/system_plan.md, Sources/Inventory-Module_AcademicPaper.md, Sources/Accounting-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, module-inventory, module-accounting, expiry-date-tracking]
last-updated: 2026-06-01
---

# FIFO Costing (First-In, First-Out)

## Definition

An inventory costing method where the cost of goods sold is based on the purchase price of the **oldest** units in stock. The remaining inventory is valued at the cost of the most recently acquired units.

## Why FIFO for VISTA

1. **Physical flow alignment** — older stock should sell first (perishable goods)
2. **Expiry synergy** — FIFO encourages selling oldest batches, complementing [[expiry-date-tracking|expiry tracking]]
3. **Balance sheet accuracy** — ending inventory approximates current replacement costs
4. **Client confirmed** — FIFO selected by business owner

## Batch-Level Implementation

Each purchase batch records:

| Field | Description |
|---|---|
| Batch ID | Unique identifier |
| Product ID | FK to product catalog |
| Qty Received | Units in this batch |
| Qty Remaining | Units not yet sold/consumed |
| Unit Cost | Purchase price at time of receipt |
| Receipt Date | When goods were received |
| Expiry Date | Nullable — for perishable products |

**On sale:** system deducts from the batch with the **oldest receipt date** that has `Qty Remaining > 0`. COGS = units × that batch's unit cost.

## Cross-Module Impact

- **Inventory** → calculates live valuation (Σ of remaining qty × batch cost)
- **Accounting** → calculates COGS for Income Statement
- **Purchasing** → records batch cost at goods receipt

## Operational Validation

The populated manager interview (`Sources/Villon_Interview_Populated.md`) validates the choice of FIFO costing:
- **Costing Selection (Q13):** When asked about inventory valuation and COGS when products are bought at different prices across deliveries, the manager explicitly selected **FIFO (First In, First Out)**, confirming that the oldest batch's price is used first.

## Source References

- [[wiki/sources/system-plan|System Plan]] — "FIFO batch-level records"
- [[wiki/sources/inventory-module-paper|Inventory Paper]] — FIFO definition, batch-level detail
- [[wiki/sources/accounting-module-paper|Accounting Paper]] — FIFO-based COGS calculation
- [[wiki/sources/villon-interview-populated|Villon Interview]] — operational validation of FIFO costing choice

