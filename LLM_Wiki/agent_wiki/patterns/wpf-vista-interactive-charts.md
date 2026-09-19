---
type: pattern
module: MerchSys.App
agent: claude-code
date: 2026-06-06
tags: [wpf, xaml, vb-net, mvvm, charts, tooltips, drill-down, period-selector, sparkline, interactive]
---

# WPF VISTA Interactive Charts Pattern

## Context

UX-41 adds interactivity to the hand-built `Rectangle`/`Path` chart primitives from UX-12 (no charting NuGet).
Three interaction layers: hover tooltips that show the point's period label + value, click-to-drill navigation
from a KPI card to its module detail view, and a period-selector that re-windows the series via an additive
read-only raw `MySqlConnector` query.

## Pattern A — Period Labels on Sparkline Tooltips

### The Extension (Additive)

`SparklineBarItem` gains a `Label As String` property. `Sparkline` gains a `Labels As IEnumerable(Of String)`
dependency property. `UpdateBars()` zips by index:

```vb
Dim labelList = If(Labels IsNot Nothing, Labels.ToList(), New List(Of String)())
Dim idx As Integer = 0
For Each pt In Points
    Dim lbl = If(idx < labelList.Count, labelList(idx), String.Empty)
    InternalBars.Add(New SparklineBarItem With {.Height = barH, .Value = pt, .Label = lbl})
    idx += 1
Next
```

### XAML Tooltip with Conditional Label

```xaml
<Rectangle.ToolTip>
    <StackPanel>
        <TextBlock Text="{Binding Label}" FontSize="10"
                   Foreground="{DynamicResource TextSecondaryBrush}">
            <TextBlock.Style>
                <Style TargetType="TextBlock">
                    <Setter Property="Visibility" Value="Visible"/>
                    <Style.Triggers>
                        <Trigger Property="Text" Value="">
                            <Setter Property="Visibility" Value="Collapsed"/>
                        </Trigger>
                    </Style.Triggers>
                </Style>
            </TextBlock.Style>
        </TextBlock>
        <TextBlock Text="{Binding Value, StringFormat={StaticResource FormatCurrencyNoDecimal}}"/>
    </StackPanel>
</Rectangle.ToolTip>
```

**Key rules:**
- `Trigger Property="Text" Value=""` collapses the label row when no label is provided — preserves backward
  compatibility with unlabelled sparklines (e.g. `FinancialOverviewView` KPI sparkline).
- Both `DynamicResource` references (`TextSecondaryBrush`) and `StaticResource` format strings resolve
  correctly inside a `ToolTip` popup because they chain to `Application.Resources`.
- The implicit `ToolTip` style (UX-22, `Components.xaml`) wraps the `StackPanel` automatically — no
  explicit `<ToolTip>` wrapper element needed.
- **Bars scale to `ActualHeight`, not a constant.** `UpdateBars` reads the control's `ActualHeight`
  (fallback to explicit `Height`, then 24px) so the same primitive fills a tall card (e.g. the 40px Owner
  trend) and a compact KPI row alike. Hook `SizeChanged` → `UpdateBars` because a `Points`/`Labels` change
  can fire before layout, when `ActualHeight` is still 0. Bars are fixed `Width="3"` (not width-scaled), so
  a wide series left-anchors rather than stretching.

## Pattern B — Drill-down from KPI Card to Detail View

### ViewModel Side (Raise Event)

```vb
Public Event NavigateToPurchasingRequested As EventHandler
Public ReadOnly Property NavigateToPurchasingCommand As RelayCommand

' In constructor:
NavigateToPurchasingCommand = New RelayCommand(Sub() RaiseEvent NavigateToPurchasingRequested(Me, EventArgs.Empty))
```

### View Code-Behind (Find MainWindowViewModel → NavigateCommand)

