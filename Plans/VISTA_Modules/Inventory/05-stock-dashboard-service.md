---
module: MerchSys.Inventory
plan-id: INV-05
title: "Stock Dashboard Service"
depends-on: [INV-03, INV-04]
estimated-files: 2
---

# Stock Dashboard Service

## Context

Provides the data aggregation service for the real-time stock dashboard — all products with current qty, stock value, low-stock status, expiry status, and inflow/outflow summary. Addresses Problem I3 (no dashboard).

## Prerequisites

- **INV-03** (Stock Management) — stock levels queryable
- **INV-04** (Expiry Tracking) — expiry status queryable

## Wiki References

- `sources/inventory-module-paper.md` — "Real-Time Stock Dashboard: all products, current qty, low-stock status, inflow/outflow, total value"
- `concepts/fifo-costing.md` — live FIFO valuation

## Deliverables

```
MerchSys.Inventory/Services/
├── IStockDashboardService.vb
└── StockDashboardService.vb
```

## Specification

### IStockDashboardService
```
GetDashboardDataAsync() As Task(Of StockDashboardDto)
GetProductDetailAsync(productId As Integer) As Task(Of ProductDetailDto)
```

### StockDashboardDto
```
TotalProducts As Integer
TotalStockValue As Decimal                     ' FIFO valuation
LowStockCount As Integer                       ' Products below threshold
NearExpiryCount As Integer                     ' Batches within expiry threshold
ExpiredCount As Integer                        ' Expired batch count
Products As List(Of ProductSummaryDto)         ' Per-product summary
```

### ProductSummaryDto
```
ProductId, ProductName, Category, CurrentStock, RetailPrice, StockValue, Unit, MinThreshold, StockStatus ("Normal", "Low", "Out"), ExpiryStatus ("OK", "NearExpiry", "HasExpired"), HasExpiry
```

### ProductDetailDto
```
Product info + StockBatches list (ordered by ReceiptDate) + ShrinkageHistory + RecentMovements (last 30 days inflow/outflow)
```

## Acceptance Criteria

1. `dotnet build` succeeds
2. Dashboard aggregates all product stock levels
3. Stock value uses FIFO valuation (Σ QtyRemaining × UnitCost per batch)
4. Low-stock and expiry counts are accurate
5. Product detail shows batch-level breakdown

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-05-summary.md`
