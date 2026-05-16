---
module: Infrastructure
audit-date: 2026-05-15
---

# VISTA Module Audit — Infrastructure

**Audit Date:** 2026-05-15  
**Plans Folder:** `Plans/VISTA_Modules/Infrastructure/`  
**Progress Folder:** `Progress/VISTA_Modules/Infrastructure/`

---

## Mirror Check Summary

| Plan ID | Plan Title | Status |
|---------|-----------|--------|
| INFRA-01 | Solution Scaffold | ✅ Completed |
| INFRA-02 | Shared Kernel | ✅ Completed |
| INFRA-03 | Database Contexts | ✅ Completed |
| INFRA-04 | MediatR Event Bus | ✅ Completed |
| INFRA-05 | Sync Worker & Dual-Condition Connectivity Probe | ✅ Completed |
| INFRA-06 | MariaDB Central Schema & Reconciliation | ✅ Completed |
| INFRA-07 | Cross-Module VAT Event Payload Contracts | ✅ Completed |
| INFRA-08 | MariaDB Receipt Integrity Triggers | ✅ Completed |
| INFRA-09 | ISyncableRepository Per-Module Implementation | ✅ Completed |
| INFRA-10 | Sync Status Shell Indicator | ✅ Completed |
| INFRA-11 | Production Deployment Configuration | ✅ Completed |

**Total Plans:** 11  
**Completed:** 11 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.

### INFRA-05 — Sync Worker & Dual-Condition Connectivity Probe

**Status:** Completed

- [ ] INFRA-06: Implement actual MariaDB data transmission in `SyncOrchestrator.RunAsync` (replace the placeholder `MarkSyncedAsync` stub)
- [ ] Each module's `Data/` folder needs an `ISyncableRepository` implementation that appends to `Sync_Journal` on local writes
- [ ] `MainWindowViewModel` can subscribe to `DefaultNotificationService.SyncStatusChanged` to display sync status in the shell status bar

### INFRA-06 — MariaDB Central Schema & Reconciliation

**Status:** Completed

- [ ] Each module's `Data/` folder needs an `ISyncableRepository` implementation that appends to `Sync_Journal` on local writes, making journal entries available to `SyncOrchestrator`
- [ ] Replace `appsettings.json` `Pwd=CHANGE_ME` with a user-level `appsettings.Production.json` outside the repo before any live deployment
- [ ] Upgrade `Pomelo.EntityFrameworkCore.MySql` to a 10.x release when available to resolve NU1608 cleanly
- [ ] Add `MainWindowViewModel` subscription to `DefaultNotificationService.SyncStatusChanged` for the shell status bar (noted in INFRA-05)
- [ ] Run `mariadb-init.sql` against a fresh MariaDB 11.4.x instance and verify acceptance criteria 2–3 manually

### INFRA-07 — Cross-Module VAT Event Payload Contracts

**Status:** Completed

- [ ] POS-14: Publish `SaleCompletedWithVatEvent` alongside `SaleCompletedEvent` from the POS transaction completion flow
- [ ] POS-13: Publish `ReceiptTamperDetectedEvent` from the receipt integrity check service
- [ ] Purchasing (follow-up): Publish `GoodsReceivedWithVatEvent` alongside `GoodsReceivedEvent` from the goods receipt confirmation flow
- [ ] ACC-10: Implement handler consuming both `SaleCompletedWithVatEvent` and `GoodsReceivedWithVatEvent` for revenue/AP journal entries
- [ ] ACC-11: Implement handler consuming both VAT events for the VAT summary ledger (three-bucket disclosure)
- [ ] Accounting audit log handler: Implement handler consuming `ReceiptTamperDetectedEvent`

### INFRA-08 — MariaDB Receipt Integrity Triggers

**Status:** Completed

- [ ] Schema alignment pass: add `Status`, `IssuedAt`, `IntegrityHash` columns to central `Pos_OfficialReceipts` if the sync worker requires them (coordinate with INFRA-05 sync wiring)
- [ ] Future remediation: replace the INFRA-06 triggers (`trg_Pos_ReceiptIntegrity_NoUpdate`, etc.) with versions using `DEFINER = <admin>` (not anonymous default) once a formal DB admin account is established

### INFRA-09 — ISyncableRepository Per-Module Implementation

**Status:** Completed

- [ ] Integration plan: migrate each module's service write paths from `_context.SaveChangesAsync()` to `_repository.SaveChangesWithJournalAsync()`. Suggested plan ID: `INT-06` or a dedicated `INFRA-10`. High file count expected (mechanical but broad).
- [ ] Consumer-side `ISyncableRepository` (non-generic) implementations — needed so `SyncOrchestrator` can iterate pending `Sync_Journal` entries per module. Currently `SyncOrchestrator` resolves `IEnumerable(Of ISyncableRepository)` (non-generic) which is not yet backed by any registered implementation.

### INFRA-10 — Sync Status Shell Indicator

**Status:** Completed

- [ ] Runtime smoke-test: launch the app and confirm the indicator renders in the status bar with no binding errors.
- [ ] Confirm `Dispose` is invoked on shutdown (requires a test or debug trace on application exit).

### INFRA-11 — Production Deployment Configuration

**Status:** Completed

- [ ] Pomelo 10.x bump — defer until `Pomelo.EntityFrameworkCore.MySql` 10.x is published on NuGet.org; remove NU1608 suppression from `Directory.Build.props` at that time.
- [ ] Live operator walkthrough against a real MariaDB 11.4.x instance to validate the INFRA-06 acceptance criteria steps 2–3 documented in the runbook.

---

## Plans With No Progress File

*None — all 11 plans have corresponding progress summaries.*

---

## Amendments & Special Files

*None found in the Infrastructure Progress folder.*

---

## Summary & Recommendations

- **100% completion** — all 11 Infrastructure plans have `status: completed` summaries.
- **22 pending tasks** remain across 7 summaries (INFRA-05 through INFRA-11); none are blockers for plan coverage, but several are cross-module integration obligations.
- **Highest priority:** INFRA-09's two items (ISyncableRepository write-path migration and non-generic consumer registration) are prerequisites for the `SyncOrchestrator` to function at runtime — this should be addressed in an INT-phase plan before production handoff.
- **INFRA-06 / INFRA-11:** The MariaDB password placeholder (`Pwd=CHANGE_ME`) and Pomelo 10.x NU1608 suppression are both deployment-gate items that must be resolved before any live instance is stood up.
- **INFRA-07:** Six downstream publisher/handler obligations are tracked here; most are already covered by POS-13/14 and ACC-10/11/15 plans — verify those summaries' own pending tasks are cleared before treating INFRA-07 as fully runtime-ready.
