**SYSTEM PLAN — ARCHITECTURE AMENDMENT**

**Villon Integrated Supply and Trade Application**

Villon Farm Supply

─────────────────────────────────────

**Document Type:** Architecture Amendment to System Plan
**Amendment ID:** AMD-2026-05-28-01
**Date:** 2026-05-28
**Supersedes:** `system_plan.md` §5.3, §5.4 (DB row), §10 row "Data Sync Conflict", §11 row "Data Sync Conflict", §12 bullet "Offline-first…"
**Status:** Authoritative — overrides the superseded sections for all implementation work from 2026-05-28 onward.
**Author:** Project Owner

---

**1. Reason for Amendment**

The original `system_plan.md` (§5.3) specifies an offline-first SQLite local store with one-way push sync to a centralized MariaDB instance. That model was correct for the originally envisioned **single-workstation** deployment (one Manager PC plus an Owner read-only view from the same machine or a second client used intermittently).

The project has since expanded the deployment target to **four concurrent client laptops** sharing a single MariaDB host on a local network. The expanded target is a deliberate stress test of true multi-client client-server operation — exercising every module under simultaneous load, even though only two human users (Manager, Owner) will operate the system in production.

In the four-client topology, offline-first SQLite is no longer protective: it actively introduces correctness failures. Specifically:

- **Stale dashboards.** With four clients each holding their own SQLite, a sale completed on Laptop A does not appear on Laptop B's Stock Dashboard until B re-pulls from MariaDB. The implemented sync is push-only — there is no pull path — so B never sees A's writes.
- **Silent write loss under last-write-wins.** If two clients sell from the same `Inv_StockBatches` row concurrently while offline, the later-arriving UPDATE overwrites the earlier one. Both sales are recorded but only one stock decrement is reflected; physical stock and system stock diverge silently.
- **Untestable concurrency.** The four-laptop stress test cannot validate inventory accuracy with offline-first SQLite in place. The very property under test (concurrent multi-client correctness) is undermined by the architecture meant to support it.

This amendment resolves the conflict by removing SQLite from the architecture and adopting pure client-server operation against MariaDB.

---

**2. Amended Architecture (Replaces §5.3 and §5.4 SQLite row)**

**2.1 Storage Model — Pure Client-Server**

The system uses a single centralized **MariaDB 11.4.x LTS** database hosted via XAMPP on a designated host laptop on the local network. All four client laptops connect to this single MariaDB instance via Entity Framework Core 10 over TCP. There is no local database on any client. There is no sync layer.

```
[Laptop 1 — VISTA Client]
[Laptop 2 — VISTA Client]   ─┐
[Laptop 3 — VISTA Client]    ├──► MariaDB 11.4.x (XAMPP) on Host Laptop
[Laptop 4 — VISTA Client]   ─┘     (LAN, TCP 3306)
```

**2.2 Connection Model**

- Each client opens an EF Core `DbContext` per unit of work (per request scope in the WPF DI container).
- Connection string is read from `appsettings.json` per client, parameterized by host IP/name.
- All writes commit to MariaDB synchronously. There is no journal, no outbox, no eventual consistency.

**2.3 Concurrency Control**

- EF Core optimistic concurrency tokens (`RowVersion` / `xmin` equivalent — MariaDB `TIMESTAMP(6) ON UPDATE`) on mutable rows (`Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`, `Pos_CreditAccounts.OutstandingBalance`).
- Inventory decrements (POS sale, shrinkage, returns) execute inside a single MariaDB transaction that selects the FIFO batch row(s) `FOR UPDATE`, decrements, and commits. This serializes concurrent sales of the same product across all four clients without application-level coordination.
- On `DbUpdateConcurrencyException`, the affected ViewModel surfaces a "Data changed elsewhere — refresh and retry" notification and reloads the affected aggregate. No silent overwrites.

**2.4 Connectivity Requirement**

- Loss of connectivity to the MariaDB host **stops the application** at the affected client. This is the explicit, intentional trade-off accepted in §3 below.
- A connectivity health indicator is shown in the shell header (Online / Reconnecting / Offline). On `Offline`, all command buttons disable; on `Reconnecting`, the client retries with exponential backoff up to 5 attempts before requiring a manual retry.
- The host laptop **must** be on a UPS (already recommended in original §11 "System Downtime" row — now elevated from recommendation to requirement).

**2.5 Removed Components**

The following components specified or implied by original §5.3 are **removed**:

- All `SQLite` packages and `DbContext` SQLite providers
- `Sync_Journal` table and its `Pur_/Inv_/Pos_/Acc_` row variants
- `SyncOrchestrator`, `SyncWorker`, `MariaDbSyncTransmitter`, `MariaDbSyncContext`
- `ISyncableRepository`, `SyncableRepositoryCore`, all `*SyncMap` classes
- `ConflictResolver`, `ConflictResolution`, `SyncAction`, `LastWriteWins` logic
- `NetworkAvailabilityChanged` + TCP-probe dual-condition check
- `SyncStatusIndicator` (replaced by simpler ConnectionStatus indicator)
- `DatabaseInitializer` SQLite migration runner (replaced by MariaDB schema bootstrap)

---

**3. Trade-offs Accepted**

