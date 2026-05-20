---
test-id: PUR-Test-1 (sub-issue b)
checklist: PUR-verification-checklist.md
branch: debug/PUR-test-1b
started: 2026-05-20T00:00
status: resolved
---

# Debug Session — PUR Test 1b (vendor dropdown blank)

## Problem Statement

After fixing the `GoodsReceiptLineConfiguration` enum type mismatch (PUR-test-1),
the Purchase Orders tab loads but the vendor ComboBox is empty — no vendors appear
despite 4 non-deleted rows existing in `Pur_Vendors`.

## Starting State
- **Commit:** `ea34244`
- **Build status:** clean (0 errors, 0 warnings)
- **Confirmed via DB query:** 4 vendors in `Pur_Vendors` with `IsDeleted=0`
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb`

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.Purchasing/ViewModels/PurchaseOrderListViewModel.vb` — the only
  file that controls how vendor data is loaded and passed to the editor

### Off-limits (do NOT touch)
- `MerchSys.SharedKernel/` — shared contracts
- Other modules' services/handlers

---

## Attempt Log

### Attempt 1
- **Hypothesis:** `LoadDataAsync` uses concurrent `Task.WhenAll` on a shared `PurchasingDbContext`
  (not thread-safe). Exception silently swallowed by fire-and-forget constructor call; `_vendorList`
  stays empty.
  Fix: replace with sequential awaits.
- **Changed:** `PurchaseOrderListViewModel.vb` — replaced `Task.WhenAll` with sequential awaits
- **Verdict:** ❌ Still svc=0

### Attempt 2 (diagnostic series — EF materialization)
- **Finding:** EF `CountAsync()` = 5, `Select(v.Id).ToListAsync()` = [1,2,3,4,5], but
  `ToListAsync()` for full `Vendor` entity = 0. EF Core 10 VB.NET silently returns empty list
  for full entity materialization; scalar projections and COUNT work.
- **Fix:** Switched to raw `SqliteConnection` (fresh, independent of EF's lifecycle) to load vendors.
  Used synchronous `reader.Read()` loop, writing directly to `_vendorList` (class field).
- **Verdict:** ⚠️ Partial — `loop=4 pre=4 post=4 final=0`. Raw load worked (4 vendors added),
  but count dropped to 0 AFTER `Editor.LoadVendors(_vendorList)`.

### Attempt 3 — Root Cause Identified
- **Hypothesis:** `PurchaseOrderEditorViewModel.LoadVendors(vendors As List(Of Vendor))` —
  VB.NET is case-insensitive, so `vendors` (the parameter) and `Vendors` (the ObservableCollection
  property) resolve to the same identifier inside the method body. The parameter shadows the
  property. `Vendors.Clear()` clears the input `List(Of Vendor)` (our `_vendorList`), not
  the ObservableCollection. The `For Each` then iterates the now-empty list; nothing gets added
  to either the collection or the list. Same shadowing bug in `PrepareForNew` and `LoadFromPO`.
- **Changed:**
  - `PurchaseOrderEditorViewModel.vb` — renamed `vendors` parameter to `vendorList` in
    `LoadVendors`, `PrepareForNew`, and `LoadFromPO`
  - `PurchaseOrderListViewModel.vb` — removed diagnostic code; kept raw `SqliteConnection`
    vendor load (EF materialization remains broken); clean status message
- **Build result:** ✅ 0 errors, 0 warnings
- **Runtime result:** ✅ Vendors appear in the dropdown
- **Verdict:** ✅ Fixed
- **Action:** `git commit e2baa86`

---

## Resolution

- **Status:** resolved
- **Root cause:** VB.NET case-insensitive identifier resolution — parameter `vendors` in
  `PurchaseOrderEditorViewModel.LoadVendors` shadows the `Vendors` property. `Vendors.Clear()`
  cleared the input list; the ObservableCollection was never populated.
  Secondary issue: EF Core 10 VB.NET `ToListAsync()` silently returns empty for full entity
  queries (not the root cause of the visible bug, but confirmed and worked around).
- **Fix description:** Renamed `vendors` → `vendorList` in all three affected methods in
  `PurchaseOrderEditorViewModel.vb`. Kept raw ADO.NET vendor load in `PurchaseOrderListViewModel.vb`
  as workaround for EF materialization issue.
- **Final commit:** `e2baa86`
- **Agent wiki entry needed?** Yes — two entries:
  1. `vbnet-parameter-shadows-property` (antipattern) — parameter name case-insensitively matches a property
  2. `efcore-vbnet-tolistasync-empty` (error-fix) — EF Core 10 VB.NET full entity ToListAsync returns empty
