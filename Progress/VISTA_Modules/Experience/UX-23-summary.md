---
module: MerchSys.App
agent: claude-code
date: 2026-06-05
plan-ref: Plans/VISTA_Modules/Experience/23-window-state-persistence.md
status: completed
---

## Task Summary

Implemented per-laptop window-placement persistence (size, position, maximized state). On close the
window's `RestoreBounds` + maximized flag are saved; on launch they are restored with an off-screen
clamp. `ui-settings.json` was upgraded from a single-key theme file to a multi-key typed store
shared by `ThemeService` and the new `WindowPlacementService`.

**Plan:** `[[23-window-state-persistence]]`

## What Was Done

- Created `Services/UiSettingsStore.vb` — singleton JSON store for all per-laptop UI preferences.
  Reads/writes `%LOCALAPPDATA%\MerchSys\ui-settings.json` with all known keys (`theme`,
  `windowLeft`, `windowTop`, `windowWidth`, `windowHeight`, `windowMaximized`). Uses
  `System.Text.Json.JsonDocument` (BCL — no new NuGet) for parsing; hand-rolled JSON string for
  writing (matching existing approach). `LoadFromDisk()` runs eagerly in the constructor.
  `Save()` is called by both `ThemeService` and `WindowPlacementService` after updating their
  respective properties, so neither save clobbers the other's keys.

- Created `Services/WindowPlacementService.vb` — contains `WindowPlacement` (data class) and
  `WindowPlacementService`. `LoadPlacement()` reads from the store, validates the saved rectangle
  against `SystemParameters.VirtualScreenLeft/Top/Width/Height`, requires ≥ 100×30 px of the
  window to be on-screen, clamps the top-left so the title bar remains reachable, and returns
  `Nothing` if off-screen (triggering first-run defaults). `SavePlacement(window)` captures
  `window.RestoreBounds` when maximized (not the full-screen extent) and the normal
  `Left/Top/Width/Height` otherwise, then calls `_store.Save()`.

- Modified `Services/Theming/ThemeService.vb` — constructor now injects `UiSettingsStore`. 
  `LoadPersisted()` reads `_store.Theme` (set by `UiSettingsStore.LoadFromDisk` at construction)
  instead of reading the file directly. `SavePersisted()` updates `_store.Theme` and calls
  `_store.Save()`. File I/O removed from this class. Theme persistence continues to work
  identically from the caller's perspective.

- Modified `MainWindow.xaml.vb` — constructor now receives `WindowPlacementService` as a fourth
  parameter. After `InitializeComponent()`, calls `LoadPlacement()`:
  - Saved placement found → `WindowStartupLocation = Manual`, sets `Left/Top/Width/Height`,
    stores `_pendingMaximize`.
  - No placement (first run or off-screen fallback) → `WindowStartupLocation = CenterScreen`,
    `_pendingMaximize = True`.
  Added `MainWindow_Loaded` handler: applies `WindowState.Maximized` if `_pendingMaximize` is
  set (deferred from constructor so WPF sets `RestoreBounds` to the normal bounds we assigned,
  enabling sensible un-maximize). Added `MainWindow_Closing` handler: calls
  `_placementService.SavePlacement(Me)`.

- Modified `MainWindow.xaml` — removed `WindowState="Maximized"` and
  `WindowStartupLocation="CenterScreen"`. These are now set exclusively from the code-behind
  (first-run defaults or restored placement), removing the unconditional maximize-on-every-launch
  behavior. `MinHeight/MinWidth/Height/Width` remain in XAML.

- Modified `Application.xaml.vb` — registered `UiSettingsStore` and `WindowPlacementService` as
  singletons before the `ThemeService` line so the DI container can resolve them as dependencies.

## Settings-File Upgrade Approach

Chose the **single typed store** approach (`UiSettingsStore`). Both `ThemeService` and
`WindowPlacementService` hold a reference to the same singleton and call `_store.Save()` after
updating their own keys. The file always contains all six keys so a save from either service never
loses the other's data. `UiSettingsStore` loads the file once in its constructor; subsequent
accesses read in-memory state only.

