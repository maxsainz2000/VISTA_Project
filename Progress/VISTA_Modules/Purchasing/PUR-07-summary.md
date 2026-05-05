---
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-05
plan-ref: Plans/VISTA_Modules/Purchasing/07-reorder-engine.md
status: completed
---

## Task Summary

Implemented the threshold-based Reorder Suggestion Engine (PUR-07). Adds two new entities, a service interface and implementation, two EF Core configurations, and expands `PurchasingDbContext` with the corresponding DbSets.

**Plan:** `[[07-reorder-engine]]`

## What Was Done

- Created `src/MerchSys.Purchasing/Entities/ReorderConfig.vb` — per-product reorder configuration; stores reorder threshold, safety stock, lead time, seasonal multiplier, and optional preferred vendor FK
- Created `src/MerchSys.Purchasing/Entities/ReorderSuggestion.vb` — generated suggestion record with workflow status ("Pending" / "Accepted" / "Dismissed") and optional link to the resulting PO
- Created `src/MerchSys.Purchasing/Services/IReorderService.vb` — interface defining `GenerateSuggestionsAsync`, `GetPendingSuggestionsAsync`, `AcceptSuggestionAsync`, `DismissSuggestionAsync`, `UpdateConfigAsync`, `GetAllConfigsAsync`
- Created `src/MerchSys.Purchasing/Services/ReorderService.vb` — implementation; sends `GetCurrentStockQuery` via MediatR, computes effective reorder point with optional seasonal multiplier, deduplicates against existing Pending suggestions, and creates a draft PO via `PurchasingDbContext` when a suggestion is accepted
- Created `src/MerchSys.Purchasing/Data/Configurations/ReorderConfigConfiguration.vb` — maps to `Pur_ReorderConfigs`; unique index on `ProductId`; nullable FK to `Pur_Vendors` with `SetNull` delete behaviour
- Created `src/MerchSys.Purchasing/Data/Configurations/ReorderSuggestionConfiguration.vb` — maps to `Pur_ReorderSuggestions`; composite index on `(ProductId, Status)`
- Modified `src/MerchSys.Purchasing/Data/PurchasingDbContext.vb` — added `DbSet(Of ReorderConfig)` and `DbSet(Of ReorderSuggestion)`

## Design Notes

- **Reorder point formula**: `MinimumThreshold` stores the pre-configured reorder point (conceptually `AvgDailyDemand × LeadTimeDays + SafetyStock`, set offline). At runtime the seasonal multiplier is applied when `IsSeasonalItem = True AND SeasonalMultiplier > 1.0`, producing the effective `ReorderPoint` stored on the suggestion.
- **Deduplication**: `GenerateSuggestionsAsync` skips products that already have a "Pending" suggestion to prevent redundant rows on repeated engine runs.
- **Draft PO from suggestion**: `AcceptSuggestionAsync` creates a `PurchaseOrder` in Draft status using `SequentialNumberGenerator`. The single line item carries `UnitCost = 0` as a placeholder — the manager must update pricing before the PO can be submitted (enforced by `PurchaseOrderService.SubmitAsync`).
- **Cross-module boundary**: Stock levels are obtained exclusively via `GetCurrentStockQuery` through MediatR — no direct reference to `MerchSys.Inventory`.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None.

## What's Next

- DI registration: `IReorderService → ReorderService` to be wired in `Application.xaml.vb` during the app bootstrap plan
- EF Core migration to create `Pur_ReorderConfigs` and `Pur_ReorderSuggestions` tables
- UI plan for the Reorder Dashboard (manager review screen)

## Cross-References

- Domain Wiki pages consulted: `[[concepts/reorder-suggestion-engine]]`, `[[entities/module-purchasing]]`
- Codebase Wiki consulted: `[[purchasing/index]]`, `[[purchasing/entities]]`, `[[purchasing/services]]`
