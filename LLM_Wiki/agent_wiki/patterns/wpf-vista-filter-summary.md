---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-05
tags: [wpf, xaml, vb-net, mvvm, search, filtering, chips, EmptyStatePanel]
---

# Search & Filter UX Maturity (Count, Chips, Clear-All, Empty States, Session Memory)

## Context

When navigating lists and databases (e.g., Stock, Product list, Purchase Orders), users need live result counters, removable facets (chips), quick reset affordances, and memory of their current filters when returning to the view. Additionally, a filtered list containing zero results must display a differentiated empty state ("No results matching filters · Clear filters") as opposed to a genuine database-empty state ("No data available · Add record").

## The Pattern

The pattern is composed of three parts:
1. **`FilterSummaryBar` Control**: Displays "{shown} of {total} results", a list of filter chips with hover-red "×" actions, and a "Clear all" button that collapses when no filters are active.
2. **`FilterChipItem` Model**: Holds display text, filter keys, and callbacks to clear specific filters.
3. **In-VM Session memory**: Utilizes `Shared` (static) backing fields inside Transient ViewModels, isolating state per logged-in user.
4. **Differentiated Empty State**: Drives `EmptyStatePanel` properties dynamically via `IsFilterActive`.

### 1. Chip and ViewModel Implementation

Each active filter property has a corresponding chip inside the `ActiveFilterChips` collection. The collection is regenerated in `RefreshFilterChips()` at the end of `ApplyFilters()`:

```vb
Imports System.Collections.ObjectModel
Imports System.Windows.Input
Imports MerchSys.SharedKernel.Interfaces

Public Class StockDashboardViewModel
    Inherits ViewModelBase

    ' Session memory static variables
    Private Shared _savedCategory As String = "All"
    Private Shared _savedStatus As String = "All"
    Private Shared _savedSearchText As String = String.Empty
    Private Shared _lastUser As String = Nothing

    Public Property SelectedCategory As String
        Get
            Return _selectedCategory
        End Get
        Set(value As String)
            If SetProperty(_selectedCategory, value) Then
                _savedCategory = value ' Save to session memory
                ApplyFilters()
            End If
        End Set
    End Property

    Public Property ActiveFilterChips As ObservableCollection(Of FilterChipItem)
    Public Property ClearFiltersCommand As RelayCommand

    Public ReadOnly Property IsFilterActive As Boolean
        Get
            Return (SelectedCategory <> "All") OrElse (SelectedStatus <> "All") OrElse Not String.IsNullOrWhiteSpace(SearchText)
        End Get
    End Property

    ' Dynamic Empty State properties
    Public ReadOnly Property EmptyStateTitle As String
        Get
            If IsFilterActive Then
                If Not String.IsNullOrWhiteSpace(SearchText) Then
                    Return $"No results for '{SearchText.Trim()}'"
                Else
                    Return "No results matching filters"
                End If
            End If
            Return "No Stock Products"
        End Get
    End Property

    Public ReadOnly Property EmptyStateDescription As String
        Get
            If IsFilterActive Then
                Return "Try adjusting your filters or search term to find what you're looking for."
            End If
            Return "There are no products in the stock inventory."
        End Get
    End Property

    Public ReadOnly Property EmptyStateActionCommand As ICommand
        Get
            If IsFilterActive Then
                Return ClearFiltersCommand
            End If
            Return Nothing
        End Get
    End Property

    Public ReadOnly Property EmptyStateActionText As String
        Get
            If IsFilterActive Then
                Return "Clear filters"
            End If
            Return Nothing
        End Get
    End Property

    Public Sub New(session As ISessionService)
        ' Prevent leak across different logged-in users
        Dim currentUser = session.CurrentUsername
        If currentUser <> _lastUser Then
            _savedCategory = "All"
            _savedStatus = "All"
            _savedSearchText = String.Empty
            _lastUser = currentUser
        End If

        _selectedCategory = _savedCategory
        _selectedStatus = _savedStatus
        _searchText = _savedSearchText

        ActiveFilterChips = New ObservableCollection(Of FilterChipItem)()
        ClearFiltersCommand = New RelayCommand(AddressOf ClearFilters)
        
        Dim initTask = LoadDataAsync()
    End Sub

    Private Sub ApplyFilters()
        ' ... filter _allProducts into Products ...
        
        RefreshFilterChips()
        OnPropertyChanged(NameOf(IsEmpty))
    End Sub

    Private Sub RefreshFilterChips()
        ActiveFilterChips.Clear()

        If SelectedCategory <> "All" Then
            ActiveFilterChips.Add(New FilterChipItem($"Category: {SelectedCategory}", "Category", New RelayCommand(Sub() SelectedCategory = "All")))
        End If
        If Not String.IsNullOrWhiteSpace(SearchText) Then
            ActiveFilterChips.Add(New FilterChipItem($"Search: {SearchText.Trim()}", "Search", New RelayCommand(Sub() SearchText = String.Empty)))
        End If
    End Sub

    Private Sub ClearFilters()
        _selectedCategory = "All"
        _selectedStatus = "All"
        _searchText = String.Empty

        OnPropertyChanged(NameOf(SelectedCategory))
        OnPropertyChanged(NameOf(SelectedStatus))
        OnPropertyChanged(NameOf(SearchText))

        _savedCategory = "All"
        _savedStatus = "All"
        _savedSearchText = String.Empty

        ApplyFilters()
    End Sub
End Class
```

