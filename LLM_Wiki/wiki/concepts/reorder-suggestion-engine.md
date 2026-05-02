---
type: concept
title: "Reorder Suggestion Engine"
aliases: [reorder engine, reorder point, auto-reorder]
sources: [Sources/Purchasing-Module_AcademicPaper.md]
related: [module-purchasing, module-inventory]
last-updated: 2026-05-02
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

## Source References

- [[wiki/sources/purchasing-module-paper|Purchasing Paper]] — P1, reorder logic, seasonal flags
