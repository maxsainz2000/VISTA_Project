---
module: MerchSys.App
plan-id: UX-22
title: "Tooltips & Affordance — Learnable Icon-Only Controls"
depends-on: [UX-07]
estimated-files: 10
---

# Tooltips & Affordance — Learnable Icon-Only Controls

> **Level 1 (Basic) — item B6** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).

## Context

UX-07 made VISTA icon-rich (the `Themes/Icons.xaml` vector set). Icon-only controls without text are
fast for experts but guesswork for everyone else until a tooltip names them. Today tooltips are largely
absent: `Views/Shell/ActivityRail.xaml` renders icon-only module buttons, the dashboards and toolbars
use icon buttons, and status glyphs (connection indicator, delta arrows) carry meaning that isn't
spelled out. This is a small, broad, high-certainty polish.

This plan adds **tooltips to every icon-only control** and short helper hints where an input's purpose
isn't self-evident, sourced wherever possible from existing labels (e.g. the nav `DisplayName`s already
on the rail's bound items). It is **cosmetic and additive** — no logic, binding-source, or layout change
beyond attaching tooltips.

Read `UX-07` (icon system) and `UX-16` (the `AllNavigableItems`/nav `DisplayName` source the rail
tooltips can reuse) first.

## Prerequisites

- **UX-07** — the icon system; icon-only controls are the targets.
- Existing nav model — rail/module buttons already bind to items carrying `DisplayName`, which is the
  natural tooltip text (no new strings invented).
- Existing deps only. **No new NuGet.**

## Scope

- **Tooltips on icon-only controls:** activity-rail module buttons, dashboard/toolbar icon buttons,
  status indicators (connection status, delta/sparkline glyphs), and any icon-only command button.
- **Helper hints** on the few inputs whose purpose isn't obvious from a nearby label.
- **A shared tooltip style** (optional) in `Themes/Components.xaml` for consistent tokenized tooltip
  chrome (surface, radius, shadow) that recolors with the theme.

> **Out of scope:**
> - Full `AutomationProperties`/screen-reader naming — that is **Pro P1** (accessibility). Tooltips help
>   sighted discovery; SR names are a separate, larger pass. (Where a tooltip text is obvious, the P1
>   plan can later reuse it as the automation name — note candidates but don't build P1 here.)
> - Adding labels/captions that change layout, or a help/onboarding system (that's heavier).
> - Re-theming or restructuring any control beyond attaching a `ToolTip`.

## Deliverables

The tooltip coverage across icon-only controls + helper hints, the optional shared tooltip style, the
implementation summary, and a wiki update.

## Specification

### 0. Watch-items

1. **Reuse existing text; don't invent inconsistent labels.** The rail/module buttons bind to items with
   `DisplayName` — bind the `ToolTip` to that same text so the tooltip and the screen name never drift.
   For other icons, use the wording already used elsewhere for that action (match the command's mnemonic
   label from UX-17 where one exists).
2. **Tooltip every icon-only control — but not redundantly.** A button that already shows text doesn't
   need a tooltip repeating it. Target controls where the *only* affordance is an icon/glyph.
3. **Bound tooltips must survive `DataContext`.** When binding a `ToolTip` inside a `DataTemplate` /
   items control, remember a `ToolTip` is outside the visual tree — use the correct binding source
   (`PlacementTarget`/`RelativeSource`) so the bound text resolves. Verify the rail tooltips actually
   show the item name, not blank.
4. **Tokens + theme.** If a shared tooltip style is added, use tokens (surface/radius/shadow) and
   `DynamicResource` so it recolors with the theme; no inline hex. Keep default show-delay sensible.
5. **Owner sees the same.** Tooltips aid the read-only Owner too; no role gating needed (they describe,
   they don't act).
6. **VB/XAML traps** — full root prefix on any `clr-namespace` (MC3074); a `<Setter>` targets a
   `DependencyProperty` only.

### 1. Coverage sweep

Surface-by-surface (shell rail/chrome → dashboards → module toolbars → status indicators), attach
tooltips to icon-only controls; bind to existing text where available. Provide a coverage table
(surface → controls tooltipped → text source) in the summary.

### 2. Helper hints

For the handful of non-obvious inputs, add a short tooltip/hint. Keep them brief and specific.

### 3. Optional shared tooltip style

If consistency warrants, add a tokenized `ToolTip` style in `Components.xaml` and apply implicitly.

### 4. Constraints

- **Cosmetic / additive** — only tooltips/hints (and an optional tooltip style) are added; no logic,
  binding-source, command, or layout change.
- **Tokens only** — UX-00/UX-05; `DynamicResource`; no inline hex; theme-reactive.
- **VB/XAML traps** — MC3074 full root prefix; `<Setter>` targets a `DependencyProperty` only.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — in **both** themes: hover every icon-only control and confirm a correct,
  readable tooltip appears (rail tooltips show the right screen name); the tooltip chrome recolors with
  the theme.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Every icon-only control (rail, toolbars, status glyphs, icon buttons) has a correct, readable tooltip;
   no control's purpose is guesswork.
3. Rail/module tooltips reuse the existing `DisplayName` (text never drifts from the screen name).
4. Non-obvious inputs have brief helper hints; text-bearing buttons aren't redundantly tooltipped.
5. No logic/binding-source/layout change; tokens only / hex sweep clean; tooltip chrome recolors on theme
   toggle.
6. Owner sees the same tooltips (no role gating).

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-22-summary.md` (template `Progress/_template.md`). Include: the
coverage table (surface → controls → text source), whether a shared tooltip style was added, the bound-
tooltip-in-template handling, candidates noted for the future P1 automation-name reuse, and the both-
themes hover check. Note `codebase_wiki` discrepancies.

### Documentation
Add a short `patterns/wpf-vista-tooltips.md` (reuse-existing-text rule, bound-tooltip-in-template source
caveat, tokenized tooltip style) per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md` +
`log.md`.
</content>
