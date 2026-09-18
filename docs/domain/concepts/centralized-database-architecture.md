---
type: concept
title: "Centralized Database Architecture"
aliases: [pure client-server, MariaDB-only, centralized DB, no-SQLite architecture]
sources: [Sources/system_plan_amendment_2026-05-28.md]
related: [client-server-Windows Forms, modular-monolith, offline-first-sync, system-plan-amendment-2026-05-28]
last-updated: 2026-05-28
---

# Centralized Database Architecture

> **Authoritative as of 2026-05-28.** Supersedes [[offline-first-sync|Offline-First Sync]] per [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]].

## Definition

VISTA is deployed as a pure client-server Windows Forms system. Every client laptop connects directly, over TCP, to a single centralized **MariaDB 11.4.x LTS** instance hosted via XAMPP on a designated host laptop on the local network. No client maintains a local database; there is no sync layer.

## Topology

```
[Laptop 1 — Client]
[Laptop 2 — Client]  ─┐
[Laptop 3 — Client]   ├──► MariaDB 11.4.x (XAMPP) on Host Laptop
[Laptop 4 — Client]  ─┘     LAN, TCP 3306
```

Four client laptops are the target deployment for testing — exercising true multi-client concurrency even though production has only two users (Manager, Owner).

## Connection Model

- Each client opens an EF Core `DbContext` per unit of work (request-scoped in the Windows Forms DI container).
- Connection string read from `appsettings.json`, parameterized by host IP/hostname.
- All writes commit to MariaDB synchronously.

## Concurrency Control

- Optimistic concurrency tokens (`TIMESTAMP(6) ON UPDATE` or equivalent `RowVersion`) on mutable rows: `Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`, `Pos_CreditAccounts.OutstandingBalance`.
- Inventory decrement paths (POS sale, shrinkage, return) execute inside a single MariaDB transaction:
  1. `SELECT ... FOR UPDATE` on the FIFO batch row(s)
  2. Decrement `QuantityRemaining`
  3. Insert `Inv_StockMovements`, `Inv_SaleCogs`
  4. Commit
- This serializes concurrent sales of the same product across all four clients without application-level coordination.
- On `DbUpdateConcurrencyException`, the affected ViewModel surfaces a "Data changed elsewhere — refresh and retry" notification and reloads the aggregate. **No silent overwrites.**

## Connectivity Requirement

- Loss of connectivity to the MariaDB host **stops the affected client.** This is the explicit, intentional trade.
- Shell header shows ConnectionStatus indicator: Online / Reconnecting / Offline.
- On `Offline`, all command buttons disable.
- On `Reconnecting`, exponential backoff up to 5 retries; then requires manual retry.

## Removed Components (Supersession)

The following from the previous [[offline-first-sync]] design are removed:

- All `SQLite` packages and providers
- `Sync_Journal` table and per-module variants
- `SyncOrchestrator`, `SyncWorker`, `MariaDbSyncTransmitter`, `MariaDbSyncContext`
- `ISyncableRepository`, `SyncableRepositoryCore`, all `*SyncMap` classes
- `ConflictResolver`, `ConflictResolution`, `SyncAction`, `LastWriteWins`
- `NetworkAvailabilityChanged` + TCP-probe dual-condition check
- `SyncStatusIndicator` (replaced by `ConnectionStatusIndicator`)
- `DatabaseInitializer` SQLite migration runner

## Operational Requirements

- **UPS on host laptop** — hard requirement, elevated from recommendation.
- **Nightly `mysqldump`** of `merchsys_central` to a secondary machine via Windows Task Scheduler.
- **Wired Ethernet** preferred over Wi-Fi for the host laptop.
- **Failover runbook**: bring MariaDB up on a backup laptop, update each client's `appsettings.json`, restart clients. Target RTO: 30 minutes.

## Trade-offs

| Property | Centralized (this) | Offline-First SQLite (superseded) |
|---|---|---|
| Operation when host offline | All clients stop | Each client continues fully |
| Multi-client stock accuracy | Serialized via DB transactions | Silent divergence possible |
| Cross-client read latency | Real-time | Up to 30 s (sync probe interval) |
| Code complexity | Low (standard EF Core) | High (sync layer) |
| BIR audit traceability | Single source of truth | Distributed across N SQLite files |

## Rationale

Availability under host-laptop failure is traded for correctness under concurrent multi-client writes. For a single-location shop on a LAN with a UPS-backed host, the new failure mode (host down → operations stop) is operationally acceptable; the previous failure mode (silent stock divergence) was not.

## Source References

- [[system-plan-amendment-2026-05-28|System Plan Amendment 2026-05-28]] — full amendment text
- [[system-plan|System Plan]] — original baseline (historical for §5.3 and §12)
