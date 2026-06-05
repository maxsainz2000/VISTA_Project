---
module: MerchSys.App
plan-id: UX-27
title: "Keyboard-Shortcut Discoverability Overlay — '?' / Ctrl+/ Cheat Sheet"
depends-on: [UX-16, UX-17]
estimated-files: 5
---

# Keyboard-Shortcut Discoverability Overlay — "?" / Ctrl+/ Cheat Sheet

> **Level 2 (Advanced) — item A4** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).

## Context

UX-16 added powerful global shortcuts (Ctrl+K palette, Ctrl+1..4 modules, Ctrl+D0 dev tools) but no
surface to **discover** them, and UX-17 (B1) added dialog/form keys (Enter/Esc, mnemonics). A cheat
sheet turns hidden power into learnable power — the standard companion to a command palette. This plan
adds a dismissible **shortcuts overlay** triggered by `?` or `Ctrl+/`, listing every global shortcut
plus the B1 dialog/form keys, closed by `Esc`, matching the palette's look.

This is **additive, no-logic** — it lists bindings that already exist; it does not add new behaviour
beyond opening/closing itself.

Read `UX-16` (`MainWindow.xaml` `<Window.InputBindings>`, the palette overlay pattern in
`CommandPalette.xaml`, and `MainWindow_PreviewKeyDown`) and `UX-17` (B1 dialog/form keys) first.

## Prerequisites

- **UX-16** — the command palette and the declared `<Window.InputBindings>` (Ctrl+K, Ctrl+1..4,
  Ctrl+D0) the overlay enumerates; reuse its overlay presentation pattern.
- **UX-17 (B1)** — the dialog/form keyboard layer whose keys (Enter confirm, Esc cancel, mnemonics)
  the sheet also documents.
- Existing deps only. **No new NuGet.**

## Scope

- **A `ShortcutsOverlay` component** (`Views/Shell/ShortcutsOverlay.xaml` + `.xaml.vb`) reusing the
  exact overlay pattern already built for the palette: `OverlayBrush` backdrop, centered `SurfaceBrush`
  panel, tokenized typography, `Esc` to dismiss.
- **An input binding** for `Ctrl+/` and `?` (`Key.OemQuestion`) in `MainWindow.xaml`
  `<Window.InputBindings>` (or via `MainWindow_PreviewKeyDown`, mirroring the palette) that toggles the
  overlay.
- **Static, grouped content** describing the bindings: Global (Ctrl+K, Ctrl+1..4, Ctrl+D0), Dialog &
  Form (Enter, Esc, access-key mnemonics from B1), and this overlay's own `?` / `Esc`.

> **Out of scope:**
> - A dynamic/registry-driven shortcut model — the content is a maintained static list (the bindings
>   are themselves static in `MainWindow.xaml`).
> - Per-screen contextual shortcut hints, or editing/remapping shortcuts.
> - The POS keypad flow — that is **A7 (UX-30)** (it may be cross-linked here once it lands).

## Specification

### 0. Watch-items

1. **Reuse the palette overlay pattern — don't reinvent.** Backdrop `OverlayBrush`, centered
   `SurfaceBrush` panel with `RadiusLarge`/shadow tokens, `Esc` handling routed exactly like the
   palette (`MainWindow_PreviewKeyDown` already owns palette Esc — extend the same handler so the two
   overlays don't conflict). Only one overlay should be open at a time.
2. **`?` vs typing `?`.** `?` must open the sheet only when not typing into a text field — if a
   `TextBox`/editable control has focus, `?` must type normally. `Ctrl+/` is unambiguous; for bare
   `?` (`Shift+OemQuestion`) guard on focus (don't hijack text entry). The palette/search inputs must
   remain typeable.
3. **Tokens + theme only.** All chrome/typography via `DynamicResource`; the sheet recolors on theme
   toggle. No inline hex. Key chips/labels use existing caption/body tokens.
4. **Keep the listed keys accurate.** The content must match the real `<Window.InputBindings>` and the
   B1 keys — if a shortcut isn't actually wired, don't list it. Single source of truth is
   `MainWindow.xaml` + the B1 plan.
5. **Role-appropriate.** Owner sees the same overlay but only role-applicable entries (e.g. dev tools
   Ctrl+D0 only if Owner can reach them — match what the bindings actually expose per role). No
   separate behaviour beyond what the bindings already gate.
6. **Esc/`?` toggle is inert otherwise** — opening the sheet changes no app state; closing restores
   focus to where it was.
7. **VB traps** — code-behind uses reserved-keyword-safe names; `.` continuation at line-end; no
   `Await` in `Catch`/`Finally` (BC36943); XAML `clr-namespace` uses the full root prefix
   `clr-namespace:MerchSys.App.Views.Shell` (MC3074).

### 1. The overlay component

A modal-over-shell panel mirroring `CommandPalette.xaml`'s structure: dimmed backdrop, centered card,
grouped two-column list (key chip · description). Visibility bound to a shell VM flag (e.g.
`IsShortcutsOverlayOpen`) toggled by the input binding, matching how the palette open-state is driven.

### 2. Trigger + dismiss wiring

Add `Ctrl+/` (and guarded `?`) to open; route `Esc` through the existing `MainWindow_PreviewKeyDown`
so palette and shortcuts overlays share consistent dismissal and never both open.

### 3. Constraints

- **Additive / no logic** — only the overlay + its toggle; no other state change.
- **Reuse the palette overlay chrome and Esc routing.**
- **No new NuGet.**
- **Tokens + theme** — recolors on toggle; no inline hex.
- **VB/XAML traps** — full `clr-namespace` root prefix (MC3074); reserved-keyword-safe names.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — press `Ctrl+/` and `?` (outside a text field) → sheet opens; press `?` while
  typing in a `TextBox` → `?` is typed, sheet does not open; `Esc` → closes and focus returns; toggle
  theme → sheet recolors; the listed keys match the actual bindings; opening the palette and the sheet
  don't conflict.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. `?` / `Ctrl+/` opens a readable, grouped shortcut list; `Esc` closes it; it matches the palette's
   look and recolors on theme toggle.
3. `?` does not hijack text entry when an editable control is focused.
4. The listed shortcuts accurately reflect the real `<Window.InputBindings>` and B1 dialog/form keys.
5. No app-state/logic change beyond open/close; palette and shortcuts overlays coexist without
   conflict; Owner sees role-appropriate entries.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-27-summary.md` (template `Progress/_template.md`). Include: the
reuse of the palette overlay pattern, the `?`-vs-text-entry guard, the Esc-routing integration with
`MainWindow_PreviewKeyDown`, the content groups listed, and the realization results. Note
`codebase_wiki` discrepancies (new `ShortcutsOverlay`, the new input binding, the shell-VM open flag).

### Documentation
Add `patterns/wpf-vista-overlay-pattern.md` (or extend the palette pattern entry) documenting the
reusable shell-overlay recipe (backdrop + centered card + shared Esc routing + open-one-at-a-time) and
the `?`-guard rule, per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
