---
module: MerchSys.Purchasing
plan-id: PUR-13
title: "View — Reorder Suggestions"
depends-on: [PUR-07]
estimated-files: 3
---

# View — Reorder Suggestions

## Context

WPF View and ViewModel for reviewing reorder suggestions — manager sees products below threshold, accepts (auto-creates draft PO) or dismisses suggestions, and configures per-product reorder settings.

## Prerequisites

- **PUR-07** (Reorder Engine) — `IReorderService` implemented

## Deliverables

```
MerchSys.App/Views/Purchasing/
├── ReorderSuggestionsView.xaml
└── ReorderSuggestionsView.xaml.vb

MerchSys.Purchasing/ViewModels/
└── ReorderSuggestionsViewModel.vb
```

## Specification

### Suggestions List

- **Generate button** — runs `GenerateSuggestionsAsync` and refreshes list
- **DataGrid:** ProductName, CurrentStock, ReorderPoint, SuggestedQty, PreferredVendor, EstLeadTime, Status, IsSeasonalAdjusted
- **Actions per row:** Accept (creates draft PO), Dismiss
- **Filter tabs:** Pending, Accepted, Dismissed
- **Seasonal indicator** — icon/badge for seasonally-adjusted suggestions

### Configuration Tab/Panel

- DataGrid of `ReorderConfig` records: ProductName, MinThreshold, SafetyStock, DefaultOrderQty, LeadTimeDays, IsSeasonalItem, SeasonalMultiplier, IsActive
- Edit in-place or dialog
- Enable/disable monitoring per product

### Accept Flow

1. Click Accept → calls `AcceptSuggestionAsync`
2. Service creates a draft PO with the suggested vendor and qty
3. Toast notification: "Draft PO PO-YYYY-XXXX created"
4. Suggestion status changes to "Accepted"

## Acceptance Criteria

1. `dotnet build` succeeds
2. Generate produces suggestions for below-threshold products
3. Accept creates a draft PO
4. Dismiss removes from pending list
5. Configuration editable per product

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-13-summary.md`
