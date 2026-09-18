---
type: entity
title: "Inventory Module"
aliases: [MerchSys.Inventory, Inventory]
sources: [Sources/system_plan.md, Sources/Inventory-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, villon-farm-supply, module-purchasing, module-pos, fifo-costing, expiry-date-tracking]
last-updated: 2026-06-01
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

## Operational Validations

The populated manager interview (`Sources/Villon_Interview_Populated.md`) validates these inventory features:
- **Manual Ledger & Stock Counts (I1, I2):** Confirms pre-system stock tracking is done via a manual physical record book updated per transaction, and that physical counts are weekly, taking 1 to 3 hours.
- **Real-Time Stock Dashboard (I3):** The manager explicitly requests a single, real-time screen showing stock levels, low-stock alerts, and recent movement.
- **Expiry Date Tracking (I4):** Confirms expiry tracking is needed for pesticides/chemicals, seeds, and feeds, while fertilizers are exempt, validating shelf-stability assumptions.
- **FIFO Costing (I5):** The manager selects FIFO as the costing method to value sold items first, aligning with batch-level costing.
- **Stockout Incidents (I6):** Confirms stockouts (e.g. insecticides and feeds) occur due to unexpected high demand and supplier delays, causing lost revenue.

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture, problem list
- [[wiki/sources/inventory-module-paper|Inventory Paper]] — detailed requirements, FIFO costing, RRL
- [[wiki/sources/villon-interview-populated|Villon Interview]] — operational validation of I1–I7 problems and workflows

