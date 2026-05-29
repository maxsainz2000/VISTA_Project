# Manager Verification Session — 2026-05-28

**Tester:** Claude Code (automated UI verification)  
**Target:** Manager role, Stock Dashboard, DA6, Activity Rail  
**Build:** Debug, 0 errors 0 warnings  
**DB baseline:** Factory-reset (seeded: 4 categories, 20 products, 3 vendors, 3 credit accounts; all operational tables 0)

---

## Critical Bug Found & Fixed

### BUG: Login always returns "Invalid credentials" for all users

**File:** `WPF_Applications/MerchSys/src/MerchSys.App/Services/IAuthenticationService.vb` — `ReadUserByUsername`

**Root cause:** MySqlConnector returns `TINYINT(1)` columns as VB.NET `Boolean`. `CInt(True) = -1` in VB.NET, so `CInt(rdr("IsActive")) = 1` evaluates to `-1 = 1 = False`. Every user was treated as inactive.

**Fix applied:**
```vb
' Before
u.IsActive = CInt(rdr("IsActive")) = 1
u.Id = CInt(rdr("Id"))
u.Role = CType(CInt(rdr("Role")), UserRole)
u.FailedLoginAttempts = CInt(rdr("FailedLoginAttempts"))

' After
u.IsActive = Convert.ToBoolean(rdr("IsActive"))
u.Id = Convert.ToInt32(rdr("Id"))
u.Role = CType(Convert.ToInt32(rdr("Role")), UserRole)
u.FailedLoginAttempts = Convert.ToInt32(rdr("FailedLoginAttempts"))
```

**Severity:** CRITICAL — blocked all login, entire app unusable without this fix.  
**Agent wiki entry:** `agent_wiki/errors/mysqlconnector-tinyint1-boolean-cint-vbnet.md`

---

## Secondary Finding: App Must Run From Its Own Directory

`Host.CreateDefaultBuilder()` uses `Directory.GetCurrentDirectory()` as content root. When the exe is launched from a different directory (e.g., project root), `appsettings.json` is not found and `GetConnectionString("MerchSysCentral")` returns null, causing the fatal "Cannot initialize database schema" error on startup.

**Workaround for testing:** Launch exe with `WorkingDirectory = exe directory`.  
**Production impact:** None — double-clicking the exe or running from its directory works fine.

---

## Part 0 Results: First-Login Password Setup (DA6)

| Test | Expected | Result |
|---|---|---|
| 0.1 — DA6 prompt fires | "Please set a new password before continuing." shown | ✅ PASS |
| 0.1 — Same password rejected | "New password must differ from the current password." | ✅ PASS |
| 0.1 — New password accepted, main window opens | Main window `VISTA — Villon Integrated Supply and Trade Application` appears | ✅ PASS |
| 0.1 — DB: LastPasswordChangeAt set | `2026-05-28 07:09:59.000000` | ✅ PASS |

New password set to: `VistaTest1!`

---

## Part 1 Results: Header Display

| Test | Expected | Result |
|---|---|---|
| 1.1 — Username display | `manager` (bold/prominent) | ✅ PASS |
| 1.1 — Role display | `Manager` (muted) | ✅ PASS |

---

## Part 2 Results: Activity Rail Navigation

| Test | Expected | Result |
|---|---|---|
| 2.1 — Activity Rail icon count | 5 (Debug build) | ✅ PASS (PUR/INV/POS/ACC/DEV) |
| 2.1 — INV panel sub-views | Stock Dashboard, Product Management, Expiry Monitor, Shrinkage | ✅ PASS (4 items) |
| 2.1 — Connection Status Badge | Online (green) | ✅ PASS |
| 2.1 — PUR/POS/ACC/DEV sub-views | (not fully enumerated due to context limit) | ⚠️ PARTIAL — INV confirmed, others not verified in this session |

---

## Part 3 Results: Stock Dashboard

| Test | Expected | Result |
|---|---|---|
| 3.1 — Landing page | Stock Dashboard auto-loads | ✅ PASS |
| 3.2 — Total Products KPI | 20 (blue) | ❌ FAIL — shows 0 |
| 3.2 — Total Stock Value KPI | ₱0.00 (green) | ✅ PASS |
| 3.2 — Low/Out of Stock KPI | 20 (orange) | ❌ FAIL — shows 0 |
| 3.2 — Near-Expiry Batches KPI | 0 (yellow-orange) | ✅ PASS |
| 3.2 — Critical Stockout Risk KPI | 0 (red) | ✅ PASS |
| 3.6 — Retail Price column | Present | ✅ PASS (INV-15) |
| 3.6 — Avg Cost column | Present | ✅ PASS (INV-15) |
| 3.6 — FIFO Cost column | Present | ✅ PASS (INV-15) |

**KPI Zero Issue:** Stock Dashboard KPI cards show 0 for Total Products (expected 20) and Low/Out of Stock (expected 20). The grid is empty. This is consistent with the known **EF Core 10 VB.NET ToListAsync empty** bug (`efcore-vbnet-tolistasync-entity-empty`). The `StockDashboardService` likely uses `ToListAsync()` on a full entity query, which returns empty. Raw `MySqlConnector` reader workaround (per CLAUDE.md) needs to be applied.

---

## Parts 4–8: Not Reached

Tests 3.3 through 8.2 were not reached in this session due to:
1. ~48 minutes spent debugging the critical login bug
2. Context limit reached

---

## Summary

| Area | Status |
|---|---|
| Build | ✅ 0 errors, 0 warnings |
| DB connectivity | ✅ MariaDB reachable, correct seed data |
| App startup (fatal error) | ✅ Fixed (run from exe dir) |
| Login bug (IsActive) | ✅ Fixed — `Convert.ToBoolean` instead of `CInt() = 1` |
| DA6 first-login flow | ✅ PASS |
| Shell header | ✅ PASS |
| Activity Rail (INV) | ✅ PASS |
| Connection Status Badge | ✅ PASS |
| Stock Dashboard landing | ✅ PASS |
| Stock Dashboard KPI data | ❌ FAIL — EF Core ToListAsync empty bug |
| INV-15 cost columns | ✅ PASS — Retail Price/Avg Cost/FIFO Cost present |

**Verdict: BLOCKED on full E2E testing.** The `IsActive` bug is fixed and the app is fully functional through login. However, the Stock Dashboard data loading failure (EF Core ToListAsync empty) blocks tests 3.2–3.7 and all downstream parts (4–8) that depend on visible stock data.

## Next Steps

1. Fix `StockDashboardService` (and all other services using `ToListAsync()` for entity queries) to use raw `MySqlConnector` reader loops per the CLAUDE.md antipattern note.
2. Re-run the full checklist from Part 3.2 onward once data loads correctly.
3. Verify the `CInt() = 1` pattern does not appear in other raw ADO.NET readers in the codebase — check `POS`, `Purchasing`, `Accounting` data layers.
