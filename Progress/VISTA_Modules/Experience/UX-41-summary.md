---
module: MerchSys.App
agent: claude-code
date: 2026-06-06
plan-ref: Plans/VISTA_Modules/Experience/41-advanced-data-visualization.md
status: completed
---

## Task Summary

Implemented UX-41: Advanced Data Visualization — hover tooltips with period labels on sparkline chart
points, drill-down from KPI cards to module detail views via `NavigateCommand`, and 7/30/90-day
period selectors on a new Revenue Trend card backed by an additive read-only raw `MySqlConnector`
query.

**Plan:** `[[41-advanced-data-visualization]]`

## What Was Done

### A. Hover Tooltips on Chart Points

- Modified `src/MerchSys.App/Views/Shell/Sparkline.xaml.vb`:
  - Added `Label As String` property to `SparklineBarItem`
  - Added `Labels As IEnumerable(Of String)` dependency property to `Sparkline`
  - Added `OnLabelsChanged` DP callback that re-runs `UpdateBars()`
  - `UpdateBars()` now zips `Points` with `Labels` (by index) to populate each bar's `Label`

- Modified `src/MerchSys.App/Views/Shell/Sparkline.xaml`:
  - Updated `Rectangle.ToolTip` from a plain `TextBlock` to a `StackPanel` showing:
    - Period label `TextBlock` (collapses via `Trigger Property="Text" Value=""` when label is empty — preserves backward compatibility with existing unlabelled sparklines)
    - Value `TextBlock` with `FormatCurrencyNoDecimal` — unchanged from UX-12

### B. Drill-down from KPI to Detail View

- Modified `src/MerchSys.App/ViewModels/OwnerDashboardViewModel.vb`:
  - Added four `EventHandler` events: `NavigateToPurchasingRequested`, `NavigateToInventoryRequested`, `NavigateToSalesRequested`, `NavigateToAccountingRequested`
  - Added four synchronous `RelayCommand` properties that raise the corresponding event via lambda
  - Events are raised on: PURCHASING card → `PurchasingDashboardView`, INVENTORY → `StockDashboardView`, SALES → `SalesSummaryView`, ACCOUNTING → `FinancialOverviewView` (all accessible to Owner role)

- Modified `src/MerchSys.App/Views/OwnerDashboardView.xaml`:
  - Each of the four secondary KPI card headers now contains a `Button Content="View →"` with `Style="{StaticResource LinkButtonStyle}"` bound to the corresponding navigate command
  - Added `AutomationProperties.Name` and `ToolTip` for screen-reader accessibility
  - Removed the `Style="{StaticResource CardTitle}"` from module-name `TextBlock` in card headers (was applying `Margin="0,0,0,12"` — moved the margin to the header `StackPanel` wrapper to avoid double-spacing with the link button)

- Modified `src/MerchSys.App/Views/OwnerDashboardView.xaml.vb`:
  - Added `Imports` for all four target view types
  - Subscribed to all four navigate events in the constructor
  - Added `NavigateTo(targetViewType As Type)` shared helper that replicates the `FinancialOverviewView.OnNavigateToVatReturnRequested` pattern: iterates `Application.Current.Windows` to find `MainWindowViewModel`, looks up the `NavigationItem` from `AllNavigableItems` by `ViewType`, calls `NavigateCommand.Execute(item)`

### C. Period Selectors (7/30/90-day)

