---
module: MerchSys.App
plan-id: UX-13
title: "Purchasing Dashboard — Module-Parity Overview View"
depends-on: [UX-12]
estimated-files: 6
---

# Purchasing Dashboard — Module-Parity Overview View

## Context

The UX-08 audit's defect #7 (**module asymmetry**): Inventory has `StockDashboardView`, Accounting has
`FinancialOverviewView`, POS has `DailySummaryView` — but **Purchasing has no dashboard.** Its module
panel (`PurchasingPanel`) is a nav list, and every Purchasing view is an *operational work screen* (PO
list, goods receiving, AP ledger, reorder worklist, vendor directory/catalog). The only place
Purchasing KPIs surface is a single card on the Owner dashboard. Graded as a dashboard, the de-facto
landing (`PurchaseOrderListView`) scored 46 — not because it's badly built, but because it was never
meant to be an overview.

This plan adds the missing **`PurchasingDashboardView`** so Purchasing reaches parity with the other
three modules. It is **Tier 2**: a new view + ViewModel + **read-only aggregation**. Critically, most
of the data is **already computed elsewhere** (the Owner dashboard's Purchasing card, `APLedgerView`,
`ReorderSuggestionsView`, `GoodsReceivingView`, `VendorDirectoryView`) — so the dashboard primarily
**aggregates existing read queries** rather than inventing new business logic.

It is built on the full sub-epic so it is born correct: UX-09 card hierarchy, UX-10 composition rules,
UX-11 overflow/responsive standards, UX-12 delta/sparkline indicators.

Read `UX-08` (standards), `UX-09`–`UX-12`, and the existing dashboards (`StockDashboardView`,
`FinancialOverviewView`) as the structural template first.

## Prerequisites

- **UX-09..UX-12** — the metric-hierarchy cards, `FilterBar`/overflow standards, and delta/sparkline
  controls all exist and are proven.
- The existing Purchasing services / MediatR query contracts and the `PurchasingDbContext` (verify
  exact names; reuse what already powers the Owner Purchasing card, AP ledger, and reorder views).
- The shell navigation wiring: `PurchasingPanel`, the `NavigationItem` model, `NavigateCommand`, and DI
  registration in `Application.xaml.vb` (verify the existing pattern used by the other dashboards).

## Scope

- **New view:** `Views/Purchasing/PurchasingDashboardView.xaml` (+ minimal `.xaml.vb` code-behind,
  following the existing dashboard views — `DataContext` injected, no logic in code-behind).
- **New ViewModel:** `PurchasingDashboardViewModel` in **`MerchSys.Purchasing/ViewModels/`**
  (CommunityToolkit.Mvvm), exposing **read-only** dashboard properties.
- **Aggregation:** reuse existing Purchasing read queries/services; add **read-only** query contracts
  only where a needed roll-up does not already exist.
- **DI + navigation:** register the VM (and view) in `Application.xaml.vb`; add the dashboard to
  `PurchasingPanel`'s nav items so it is reachable (matching how the other module dashboards register).

> **Out of scope:**
> - Any **write** action. The dashboard is read-only/overview; create/edit/receive/pay actions stay in
>   the existing operational views. Cards may **navigate** to those views (reusing the existing
>   `NavigateCommand`), but perform no mutations.
> - New business metrics not derivable from existing data — only aggregate/derive what services already
>   produce (or can produce via a straightforward read-only query).
> - Changing any existing Purchasing view, VM, service, or write path.

## Deliverables

`PurchasingDashboardView.xaml` (+ code-behind), `PurchasingDashboardViewModel`, any new read-only query
contract(s)/handler(s), the DI + nav registration, the implementation summary, and a wiki update.

## Specification

### 0. Carry-forward watch-items (from the UX-12 review)

