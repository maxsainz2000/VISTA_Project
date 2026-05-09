---
module: Integration
plan-id: INT-06
title: "End-to-End QA & Smoke Testing"
depends-on: [INT-05]
estimated-files: 1
---

# End-to-End QA & Smoke Testing

## Context

All 56 module plans (INFRA-01 through INT-05) are complete, and the solution compiles with 0 errors and 0 warnings. However, end-to-end UI smoke testing and runtime validation have been deferred throughout the implementation phase per project policy. This plan consolidates the remaining genuine tasks surfaced by the 2026-05-09 module audits into a single project-wide QA pass.

**Audit sources:** `Pending_Tasks/POS-audit-2026-05-09.md` (POS-12 QA item), `Pending_Tasks/Integration-audit-2026-05-09.md` (INT-04 EF Core CLI item)

## Prerequisites

- INT-05 (Phase 2 Enhancements) — all code and migrations must be in place.
- Application must launch without runtime errors (DI composition root resolves all services).

## Deliverables

### 1. Application Launch Smoke Test

- Run `dotnet run --project WPF_Applications/MerchSys/src/MerchSys.App`
- Verify the application starts without unhandled exceptions
- Confirm the main shell window renders with the navigation sidebar
- Verify DI composition root resolves all registered services without `InvalidOperationException`

### 2. Navigation Shell Verification (All Modules)

Verify that every registered view is navigable from the main shell:

| Module | Views to Verify |
|--------|----------------|
| Purchasing | PO Management, Goods Receiving, Vendor Directory, AP Ledger, Reorder Suggestions |
| Inventory | Stock Dashboard, Product Management, Expiry Monitor, Shrinkage |
| POS | Sales Cart, Credit Management, Transaction History, Daily Summary |
| Accounting | Financial Overview, Income Statement, Sales Summary |

**Acceptance:** Each view loads without exception and displays its bound ViewModel data (or empty-state placeholder if no seed data is present).

### 3. Database Schema Validation

- Verify `DatabaseInitializer.vb` creates the SQLite database file (`merchsys.db`)
- Confirm all 24 tables exist with correct prefixes (`Pur_`, `Inv_`, `Pos_`, `Acc_`)
- Verify seed data is populated for reference tables
- Test that each DbContext can execute a simple query (e.g., `ToListAsync` on the primary entity set)

### 4. EF Core CLI Migration Validation (Deferred — Conditional)

> **Blocked:** EF Core 10 does not currently support VB.NET projects via `dotnet ef` CLI. See `LLM_Wiki/agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md`.

When EF Core adds VB.NET CLI support:

- Run `dotnet ef database update` for each of the 4 module projects:
  - `MerchSys.Purchasing`
  - `MerchSys.Inventory`
  - `MerchSys.POS`
  - `MerchSys.Accounting`
- Confirm manually-written migration files align with the compiled entity model
- Report any schema drift or migration errors

### 5. Cross-Module Event Flow Verification

Verify the MediatR event pipeline works end-to-end:

- **GoodsReceivedEvent:** Creating a PO receipt should trigger Inventory stock addition + Accounting expense record
- **SaleCompletedEvent:** Completing a sale should trigger Inventory stock deduction + Accounting revenue record
- **ShrinkageRecordedEvent:** Recording shrinkage should trigger Accounting expense record
- **CreditPaymentEvent:** Recording a credit payment should trigger Accounting AR reduction record
- **StockReturnedEvent:** Processing a return should trigger Inventory stock restoration

### 6. Known Issue Verification

- Verify `CreditAccount.IsBlocked` enforces the hard blocking rule (no new credit sales when `CurrentBalance > 0`)
- Verify `ISessionService` is injected into `CreditManagementViewModel` (replacing hardcoded `"Manager"`)
- Verify `StockMovement` entity logs are created on stock changes (INT-05 enhancement)

## Acceptance Criteria

1. Application launches and main window renders without exceptions
2. All 17 registered views are navigable from the shell
3. Database file is created with 24 tables and seed data
4. At least one cross-module event flow is verified end-to-end
5. No runtime `NullReferenceException`, `InvalidOperationException`, or unhandled exceptions during the smoke test
6. All findings documented in the progress summary

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-06-summary.md`.

## Post-Completion Notes

> **Added 2026-05-09** — Surfaced by `Progress/VISTA_Modules/Integration/INT-06-summary.md` execution findings.

**Follow-up plans created for gaps identified during QA execution:**

- **INT-07 (DI Registration Gaps):** 10 service interfaces (`IStockService`, `ICreditService`, `ICartService`, `IPaymentService`, `ISalesReturnService`, `IInventoryAuditService`, `IFinancialOverviewService`, `IIncomeStatementService`, `ISalesSummaryService`, `IWhatThisMeansService`) are not registered in the DI container. Blocks runtime navigation to POS, Accounting, and Inventory views, and prevents cross-module event handlers from resolving. See `Plans/VISTA_Modules/Integration/07-di-registration-gaps.md`.
- **INT-08 (StockMovement Log Writes):** `StockService.AddStockBatchAsync` and `DeductStockFIFOAsync` do not write `StockMovement` records despite the entity and table existing (INT-05 deliverable). Impacts `VelocityService` and `StockoutEstimationService` accuracy. See `Plans/VISTA_Modules/Integration/08-stockmovement-writes.md`.
