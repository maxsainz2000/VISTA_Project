---
module: MerchSys.Infrastructure
plan-id: INFRA-23
title: "EF Core 10 ↔ MariaDB Provider Decision & Validation"
depends-on: []
estimated-files: 4
priority: critical
amendment-ref: AMD-2026-05-28-01
---

# INFRA-23: EF Core 10 ↔ MariaDB Provider Decision & Validation

## Context

Per the 2026-05-28 architecture amendment (`LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md`), VISTA pivots from offline-first SQLite to pure client-server against a single centralized MariaDB 11.4.x LTS instance. All four module `DbContext`s (`PurchasingDbContext`, `InventoryDbContext`, `PosDbContext`, `AccountingDbContext`) must switch from `Microsoft.EntityFrameworkCore.Sqlite` to a MariaDB EF Core provider.

**The blocker:** INFRA-17 and `Operator/debug-logs/INFRA-test-5.md` documented a binary incompatibility between `Pomelo.EntityFrameworkCore.MySql 9.0.0` and EF Core 10 (`MissingMethodException` on `AbstractionsStrings.ArgumentIsEmpty`). Pomelo 10.x has not shipped and shows no upstream activity. The previous sync-layer workaround was to drop EF Core for MariaDB writes and use raw `MySqlConnector` — that workaround is not viable for the full module-DbContext layer (LINQ queries, change tracking, navigation properties are pervasive in module code).

This plan **decides** the provider and pins it. Every subsequent INFRA plan in the SQLite-removal sequence (INFRA-24 onward) depends on the decision recorded here.

## Prerequisites

- None. This is the first plan in the post-amendment sequence.

## Wiki References

- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` — amendment authority
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` — new architecture
- `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md` — pattern entry (will be updated with the chosen provider)
- `Operator/debug-logs/INFRA-test-5.md` — Pomelo 9 ↔ EF Core 10 binary incompat crash details

## Deliverables

```
Plans/VISTA_Modules/Infrastructure/23/
├── provider-evaluation.md                              ' NEW — written analysis of candidates
├── spike/MerchSys.ProviderSpike.vbproj                 ' NEW — disposable spike project
├── spike/SpikeContext.vb                               ' NEW — minimal DbContext, 2 entities, 1 nav property
└── spike/Program.vb                                    ' NEW — CRUD smoke run + ToListAsync materialization check
Progress/VISTA_Modules/Infrastructure/INFRA-23-summary.md   ' NEW — decision record
```

No production code is touched in this plan. The spike project is disposable — it does not enter `MerchSys.slnx`.

## Specification

### Candidates to evaluate (in priority order)

1. **`MySql.EntityFrameworkCore`** — Oracle's official provider. Check NuGet for an EF Core 10–compatible version. Validate against:
   - Basic CRUD against MariaDB 11.4.x
   - LINQ query translation (Where, OrderBy, GroupBy, Include for navigation)
   - `ToListAsync()` on a full entity query (the `efcore-vbnet-tolistasync-entity-empty` bug must be re-tested under MariaDB — re-validation required regardless of provider choice)
   - `UseRowVersion`/`ValueGeneratedOnAddOrUpdate` with `TIMESTAMP(6)` columns
   - `BeginTransactionAsync` + `SqlQueryRaw` with `FOR UPDATE` row locking

2. **Pomelo 9.0.0 against EF Core 10 with explicit binding redirects / direct dependency overrides.** Re-test the `MissingMethodException` from INFRA-test-5 under .NET 10 SDK 10.0.7+ — confirm the bug persists or has been fixed. If a workaround binding redirect resolves it cleanly, Pomelo remains a candidate.

3. **Downgrade `Microsoft.EntityFrameworkCore` from 10.x to 9.x** to use Pomelo 9 unblocked. Cost: loss of EF Core 10 features (notably ExecuteUpdate/Delete in their 10.x form). Document the loss.

