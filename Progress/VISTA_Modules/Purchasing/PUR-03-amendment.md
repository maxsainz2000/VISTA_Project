---
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-05
plan-ref: Plans/VISTA_Modules/Purchasing/03-po-lifecycle-service.md
status: completed
---

## Task Summary

Retrofitted `IPurchaseOrderService` (PUR-03) to persist `Notes` and `ExpectedDeliveryDate` on draft create and update. This gap was identified during PUR-09 (PO Management View) where the editor UI collected both fields but had no service path to save them.

## What Was Done

- Modified `src/MerchSys.Purchasing/Services/IPurchaseOrderService.vb` — Added `Optional notes As String = Nothing` and `Optional expectedDeliveryDate As DateTime? = Nothing` to `CreateDraftAsync` and `UpdateDraftAsync` signatures.
- Modified `src/MerchSys.Purchasing/Services/PurchaseOrderService.vb` — `CreateDraftAsync`: assigns both fields directly in the object initializer. `UpdateDraftAsync`: conditionally updates `po.Notes` when non-Nothing, and `po.ExpectedDeliveryDate` when `HasValue`, preserving existing values when callers omit the params.
- Modified `src/MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb` — Updated `SaveDraftAsync` and `SubmitFromEditorAsync` to pass `Editor.Notes` and `Editor.ExpectedDeliveryDate` through to the service calls.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

None. The `Optional` parameter approach ensured all existing call sites (including `ReorderService.AcceptSuggestionAsync` which calls `CreateDraftAsync`) compiled without modification.

## Cross-References

- Identified by: `Progress/VISTA_Modules/Purchasing/PUR-09-summary.md`
- Plan updated: `Plans/VISTA_Modules/Purchasing/03-po-lifecycle-service.md`
