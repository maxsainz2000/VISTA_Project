---
type: pattern
module: MerchSys.Purchasing
agent: antigravity
date: 2026-06-03
tags: [wpf, xaml, vb-net, mvvm, purchasing, dashboard]
---

# Pattern: Aggregate-Read Module Dashboard (WPF/VB.NET)

This pattern documents the structure, database query mechanisms, and cross-assembly navigation routing for the VISTA modular-monolith dashboards, using the **Purchasing Dashboard (UX-13)** as a reference implementation.

## Context

When introducing a landing overview dashboard for a module (e.g., Purchasing) that:
1. Resides in a modular monolith structure where the View resides in the main startup project (`MerchSys.App`) and the ViewModel/Services reside in the module class library (`MerchSys.Purchasing`).
2. Must aggregate data from multiple entities/services within the same module, as well as fetch some cross-module metrics.
3. Must provide navigation links from the dashboard cards/grids to operational screens without introducing circular assembly references.
4. Needs to perform heavy read-only aggregation (e.g. monthly spend trends or top-performing entities) without invoking write paths or causing EF Core 10 `ToListAsync` empty-list bugs in VB.NET.

## The Pattern

### 1. Separation of Concerns & Namespace Rules
The View is defined in `MerchSys.App.Views.Purchasing.PurchasingDashboardView` (referencing `Views.Purchasing` namespace).
The ViewModel is defined in `MerchSys.Purchasing.ViewModels.PurchasingDashboardViewModel` (referencing `ViewModels` namespace relative to RootNamespace).

### 2. Cross-Assembly Navigation Mapping
To allow the VM in the module library to trigger navigation in the shell without depending on the View assemblies, use a string-based event mapping:

**ViewModel (Module Library):**
```vb
Public Event NavigateToViewRequested As EventHandler(Of String)

Public Property NavigateToViewCommand As RelayCommand(Of String)

Private Sub NavigateToView(viewName As String)
    If Not String.IsNullOrEmpty(viewName) Then
        RaiseEvent NavigateToViewRequested(Me, viewName)
    End If
End Sub
```

**View Code-Behind (Shell Application):**
```vb
Private Sub OnNavigateToViewRequested(sender As Object, viewName As String)
    Dim targetType As Type = Nothing
    Select Case viewName
        Case "APLedgerView" : targetType = GetType(APLedgerView)
        Case "PurchaseOrderListView" : targetType = GetType(PurchaseOrderListView)
        Case "ReorderSuggestionsView" : targetType = GetType(ReorderSuggestionsView)
        Case "VendorDirectoryView" : targetType = GetType(VendorDirectoryView)
    End Select

    If targetType Is Nothing Then Return

    Dim mainVm As MainWindowViewModel = Nothing
    For Each win As Window In Application.Current.Windows
        Dim vm = TryCast(win.DataContext, MainWindowViewModel)
        If vm IsNot Nothing Then
            mainVm = vm
            Exit For
        End If
    Next
    If mainVm Is Nothing Then Return

    Dim item = mainVm.NavigationGroups _
        .SelectMany(Function(g) g.Items) _
        .FirstOrDefault(Function(i) i.ViewType = targetType)
    If item IsNot Nothing AndAlso mainVm.NavigateCommand.CanExecute(item) Then
        mainVm.NavigateCommand.Execute(item)
    End If
End Sub
```

### 3. Safe ADO.NET SQL Aggregation (EF Core 10 VB.NET Bug Workaround)
To compute 6-month trends and top lists safely, use raw `MySqlConnection` reader loops. This guarantees correct data collection in VB.NET targeting EF Core 10.

```vb
Private Async Function LoadMonthlyTrendAsync(connStr As String) As Task(Of List(Of TrendBarItem))
    Dim trendList As New List(Of TrendBarItem)()
    Using conn As New MySqlConnection(connStr)
        Await conn.OpenAsync()
        Using cmd = conn.CreateCommand()
            cmd.CommandText = "SELECT YEAR(OrderDate), MONTH(OrderDate), SUM(TotalAmount) " &
                              "FROM Pur_PurchaseOrders WHERE IsDeleted = 0 GROUP BY YEAR(OrderDate), MONTH(OrderDate) " &
                              "ORDER BY 1 ASC, 2 ASC"
            Using reader = Await cmd.ExecuteReaderAsync()
                While Await reader.ReadAsync()
                    ' Read and map values synchronously...
                End While
            End Using
        End Using
    End Using
    Return trendList
End Function
```

## Why It Works

- **No circular dependency:** The module VM does not know about View types; it communicates using abstract strings. The shell View maps these strings to concrete View classes, preserving strict monolith layer boundaries.
- **EF Core Bug Avoidance:** Standard EF Core `ToListAsync()` on full entities fails silently in VB.NET under .NET 10. Projections or ADO.NET reader loops solve this completely.
- **Secure Role Gating:** Because navigation uses `MainWindowViewModel`'s `NavigationGroups`, attempting to navigate to an unauthorized screen (e.g. Owner trying to access Reorder Suggestions) resolves to a `Nothing` navigation item, silently preserving UI permission gates.

## Rules

- **Prepend dashboards:** Module dashboards should always be registered as the first/default navigation item in `BuildRoleAware*Items()` in `MainWindowViewModel.vb`.
- **Read-Only execution:** Dashboard ViewModels must not expose any write commands or mutate data. Clicking cards/rows should delegate to operational views.
- **ADO.NET for aggregations:** Multi-table or historical trend queries must use the raw reader loop pattern to avoid EF Core VB.NET traps.

## Related

- Domain Wiki: `[[modular-monolith]]`, `[[client-server-wpf]]`
- Agent Wiki errors: `[[efcore10-vbnet-migration-discovery-bug]]`
