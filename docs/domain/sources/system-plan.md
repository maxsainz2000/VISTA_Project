---
type: source-summary
title: "System Plan"
aliases: [VISTA System Plan, system_plan.md]
sources: [Sources/system_plan.md]
related: [villon-farm-supply, module-purchasing, module-inventory, module-pos, module-accounting, modular-monolith, centralized-database-architecture, offline-first-sync, owasp-da-top10, client-server-Windows Forms, system-plan-amendment-2026-05-28]
last-updated: 2026-05-28
---

# System Plan — Source Summary

> **⚠️ Partially superseded by [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]].**
> §5.3 (Offline-First & Sync), §5.4 (Local DB row), §10 "Data Sync Conflict" row, §11 "Data Sync Conflict" mitigation, and the §12 offline-first bullet are **no longer authoritative**. SQLite and the sync layer have been removed; the system now uses pure client-server against centralized MariaDB. See [[centralized-database-architecture|Centralized Database Architecture]].
> All other sections remain in force.

**Raw source:** `Sources/system_plan.md` (375 lines)

## Executive Summary

The master specification for VISTA — a Windows Forms client-server desktop application built in VB.NET 10 for Villon Farm Supply. Defines the modular monolith architecture (4 class libraries), the full tech stack, all 25 problems across 4 modules, security requirements (OWASP DA Top 10), database design principles, and risk mitigation strategies.

## Business Profile

| Field | Value |
|---|---|
| Business | Villon Farm Supply — agricultural supply and trade |
| Products | Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds (~50 SKUs) |
| Users | Manager (full control), Owner (read-only KPIs) |
| BIR Status | Registered — issues Official Receipts; VAT unconfirmed |
| Peak Day | Sunday (local market day) |
| Peak Season | Palay planting and growing season |

## Problems Identified (25 total)

| Module | IDs | Count | Key Themes |
|---|---|---|---|
| [[module-purchasing\|Purchasing]] | P1–P6 | 6 | Gut-feel reorders, no audit trail, manual price updates, no AP ledger |
| [[module-inventory\|Inventory]] | I1–I7 | 7 | Manual ledger, no dashboard, no expiry tracking, unknown valuation |
| [[module-pos\|POS]] | S1–S5 | 5 | No digital receipts, informal credit, no BIR OR system |
| [[module-accounting\|Accounting]] | A1–A7 | 7 | Manual bookkeeping, no real-time reports, partial financial literacy |

## Architecture

- **Pattern:** [[modular-monolith|Modular Monolith]] — 4 class libraries in a single .exe
- **Communication:** [[mediatr-mediator|MediatR]] event-driven mediator — no cross-module data access
- **Data access:** [[centralized-database-architecture|Pure client-server]] against centralized MariaDB (amended 2026-05-28; supersedes original §5.3 [[offline-first-sync|offline-first SQLite + sync]])
- **Security:** [[owasp-da-top10|OWASP DA Top 10 (2021)]] — all 10 requirements addressed
- **Database:** 3NF, [[fifo-costing|FIFO]] batch-level records, audit columns, soft deletes, optimistic concurrency tokens on mutable rows

## Technology Stack

| Component | Choice |
|---|---|
| Language | VB.NET 10 |
| IDE | Visual Studio 2026 |
| UI | Windows Forms |
| MVP | CommunityToolkit.MVP |
| ORM | Entity Framework Core 10 |
| Mediator | MediatR |
| ~~Local DB~~ | ~~SQLite~~ — **removed 2026-05-28** |
| Database | MariaDB 11.4.x LTS (XAMPP) — single centralized instance |
| Notifications | ToastNotifications NuGet |
| Deployment | Single .exe |

## Key Constraints

- Client-server Windows Forms per professor requirement
- ~~Offline-first with dual-condition sync~~ — **superseded 2026-05-28**: pure client-server against centralized MariaDB; host laptop UPS-backed; loss of host stops affected client
- FIFO costing confirmed by client
- All reports must include plain-language interpretation
- Owner = strictly read-only at data-access layer (not just UI)
- Module interfaces designed for future microservice extraction

## Risk Register Highlights

| Risk | Likelihood | Impact |
|---|---|---|
| Price Volatility & Costing Error | High | High |
| Host Laptop Failure (replaces "Data Loss / SQLite corruption") | Medium | High |
| BIR Compliance Gap | Low | High |
| Concurrent Update Conflict (replaces "Data Sync Conflict") | Medium | Low (handled by retry UX) |

See [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]] for the full amended risk set.

## Source Citations

All content from `Sources/system_plan.md`. No external sources.
