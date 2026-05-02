---
module: MerchSys.Inventory
plan-id: INV-03
title: "Stock Management (FIFO)"
depends-on: [INV-02, INFRA-04]
estimated-files: 3
---

# Stock Management (FIFO)

## Context

Implements the core stock-in and stock-out operations using FIFO (First-In, First-Out) costing. Handles `GoodsReceivedEvent` from Purchasing to add stock batches, and processes stock deductions for sales (consumed by POS via `SaleCompletedEvent`). This is the FIFO deduction engine — the most critical business logic in the Inventory module.

## Prerequisites

- **INV-02** (Data Access) — `InventoryDbContext` with `StockBatch` DbSet
- **INFRA-04** (MediatR) — `GoodsReceivedEvent`, `SaleCompletedEvent`, `GetCurrentStockQuery`

## Wiki References

- `concepts/fifo-costing.md` — "On sale: deduct from batch with oldest receipt date that has QtyRemaining > 0"
- `analysis/cross-module-data-flow.md` — event catalog
- `entities/module-inventory.md` — "central hub", auto-updated on every receive/sale

## Deliverables

```
MerchSys.Inventory/
├── Services/
│   ├── IStockService.vb
│   └── StockService.vb
└── Handlers/
    ├── GoodsReceivedHandler.vb           ← Handles GoodsReceivedEvent
    ├── SaleCompletedHandler.vb           ← Handles SaleCompletedEvent
    └── GetCurrentStockHandler.vb         ← Handles GetCurrentStockQuery
```

## Specification

### IStockService
```
AddStockBatchAsync(productId As Integer, qty As Integer, unitCost As Decimal, receiptDate As DateTime, expiryDate As DateTime?, sourcePOId As Integer?) As Task(Of StockBatch)
DeductStockFIFOAsync(productId As Integer, quantity As Integer) As Task(Of List(Of FIFODeductionResult))
GetCurrentStockAsync(productId As Integer?) As Task(Of List(Of StockLevelDto))
GetStockBatchesAsync(productId As Integer) As Task(Of List(Of StockBatch))
GetTotalValuationAsync() As Task(Of Decimal)
```

### FIFO Deduction Algorithm

```
Function DeductStockFIFOAsync(productId, quantity):
    1. Get all non-expired batches for productId 
       WHERE QtyRemaining > 0 
       ORDER BY ReceiptDate ASC (oldest first)
    
    2. remainingToDeduct = quantity
    3. For Each batch In orderedBatches:
         deductFromBatch = Min(batch.QtyRemaining, remainingToDeduct)
         batch.QtyRemaining -= deductFromBatch
         remainingToDeduct -= deductFromBatch
         Record: (batchId, deductFromBatch, batch.UnitCost) → for COGS
         If remainingToDeduct = 0 Then Exit
       Next
    
    4. If remainingToDeduct > 0, throw InsufficientStockException
    
    5. Return list of FIFODeductionResult (batchId, qtyDeducted, unitCost)
```

### FIFODeductionResult
```
Public Class FIFODeductionResult
    Public Property BatchId As Integer
    Public Property QuantityDeducted As Integer
    Public Property UnitCost As Decimal
    Public Property COGS As Decimal              ' QtyDeducted × UnitCost
End Class
```

### MediatR Handlers

**GoodsReceivedHandler** (handles `GoodsReceivedEvent`):
1. For each item in event, find or create Product by ProductId
2. Call `StockService.AddStockBatchAsync` with item details
3. Log the stock addition

**SaleCompletedHandler** (handles `SaleCompletedEvent`):
1. For each item in event, call `StockService.DeductStockFIFOAsync`
2. The FIFO results (COGS data) are used internally — Accounting gets its data via its own handler of the same event

**GetCurrentStockHandler** (handles `GetCurrentStockQuery`):
1. Call `StockService.GetCurrentStockAsync`
2. Map to `GetCurrentStockResult`

## Implementation Notes

- FIFO order: `ORDER BY ReceiptDate ASC` — oldest batches consumed first
- Skip expired batches (`ExpiryDate < DateTime.UtcNow`) during deduction
- If insufficient stock, throw a custom `InsufficientStockException`
- `AddStockBatchAsync` creates a new batch — does NOT merge with existing batches (each receipt = new batch)

## Acceptance Criteria

1. `dotnet build` succeeds
2. FIFO deduction consumes oldest batches first
3. Expired batches are skipped during deduction
4. Insufficient stock throws exception
5. `GoodsReceivedHandler` creates stock batches
6. `GetCurrentStockHandler` returns correct aggregated levels
7. Stock valuation = Σ(QtyRemaining × UnitCost) across all batches

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-03-summary.md`
