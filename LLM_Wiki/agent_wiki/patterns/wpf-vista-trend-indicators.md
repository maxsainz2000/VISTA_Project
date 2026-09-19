---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-03
tags: [wpf, xaml, vb-net, mvvm, trend-indicators, delta-indicator, sparkline]
---

# WPF Vista Trend & Delta Indicators Pattern

## Context

On administrative and decision-making dashboards, displaying absolute metrics (e.g. "MTD Revenue is ₱100,000") is often insufficient without context. Dashboard users need to see period-over-period direction (e.g. "up 12% vs last month") to make informed operational decisions. This pattern establishes the implementation of **read-only, additive** trend & delta indicators, including a reusable arrow-indicator control (`DeltaIndicator`) and a lightweight inline trend chart (`Sparkline`).

## The Pattern

### 1. Presentation-Only Reusable Controls

All visual behaviors are encapsulated in markup triggers that automatically resolve tokens via `DynamicResource`. No code-behind handles direct theme-switching or color calculations, keeping them theme-reactive and clean of inline hex colors.

#### DeltaIndicator
- **Percent** (`Double`): The percentage change value. Formatted in XAML using `StringFormat='{}{0:+0.0;-0.0;0.0}%'`.
- **InvertSemantics** (`Boolean`): Set to `True` for liability or negative-meaning metrics (e.g., Accounts Payable, Overdue count, Expiry counts) where an upward movement is bad (Danger) and downward is good (Success).
- **Direction** (`DeltaDirection` - Computed Read-Only): `Up` for values > 0.001, `Down` for values < -0.001, and `Flat` otherwise.

#### Sparkline
- **Points** (`IEnumerable(Of Double)`): Point series plotted as adjacent bars. Auto-scales bar heights to fit a tiny inline chart container (24px height limit) without using heavy charting libraries.

### 2. Additive VM Design (Tier 2 Principle)

All deltas are **derived read-only values** computed at load time. They must not modify any existing ViewModel property, write path, command, or concurrency handler.

```vb
' Exposing Sparkline points and Delta percentage in FinancialOverviewViewModel
Public Property RevenueSparkPoints As IEnumerable(Of Double)
' ...
Public Property RevenueDeltaPercent As Double

' Calculation in LoadDataAsync:
RevenueSparkPoints = data.MonthlyTrend.Select(Function(t) CDbl(t.Revenue)).ToList()
If data.MonthlyTrend.Count >= 2 Then
    Dim priorMonthRevenue = data.MonthlyTrend(data.MonthlyTrend.Count - 2).Revenue
    If priorMonthRevenue > 0 Then
        RevenueDeltaPercent = CDbl(Math.Round(((MonthToDateRevenue - priorMonthRevenue) / priorMonthRevenue) * 100D, 1))
    Else
        RevenueDeltaPercent = 0.0
    End If
End If
```

## Why It Works

By separating the visual trigger logic (`MultiDataTrigger` on `Direction` + `InvertSemantics` in XAML) from the state calculation, we leverage the WPF layout engine to dynamically apply theme brushes (`SuccessBrush`, `DangerBrush`, `TextSecondaryBrush`). Renaming loop variables inside the VB.NET code-behind to avoid clashing with global namespace functions (like `Val()`) prevents overload resolution compilation failures (BC30516).

## Rules

- **Derive Read-Only**: Never create a new database write path or modify existing business rule VM properties when adding deltas.
- **Skip Speculative Queries**: If prior-period data is not cheaply available in the active service (e.g. historical stock value or AR/AP without date parameters), skip the delta and document it in the progress summary.
- **Invert Semantics**: Always set `InvertSemantics=True` when applying delta indicators to liability, overdue, or expiring-soon counts.
- **Avoid keyword clash**: Never use the variable name `val` in VB.NET loops as it collides with the built-in `Val()` function in the `Microsoft.VisualBasic` namespace.
- **Full Root Prefix on namespace**: When declaring namespaces in XAML, use the full root prefix (e.g. `xmlns:local="clr-namespace:MerchSys.App.Views.Shell"`) to avoid compilation issues.

## Related

- Patterns: `[[wpf-vista-dashboard-layout]]`
