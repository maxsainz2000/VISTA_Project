---
module: Integration
plan-id: INT-11
title: "IEventBus DI Registration Gap"
depends-on: [INT-10]
estimated-files: 2
---

# IEventBus DI Registration Gap

## Context

INT-10's interactive 16-view navigation test (performed by the user on 2026-05-10) revealed that **3 of 16 views fail** with `InvalidOperationException` at runtime:

| View | Service | Error |
|------|---------|-------|
| SalesCartView | `PaymentService` | Unable to resolve `IEventBus` |
| CreditManagementView | `CreditService` | Unable to resolve `IEventBus` |
| TransactionHistoryView | `SalesReturnService` | Unable to resolve `IEventBus` |

**Root cause:** `IEventBus` is defined in `MerchSys.SharedKernel/Interfaces/IEventBus.vb` as a thin abstraction over MediatR's `IMediator.Publish`, but no implementation class was ever created, and no DI registration was added to `Application.xaml.vb`.

This gap was not caught by INT-07 (DI Registration Gaps) because INT-07 audited service interfaces that were already implemented but unregistered. `IEventBus` had no implementation at all — it was a design-time interface without a corresponding concrete class.

**User observation source:** `Pending_Tasks/Integration-audit-2026-05-09.md`, User Observations section (2026-05-10)

## Prerequisites

- INT-10 (Runtime Verification) — completed; user testing identified the gap.
- MediatR is already registered via `AddMediatRServices()` in `Application.xaml.vb`.

## Deliverables

### 1. Create MediatREventBus Adapter

Create `MerchSys.App/Services/MediatREventBus.vb` — a thin adapter class that:
- Implements `IEventBus`
- Injects `IMediator` via constructor
- Delegates `PublishAsync` to `IMediator.Publish`

Place in `MerchSys.App/Services/` following the existing `DefaultSessionService` pattern.

### 2. Register IEventBus in DI Container

Add to `Application.xaml.vb` in the Infrastructure section:

```vb
services.AddScoped(Of IEventBus, MediatREventBus)()
```

**Lifetime:** Scoped — matches the 3 consuming POS services (`PaymentService`, `CreditService`, `SalesReturnService`) which are all Scoped.

## Acceptance Criteria

1. Solution builds with 0 errors, 0 warnings
2. All 16 views navigable without `InvalidOperationException` (including the 3 previously-failing POS views)
3. `MediatREventBus` correctly delegates to `IMediator.Publish`

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-11-summary.md`.
