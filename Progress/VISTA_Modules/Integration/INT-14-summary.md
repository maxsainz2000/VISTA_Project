---
module: MerchSys.Integration
agent: claude-code
date: 2026-05-25
plan-ref: Plans/VISTA_Modules/Integration/14-tolistasync-remediation-triage.md
status: completed
---

## Task Summary

Produced the ToListAsync remediation triage checklist for INT-15 and INT-16. Applied the corrected INFRA-18 Rule 3 detector against all 63 sites from the 2026-05-24 baseline, classified each site as true or false positive, and compiled a 47-row checklist with method names, entity types, Include complexity, UI surface descriptions, and batch assignments.

**Plan:** `[[14-tolistasync-remediation-triage]]`

## What Was Done

- Read all flagged service and ViewModel files from the 2026-05-24 baseline (63 sites across 18 files)
- Applied the INFRA-18 corrected Rule 3 detector (shape-aware: GroupBy gate, Select-projection gate, scalar-projection gate)
- Cleared 16 false positives; confirmed 47 true positives
- Traced UI callers for each method (ViewModel → View surface described per row)
- Assigned each row to INT-15 (Inventory + POS) or INT-16 (Purchasing + Accounting)
- Classified fix complexity: `simple` (no Include), `joined` (one Include), `graph` (multiple Includes or nested ThenInclude)
- Documented the already-applied Vendor fix in `PurchaseOrderListViewModel.vb` as the reference fix shape
- Created `Operator/debug-logs/tolistasync-remediation-checklist.md`
- Created this progress summary

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | N/A — no product code modified |
| Unit tests pass | N/A |
| Manual verification | N/A — read-only triage plan |

## Row Count Summary

| Scope | Count |
|---|---|
| 2026-05-24 baseline (uncorrected) | 63 |
| False positives cleared by INFRA-18 detector | 16 |
| **True positives in corrected output** | **47** |
| INT-15 rows (Inventory + POS) | 27 |
| INT-16 rows (Purchasing + Accounting) | 20 |
| Fix complexity = `simple` | 27 |
| Fix complexity = `joined` | 11 |
| Fix complexity = `graph` | 9 |

## Disagreement Between INFRA-18 Output and 2026-05-24 Baseline

The corrected detector cleared exactly **16 sites** that the original literal-grep approach flagged incorrectly. All 16 are in one of three categories:

1. **GroupBy → Select(New With {…}) chains** (10 sites) — anonymous-type projection. Excluded by the wiki rule; cleared by the GroupBy gate. Concentrated in `FinancialOverviewService`, `IncomeStatementService`, `SalesSummaryService`, `LowStockAlertService`, `VelocityService`, and `InventoryAuditService`.
2. **Select(scalar member) chains** (5 sites) — single-column string or int projection. Cleared by the Select-projection gate. Found in `ITamperAuditQueryService`, `SalesReturnService`, `GoodsReceivingService`, `PurchaseOrderService` (×2), and `ReorderService` (×2). *Note: PurchaseOrderService:30 and ReorderService:113 are both Select(OrderNumber) for sequential number generation — identical shapes.*
3. **LINQ query-syntax Select New With {…}** (1 site) — `ReceiptArchivalService:183` uses query-expression syntax (`From r … Select New With { Key .Receipt = r, …}`), which is an anonymous projection and cleared by the negative gate.

No previously-cleared site was re-flagged by the INFRA-18 detector. The 47 true positives are a strict subset of the 63.

## Include-Graph Risk Profile

**9 of 47 rows (19%)** are classified `graph`. These are concentrated in AccountsPayableService (4 rows, all with `Include(Vendor).Include(PurchaseOrder)` double-navigation) and spread across StockDashboardService, ShrinkageService, VelocityService, ReceiptIntegrityService, and PurchaseOrderService. INT-16 carries 5 graph rows; INT-15 carries 4.

The deepest graph is `ReceiptIntegrityService.ValidateChainAsync` (row 27), which has a nested three-level `Include(Receipt).ThenInclude(Transaction).ThenInclude(Lines)`. This requires three manual JOINs and per-row entity reassembly at two navigation levels.

## Issues Encountered

- **No INFRA-test debug log for the Vendor fix** — the improvement plan references "INFRA-test-X resolved this for vendors only" but no `Operator/debug-logs/INFRA-test-X.md` file exists. The fix is documented in the improvement plan at `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md` §2 and in the code at `PurchaseOrderListViewModel.vb:LoadDataAsync`. The checklist references the improvement plan as the pointer.

## What's Next

- [x] INT-15 — apply raw SqliteConnection fix to 27 Inventory + POS methods (ordered by row sequence in checklist) *(completed in INT-15)*
- [x] INT-16 — apply raw SqliteConnection fix to 20 Purchasing + Accounting methods (ordered by row sequence in checklist) *(completed in INT-16)*
- [x] INT-15 / INT-16 — budget extra time for the 9 `graph` rows (manual JOIN SQL + entity reassembly) *(completed in INT-15 and INT-16)*
- [x] Fill `Fix applied` and `Verified` columns in the checklist as each method is fixed and tested *(completed in INT-15 and INT-16)*

## Cross-References

- Domain Wiki pages consulted: none (triage only)
- Agent Wiki entries consulted: `[[efcore-vbnet-tolistasync-entity-empty]]`
- Deliverable: `Operator/debug-logs/tolistasync-remediation-checklist.md`
- Source baseline: `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md`
- INFRA-18 improvement plan: `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md`
