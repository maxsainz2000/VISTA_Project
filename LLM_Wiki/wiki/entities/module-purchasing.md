---
type: entity
title: "Purchasing Module"
aliases: [MerchSys.Purchasing, Purchasing]
sources: [Sources/system_plan.md, Sources/Purchasing-Module_AcademicPaper.md]
related: [villon-farm-supply, module-inventory, fifo-costing, reorder-suggestion-engine]
last-updated: 2026-05-02
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

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture, problem list
- [[wiki/sources/purchasing-module-paper|Purchasing Paper]] — detailed requirements, RRL, methodology
