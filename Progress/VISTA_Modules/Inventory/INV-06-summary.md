---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/06-low-stock-alerts.md
status: completed
---

## Task Summary

Implemented threshold-based low-stock alerts for the Inventory module. Products with current stock at or below their minimum threshold appear in the alert list, sorted by deficit (most critical first). A desktop toast notification fires when `CheckAndGenerateAlertsAsync` detects active alerts.

**Plan:** `[[06-low-stock-alerts]]`

## What Was Done

- Created `src/MerchSys.Inventory/Services/ILowStockAlertService.vb` — defines `ILowStockAlertService` (3 methods), `ILowStockNotifier` abstraction, and `LowStockAlertDto`
- Created `src/MerchSys.Inventory/Services/LowStockAlertService.vb` — full implementation of all three interface methods

## Architecture Notes

### Notification Abstraction
`Notification.Wpf` targets `net10.0-windows` (WPF-only) and cannot be referenced from the plain `net10.0` class library `MerchSys.Inventory`. To satisfy the toast requirement without violating the project's target framework, a local `ILowStockNotifier` interface is defined in `ILowStockAlertService.vb`. The concrete `Notification.Wpf`-backed implementation must be registered in `MerchSys.App` DI.

### Threshold Resolution
`LowStockAlertService.BuildAlertsAsync` resolves the effective threshold per product:
1. If a `StockAlertConfig` record exists for the product with `IsAlertEnabled = True` → use `StockAlertConfig.MinimumThreshold`
2. Otherwise → fall back to `Product.MinimumThreshold` (the field used by `StockDashboardService`)

### `UpdateThresholdAsync` Upsert
Updates `StockAlertConfig.MinimumThreshold` (creates the record if absent) and keeps `Product.MinimumThreshold` in sync so the dashboard and alert service always agree on the threshold value.

### Stock Level Source
Delegates to `IStockService.GetCurrentStockAsync(Nothing)` to reuse the existing FIFO-aware stock computation rather than duplicating it.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None.

## What's Next

- Register `ILowStockAlertService` / `LowStockAlertService` in `MerchSys.App` DI
- Register a concrete `ILowStockNotifier` in `MerchSys.App` backed by `Notification.Wpf.NotificationManager`
- Call `CheckAndGenerateAlertsAsync` from `SaleCompletedEvent` and `ShrinkageRecordedEvent` handlers

## Cross-References

- Domain Wiki pages consulted: `[[inventory-module-paper]]`, `[[tech-stack-reference]]`
- Agent Wiki entries consulted: `[[vbnet-rootnamespace-relative-declarations]]`
