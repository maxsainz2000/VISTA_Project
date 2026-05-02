---
type: analysis
title: "Cross-Module Data Flow"
aliases: [data flow, module integration, event flow]
sources: [Sources/system_plan.md, Sources/Inventory-Module_AcademicPaper.md, Sources/POS-Module_AcademicPaper.md, Sources/Accounting-Module_AcademicPaper.md]
related: [mediatr-mediator, modular-monolith, module-purchasing, module-inventory, module-pos, module-accounting]
last-updated: 2026-05-02
---

# Cross-Module Data Flow

## Overview

All inter-module communication flows through [[mediatr-mediator|MediatR]]. No module directly references another's classes or DbContext.

## Flow Diagram

```
┌─────────────┐    GoodsReceived     ┌─────────────┐
│  Purchasing  │ ──────────────────►  │  Inventory   │
│  (P1–P6)    │                      │  (I1–I7)    │
└─────────────┘                      └──────┬──────┘
       │                                     │
       │ AP Created                          │ SaleCompleted
       │                                     │ (stock −)
       ▼                                     ▼
┌─────────────┐                      ┌─────────────┐
│  Accounting  │ ◄───────────────── │    POS       │
│  (A1–A7)    │    Revenue, AR      │  (S1–S5)    │
└─────────────┘                      └─────────────┘
       ▲                                     
       │ COGS, Valuation                     
       │                                     
       └──────── Inventory ──────────────────┘
```

## Event Catalog

| Event | Source | Consumer(s) | Payload |
|---|---|---|---|
| `GoodsReceivedEvent` | Purchasing | Inventory, Accounting | PO ID, items, qtys, costs, expiry |
| `SaleCompletedEvent` | POS | Inventory, Accounting | TX ID, items, qtys, payment method |
| `CreditPaymentEvent` | POS | Accounting | Customer ID, amount, date |
| `ShrinkageRecordedEvent` | Inventory | Accounting | Product, qty, reason, value |

## Key Rules

1. **Inventory is the central hub** — both Purchasing and POS flow through it
2. **Accounting aggregates all** — reads from all 3 operational modules
3. **No direct data access** — all flows via MediatR events/queries
4. **Batch-level data** persists across the chain (receipt → stock → COGS)

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture
- [[wiki/sources/inventory-module-paper|Inventory Paper]] — "central hub"
- [[wiki/sources/accounting-module-paper|Accounting Paper]] — "aggregates data from all three"
