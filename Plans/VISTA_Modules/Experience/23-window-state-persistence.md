---
module: MerchSys.App
plan-id: UX-23
title: "Window-State Persistence — Remember Size / Position / Maximized Per Laptop"
depends-on: [UX-01]
estimated-files: 5
---

# Window-State Persistence — Remember Size / Position / Maximized Per Laptop

> **Level 1 (Basic) — item B7** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md). Final
> item of the Basic correctness floor.

## Context

`MainWindow.xaml` hardcodes `WindowState="Maximized"` (plus `Height="720" Width="1280"
WindowStartupLocation="CenterScreen"`), so the app ignores wherever the user left the window and
re-maximizes on every launch. Across the 4 client laptops (different screens) and the read-only Owner
station, remembering the last window arrangement is a small desktop courtesy users expect.

There is already a proven per-laptop persistence mechanism to extend: `Services/Theming/ThemeService.vb`
writes a hand-rolled mini-JSON to `%LOCALAPPDATA%\MerchSys\ui-settings.json`
(`{"theme": "Dark"}`) and reads it back. This plan adds **window-placement persistence** — save size,
position, and maximized/normal state on close; restore on launch — reusing that same file and folder. It
is **shell-only and additive**; no view, data, or business change.

Read `UX-01` (`ThemeService` persistence) and `[[wpf-mainwindow-not-shell-window]]` first.

## Prerequisites

- **UX-01** — `ThemeService`'s `%LOCALAPPDATA%\MerchSys\ui-settings.json` read/write pattern (the file
  and folder to reuse).
- Existing deps only. **No new NuGet** (hand-rolled JSON or a tiny serializer, matching the existing
  no-external-lib approach).

## Scope

- **A window-placement service** (`Services/WindowPlacementService.vb` or fold into a small UI-settings
  service): save `Left`/`Top`/`Width`/`Height` and whether the window was maximized; load them back.
- **Persist to the existing `ui-settings.json`** — extend it to carry window-placement keys **alongside**
  the existing `theme` key (do not clobber the theme on save, and do not break theme load).
- **Wire `MainWindow`** to restore placement on load (replacing the hardcoded `Maximized`/`CenterScreen`
  defaults) and save on close.

> **Out of scope:**
> - Per-*user* preferences, last-viewed-screen restore, favorites/recents — that is **Pro P3**
>   (personalization), which builds on this per-laptop mechanism. This plan persists **window placement
>   per laptop** only.
> - Density/theme/other prefs (theme already persists; density is Pro P5).
> - Multi-window/secondary-window placement — there is one shell window.

## Specification

### 0. Watch-items

1. **Don't clobber the existing `theme` key.** `ThemeService` currently writes the whole file as
   `{"theme": "<value>"}` via naive string interpolation, and reads theme back with a fragile
   `json.Contains("Dark")`. Adding window keys means the file now has multiple values — **upgrade the
   read/write to handle multiple keys without breaking theme load**. Safest: introduce a single small
   typed settings object (e.g. `UiSettings` with `Theme` + `WindowPlacement`) serialized/deserialized in
   one place, and have *both* `ThemeService` and the placement service go through it. If you keep them
   separate, the placement save must preserve the existing `theme` value (read-modify-write), and the
   theme save must preserve placement. Verify theme persistence still works after the change.
2. **Validate restored bounds against current screens (the off-screen trap).** A laptop may reconnect
   with fewer/smaller monitors than when the window was last saved; blindly restoring `Left`/`Top` can
   place the window off-screen and "lost". On load, clamp the restored rectangle to the current
   `SystemParameters`/virtual screen working area; if it doesn't intersect any screen, fall back to
   `CenterScreen` defaults. Restore `Maximized` as a state, not as literal saved bounds.
3. **Save the *restore* bounds, not the maximized bounds.** When the window is maximized, persist the
   normal (restored) size/position plus a `wasMaximized=true` flag — otherwise restoring yields a
   window that can't be un-maximized to a sensible size. Use `RestoreBounds` when maximized.
4. **Fail-safe, like `ThemeService`.** All file I/O is wrapped so a corrupt/inaccessible settings file
   never crashes startup — fall back to the current sensible defaults (maximized/centered). Mirror the
   existing `Try/Catch` fail-safe style.
5. **No `Await` in `Catch`/`Finally`** if any of this is async (BC36943) — the existing persistence is
   synchronous file I/O; keep it synchronous and simple.
6. **Owner = same behavior.** The Owner station remembers its window too; no role distinction.

### 1. The placement service

Save (`Left`, `Top`, `Width`, `Height`, `IsMaximized`) and load with screen-bounds validation + defaults
fallback. Persist via the shared `ui-settings.json` (watch-item #1).

### 2. Wiring `MainWindow`

On load, apply restored placement (clamped); set `WindowState=Maximized` if the flag was set; otherwise
apply the saved normal bounds. On `Closing`, capture `RestoreBounds` + maximized flag and save. Remove
reliance on the hardcoded `Maximized`/`CenterScreen` as the *only* behavior (they become the first-run
fallback).

### 3. Constraints

- **Shell-only / additive** — only `MainWindow` placement + the settings plumbing change; no view, data,
  binding, command, or business change.
- **Reuse the existing settings file** — extend `ui-settings.json`; do not introduce a second settings
  store; do not break theme persistence.
- **No new NuGet.**
- **VB traps** — reserved-keyword-safe names; no `Await` in `Catch`/`Finally` (BC36943).
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — resize/move the window, close, relaunch → placement restored; maximize, close,
  relaunch → returns maximized and un-maximizes to a sensible size; simulate an off-screen/smaller-screen
  case → window lands on-screen (clamp/fallback works); confirm theme still persists after the change;
  delete the settings file → first-run defaults (maximized/centered) apply without error.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. The window's size, position, and maximized state are saved on close and restored on launch (per
   laptop), replacing the unconditional `Maximized`/`CenterScreen` startup.
3. Restored bounds are validated against current screens — the window is never restored off-screen;
   maximized restores as a state with a sensible un-maximize size (`RestoreBounds`).
4. Placement shares `ui-settings.json` with the theme key **without** breaking theme persistence;
   corrupt/missing settings fall back to first-run defaults without crashing.
5. No view/data/business change; Owner station behaves identically.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-23-summary.md` (template `Progress/_template.md`). Include: the
settings-file upgrade approach (single typed object vs read-modify-write) and the theme-persistence
regression check, the off-screen clamp/fallback logic, the maximized/`RestoreBounds` handling, and the
realization results for the resize/maximize/off-screen/missing-file cases. Note `codebase_wiki`
discrepancies (the new placement service + the `ui-settings.json` schema change).

### Documentation
Add `patterns/wpf-vista-ui-settings-persistence.md` (the shared `ui-settings.json` multi-key upgrade, the
off-screen clamp rule, the `RestoreBounds`/maximized handling, fail-safe I/O) per
`workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`. This pattern is the foundation
Pro P3 (personalization) will extend.
</content>
