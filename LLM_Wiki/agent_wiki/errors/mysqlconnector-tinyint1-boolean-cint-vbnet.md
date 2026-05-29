---
type: error-fix
module: MerchSys.App
agent: claude-code
date: 2026-05-28
tags: [mariadb, mysqlconnector, tinyint, boolean, vb-net, authentication, logic-bug, runtime-error]
error-code: (silent logic bug)
severity: logic-bug
---

## Problem

Login always returned "Invalid credentials" even with correct credentials. `FailedLoginAttempts` stayed at 0 after every attempt — meaning `BumpFailedAttempts` was never called, which only happens when the user lookup returns `Nothing`.

Discovered in manager verification testing (2026-05-28). `ReadUserByUsername` was returning a `UserAccount` object, but the `IsActive` field was evaluating to `False` even though the DB row has `IsActive=1`.

## Root Cause

**MySqlConnector maps `TINYINT(1)` columns to `Boolean`** (not `Integer`). So `rdr("IsActive")` returns `True` (VB.NET `Boolean`), not `1` (Integer).

In VB.NET, `CInt(True) = -1` (because VB.NET's `True` is `-1`). So:

```vb
u.IsActive = CInt(rdr("IsActive")) = 1
' CInt(True) = -1
' -1 = 1 → False ← IsActive is always False!
```

The same trap applies to any `TINYINT(1)` column read via `CInt()` in VB.NET with MySqlConnector.

## Fix

Use `Convert.ToBoolean()` for boolean columns, and `Convert.ToInt32()` for all integer columns to handle both `Boolean` and numeric return types safely:

```vb
' Before (broken)
u.IsActive = CInt(rdr("IsActive")) = 1
u.Id = CInt(rdr("Id"))
u.Role = CType(CInt(rdr("Role")), UserRole)
u.FailedLoginAttempts = CInt(rdr("FailedLoginAttempts"))

' After (fixed)
u.IsActive = Convert.ToBoolean(rdr("IsActive"))
u.Id = Convert.ToInt32(rdr("Id"))
u.Role = CType(Convert.ToInt32(rdr("Role")), UserRole)
u.FailedLoginAttempts = Convert.ToInt32(rdr("FailedLoginAttempts"))
```

**File:** `WPF_Applications/MerchSys/src/MerchSys.App/Services/IAuthenticationService.vb` — `ReadUserByUsername` method.

## Prevention

- **Never use `CInt()` on columns that might be `TINYINT(1)`.** MySqlConnector returns these as `Boolean`.
- For boolean DB columns (`IsActive`, `IsDeleted`, soft-delete flags): always use `Convert.ToBoolean(rdr("col"))`.
- For integer DB columns in raw ADO.NET reader loops: always use `Convert.ToInt32(rdr("col"))` instead of `CInt(rdr("col"))`.
- `CInt(True) = -1` in VB.NET is a known language trap — `True` maps to `-1`, not `1`.
- This affects ALL raw `MySqlDataReader` loops in the codebase, not just authentication.

## Related

- [[efcore-vbnet-tolistasync-entity-empty]] — related MySqlConnector/EF Core materialization issues
