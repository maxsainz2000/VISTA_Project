---
module: Integration
plan-id: INT-01
title: "App Composition Root & DI Registration"
depends-on: [INFRA-04]
estimated-files: 3
---

# App Composition Root & DI Registration

## Context

All 51 module plans (INFRA-01–04, PUR-01–13, INV-01–13, POS-01–12, ACC-01–09) are fully implemented with green builds. Every service, ViewModel, and View was created in its respective module library, but **none were registered in the App DI container**. Each module progress summary deferred DI registration to "the App bootstrap plan" — this is that plan.

This plan addresses the single largest integration gap: wiring `Application.xaml.vb` (or the Generic Host builder) so that all module types are resolvable at runtime.

**Audit sources:** `Pending_Tasks/Purchasing-audit-2026-05-07.md`, `Pending_Tasks/Inventory-audit-2026-05-07.md`, `Pending_Tasks/POS-audit-2026-05-07.md`, `Pending_Tasks/Accounting-audit-2026-05-07.md`, `Pending_Tasks/Infrastructure-audit-2026-05-07.md`

## Prerequisites

- INFRA-04 (MediatR Event Bus) — completed. `MediatRConfig.AddMediatRServices` exists but must be verified as called from the host builder.

## Wiki References

- `LLM_Wiki/wiki/concepts/modular-monolith.md` — DI boundary rules
- `LLM_Wiki/codebase_wiki/index.md` — current codebase structure
- `LLM_Wiki/wiki/analysis/tech-stack-reference.md` — package versions

## Deliverables

### 1. Verify MediatR Bootstrap

Confirm that `MediatRConfig.AddMediatRServices` is called from the DI bootstrap in `Application.xaml.vb` (or host builder). If missing, add it.

### 2. Purchasing Module Registrations

Register the following in the DI container (Scoped unless noted):

| Interface / Type | Implementation | Lifetime | Source Plan |
|-----------------|---------------|----------|-------------|
| `IAccountsPayableService` | `AccountsPayableService` | Scoped | PUR-06 |
| `IReorderService` | `ReorderService` | Scoped | PUR-07 |
| `PurchaseOrderListViewModel` | *(self)* | Transient | PUR-09 |
| `GoodsReceivingViewModel` | *(self)* | Transient | PUR-10 |
| `VendorListViewModel` | *(self)* | Transient | PUR-11 |
| `APLedgerViewModel` | *(self)* | Transient | PUR-12 |
| `ReorderSuggestionsViewModel` | *(self)* | Transient | PUR-13 |

### 3. Inventory Module Registrations

| Interface / Type | Implementation | Lifetime | Source Plan |
|-----------------|---------------|----------|-------------|
| `IExpiryTrackingService` | `ExpiryTrackingService` | Scoped | INV-04 |
| `IStockDashboardService` | `StockDashboardService` | Scoped | INV-05 |
| `ILowStockAlertService` | `LowStockAlertService` | Scoped | INV-06 |
| `ILowStockNotifier` | `WpfLowStockNotifier` | Singleton | INV-06 *(see §5)* |
| `IShrinkageService` | `ShrinkageService` | Scoped | INV-07 |
| `IVelocityService` | `VelocityService` | Scoped | INV-08 |
| `IStockoutEstimationService` | `StockoutEstimationService` | Scoped | INV-09 |
| `StockDashboardViewModel` | *(self)* | Transient | INV-10 |
| `ProductManagementViewModel` | *(self)* | Transient | INV-11 |
| `ExpiryMonitorViewModel` | *(self)* | Transient | INV-12 |
| `ShrinkageViewModel` | *(self)* | Transient | INV-13 |

### 4. POS Module Registrations

| Interface / Type | Implementation | Lifetime | Source Plan |
|-----------------|---------------|----------|-------------|
| `IReceiptService` | `ReceiptService` | Scoped | POS-06 |
| `IDailySummaryService` | `DailySummaryService` | Scoped | POS-08 |
| `SalesCartViewModel` | *(self)* | Transient | POS-09 |
| `CreditManagementViewModel` | *(self)* | Transient | POS-10 |
| `TransactionHistoryViewModel` | *(self)* | Transient | POS-11 |
| `DailySummaryViewModel` | *(self)* | Transient | POS-12 |

### 5. Accounting Module Registrations

| Interface / Type | Implementation | Lifetime | Source Plan |
|-----------------|---------------|----------|-------------|
| `FinancialOverviewViewModel` | *(self)* | Transient | ACC-07 |
| `IncomeStatementViewModel` | *(self)* | Transient | ACC-08 |
| `SalesSummaryViewModel` | *(self)* | Transient | ACC-09 |

### 6. ILowStockNotifier Concrete Implementation

Create a new class `WpfLowStockNotifier` in `MerchSys.App/Services/` that implements `ILowStockNotifier` (defined in `MerchSys.Inventory`). This class wraps `Notification.Wpf`'s `NotificationManager` to display desktop toast alerts when low stock is detected.

- The `MerchSys.App` project already targets `net10.0-windows` and has the `Notification.Wpf` NuGet package.
- The concrete class lives in App (not Inventory) to avoid a WPF dependency in the class library.

## Implementation Notes

- Group registrations by module in `Application.xaml.vb` with clear comment blocks for readability.
- Services that depend on `DbContext` must be Scoped (matching the DbContext lifetime).
- ViewModels are Transient so each navigation creates a fresh instance.
- Verify that all referenced interfaces and classes exist and compile before adding registrations — if any type name differs from what's listed here, use the actual name from the codebase.

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` completes with **0 errors, 0 warnings**
2. All services listed above are registered in the DI container
3. `WpfLowStockNotifier` class exists in `MerchSys.App/Services/` and implements `ILowStockNotifier`
4. `MediatRConfig.AddMediatRServices` is confirmed called during app startup

## Output Requirements

### Implementation Summary
After completing all code, create a progress report at:
```
Progress/VISTA_Modules/Integration/INT-01-summary.md
```
Using the template structure from `Progress/_template.md`. Include:
- All files created/modified with full paths
- Full list of DI registrations added
- Build status confirmation
- Any deviations from this plan
