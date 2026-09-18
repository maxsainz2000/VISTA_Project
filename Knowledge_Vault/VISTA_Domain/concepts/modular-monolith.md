---
type: concept
title: "Modular Monolith"
aliases: [modular monolith pattern]
sources: [Sources/system_plan.md]
related: [mediatr-mediator, module-purchasing, module-inventory, module-pos, module-accounting]
last-updated: 2026-05-02
---

# Modular Monolith

## Definition

An architecture where the application ships as a single executable but internally is organized into independent class libraries (modules) with enforced boundaries. Modules communicate only through a [[mediatr-mediator|mediator]] — no direct cross-module data access.

## VISTA Implementation

| Aspect | Detail |
|---|---|
| Deployment | Single `.exe` |
| Modules | 4 class libraries: Purchasing, Inventory, POS, Accounting |
| Communication | [[mediatr-mediator|MediatR]] — event-driven, loosely coupled |
| DB Contexts | One per module — no cross-module EF navigation |
| Future path | Interfaces designed for potential microservice extraction |

## Why This Pattern?

- Professor requirement: client-server Windows Forms desktop
- Small business context: one machine, one install
- Maintainability: modules can be developed and tested independently
- Scalability path: can extract to services later without rewriting

## Source References

- [[wiki/sources/system-plan|System Plan]] — architecture section