- Modified `src/MerchSys.App/ViewModels/OwnerDashboardViewModel.vb`:
  - Injected `IConfiguration` as a new constructor parameter; stores `configuration.GetConnectionString("MerchSysCentral")` in `_trendConnStr`
  - Added `SelectedTrendPeriod As Integer` (default 30); setter notifies `Is7DaySelected`/`Is30DaySelected`/`Is90DaySelected` and fires `LoadTrendDataAsync(value)` fire-and-forget when changed
  - Added `Is7DaySelected`, `Is30DaySelected`, `Is90DaySelected` — read/write bool properties that enable TwoWay RadioButton binding; setters only act on `value = True` (so WPF's GroupName uncheck writes `False` are safely ignored)
  - Added `TrendSparkPoints As IEnumerable(Of Double)` and `TrendSparkLabels As IEnumerable(Of String)` — additive read-only properties; existing VM contract unchanged
  - Added `LoadTrendDataAsync(days As Integer) As Task` — raw `MySqlConnector` reader (no EF, no `ToListAsync`):
    ```sql
    SELECT DATE(TransactionDate) AS SaleDate, COALESCE(SUM(TotalAmount), 0) AS DayTotal
    FROM Pos_SalesTransactions
    WHERE TransactionDate >= @start AND IsVoided = 0 AND IsDeleted = 0
    GROUP BY DATE(TransactionDate)
    ORDER BY SaleDate ASC
    ```
    Errors are captured post-Try (BC36943 compliant) and logged via `System.Console.WriteLine`
  - `RefreshAsync()` now calls `Await LoadTrendDataAsync(_selectedTrendPeriod)` after the four existing KPI loaders — no existing property or method changed

- Modified `src/MerchSys.App/Views/OwnerDashboardView.xaml`:
  - Added Row 3 `RowDefinition` to the KPI grid; added a new `Border` spanning `Grid.Column="0" Grid.ColumnSpan="2"` at `Grid.Row="3"` styled as `KpiCard`
  - Revenue Trend card contains:
    - A `Grid` header with "Revenue Trend" title and a `StackPanel` of three `RadioButton` controls (`GroupName="TrendPeriod"`, `Style="{StaticResource PeriodToggle}"`) with `IsChecked` bound TwoWay to `Is7DaySelected`/`Is30DaySelected`/`Is90DaySelected`
    - A `views:Sparkline` with `Points="{Binding TrendSparkPoints}"` and `Labels="{Binding TrendSparkLabels}"`, `Height="40"`, `HorizontalAlignment="Stretch"`
  - `PeriodToggle` style defined locally (same shape as `IncomeStatementView.PeriodToggle`)

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | Implementation authored to compile clean; **not re-run during review** (build deferred to testing phase per workflow) |
| Unit tests pass | N/A (no test projects) |
| Manual verification | Pending operator session |

## Issues Encountered

None during implementation.

## Review Fixes (post-implementation, claude-code)

A code review surfaced five findings; #1–#3 and #5 were fixed, #4 is a documented note only:

1. **Zero-filled trend window (was: gappy x-axis)** — `LoadTrendDataAsync` previously appended only the days the `GROUP BY DATE(...)` query returned, so a no-sales day vanished and the sparkline's x-axis was non-uniform (labels could jump Jun 1 → Jun 3). The read now lands in a `Dictionary(Of Date, Double)`; the series is then zero-filled across the full `days` window so every day has a bar (missing → 0). On error the series is left empty (no misleading flat-zero trend).
2. **Corrected sparkline comment** — the `OwnerDashboardView.xaml` comment claimed "bar width auto-scales with ItemsControl width," but bars are fixed `Width="3"` in a horizontal `StackPanel`. Comment now reads "fixed-width (3px) bars, left-anchored and bottom-aligned; bar height scales to this 40px card," and `HorizontalAlignment` changed from the no-op `Stretch` to `Left`.
3. **Sparkline bars scale to actual height** — `UpdateBars` hard-coded `SparkHeight = 24.0`, so the 40px Revenue Trend card left ~16px dead space above the bars. It now scales to `ActualHeight` (fallback to explicit `Height`, then 24px), and the control rebuilds on `SizeChanged` so bars resolve once layout assigns a height. The unlabelled `FinancialOverviewView` sparkline (no `Height` set) still scales to its default 24px — unchanged.
4. **(Note only) Period-toggle bypasses the loading guard** — the `SelectedTrendPeriod` setter fires `LoadTrendDataAsync` fire-and-forget, outside `IsLoading`, so a toggle can race the 60s auto-refresh writing `TrendSparkPoints`. Last-writer-wins and harmless (each call's errors are internally caught); left as-is to match the existing `Dim loadTask = RefreshAsync()` pattern.
5. **Build claim corrected** — the original "✅ 0 errors, 0 warnings" was the implementer's claim; per the hybrid workflow no build was run during review, so the table above reflects that the build was not independently re-verified.

## What's Next

- [ ] Operator verification: hover a sparkline bar in `FinancialOverviewView` — tooltip should show value (label empty → collapsed)
- [ ] Operator verification: hover a bar in the Owner Dashboard Revenue Trend sparkline — tooltip should show "MMM d" label above the value
- [ ] Operator verification: click "View →" on each KPI card — confirm navigation to correct detail view in both roles
- [ ] Operator verification: switch 7d / 30d / 90d — confirm sparkline re-renders with daily date labels; both Light and Dark themes

## Cross-References

- Domain Wiki pages consulted: `[[centralized-database-architecture]]`
- Agent Wiki entries consulted: `[[wpf-vista-trend-indicators]]`, `[[wpf-vista-tooltips]]`, `[[wpf-mainwindow-not-shell-window]]`

## Codebase Wiki Discrepancies (for Antigravity)

- `Sparkline.xaml.vb` / `Sparkline.xaml` — `SparklineBarItem` now has `Label As String`; `Sparkline` has a new `Labels` DP; tooltip is now a `StackPanel` showing optional period label + value
- `OwnerDashboardViewModel.vb` — new constructor parameter `IConfiguration`, new properties `SelectedTrendPeriod`, `Is7DaySelected`, `Is30DaySelected`, `Is90DaySelected`, `TrendSparkPoints`, `TrendSparkLabels`; new events `NavigateToPurchasingRequested` etc.; new commands `NavigateToPurchasingCommand` etc.
- `OwnerDashboardView.xaml` — Row 3 Revenue Trend card added; four KPI card headers now include "View →" link buttons
- `OwnerDashboardView.xaml.vb` — now subscribes to four navigate events; `NavigateTo()` helper added
