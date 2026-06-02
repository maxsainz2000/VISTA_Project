---
type: error-fix
module: MerchSys.App
agent: claude-code
date: 2026-06-02
tags: [mariadb, mysqlconnector, schema-bootstrap, ddl, grants, least-privilege, owner, read-only, tailscale, startup, runtime-error]
error-code: MySQL 1142
severity: runtime-error
status: diagnosed (remediation deferred — see Fix)
---

## Problem

When the VISTA app is configured as a **read-only Owner remote client** (connection string user = a `GRANT SELECT`-only MariaDB account, e.g. `merchsys_owner`), the app aborts at startup with the fatal dialog **"VISTA — Fatal" → "Cannot initialize database schema"** (`Application.xaml.vb:224`). It never reaches the login screen.

Exact server error (reproduced via `mysql.exe` as the read-only user):
```
ERROR 1142 (42000): CREATE command denied to user 'merchsys_owner'@'<host>'
                    for table `merchsys_central`.`__SchemaMigrations`
```

Discovered during the Phase 1 Tailscale Owner remote-access acceptance test (2026-06-02). See `Operator/PHASE-1-tailscale-owner-acceptance-2026-06-02.md`.

## Root Cause

`MariaDbSchemaInitializer.Initialize` runs on **every** application launch, and its first action `EnsureSchemaMigrationsTable` executes `CREATE TABLE IF NOT EXISTS __SchemaMigrations`.

The non-obvious part: **MariaDB checks the `CREATE` privilege *before* evaluating `IF NOT EXISTS`.** So even though the table already exists on the central DB, a SELECT-only account is denied. The failure happens *before authentication*, so it cannot be fixed by any login-time / auth-table grant.

A second, related write dependency surfaces immediately after, at login: `AuthenticationService.ClearFailedAttempts` runs `UPDATE Sys_UserAccounts SET FailedLoginAttempts=0 ...` on every **successful** auth (and `BumpFailedAttempts` on failure; DA6 first-login does `UPDATE PasswordHash, LastPasswordChangeAt`). All denied to a SELECT-only account.

Net: the app, as written, assumes a write/DDL-capable DB account at startup and login. The app-layer "Owner = read-only" enforcement (`RoleGuardInterceptor`, OWASP DA5) is independent of the DB grant; the DB-level SELECT-only grant (intended as defense-in-depth per the Phase 1 plan) is *incompatible* with the unconditional startup bootstrap.

## Fix

Remediation deferred (operator chose manual verification on 2026-06-02; no code/grant changes were made in the acceptance run). Recommended resolution:

1. **Login bookkeeping (plan-sanctioned):** grant the minimal auth-table writes on the host as root —
   ```sql
   GRANT UPDATE (FailedLoginAttempts, LockedUntil, ModifiedAt, LastPasswordChangeAt, PasswordHash)
     ON merchsys_central.Sys_UserAccounts TO 'merchsys_owner'@'%';
   FLUSH PRIVILEGES;
   ```
2. **Startup DDL bootstrap (prefer code gate, NOT a DDL grant):** gate the bootstrap so read-only clients skip it — do **not** grant `CREATE`/`ALTER` to the Owner account (that defeats least-privilege). Options:
   ```vb
   ' In the bootstrap entry point: honor a config flag set in the Owner overlay
   ' appsettings.Production.json:  "Schema": { "RunBootstrap": false }
   If config.GetValue(Of Boolean)("Schema:RunBootstrap", True) Then
       MariaDbSchemaInitializer.Initialize(connStr, logger)
   End If
   ```
   Alternative: catch MySQL error 1142 on the `__SchemaMigrations` ensure and continue when the schema is already present.

## Prevention

- A pure `GRANT SELECT` DB account **cannot run the current app** — the startup schema bootstrap always issues at least one DDL statement. Any "read-only client" deployment must either gate the bootstrap (config flag) or accept a minimal, scoped grant set; never grant blanket DDL to a read-only role.
- Remember: `CREATE TABLE IF NOT EXISTS` still requires the `CREATE` privilege in MySQL/MariaDB even when the table exists — the privilege check precedes the existence check.
- The Phase 1 plan's "no application code changes" premise was incorrect for read-only clients; it anticipated login-bookkeeping writes (Blocker 2) but missed the startup DDL (Blocker 1). Update the plan when this is implemented.

## Related

- `Operator/PHASE-1-tailscale-owner-acceptance-2026-06-02.md` (acceptance log)
- `Plans/Future/PHASE-1-owner-remote-access-tailscale.md` (deployment plan, step 5)
- [[owner-readonly-kpi-write-on-read]] (related Owner read-only write-on-read enforcement)
- [[mariadb-pure-client-server-architecture]] (architecture pattern)
