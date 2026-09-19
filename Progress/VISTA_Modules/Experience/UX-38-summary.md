---
module: MerchSys.App
agent: claude-code
date: 2026-06-06
plan-ref: Plans/VISTA_Modules/Experience/38-personalization-workspace-memory.md
status: completed
---

## Task Summary

Implements UX-38: Personalization & Workspace Memory. A small `IUserPreferencesService` (backed by the existing `UiSettingsStore`) persists per-laptop user preferences—last-viewed screen, favorites list, and bounded recents list—without touching any business data or query paths.

**Plan:** `[[38-personalization-workspace-memory]]`

## What Was Done

### New Files
- `Services/IUserPreferencesService.vb` — interface with `GetLastViewKey`, `RecordNavigation`, `GetFavorites`, `GetRecents`, `ToggleFavorite`, `IsFavorite`, `SyncFavoriteFlagsToItems`
- `Services/UserPreferencesService.vb` — implementation; depends on `UiSettingsStore`; recents capped at 10, de-duplicated, most-recent-first

### Modified Files
- `Services/UiSettingsStore.vb` — added `LastViewKey As String`, `FavoriteKeys As List(Of String)`, `RecentKeys As List(Of String)` properties; extended `LoadFromDisk` to read JSON arrays using `JsonDocument.EnumerateArray()`; extended `Save()` to emit the 3 new fields; added private `EscapeJsonString` and `SerializeStringArray` helpers (no new NuGet — BCL `System.Text.Json`)
- `Models/NavigationItem.vb` — added `IsFavorite As Boolean` observable property (notifies via `SetProperty`)
- `ViewModels/MainWindowViewModel.vb` — added `IUserPreferencesService` constructor injection; added `ToggleFavoriteCommand (RelayCommand(Of NavigationItem))`; `Navigate()` now calls `_prefs.RecordNavigation(item)` on every navigation; added `NavigateToLastOrDefault()` (resolves saved key against `AllNavigableItems`, falls back to `NavigateToDefault()` on stale or absent key); `RebuildAllNavigableItems()` now calls `_prefs.SyncFavoriteFlagsToItems(...)` to stamp `IsFavorite` on fresh nav items
- `ViewModels/Shell/CommandPaletteViewModel.vb` — added `IUserPreferencesService` constructor injection; zero-state (empty query) now prepends a "Favorites" group (star icon) and a "Recent" group (module icon) before the "Screens" group; added `GetModuleIcon(AppModule) As Geometry` private helper; `IconStarGeometry` from `Themes/Icons.xaml` used for Favorites section
- `Application.xaml.vb` — registered `AddSingleton(Of IUserPreferencesService, UserPreferencesService)()`; `HandleLoginSucceeded` now calls `NavigateToLastOrDefault()` instead of `NavigateToDefault()`
- `MainWindow.xaml.vb` — removed the duplicate `_viewModel.NavigateToDefault()` call from `MainWindow_Loaded` (navigation is now solely driven by `HandleLoginSucceeded` after `RefreshNavigation()`, eliminating the pre-existing double-navigation race)
- `Views/Shell/ModuleDetailPanel.xaml` — added `FavoriteToggleButtonStyle` and `NavItemWithFavoriteTemplate` DataTemplate to `UserControl.Resources`; all four module ItemsControls (Purchasing, Inventory, POS, Accounting) now reference `ItemTemplate="{StaticResource NavItemWithFavoriteTemplate}"`; each nav row shows the screen button + a ☆/★ pin button
- `Views/Shell/Modules/DeveloperToolsPanel.xaml` — updated inline DataTemplate to match: nav button + pin button using `{DynamicResource FavoriteToggleButtonStyle}` (resolves via visual-tree parent ModuleDetailPanel)

## Prefs-Service Shape and Storage Strategy

`UserPreferencesService` wraps `UiSettingsStore`, which owns a single `ui-settings.json` at `%LOCALAPPDATA%\MerchSys\ui-settings.json`. The file now carries six keys: existing `theme`, `windowLeft/Top/Width/Height`, `windowMaximized`; new `lastViewKey`, `favoriteKeys` (JSON array), `recentKeys` (JSON array). All reads/writes go through `UiSettingsStore` so no service ever clobbers another's keys.

## Stable-Key Scheme + Fallback

The stable key is `NavigationItem.ViewType.FullName` (e.g., `"MerchSys.App.Views.Inventory.StockDashboardView"`). On relaunch:
1. `_prefs.GetLastViewKey()` reads the stored key.
2. `_allNavigableItems.FirstOrDefault(Function(n) n.Item.ViewType.FullName = lastKey)` resolves it against the role-gated current nav.
3. If found → navigate there, set `ActiveModule`. If not found (stale or role-excluded) → `NavigateToDefault()`.

## How Favorites/Recents Surface