### 2. View Integration (XAML)

```xaml
<!-- The responsive Filter Summary Bar -->
<views:FilterSummaryBar DockPanel.Dock="Top"
                        Margin="4,0,4,8"
                        ShownCount="{Binding Products.Count}"
                        TotalCount="{Binding TotalProducts}"
                        ActiveFilters="{Binding ActiveFilterChips}"
                        ClearAllCommand="{Binding ClearFiltersCommand}"/>

<!-- Dynamic EmptyStatePanel -->
<views:EmptyStatePanel Title="{Binding EmptyStateTitle}"
                       Description="{Binding EmptyStateDescription}"
                       ActionText="{Binding EmptyStateActionText}"
                       ActionCommand="{Binding EmptyStateActionCommand}"
                       HorizontalAlignment="Center" VerticalAlignment="Center"
                       Visibility="{Binding IsEmpty, Converter={StaticResource BoolToVis}}"/>
```

## Why It Works

- **Zero Redundant Loads**: Since ViewModel initialization binds direct fields (`_selectedCategory = _savedCategory`) instead of properties, constructor filter binding restores previous settings without firing setter triggers, meaning the database is loaded exactly once.
- **Setter Backup**: Bidirectionally bound properties (like search text or combo box selections) must save their new values to their respective `Shared` backing variables (`_savedCategory = value`) in their property setters. This ensures that the state persists when the transient ViewModel is destroyed and later reconstructed.
- **WPF Native Property Binding**: Shown count binds to `Products.Count`, utilizing WPF's automatic collection-change notifications to keep the count updated without manual hooks.
- **Dynamic Action Button Injection**: By returning `Nothing` in the ViewModel when no filter is active, the `NullToVisibilityConverter` automatically collapses the EmptyState's action button when a genuine empty state needs no CTA, and renders the "Clear filters" button when filters are active.

## VB.NET Compiler Pitfalls & Rules

> [!WARNING]
> **VB.NET Type Names (BC32016)**:
> In VB.NET, if you have a class property called `Count`, writing `List.Count(predicate)` might bind directly to the property instead of resolving the Extension Method `Enumerable.Count`. To prevent silent compilation errors (e.g., `BC32016` "is not a parameterless property"), always use explicit Extension syntax `Enumerable.Count(list, predicate)` or filter using `.Where(predicate).Count()`.
> Also, fully qualify `ICommand` or import `System.Windows.Input` since it is not included by default in standard ViewModel namespaces.

## Related

- Links to related entries: `[[wpf-vista-freshness-chip]]`, `[[wpf-vista-state-feedback]]`
- Links to Domain Wiki pages: `[[modular-monolith]]`