## Theme-Persistence Regression Check

`ThemeService.LoadPersisted()` previously read the file directly and checked
`json.Contains("""Dark""")`. After the change it reads `_store.Theme` (populated by the same file
read, same timing). The `"Dark"` / `"Light"` string values written by `AppTheme.ToString()` are
unchanged; the `.Equals("Dark", OrdinalIgnoreCase)` check replaces `Contains`. Verified: the
theme key is written in every `_store.Save()` call regardless of which service triggered it.

## Off-Screen Clamp / Fallback Logic

`WindowPlacementService.LoadPlacement()`:
1. Returns `Nothing` immediately if `WindowLeft` or `WindowTop` is `Double.NaN` (no saved state).
2. Computes the visible overlap of the saved rectangle with `SystemParameters.VirtualScreen*`.
3. If visible overlap < 100 px horizontally **or** < 30 px vertically → returns `Nothing`
   (off-screen fallback: `CenterScreen` + Maximized). This handles the common case of a laptop
   that was docked to a larger/secondary monitor that is now disconnected.
4. Otherwise clamps `Left` and `Top` so the title bar stays reachable, and caps `Width/Height` to
   the virtual screen dimensions.

## Maximized / RestoreBounds Handling

- **Saving:** when `window.WindowState = Maximized`, `window.RestoreBounds` (the pre-maximize
  extent) is saved — not `window.Left/Top/Width/Height` which are the full-screen bounds.
- **Restoring:** in the `MainWindow` constructor, `Left/Top/Width/Height` are set to the saved
  **normal** bounds; `WindowState.Maximized` is applied later in `MainWindow_Loaded` (after the
  visual tree is ready). WPF then sets `RestoreBounds` to the bounds we assigned, so the user can
  un-maximize to those exact dimensions.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build`) | ✅ 0 errors, 0 warnings |
| Theme persistence | ✅ Verified — `ui-settings.json` is extended, not replaced |
| Manual UI verification | N/A (testing phase) |

## Realization Notes

Build confirmed clean. Full realization checklist (resize/move → close → relaunch; maximize → close
→ relaunch; simulate off-screen → fallback; delete settings file → first-run defaults; theme toggle
→ verify theme key preserved) deferred to the operator testing phase per project protocol.

## Issues Encountered

None. The `System.Text.Json.JsonDocument` BCL API (`TryGetProperty(name, ByRef element)`) works
cleanly in VB.NET. The `Using doc = JsonDocument.Parse(json)` pattern handles disposal correctly.

## Codebase Wiki Discrepancies

- `UiSettingsStore` (`Services/UiSettingsStore.vb`) is a new class not yet tracked in
  `codebase_wiki/app/index.md`.
- `WindowPlacement` and `WindowPlacementService` (`Services/WindowPlacementService.vb`) are new,
  not yet tracked.
- `ThemeService` constructor signature changed — now takes `UiSettingsStore`. The
  `codebase_wiki` entry for `ThemeService` needs updating.
- `MainWindow.xaml.vb` constructor now has a fourth parameter (`WindowPlacementService`) and two
  new event handlers (`Closing`, updated `Loaded`). The `codebase_wiki` entry needs updating.
- `MainWindow.xaml`: `WindowState` and `WindowStartupLocation` attributes removed.

## What's Next

> Tracked by `Operator/UX-verification-checklist.md` Tests 2–6.

- [ ] Operator realization test: resize/move → close → relaunch (verify position restored)
- [ ] Operator realization test: maximize → close → relaunch (verify maximized; un-maximize to sensible size)
- [ ] Operator realization test: simulate off-screen → verify CenterScreen fallback
- [ ] Operator realization test: delete `ui-settings.json` → verify first-run defaults, no crash
- [ ] Operator realization test: toggle theme after window-move → verify both keys survive in file

## Cross-References

- Agent Wiki entries consulted: `[[wpf-mainwindow-not-shell-window]]`
- Pattern added: `[[wpf-vista-ui-settings-persistence]]`
