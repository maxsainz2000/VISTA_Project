---
module: Integration
agent: claude-code
date: 2026-05-10
plan-ref: Plans/VISTA_Modules/Integration/11-eventbus-registration.md
status: completed
---

## Task Summary

Implemented `MediatREventBus` — the missing `IEventBus` adapter — and registered it in the DI container. This resolves the `InvalidOperationException` preventing 3 POS views (`SalesCartView`, `CreditManagementView`, `TransactionHistoryView`) from navigating at runtime.

**Plan:** `[[11-eventbus-registration]]`

## What Was Done

- Created `MerchSys.App/Services/MediatREventBus.vb` — thin adapter class implementing `IEventBus`; injects `IMediator` and delegates `PublishAsync` to `IMediator.Publish`. Follows the existing `DefaultSessionService` pattern in the same directory.
- Modified `MerchSys.App/Application.xaml.vb` — added `services.AddScoped(Of IEventBus, MediatREventBus)()` in the Infrastructure section, after `ISessionService` registration.
- Modified `MerchSys.App/Views/POS/TransactionHistoryView.xaml` (line 601) — removed invalid `Style="{StaticResource FieldLabel}"` from a `<Run>` element. The `FieldLabel` style has `TargetType="TextBlock"` which cannot be applied to `Run` (an inline element). Moved `FontSize="11"` and `Foreground="#7F8C8D"` directly onto the parent `<TextBlock>`. This was a XAML parse-time crash separate from the DI issue.

## Root Cause Analysis

`IEventBus` was defined in `MerchSys.SharedKernel/Interfaces/IEventBus.vb` as a design-time interface abstracting MediatR's publish functionality. Three POS services (`PaymentService`, `CreditService`, `SalesReturnService`) injected it via constructor DI. However:

1. **No implementation class** was ever created — unlike `ISessionService` which had `DefaultSessionService`
2. **No DI registration** was added to `Application.xaml.vb`
3. The `di-registry.md` wiki listed `IEventBus | MediatR | Transient` — this was aspirational documentation, not reflecting actual registrations

This gap was invisible to prior DI audits (INT-07) because INT-07 only checked for interfaces that had implementations but were unregistered. `IEventBus` had no implementation at all, so it didn't appear in the unregistered-service scan. The gap was only surfaced when the user performed the interactive 16-view navigation test during INT-10.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors, 0 warnings |
| Unit tests | N/A |
| SalesCartView | ✅ Pass (user-verified after IEventBus fix) |
| CreditManagementView | ✅ Pass (user-verified after IEventBus fix) |
| TransactionHistoryView | ✅ Pass (verified in Operator checklist) |

## Issues Encountered

None. Build succeeded on the first attempt.

## What's Next

- [x] User re-test: SalesCartView — ✅ Pass
- [x] User re-test: CreditManagementView — ✅ Pass
- [x] User re-test: TransactionHistoryView — XAML fix applied (FieldLabel style on Run element), awaiting re-test *(completed/verified in Operator checklist)*
- [x] If all 16 views pass, update INT-10 summary's interactive navigation item from `[/]` to `[x]` *(completed/verified in Operator checklist)*

## Codebase Wiki Updates Made

- `di-registry.md` row for `IEventBus` updated from `MediatR / Transient` to `MediatREventBus / Scoped`

## Cross-References

- Domain Wiki pages consulted: none
- Agent Wiki entries consulted: none
- Prior plans: INT-10 (user's interactive testing surfaced the gap), INT-07 (original DI registration audit)
