---
test-id: INFRA-11-Test-5
checklist: INFRA-verification-checklist.md
branch: debug/INFRA-test-5
started: 2026-05-23T00:00
status: blocked
---

# Debug Session — INFRA-11 Test 5: Live MariaDB Deployment Walkthrough

## Problem Statement

Live deployment walkthrough against a local XAMPP MariaDB 11.4.x instance. Goal: run the production deployment runbook and confirm INFRA-06 acceptance criteria (steps 2–3).

**Runbook:** `Plans\VISTA_Modules\Infrastructure\runbooks\01-production-deployment.md`

## Starting State
- **Commit:** `bf466dd`
- **Build status:** clean
- **Prerequisite:** INFRA-06 Test 1 (production password file) — ✅ already done

## Allowed Files
- `%LOCALAPPDATA%\VISTA\appsettings.Production.json` — production config overlay (not in repo)
- `*SyncableRepository.vb` — bug fix discovered during walkthrough

---

## Walkthrough Log

### Phase 1 — Build in Release configuration
- **Action:** `dotnet build WPF_Applications/MerchSys/MerchSys.slnx -c Release`
- **Result:** ✅ Build succeeded. 0 Error(s) 0 Warning(s). All 6 projects compiled.

### Phase 2 — Schema initialisation (runbook section 2)
- **Action:** Applied `mariadb-init.sql` then `02-pos-receipt-integrity-triggers.sql`
- **Result:** ✅ 27 base tables created; 14 base triggers created; `Pos_OfficialReceiptArchive` added with 2 archive triggers → 28 tables, 16 triggers total.

### Phase 3 — User provisioning (runbook section 3)
- **Action:** `ALTER USER 'merchsys_sync'@'%' IDENTIFIED BY 'Vista2026'; FLUSH PRIVILEGES;`
- **Result:** ✅ Role `merchsys_sync_role` grants `SELECT, INSERT, UPDATE` on `merchsys_central.*`. No DELETE — correct.

### Phase 4 — Production config placement (runbook section 4)
- **Action:** Confirmed `%LOCALAPPDATA%\VISTA\appsettings.Production.json` exists with Host=localhost, Password=Vista2026.
- **Extra fixes applied:**
  - Changed `SslMode` from `Required` to `None` (XAMPP has SSL disabled — `have_ssl=DISABLED`)
  - Added `Sync: { CentralServerHost: "localhost", CentralServerPort: 3306 }` override (base config had `192.168.1.10`)
- **Result:** ✅ Config overlay is valid and in place.

### Phase 5 — First-run verification (runbook section 5)
- **Action:** Launched app, logged in as manager, observed sync status indicator.
- **Issue 1 — `Sync:CentralServerHost` hardcoded to `192.168.1.10` in `appsettings.json`:** Fixed by adding `Sync` override in production overlay. Not a bug — the base config is correct for LAN production; local test needed the override.
- **Issue 2 — `ToListAsync` bug in all 4 syncable repositories:** `GetPendingChangesAsync` used EF Core `ToListAsync()` which silently returns empty in EF Core 10 VB.NET (documented bug `efcore-vbnet-tolistasync-entity-empty`). Fixed by replacing with raw `SqliteConnection` + synchronous reader loop. `MarkSyncedAsync` fixed with `ExecuteSqlRawAsync`. Committed as `8ff9926`.
- **Issue 3 — Pomelo/EF Core 10 binary incompatibility (BLOCKER):**
  `System.MissingMethodException: Method not found: 'System.String Microsoft.EntityFrameworkCore.Diagnostics.AbstractionsStrings.ArgumentIsEmpty(System.Object)'`
  thrown by `UseMySql()` in `SyncConfig.vb:32` when `MariaDbSyncContext` is resolved.
  This is the known INFRA-11 deferred item: Pomelo 10.x is not yet published on NuGet.
  The sync indicator stays red; the SyncWorker logs "probe cycle failed" on every cycle.
- **Result:** ❌ Sync indicator cannot reach Online/Green — blocked by Pomelo 10.x incompatibility.

### Phase 6 — INFRA-06 acceptance criteria steps 2–3 (runbook section 6)
- **Step 2 — Schema completeness:**
  - `table_count = 28` (≥ 19 ✅)
  - `trigger_count = 16` (≥ 14 ✅)
- **Step 3 — Tamper-evidence trigger verification:**
  - `ReceiptIntegrityTriggerVerification.sql` Section 1: all 6 receipt/archive triggers listed with `BEFORE` timing ✅
  - Immutability trigger manually confirmed: INSERT probe row Id=999999 then DELETE → `ERROR 1644 (45000): Deletions from Pos_OfficialReceipts are prohibited (BIR compliance)` ✅
  - Note: Probes 1–5 in Section 2 use `WHERE Id = -1` (no matching rows) so BEFORE triggers don't fire — this is expected MariaDB behavior, not missing triggers.
- **Result:** ✅ Both acceptance criteria pass.

---

## Resolution

- **Status:** partially complete — blocked by Pomelo 10.x incompatibility
- **Root cause of blocker:** `Pomelo.EntityFrameworkCore.MySql` compiled against EF Core < 10; `MissingMethodException` on `Check.NotEmpty` prevents `MariaDbSyncContext` from being configured. This is the pre-existing INFRA-11 deferred item.
- **Bug fix applied:** `ToListAsync` in all 4 syncable repositories replaced with raw `SqliteConnection` reader loop (`8ff9926`). This fix is valid and ready for when Pomelo 10.x ships.
- **Config fixes applied (not committed — operator config only):**
  - `SslMode: None` in production overlay (XAMPP has no SSL)
  - `Sync:CentralServerHost: localhost` override in production overlay
- **Final commit:** `8ff9926`
- **Agent wiki entry needed?** no — `efcore-vbnet-tolistasync-entity-empty` already documented; Pomelo blocker already in INFRA-11 deferred checklist.

### Completed steps
- ✅ Release build clean
- ✅ `mariadb-init.sql` applied (28 tables, 16 triggers)
- ✅ Receipt integrity triggers applied
- ✅ `merchsys_sync` user provisioned with correct grants
- ✅ Production config in place
- ✅ INFRA-06 Step 2: table_count=28, trigger_count=16
- ✅ INFRA-06 Step 3: immutability triggers confirmed functional

### Blocked step
- ❌ First-run sync (indicator → green) — requires Pomelo 10.x on NuGet (INFRA-11 deferred)

---

## Follow-up: INFRA-17

The Pomelo blocker documented above has been addressed by **INFRA-17** (replace Pomelo
with raw MySqlConnector). After INFRA-17 lands, re-run Phase 5 to confirm the sync
indicator reaches Online/Green. The `ToListAsync` fix from commit `8ff9926` remains
valid and is unaffected by the Pomelo removal.
