---
module: MerchSys.Purchasing
plan-id: PUR-07
title: "Reorder Suggestion Engine"
depends-on: [PUR-01, PUR-02, INFRA-04]
estimated-files: 3
---

# Reorder Suggestion Engine

## Context

Implements the threshold-based reorder suggestion system. Monitors current stock levels (via MediatR query to Inventory), compares against reorder points, and generates suggestions for the manager to review. Addresses Problem P1 (gut-feel reorders) and P6 (no seasonal demand model).

## Prerequisites

- **PUR-01, PUR-02** — domain models and data access
- **INFRA-04** — `GetCurrentStockQuery` for cross-module stock check

## Wiki References

- `concepts/reorder-suggestion-engine.md` — formula: `Reorder Point = (Avg Daily Demand × Lead Time) + Safety Stock`
- `sources/purchasing-module-paper.md` — "statistical methods only, no ML/AI"
- `entities/module-purchasing.md` — "P1 — Gut-feel reorders", "P6 — No seasonal demand model"

## Deliverables

```
MerchSys.Purchasing/
├── Entities/
│   ├── ReorderConfig.vb             ← Per-product reorder configuration
│   └── ReorderSuggestion.vb         ← Generated suggestion record
├── Services/
│   ├── IReorderService.vb
│   └── ReorderService.vb
```

## Specification

### ReorderConfig Entity
```
Inherits AuditableEntity

Property ProductId As Integer              ' Cross-module reference
Property ProductName As String             ' Denormalized
Property PreferredVendorId As Integer?     ' FK to Vendor
Property MinimumThreshold As Integer       ' Reorder point
Property SafetyStock As Integer            ' Buffer quantity
Property DefaultOrderQuantity As Integer   ' Suggested order qty
Property LeadTimeDays As Integer           ' Days from order to delivery
Property IsSeasonalItem As Boolean         ' Flag for seasonal demand
Property SeasonalMultiplier As Decimal     ' 1.0 = normal, 1.5 = 50% more, etc.
Property IsActive As Boolean               ' Enable/disable monitoring
```

### ReorderSuggestion Entity
```
Inherits AuditableEntity

Property ProductId As Integer
Property ProductName As String
Property CurrentStock As Integer
Property ReorderPoint As Integer
Property SuggestedQuantity As Integer
Property PreferredVendorId As Integer?
Property PreferredVendorName As String
Property EstimatedLeadTimeDays As Integer
Property IsSeasonalAdjusted As Boolean
Property Status As String                  ' "Pending", "Accepted", "Dismissed"
Property ConvertedToPOId As Integer?       ' FK if accepted and converted to PO
```

### IReorderService
```
GenerateSuggestionsAsync() As Task(Of List(Of ReorderSuggestion))
GetPendingSuggestionsAsync() As Task(Of List(Of ReorderSuggestion))
AcceptSuggestionAsync(id As Integer) As Task(Of PurchaseOrder)    ' Creates draft PO
DismissSuggestionAsync(id As Integer) As Task
UpdateConfigAsync(config As ReorderConfig) As Task(Of ReorderConfig)
GetAllConfigsAsync() As Task(Of List(Of ReorderConfig))
```

### Reorder Point Calculation
```
ReorderPoint = (AvgDailyDemand × LeadTimeDays) + SafetyStock

If IsSeasonalItem And currently in peak season:
    ReorderPoint = ReorderPoint × SeasonalMultiplier
```

### Suggestion Generation Flow

1. Send `GetCurrentStockQuery` via MediatR to get all product stock levels
2. For each product with an active `ReorderConfig`:
   - If `CurrentStock ≤ ReorderPoint`, generate a `ReorderSuggestion`
   - Set `SuggestedQuantity = DefaultOrderQuantity` (or calculated based on demand)
3. Save suggestions to database
4. Manager reviews and either accepts (creates draft PO) or dismisses

## Acceptance Criteria

1. `dotnet build` succeeds
2. Suggestions generated when stock ≤ reorder point
3. Seasonal multiplier applied correctly
4. Accepting a suggestion creates a draft PO
5. Queries Inventory via MediatR (no direct reference)

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-07-summary.md`
