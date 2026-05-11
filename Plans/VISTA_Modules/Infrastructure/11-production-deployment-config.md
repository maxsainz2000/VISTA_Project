---
module: MerchSys.Infrastructure
plan-id: INFRA-11
title: "Production Deployment Configuration"
depends-on: [INFRA-05, INFRA-06]
estimated-files: 5
---

# Production Deployment Configuration

## Context

The 2026-05-11 Infrastructure audit lists three deployment-readiness gaps that all share the same theme: the codebase ships with development defaults that are unsafe for live use.

1. **MariaDB password is `CHANGE_ME` in `appsettings.json`.** No `appsettings.Production.json` exists, and there is no documented mechanism for the operator to supply a real password outside the repository.
2. **`Pomelo.EntityFrameworkCore.MySql` is pinned to a pre-10.x version**, raising NU1608 build warnings against the EF Core 10.x line used everywhere else. The audit flags this as "upgrade when a 10.x release is available". As of the audit date a 10.x release exists upstream; this plan adopts it.
3. **`mariadb-init.sql` has never been applied to a fresh MariaDB 11.4.x instance** for acceptance-criteria verification of INFRA-06 (steps 2–3 of that plan's criteria).

This plan delivers a deployment-config layer, the package upgrade, and a verification runbook that closes all three gaps as one unit because they share a single operator-facing audience.

## Prerequisites

- **INFRA-05** (Sync Worker) — `appsettings.json` connection-string consumer
- **INFRA-06** (MariaDB Central Schema) — `mariadb-init.sql`

## Wiki References

- `concepts/owasp-da-top10.md` — Secrets management; no credentials in source control
- `concepts/client-server-wpf.md` — Central server is operator-managed, not a developer-shared resource
- `analysis/tech-stack-reference.md` — Pinned NuGet versions

## Deliverables

```
WPF_Applications/MerchSys/src/MerchSys.App/appsettings.json                  ' Modified — remove the CHANGE_ME credential
WPF_Applications/MerchSys/src/MerchSys.App/appsettings.Production.template.json   ' New — template, not the real file
WPF_Applications/MerchSys/src/MerchSys.App/Configuration/ConnectionStringLoader.vb  ' Modified — load order

Plans/VISTA_Modules/Infrastructure/runbooks/
└── 01-production-deployment.md                         ' New — operator-facing runbook

Directory.Packages.props (or the .csproj)              ' Modified — Pomelo 10.x bump
```

`appsettings.Production.json` itself is **never** committed; the template ships with placeholder values and a comment explaining where the operator must place the real file. `.gitignore` is updated to exclude `appsettings.Production.json`.

## Specification

### appsettings.json sanitisation

Strip the MariaDB credential block entirely from the committed `appsettings.json`. Leave only the SQLite-local connection string and feature flags. The MariaDB connection becomes opt-in: if no production overlay is present, the sync worker logs a warning at startup and runs in **local-only** mode (which is also the correct dev-machine behaviour for any contributor without a MariaDB instance).

### appsettings.Production.template.json

A near-identical structure to `appsettings.json` with a single `MariaDb` section containing placeholder strings:

```json
{
  "MariaDb": {
    "Host": "<central-server-hostname>",
    "Port": 3306,
    "Database": "vista_central",
    "User": "vista_sync",
    "Password": "<replace-with-real-password>",
    "SslMode": "Required"
  }
}
```

Header comment in the file (JSON does not support comments, so use a `"_comment"` key at the top level): operator-facing instructions for where to place the live file, OS-level permissions (read-only by the service account), and a reminder that `SslMode: Required` is non-negotiable for production.

### ConnectionStringLoader.vb

Existing loader is modified to:

1. Read `appsettings.json` (committed defaults).
2. Read `appsettings.Production.json` if it exists at the user-level config path (`%LOCALAPPDATA%\VISTA\appsettings.Production.json` on Windows). Overlay wins.
3. If a `MariaDb` section is present and `Password` is the placeholder string `<replace-with-real-password>`, treat the configuration as **missing** (not partially valid) and log a warning.
4. Pass the merged configuration to the existing `IConfiguration` builder. No new abstraction; this is a small additive method on the existing loader.

The user-level path lookup is OS-aware: Windows uses `LocalApplicationData`, but the loader should call `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` rather than hard-coding the path. The runbook documents the exact path on Windows.

### Pomelo upgrade

Bump `Pomelo.EntityFrameworkCore.MySql` to the latest 10.x release. If the project uses `Directory.Packages.props` for central package management, edit that file; otherwise edit the consuming `.csproj` files. Confirm `dotnet restore` reports no NU1608 against the EF Core 10.x line afterwards.

If a 10.x release is **not** in fact available at implementation time (the audit dates from 2026-05-11; this plan may be executed weeks later), record the actual published versions in the implementation summary and pick the highest 10.x available. If only an `8.x` release exists, the plan is partially blocked: deliver the rest of the scope and document the Pomelo bump as deferred, do not silently re-pin to a non-10.x version.

### runbooks/01-production-deployment.md

The first operator-facing runbook in the repository. Sections:

1. **Prerequisites** — MariaDB 11.4.x installed, network reachability from the WPF host.
2. **Schema initialisation** — apply `mariadb-init.sql` followed by INFRA-08's `02-pos-receipt-integrity-triggers.sql`. Exact command lines. Expected output. Verification queries against `INFORMATION_SCHEMA.TABLES` and `INFORMATION_SCHEMA.TRIGGERS`.
3. **User provisioning** — create `vista_sync` MariaDB user, grant `INSERT, SELECT` on the schema, deny `UPDATE` and `DELETE` on the tamper-evidence tables.
4. **Production config placement** — copy `appsettings.Production.template.json` to `%LOCALAPPDATA%\VISTA\appsettings.Production.json`, fill in real values, set NTFS read permission to the running user only.
5. **First-run verification** — start the WPF app; the new sync-status indicator (INFRA-10) should transition Offline → Probing → Online.
6. **Acceptance verification** — re-run the INFRA-06 acceptance criteria steps 2–3 against the freshly provisioned instance. Document the observed output inline.

This is the first runbook; create the `runbooks/` directory under `Plans/VISTA_Modules/Infrastructure/` to host it.

## Implementation Notes

- The removal of the `CHANGE_ME` credential is a small but security-meaningful change. Once landed, anyone running the app without a production overlay gets local-only behaviour by default — there is no risk of a developer accidentally pushing to a shared MariaDB because they forgot to override a placeholder.
- Per CLAUDE.md OWASP DA Top 10 reference: `appsettings.Production.json` is the secret-bearing file; its existence and permissions are the security perimeter. The template makes the requirement explicit but does not itself constitute a leak.
- `.gitignore` must include `appsettings.Production.json` **before** the template file is committed — sequencing matters. The implementation summary should record the order of operations used.
- The Pomelo bump may surface a transitive breaking change. If the build no longer succeeds after the bump, do **not** fix the breakage in this plan — per CLAUDE.md, document the errors in `Progress/` and stop. The bump is a self-contained scope; downstream remediation is a separate session.
- The runbook is markdown, not code. It still must be precise enough that a future operator can follow it without asking. Every command, path, and expected output goes in.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings after the Pomelo bump (or — if no 10.x release is available — the scope is delivered minus the bump, with the deferral documented).
2. `appsettings.json` no longer contains a `Password` field for MariaDB; it contains no credential material of any kind.
3. `appsettings.Production.template.json` exists, is committed, and contains placeholder values.
4. `.gitignore` excludes `appsettings.Production.json`.
5. `ConnectionStringLoader` overlays the production file from `%LOCALAPPDATA%\VISTA\appsettings.Production.json` when present.
6. If the production file is absent, the loader logs a warning and the sync worker operates in local-only mode without crashing.
7. If the production file is present but `Password` is still the placeholder string, the loader treats it as absent and logs a distinct, actionable warning.
8. `runbooks/01-production-deployment.md` exists under `Plans/VISTA_Modules/Infrastructure/runbooks/` and covers all six numbered sections in the specification.
9. The runbook references INFRA-08's trigger SQL by exact filename.
10. The runbook references INFRA-10's status indicator as the first-run smoke signal.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-11-summary.md` using `Progress/_template.md`. Include:

- The exact final pinned version of `Pomelo.EntityFrameworkCore.MySql` (or a deferral note if no 10.x is available).
- The `git diff` of `appsettings.json` showing the credential removal.
- The exact `%LOCALAPPDATA%`-relative path the loader looks at.
- Any breaking changes surfaced by the Pomelo bump and whether they were resolved or deferred.

### Documentation
- Inline `"_comment"` keys in the template file explaining each field.
- A short header comment in the runbook citing INFRA-05, INFRA-06, INFRA-08, and INFRA-10 as the prerequisite landing zones.
- XML doc on the overlay logic in `ConnectionStringLoader` explaining the three-state outcome (absent / placeholder / valid).
