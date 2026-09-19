---
type: concept
title: "MediatR Mediator Pattern"
aliases: [MediatR, mediator pattern, event-driven mediator]
sources: [Sources/system_plan.md, Sources/Accounting-Module_AcademicPaper.md]
related: [modular-monolith, module-purchasing, module-inventory, module-pos, module-accounting]
last-updated: 2026-05-02
---

# MediatR Mediator Pattern

## Definition

MediatR is a .NET library that implements the mediator pattern — modules send requests/notifications through a central dispatcher rather than referencing each other directly. This enforces module isolation in the [[modular-monolith|Modular Monolith]].

## VISTA Usage

| Flow | Event | Source | Consumer |
|---|---|---|---|
| Stock In | `GoodsReceivedEvent` | Purchasing | Inventory |
| Stock Out | `SaleCompletedEvent` | POS | Inventory |
| Revenue | `SaleCompletedEvent` | POS | Accounting |
| AP Created | `GoodsReceivedEvent` | Purchasing | Accounting |
| COGS Data | `InventoryValuationQuery` | Accounting | Inventory |

## Rules

1. **No direct class references** across module boundaries
2. **No shared EF DbContext** — each module owns its data
3. Events are fire-and-forget notifications; queries are request/response
4. All cross-module data flows through MediatR — no exceptions

## NuGet Package

`MediatR` (latest stable)

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture
- [[wiki/sources/accounting-module-paper|Accounting Paper]] — "mediator-based architecture (MediatR)"