1. **The AP delta on the hero is the proven risk — UX-12 already hit this wall.** UX-12 *skipped* the
   AP/AR/overdue deltas because the existing services expose **no date-parameter support for
   prior-period history**. This dashboard's hero is **Total Outstanding AP with a `DeltaIndicator
   InvertSemantics=True`**, so it faces the same gap for its headline metric. Resolve it one of two
   ways — do not invent a write path either way:
   - **Add a new read-only query** for last-period AP outstanding. This is a genuinely new read, so it
     **must** use the raw `MySqlConnector.MySqlConnection` reader loop (**not** `ToListAsync` on an
     entity query — EF Core 10 VB.NET empty-list bug), with no `Await` inside `Catch`/`Finally`
     (BC36943); **or**
   - **Render the AP hero without a delta** and document the omission (consistent with how UX-12
     handled the same metric). The hero value itself still binds an existing figure — only the delta
     is in question.
2. **This is the first real consumer of `InvertSemantics=True`.** UX-12 built and unit-wired the
   inverted path but never exercised it (all UX-12 deltas were revenue / up-is-good). Visually verify
   in **both themes** that a **rising** AP balance renders the delta **red (`DangerBrush`)**, not
   green — and likewise any other rising-is-bad metric (Overdue AP) you decorate.
3. **Nav/DI is the only non-additive surface — match the existing pattern.** Registering the view in
   `Application.xaml.vb` and adding the nav entry to `PurchasingPanel`/the `NavigationItem` registry is
   the one place this plan touches shell code. Read how the other module dashboards register **before**
   editing; do not invent a new navigation scheme. Everything else in this plan is purely additive.

### 1. Dashboard content (KPI band → trend/insight → detail, per UX-08)

Compose with UX-09 hierarchy and UX-10 rules. Suggested metrics (all sourced from existing Purchasing
data — confirm availability; skip and document any that isn't cheaply available):

- **Hero (primary):** Total Outstanding AP (₱) — reuse the figure `APLedgerView` already computes;
  attach a UX-12 `DeltaIndicator` with `InvertSemantics=True` (rising AP is not "good").
- **Secondary (2–3):** Open POs (count by status — Draft/Submitted/Received), Overdue AP (count/₱,
  `DangerBrush`), Pending Deliveries.
- **Tertiary strip:** Active Vendors, Reorder Suggestions pending, Avg lead time.
- **Trend/insight band:** a "What This Means" plain-language panel (reuse the Accounting pattern) +
  optionally a small PO-aging or monthly-spend sparkline if a series is cheaply available.
- **Detail band:** a compact read-only grid — e.g. **PO aging** (Submitted POs past expected delivery)
  or **top vendors by spend** — each row optionally navigating to the relevant operational view.

> Every figure must trace to an existing query/service or a new **read-only** query. If the ideal
> metric isn't available without new write-side work, **omit it and note it** — do not add a write path.

### 2. ViewModel (read-only, module-resident)

`PurchasingDashboardViewModel` (in `MerchSys.Purchasing/ViewModels/`):

- `ObservableObject`; read-only observable properties for each metric + collections for the detail
  grid; a `RefreshCommand` (read-only reload) and `IsBusy`/`LastRefreshed` like the other dashboards.
- Loads via the existing Purchasing services / MediatR **query** contracts; cross-module reads (e.g.
  any inventory figure) use a MediatR query, never a direct cross-module service call.
- **No write/save/concurrency logic.**

### 3. New read-only query contracts (only if needed)

If a roll-up isn't already exposed, add a **read-only** query contract in `SharedKernel` (mirroring
`GetCurrentStockQuery`) + a handler in `MerchSys.Purchasing`. The handler's read path uses the raw
`MySqlConnector.MySqlConnection` reader loop (not `ToListAsync` on entity queries — EF Core 10 VB.NET
empty-list bug). Prefer reusing an existing query before adding one.

### 4. Navigation + DI

- Register `PurchasingDashboardViewModel` (and the view) in `Application.xaml.vb` using the **same DI
  pattern** the other dashboards use (verify before editing).
- Add a nav entry to `PurchasingPanel`'s items / the navigation registry so the dashboard appears in
  the Purchasing module and is selectable — ideally as the module's **default/first** view, matching
  how Inventory/POS/Accounting present their dashboard first. Confirm the existing registration
  mechanism; do not invent a new navigation scheme.

### 5. Constraints

- **Read-only / additive** — no existing Purchasing view, VM, service, write path, or business rule is
  modified. New code is read-only.
- **Architecture** — VM in the module library; Views in `MerchSys.App`; cross-module data via MediatR
  query contracts only; DI via `Application.xaml.vb`. Respects the modular-monolith boundaries.
- **Reuses the sub-epic** — UX-09 cards, UX-10 hero/40-30-20-10, UX-11 `ScrollViewer` + responsive
  filter, UX-12 delta/sparkline. No bespoke re-implementation of those.
- **No new NuGet package.**
- **No inline hex** — tokens only; UX-04 sweep stays clean.
- **VB/EF/XAML traps** — raw `MySqlConnector` reader for new reads (no `ToListAsync`); no `Await` in
  `Catch`/`Finally`; full root prefix on `clr-namespace` (MC3074, the new view's `x:Class`/`xmlns`);
  `<Setter>` → `DependencyProperty` only; no brush token into a `Color` property; Manager/Owner role
  enforcement consistent with the other read-only dashboards (Owner = read-only).
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — boot, navigate to the new Purchasing dashboard in **both** themes; confirm it
  loads, the hero/delta render, cards navigate (read-only), and `RefreshCommand` reloads.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A new `PurchasingDashboardView` exists, is reachable from the Purchasing module's navigation, and
   follows the KPI-band → trend/insight → detail structure with a single hero (Outstanding AP) and
   UX-09 hierarchy.
3. Every displayed figure traces to an existing query/service or a new **read-only** query; any
   intended metric omitted for lack of data is documented.
4. The dashboard is **read-only** — no create/edit/receive/pay; navigation-only links to operational
   views work.
5. Architecture respected: VM in `MerchSys.Purchasing`, cross-module reads via MediatR queries, DI in
   `Application.xaml.vb`; no existing Purchasing view/VM/service/write path changed.
6. Uses UX-09 cards, UX-11 overflow/responsive standards, and UX-12 deltas; **hex sweep clean**; theme
   toggle recolors it live.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-13-summary.md` (template `Progress/_template.md`). Include: the
final metric set and each figure's source (reused query vs new read-only query), any metric omitted
(and why), the navigation/DI registration touchpoints, confirmation it is read-only, and `codebase_wiki`
discrepancies (the new view/VM the wiki will pick up on next sync).

### Documentation
Add `patterns/wpf-vista-purchasing-dashboard.md` (the aggregate-existing-reads approach, the read-only
overview pattern, nav/DI registration steps), per `workflow-agent-wiki-update.md`. Update
`agent_wiki/index.md` + `log.md`.
