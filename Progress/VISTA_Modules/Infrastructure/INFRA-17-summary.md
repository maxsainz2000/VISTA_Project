---
module: Infrastructure
agent: claude-code
date: 2026-05-23
plan-ref: Plans/VISTA_Modules/Infrastructure/17-pomelo-to-mysqlconnector.md
status: completed
---

## Task Summary

Implemented INFRA-17 to resolve the Pomelo 9 / EF Core 10 incompatibility block. Replaced the `Pomelo.EntityFrameworkCore.MySql` dependency with raw `MySqlConnector` ADO.NET. `MariaDbSyncContext` was completely rewritten to a lightweight connection wrapper featuring reflection-based SQL generation for INSERT/UPDATE with per-type query caching. 

**Plan:** `[[17-pomelo-to-mysqlconnector]]`

## What Was Done

- Modified `WPF_Applications/MerchSys/src/MerchSys.SharedKernel/MerchSys.SharedKernel.vbproj` — swapped Pomelo for `MySqlConnector` 2.5.0.
- Modified `WPF_Applications/MerchSys/Directory.Build.props` — removed NU1608 suppression.
- Rewrote `WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Sync/MariaDbSyncContext.vb` — replaced EF Core DbContext with a thin ADO.NET wrapper. Added caching reflection SQL builder.
- Rewrote `WPF_Applications/MerchSys/src/MerchSys.App/Services/Sync/MariaDbSyncTransmitter.vb` — replaced EF Core persistence calls with parameterized ADO.NET execution, preserving exact conflict semantics per table.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Startup/SyncConfig.vb` — updated DI registration from `AddDbContext` to `AddScoped` factory.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors |
| Pre-existing warnings | 1 (BC40000 in `VatConfigurationMap.vb`) |
| No new warnings introduced | ✅ (NU1608 eliminated) |

## What's Next

- [x] Operator must re-run INFRA Test 5b to confirm the sync indicator goes green against the live MariaDB instance. *(completed/verified in Operator checklist)*
- [x] Codebase Wiki audit must be run for the Infrastructure module ~~to align the Data Access Layer manifests with the removal of `Pomelo`~~ — **Scope superseded by INFRA-23–30 (2026-05-28).** `MariaDbSyncContext.vb` (the file this plan rewrote) was deleted by INFRA-27. The Infrastructure module wiki audit now covers the full INFRA-23–30 migration (MariaDB schema bootstrap, DbContext conversion, concurrency, sync decommission, connection health monitor, runbooks, Activity Rail) — not just Pomelo removal in isolation.

## Cross-References

- Domain Wiki pages consulted: none
- Codebase wiki discrepancies: Agent must update wiki to reflect the fact that `MariaDbSyncContext` is no longer a DbContext.
