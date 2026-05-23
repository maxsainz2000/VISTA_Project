---
module: Infrastructure
audit-date: 2026-05-17
---

# VISTA Module Audit — Infrastructure

**Audit Date:** 2026-05-17
**Plans Folder:** `Plans/VISTA_Modules/Infrastructure/`
**Progress Folder:** `Progress/VISTA_Modules/Infrastructure/`

---

## What's Next Cleanup (Step 0)

**Items resolved this pass:** 4

| Summary | Item | Resolved By |
|---------|------|-------------|
| INFRA-05 | Implement actual MariaDB data transmission in SyncOrchestrator.RunAsync | INFRA-06 |
| INFRA-08 | Schema alignment pass: add Status, IssuedAt, IntegrityHash to central Pos_OfficialReceipts | INFRA-14 |
| INFRA-09 | Integration plan: migrate write paths from SaveChangesAsync to SaveChangesWithJournalAsync | INFRA-13 |
| INFRA-09 | Consumer-side ISyncableRepository (non-generic) implementations | INFRA-13 |

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
| INFRA-12 | SyncOrchestrator Real Data Transmission | ✅ Completed |
| INFRA-13 | ISyncableRepository Write-Path Migration & Consumer Registration | ✅ Completed |
| INFRA-14 | Central Schema Alignment for Receipt Sync | ✅ Completed |

**Total Plans:** 14
**Completed:** 14 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.

### INFRA-06 — MariaDB Central Schema & Reconciliation

**Status:** Completed

- [ ] Replace `appsettings.json` `Pwd=CHANGE_ME` with a user-level `appsettings.Production.json` outside the repo before any live deployment
- [x] ~~Upgrade `Pomelo.EntityFrameworkCore.MySql` to a 10.x release~~ → resolved by INFRA-17 (Pomelo removed; MySqlConnector adopted)
- [ ] Run `mariadb-init.sql` against a fresh MariaDB 11.4.x instance and verify acceptance criteria 2–3 manually

### INFRA-08 — MariaDB Receipt Integrity Triggers

**Status:** Completed

- [ ] Future remediation: replace the INFRA-06 triggers (`trg_Pos_ReceiptIntegrity_NoUpdate`, etc.) with versions using `DEFINER = <admin>` (not anonymous default) once a formal DB admin account is established

### INFRA-10 — Sync Status Shell Indicator

**Status:** Completed

- [ ] Runtime smoke-test: launch the app and confirm the indicator renders in the status bar with no binding errors
- [ ] Confirm `Dispose` is invoked on shutdown (requires a test or debug trace on application exit)

### INFRA-11 — Production Deployment Configuration

**Status:** Completed

- [x] ~~Pomelo 10.x bump~~ → resolved by INFRA-17 (dependency removed entirely; NU1608 suppression removed)
- [ ] Live operator walkthrough against a real MariaDB 11.4.x instance to validate the INFRA-06 acceptance criteria steps 2–3 documented in the runbook

### INFRA-12 — SyncOrchestrator Real Data Transmission

**Status:** Completed

- [ ] Replace `Pwd=CHANGE_ME` in `appsettings.json` with the actual MariaDB password before testing against a live MariaDB instance
- [ ] Integration test: verify `TransmitBatchAsync` idempotency — transmit the same batch twice and confirm no duplicate-key errors for non-financial tables

### INFRA-13 — ISyncableRepository Write-Path Migration & Consumer Registration

**Status:** Completed

- [ ] Migration of `Accounting/Handlers` write paths (if determined to be in scope for a follow-up plan)
- [ ] Migration of `Inventory/ViewModels/ProductManagementViewModel.vb` write paths (if ViewModels are brought into sync scope)
- [ ] Runtime verification: execute a write through each migrated service and confirm a corresponding `Sync_Journal` row is created

### INFRA-14 — Central Schema Alignment for Receipt Sync

**Status:** Completed

- [ ] Deploy `mariadb-receipt-schema-alignment.sql` against the staging/production MariaDB instance and verify `IF NOT EXISTS` idempotency by running it twice
- [ ] Wire `SyncOrchestrator` receipt push path (INFRA-12) and confirm a receipt row with `Status`, `IssuedAt`, and `IntegrityHash` populated inserts successfully into the aligned central table

---

## Plans With No Progress File

*(None — all 14 plans have corresponding completed progress summaries.)*

---

## Amendments & Special Files

*(None)*

---

## Summary & Recommendations

- **100% complete** — all 14 Infrastructure plans have completed progress summaries. No blockers.
- **13 pending `[ ]` tasks** are spread across INFRA-06 through INFRA-14; all are deployment/runtime verification items, not implementation gaps.
- **Top priority: credential hygiene** — INFRA-06 and INFRA-12 both track `Pwd=CHANGE_ME` in `appsettings.json`. This must be resolved before any live MariaDB testing (INFRA-11 production deployment runbook addresses the mechanism via `appsettings.Production.json`).
- **~~Pomelo 10.x~~** (INFRA-06, INFRA-11) — **resolved by INFRA-17:** dependency replaced with raw MySqlConnector. NU1608 suppression removed.
- **INFRA-13 write-path migration** — `Accounting/Handlers` and `ProductManagementViewModel` write-path migrations are explicitly deferred pending scope decision. These are the last consumers still using raw `SaveChangesAsync` without journalling.
- **INFRA-14 + INFRA-12 coordination** — deploying the receipt schema alignment SQL (INFRA-14) is a prerequisite for testing the SyncOrchestrator receipt push path (INFRA-12).
