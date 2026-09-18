---
type: concept
title: "Offline-First Sync (SUPERSEDED)"
aliases: [offline-first, SQLite-MariaDB sync, dual-condition sync]
sources: [Sources/system_plan.md, Sources/system_plan_amendment_2026-05-28.md]
related: [centralized-database-architecture, modular-monolith, client-server-Windows Forms]
last-updated: 2026-05-28
status: superseded
superseded-by: centralized-database-architecture
superseded-on: 2026-05-28
---

# Offline-First Sync — SUPERSEDED

> **⚠️ This concept is superseded as of 2026-05-28.**
> See [[centralized-database-architecture|Centralized Database Architecture]] and [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]].
>
> SQLite and the sync layer have been removed from the architecture in favor of pure client-server operation against a single centralized MariaDB instance. This page is retained only as a historical record of the original design.

---

## Historical Definition (Original §5.3 of System Plan)

The application operated fully on a local SQLite database without requiring network connectivity. When a stable connection was confirmed, data synced to a centralized MariaDB instance.

## Historical Dual-Condition Sync

Both conditions had to be true before sync initiated:

1. **Network reachable** — server responds to health check
2. **Stable connection** — no packet loss over N-second window

If either condition failed, the app continued in offline mode with no degradation.

## Historical Architecture

```
[Windows Forms Client] → SQLite (local, always available)
                  ↕ (sync when dual-condition met)
              MariaDB 11.4.x LTS (central, XAMPP)
```

## Why It Was Superseded

The original design assumed a single-workstation deployment. The project expanded to four concurrent client laptops sharing one MariaDB host. In that topology, offline-first SQLite introduced:

- Stale dashboards (push-only sync gave no cross-client read consistency)
- Silent stock divergence under concurrent `LastWriteWins` UPDATEs
- An untestable multi-client concurrency property (the very thing the four-laptop test was meant to validate)

See the [[system-plan-amendment-2026-05-28|amendment]] for the full rationale.

## Source References

- [[system-plan|System Plan]] — original sync strategy (historical)
- [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]] — supersession authority
- [[centralized-database-architecture|Centralized Database Architecture]] — replacement concept
