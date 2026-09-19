---
module: MerchSys.Inventory
plan-id: INV-15
title: "Stock Dashboard — FIFO Cost Column"
depends-on: [INV-05, INV-10]
estimated-files: 4
priority: medium
---

# Stock Dashboard — FIFO Cost Column

## Context

The Stock Dashboard's per-product grid shows a column labelled **"Price"** that renders `ProductSummaryDto.RetailPrice` (`MerchSys.Inventory/Services/StockDashboardService.vb:156`). That value is sourced from `Inv_Products.RetailPrice` — the **selling** price set in Product Management — not the per-batch purchase cost the store actually paid.

This was discovered during hands-on multi-vendor purchasing testing on 2026-05-27: after receiving the same product (Ammonium Sulfate) from three vendors at ₱1,000, ₱1,100, and ₱1,200 per unit, the dashboard still showed a single per-row price of ₱1,100 (the retail price) with no indication of the spread between vendor costs. The aggregate `TotalStockValue` card *is* correct (it sums `QuantityRemaining × UnitCost` across non-expired batches at line 116-117) — the bug is **per-row visibility only**.

Promoted from `Plans/Future/deferred-features-backlog.md` item 18a (2026-05-27).

This plan is the UI-visibility half of the bug triplet:
- **18a (this plan, INV-15)** — surface per-product purchase cost on the dashboard
- **18b (ACC-21)** — fix the accounting handler so COGS uses *actual* per-batch costs, not the FIFO-oldest unit cost applied to the entire sale quantity
- **18c (ACC-22)** — eliminate the duplicate `RevenueRecord` race between the legacy and VAT accounting handlers

18a is independent of 18b/c — it is a read-only display change inside the Inventory module and does not touch any accounting code or event contract.

## Prerequisites

- **INV-05** (Stock Dashboard Service) — `StockDashboardService.GetDashboardDataAsync`, `ProductSummaryDto`
- **INV-10** (Stock Dashboard View) — XAML `DataGrid`, column layout

## Wiki References

- `concepts/fifo-costing.md` — FIFO is per-batch; "cost" is not a single scalar per product
- `analysis/cross-module-data-flow.md` — `StockBatch.UnitCost` is the authoritative cost source

## Deliverables

```
MerchSys.Inventory/Services/
├── StockDashboardService.vb        ' MOD — compute weighted-average cost per product, populate new DTO fields
└── IStockDashboardService.vb       ' unchanged (return shape extended via DTO only)

MerchSys.Inventory/Dtos/
└── ProductSummaryDto.vb            ' MOD — add AverageUnitCost, FifoOldestUnitCost properties

MerchSys.App/Views/Inventory/
└── StockDashboardView.xaml         ' MOD — rename "Price" column to "Retail Price"; add "Avg Cost" and "FIFO Cost" columns
```

No new files. Four files modified.

## Specification

### `ProductSummaryDto` additions

```vb
''' <summary>
''' Weighted-average purchase cost across non-expired batches with remaining stock:
''' SUM(QuantityRemaining × UnitCost) / SUM(QuantityRemaining).
''' Zero when no non-expired stock remains.
''' </summary>
Public Property AverageUnitCost As Decimal

''' <summary>
''' UnitCost of the FIFO-oldest non-expired batch with remaining stock —
''' the unit cost that the Inventory FIFO deduction engine would consume next.
''' Zero when no non-expired stock remains.
''' </summary>
Public Property FifoOldestUnitCost As Decimal
```

The existing `RetailPrice` field is kept — the dashboard now shows both retail and cost side-by-side instead of conflating them.

### `StockDashboardService.GetDashboardDataAsync` change

Inside the existing `For Each product In products` loop, after `nonExpiredBatches` is computed, add:

```vb
Dim totalNonExpiredQty As Integer = nonExpiredBatches.Sum(Function(b) b.QuantityRemaining)
Dim weightedCost As Decimal = 0D
Dim fifoOldestCost As Decimal = 0D
If totalNonExpiredQty > 0 Then
    weightedCost = nonExpiredBatches.Sum(Function(b) CDec(b.QuantityRemaining) * b.UnitCost) / CDec(totalNonExpiredQty)
    fifoOldestCost = nonExpiredBatches.
        OrderBy(Function(b) b.ReceiptDate).
        First().UnitCost
End If
```

