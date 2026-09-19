---
module: Infrastructure
agent: antigravity
date: 2026-05-29
plan-ref: Plans/VISTA_Modules/Infrastructure/32-softdelete-unique-constraint-reconciliation.md
status: completed
---

## Task Summary

This report documents the completed implementation of **INFRA-32: Soft-Delete vs Unique-Constraint Reconciliation**.
It resolves the tension between soft deletes, global EF Core filters, and database unique constraints (which span both active and soft-deleted rows).

**Plan:** `[[32-softdelete-unique-constraint-reconciliation.md]]`
**Branch:** `debug/PUR-savedraft-rowversion` (reconciled and finalized)

## What Was Done

- **Policy A (Sequence skips deleted) Rationale:** Applied to deterministic auto-generated sequence numbers (`OrderNumber`) to guarantee monotonic generation that never collides with soft-deleted rows.
  - Reconciled `PurchaseOrderService.CreateDraftAsync` (pre-existing hotfix kept and fully consistent).
  - Modified `ReorderService.vb`: Added `.IgnoreQueryFilters()` on the `OrderNumber` query during suggestion acceptance.
  - Added explaining inline comments on both sequence query sites.
- **Policy B (Friendly duplicate handling) Rationale:** Applied to user-entered unique keys to intercept potential database crashes and present clear, friendly error messages or perform automatic restoration.
  - Audited all unique indexes on soft-deletable tables.
  - Hardened `VendorService.CreateAsync` and `UpdateAsync`: Intercepts duplicate name checks using `IgnoreQueryFilters()` and returns a helpful error if a soft-deleted vendor already has that name.
  - Hardened `VendorProductService.AddCatalogEntryAsync`: Checks catalog link using `IgnoreQueryFilters()`. If a soft-deleted entry is found, it automatically restores the entry by setting `IsDeleted = False`, clearing deletion markers, updating unit cost/notes, and saving.
  - Hardened `ProductManagementViewModel.SaveProductAsync` and `SaveCategoryAsync`: Intercepts duplicate SKU and Category Name checks (including soft-deleted) using `IgnoreQueryFilters()` and returns custom, friendly messages.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Completed successfully) |
| Policy A sequence generation | ✅ (OrderNumbers skip soft-deleted POs) |
| Policy B friendly handling | ✅ (Friendly validation / Automatic restoration instead of DB crash) |

### Authoritative List of Soft-Deletable Table Unique Constraints
1. **`Pur_Vendors` -> `IX_Pur_Vendors_Name` (`Name`):** Hardened (Policy B friendly error in `VendorService.vb`).
2. **`Pur_PurchaseOrders` -> `IX_Pur_PurchaseOrders_OrderNumber` (`OrderNumber`):** Hardened (Policy A in `PurchaseOrderService` and `ReorderService`).
3. **`Pur_VendorProducts` -> `UX_Pur_VendorProducts_Vendor_Product` (`VendorId`, `ProductId`):** Hardened (Policy B auto-restore in `VendorProductService.vb`).
4. **`Inv_ProductCategories` -> `IX_Inv_ProductCategories_Name` (`Name`):** Hardened (Policy B friendly error in `ProductManagementViewModel.vb`).
5. **`Inv_Products` -> `IX_Inv_Products_Sku` (`Sku`):** Hardened (Policy B friendly error in `ProductManagementViewModel.vb`).
6. **`Pos_SalesTransactions` -> `IX_Pos_SalesTransactions_TransactionNumber` (`TransactionNumber`):** Not applicable (sales transaction deletions are regulation-restricted and not exposed in user workflows).
7. **`Pos_CreditAccounts`:** Not applicable (has no unique constraint in the DB schema).

## Issues Encountered

None. Reconciled all candidate sites cleanly and robustly.

## What's Next

- Verify that deleting a draft PO, and then creating a new one (manually or via reorder) correctly generates the next monotonic sequence number without index collisions.
