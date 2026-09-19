---
module: MerchSys.Inventory
plan-id: INV-04
title: "Expiry Date Tracking"
depends-on: [INV-03]
estimated-files: 2
---

# Expiry Date Tracking

## Context

Implements batch-level expiry monitoring for perishable products (pesticides, seeds, animal feeds). Alerts when products approach expiry and blocks sale of expired goods. Addresses Problem I4.

## Prerequisites

- **INV-03** (Stock Management) — `StockBatch` with `ExpiryDate` field, FIFO deduction skips expired batches

## Wiki References

- `concepts/expiry-date-tracking.md` — alert logic, affected products, FIFO synergy
- `sources/inventory-module-paper.md` — "Problem I4: Expiry dates not tracked systematically"

## Deliverables

```
MerchSys.Inventory/Services/
├── IExpiryTrackingService.vb
└── ExpiryTrackingService.vb
```

## Specification

### IExpiryTrackingService
```
GetNearExpiryBatchesAsync(daysThreshold As Integer) As Task(Of List(Of ExpiryAlertDto))
GetExpiredBatchesAsync() As Task(Of List(Of ExpiryAlertDto))
GetExpiryStatusForProductAsync(productId As Integer) As Task(Of ProductExpiryStatusDto)
WriteOffExpiredBatchAsync(batchId As Integer, reason As String) As Task(Of ShrinkageRecord)
```

### ExpiryAlertDto
```
BatchId, ProductId, ProductName, QtyRemaining, UnitCost, TotalValue, ExpiryDate, DaysUntilExpiry, Status ("NearExpiry" or "Expired")
```

### Alert Logic

1. `DaysUntilExpiry = ExpiryDate - Today`
2. **Near-expiry:** `0 < DaysUntilExpiry ≤ threshold` (configurable, default 30 days)
3. **Expired:** `ExpiryDate < Today` — flagged, blocked from sale (FIFO deduction already skips these)
4. `WriteOffExpiredBatchAsync` creates a `ShrinkageRecord` with reason "Expiry" and sets batch `QtyRemaining = 0`

### Batch Expiry Products

Only products with `HasExpiry = True` are monitored: Pesticides, Seeds, Animal Feeds. Fertilizers are shelf-stable.

## Acceptance Criteria

1. `dotnet build` succeeds
2. Near-expiry batches returned when within threshold
3. Expired batches identified correctly
4. Write-off creates shrinkage record with financial impact
5. Only `HasExpiry = True` products monitored

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-04-summary.md`
