---
module: MerchSys.Integration
plan-id: INT-16
title: "ToListAsync remediation — Purchasing + Accounting service methods"
depends-on: [INT-14]
estimated-files: 8
priority: critical
---

# INT-16: ToListAsync remediation — Purchasing + Accounting service methods

## Context

Sibling of INT-15. Same bug, same workaround, different module surface. The EF Core 10 + VB.NET `ToListAsync()` silent-empty-list defect (`agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`) affects every full-entity list query. INT-14's triage checklist enumerates the Purchasing and Accounting rows; this plan resolves them.

**Why critical.** Purchasing screens (Vendor Directory, PO Management, AP Ledger, Reorder Suggestions) and the few full-entity Accounting reads (notably tamper-audit queries and VAT-line entity reads) all back user-visible lists. The defect is silent — empty lists with no exception.

The existing `VendorService` fix lives in this batch; this plan extends it to `SearchAsync`, `GetVendorWithPurchaseHistoryAsync`, and the rest of the Purchasing service surface.

## Prerequisites

- **INT-14** — produces the authoritative checklist this plan consumes.
- **INFRA-18** — needed transitively via INT-14, and re-used at acceptance time to confirm the fixed sites no longer trigger Rule 3.

## Wiki References

- `agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` — canonical fix
- `Operator/debug-logs/tolistasync-remediation-checklist.md` — scope (INT-16 rows only)
- `Operator/debug-logs/` Vendor fix log — the working precedent

## Deliverables

The exact file list comes from INT-14's checklist. Based on the verified true positives in `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md` section 2 Rule 3, the expected Purchasing + Accounting surface is:

```
MerchSys.Purchasing/Services/VendorService.vb              ' Modified — extend existing fix to SearchAsync, GetVendorWithPurchaseHistoryAsync, and any full-entity sibling
MerchSys.Purchasing/Services/PurchaseOrderService.vb       ' Modified — full PO entity reads (anonymous projections are out of scope)
MerchSys.Purchasing/Services/ReorderService.vb             ' Modified — full Product / ReorderRule entity reads
MerchSys.Purchasing/Services/AccountsPayableService.vb     ' Modified — full AP-record entity reads
MerchSys.Purchasing/Services/GoodsReceivingService.vb      ' Modified — full GoodsReceipt entity reads
MerchSys.Purchasing/Services/PriceChangeService.vb         ' Modified — full PriceChange entity reads (verify against checklist; some are projections)
MerchSys.Accounting/Services/ITamperAuditQueryService.vb   ' Modified — verify it returns full entities, not DTOs
MerchSys.Accounting/Services/VatReportingService.vb        ' Modified — only the full VatReturnLine entity reads, not the aggregate projections
```

If the checklist puts `FinancialOverviewService`, `IncomeStatementService`, or `SalesSummaryService` in INT-16, double-check by reading the chain — the verification report classified the high-traffic queries in those files as anonymous-projection FALSE POSITIVES. If after re-reading they are still anonymous projections, they are out of scope. Record the rebuttal in the checklist.

## Specification

### Per-method fix shape

Identical to INT-15. Replicate the Vendor pattern verbatim:

1. Fresh `New SqliteConnection(_db.Database.GetConnectionString())`.
2. Synchronous `cmd.ExecuteReader()` + `reader.Read()`.
3. Write to a class field (`_<entity>List`), not a local.
4. `Imports Microsoft.Data.Sqlite` added if missing.

See INT-15's Specification → "Per-method fix shape" for the canonical code skeleton. Do not deviate; the goal is uniformity across modules so the next agent can read any fixed method and recognise it instantly.

### `Include` graphs

The Purchasing surface has at least two known `graph` methods:

- `VendorService.GetVendorWithPurchaseHistoryAsync` — Vendor + `Include(PurchaseOrders)` + downstream join to GoodsReceipts. Split into: vendor SELECT, POs by VendorId, receipts by PO IDs. Reassemble in memory.
- `PurchaseOrderService.GetOrderHistoryAsync` (or equivalent) — POs + `Include(Lines)` + `Include(Vendor)`. Two SELECTs plus a vendor lookup.

