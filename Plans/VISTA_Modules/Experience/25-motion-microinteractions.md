---
module: MerchSys.App
plan-id: UX-25
title: "Motion & Micro-interactions — Restrained Transitions on Views & Controls"
depends-on: [UX-02, UX-03, UX-04, UX-05]
estimated-files: 7
---

# Motion & Micro-interactions — Restrained Transitions on Views & Controls

> **Level 2 (Advanced) — item A2** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).
> Picks up the animation polish that UX-08's non-goals explicitly deferred.

## Context

After typography, motion is the clearest "premium" signal. Today content swaps via
`ContentControl Content="{Binding CurrentView}"` in `MainWindow.xaml` are **instant**; the shell has
gentle hover/selection from UX-02 but content changes and most control states pop without transition.
This plan adds **tasteful, restrained** motion: a short fade/slide on content-view changes, eased
hover/pressed states on buttons and list rows, a gentle selection transition, and expand/collapse
easing. **No bounce, no spectacle, ~150–250 ms, eased.**

This is **cosmetic only** — no layout, data, or logic change. Reduced-motion is respected.

Read the shell/control-style work (`UX-02..UX-05`) and `[[wpf-vista-theming-conventions]]` first.

## Prerequisites

- **UX-02..UX-05** — the shell re-skin, control styles, and view migration that define the templates
  motion attaches to (`Themes/Controls*.xaml`, the content host in `MainWindow.xaml`).
- Existing deps only. **No new NuGet** (WPF `Storyboard`/`VisualStateManager`/`DoubleAnimation` are
  built in).

## Scope

- **Centralised motion tokens** in `Themes/Tokens.xaml`: a standard `Duration` (e.g.
  `MotionDurationFast` ≈ 150 ms, `MotionDurationStd` ≈ 220 ms) and a shared easing function resource
  (e.g. a `CubicEase`/`QuadraticEase` with `EaseOut`). All motion references these — no inline
  durations scattered in templates.
- **Content-host transition**: a short fade (optionally a few-px slide) when `CurrentView` changes in
  the shell content `ContentControl`.
- **Control micro-interactions**: eased hover/pressed/selected transitions on the shared button, list
  row / `DataGridRow`, and nav-item templates via `VisualStateManager` `VisualTransition`s.
- **Expand/collapse easing** where the shell already expands/collapses regions.

> **Out of scope:**
> - Skeleton loaders / shimmer — that is **A3 (UX-26)**.
> - Page-level choreography, parallax, hero animations, or anything decorative.
> - Animating data values (e.g. counting-up KPI numbers).

## Specification

### 0. Watch-items

1. **Subtle and fast — clarify, not decorate.** Durations live in the 150–250 ms band; easing is
   `EaseOut`-biased; no overshoot/bounce. Anything a user would notice as "slow" or "showy" is wrong.
   Motion must never delay the moment data becomes interactive.
2. **Respect reduced motion.** If the OS signals reduced motion / disabled animations, transitions
   must degrade to instant. Read `SystemParameters.ClientAreaAnimation` (and/or the
   menu/animation-enabled system flags) once at startup; when false, do not run the storyboards.
   Provide a single switch so this is centrally controllable.
3. **Tokens only.** Durations and easing come from `Tokens.xaml` via `DynamicResource`/`StaticResource`
   — never hardcode `Duration="0:0:0.2"` inline in a control template.
4. **No layout reflow from motion.** Transitions animate opacity/transform (composited, cheap), not
   `Width`/`Height`/`Margin` that force layout passes — except the deliberate expand/collapse, which
   must stay smooth on the target laptops.
5. **Don't break virtualization or selection.** Row transitions must not disable `DataGrid`/list UI
   virtualization or interfere with selection, keyboard focus (B1/UX-17), or the existing hover styles.
6. **Theme-safe.** Animated brushes/states must read theme tokens so motion looks right in both Light
   and Dark; no inline colors introduced by the animation work.
7. **VB traps** — any code-behind hook (e.g. wiring the reduced-motion flag) uses reserved-keyword-safe
   names; `.` continuation at line-end; no `Await` in `Catch`/`Finally` (BC36943).

### 1. Motion tokens

Define `MotionDurationFast`, `MotionDurationStd`, and a shared easing resource in `Tokens.xaml`. A
single boolean resource (e.g. `MotionEnabled`) gates whether storyboards run, set from the
reduced-motion check at startup.

### 2. Content-host transition

When `CurrentView` changes, the shell content host plays a short fade-in (optionally a small
upward/sideways slide of a few px) using the standard duration/easing. Keep it ≤ ~220 ms so navigation
still feels instant.

### 3. Control micro-interactions

Add `VisualTransition`s to the shared templates in `Themes/Controls*.xaml` (button hover/pressed,
`DataGridRow`/list-row hover/selected, nav-item). Ease the state changes that currently snap.

### 4. Constraints

- **Cosmetic / additive** — no layout, data, binding, command, or business change.
- **Reduced-motion respected** — degrades to instant centrally.
- **No new NuGet.**
- **Tokens + theme** — durations/easing/brushes via resources; recolors and re-times from one place.
- **No virtualization/selection/focus regressions.**
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — navigate between modules → content fades/slides subtly; hover/press buttons
  and rows → eased states; toggle theme → motion still correct in both; enable OS reduced-motion →
  transitions become instant; confirm no lag or layout jump on the target laptops.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Content-view changes and interactive control states (hover/pressed/selected) animate consistently
   and subtly (~150–250 ms, eased) in both themes.
3. All durations/easing come from centralised `Tokens.xaml` resources — no inline timing in templates.
4. OS reduced-motion degrades all transitions to instant via a single central switch.
5. No layout/data/business change; no virtualization, selection, or keyboard-focus regression.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-25-summary.md` (template `Progress/_template.md`). Include: the
motion-token values chosen, the content-host transition approach, which control templates got
`VisualTransition`s, the reduced-motion detection mechanism and central switch, and the realization
results (subtlety, theme-correctness, reduced-motion fallback, no-jank). Note `codebase_wiki`
discrepancies (new tokens, transition additions to control styles).

### Documentation
Add `patterns/wpf-vista-motion.md` (the duration/easing token set, the reduced-motion gate,
animate-opacity/transform-not-layout rule, the content-host transition recipe) per
`workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
