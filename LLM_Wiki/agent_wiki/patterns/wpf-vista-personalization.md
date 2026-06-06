---
name: wpf-vista-personalization
description: Pattern for the VISTA prefs service — stable-key last-view restore, favorites/recents persistence through UiSettingsStore, sidebar pin toggle, and command-palette zero-state accelerators
metadata:
  type: pattern
  module: MerchSys.App
  tags: [wpf, vb-net, persistence, personalization, favorites, recents, navigation, command-palette, ui-settings, json]
  agent: claude-code
  date: 2026-06-06
---

## Context

UX-38 adds per-laptop workspace memory to VISTA: restore the last-visited screen on relaunch, let users pin favorites, and surface a recently-viewed list. All state piggybacks on the existing `UiSettingsStore` / `ui-settings.json` store introduced by `[[wpf-vista-ui-settings-persistence]]` — no new file, no new NuGet, no server-side profile.

## The Pattern

### 1. Stable Key: `ViewType.FullName`

The persistence key for a nav screen is `NavigationItem.ViewType.FullName`, e.g.:

```
MerchSys.App.Views.Inventory.StockDashboardView
```

This is stable across restarts (assembly stays the same) and is unique per screen. Stale keys (from removed or renamed views) are detected at restore time by a `FirstOrDefault` lookup against the current role-gated `AllNavigableItems` — if the key doesn't resolve, the fallback fires silently.

### 2. Single-Store Extension (`UiSettingsStore`)

Three new properties alongside `Theme` and the window-placement fields:

```vb
Public Property LastViewKey As String = String.Empty
Public Property FavoriteKeys As List(Of String) = New List(Of String)()
Public Property RecentKeys As List(Of String) = New List(Of String)()
```

**Reading JSON arrays in `LoadFromDisk`:**

```vb
If root.TryGetProperty("favoriteKeys", prop) AndAlso prop.ValueKind = JsonValueKind.Array Then
    FavoriteKeys = New List(Of String)()
    For Each el In prop.EnumerateArray()
        If el.ValueKind = JsonValueKind.String Then
            Dim sk = el.GetString()
            If Not String.IsNullOrEmpty(sk) Then FavoriteKeys.Add(sk)
        End If
    Next
End If
```

**Writing JSON arrays in `Save()`** (hand-rolled, no NuGet):

```vb
Private Shared Function SerializeStringArray(items As List(Of String)) As String
    If items.Count = 0 Then Return "[]"
    Dim parts = items.Select(Function(s) $"""{EscapeJsonString(s)}""")
    Return "[" & String.Join(",", parts) & "]"
End Function
```

Extend the existing interpolated-string Save line to include the three new keys:

```vb
Dim json = $"{{...,""lastViewKey"":""{lastViewEsc}"",""favoriteKeys"":{favArr},""recentKeys"":{recArr}}}"
```

### 3. `IUserPreferencesService` + `UserPreferencesService`

A thin singleton service wraps the store so callers never access the store directly:

```vb
Public Interface IUserPreferencesService
    Function GetLastViewKey() As String
    Sub RecordNavigation(item As NavigationItem)   ' saves lastViewKey + pushes recents + Save()
    Function GetFavorites(allItems As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))) As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))
    Function GetRecents(allItems As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))) As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))
    Sub ToggleFavorite(item As NavigationItem)
    Function IsFavorite(item As NavigationItem) As Boolean
    Sub SyncFavoriteFlagsToItems(items As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem)))
End Interface
```

Register as `AddSingleton(Of IUserPreferencesService, UserPreferencesService)()` after `UiSettingsStore`.

**Recents invariant:** bounded at `MaxRecents = 10`, de-duplicated, most-recent-first. `RecordNavigation` does:
```vb
_store.RecentKeys.Remove(viewKey)   ' de-dup
_store.RecentKeys.Insert(0, viewKey)
If _store.RecentKeys.Count > MaxRecents Then _store.RecentKeys.RemoveAt(_store.RecentKeys.Count - 1)
_store.Save()
```

### 4. Restore-Last-View in `MainWindowViewModel`

```vb
Public Sub NavigateToLastOrDefault()
    Dim lastKey = _prefs.GetLastViewKey()
    If Not String.IsNullOrEmpty(lastKey) Then
        Dim pair = _allNavigableItems.FirstOrDefault(
            Function(n) n.Item IsNot Nothing AndAlso
                        n.Item.ViewType IsNot Nothing AndAlso
                        n.Item.ViewType.FullName = lastKey)
        If pair.Item IsNot Nothing Then
            ActiveModule = pair.[Module]
            Navigate(pair.Item)
            Return
        End If
    End If
    NavigateToDefault()   ' fallback: stale key, absent key, or role-excluded screen
End Sub
```

Call `NavigateToLastOrDefault()` (not `NavigateToDefault()`) from `HandleLoginSucceeded` after `RefreshNavigation()`.

**Double-navigation fix:** Remove the `NavigateToDefault()` call from `MainWindow_Loaded`. Navigation should only be driven from `HandleLoginSucceeded`, which fires once per login (including first launch when `_mainWindow.Show()` triggers `Loaded`).

### 5. Recording Navigation

Every call to `Navigate(item)` calls:

```vb
_prefs.RecordNavigation(item)
```

This persists `lastViewKey` and pushes to `recentKeys` in a single `Save()`. **Do not** call `_store.Save()` directly from the VM.

### 6. Syncing IsFavorite Flags to Nav Items

After `RebuildAllNavigableItems()`, call:

```vb
_prefs.SyncFavoriteFlagsToItems(_allNavigableItems)
```

This stamps `IsFavorite = True/False` on each `NavigationItem` from the stored `FavoriteKeys`. Required because nav items are recreated fresh on each `RefreshNavigation()`.

