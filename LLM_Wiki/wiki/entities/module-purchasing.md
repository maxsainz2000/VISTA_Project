---
type: entity
title: "Purchasing Module"
aliases: [MerchSys.Purchasing, Purchasing]
sources: [Sources/system_plan.md, Sources/Purchasing-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, villon-farm-supply, module-inventory, fifo-costing, reorder-suggestion-engine]
last-updated: 2026-06-01
---

# Purchasing Module (MerchSys.Purchasing)

Manages the upstream supply chain — from reorder decision through goods receipt and AP payment.

## Problems → Features

| Problem | Feature |
|---|---|
| P1 — Gut-feel reorders | [[reorder-suggestion-engine\|Reorder Suggestion Engine]] |
| P2 — No PO audit trail | PO Lifecycle (Draft → Submitted → Received → Verified) |
| P3 — Manual retail price updates | Price Change Detection & Alerts |
| P4 — No AP ledger | AP Tracking with due dates |
| P5 — Price volatility → bad COGS | Batch-level cost recording at receipt |
| P6 — No seasonal demand model | Seasonal flags in reorder engine |

## Data Flow

```
Supplier → [PO Created] → [Goods Received] → Inventory Module (stock updated)
                                             → Accounting Module (AP created)
```

## User Roles

- **Manager:** Full access — create POs, receive goods, manage vendors, manage AP
- **Owner:** Read-only — view PO history, AP balances

## Namespace

`MerchSys.Purchasing`

## Operational Validations

The populated manager interview (`Sources/Villon_Interview_Populated.md`) validates these purchasing features:
- **Reorder Suggestion Engine (P1):** Confirms that prior to VISTA, reordering was strictly based on gut feel and visual shelf checks, leading to stockouts.
- **Supplier Management & Volatility (P3, P5):** High supplier price volatility was confirmed as the manager's primary frustration. When a supplier changes a price, retail prices must currently be adjusted manually.
- **Accounts Payable (P4):** Confirms credit tracking is managed informally based on the schedules of supplier visits rather than a formal ledger.
- **Seasonal Flags (P6):** Purchasing activity spikes significantly during the palay (rice) planting and growing seasons, particularly for agricultural chemicals, seeds, and fertilizers.

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture, problem list
- [[wiki/sources/purchasing-module-paper|Purchasing Paper]] — detailed requirements, RRL, methodology
- [[wiki/sources/villon-interview-populated|Villon Interview]] — operational validation of P1–P6 problems and workflows

