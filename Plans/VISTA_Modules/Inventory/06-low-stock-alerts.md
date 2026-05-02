---
module: MerchSys.Inventory
plan-id: INV-06
title: "Low Stock Alerts"
depends-on: [INV-03]
estimated-files: 2
---

# Low Stock Alerts

## Context

Implements threshold-based low-stock alerts. Each product has a configurable minimum threshold; when current stock falls at or below it, an alert is generated. Uses ToastNotifications for desktop alerts. Addresses Problem I2 (manual counting) and I3 (no dashboard alerts).

## Prerequisites

- **INV-03** (Stock Management) — `GetCurrentStockAsync` available

## Wiki References

- `sources/inventory-module-paper.md` — "Automated Low-Stock Alerts: triggers at manager-defined minimum threshold"
- `analysis/tech-stack-reference.md` — "ToastNotifications NuGet"

## Deliverables

```
MerchSys.Inventory/Services/
├── ILowStockAlertService.vb
└── LowStockAlertService.vb
```

## Specification

### ILowStockAlertService
```
CheckAndGenerateAlertsAsync() As Task(Of List(Of LowStockAlertDto))
GetCurrentAlertsAsync() As Task(Of List(Of LowStockAlertDto))
UpdateThresholdAsync(productId As Integer, newThreshold As Integer) As Task
```

### LowStockAlertDto
```
ProductId, ProductName, CurrentStock, MinimumThreshold, Deficit (threshold - current), LastRestockDate
```

### Logic

1. Query all active products with their `StockAlertConfig.MinimumThreshold`
2. Compare against current stock (from `StockService`)
3. If `CurrentStock ≤ MinimumThreshold`, include in alert list
4. Sort by deficit (most critical first)

### Toast Integration

When new alerts are generated (after goods sold or shrinkage recorded), show a desktop toast notification summarizing the count: "⚠ {N} products are at or below minimum stock level."

## Acceptance Criteria

1. `dotnet build` succeeds
2. Alerts generated for products at/below threshold
3. Threshold configurable per product
4. Alerts sorted by deficit (most critical first)

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-06-summary.md`
