---
module: MerchSys.App
plan-id: UX-12
title: "Trend & Delta Indicators — Period-over-Period Direction on Dashboards"
depends-on: [UX-10]
estimated-files: 8
---

# Trend & Delta Indicators — Period-over-Period Direction on Dashboards

## Context

The UX-08 audit's defect #6: **no dashboard shows movement.** Every KPI is an absolute number with no
period-over-period direction, so a read-only Owner can see "today's revenue is ₱X" but not "up 12% vs
yesterday" — the question they actually care about. This is the difference between an *operational*
dashboard (status now) and one that supports decisions (direction).

This is the first **Tier 2** plan: it adds **read-only, additive** data and a reusable presentation
control. It must not change any existing property, write path, concurrency path, or business rule — it
only *derives* direction from data the services can already produce.

Two reusable presentation pieces + targeted wiring:

- A **delta indicator** (▲/▼ + percentage, colored by direction) shown beside a hero/secondary metric.
- A **sparkline** (tiny inline trend line/bars) for metrics that have a short time series.

Read `UX-08` (standards, the "Tier 2 = additive only" rule), `UX-09` (hero/secondary card styles the
indicators attach to), `UX-10` (the heroes are already chosen and placed), and `UX-07` (the
`IconBase` + geometry mechanism — the ▲/▼ marks reuse it) first.

## Prerequisites

- **UX-10** — heroes are promoted; this plan decorates them, so their markup is final.
- **UX-07** — `Themes/Icons.xaml` + `IconBase`; add `IconArrowUpGeometry`/`IconArrowDownGeometry` here
  if not already present (24×24, same mechanism).
- The module ViewModels and their backing services/queries (verify exact names during implementation —
  e.g. `FinancialOverviewViewModel` already exposes `MonthlyTrend`; the Owner dashboard VM exposes none).

## Scope

- **New control:** a `DeltaIndicator` and a `Sparkline` presentation control (a `UserControl` or
  templated control under `Views/Shell/`, or styles + a small `Path`/`ItemsControl` template in
  `Themes/`). They are **presentation-only** — `DependencyProperty` inputs (value %, direction,
  point series, brush); **no behavior, no data access.**
- **New geometries (if missing):** `IconArrowUpGeometry` / `IconArrowDownGeometry` in `Themes/Icons.xaml`.
- **Additive VM members (read-only):** on the dashboards that should show direction — compute a
  prior-period comparison and/or expose an existing series for the sparkline. **No existing member is
  changed.**
- **View wiring:** drop the `DeltaIndicator`/`Sparkline` beside the relevant hero/secondary metrics.

> **Out of scope:**
> - Any change to how the underlying metric is computed, stored, or written. Deltas are **derived
>   read-only**.
> - New full-size charts (the 6-month bar chart in Financial Overview stays as-is — only a y-axis
>   reference may be added as a stretch, markup-only).
> - The Purchasing dashboard (UX-13).
> - Touching concurrency/save/command paths in any VM.

## Deliverables

The `DeltaIndicator` + `Sparkline` controls, the new arrow geometries, the additive read-only VM
members, the view wiring, the implementation summary, and a wiki update.

## Specification

### 0. Carry-forward watch-items (from the UX-11 review)

1. **The Owner rollup is the only genuinely new data path — treat it as the main risk.** Every other
   delta in this plan decorates a series the VM already produces (Financial Overview reuses
   `MonthlyTrend`; Daily derives from the period it already loads). The **Owner dashboard VM exposes no
   time series**, so "Today's Revenue Δ vs yesterday / week Δ vs last week" requires a **new read-only
   member backed by a new read**. This is where the Tier-2 "additive only" discipline matters most:
   - Add the rollup as a **read-only** member sourced from the existing Owner read query/service (or a
     new read-only MediatR query contract following `GetCurrentStockQuery`) — **never** a write path,
     and do not modify any existing Owner VM member.
   - The new read **must** use the raw `MySqlConnector.MySqlConnection` reader loop, **not**
     `ToListAsync` on an entity query (EF Core 10 VB.NET empty-list bug). Any async added must not
     `Await` inside `Catch`/`Finally` (BC36943).
   - If yesterday/last-week revenue is **not cheaply available** from the existing Owner data path,
     **skip that delta and document it** rather than building a speculative query.
2. **Add the indicator inside the existing hero card, don't restructure UX-11's wrappers.** UX-11
   moved several heroes (e.g. Financial Overview) into a `ScrollViewer` + `StackPanel` with a
   `MinHeight`-sized hero card. Drop the `DeltaIndicator`/`Sparkline` **inside the existing hero card's
   `StackPanel`** — do not touch the `ScrollViewer`, the `MinHeight`, or the filter toolbars UX-11
   established.

### 1. Presentation controls (no behavior, no data access)

`DeltaIndicator` — inputs (all `DependencyProperty`):

