---
module: Infrastructure
agent: claude-code
date: 2026-05-15
plan-ref: Plans/VISTA_Modules/Infrastructure/11-production-deployment-config.md
status: completed
---

## Task Summary

Implements the production deployment configuration layer for INFRA-11. Delivers credential sanitisation of `appsettings.json`, a `ConnectionStringLoader` module with three-state overlay logic, a committed production template, `.gitignore` protection, and the first operator-facing runbook. The Pomelo 10.x bump is **deferred** (see below).

**Plan:** `[[11-production-deployment-config]]`

## What Was Done

- Modified `.gitignore` — added `appsettings.Production.json` exclusion (sequenced before the template was created)
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/appsettings.json` — removed `Sync:MariaDbConnection` key (the only credential-bearing field); all other `Sync` fields retained for the TCP probe
- Created `WPF_Applications/MerchSys/src/MerchSys.App/appsettings.Production.template.json` — operator template with `_comment` keys; placeholder values for `Host`, `Password`; real defaults for `Database` (`merchsys_central`) and `User` (`merchsys_sync`) matching `mariadb-init.sql`
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Configuration/ConnectionStringLoader.vb` — VB Module with three-state overlay logic: `GetProductionConfigPath()`, `AddProductionOverlay()` extension on `IConfigurationBuilder`, `GetMariaDbConnectionString(cfg, logger)`
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb` — added `builder.ConfigureAppConfiguration` call to load the production overlay before DI resolution; added `Imports MerchSys.App.Configuration`
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Startup/SyncConfig.vb` — replaced `cfg.GetSection("Sync")("MariaDbConnection")` direct read with `ConnectionStringLoader.GetMariaDbConnectionString(cfg, logger)` call; added `Imports Microsoft.Extensions.Logging` and `Imports MerchSys.App.Configuration`
- Created `Plans/VISTA_Modules/Infrastructure/runbooks/01-production-deployment.md` — operator runbook covering all six specified sections

## Pomelo 10.x Bump — DEFERRED

NuGet search result as of 2026-05-15:

```
Pomelo.EntityFrameworkCore.MySql  Latest Version: 9.0.0
```

No 10.x release is available. The current pinned version remains `9.0.0` in `MerchSys.SharedKernel.vbproj`. The NU1608 suppression in `Directory.Build.props` is retained. The bump is deferred to a future session once a Pomelo 10.x stable release is published.

## Production Config Path

The loader resolves the overlay at:
```
%LOCALAPPDATA%\VISTA\appsettings.Production.json
```

Implemented via `Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)` — no hard-coded path strings.

## appsettings.json git diff (credential removal)

```diff
-    "MariaDbConnection": "Server=192.168.1.10;Port=3306;Database=merchsys_central;Uid=merchsys_sync;Pwd=CHANGE_ME;",
```

All other `Sync` section keys (`CentralServerHost`, `CentralServerPort`, `ProbeIntervalSeconds`, `ProbeTimeoutMs`, `MaxAttempts`, `BatchSize`) are retained — they are used by `DualConditionSyncProbe` and `SyncSettings` and carry no credential material.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors |
| Pre-existing warnings | 1 (BC40000 in `VatConfigurationMap.vb` — not introduced by this plan) |
| No new warnings introduced | ✅ |
| Manual verification | N/A (deployment-time feature) |

## Issues Encountered

- **Naming discrepancy between plan spec and INFRA-06 SQL.** The plan spec for INFRA-11 specifies `"Database": "vista_central"` and `"User": "vista_sync"` in the template. The actual `mariadb-init.sql` (INFRA-06) creates `marchsys_central` and `merchsys_sync`. The template and runbook were written to match the actual SQL to avoid misleading operators. The plan spec values appear to be an early-draft artefact that predated INFRA-06 implementation.
  - **Resolution:** Template uses `merchsys_central` / `merchsys_sync`; discrepancy noted here and in the template `_comment` fields.
  - **Agent Wiki entry:** Not required (no code fix needed).

## What's Next

- [ ] Pomelo 10.x bump — defer until `Pomelo.EntityFrameworkCore.MySql` 10.x is published on NuGet.org; remove NU1608 suppression from `Directory.Build.props` at that time.
- [ ] Live operator walkthrough against a real MariaDB 11.4.x instance to validate the INFRA-06 acceptance criteria steps 2–3 documented in the runbook.

## Cross-References

- Domain Wiki pages consulted: `[[owasp-da-top10]]`, `[[client-server-wpf]]`, `[[tech-stack-reference]]`
- Agent Wiki entries consulted: none
- Codebase wiki discrepancies: plan spec for INFRA-11 uses `vista_central`/`vista_sync`; actual INFRA-06 SQL uses `merchsys_central`/`merchsys_sync` — codebase_wiki does not document this naming.