Same deferral rule as INT-15: if the manual JOIN plumbing exceeds ~80 lines, defer to a follow-up plan with explicit justification.

### Out of scope

- All anonymous-type projections in `FinancialOverviewService`, `IncomeStatementService`, `SalesSummaryService`, `VatReportingService`. These are the rule's exclusion list per the wiki and are documented FALSE POSITIVES in the verification report.
- Anything outside Purchasing/Accounting modules — that is INT-15.
- `ReceiptIntegrityService` — that is POS and lives in INT-15.

### Verification

Same protocol as INT-15:

- Screen-level smoke check where the method backs a visible list (Vendor Directory, PO Management, AP Ledger, Reorder Suggestions).
- SQL-level verification with `sqlite3` for methods whose UI is not yet wired.
- Update `Fix applied` and `Verified` columns in `tolistasync-remediation-checklist.md` per row.

### Parameterisation

Every Purchasing search method (`VendorService.SearchAsync`, `PurchaseOrderService.SearchAsync`, etc.) takes user input. Use `SqliteParameter` for every value. No string concatenation into `CommandText`. This is non-negotiable — the original EF Core queries used `LIKE %term%` patterns which must translate to parameter-bound queries here.

## Implementation Notes

- **VAT entity reads in Accounting.** `VatReportingService` has both aggregate projections (out of scope) and full `VatReturnLine` entity reads (in scope). Read the method carefully — the `.GroupBy(...).Select(New With { ... })` shape is a projection; the `.Where(...).OrderBy(...).ToListAsync()` shape on `_db.VatReturnLines` is a full-entity read.
- **Soft-delete filter.** Several Purchasing entities have `IsDeleted`. Preserve the `WHERE IsDeleted = 0` filter when translating from EF to SQL.
- **PO lifecycle status.** `PurchaseOrderService.GetOrderHistoryAsync` filters by `Status`. Use an enum-int parameter (`SqliteParameter` with the integer value) — VB.NET enums map to integers by default and the table stores them as such.
- **Existing Vendor fix.** The Vendor fix should already be present in `VendorService.vb` from INFRA-test-X. Read it first — your new code in `SearchAsync` and `GetVendorWithPurchaseHistoryAsync` should follow the same connection-management idiom verbatim.

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` — 0 errors, 0 warnings.
2. Every Purchasing + Accounting row in `tolistasync-remediation-checklist.md` has `Fix applied = yes` and `Verified = <screen or sqlite check>`.
3. Re-running the INFRA-18 corrected Rule 3 detector against the live codebase produces zero hits in `MerchSys.Purchasing.*` files and zero hits in `MerchSys.Accounting.*` files outside the documented exclusion list.
4. Vendor Directory, PO Management, AP Ledger, and Reorder Suggestions screens all render rows when the underlying tables contain rows.
5. `VatReportingService` continues to produce identical aggregate output (anonymous projections were not touched) and now correctly returns non-empty `VatReturnLine` entity lists for the in-scope methods.
6. Search and detail methods use `SqliteParameter` for every user-supplied value.
7. Any `Fix complexity = graph` deferral is recorded in the summary with the file, method, line, and a one-paragraph justification.

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-16-summary.md` using `Progress/_template.md`. Include:

- The updated checklist diff (which rows flipped to `Fix applied = yes`).
- The SQL JOIN strategy for each `graph` method, with the parent-key dictionary scheme.
- Any rebuttals to INT-14's classification (e.g., a method INT-14 marked `graph` that turned out to be a projection).
- The before/after Rule 3 hit counts for Purchasing + Accounting modules.

## Post-Completion Notes

If the Accounting projection reads (`FinancialOverviewService`, `IncomeStatementService`, `SalesSummaryService`) turn out to be broken in production despite being out of scope here, that is a separate defect — open a new follow-up plan rather than expanding INT-16. The wiki rule says projections are not affected by this specific bug; if they are, a new bug exists and needs its own wiki entry.
