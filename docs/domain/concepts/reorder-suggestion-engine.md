---
type: concept
title: "Reorder Suggestion Engine"
aliases: [reorder engine, reorder point, auto-reorder]
sources: [Sources/Purchasing-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, module-purchasing, module-inventory]
last-updated: 2026-06-01
---

# Reorder Suggestion Engine

## Definition

An automated system that suggests when and how much to reorder based on quantitative data rather than gut feel (Problem P1).

## Formula

```
Reorder Point = (Avg Daily Demand × Lead Time) + Safety Stock
```

## Inputs

| Input | Source |
|---|---|
| Avg Daily Demand | Calculated from POS sales history |
| Lead Time | Days from PO placement to goods receipt (per vendor) |
| Safety Stock | Buffer for demand variability |
| Current Stock | From [[module-inventory\|Inventory Module]] |
| Seasonal Flags | Manual tags for peak-demand periods |

## Behavior

1. System monitors `Current Stock` vs `Reorder Point` continuously
2. When `Current Stock ≤ Reorder Point`, a suggestion is generated
3. Suggestion includes: product, suggested qty, preferred vendor, estimated lead time
4. Manager reviews and creates PO — system does not auto-order

## Delimitations

- Statistical methods only — no ML/AI forecasting
- Simple statistical models outperform gut-feel even with limited data (Syntetos et al., 2016)

## Operational Validation

The populated manager interview (`Sources/Villon_Interview_Populated.md`) validates these reorder engine parameters:
- **Gut Feel vs Thresholds (Q1, Q15):** The manager confirmed that pre-system reordering is based entirely on gut feel and visual checks, but they do have intuitive minimum stock levels in mind, validating the transition to system-defined thresholds.
- **Supplier Lead Time (Q15):** Supplier deliveries typically arrive "within a few days," which establishes the baseline `Lead Time` parameter.
- **Seasonal Demand Inputs (Q8, Q16):** Confirms peak seasons align with the **Palay (rice) cropping calendar** (planting/growing cycles), causing major demand spikes for fertilizers, seeds, and pesticides.
- **Reorder Warnings (Q28):** The manager identified a low-stock alert for fast-moving items (e.g. UNO animal feeds) as the single most valuable warning for sales and purchasing operations.

## Source References

- [[wiki/sources/purchasing-module-paper|Purchasing Paper]] — P1, reorder logic, seasonal flags
- [[wiki/sources/villon-interview-populated|Villon Interview]] — operational validation of lead times, demand cycles, and alerts

