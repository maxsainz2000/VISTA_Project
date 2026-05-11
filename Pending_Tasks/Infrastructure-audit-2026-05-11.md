---
module: Infrastructure
audit-date: 2026-05-11
---

# VISTA Module Audit — Infrastructure

**Audit Date:** 2026-05-11  
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

**Total Plans:** 7  
**Completed:** 7 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.

### INFRA-05 — Sync Worker & Dual-Condition Connectivity Probe

**Status:** Completed

- [ ] INFRA-06: Implement actual MariaDB data transmission in `SyncOrchestrator.RunAsync` (replace the placeholder `MarkSyncedAsync` stub) *(likely stale — INFRA-06 implemented this)*
- [ ] Each module's `Data/` folder needs an `ISyncableRepository` implementation that appends to `Sync_Journal` on local writes
- [ ] `MainWindowViewModel` can subscribe to `DefaultNotificationService.SyncStatusChanged` to display sync status in the shell status bar

### INFRA-06 — MariaDB Central Schema & Reconciliation

**Status:** Completed

- [ ] Each module's `Data/` folder needs an `ISyncableRepository` implementation that appends to `Sync_Journal` on local writes
- [ ] Replace `appsettings.json` `Pwd=CHANGE_ME` with a user-level `appsettings.Production.json` outside the repo before any live deployment
- [ ] Upgrade `Pomelo.EntityFrameworkCore.MySql` to a 10.x release when available to resolve NU1608 cleanly
- [ ] Add `MainWindowViewModel` subscription to `DefaultNotificationService.SyncStatusChanged` for the shell status bar (noted in INFRA-05)
- [ ] Run `mariadb-init.sql` against a fresh MariaDB 11.4.x instance and verify acceptance criteria 2–3 manually

### INFRA-07 — Cross-Module VAT Event Payload Contracts

**Status:** Completed

- [ ] POS-14: Publish `SaleCompletedWithVatEvent` alongside `SaleCompletedEvent` from the POS transaction completion flow *(stale — completed by POS-14)*
- [ ] POS-13: Publish `ReceiptTamperDetectedEvent` from the receipt integrity check service *(stale — completed by POS-13)*
- [ ] Purchasing (follow-up): Publish `GoodsReceivedWithVatEvent` alongside `GoodsReceivedEvent` from the goods receipt confirmation flow *(genuinely pending — no Purchasing VAT event plan exists)*
- [ ] ACC-10: Implement handler consuming both `SaleCompletedWithVatEvent` and `GoodsReceivedWithVatEvent` for revenue/AP journal entries *(stale — completed by ACC-10)*
- [ ] ACC-11: Implement handler consuming both VAT events for the VAT summary ledger (three-bucket disclosure) *(stale — completed by ACC-11)*
- [ ] Accounting audit log handler: Implement handler consuming `ReceiptTamperDetectedEvent` *(genuinely pending — not found in any ACC summary)*

---

## Plans With No Progress File

None. All 7 plans have matching progress summaries.

---

## Amendments & Special Files

None found in the Infrastructure Progress folder.

---

## Summary & Recommendations

- **100% complete** — all 7 Infrastructure plans have `status: completed` progress summaries.
- **14 raw pending `[ ]` items** across INFRA-05, INFRA-06, INFRA-07 — but ~8 are stale (superseded by subsequent plans). Genuinely outstanding: `ISyncableRepository` per-module implementation, MariaDB production config, Pomelo upgrade, shell status bar sync indicator, Purchasing `GoodsReceivedWithVatEvent` publisher, and `ReceiptTamperDetectedEvent` accounting audit log handler.
- **Build note:** All Infrastructure plans built cleanly (✅ 0 errors, 0 warnings). The full-solution ❌ reported in ACC-10 was a pre-existing POS issue (`SemaphoreSlim` missing import), fixed in ACC-11.
- **Priority 1:** `ISyncableRepository` per-module implementation — without this, the entire INFRA-06 sync pipeline has no data to push. Affects Purchasing, Inventory, POS, and Accounting data/ folders.
- **Priority 2:** `ReceiptTamperDetectedEvent` accounting audit log handler — BIR compliance gap; the event is published by POS-13 but has no handler that records it in the Accounting ledger.