**Command Palette zero-state (empty query):**
- "Favorites" group: pinned items with ★ (`IconStarGeometry`), subtitle "Favorite · {Module}"
- "Recent" group: recent items with their module icon, subtitle "Recent · {Module}"
- Both groups appear above the existing "Screens" group, using the same `CollectionViewSource` grouping already in `CommandPalette.xaml`
- Zero-state is de-duplicated: a screen shown under Favorites or Recent is omitted from the "Screens" group, and the current screen (always `recents[0]`) is omitted from Recent (see Review Fixes #1, #2)
- Searching (non-empty query) hides Favorites/Recents — only Screens + Products show

**Sidebar pin toggle:**
- Each nav row (all 5 module panels) has a `☆/★` button overlaid on the trailing edge of the full-width nav button (so the active-row highlight spans the whole row — Review Fix #5)
- Dim (opacity 0.3) when not pinned; full accent color (opacity 1) when pinned or hovered
- Calls `ToggleFavoriteCommand` → `_prefs.ToggleFavorite(item)` → flips `IsFavorite`, saves
- `DataTrigger Binding="{Binding IsFavorite}"` drives the ☆/★ text and accent color

## Both-Theme Realization

- **Restore last screen:** Relaunch → `NavigateToLastOrDefault()` restores via `LastViewKey`; stale key falls back cleanly
- **Pin persistence:** `ToggleFavorite` writes `FavoriteKeys` to JSON immediately; `SyncFavoriteFlagsToItems` stamps `IsFavorite` after login/refresh so star state survives logout
- **Recents:** Every `Navigate()` call pushes to `RecentKeys` (de-duped, bounded at 10), persisted immediately; survive restart; filtered by role on restore (stale keys for role-excluded screens are silently skipped)
- **Both themes:** No theme-specific code — all colors use design tokens (`AccentBrush`, `TextSecondaryBrush`)

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | Implementation authored to compile clean; **not re-run during review** (build deferred to testing phase per workflow) |
| Unit tests pass | N/A (no test projects) |
| Manual verification | Pending (separate testing session) |

## Issues Encountered

None during implementation. The implementation followed the established `UiSettingsStore` extension pattern documented in `[[wpf-vista-ui-settings-persistence]]`.

## Review Fixes (post-implementation, claude-code)

A code review surfaced six findings; all were addressed:

1. **Palette zero-state de-dup** — `CommandPaletteViewModel.RunSearchAsync` previously listed every screen in the "Screens" group on an empty query, so a favorited/recent screen appeared two–three times. The Favorites/Recent loops now record their keys in an `acceleratorKeys` `HashSet`, and the zero-state "Screens" pass skips any key already shown above.
2. **Recent excludes current screen** — `RecordNavigation` makes the current screen `recents[0]`, so the "Recent" group echoed the screen you were already on. The recents loop now skips the key equal to `GetLastViewKey()` (and any key already pinned as a favorite).
3. **Dynamic favorite-toggle a11y name** — the pin button's `AutomationProperties.Name` was the static "Toggle Favorite" on every row; it now binds `"Toggle favorite for {0}"` against `DisplayName` in both `ModuleDetailPanel.xaml` and `DeveloperToolsPanel.xaml`.
4. **JSON escaper scope comment** — `UiSettingsStore.EscapeJsonString` escapes only `\` and `"`; added a comment documenting that all stored values (type FullNames, theme name) are control-char-free so the minimal escaper is sufficient, and not to reuse it for arbitrary text.
5. **Full-width active-row highlight** — the nav row was a 2-column Grid (`*` + 28 px), so the active highlight stopped short of the star column. The row is now a single-cell Grid with the full-width `NavItemStyle` button and the star button overlaid `HorizontalAlignment=Right`, restoring an edge-to-edge highlight.
6. **Build claim corrected** — the original "0 errors, 0 warnings" line is the implementer's claim; per the hybrid workflow no build was run during review, so the table above now reflects that the build was not independently re-verified.

## What's Next

- [ ] Manual verification: relaunch restores last screen; stale key falls back; pin survives restart; recents update and survive restart (separate testing session)
- ~~UX-44 (Language preferences) can add its own keys to `UiSettingsStore` using the same extension pattern~~ — voided; UX-44 / localization was dropped 2026-06-07 (UI stays English-only)

## Codebase Wiki Discrepancies

The following are new and must be synced by Antigravity:
- New file: `Services/IUserPreferencesService.vb`
- New file: `Services/UserPreferencesService.vb`
- Modified: `Services/UiSettingsStore.vb` — 3 new properties, 2 new private helpers
- Modified: `Models/NavigationItem.vb` — `IsFavorite` property added
- Modified: `ViewModels/MainWindowViewModel.vb` — `ToggleFavoriteCommand`, `NavigateToLastOrDefault()`, `_prefs` field; constructor signature changed
- Modified: `ViewModels/Shell/CommandPaletteViewModel.vb` — `_prefs` field; constructor signature changed; `GetModuleIcon` helper
- Modified: `Views/Shell/ModuleDetailPanel.xaml` — `FavoriteToggleButtonStyle`, `NavItemWithFavoriteTemplate`, 4 ItemsControl ItemTemplate attrs
- Modified: `Views/Shell/Modules/DeveloperToolsPanel.xaml` — DataTemplate extended with star button

## Cross-References

- Agent Wiki patterns consulted: `[[wpf-vista-ui-settings-persistence]]`, `[[wpf-vista-command-palette]]`, `[[wpf-vista-iconography]]`, `[[wpf-vista-theming-conventions]]`
- Domain Wiki: N/A (personalization only, no business data)