Populate the new DTO fields:

```vb
summaries.Add(New ProductSummaryDto With {
    ...
    .RetailPrice = product.RetailPrice,
    .AverageUnitCost = weightedCost,
    .FifoOldestUnitCost = fifoOldestCost,
    ...
})
```

`stockValue` already equals `nonExpiredBatches.Sum(Function(b) CDec(b.QuantityRemaining) * b.UnitCost)` (line 116). The weighted-average computation is the same numerator divided by the matching quantity sum, so both values can share the intermediate sum if a tiny refactor is preferred — but it is fine to compute it once more for readability; the row counts are ≈50 SKUs.

`fifoOldestCost` uses `OrderBy(ReceiptDate).First()` because that is **exactly** what `GetProductCostQueryHandler` (`MerchSys.Inventory/Handlers/GetProductCostQueryHandler.vb:28`) returns. Same ordering, same predicate — so the dashboard's FIFO cost column matches what the (pre-ACC-21) accounting handler would record as COGS unit cost. If ACC-21 ships and changes COGS to per-batch breakdown, **this column still represents the next-batch-out cost** — which is independently useful to a Manager planning their next price tag.

### `StockDashboardView.xaml` change

In the per-product `DataGrid`, replace the single `Price` column with three:

| Header | Binding | Format |
|---|---|---|
| Retail Price | `RetailPrice` | `{0:C}` |
| Avg Cost | `AverageUnitCost` | `{0:C}` |
| FIFO Cost | `FifoOldestUnitCost` | `{0:C}` |

Header tooltips:
- **Retail Price** — "Selling price set in Product Management."
- **Avg Cost** — "Weighted-average purchase cost across remaining non-expired batches."
- **FIFO Cost** — "Unit cost of the oldest batch — the cost the next sale will draw from."

No new converters required (currency formatting works as today). No ViewModel binding changes needed — the new DTO fields surface through the existing `Products` collection.

If column width is tight, leave the existing layout in place and let the user resize. Do not introduce a tab or a popover for this — the whole point is visibility at a glance.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors and 0 warnings.
2. With three batches of the same product at ₱1,000, ₱1,100, ₱1,200 per unit (10 units each, no expiry), the row shows: Avg Cost ₱1,100.00, FIFO Cost ₱1,000.00 (or whichever ReceiptDate was earliest).
3. With zero remaining non-expired stock, both Avg Cost and FIFO Cost render ₱0.00 — no division-by-zero exception.
4. `TotalStockValue` aggregate card value is unchanged (already correct).
5. The Retail Price column still shows `Inv_Products.RetailPrice` — the previous behaviour for that field is preserved, only renamed and accompanied by new columns.
6. Manager and Owner can both view the dashboard — no role check changes.

## Out of Scope (Defer)

- **Editing cost from the dashboard.** Costs live in `StockBatch.UnitCost` which is immutable per the FIFO design. Any "fix the cost" UI is a separate, larger change.
- **Per-batch breakdown in the grid row.** The product detail drawer (`GetProductDetailAsync`) already shows batch-level cost when the user clicks a row. Do not duplicate that here.
- **Historical cost trend chart.** Out of scope — vendor-cost history is implicit via `StockBatch` and explicitly out of scope per INV-14's scope decision.
- **Touching `GetProductCostQueryHandler`.** The COGS-correctness fix is ACC-21's responsibility, not this plan's.

## Notes for Implementers

- The weighted-average formula must use **non-expired batches with `QuantityRemaining > 0`** to match `stockValue`. If you accidentally divide the full `stockValue` by `currentStock` you will get the right number — those are the same set — but be explicit about the predicate so a future refactor of `stockValue` doesn't silently break the cost column.
- **VB.NET trap — `Enumerable.Count` vs `.Count(predicate)`** is already a known issue in `StockDashboardService` (see lines 141-142 where `Enumerable.Count` is explicitly used). Do not regress that pattern when adding new aggregates.
- **VB.NET trap — parameter shadows property:** if you introduce a lambda parameter named `product` it will shadow the outer `For Each product` loop variable. Use `b` or `batch` inside the lambdas as the existing code does.
- Confirm `StockDashboardService.GetDashboardDataAsync` line numbers at implementation time — the 116 / 156 references are from the 2026-05-27 read.
