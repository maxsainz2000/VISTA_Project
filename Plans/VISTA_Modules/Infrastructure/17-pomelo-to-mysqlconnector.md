---
module: MerchSys.Infrastructure
plan-id: INFRA-17
title: "Replace Pomelo with MySqlConnector (Raw ADO.NET)"
depends-on: [INFRA-06, INFRA-12]
estimated-files: 5
priority: critical
---

# INFRA-17: Replace Pomelo with MySqlConnector (Raw ADO.NET)

## Context
The sync layer to MariaDB is blocked by a binary incompatibility between `Pomelo.EntityFrameworkCore.MySql 9.0.0` and EF Core 10.0.7 (`MissingMethodException` on `AbstractionsStrings.ArgumentIsEmpty`). Pomelo 10.x has not been released and shows no sign of shipping before the June 2026 deadline. See [INFRA-test-5.md](../../../../Operator/debug-logs/INFRA-test-5.md) for crash details.

## Deliverables
- `MerchSys.SharedKernel.vbproj` — swap Pomelo 9 for MySqlConnector 2.x
- `Directory.Build.props` — remove NU1608 suppression
- `MariaDbSyncContext.vb` — rewrite from DbContext to connection wrapper with reflection-based SQL generation
- `MariaDbSyncTransmitter.vb` — rewrite EF Core Add/Update/Remove to use raw parameterized SQL
- `SyncConfig.vb` — replace AddDbContext with factory registration

## Specification
- Raw `MySqlConnector` ADO.NET with parameterized SQL
- `ISyncTransmitter` interface must remain unchanged
- Module SyncMap POCOs must remain unchanged
- Conflict semantics (financial reject-on-conflict, non-financial upsert) must remain unchanged and be implemented at the SQL level

## Acceptance Criteria
1. `dotnet build` is completely clean (0 errors, 0 warnings).
2. The NU1608 warning is eliminated.
3. The sync status indicator successfully transitions to Online (green) against a live MariaDB 11.4.x instance.
4. Transmission idempotency is preserved (Test 6 passes).
