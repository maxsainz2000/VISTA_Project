---
type: entity
title: "Inventory Module"
aliases: [MerchSys.Inventory, Inventory]
sources: [Sources/system_plan.md, Sources/Inventory-Module_AcademicPaper.md]
related: [villon-farm-supply, module-purchasing, module-pos, fifo-costing, expiry-date-tracking]
last-updated: 2026-05-02
---

# Inventory Module (MerchSys.Inventory)

The **central hub** of VISTA. Receives stock data from Purchasing and passes deduction data to POS. All stock visibility flows through this module.

## Problems → Features

| Problem | Feature |
|---|---|
| I1 — Manual ledger only | Real-time digital stock records |
| I2 — Weekly count 1–3 hrs | Auto-updated on every receive/sale |
| I3 — No dashboard | Real-Time Stock Dashboard |
| I4 — No expiry tracking | [[expiry-date-tracking\|Batch-level Expiry Monitoring]] |
| I5 — Unknown inventory value | Live [[fifo-costing\|FIFO]] Valuation |
| I6 — Demand-spike stockouts | Predictive Stockout Estimation |
| I7 — No fast/slow product view | Product Velocity Classification |

## Data Flow

```
Purchasing Module → [Goods Received] → Inventory (stock +)
POS Module → [Sale Completed] → Inventory (stock −)
Inventory → Accounting Module (valuation, COGS data)
```

## User Roles

- **Manager:** Full access — view dashboard, record shrinkage, manage thresholds
- **Owner:** Read-only — view stock levels, valuations

## Namespace

`MerchSys.Inventory`

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture, problem list
- [[wiki/sources/inventory-module-paper|Inventory Paper]] — detailed requirements, FIFO costing, RRL