- `Percent` (Double) and/or `Direction` (enum Up/Down/Flat),
- renders `IconArrowUpGeometry`/`IconArrowDownGeometry` (via `IconBase`) + a formatted `%` label,
- `Fill`/`Foreground` resolves to `SuccessBrush` (up), `DangerBrush` (down), `TextSecondaryBrush`
  (flat) via `DynamicResource` — **and the up/down→good/bad mapping must be invertible** (a rising AP
  or AR balance is *not* "good"); expose an `InvertSemantics` (Bool) DP so AP/AR/expiry deltas color
  correctly.

`Sparkline` — inputs:

- `Points` (IEnumerable of Double) bound to an existing short series,
- draws a `Polyline`/bars sized to a small fixed box (reuse the existing `Rectangle`+bound-height bar
  approach already in Financial Overview / Daily Summary — **no charting NuGet**),
- `Stroke`/`Fill` via `DynamicResource AccentBrush`.

Both controls are theme-reactive (all brushes `DynamicResource`) and carry **no** commands or VM logic.

### 2. Additive read-only VM data (per dashboard)

For each dashboard that should show direction, add **read-only** members only, sourced from the VM's
existing backing service/query (or a new **read-only** query contract following the
`GetCurrentStockQuery`/`GetInventoryValuationQuery` pattern). Verify exact VM/service names first.

| Dashboard | Delta / series to add (read-only) | Source |
|---|---|---|
| `Accounting/FinancialOverviewView` | Revenue Δ vs prior period; **sparkline reuses existing `MonthlyTrend`** | reuse `MonthlyTrend`; prior-period from the same service |
| `OwnerDashboardView` | Today's Revenue Δ vs yesterday; week Δ vs last week | **needs a new read-only rollup** (Owner VM has no series) — add via the existing read query/service, not a write path |
| `POS/DailySummaryView` | Total Sales Δ vs previous comparable period | derive from the period the VM already loads |
| `Inventory/StockDashboardView` *(optional)* | Stock-value Δ (info only) | only if cheaply available; else skip and note |

> **Where prior-period data is not cheaply available, skip that delta and document it** rather than
> adding an expensive or speculative query. Accuracy over coverage.
> **Read path rule:** any new read uses the raw `MySqlConnector.MySqlConnection` reader loop, **not**
> `ToListAsync` on an entity query (the EF Core 10 VB.NET empty-list bug). Any async added to a VM must
> not `Await` inside `Catch`/`Finally` (BC36943).

### 3. View wiring

Place a `DeltaIndicator` beside each hero value (and selected secondaries), and a `Sparkline` inside
the hero card where a series exists (Financial Overview first — it already has `MonthlyTrend`). Bind to
the new read-only members. Set `InvertSemantics=True` for AP/AR/overdue/expiry-type metrics so "up" is
shown as bad. Do not alter the hero's existing value binding.

### 4. Constraints

- **Additive only** — no existing VM member, command, write path, concurrency handler, or business
  rule is modified. New members are read-only and computed.
- **No new NuGet package** — sparkline is hand-built.
- **No inline hex** — all indicator colors are tokens; UX-04 sweep stays clean.
- **MVVM/architecture** — controls hold no data logic; VM lives in its module library; any cross-module
  read uses a MediatR **query** contract (`SharedKernel`), never a direct cross-module service call.
- **VB/EF traps** — raw `MySqlConnector` reader for new reads (no `ToListAsync`); no `Await` in
  `Catch`/`Finally`; full root prefix on `clr-namespace` (MC3074); `<Setter>` → `DependencyProperty`
  only; no brush token into a `Color` property.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — boot each wired dashboard in **both** themes; confirm deltas show the correct
  direction/color (and inverted semantics for AP/AR), and sparklines render.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. `DeltaIndicator` and `Sparkline` are reusable presentation controls with `DependencyProperty`
   inputs, theme-reactive brushes, and **no** behavior/data access.
3. Each wired dashboard shows period-over-period direction on its hero (and selected secondaries),
   colored correctly — including **inverted** semantics where a rising balance is bad (AP/AR/overdue).
4. Financial Overview's sparkline **reuses the existing `MonthlyTrend`** (no new query for it); any
   genuinely new read uses the raw `MySqlConnector` reader pattern.
5. **No existing VM member, command, write/concurrency path, or business rule changed** — diff shows
   only additive read-only members + view wiring + the new controls/geometries.
6. **Hex sweep clean**; theme toggle recolors indicators live.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-12-summary.md` (template `Progress/_template.md`). Include: the
control APIs (DP list), which dashboards got deltas/sparklines and the exact read-only members + source
(reused vs new query), which deltas were **skipped** for lack of cheap prior-period data (and why), the
`InvertSemantics` mapping per metric, and `codebase_wiki` discrepancies.

### Documentation
Add `patterns/wpf-vista-trend-indicators.md` (the delta/sparkline mechanism, the invert-semantics rule,
the "derive read-only, never new write path / never `ToListAsync`" rule), per
`workflow-agent-wiki-update.md`. Update `agent_wiki/index.md` + `log.md`.
