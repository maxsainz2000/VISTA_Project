---
type: concept
title: "Offline-First Sync"
aliases: [offline-first, SQLite-MariaDB sync, dual-condition sync]
sources: [Sources/system_plan.md]
related: [modular-monolith, client-server-wpf]
last-updated: 2026-05-02
---

# Offline-First Sync

## Definition

The application operates fully on a local SQLite database without requiring network connectivity. When a stable connection is confirmed, data syncs to a centralized MariaDB instance.

## Dual-Condition Sync

Both conditions must be true before sync initiates:

1. **Network reachable** — server responds to health check
2. **Stable connection** — no packet loss over N-second window

If either condition fails, the app continues in offline mode with no degradation.

## Architecture

```
[WPF Client] → SQLite (local, always available)
                  ↕ (sync when dual-condition met)
              MariaDB 11.4.x LTS (central, XAMPP)
```

## Conflict Resolution

- To be defined during implementation
- System plan identifies "Data Sync Conflict" as Medium-likelihood, Medium-impact risk

## Source References

- [[wiki/sources/system-plan|System Plan]] — sync strategy, risk register
- [[wiki/sources/inventory-module-paper|Inventory Paper]] — "offline-first SQLite database that syncs to a centralized MariaDB instance"
