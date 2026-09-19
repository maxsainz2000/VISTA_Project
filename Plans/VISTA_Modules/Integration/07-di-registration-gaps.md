---
module: Integration
plan-id: INT-07
title: "DI Registration Gaps"
depends-on: [INT-06]
estimated-files: 1
---

# DI Registration Gaps

## Context

The INT-06 end-to-end QA pass confirmed that the solution builds cleanly (0 errors, 0 warnings) and the application launches successfully. However, **10 service interfaces** required by ViewModels are not registered in the DI container. Navigation to any view that depends on these services will throw `InvalidOperationException` at runtime.

These gaps were documented in `codebase_wiki/schemas/di-registry.md` as `*Pending* / Not yet registered` and catalogued in the INT-06 progress summary (Deliverable 2 and Deliverable 5 caveats).

**Audit sources:** `Pending_Tasks/Integration-audit-2026-05-09.md`, `Pending_Tasks/POS-audit-2026-05-09.md`, `Pending_Tasks/Accounting-audit-2026-05-09.md`, `Pending_Tasks/Inventory-audit-2026-05-09.md`, `Progress/VISTA_Modules/Integration/INT-06-summary.md`

## Prerequisites

- INT-06 (End-to-End QA) — completed. Gaps identified and documented.
- INT-01 (App Composition Root) — completed. The registration infrastructure exists in `Application.xaml.vb`; these are services that were missed during that pass.

## Deliverables

### 1. POS Module — Missing DI Registrations

Register the following in `Application.xaml.vb` (Scoped lifetime, matching DbContext):

| Interface | Implementation | Source Plan | Affected Views |
|---|---|---|---|
| `ICartService` | `CartService` | POS-03 | `SalesCartView` |
| `IPaymentService` | `PaymentService` | POS-04 | `SalesCartView` |
| `ICreditService` | `CreditService` | POS-05 | `CreditManagementView`, `SalesCartView` |
| `ISalesReturnService` | `SalesReturnService` | POS-07 | `TransactionHistoryView` |

### 2. Inventory Module — Missing DI Registrations

| Interface | Implementation | Source Plan | Affected Flows |
|---|---|---|---|
| `IStockService` | `StockService` | INV-03 | `SaleCompletedHandler`, `GoodsReceivedHandler`, `StockReturnedEventHandler` (cross-module events) |
| `IInventoryAuditService` | `InventoryAuditService` | INV-03 | `StockDashboardView` (indirect) |

### 3. Accounting Module — Missing DI Registrations

| Interface | Implementation | Source Plan | Affected Views |
|---|---|---|---|
| `IFinancialOverviewService` | `FinancialOverviewService` | ACC-03 | `FinancialOverviewView` |
| `IIncomeStatementService` | `IncomeStatementService` | ACC-04 | `IncomeStatementView` |
| `ISalesSummaryService` | `SalesSummaryService` | ACC-05 | `SalesSummaryView` |
| `IWhatThisMeansService` | `WhatThisMeansService` | ACC-06 | All 3 Accounting views |

### 4. Runtime Navigation Smoke Test

After registering all services, verify that all 16 views are navigable from the shell without `InvalidOperationException`:

- Launch application via `dotnet run`
- Navigate to each of the 16 registered views
- Confirm no unhandled exceptions

### 5. Cross-Module Event Flow Runtime Verification

With `IStockService` now resolvable, verify at least one cross-module event chain at runtime:

- Create a purchase order → receive goods → confirm `GoodsReceivedHandler` fires and `StockService.AddStockBatchAsync` executes
- OR: Complete a sale → confirm `SaleCompletedHandler` fires and `StockService.DeductStockFIFOAsync` executes

### 6. Codebase Wiki Update

Update `LLM_Wiki/codebase_wiki/schemas/di-registry.md` to change all 10 entries from `*Pending*` to their registered status.

## Implementation Notes

- All services should be registered as **Scoped** to match their DbContext lifetimes.
- Verify that the actual class names match the interface names listed here — if any differ, use the actual names from the codebase.
- Group the new registrations with the existing module registration blocks in `Application.xaml.vb`.

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` — **0 errors, 0 warnings**
2. All 10 service interfaces are registered in the DI container
3. Application launches without `InvalidOperationException` on any view navigation
4. At least one cross-module event flow verified at runtime
5. `di-registry.md` updated to reflect new registrations

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-07-summary.md`.

## Post-Completion Notes

> **Added 2026-05-09** — Surfaced by `Progress/VISTA_Modules/Integration/INT-07-summary.md` execution findings and `Pending_Tasks/Integration-audit-2026-05-09.md`.

**Follow-up plans created for gaps identified during INT-07 execution:**

- **INT-09 (IInventoryAuditService Implementation):** `IInventoryAuditService` and `InventoryAuditService` are absent from the codebase (not just unregistered). A new plan creates the interface, implementation, and DI registration. See `Plans/VISTA_Modules/Integration/09-inventory-audit-service.md`.
- **INT-10 (Runtime Verification & Smoke Testing):** Runtime navigation smoke test (all 16 views) and cross-module event flow verification require a live runtime session. See `Plans/VISTA_Modules/Integration/10-runtime-verification.md`.
