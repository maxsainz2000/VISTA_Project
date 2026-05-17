---
generated: 2026-05-16
source: Module audits dated 2026-05-15
---

# VISTA Master Pending Task Tracker

**Generated:** 2026-05-16  
**Source:** Module audits dated 2026-05-15  
**All modules:** 84/84 plans completed (100%)

---

## Summary

| Module | New Plans | Verification Items | Total Pending |
|---|---|---|---|
| Accounting | 3 (ACC-16, 17, 18) | 9 | 12 |
| Infrastructure | 3 (INFRA-12, 13, 14) | 7 | 10 |
| Integration | 0 | 7 (+INT-12 checklist) | 7 |
| Inventory | 0 | 0 | 0 |
| POS | 0 | 14 | 14 |
| Purchasing | 1 (PUR-15) | 2 | 3 |
| **TOTAL** | **7** | **39** | **46** |

---

## 🔴 Critical Items

These must be resolved before production deployment.

### INFRA-13 — ISyncableRepository Write-Path Migration & Consumer Registration
- **Module:** Infrastructure
- **Plan:** [13-syncable-repository-migration.md](../VISTA_Modules/Infrastructure/13-syncable-repository-migration.md)
- **Impact:** Sync pipeline is inert — `Sync_Journal` is never populated at runtime. Without this, INFRA-05/06/12 push nothing to the central MariaDB.
- **Blocks:** INFRA-12 (real data transmission requires populated journal)

---

## 🟡 Medium Priority Items

### ACC-16 — VatPayableTile Financial Overview Placement
- **Module:** Accounting
- **Plan:** [16-vat-tile-placement.md](../VISTA_Modules/Accounting/16-vat-tile-placement.md)
- **Impact:** VAT Payable KPI invisible on dashboard despite being fully computed

### INFRA-12 — SyncOrchestrator Real Data Transmission
- **Module:** Infrastructure
- **Plan:** [12-sync-orchestrator-transmission.md](../VISTA_Modules/Infrastructure/12-sync-orchestrator-transmission.md)
- **Impact:** Sync loop runs but transmits nothing (placeholder stub)
- **Depends on:** INFRA-13 (journal must be populated first)

### INFRA-14 — Central Schema Alignment for Receipt Sync
- **Module:** Infrastructure
- **Plan:** [14-central-schema-receipt-alignment.md](../VISTA_Modules/Infrastructure/14-central-schema-receipt-alignment.md)
- **Impact:** Receipt sync will fail due to missing columns in central MariaDB schema

### PUR-15 — GoodsReceiptLine VAT Classification Extension
- **Module:** Purchasing
- **Plan:** [15-goods-receipt-vat-classification.md](../VISTA_Modules/Purchasing/15-goods-receipt-vat-classification.md)
- **Impact:** Input VAT is aggregate-level, not per-line as BIR requires

---

## 🟢 Low Priority Items

### ACC-17 — Schema Verification Harness Dev-Menu Integration
- **Module:** Accounting
- **Plan:** [17-schema-harness-dev-menu.md](../VISTA_Modules/Accounting/17-schema-harness-dev-menu.md)
- **Impact:** QoL — harness can still be invoked from Immediate Window

### ACC-18 — Tamper Incident Report UI
- **Module:** Accounting
- **Plan:** [18-tamper-incident-report.md](../VISTA_Modules/Accounting/18-tamper-incident-report.md)
- **Impact:** Audit compliance reporting — incidents are logged but not viewable via UI

---

## ⏳ Deferred Items

These are blocked on external dependencies and cannot proceed now.

| Item | Module | Blocked On |
|---|---|---|
| Pomelo 10.x bump | INFRA-11 | NuGet publish of `Pomelo.EntityFrameworkCore.MySql` 10.x |
| MariaDB trigger DEFINER remediation | INFRA-08 | Formal DB admin account creation |
| EF Core VB.NET migration discovery | INT-04 | Upstream EF Core bug fix |
| ESC/POS or PDF receipt rendering | POS-18 | Future scope — no blocker, just not planned |

---

## Cross-Module Dependency Chain

```
INFRA-13 (write-path migration)
  └──→ INFRA-12 (real data transmission)
        └──→ INFRA-14 (central schema alignment)

ACC-16 (tile placement)
  └──→ ACC verification checklist (runtime verification)

PUR-15 (per-line VAT)
  └──→ ACC-10/ACC-11 handlers may need update (consumer-side)
```

### INFRA-07 Downstream Obligations

INFRA-07 defined 6 downstream publisher/handler items. Current coverage:

| Obligation | Status |
|---|---|
| POS-14: Publish `SaleCompletedWithVatEvent` | ✅ Completed in POS-14 |
| POS-13: Publish `ReceiptTamperDetectedEvent` | ✅ Completed in POS-13 |
| Purchasing: Publish `GoodsReceivedWithVatEvent` | ✅ Completed in PUR-14 |
| ACC-10: Handler for VAT revenue journal entries | ✅ Completed in ACC-10 |
| ACC-11: Handler for VAT summary ledger | ✅ Completed in ACC-11 |
| Accounting: Handler for `ReceiptTamperDetectedEvent` | ✅ Completed in ACC-15 |

**All 6 INFRA-07 obligations are resolved.** No new plans needed.

---

## Verification Checklists

| Checklist | Location |
|---|---|
| Accounting | [ACC-verification-checklist.md](ACC-verification-checklist.md) |
| Infrastructure | [INFRA-verification-checklist.md](INFRA-verification-checklist.md) |
| Integration | [INT-verification-checklist.md](INT-verification-checklist.md) |
| Integration (INT-12) | [INT-12-checklist.md](INT-12-checklist.md) |
| POS | [POS-verification-checklist.md](POS-verification-checklist.md) |
| Purchasing | [PUR-verification-checklist.md](PUR-verification-checklist.md) |

---

## Resolved Audit

The [Feature-Gap-Audit-2026-05-10.md](Feature-Gap-Audit-2026-05-10.md) identified 4 feature gaps — all have been marked ✅ Resolved with the plan IDs that addressed them.
