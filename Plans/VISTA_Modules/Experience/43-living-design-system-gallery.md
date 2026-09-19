---
module: MerchSys.App
plan-id: UX-43
title: "Living Design-System Gallery — Token/Component/State Catalogue in Developer Tools"
depends-on: [UX-05, UX-07]
estimated-files: 4
---

# Living Design-System Gallery — Token/Component/State Catalogue in Developer Tools

> **Level 3 (Pro) — item P8** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). A Developer-Tools screen that
> renders **every** design token, component, and state — a Storybook-equivalent and a one-screen
> realization-check. Pure presentation; renders what already exists.

## Context

A living gallery is how a design system stays coherent: it documents what exists (killing the
duplication this roadmap fights — "does this already exist?" answered in one place), and it is a visual
regression surface — boot it in both themes to catch a token or component that fails to realize (the
UX-00 realization-check, automated into one screen).

Everything it shows already exists: tokens in `Themes/Tokens.xaml`/`Light.xaml`/`Dark.xaml`, the shared
components in `Themes/Components.xaml` and `Views/Shell/` (`BusyOverlay`, `EmptyStatePanel`,
`ErrorStatePanel`, `DeltaIndicator`, `Sparkline`, the B-level field row/confirm dialog), and the icon
set in `Themes/Icons.xaml`. The host is the existing `Views/Shell/Modules/DeveloperToolsPanel.xaml`.

**Pure presentation — no logic, no data, no contract.**

## Prerequisites

- **UX-05** — `Themes/Components.xaml`, the shared component library to enumerate.
- **UX-07** — `Themes/Icons.xaml`, the icon set to render.
- Existing host: `Views/Shell/Modules/DeveloperToolsPanel.xaml` (the Developer-Tools surface); tokens in
  `Themes/Tokens.xaml`/`Light.xaml`/`Dark.xaml`; state components in `Views/Shell/`.
- Existing deps only. **No new NuGet.**

## Scope

### A. Token catalogue
Render every design token live: the color/brush tokens (swatch + key name), the type ramp (each level
shown at size with its key), spacing/radii tokens (visual rule/box per token). Pull from the token
dictionaries so the gallery updates automatically as tokens change.

### B. Component & state catalogue
Render each shared component in its states: `BusyOverlay`, `EmptyStatePanel`, `ErrorStatePanel`,
`DeltaIndicator` (up/down/flat), `Sparkline`, the field row, and the confirmation dialog (a triggerable
preview). Each labelled with its name/source so the gallery is the canonical "what exists" reference.

### C. Developer-Tools gating
Surface the gallery as a Developer-Tools view (Developer role only, consistent with the existing
Developer Tools gating). Not an Owner or Manager surface.

> **Out of scope:**
> - Any logic, data, or contract; the gallery renders existing resources only.
> - New components or tokens — it catalogues, it does not create.
> - Editing tokens from the gallery (read/display only).

## Specification

### 0. Watch-items

1. **Renders what exists.** No new component/token; pull from the live dictionaries so the catalogue
   can't drift from reality.
2. **Both themes are the point.** The gallery is the realization surface — it must look correct in Light
   and Dark; every swatch/component recolors via `DynamicResource`.
3. **Developer-only.** Gate to the Developer role like the rest of Developer Tools; not exposed to
   Manager/Owner navigation.
4. **No inline hex.** Swatches read the actual brush tokens, never hardcoded colors (that would defeat
   the regression-surface purpose).
5. **Triggerable previews, not live side effects.** Showing the confirm dialog/busy overlay is a preview
   in the gallery; it must not fire real commands or mutate state.
6. **VB traps** — full `clr-namespace` root prefix (MC3074); reserved-keyword-safe names; `<Setter>`
   targets a DP only; `System.Console` if logging.

### 1. Constraints

- Render from `Tokens.xaml`/`Light.xaml`/`Dark.xaml`, `Components.xaml`, `Icons.xaml`, and the
  `Views/Shell/` state components; add no parallel copies.
- No new NuGet. No inline hex.
- **Role model** — Developer Tools only; not an Owner/Manager surface.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both themes):** the gallery shows every token (color/type/spacing/radii), every
  shared component in its states, and every icon, all correct in Light and Dark; toggling theme
  recolors the whole gallery live.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A Developer-Tools gallery view renders every design token (color/type/spacing/radii) with key names.
3. It renders each shared component in its states and the full icon set, labelled by name/source.
4. It is gated to the Developer role and toggles correctly in both themes (the realization surface).
5. No logic/data/contract change; nothing in the gallery fires real commands or mutates state.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-43-summary.md` (template `Progress/_template.md`). Include: how the
catalogue enumerates tokens/components/icons from the live dictionaries, the Developer-role gating, the
triggerable-preview approach, and both-theme realization. Note `codebase_wiki` discrepancies (new
Developer-Tools gallery view) for Antigravity.

### Documentation
Add a short `patterns/wpf-vista-design-gallery.md` note (the catalogue-from-live-dictionaries approach
and its role as the realization/regression surface) per `workflow-agent-wiki-update.md`; update
`agent_wiki/log.md`.
