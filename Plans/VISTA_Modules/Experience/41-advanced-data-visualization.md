---
module: MerchSys.App
plan-id: UX-41
title: "Advanced Data Visualization — Interactive Charts, Hover Tooltips, Drill-down & Period Selectors"
depends-on: [UX-12, UX-16]
estimated-files: 7
---

# Advanced Data Visualization — Interactive Charts, Hover Tooltips, Drill-down & Period Selectors

> **Level 3 (Pro) — item P6** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). Makes the dashboards' charts
> *interactive*: hover tooltips on points, drill-down from a KPI to its detail view, and 7/30/90-day
> period selectors. Additive markup + read-only VM queries on the hand-built UX-12 primitives.

## Context

UX-12 added **static** sparklines and delta indicators (`Views/Shell/Sparkline.xaml`,
`DeltaIndicator.xaml`), hand-built with `Rectangle`/`Path` (UX-08 mandated no charting NuGet). The next
step lets the Owner *explore* trends rather than only glance at them — Shneiderman's "overview first,
zoom and filter, details on demand" and NN/g dashboard drill-down patterns, on Tufte data-ink
principles.

Interactivity here is **additive**: hover/tooltip + click-to-drill on the existing chart primitives, and
period selectors bound to **additive read-only** queries. **No new business data, no new package.**

## Prerequisites

- **UX-12** — `Views/Shell/Sparkline.xaml` and `DeltaIndicator.xaml`, the hand-built chart primitives
  this makes interactive.
- **UX-16** — `NavigateCommand` (drill-down reuses navigation to the KPI's detail view).
- Existing deps only. **No new NuGet** (charts stay `Rectangle`/`Path`; tooltips/period selection are
  WPF + read-only VM state).

## Scope

### A. Hover tooltips on chart points
Add hover affordance to the sparkline/bar primitives so hovering a point/bar shows its value (and
period label) in a themed tooltip. Pure presentation over the existing geometry; the value comes from
the data the primitive already binds.

### B. Drill-down from KPI to detail
Make a KPI/trend card actionable: clicking it navigates to the relevant detail view via
`NavigateCommand`. No new navigation infrastructure — reuse the nav source-of-truth from UX-16.

### C. Period selectors (7/30/90-day)
Add a period switch on trend cards bound to an **additive read-only** query that returns the series for
the selected window. The query is new and read-only (raw `MySqlConnector` reader); it must not modify or
replace an existing VM property/contract — it adds one.

> **Out of scope:**
> - Any charting/visualization NuGet (UX-08 non-goal stands — charts remain hand-built).
> - New business metrics or KPI *definitions* not already computed by a service; period selectors only
>   re-window existing series.
> - Changing or removing an existing VM property/contract (additive read-only only).

## Specification

### 0. Watch-items

1. **Additive read-only.** Any data a period selector needs is a **new** read-only VM property/query —
   never a change to an existing one. No write path, no contract change.
2. **Raw reader for new reads.** New period queries use `MySqlConnector` + `reader.Read()`, never
   `ToListAsync` on an entity query (silent-empty bug).
3. **Hand-built charts stay.** Interactivity attaches to the existing `Rectangle`/`Path` primitives; no
   charting package is introduced.
4. **Theme-safe + tokens.** Tooltips, hover states, and selectors recolor via tokens in both themes; no
   inline hex.
5. **Drill-down reuses nav.** Click-to-drill goes through `NavigateCommand`; it does not spawn a parallel
   navigation path or open ad-hoc windows.
6. **VB traps** — no `Await` in `Catch`/`Finally` (BC36943) in any async query; full `clr-namespace`
   root prefix (MC3074); reserved-keyword-safe names; `System.Console` if logging.

### 1. Constraints

- Build on the UX-12 primitives and UX-16 navigation; add no parallel chart or nav infrastructure.
- No new NuGet. No inline hex. **Role model** — Owner is the primary beneficiary (read-only KPI
  consumer); drill-down lands on read-only detail for Owner.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both themes):** hovering a chart point shows a correct themed tooltip; clicking a
  KPI drills into its detail view; switching 7/30/90-day re-renders the series from the read-only query;
  all correct in Light and Dark.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Sparkline/bar points show hover tooltips with the point's value/period in both themes.
3. KPI/trend cards drill into the relevant detail view via `NavigateCommand`.
4. Trend cards offer a 7/30/90-day period switch bound to an additive read-only query (raw reader);
   no existing VM property/contract changed.
5. No new NuGet, no new business metric/KPI definition, no write path.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-41-summary.md` (template `Progress/_template.md`). Include: the
hover/tooltip approach on the primitives, the drill-down wiring, the period-selector query (additive,
read-only, raw reader), and both-theme realization (tooltip, drill, period switch). Note `codebase_wiki`
discrepancies (interactive additions to Sparkline/DeltaIndicator, new read-only queries) for Antigravity.

### Documentation
Add `patterns/wpf-vista-interactive-charts.md` (hover/tooltip on hand-built primitives, drill-down via
`NavigateCommand`, additive read-only period queries) per `workflow-agent-wiki-update.md`; update
`agent_wiki/index.md` + `log.md`.
