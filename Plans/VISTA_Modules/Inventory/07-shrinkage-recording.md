---
module: MerchSys.Inventory
plan-id: INV-07
title: "Shrinkage Recording"
depends-on: [INV-03, INFRA-04]
estimated-files: 2
---

# Shrinkage Recording

## Context

Implements inventory shrinkage recording — damage, spoilage, expiry write-off, and administrative discrepancies. Each shrinkage record captures the financial impact and publishes a `ShrinkageRecordedEvent` to Accounting.

## Prerequisites

- **INV-03** (Stock Management) — `StockBatch` data, FIFO costing for loss valuation
- **INFRA-04** (MediatR) — `ShrinkageRecordedEvent` contract

## Wiki References

- `sources/inventory-module-paper.md` — "Shrinkage Recording: damage, expiry write-off, admin discrepancy with financial impact"
- `analysis/cross-module-data-flow.md` — `ShrinkageRecordedEvent`: Inventory → Accounting

## Deliverables

```
MerchSys.Inventory/Services/
├── IShrinkageService.vb
└── ShrinkageService.vb
```

## Specification

### IShrinkageService
```
RecordShrinkageAsync(productId As Integer, quantity As Integer, reason As String, notes As String, Optional batchId As Integer? = Nothing) As Task(Of ShrinkageRecord)
GetShrinkageHistoryAsync(Optional productId As Integer? = Nothing) As Task(Of List(Of ShrinkageRecord))
GetTotalShrinkageValueAsync(startDate As DateTime, endDate As DateTime) As Task(Of Decimal)
```

### Flow

1. If `batchId` specified, deduct from that batch; otherwise use FIFO (oldest first)
2. Calculate `UnitCost` from the affected batch(es)
3. Calculate `TotalValue = QuantityLost × UnitCost`
4. Save `ShrinkageRecord`
5. Publish `ShrinkageRecordedEvent` via MediatR with product, qty, cost, reason

### Valid Reasons
- "Damage" — physical damage to product
- "Spoilage" — perishable product deteriorated
- "Expiry" — product past expiry date (see INV-04 write-off)
- "Admin Error" — count discrepancy during physical inventory

## Acceptance Criteria

1. `dotnet build` succeeds
2. Shrinkage deducts from stock batches correctly
3. Financial impact calculated using batch unit cost
4. `ShrinkageRecordedEvent` published to MediatR
5. History queryable by product and date range

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-07-summary.md`
