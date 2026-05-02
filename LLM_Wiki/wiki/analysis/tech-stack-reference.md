---
type: analysis
title: "Technology Stack Reference"
aliases: [tech stack, dependencies, NuGet packages]
sources: [Sources/system_plan.md]
related: [client-server-wpf, modular-monolith, offline-first-sync]
last-updated: 2026-05-02
---

# Technology Stack Reference

## Core Stack

| Component | Technology | Version | Purpose |
|---|---|---|---|
| Language | VB.NET | 10 | Application logic |
| IDE | Visual Studio | 2026 | Development environment |
| UI Framework | WPF | .NET 10 | Desktop UI with XAML |
| MVVM Toolkit | CommunityToolkit.Mvvm | Latest | ViewModel base, commands, messaging |
| ORM | Entity Framework Core | 10 | Database access, migrations |
| Mediator | MediatR | Latest | Cross-module event/query bus |
| Local DB | SQLite | Latest | Offline-first local storage |
| Central DB | MariaDB | 11.4.x LTS | Central data store (XAMPP) |
| Notifications | ToastNotifications | Latest | Desktop toast alerts |

## Deployment

| Aspect | Detail |
|---|---|
| Target OS | Windows 10/11 |
| Package | Single `.exe` |
| Hardware | Standard Windows desktop |
| Internet | Not required for core operations |
| Server | XAMPP (MariaDB instance) |

## Database Design Principles

- 3rd Normal Form (3NF)
- [[fifo-costing|FIFO]] batch-level records
- Audit columns on every table (CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)
- Soft deletes (IsDeleted flag)
- One DbContext per module

## Source References

- [[wiki/sources/system-plan|System Plan]] — technology stack, database design
