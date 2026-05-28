---
module: Infrastructure
agent: antigravity
date: 2026-05-28
plan-ref: Plans/VISTA_Modules/Infrastructure/23-mariadb-efcore-provider-decision.md
status: completed
---

## Task Summary

Evaluated and pinned the Entity Framework Core 10 provider for MariaDB 11.4.x. Built and executed a disposable validation spike to confirm that Oracle's official `MySql.EntityFrameworkCore` version `10.0.7` satisfies all technical criteria, including basic CRUD, LINQ queries, asynchronous materialization, optimistic concurrency, and pessimistic row locking.

**Plan:** `[[23-mariadb-efcore-provider-decision]]`

## What Was Done

- Created `Plans/VISTA_Modules/Infrastructure/23/provider-evaluation.md` — Detailed candidates and the technical decision matrix.
- Created `Plans/VISTA_Modules/Infrastructure/23/spike/MerchSys.ProviderSpike.vbproj` — Disposable .NET 10 console project referencing `MySql.EntityFrameworkCore` v10.0.7.
- Created `Plans/VISTA_Modules/Infrastructure/23/spike/SpikeContext.vb` — Defined a minimal DbContext with two relational entities and a TIMESTAMP(6)-based `RowVersion` optimistic concurrency token.
- Created `Plans/VISTA_Modules/Infrastructure/23/spike/Program.vb` — Coded validation steps checking CRUD, LINQ `Include` queries, optimistic concurrency (`DbUpdateConcurrencyException`), and pessimistic row locking (`SELECT ... FOR UPDATE`).
- Executed the spike project against the live host MariaDB instance. Spike output:
  ```
  INFRA-23 Spike: Starting Verification...
  OK: EnsureCreated schema completed.
  OK: CRUD inserts completed.
  OK: LINQ query and ToListAsync navigation load completed.
  OK: Context A saved changes successfully.
  OK: Context B threw DbUpdateConcurrencyException as expected.
  Starting SELECT FOR UPDATE locking query...
  OK: SELECT FOR UPDATE executed and locked successfully.
  OK: Schema cleanup completed successfully.
  ALL SPIKE CHECKS: OK
  ```
- Modified `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md` — Updated the MySQL Provider section with the chosen provider, package name, version, and spike findings.

## Build & Test Status

| Check | Status |
|---|---|
| Spike builds cleanly | ✅ |
| Spike program runs | ✅ (All Checks: OK) |
| Manual verification | ✅ |

## Issues Encountered

None. The Oracle provider v10.0.7 compiles and operates beautifully against EF Core 10.0.7 and MariaDB 11.4.x with zero warning flags.

## What's Next

Proceed to **INFRA-24 (MariaDB Schema Bootstrap)** to translate the SQLite migration schema and implement a raw startup schema initializer.

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`
- Agent Wiki entries consulted: `[[mariadb-pure-client-server-architecture]]`
