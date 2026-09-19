---
module: Integration
agent: claude-code
date: 2026-05-09
plan-ref: Plans/VISTA_Modules/Integration/07-di-registration-gaps.md
status: completed
---

# INT-07: DI Registration Gaps

## Task Summary

Registered 9 of the 10 missing service interfaces in `Application.xaml.vb`. All registrations use Scoped lifetime to match their module DbContext lifetimes. `IInventoryAuditService` was not registered because the interface and implementation do not exist in the codebase (see Issues section).

**Plan:** `[[07-di-registration-gaps]]`

## What Was Done

- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb`:
  - Added `Imports MerchSys.Accounting.Services` (was missing, required for new Accounting registrations)
  - **POS:** Added `ICartService/CartService`, `IPaymentService/PaymentService`, `ICreditService/CreditService`, `ISalesReturnService/SalesReturnService` — all Scoped
  - **Inventory:** Added `IStockService/StockService` — Scoped
  - **Accounting:** Added `IFinancialOverviewService/FinancialOverviewService`, `IIncomeStatementService/IncomeStatementService`, `ISalesSummaryService/SalesSummaryService`, `IWhatThisMeansService/WhatThisMeansService` — all Scoped

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual view navigation smoke test | Pending (requires runtime session) |
| Cross-module event flow verification | Pending (requires runtime session) |

## Issues Encountered

- **Issue:** `IInventoryAuditService` / `InventoryAuditService` listed in the plan (INV-03) are absent from the codebase — no `.vb` files found under `MerchSys.Inventory/Services/` or anywhere in `src/`.
  - **Resolution:** Registration skipped. This interface was referenced in plan INT-07 as a gap from INV-03, but INV-03 did not create the file. Needs to be created in a future INV-03 amendment or a new Inventory plan pass.
  - **Codebase wiki discrepancy:** `di-registry.md` lists `IInventoryAuditService` as `*Pending* / Not yet registered` — it should be updated to reflect that the interface itself is missing, not just unregistered.

## What's Next

- [x] Runtime smoke test: launch application and navigate all 16 views *(DI startup verified in INT-10; interactive pass deferred to user session)*
- [x] Cross-module event flow runtime verification *(synthetic verification completed in INT-10)*
- [x] Create `IInventoryAuditService` / `InventoryAuditService` *(completed by INT-09)*
- [x] Register `IInventoryAuditService` in `Application.xaml.vb` *(completed by INT-09)*

## Codebase Wiki Discrepancies

The following `codebase_wiki/schemas/di-registry.md` entries need updating by Antigravity:

| Entry | Current | Correct |
|---|---|---|
| `ICartService` | *Pending* / Not yet registered | Scoped — registered |
| `IPaymentService` | *Pending* / Not yet registered | Scoped — registered |
| `ICreditService` | *Pending* / Not yet registered | Scoped — registered |
| `ISalesReturnService` | *Pending* / Not yet registered | Scoped — registered |
| `IStockService` | *Pending* / Not yet registered | Scoped — registered |
| `IInventoryAuditService` | *Pending* / Not yet registered | **Interface does not exist in codebase** |
| `IFinancialOverviewService` | *Pending* / Not yet registered | Scoped — registered |
| `IIncomeStatementService` | *Pending* / Not yet registered | Scoped — registered |
| `ISalesSummaryService` | *Pending* / Not yet registered | Scoped — registered |
| `IWhatThisMeansService` | *Pending* / Not yet registered | Scoped — registered |

## Cross-References

- Domain Wiki pages consulted: none
- Agent Wiki entries consulted: none
