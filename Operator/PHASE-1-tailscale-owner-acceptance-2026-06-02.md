---
title: "Phase 1 — Owner Remote Read Access via Tailscale — Acceptance Verification Log"
plan: Plans/Future/PHASE-1-owner-remote-access-tailscale.md
date: 2026-06-02
performed-by: Claude Code (operator-assisted)
client: DESKTOP-OUU3M8J (Owner laptop)
host: 100.76.155.51 (laptop-3hh6ohhe, XAMPP / MariaDB)
db-user: merchsys_owner (SELECT-only)
verdict: PARTIAL — steps 1–4 + 5a PASS; step 5b (in-GUI Owner login + rendered dashboard) BLOCKED, deferred to manual operator verification
---

# Phase 1 Tailscale Owner Remote Access — Acceptance Verification

## Summary

| # | Step | Result |
|---|------|--------|
| 1 | `%LOCALAPPDATA%\VISTA\appsettings.Production.json` exists, points to host | ✅ PASS |
| 2 | NTFS perms locked to current Windows user only | ✅ PASS |
| 3 | Tailscale reachability to host | ✅ PASS |
| 4 | Direct DB connectivity (read) | ✅ PASS |
| 5a | Live KPI / financial data readable as Owner (DB layer) | ✅ PASS |
| 5b | App GUI launch → Owner login → rendered dashboard | ❌ BLOCKED — deferred to manual operator verification |

Everything automatable from the Owner client passes, including the **live KPI/financial read path** over the tunnel. The app **cannot launch under the SELECT-only `merchsys_owner` grant** because of two write dependencies (detailed below); the rendered-dashboard check is therefore deferred to a manual operator run after remediation.

---

## Step details

### Step 1 — Production config ✅
`%LOCALAPPDATA%\VISTA\appsettings.Production.json` exists and contains:
```
ConnectionStrings:MerchSysCentral =
  Server=100.76.155.51;Port=3306;Database=merchsys_central;UserId=merchsys_owner;
  Password=***;SslMode=None;ConnectionTimeout=10;DefaultCommandTimeout=15;
```
Confirmed this is the key the live code path consumes (`DatabaseConfig.AddModuleDbContexts` → `GetConnectionString("MerchSysCentral")` + `UseMySQL`; also `Application.xaml.vb:63`, `ConnectionConfig.vb:21`). Overlay loads unconditionally via `ConnectionStringLoader.AddProductionOverlay()`. The app confirmed it at runtime: `Hosting environment: Production`.
- **Note:** `SslMode=None` is in use because MariaDB server TLS is not yet configured; WireGuard provides transport encryption. Switch to `SslMode=Required` once server TLS is enabled (plan step 7 / OWASP transport requirement).

### Step 2 — File permissions ✅
```
icacls "%LOCALAPPDATA%\VISTA\appsettings.Production.json"
C:\Users\Max Sainz\AppData\Local\VISTA\appsettings.Production.json DESKTOP-OUU3M8J\Max Sainz:(R)
Successfully processed 1 files; Failed processing 0 files
```
Inheritance disabled; sole ACE is the current user, read-only.

### Step 3 — Tailscale reachability ✅
```
tailscale ping 100.76.155.51
pong from laptop-3hh6ohhe (100.76.155.51) via 192.168.100.165:41641 in 11ms
```
Direct WireGuard path (not relayed), 11 ms RTT.

### Step 4 — Direct DB connectivity ✅
```
mysql -h 100.76.155.51 -u merchsys_owner -p*** merchsys_central -e "SELECT COUNT(*) FROM Sys_UserAccounts;"
COUNT(*)
3
```
Auth + SELECT + transport over Tailscale all confirmed.

### Step 5a — Live KPI / financial data readable as Owner ✅
The Owner dashboard's underlying aggregations, run directly as `merchsys_owner`, returned live data:

| KPI | Value |
|---|---|
| Net Revenue (recorded) | PHP 28,090.00 |
| Gross Profit (recorded) | PHP 4,090.00 |
| Total Expenses (recorded) | PHP 98,700.00 |
| Output VAT (sales) | PHP 133.93 |
| Sales Transactions / Total | 6 txns / PHP 28,090.00 |
| Official Receipts issued | 6 |
| Outstanding Utang (credit) | PHP 2,500.00 |
| Active Products | 20 |
| Inventory Valuation (FIFO on hand) | PHP 47,800.00 |

The data the KPI dashboard and financial reports consume is reachable and live for the Owner account over the tunnel.

### Step 5b — App GUI launch → Owner login → rendered dashboard ❌ BLOCKED
The app builds, loads the Production config (`Hosting environment: Production`), connects to MariaDB, then **aborts at startup** with the *"VISTA — Fatal"* dialog (`Application.xaml.vb:224` — "Cannot initialize database schema") **before the login screen**. Owner login and the rendered dashboard could not be exercised.

---

## Blockers (root cause)

