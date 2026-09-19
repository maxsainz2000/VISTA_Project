---
type: concept
title: "Client-Server Windows Forms Architecture"
aliases: [Windows Forms desktop, client-server, desktop application]
sources: [Sources/system_plan.md, Sources/system_plan_amendment_2026-05-28.md]
related: [modular-monolith, centralized-database-architecture]
last-updated: 2026-05-28
---

# Client-Server Windows Forms Architecture

## Definition

VISTA is a Windows Forms desktop application built as a pure client-server system per professor requirement. Each client laptop runs the same Windows Forms `.exe` and connects directly to a single centralized **MariaDB 11.4.x LTS** instance hosted via XAMPP on a designated host laptop on the local network.

Production runs two human users (Manager, Owner). The deployment target for testing is **four concurrent client laptops** on the LAN, exercising true multi-client concurrency.

## Stack

| Layer | Technology |
|---|---|
| UI | Windows Forms (WinForms Designer) |
| MVP | CommunityToolkit.MVP |
| Language | VB.NET 10 |
| ORM | Entity Framework Core 10 (Pomelo MySQL provider) |
| Database | MariaDB 11.4.x LTS (XAMPP, single centralized instance) |
| Deployment | Single `.exe` per client — no installer complexity |

There is **no local database**. There is **no sync layer**. See [[centralized-database-architecture|Centralized Database Architecture]] for the full data-access model and concurrency strategy.

## Constraints

- No mobile or web deployment
- No cloud services or internet dependency
- Standard Windows desktop hardware on a LAN
- Host laptop must be UPS-backed
- Loss of host connectivity stops the affected client (explicit trade for cross-client write correctness — see [[centralized-database-architecture]])

## History

The original [[system-plan|System Plan]] §5.3 specified [[offline-first-sync|offline-first SQLite + sync to MariaDB]]. That model was superseded on 2026-05-28 by [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]] when the deployment target expanded to four concurrent clients and offline-first became incompatible with multi-client stock correctness.

## Source References

- [[system-plan|System Plan]] — original technology stack (historical for §5.3)
- [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]] — current authoritative architecture
- [[centralized-database-architecture|Centralized Database Architecture]] — data access and concurrency
