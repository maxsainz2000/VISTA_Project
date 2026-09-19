---
module: MerchSys.Inventory
plan-id: INV-12
title: "View — Expiry Monitor"
depends-on: [INV-04]
estimated-files: 3
---

# View — Expiry Monitor

## Context

Dedicated screen for monitoring batch expiry dates — near-expiry alerts, expired batch list, and one-click write-off to shrinkage.

## Prerequisites

- **INV-04** (Expiry Tracking Service)

## Deliverables

```
MerchSys.App/Views/Inventory/
├── ExpiryMonitorView.xaml
└── ExpiryMonitorView.xaml.vb

MerchSys.Inventory/ViewModels/
└── ExpiryMonitorViewModel.vb
```

## Specification

### Screen Layout

1. **Summary:** NearExpiryCount, ExpiredCount, TotalValueAtRisk
2. **Tabs:**
   - **Near-Expiry:** Batches expiring within configurable threshold (default 30 days). Columns: Product, BatchId, QtyRemaining, UnitCost, Value, ExpiryDate, DaysRemaining
   - **Expired:** Batches past expiry. Same columns + "Write Off" button per row
3. **Threshold config:** spinner/input for days threshold (applies to near-expiry tab)
4. **Write-Off action:** Creates shrinkage record, sets batch QtyRemaining to 0, shows confirmation

### Row Styling
- Red: ≤7 days or expired
- Orange: 8–14 days
- Yellow: 15–30 days

## Acceptance Criteria

1. `dotnet build` succeeds
2. Near-expiry and expired batches displayed correctly
3. Write-off creates shrinkage record
4. Threshold configurable
5. Color coding by urgency

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-12-summary.md`