### Blocker 1 — Startup schema bootstrap requires DDL (NOT anticipated by the plan)
`MariaDbSchemaInitializer.Initialize` runs on **every** launch and its first action (`EnsureSchemaMigrationsTable`) executes:
```sql
CREATE TABLE IF NOT EXISTS `__SchemaMigrations` ( ... );
```
MariaDB checks the `CREATE` privilege **before** the `IF NOT EXISTS` existence check, so this fails even though the table already exists. Reproduced directly:
```
ERROR 1142 (42000): CREATE command denied to user 'merchsys_owner'@'desktop-ouu3m8j.tail74f74a.ts.net.'
                    for table `merchsys_central`.`___ddl_probe`
```
Owner grants confirmed minimal:
```
GRANT USAGE ON *.* TO `merchsys_owner`@`%`
GRANT SELECT ON `merchsys_central`.* TO `merchsys_owner`@`%`
```
**No login-time grant fixes this** — it fires before authentication. After this one DDL statement, the rest of the bootstrap is read-only for an already-provisioned central DB (migration hashes already recorded; `Sys_UserAccounts`/Developer seed counts > 0 → early return).

> The deployment plan states "no application code changes" and did not account for this unconditional startup DDL. That premise needs revisiting (see Remediation).

### Blocker 2 — Login bookkeeping requires UPDATE (anticipated by the plan, step 5 lines 91–100)
On a **successful** auth, `AuthenticationService.ClearFailedAttempts` runs:
```sql
UPDATE Sys_UserAccounts SET FailedLoginAttempts=0, LockedUntil=NULL, ModifiedAt=@now WHERE Id=@id;
```
This `UPDATE` is denied to the SELECT-only account. (Failed-attempt path `BumpFailedAttempts` and Owner self-service password change `ChangePasswordCore` are also UPDATEs.) This is exactly the "login bookkeeping" write the plan predicted.

Additional note: the seeded `owner` app account has `LastPasswordChangeAt = NULL`, so a first login triggers the DA6 forced password-change flow (`UPDATE PasswordHash, LastPasswordChangeAt`) — another write gated by the same grant.

---

## Remediation (to be applied by the host operator, then re-verify manually)

**Blocker 2 — minimal write grant (plan-sanctioned defense-in-depth):** run on the host as root:
```sql
GRANT UPDATE (FailedLoginAttempts, LockedUntil, ModifiedAt, LastPasswordChangeAt, PasswordHash)
  ON merchsys_central.Sys_UserAccounts TO 'merchsys_owner'@'%';
FLUSH PRIVILEGES;
```
App-layer read-only enforcement (`RoleGuardInterceptor`, OWASP DA5) still blocks all business-data writes; this grant only permits the auth/self-service columns the login flow needs.

**Blocker 1 — prefer an app-side gate over granting DDL.** Granting `CREATE`/`ALTER` to the Owner account would defeat the SELECT-only least-privilege model. Recommended (deferred to a code session): make `MariaDbSchemaInitializer.Initialize` skip the bootstrap for read-only clients — e.g. a `"Schema": { "RunBootstrap": false }` flag in the Owner's `appsettings.Production.json`, or catch error 1142 on the `__SchemaMigrations` ensure and continue when the schema already exists. This contradicts the plan's "no application code changes" premise; the plan should be updated to record the startup-DDL blocker.

**TLS:** when MariaDB server TLS is enabled, change the Owner config to `SslMode=Required` (plan step 7).

---

## Manual operator verification still required (step 5b)

After the remediation above, an operator must, **at the Owner laptop off-LAN (phone tether), Tailscale up**:
1. Launch VISTA (`dotnet run --project WPF_Applications/MerchSys/src/MerchSys.App`).
2. Log in as `owner` (password `Vista2026!`; complete the DA6 first-login password change).
3. Confirm the **Owner KPI Dashboard** and **financial reports** render with **live** data (cross-check against the Step 5a values above and the local-host `Operator/owner-verification-checklist.md` Part 3 expectations).
4. Confirm an Owner write attempt is rejected with the standard message (acceptance criterion, plan line 143).

This GUI step is not automatable from the agent environment and was intentionally deferred (operator decision, 2026-06-02).

---

## Why step 5b cannot be completed from the Owner client (decisive findings, 2026-06-02)

Two independent facts make an in-GUI Owner login impossible from this laptop without the host operator:

1. **Remote `root` is denied** — cannot add the Blocker-2 grant from the client:
   ```
   mysql -h 100.76.155.51 -u root merchsys_central -e "SELECT CURRENT_USER();"
   ERROR 1045 (28000): Access denied for user 'root'@'desktop-ouu3m8j.tail74f74a.ts.net.' (using password: NO)
   ```
   The minimal `UPDATE` grant must be applied **on the host** by an account with `GRANT` rights. Without it, login's `UPDATE Sys_UserAccounts` fails even if the startup bootstrap is gated — so a code-only fix from the client is insufficient.

2. **The `owner` app password is already rotated and is not known to the agent** — `Sys_UserAccounts` shows the owner account (`Id=2`, `Role=2`) with `LastPasswordChangeAt = 2026-06-01 05:53:33` (not NULL). The DA6 first-login change already occurred, so the seed password `Vista2026!` is no longer valid. Only the operator holds the current password; the agent cannot authenticate as Owner.

**Conclusion:** step 5b is an operator-side task by necessity. It requires the host operator to (1) apply the `UPDATE` grant on `Sys_UserAccounts`, (2) decide on gating the startup schema bootstrap for read-only clients, and (3) perform the GUI login with the current owner password and confirm the dashboard renders live data.

## Session notes
- Stuck *"VISTA — Fatal"* process and the `dotnet run` host were terminated after diagnosis. No code or DB grants were changed during this run.
- The agent verified everything automatable (steps 1–4, 5a) and exhausted all client-side paths to 5b; the remainder is blocked on host `root` access and the operator-held owner password.
