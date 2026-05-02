---
type: concept
title: "Client-Server WPF Architecture"
aliases: [WPF desktop, client-server, desktop application]
sources: [Sources/system_plan.md]
related: [modular-monolith, offline-first-sync]
last-updated: 2026-05-02
---

# Client-Server WPF Architecture

## Definition

VISTA is a Windows Presentation Foundation (WPF) desktop application built as a client-server system per professor requirement. The client runs on a standard Windows desktop; the server is a MariaDB instance.

## Stack

| Layer | Technology |
|---|---|
| UI | WPF (XAML) |
| MVVM | CommunityToolkit.Mvvm |
| Language | VB.NET 10 |
| ORM | Entity Framework Core 10 |
| Local DB | SQLite |
| Central DB | MariaDB 11.4.x LTS (XAMPP) |
| Deployment | Single `.exe` — no installer complexity |

## Constraints

- No mobile or web deployment
- No cloud services or internet dependency for core operations
- Standard Windows desktop hardware — no enterprise-grade requirements
- [[offline-first-sync|Offline-first]] with sync when connection is stable

## Source References

- [[wiki/sources/system-plan|System Plan]] — technology stack, deployment