```vb
' In constructor:
AddHandler viewModel.NavigateToPurchasingRequested, AddressOf OnNavigateToPurchasingRequested

Private Sub OnNavigateToPurchasingRequested(sender As Object, e As EventArgs)
    NavigateTo(GetType(PurchasingDashboardView))
End Sub

Private Shared Sub NavigateTo(targetViewType As Type)
    Dim mainVm As MainWindowViewModel = Nothing
    For Each win As Window In Application.Current.Windows
        Dim vm = TryCast(win.DataContext, MainWindowViewModel)
        If vm IsNot Nothing Then
            mainVm = vm
            Exit For
        End If
    Next
    If mainVm Is Nothing Then Return
    Dim pair = mainVm.AllNavigableItems.FirstOrDefault(Function(n) n.Item.ViewType = targetViewType)
    Dim navItem = pair.Item
    If navItem IsNot Nothing AndAlso mainVm.NavigateCommand.CanExecute(navItem) Then
        mainVm.NavigateCommand.Execute(navItem)
    End If
End Sub
```

**Rules:**
- Never use `Application.Current.MainWindow` — it is the `LoginView`, not the shell (see `[[wpf-mainwindow-not-shell-window]]`).
- Iterate `Application.Current.Windows` and cast `DataContext` to `MainWindowViewModel` to find the shell.
- `NavigateCommand` is a `RelayCommand(Of NavigationItem)` — use `.CanExecute()` before `.Execute()`.
- Navigation is silently suppressed when the target view is not in `AllNavigableItems` (role-gated) — no error, no crash.
- Precedent and canonical reference: `FinancialOverviewView.OnNavigateToVatReturnRequested`.

### XAML Link Button in Card Header

```xaml
<StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,12">
    <Path Style="{StaticResource IconBase}" Data="{StaticResource IconBoxGeometry}"
          Width="18" Height="18" Margin="0,0,8,0" VerticalAlignment="Center"
          Fill="{DynamicResource TextSecondaryBrush}"/>
    <TextBlock FontSize="13" FontWeight="Bold" Foreground="{DynamicResource TextPrimaryBrush}"
               Text="PURCHASING" VerticalAlignment="Center"/>
    <Button Content="View →" Command="{Binding NavigateToPurchasingCommand}"
            Style="{StaticResource LinkButtonStyle}" FontSize="11"
            Margin="8,0,0,0" VerticalAlignment="Center"
            ToolTip="Open Purchasing Dashboard"
            AutomationProperties.Name="View Purchasing Dashboard"/>
</StackPanel>
```

## Pattern C — Period Selector with Additive Read-Only Query

### ViewModel Properties (Additive)

```vb
' No existing property is changed. Three new read/write bool shims enable RadioButton TwoWay binding:
Private _selectedTrendPeriod As Integer = 30
Public Property SelectedTrendPeriod As Integer
    Get
        Return _selectedTrendPeriod
    End Get
    Set(value As Integer)
        If SetProperty(_selectedTrendPeriod, value) Then
            OnPropertyChanged(NameOf(Is7DaySelected))
            OnPropertyChanged(NameOf(Is30DaySelected))
            OnPropertyChanged(NameOf(Is90DaySelected))
            Dim trendTask = LoadTrendDataAsync(value)   ' fire-and-forget
        End If
    End Set
End Property

Public Property Is7DaySelected As Boolean
    Get
        Return _selectedTrendPeriod = 7
    End Get
    Set(value As Boolean)
        If value Then SelectedTrendPeriod = 7   ' False writes from GroupName uncheck are ignored
    End Set
End Property
' ... Is30DaySelected, Is90DaySelected analogous
```

### Raw MySqlConnector Query (No EF, No ToListAsync)