`NavigationItem.IsFavorite` must be an observable property using `SetProperty` so the sidebar star button reacts to the sync.

### 7. Sidebar Pin Toggle (XAML)

Define `FavoriteToggleButtonStyle` in `ModuleDetailPanel.UserControl.Resources`:
- `Opacity="0.3"` at rest → `1` + `AccentBrush` on hover and when `IsFavorite = True`
- `Focusable="False"` to avoid keyboard focus on the icon

Wrap each ItemsControl row in a shared DataTemplate. Use a single-cell `Grid` with the
full-width nav button and the star button **overlaid** on the trailing edge — not a 2-column
split — so the active-row highlight spans the entire row instead of stopping short of the star:

```xaml
<DataTemplate x:Key="NavItemWithFavoriteTemplate">
    <Grid>
        <Button Content="{Binding DisplayName}"
                Style="{StaticResource NavItemStyle}"
                Command="{Binding DataContext.NavigateCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                CommandParameter="{Binding}"/>
        <Button HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,4,0"
                Style="{StaticResource FavoriteToggleButtonStyle}"
                Command="{Binding DataContext.ToggleFavoriteCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                CommandParameter="{Binding}"
                AutomationProperties.Name="{Binding DisplayName, StringFormat='Toggle favorite for {0}'}">
            <TextBlock>
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Text" Value="☆"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding IsFavorite}" Value="True">
                                <Setter Property="Text" Value="★"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
        </Button>
    </Grid>
</DataTemplate>
```

Reference it as `ItemTemplate="{StaticResource NavItemWithFavoriteTemplate}"` on each `ItemsControl`.

For child UserControl panels (e.g., `DeveloperToolsPanel`), use `{DynamicResource FavoriteToggleButtonStyle}` — it resolves via the visual-tree parent `ModuleDetailPanel`. Bind the pin's `AutomationProperties.Name` to the row's `DisplayName` (`StringFormat='Toggle favorite for {0}'`) rather than a static string, so each pin is distinguishable to assistive tech.

### 8. Command Palette Zero-State Accelerators

In `CommandPaletteViewModel.RunSearchAsync`, prepend Favorites/Recents groups when `term` is empty.
**De-duplicate against the full "Screens" list:** the "Screens" pass lists *every* navigable item on
an empty query, so without care a favorited/recent screen shows up two–three times. Track the keys
surfaced as accelerators in a `HashSet` and skip them in the Screens pass; also skip the current
screen (always `recents[0]` = `GetLastViewKey()`) from Recent so the palette doesn't echo the screen
the user is already on:

```vb
Dim acceleratorKeys As New HashSet(Of String)()
If String.IsNullOrWhiteSpace(term) Then
    Dim lastKey = _prefs.GetLastViewKey()
    For Each fp In _prefs.GetFavorites(mainVm.AllNavigableItems)
        matchedItems.Add(New CommandPaletteItem With {
            .DisplayName = fp.Item.DisplayName, .Subtitle = "Favorite · " & GetModuleName(fp.[Module]),
            .Section = "Favorites", .IconData = starIcon, .TargetItem = fp.Item})
        acceleratorKeys.Add(fp.Item.ViewType.FullName)
    Next
    For Each rp In _prefs.GetRecents(mainVm.AllNavigableItems)
        Dim recKey = rp.Item.ViewType.FullName
        If recKey = lastKey OrElse acceleratorKeys.Contains(recKey) Then Continue For
        matchedItems.Add(New CommandPaletteItem With {
            .DisplayName = rp.Item.DisplayName, .Subtitle = "Recent · " & GetModuleName(rp.[Module]),
            .Section = "Recent", .IconData = GetModuleIcon(rp.[Module]), .TargetItem = rp.Item})
        acceleratorKeys.Add(recKey)
    Next
End If

' Screens pass (empty query): list everything NOT already shown as an accelerator.
'   matches = Not acceleratorKeys.Contains(navigable.Item.ViewType.FullName)
```

The `CollectionViewSource` grouping in `CommandPalette.xaml` (grouped by `Section`) orders groups in insertion order, so Favorites → Recent → Screens appears naturally.

## Rules

- **Never read/write `ui-settings.json` directly** — all access goes through `UiSettingsStore` properties.
- **Stable key = `ViewType.FullName`**, not `DisplayName` (which can change for L10n).
- **Always check `pair.Item IsNot Nothing`** after `FirstOrDefault` on a value-tuple list — the default struct has `Item = Nothing`.
- **Recents are bounded at 10 and de-duplicated** before insertion.
- **Call `RecordNavigation` in `Navigate()`, not in individual callsites** — centralise the side-effect.
- **Sync `IsFavorite` flags in `RebuildAllNavigableItems()`**, not on demand — nav items are new objects after each `RefreshNavigation()`.
- **Remove the `NavigateToDefault` call from `MainWindow_Loaded`** — navigation must be driven by `HandleLoginSucceeded` to avoid double-navigation.
- **De-dup the palette zero-state** — the empty-query "Screens" group lists every screen, so skip keys already shown as Favorites/Recent (and skip the current screen from Recent) to avoid showing a screen two–three times.
- **Overlay the pin star, don't column-split** — a full-width nav button with the star overlaid `HorizontalAlignment=Right` keeps the active-row highlight edge-to-edge.

## Related

- `[[wpf-vista-ui-settings-persistence]]` — `UiSettingsStore` extension pattern, JSON read/write conventions
- `[[wpf-vista-command-palette]]` — `AllNavigableItems`, section grouping, `ExecuteSelected` routing
- `[[wpf-vista-iconography]]` — `IconStarGeometry` and other geometry resources in `Themes/Icons.xaml`
