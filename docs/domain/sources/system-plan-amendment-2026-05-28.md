---
type: source-summary
title: "System Plan Amendment — 2026-05-28 (Pure Client-Server)"
aliases: [AMD-2026-05-28-01, system plan amendment, sqlite removal amendment]
sources: [Sources/system_plan_amendment_2026-05-28.md]
related: [system-plan, centralized-database-architecture, client-server-Windows Forms, modular-monolith, offline-first-sync]
last-updated: 2026-05-28
---

# System Plan Amendment — 2026-05-28

**Raw source:** `Sources/system_plan_amendment_2026-05-28.md`
**Amendment ID:** AMD-2026-05-28-01
**Status:** Authoritative — supersedes named sections of the [[system-plan|System Plan]].

## What Changed

The architecture pivots from **offline-first SQLite + push-only sync to MariaDB** to **pure client-server against centralized MariaDB**. SQLite and the entire sync layer are removed from the design.

## Why

The deployment target expanded from a single Manager workstation to **four concurrent client laptops** sharing one MariaDB host on a LAN. Offline-first SQLite was protective for the single-workstation case but actively harmful in the multi-client case:

- Push-only sync provides no cross-client read consistency — Laptop B's Stock Dashboard never sees Laptop A's sale until B's local SQLite is rebuilt.
- `LastWriteWins` on concurrent `Inv_StockBatches` UPDATEs silently loses decrements.
- The four-laptop concurrency stress test cannot validate inventory accuracy with the previous architecture.

## Superseded Sections

| Original §  | Topic | Disposition |
|---|---|---|
| §5.3 | Offline-First & Sync Strategy | Removed; replaced with pure client-server model |
| §5.4 (Local DB row) | SQLite as local DB | Removed |
| §10 ("Data Sync Conflict" row) | Sync conflict risk | Removed (no sync to conflict) |
| §11 ("Data Sync Conflict" mitigation) | Dual-condition probe + LWW policy | Removed |
| §12 ("Offline-first…" bullet) | Offline-first constraint | Removed; replaced with pure client-server constraint |

## New Architecture (Summary)

- All four clients connect directly to a single **MariaDB 11.4.x LTS** instance via EF Core 10 over TCP.
- No local DB on any client. No sync layer.
- Concurrency handled by EF Core optimistic concurrency tokens + `SELECT ... FOR UPDATE` on FIFO decrement paths.
- Host laptop must be UPS-backed; nightly `mysqldump` backup to a second machine.
- Loss of host connectivity stops the affected client — the explicit trade for cross-client correctness.

See [[centralized-database-architecture|Centralized Database Architecture]] for the full concept page.

## Trade-offs (Summary)

| Property | Before | After |
|---|---|---|
| Host offline → client | Continues fully | Stops |
| Multi-client stock accuracy | Silent divergence possible | Serialized |
| Cross-client read latency | Up to 30 s | Real-time |
| Sync code complexity | High | Removed |

## New Risks (Replace "Data Sync Conflict")

| Risk | Likelihood | Impact |
|---|---|---|
| Host Laptop Failure | Medium | High |
| LAN Failure | Low–Medium | Medium |
| Concurrent Update Conflict | Medium | Low (handled by retry UX) |

## Effect on Existing Code

Existing INFRA-04, INFRA-05, INFRA-13, INFRA-22 sync implementations are removed. Implementation plans for the SQLite-removal refactor will be authored in a subsequent session.

## Effect on Academic Papers

The four module academic papers in `Sources/` remain unmodified — they are submitted academic documents describing the original design. Future revisions should describe the architecture as "pure client-server with centralized MariaDB" and frame offline-first as a considered-and-rejected alternative.

## Source Citations

All content from `Sources/system_plan_amendment_2026-05-28.md`. No external sources.
