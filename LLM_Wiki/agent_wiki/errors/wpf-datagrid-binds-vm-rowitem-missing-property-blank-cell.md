---
type: error-fix
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-29
tags: [wpf, mvvm, data-binding, datagrid, viewmodel, dto, silent-bug, vb-net]
error-code: none
severity: logic-bug
---

## Problem

On the Stock Dashboard, the **Avg Cost** and **FIFO Cost** columns rendered as completely
blank cells (not even `₱0.00`) for products that clearly had stock — e.g. Complete
Fertilizer (10 units) and Urea (20 units) — while **Current Stock**, **Stock Value**, and
**Retail Price** in the same rows displayed correctly. No exception was thrown.

DB was correct (`Inv_StockBatches.UnitCost` = 1500/1600/1700) and
`StockDashboardService.GetDashboardDataAsync` correctly computed `AverageUnitCost` /
`FifoOldestUnitCost` into `ProductSummaryDto`.

## Root Cause

The DataGrid binds to `Products` = `ObservableCollection(Of ProductRowItem)` — a **flattened
VM row type**, not the service DTO. `ProductRowItem` was missing the `AverageUnitCost` and
`FifoOldestUnitCost` properties, and `StockDashboardViewModel.LoadDataAsync`'s
`ProductSummaryDto` → `ProductRowItem` projection never copied them.

The XAML columns `Binding="{Binding AverageUnitCost}"` / `{Binding FifoOldestUnitCost}`
therefore resolved to a non-existent path. **WPF binding failures are silent** — they emit a
trace warning at most and leave the cell empty, so the bug never surfaced as an error. The
`StringFormat='₱{0:N2}'` never ran (no source value to format), which is why the cells were
truly blank rather than `₱0.00`.

This is the classic trap of a two-layer model: the service computes a value into a DTO, but
a separate bindable row class is what the view actually consumes. Any field added to the DTO
must also be added to the row class **and** the mapping, or it silently vanishes.

## Fix

```vb
' ProductRowItem — add the two missing properties
Public Property AverageUnitCost As Decimal
Public Property FifoOldestUnitCost As Decimal

' LoadDataAsync projection — copy them from the DTO
Return New ProductRowItem With {
    ...
    .StockValue = p.StockValue,
    .AverageUnitCost = p.AverageUnitCost,        ' added
    .FifoOldestUnitCost = p.FifoOldestUnitCost,  ' added
    .StockStatus = p.StockStatus,
    ...
}
```

## Prevention

- When a DataGrid column shows **blank with no error**, suspect a missing/typo'd binding path
  first — confirm the bound type actually exposes that property. WPF won't tell you loudly.
- When a ViewModel flattens a service DTO into a separate bindable row class, treat the row
  class + its projection as part of the DTO's contract: adding a DTO field is a three-edit
  change (DTO, row class, mapping). A blank column is the symptom of forgetting edits 2–3.
- Temporary diagnostic: set `PresentationTraceSources.TraceLevel=High` on a suspect binding
  (or watch the Output window for `System.Windows.Data Error`) to make silent binding
  failures visible.

## Related

- `[[efcore-vat-ledger-columns-missing-central-schema]]` — same session; the goods-receipt
  path that produced the stock batches whose cost wasn't displaying.
- `[[classlib-viewmodel-auto-refresh-timer]]` — the same dashboard VM's refresh pattern.
