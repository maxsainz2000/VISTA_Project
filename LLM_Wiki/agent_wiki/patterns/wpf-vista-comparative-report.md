---
type: pattern
module: MerchSys.Accounting
agent: antigravity
date: 2026-06-10
tags: [wpf, xaml, vb-net, mvvm, comparative-reports, delta-indicator, reporting]
---

## Context

This pattern applies when implementing a comparative (two-period or multi-period) financial report (such as a comparative Income Statement or Balance Sheet) in WPF, particularly under the MVVM architecture in VB.NET. It addresses three core problems:
1. Surfacing a prior-period DTO side-by-side with the current period's data.
2. Displaying per-line trend/change indicators (`DeltaIndicator`) with correct business semantics (e.g., whether rising values are good or bad).
3. Providing a friendly name selector (like month names) without clashing with the underlying integer bindings used in queries and exports.

## The Pattern

### 1. Surfacing Prior-Period Data (Additive Read-Only)
Instead of executing new database calls, reuse the prior-period DTO which is typically already generated or can be queried in a single async invocation block. Expose the prior-period amounts as formatted display strings from the ViewModel. If the prior period is not available (e.g., no baseline data exists), return clean empty defaults to hide indicators and display blank spaces rather than misleading `0%` or `∞` values.

```vb
' VM code to calculate delta and toggle visibility
Private Sub CalculateDelta(currentVal As Decimal, priorVal As Decimal, ByRef deltaValue As Double, ByRef showDelta As Boolean)
    If priorVal = 0D Then
        deltaValue = 0.0
        showDelta = False
    Else
        deltaValue = CDbl(Math.Round(((currentVal - priorVal) / Math.Abs(priorVal)) * 100D, 1))
        showDelta = True
    End If
End Sub
```

### 2. Standardized Delta Indicators with Inverted Semantics
When rendering the comparative table, use the shared `DeltaIndicator` control and configure `InvertSemantics` per row:
- **`InvertSemantics="False"`** (Default): Rising values are positive/good (Net Sales, Gross Profit, Net Income).
- **`InvertSemantics="True"`**: Rising values are negative/bad (COGS, Operating Expenses, Shrinkage Loss).

```xml
<!-- Net Sales (Normal Semantics) -->
<views:DeltaIndicator Grid.Row="2" Grid.Column="3"
                      Percent="{Binding NetSalesDelta}"
                      InvertSemantics="False"
                      Visibility="{Binding ShowNetSalesDelta, Converter={StaticResource BoolToVis}}"/>

<!-- Cost of Goods Sold (Inverted Semantics) -->
<views:DeltaIndicator Grid.Row="3" Grid.Column="3"
                      Percent="{Binding COGSDelta}"
                      InvertSemantics="True"
                      Visibility="{Binding ShowCOGSDelta, Converter={StaticResource BoolToVis}}"/>
```

### 3. Friendly Value Mapping for Selectors
To present readable labels (e.g. "January" instead of "1") in a ComboBox without breaking the underlying integer bindings, wrap selection options in a helper class (e.g. `MonthOption`) and utilize WPF's `SelectedValuePath` and `DisplayMemberPath` properties.

```vb
Public Class MonthOption
    Public Property Number As Integer
    Public Property Name As String
    Public Sub New(num As Integer, nm As String)
        Number = num
        Name = nm
    End Sub
End Class

Public ReadOnly Property AvailableMonths As IReadOnlyList(Of MonthOption)
    Get
        Return New List(Of MonthOption) From {
            New MonthOption(1, "January"),
            ...
        }
    End Get
End Property
```

```xml
<ComboBox ItemsSource="{Binding AvailableMonths}"
          SelectedValue="{Binding SelectedMonth, Mode=TwoWay}"
          SelectedValuePath="Number"
          DisplayMemberPath="Name"/>
```

## Why It Works

- **Performance**: Exposing prior-period values as simple read-only properties derived from already-fetched data avoids redundant database network queries.
- **WPF Data Binding Discipline**: Binding `ComboBox.SelectedValue` combined with `SelectedValuePath="Number"` ensures that the VM's integer property (`SelectedMonth`) is directly modified when a user selects a month name, preserving compatibility with all downstream loaders, calculations, and CSV/PDF export streams.
- **Legibility and Visual Cleanliness**: Hiding delta indicators when `ShowXxxDelta` is false prevents divide-by-zero errors and keeps the screen clean when no historical data exists.

## Rules

- **Always configure `InvertSemantics` correctly**: Set it to `True` for expenses, COGS, and losses, and `False` for revenues, profits, and incomes.
- **Never display `0.0%` or `NaN%` when prior baseline is missing**: Wrap delta indicators with a Boolean visibility check bound to `ShowXxxDelta` in the View.
- **Sync CSV/PDF exports**: Ensure both printable PDF reports and CSV exports are extended to include the new columns (**Section | Current | Prior | Change**) to maintain on-screen parity.

## Related

- Links to related entries: `[[wpf-vista-trend-indicators]]`, `[[wpf-vista-print-export]]`
- Links to Domain Wiki pages: `[[wpf-vista-formatting]]`