4. **Stay on EF Core 10 + raw `MySqlConnector` for all data access.** No EF Core for module DbContexts. Cost: massive rewrite of every query/handler. Document as the "nuclear option" baseline.

### Evaluation matrix

For each candidate, the spike must produce a single table:

| Capability | Candidate 1 | Candidate 2 | Candidate 3 | Candidate 4 |
|---|---|---|---|---|
| Compiles against EF Core 10 | | | | |
| Connects to MariaDB 11.4.x | | | | |
| Basic CRUD round-trip | | | | |
| LINQ `Where` + `OrderBy` | | | | |
| `Include` (nav property load) | | | | |
| `ToListAsync()` on full entity (VB.NET bug?) | | | | |
| `RowVersion` / `TIMESTAMP(6)` mapped correctly | | | | |
| `BeginTransactionAsync` + `FOR UPDATE` works | | | | |
| NuGet package vulnerability flags | | | | |
| Active maintenance (last release date) | | | | |

### Decision criteria

The chosen provider must satisfy **all** of the following:

- Compiles and runs against `Microsoft.EntityFrameworkCore` 10.0.7+ with **zero warnings** (no NU1608 etc.) and **zero runtime exceptions** on the smoke run.
- Supports `IsRowVersion()` against MariaDB `TIMESTAMP(6)` (or equivalent) for optimistic concurrency. If the provider can't express this, document the alternative (e.g., a `BIGINT` version column updated via trigger).
- Supports `Database.SqlQueryRaw` (or equivalent) for `SELECT ... FOR UPDATE` row locking inside an `IDbContextTransaction`.
- Maintained upstream (last release within the past 12 months) OR explicitly pinned and accepted as frozen.

### Output: the decision record

`Progress/VISTA_Modules/Infrastructure/INFRA-23-summary.md` must contain:

1. The completed evaluation matrix.
2. The chosen provider with exact NuGet package name and pinned version.
3. The connection string template (with placeholders for host, user, password).
4. Any documented gotchas discovered during the spike (e.g., `ToListAsync` bug status under MariaDB, `TIMESTAMP(6)` mapping quirks, transaction isolation defaults).
5. A short "rejected because" paragraph for each non-chosen candidate.

This document becomes the single citation for every subsequent INFRA plan's "EF Core MariaDB provider" reference.

## Acceptance Criteria

1. Spike project builds with 0 errors / 0 warnings against EF Core 10.
2. Spike `Program.vb` executes a Round-trip (insert 2 rows, query by predicate with `ToListAsync`, materialize navigation property via `Include`, update with `RowVersion` check, transactional `SELECT ... FOR UPDATE`) against a local MariaDB instance and prints `OK` for each step.
3. `provider-evaluation.md` contains the filled evaluation matrix for all four candidates (even rejected ones — show the work).
4. `Progress/VISTA_Modules/Infrastructure/INFRA-23-summary.md` names the chosen provider, pins its version, and documents the connection string template.
5. `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md` "EF Core ↔ MySQL Provider — Open Question" section is updated with the resolution.

## Out of Scope (Defer)

- Refactoring any existing module `DbContext` to MariaDB. That is INFRA-24.
- Schema bootstrap (CREATE TABLE) on MariaDB. That is INFRA-26 (independent of provider choice — uses raw `MySqlConnector`).
- Multi-client `appsettings.json` distribution. That is INFRA-28.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-23-summary.md` per `Progress/_template.md`. Must include:

- Evaluation matrix (filled).
- Chosen provider + version + NuGet package ID.
- Connection string template.
- Spike output log (CRUD + RowVersion + FOR UPDATE round-trip succeeded).
- Status of the `efcore-vbnet-tolistasync-entity-empty` bug under MariaDB (still reproducible? if so, raw-connection workaround pattern carries over).
- Explicit rejected-because lines for the other candidates.

### Documentation

- Header comment in `SpikeContext.vb` noting that this is a disposable spike for INFRA-23 and must not be referenced from production code.
- Update `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md` — replace the "Open Question" section with the resolution.