```vb
Private ReadOnly _trendConnStr As String  ' = configuration.GetConnectionString("MerchSysCentral")

Private Async Function LoadTrendDataAsync(days As Integer) As Task
    If String.IsNullOrEmpty(_trendConnStr) Then
        TrendSparkPoints = New List(Of Double)()
        TrendSparkLabels = New List(Of String)()
        Return
    End If

    Dim startDate = DateTime.Today.AddDays(-(days - 1)).Date
    Dim totalsByDay As New Dictionary(Of Date, Double)()
    Dim errMsg As String = Nothing

    Try
        Using conn As New MySqlConnection(_trendConnStr)
            Await conn.OpenAsync()          ' Await inside Try is legal (BC36943 only blocks Catch/Finally)
            Using cmd = conn.CreateCommand()
                cmd.CommandText =
                    "SELECT DATE(TransactionDate) AS SaleDate, " &
                    "COALESCE(SUM(TotalAmount), 0) AS DayTotal " &
                    "FROM Pos_SalesTransactions " &
                    "WHERE TransactionDate >= @start AND IsVoided = 0 AND IsDeleted = 0 " &
                    "GROUP BY DATE(TransactionDate) ORDER BY SaleDate ASC"
                cmd.Parameters.Add(New MySqlParameter("@start", startDate.ToString("yyyy-MM-dd")))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        totalsByDay(reader.GetDateTime(0).Date) = CDbl(reader.GetDecimal(1))
                    End While
                End Using
            End Using
        End Using
    Catch ex As Exception
        errMsg = ex.Message   ' captured outside Catch to avoid BC36943
    End Try

    ' Zero-fill the full window so a no-sales day reads as a zero bar and the x-axis stays uniform.
    ' GROUP BY DATE(...) only returns days that HAD sales, so appending rows directly produces a gappy
    ' axis (labels jump Jun 1 → Jun 3). Leave the series empty on error (no misleading flat-zero trend).
    Dim pts As New List(Of Double)()
    Dim lbls As New List(Of String)()
    If errMsg Is Nothing Then
        For dayOffset = 0 To days - 1
            Dim d = startDate.AddDays(dayOffset)
            Dim dayTotal As Double
            totalsByDay.TryGetValue(d, dayTotal)   ' missing → 0.0
            pts.Add(dayTotal)
            lbls.Add(d.ToString("MMM d"))
        Next
    End If

    TrendSparkPoints = pts
    TrendSparkLabels = lbls
    If errMsg IsNot Nothing Then
        System.Console.WriteLine($"[OwnerDashboard] Revenue trend load error: {errMsg}")
    End If
End Function
```

### XAML Period Selector (RadioButton Group)

```xaml
<Style x:Key="PeriodToggle" TargetType="RadioButton">
    <Setter Property="FontSize" Value="11"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
    <Setter Property="Margin" Value="0,0,6,0"/>
    <Setter Property="Cursor" Value="Hand"/>
</Style>

<!-- In card header: -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right" VerticalAlignment="Center">
    <TextBlock Text="Period:" FontSize="11" Foreground="{DynamicResource TextSecondaryBrush}"
               VerticalAlignment="Center" Margin="0,0,6,0"/>
    <RadioButton Content="7d"  IsChecked="{Binding Is7DaySelected,  Mode=TwoWay}"
                 GroupName="TrendPeriod" Style="{StaticResource PeriodToggle}"/>
    <RadioButton Content="30d" IsChecked="{Binding Is30DaySelected, Mode=TwoWay}"
                 GroupName="TrendPeriod" Style="{StaticResource PeriodToggle}"/>
    <RadioButton Content="90d" IsChecked="{Binding Is90DaySelected, Mode=TwoWay}"
                 GroupName="TrendPeriod" Style="{StaticResource PeriodToggle}" Margin="0"/>
</StackPanel>
```

**Key rules:**
- Inject `IConfiguration` (not a `DbContext`) when only the connection string is needed — avoids adding a
  module-boundary DbContext dependency to `MerchSys.App` ViewModels.
- Always use `COALESCE(SUM(...), 0)` to avoid `NULL` reads on days with no transactions.
- Use `DATE(TransactionDate)` in `GROUP BY` for calendar-day bucketing (MariaDB syntax).
- **Zero-fill the window in the VM** — `GROUP BY DATE(...)` omits days with no sales, so read into a
  `Dictionary(Of Date, Double)` then iterate `0 .. days-1` filling misses with `0`. Appending query rows
  directly yields a non-uniform x-axis (labels skip empty days). Leave the series empty on error.
- Use `reader.GetDecimal()` / `reader.GetDateTime()` — never `reader[i]` untyped (avoids boxing surprises).
- Errors are captured in a `String` variable *before* the `Try` exits, then acted on after the `Try` block
  (BC36943 — `Await` forbidden in `Catch`/`Finally`).
- The `IConfiguration` parameter is auto-resolved by the Generic Host DI because `IConfiguration` is
  registered as a singleton by `Host.CreateDefaultBuilder`.

## Related

- Patterns: `[[wpf-vista-trend-indicators]]`, `[[wpf-vista-tooltips]]`, `[[wpf-mainwindow-not-shell-window]]`
- Errors: `[[efcore-vbnet-tolistasync-entity-empty]]` (why raw reader is used for period queries)