| Property | Before (Offline-First SQLite + Sync) | After (Pure MariaDB Client-Server) |
|---|---|---|
| Operation when host laptop offline | Each client continues fully | All clients stop |
| Multi-client stock accuracy | Silent divergence possible | Serialized via DB transactions |
| Cross-client visibility latency | Up to 30 s (sync probe interval) | Real-time (next read) |
| Code complexity | High (sync orchestrator, journal, conflict resolver, maps per module) | Low (standard EF Core against single DB) |
| BIR audit traceability | Distributed across 4 SQLite files + central | Single source of truth |
| Fit for 4-client concurrency test | Fails — push-only sync provides no cross-client read consistency | Native |

The trade is explicit: **availability under host-laptop failure is exchanged for correctness under concurrent multi-client writes**. For a single-location shop on a LAN with a UPS-backed host, the failure mode (host down → operations stop) is acceptable; the previous failure mode (silent stock divergence) was not.

---

**4. Amended Risk Register (Replaces "Data Sync Conflict" row)**

| Risk | Description | Likelihood | Impact |
|---|---|---|---|
| **Host Laptop Failure** | Designated MariaDB host laptop fails, loses power, or drops off the LAN; all clients stop until host is restored or the database is moved. | Medium | High |
| **LAN Failure** | Local network outage isolates one or more clients from the host. | Low–Medium | Medium |
| **Concurrent Update Conflict** | Two clients attempt to modify the same row simultaneously; one receives `DbUpdateConcurrencyException` and must retry. | Medium | Low (handled by retry UX) |

The "Data Sync Conflict" risk from original §10 is **removed** — there is no sync layer to conflict.

---

**5. Amended Mitigations (Replaces "Data Sync Conflict" mitigation in §11)**

| Risk | Mitigation |
|---|---|
| **Host Laptop Failure** | (a) UPS on host laptop — now a hard requirement, not a recommendation. (b) Nightly automated `mysqldump` backup of `merchsys_central` to a second machine via Windows Task Scheduler. (c) Documented runbook: spin MariaDB up on a backup laptop, update each client's `appsettings.json` host entry, restart clients. Target RTO: 30 minutes. |
| **LAN Failure** | (a) Wired Ethernet preferred over Wi-Fi for the host laptop. (b) Connection retry with exponential backoff in client; clear visual indicator of connection state. (c) On extended outage, the affected client displays "Cannot reach VISTA server — contact administrator" with no partial-state writes attempted. |
| **Concurrent Update Conflict** | (a) Optimistic concurrency tokens on all mutable rows. (b) `SELECT ... FOR UPDATE` inside the transactional FIFO decrement path. (c) User-facing retry on conflict; never silent overwrite. |

---

**6. Amended Constraint List (Replaces §12 bullet on offline-first)**

- ~~Offline-first: all transactions committed to local SQLite; auto-sync to MariaDB on stable dual-condition connection.~~ **REMOVED.**
- **NEW:** Pure client-server: all reads and writes go directly to the centralized MariaDB 11.4.x LTS database hosted via XAMPP on a designated host laptop. No local database on clients.
- **NEW:** Host laptop must be UPS-backed.
- **NEW:** Nightly automated backup of `merchsys_central` to a secondary machine.
- **NEW:** All mutable financial and inventory rows carry an optimistic concurrency token.
- **NEW:** Inventory FIFO decrement path uses `SELECT ... FOR UPDATE` under a single transaction to serialize concurrent client writes.

All other constraints in §12 (modular monolith, MediatR-only inter-module communication, FIFO costing, expiry tracking, plain-language reporting, OWASP DA Top 10, 3NF, audit trail, microservice-extraction-ready interfaces) remain in force.

---

**7. Tech Stack Delta**

| Component | Before | After |
|---|---|---|
| Local DB | SQLite (offline-first) | **Removed** |
| Central DB | MariaDB 11.4.x LTS (XAMPP) | MariaDB 11.4.x LTS (XAMPP) — unchanged |
| ORM | EF Core 10 (Sqlite + Pomelo MySQL providers) | EF Core 10 (Pomelo MySQL provider only) |
| Sync infrastructure | `SyncWorker` BackgroundService, journal tables, conflict resolver | **Removed entirely** |
| Concurrency | LastWriteWins on sync | EF Core optimistic concurrency + `FOR UPDATE` |

---

**8. Notes on Academic Papers in `Sources/`**

The four academic papers (`Purchasing-Module_AcademicPaper.md`, `Inventory-Module_AcademicPaper.md`, `POS-Module_AcademicPaper.md`, `Accounting-Module_AcademicPaper.md`) reference SQLite and offline-first architecture in their Scope and Significance sections. Those papers are submitted academic documents and remain unmodified — they describe the original design intent.

For implementation purposes (and any future paper revision), this amendment supersedes those references. Future paper revisions should describe the architecture as "pure client-server with centralized MariaDB" and frame offline-first as a design alternative considered and rejected in favor of strong multi-client consistency.

---

**9. Effect on Existing Code**

This amendment requires removal of the existing INFRA-04, INFRA-05, INFRA-13, INFRA-22 sync implementations and a one-time migration of any data currently in local SQLite files to the central MariaDB instance. Implementation plans for these removals and the MariaDB-only refactor will be authored in a subsequent session.

---

**10. Acceptance**

This amendment is accepted by the Project Owner as of 2026-05-28 and is the authoritative architecture specification from this date forward. The original `system_plan.md` is retained unchanged as the historical baseline.
