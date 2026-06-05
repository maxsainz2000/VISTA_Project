---
module: MerchSys.App
plan-id: UX-26
title: "Skeleton Loaders — Content-Shaped First-Load Placeholders"
depends-on: [UX-06]
estimated-files: 7
---

# Skeleton Loaders — Content-Shaped First-Load Placeholders

> **Level 2 (Advanced) — item A3** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).

## Context

First load currently shows a spinner-only affordance (`Views/Shell/BusyOverlay`, UX-06) bound to the
`IsBusy` flag standardised in UX-15. A blank panel with a spinner reads slower than the screen
"looking like itself" immediately. This plan adds **content-shaped skeleton placeholders** — greyed
card and row silhouettes with a gentle shimmer — for the **first load** of the dashboards and the
largest lists. Skeletons reduce *perceived* latency and prevent the layout jump when data arrives.

This is **cosmetic only** and **opt-in per view** — a view that isn't worth authoring a skeleton for
keeps the existing `BusyOverlay`. No data/logic change.

Read `UX-06` (`BusyOverlay`, `INotificationService`, the first-load affordances) and
`[[wpf-vista-state-feedback]]` first.

## Prerequisites

- **UX-06** — the `IsBusy`-bound `BusyOverlay` first-load surface this offers an alternate visual for.
  **Grounding caveat:** `IsBusy` is declared **per ViewModel** (no shared base class); the skeleton
  binds to the same per-VM flag the overlay already uses.
- Builds naturally alongside **A2 (UX-25)** motion tokens if present (shimmer easing), but does not
  depend on it. **No new NuGet.**

## Scope

- **A `SkeletonPanel` component** (`Views/Shell/SkeletonPanel.xaml` + `.xaml.vb`): a reusable greyed
  silhouette host with a subtle horizontal shimmer, visible while `IsBusy` is true on first load.
- **A small set of skeleton shapes**: a "card" silhouette (for dashboard tiles) and a "table row"
  silhouette (for list/grid first load), composed from tokenized rounded rectangles.
- **Opt-in placement** on the highest-traffic screens: the dashboards (`StockDashboardView`,
  `PurchasingDashboardView`, `OwnerDashboardView`, Accounting overviews) and the largest lists
  (`PurchaseOrderListView`, `ProductManagementView`, `TransactionHistoryView`). Other views keep
  `BusyOverlay`.

> **Out of scope:**
> - Replacing `BusyOverlay` everywhere — skeletons are opt-in for the views that benefit most.
> - Skeletons for in-place **refresh** (A1/UX-24) — skeletons are for **first** load (empty → data);
>   a refresh of already-shown data keeps the lighter busy affordance so content doesn't blank out.
> - Per-field/realistic data placeholders.

## Specification

### 0. Watch-items

1. **First load only — don't blank existing content on refresh.** Show the skeleton when `IsBusy` is
   true **and** there is no data yet (first load). A refresh that already has rows on screen must not
   swap them for a skeleton — distinguish "first load" from "reloading" (e.g. a `HasLoadedOnce` flag
   or `LastLoadedAt Is Nothing` from UX-24 if present). Otherwise the screen flashes empty each refresh.
2. **Shimmer is subtle and tokenized.** The shimmer uses theme brushes (`{DynamicResource ...}`) — no
   inline hex — so it works in Light and Dark. Keep it slow/soft; reuse the A2 motion duration/easing
   tokens if available, otherwise a single local duration resource. Respect reduced-motion (static
   greys, no shimmer) using the same gate as A2.
3. **Match the real layout's shape.** The skeleton silhouette should roughly match the arrived
   content's geometry (tile grid → card skeletons; list → row skeletons) so there's **no layout jump**
   when real data replaces it.
4. **Don't fight virtualization.** A list skeleton renders a small fixed count of row silhouettes (not
   thousands); it must not instantiate per-data-item or interfere with the real grid's virtualization
   once data arrives.
5. **Opt-in, backward-compatible.** Views that don't adopt a skeleton render exactly as today with
   `BusyOverlay`. The component must be droppable without touching VM logic.
6. **Owner sees the same.** No role distinction in first-load presentation.
7. **VB traps** — code-behind (if any) uses reserved-keyword-safe names; `.` continuation at line-end;
   no `Await` in `Catch`/`Finally` (BC36943).

### 1. The SkeletonPanel + shapes

`SkeletonPanel` exposes enough configuration (e.g. a "kind": cards vs rows, and a row/card count) to
shape the silhouette. Shapes are tokenized rounded rectangles with a shimmering overlay brush. A
single style controls the shimmer animation and honours the reduced-motion gate.

### 2. Placement

Bind each adopted view's skeleton visibility to `IsBusy AndAlso (first-load)`. Place it in the same
content region the real data fills so the transition is in-place. Dashboards use card skeletons; lists
use row skeletons.

### 3. Constraints

- **Cosmetic / opt-in / additive** — no VM logic, data, or business change.
- **First-load only** — never blanks already-shown content on refresh.
- **No new NuGet.**
- **Tokens + theme + reduced-motion** — shimmer via resources, static fallback when motion disabled.
- **No virtualization regression.**
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — open a dashboard/list cold → shaped skeleton with subtle shimmer, then data
  replaces it with no layout jump; trigger a refresh with data already shown → no blank/skeleton flash;
  toggle theme → skeleton recolors; enable reduced-motion → static greys, no shimmer; a non-adopted
  view → still shows `BusyOverlay`.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Dashboards and major lists show content-shaped skeletons on **first** load; no blank-with-spinner
   flash on those screens.
3. Skeleton geometry matches the arrived content (no layout jump on data arrival).
4. Refresh of already-shown data does **not** blank content into a skeleton; non-adopted views keep
   `BusyOverlay`.
5. Shimmer is tokenized/theme-correct and respects reduced-motion; no virtualization regression;
   Owner sees it identically.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-26-summary.md` (template `Progress/_template.md`). Include: the
first-load-vs-refresh discriminator used, the skeleton shapes authored and which views adopted them,
the shimmer/reduced-motion handling, and the realization results (no-jump, no refresh-flash,
theme/reduced-motion). Note `codebase_wiki` discrepancies (new `SkeletonPanel` + per-view adoption).

### Documentation
Add `patterns/wpf-vista-skeleton-loaders.md` (the first-load-only rule, the shape-match-to-avoid-jump
rule, the tokenized shimmer + reduced-motion gate, opt-in coexistence with `BusyOverlay`) per
`workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
