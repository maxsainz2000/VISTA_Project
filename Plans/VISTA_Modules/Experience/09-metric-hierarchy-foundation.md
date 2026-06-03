---
module: MerchSys.App
plan-id: UX-09
title: "Metric-Hierarchy & Layout Foundation — Shared Card, FilterBar, and Overflow Standards"
depends-on: [UX-08]
estimated-files: 4
---

# Metric-Hierarchy & Layout Foundation — Shared Card, FilterBar, and Overflow Standards

## Context

UX-08 established that the dashboards' core defect is **no primary-metric dominance** — every KPI row
uses equal-weight cards, violating the 40-30-20-10 rule — plus inconsistent overflow safety and rigid
filter toolbars. Before any view is restructured, the **reusable building blocks** those changes
depend on must exist in the shared library, exactly as UX-05 built `Components.xaml` before sweeping
views to it.

This plan adds **only the foundation**: a small family of metric-hierarchy card styles, a responsive
`FilterBar` container style, and the documented overflow standard. It edits **no view files** — that is
UX-10's and UX-11's job. Keeping the foundation in its own batch means the styles are reviewed and
build-verified once, in isolation, before ten views start depending on them (highest accuracy).

Read `UX-08` (the standards contract) and `UX-05` (`Themes/Components.xaml` + the `Application.xaml`
merge order — this plan extends that library) first.

## Prerequisites

- **UX-05** — `Themes/Components.xaml` exists and is merged in `Application.xaml` after
  `Controls.xaml`/`Controls.DataGrid.xaml`. The new styles live beside the existing `CardStyle`,
  `MetricValueStyle`, `MetricValueLargeStyle`, `CaptionLabelStyle`.
- **UX-01** — the token contract (`AccentBrush`, `SurfaceBrush`, `TextPrimaryBrush`,
  `TextSecondaryBrush`, type-scale doubles `FontSizeCaption…LargeTitle`, radii, `CardShadow`).

## Scope

- **Modified:** `Themes/Components.xaml` — add the metric-hierarchy card family + the `FilterBar`
  container style. (If `Components.xaml` is already large, a sibling `Themes/Dashboard.xaml` may be
  created instead and merged after `Components.xaml`; document the choice.)
- **Modified (only if a new dictionary is added):** `Application.xaml` — merge the new dictionary after
  `Components.xaml`.
- **No view files are edited in this plan.**

> **Out of scope:** applying any of these styles to a view (UX-10/UX-11), and any sparkline/delta
> control (UX-12). This plan ships *unused* styles; that is intentional — they are consumed by the
> next plans. Do not pre-wire a view "to test"; the realization check here is a throwaway harness only.

## Deliverables

The new styles in `Themes/Components.xaml` (or `Themes/Dashboard.xaml` + merge edit), the
implementation summary, and a wiki update.

## Specification

### 1. Metric-hierarchy card family (the 40-30-20-10 vocabulary)

Three card tiers + matching value/label type roles, so a dashboard expresses weight through **shared
styles**, never per-view font sizes. All keyed, all `BasedOn` the existing `CardStyle` where sensible,
all colors via `DynamicResource` tokens.

| New key | Role | Spec (intent — tune to the type scale) |
|---|---|---|
| `PrimaryMetricCardStyle` | the single hero metric (40%) | `Border` `BasedOn CardStyle`; larger padding; optional 1px `AccentBrush` accent edge or subtle elevation so it reads first. |
| `PrimaryMetricValueStyle` | hero value text | `FontSizeLargeTitle` (28) / `FontSizeTitle` (20) range, `SemiBold`/`Bold`, `TextPrimaryBrush`. |
| `SecondaryMetricCardStyle` | 2–3 supporting metrics (30%) | `BasedOn CardStyle`; standard padding. |
| `SecondaryMetricValueStyle` | secondary value text | `FontSizeTitle` (20), reuse/sit just below `MetricValueLargeStyle`. |
| `TertiaryMetricStyle` | the long tail (20%) | compact label+value, `FontSizeSubhead`/`Body`; for the demoted "secondary strip". |
| `MetricCaptionStyle` *(reuse `CaptionLabelStyle` if identical)* | the small label under every value | `FontSizeCaption` (11), `TextSecondaryBrush`. |

