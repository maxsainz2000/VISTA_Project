---
type: analysis
title: "Technology Stack Reference"
aliases: [tech stack, dependencies, NuGet packages]
sources: [Sources/system_plan.md, Sources/system_plan_amendment_2026-05-28.md]
related: [client-server-wpf, modular-monolith, centralized-database-architecture]
last-updated: 2026-05-28
---

# Technology Stack Reference

> Authoritative as of 2026-05-28. SQLite is **removed** per [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]].

## Core Stack

| Component | Technology | Version | Purpose |
|---|---|---|---|
| Language | VB.NET | 10 | Application logic |
| IDE | Visual Studio | 2026 | Development environment |
| UI Framework | WPF | .NET 10 | Desktop UI with XAML |
| MVVM Toolkit | CommunityToolkit.Mvvm | Latest | ViewModel base, commands, messaging |
| ORM | Entity Framework Core | 10 | Database access |
| MySQL Provider | Pomelo.EntityFrameworkCore.MySql | Latest stable for EF Core 10 | MariaDB connector |
| Mediator | MediatR | Latest | Cross-module event/query bus |
| Database | MariaDB | 11.4.x LTS | Single centralized data store (XAMPP) |
| Notifications | ToastNotifications | Latest | Desktop toast alerts |

**Removed (superseded):** `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.Data.Sqlite`, all sync infrastructure components. See [[offline-first-sync]] (deprecated) and [[centralized-database-architecture]] for the new model.

## Deployment

| Aspect | Detail |
|---|---|
| Target OS | Windows 10/11 |
| Package | Single `.exe` per client laptop |
| Hardware | Standard Windows desktop on LAN |
| Clients (test target) | 4 concurrent client laptops |
| Clients (production) | 2 users (Manager, Owner) |
| Server | One host laptop running XAMPP (MariaDB instance), UPS-backed |
| Network | LAN, TCP 3306; wired Ethernet preferred on host |
| Internet | Not required |

## Database Design Principles

- 3rd Normal Form (3NF)
- [[fifo-costing|FIFO]] batch-level records
- Audit columns on every table (CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)
- Soft deletes (IsDeleted flag)
- One `DbContext` per module, all bound to the same MariaDB connection string
- **Optimistic concurrency tokens** (`TIMESTAMP(6) ON UPDATE` or `RowVersion`) on all mutable rows
- **`SELECT ... FOR UPDATE`** on FIFO decrement transactions to serialize concurrent client writes

## Source References

- [[system-plan|System Plan]] — original technology stack (historical for local-DB row)
- [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]] — current authoritative stack
- [[centralized-database-architecture|Centralized Database Architecture]] — concurrency strategy
