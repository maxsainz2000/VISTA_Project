---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-27
plan-ref: Plans/VISTA_Modules/Inventory/15-stock-dashboard-cost-column.md
status: completed
---

## Task Summary

Enhanced the Stock Dashboard product list grid by replacing the single "Price" column (which rendered the retail price) with three distinct columns: `Retail Price`, `Avg Cost`, and `FIFO Cost`. This provides store managers with direct visibility into the purchase cost spread and next-sale unit costs per SKU.

**Plan:** `[[15-stock-dashboard-cost-column.md]]`

## What Was Done

- **Modified:** `WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/IStockDashboardService.vb` — added properties `AverageUnitCost As Decimal` and `FifoOldestUnitCost As Decimal` to `ProductSummaryDto` class inside the file.
- **Modified:** `WPF_Applications/MerchSys/src/MerchSys.Inventory/Services/StockDashboardService.vb` — calculated weighted average cost across remaining non-expired batches, retrieved FIFO oldest non-expired batch unit cost, and populated the DTO. Avoided variable shadowing inside lambdas.
- **Modified:** `WPF_Applications/MerchSys/src/MerchSys.App/Views/Inventory/StockDashboardView.xaml` — replaced the single Price column with three columns incorporating bindings, currency formatting `₱{0:N2}`, right-alignment, and helpful tooltips for each header.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Completed successfully with 0 errors and 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified WPF grid columns display properly) |

## Division-by-Zero Safety

Verified that if `currentStock = 0` (product has no remaining non-expired stock), the `weightedCost` is set to `0D` unconditionally, and no `DivideByZeroException` can occur during calculation in `StockDashboardService.vb`:
```vb
Dim weightedCost As Decimal = 0D
Dim fifoOldestCost As Decimal = 0D
If currentStock > 0 Then
    weightedCost = stockValue / CDec(currentStock)
    ...
End If
```

## XAML DataGrid Columns Definition

```xml
                        <DataGridTextColumn Width="85"
                                            SortMemberPath="RetailPrice"
                                            Binding="{Binding RetailPrice, StringFormat='₱{0:N2}'}">
                            <DataGridTextColumn.Header>
                                <TextBlock Text="Retail Price" ToolTip="Selling price set in Product Management." />
                            </DataGridTextColumn.Header>
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="Padding" Value="0,0,4,0"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>

                        <DataGridTextColumn Width="80"
                                            SortMemberPath="AverageUnitCost"
                                            Binding="{Binding AverageUnitCost, StringFormat='₱{0:N2}'}">
                            <DataGridTextColumn.Header>
                                <TextBlock Text="Avg Cost" ToolTip="Weighted-average purchase cost across remaining non-expired batches." />
                            </DataGridTextColumn.Header>
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="Padding" Value="0,0,4,0"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>

                        <DataGridTextColumn Width="80"
                                            SortMemberPath="FifoOldestUnitCost"
                                            Binding="{Binding FifoOldestUnitCost, StringFormat='₱{0:N2}'}">
                            <DataGridTextColumn.Header>
                                <TextBlock Text="FIFO Cost" ToolTip="Unit cost of the oldest batch — the cost the next sale will draw from." />
                            </DataGridTextColumn.Header>
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="Padding" Value="0,0,4,0"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
```

## Cross-References

- Domain Wiki pages consulted: `[[fifo-costing.md]]`, `[[modular-monolith.md]]`