> **Express hierarchy with type scale and one accent, not many colors.** The hero card may carry a
> single `AccentBrush` cue (edge, value color, or subtle shadow); secondary/tertiary cards stay neutral
> `SurfaceBrush`. Do **not** introduce new hex — every value here is a token from UX-00.
> Keep semantic value colors (Danger/Warning/Success) available as a per-use `Foreground` override, as
> the views already do (e.g. a hero "Critical Stockout" value stays `DangerBrush`).

### 2. Responsive `FilterBar` container (the toolbar standard)

A reusable container that reflows instead of clipping, so UX-11 can replace fixed-`Grid` filter
toolbars with a single style.

```xml
<!-- A toolbar surface whose children wrap on narrow windows. -->
<Style x:Key="FilterBarStyle" TargetType="Border">
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}"/>
    <Setter Property="BorderBrush" Value="{DynamicResource SeparatorBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusSmall}"/>
    <Setter Property="Padding" Value="12,8"/>
</Style>

<!-- A WrapPanel item style for consistent inter-control spacing inside the bar. -->
<Style x:Key="FilterBarItemStyle" TargetType="FrameworkElement">
    <Setter Property="Margin" Value="0,0,12,4"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
</Style>
```

The intended UX-11 usage is a `Border Style="{StaticResource FilterBarStyle}"` wrapping a
`WrapPanel Orientation="Horizontal"`, with each label/input grouped in a small inner
`StackPanel Orientation="Horizontal"`. Document the canonical snippet in the summary so UX-11 applies
it identically everywhere.

### 3. Overflow standard (documentation deliverable)

No code here beyond the styles above — but the **standard** UX-11 will roll out is defined and recorded:

> **Overflow rule.** Any view whose content height can exceed the viewport (every dashboard, report,
> and settings form) wraps its scrolling body in
> `<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">`.
> Persistent chrome (toolbar, status bar, `BusyOverlay`) stays **outside** the `ScrollViewer` so it
> doesn't scroll away. Grids that bring their own virtualized scrolling are **not** double-wrapped.

Capture this verbatim in `wpf-vista-dashboard-layout.md` (see Output Requirements) so UX-11 cites one
authority.

### 4. Constraints

- **Behavior frozen** — additive styles only; no view, VM, binding, or command touched.
- **No new NuGet package.**
- **No inline hex** — every value is a token; the UX-04 sweep stays clean.
- **XAML/VB traps** — full root prefix on any new `clr-namespace` (MC3074); `<Setter>` targets a
  `DependencyProperty` only; never a brush token into a `Color` property.
- **Merge order** — if a new `Dashboard.xaml` dictionary is added, merge it **after** `Components.xaml`
  in `Application.xaml` so its styles may `BasedOn` the component styles.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — because no view consumes the styles yet, temporarily drop one
  `PrimaryMetricCardStyle` card and one `FilterBarStyle` bar onto a scratch view (or the existing
  Owner dashboard, reverted before commit) to confirm the keys resolve in both themes. The scratch
  usage must **not** be committed.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. All new keys are defined exactly once, keyed (not implicit), and resolve to tokens via
   `DynamicResource` (no literal hex).
3. The metric-hierarchy family expresses three visibly distinct weight tiers (hero / secondary /
   tertiary) using the UX-00 type scale.
4. `FilterBarStyle` + the documented `WrapPanel` usage reflow correctly when the window is narrowed
   (verified on the scratch harness, then reverted).
5. No view file, VM, or `Application.xaml` behavior is changed (only an optional dictionary merge).
6. The overflow standard text is recorded in the agent wiki for UX-11 to cite.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-09-summary.md` (template `Progress/_template.md`). Include: the
final key inventory (key → role → token spec), whether the styles went into `Components.xaml` or a new
`Dashboard.xaml` (+ merge slot) and why, the canonical `FilterBar` + `ScrollViewer` snippets UX-11 will
reuse, and confirmation the scratch realization harness was reverted.

### Documentation
Add `patterns/wpf-vista-dashboard-layout.md` documenting the metric-hierarchy vocabulary
(40-30-20-10 → which style), the responsive `FilterBar` pattern, and the overflow rule, per
`workflow-agent-wiki-update.md`. Update `agent_wiki/index.md` + `log.md`.
