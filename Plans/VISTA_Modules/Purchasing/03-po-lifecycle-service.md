---
module: MerchSys.Purchasing
plan-id: PUR-03
title: "PO Lifecycle Service"
depends-on: [PUR-01, PUR-02]
estimated-files: 3
---

# PO Lifecycle Service

## Context

Implements the Purchase Order state machine: Draft → Submitted → Received → Verified → Closed. Manages PO creation, number generation, status transitions, total recalculation, and validation rules.

## Prerequisites

- **PUR-01** (Domain Models) — `PurchaseOrder`, `PurchaseOrderLine` entities
- **PUR-02** (Data Access) — `PurchasingDbContext` with DbSets

## Wiki References

- `entities/module-purchasing.md` — PO lifecycle, user roles
- `sources/purchasing-module-paper.md` — "Draft → Submitted → Received → Verified" lifecycle

## Deliverables

```
MerchSys.Purchasing/
├── Services/
│   ├── IPurchaseOrderService.vb
│   └── PurchaseOrderService.vb
└── Helpers/
    └── SequentialNumberGenerator.vb    ← PO-YYYY-XXXX generation
```

## Specification

### IPurchaseOrderService
```
Public Interface IPurchaseOrderService
    Function CreateDraftAsync(vendorId As Integer,
                              lines As List(Of CreatePOLineDto),
                              Optional notes As String = Nothing,
                              Optional expectedDeliveryDate As DateTime? = Nothing) As Task(Of PurchaseOrder)
    Function GetByIdAsync(id As Integer) As Task(Of PurchaseOrder)
    Function GetAllAsync(Optional status As PurchaseOrderStatus? = Nothing) As Task(Of List(Of PurchaseOrder))
    Function UpdateDraftAsync(id As Integer,
                              lines As List(Of CreatePOLineDto),
                              Optional notes As String = Nothing,
                              Optional expectedDeliveryDate As DateTime? = Nothing) As Task(Of PurchaseOrder)
    Function SubmitAsync(id As Integer) As Task(Of PurchaseOrder)
    Function MarkReceivedAsync(id As Integer) As Task(Of PurchaseOrder)
    Function VerifyAsync(id As Integer) As Task(Of PurchaseOrder)
    Function CloseAsync(id As Integer) As Task(Of PurchaseOrder)
    Function DeleteDraftAsync(id As Integer) As Task(Of Boolean)
End Interface
```

> **Amendment (PUR-09):** `notes` and `expectedDeliveryDate` were added as optional parameters after `PurchaseOrderListView` (PUR-09) identified they were collected in the UI but had no service-layer path to persist. Using `Optional` keeps all existing call sites unchanged.

### CreatePOLineDto
```
Public Class CreatePOLineDto
    Public Property ProductId As Integer
    Public Property ProductName As String
    Public Property QuantityOrdered As Integer
    Public Property UnitCost As Decimal
End Class
```

### State Machine Rules

| Current State | Allowed Transitions | Who Can |
|---|---|---|
| Draft | Submit, Delete | Manager |
| Submitted | MarkReceived | Manager |
| Received | Verify | Manager |
| Verified | Close | Manager |
| Closed | *(terminal state)* | — |

- Only **Draft** POs can be edited or deleted
- **Submit** validates: must have ≥1 line, all quantities > 0, all costs > 0
- **MarkReceived** is triggered after goods receiving (Plan PUR-04)
- **Verify** confirms all goods receipt lines match PO lines
- **Close** marks the PO as complete; triggers AP entry creation

### Sequential Number Generator

```
' Generates: PO-2026-0001, PO-2026-0002, ...
' Pattern: {prefix}-{year}-{4-digit sequence}
' Sequence resets each year
' Thread-safe: query max existing number for current year, increment
```

Reuse this generator for GR numbers (GR-YYYY-XXXX) in Plan PUR-04.

### Total Recalculation

`PurchaseOrder.TotalAmount` = Sum of all `PurchaseOrderLine.LineTotal`
`PurchaseOrderLine.LineTotal` = `QuantityOrdered × UnitCost`

Recalculate on every add/update/remove of lines.

## Implementation Notes

- All status transitions should throw `InvalidOperationException` for invalid transitions
- Use `PurchasingDbContext` for all data access — no repository abstraction needed
- Include `.Include(Function(po) po.Lines).Include(Function(po) po.Vendor)` in read queries
- Register `IPurchaseOrderService` / `PurchaseOrderService` in DI (add a module-level DI extension method)
- In `CreateDraftAsync` and `UpdateDraftAsync`: if `notes` is non-Nothing, assign `po.Notes = notes`; if `expectedDeliveryDate` has a value, assign `po.ExpectedDeliveryDate = expectedDeliveryDate`. Callers that omit the optional params retain existing behaviour.

## Acceptance Criteria

1. `dotnet build` succeeds
2. State machine enforces valid transitions only
3. Invalid transitions throw `InvalidOperationException`
4. PO numbers follow `PO-YYYY-XXXX` pattern
5. Total recalculates on line changes
6. Only Draft POs can be edited/deleted
7. `notes` and `expectedDeliveryDate` optional params are persisted when provided; omitting them leaves existing values unchanged

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-03-summary.md`
