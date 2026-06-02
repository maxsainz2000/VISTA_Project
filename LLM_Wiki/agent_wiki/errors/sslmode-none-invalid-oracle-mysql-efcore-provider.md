---
type: error-fix
module: MerchSys.App
agent: claude-code
date: 2026-06-02
tags: [mariadb, mysqlconnector, mysql-efcore, oracle-provider, sslmode, connection-string, enum-parse, dual-provider, runtime-error]
error-code: System.ArgumentException "Requested value 'None' was not found"
severity: runtime-error
status: resolved
---

## Problem

With a valid login, the app crashed (unhandled) on the **first DbContext creation after login** — navigating to the Owner dashboard:

```
System.ArgumentException: Requested value 'None' was not found.
   at System.Enum.Parse(Type enumType, String value, Boolean ignoreCase)
   at MySql.Data.MySqlClient.MySqlConnectionStringBuilder...
   at MySql.EntityFrameworkCore.Internal.MySQLOptions.GetConnectionSettings(MySQLOptionsExtension)
   at Microsoft.EntityFrameworkCore.DbContext..ctor(DbContextOptions options)
   at MerchSys.Inventory.Data.InventoryDbContext..ctor(...)
   at MerchSys.App.ViewModels.MainWindowViewModel.Navigate -> NavigateToDefault -> MainWindow_Loaded
```

The connection string contained `SslMode=None`.

## Root Cause

VISTA uses **two different MySQL client stacks against the same connection string**:
- **Raw `MySqlConnector`** for auth, schema bootstrap, and the health monitor.
- **Oracle `MySql.EntityFrameworkCore` (`UseMySQL`, which sits on `MySql.Data`)** for every module `DbContext`.

Their `MySqlSslMode` enums differ:
- `MySqlConnector`: `None`, `Preferred`, `Required`, `VerifyCA`, `VerifyFull` (**has `None`**).
- Oracle `MySql.Data`: `Disabled`, `Preferred`, `Required`, `VerifyCA`, `VerifyFull` (**no `None`** — uses `Disabled`).

So `SslMode=None` parses fine for the raw-connector paths (login worked!) but Oracle's `MySqlConnectionStringBuilder` does `Enum.Parse(GetType(MySqlSslMode), "None")` and throws `ArgumentException`. The failure is **masked until the first EF context is built** — which is after login, on dashboard navigation — making it look like "the app closes after I log in."

## Fix

Use a value **both** providers accept. `Preferred` is the safe choice (also `Required` once TLS is on):

```jsonc
// %LOCALAPPDATA%\VISTA\appsettings.Production.json
// before:  ...;SslMode=None;...
// after:   ...;SslMode=Preferred;...
```

Against a non-TLS server over the Tailscale/WireGuard tunnel, `Preferred` negotiates plaintext (no error), giving the same effective transport as the intended "no TLS". `Disabled` would suit Oracle but **breaks `MySqlConnector`** (it has no `Disabled`), so do not use it while both stacks share one string.

## Prevention

- In this codebase a connection string is consumed by **both** MySqlConnector and the Oracle EF provider — only use `SslMode` values valid for both: `Preferred`, `Required`, `VerifyCA`, `VerifyFull`. **Never `None` (Oracle rejects) and never `Disabled` (MySqlConnector rejects).**
- Deployment docs / templates that say "use `SslMode=None` for no-TLS" are wrong for this app — use `Preferred`.
- Symptom heuristic: "app closes right after a successful login" + `ArgumentException: Requested value '<x>' was not found` → an enum value in the connection string that one of the two providers doesn't recognize; the crash surfaces at first `DbContext` construction (post-login), not at startup.

## Related

- [[readonly-client-blocked-by-startup-ddl-bootstrap]] (the bootstrap gate that exposed this — once the read-only client got past startup, it reached the EF path)
- [[mariadb-pure-client-server-architecture]]
- `Operator/PHASE-1-tailscale-owner-acceptance-2026-06-02.md` (Fix C)
