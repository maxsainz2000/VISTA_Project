---
module: MerchSys.App
plan-id: UX-24
title: "Data Freshness & Manual Refresh — 'Updated Nm ago · ↻' Header Chip"
depends-on: [UX-14, UX-15]
estimated-files: 10
---

# Data Freshness & Manual Refresh — "Updated Nm ago · ↻" Header Chip

> **Level 2 (Advanced) — item A1** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).
> First item of the Advanced tier. Master map governs ordering.

## Context

On 4 client laptops against one MariaDB instance, the list on screen can be minutes old because
another client changed the underlying rows. UX-14 handles the **write-collision moment** (the
`DbUpdateConcurrencyException` → "Data changed elsewhere — refresh and retry" path); nothing yet
tells a user their data is **stale before they act**. This plan adds the read-side half: a small
**"Updated 3m ago · ↻ Refresh"** chip in each data view's header, driven off the load timestamp, that
softens to a `WarningBrush` tone past a staleness threshold and re-runs the existing load command.

This is **read-only and additive** — a header chip plus one timestamp property per view. No new data
plumbing, no query change.

Read `UX-15` (the `IsBusy/IsError/IsEmpty` three-state load model and the `RetryCommand`/load paths)
and `[[wpf-vista-state-feedback]]` first.

## Prerequisites

- **UX-14** — concurrency-conflict consolidation (the write-collision UX this complements).
- **UX-15** — the standardised three-state load surface. **Grounding caveat:** UX-15 is a
  *convention*, not a base class — `IsBusy` and the load/`RetryCommand` members are declared
  **independently in each module ViewModel** (≈20 VMs: `StockDashboardViewModel`,
  `PurchaseOrderListViewModel`, `APLedgerViewModel`, `SalesSummaryViewModel`, etc.). There is no
  shared `ViewModelBase` to extend. Plan accordingly (see watch-item #1).
- Existing deps only. **No new NuGet.**

## Scope

- **A `FreshnessChip` shell component** (`Views/Shell/FreshnessChip.xaml` + `.xaml.vb`): renders
  "Updated {relative} · ↻" where `{relative}` is a live "Nm ago" string; exposes a `LastLoadedAt`
  (`DateTime?`) dependency property and a `RefreshCommand` (`ICommand`) DP; recolors to a warning
  tone past the staleness threshold.
- **A `RelativeTimeConverter`** (`Converters/`) turning a `DateTime?` into "just now / Nm ago / Nh
  ago"; the chip re-evaluates on a shared low-frequency `DispatcherTimer` (one tick ≈ 30–60 s) so the
  label ages without a reload.
- **A `LastLoadedAt` property per data ViewModel**, stamped at the end of each successful load, plus
  reuse of the existing refresh/load command as the chip's `RefreshCommand`.
- **Mount the chip in each data view header**, bound to that view's `LastLoadedAt` + load command.

> **Out of scope:**
> - Auto-refresh / polling / push invalidation — this is **manual** refresh only (auto-refresh is a
>   later concern and would fight the optimistic-concurrency model).
> - Real-time "another client changed this row" signalling — that is the write-time path UX-14 owns.
> - Per-row freshness or diff highlighting.

## Specification

### 0. Watch-items

1. **No shared base ViewModel — add `LastLoadedAt` per VM.** Do not invent a base class refactor;
   that is out of scope and risks every module. Add a `LastLoadedAt As DateTime?` (or
   `ObservableProperty`) to each data VM and set it at the **end of the successful load** (after data
   is assigned, not on entry). If a thin shared **interface** (`IFreshnessAware`) reduces repetition
   in the view binding, it may be introduced in `SharedKernel` — but it must be additive and must not
   force a base class.
2. **Stamp `LastLoadedAt` only on success.** A failed load (`IsError`) must not advance the
   timestamp — the chip should reflect the age of the data actually shown, so a failed refresh keeps
   the old (now older) timestamp.
3. **Tokens only — no inline hex.** The warning tone uses `{DynamicResource WarningBrush}`; the
   normal tone uses the existing secondary-text token. The chip must recolor on theme toggle.
4. **One shared timer, not one per chip.** A `DispatcherTimer` per mounted chip is wasteful. Use a
   single shared ticking source (a static/`DispatcherTimer` in the converter host or a shared
   "clock" the chip subscribes to) so relative labels age in lockstep. Stop/dispose correctly to
   avoid a leak when a view unloads.
5. **`RefreshCommand` reuses the existing command — no new load logic.** The chip's `↻` binds to the
   VM's existing load/`RetryCommand`; it must respect `IsBusy` (disabled mid-load) and must not
   double-invoke. No new query, no new MediatR contract.
6. **Owner sees the same chip.** Owner is read-only but watches live KPIs — the chip is arguably more
   valuable there. No role gating.
7. **VB traps** — reserved-keyword-safe names (`now`/`date` are reserved → use `nowUtc`,
   `loadedAt`); fluent/`.` continuation at line-end; no `Await` in `Catch`/`Finally` (BC36943) when
   stamping inside the load's `Try`.

### 1. The FreshnessChip component

A compact header element: `↻` glyph (reuse the UX-07 icon set) + relative-time text. Bound DPs:
`LastLoadedAt` and `RefreshCommand`. A `DataTrigger`/style switches the foreground to `WarningBrush`
when the age exceeds the staleness threshold (default ≈ 5 min — expose as a styleable constant). The
chip is hidden (collapsed) when `LastLoadedAt Is Nothing` (never loaded).

### 2. ViewModel wiring

Each data VM gains `LastLoadedAt` set at the tail of its successful load. The view binds the chip's
`LastLoadedAt` to it and `RefreshCommand` to the existing load/refresh command. Mount the chip in the
view header row (alongside the title), in the highest-traffic views first: the dashboards
(`StockDashboardView`, `PurchasingDashboardView`, `OwnerDashboardView`, Accounting overviews) and the
major lists (`PurchaseOrderListView`, `ProductManagementView`, `TransactionHistoryView`).

### 3. Constraints

- **Read-only / additive** — no write paths, no query changes, no business logic change.
- **Reuse the existing load command** — do not author a second refresh path.
- **No new NuGet.**
- **Tokens + theme** — `WarningBrush`/secondary-text via `DynamicResource`; recolors on toggle.
- **VB traps** — reserved-keyword-safe names; no `Await` in `Catch`/`Finally` (BC36943).
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — open a view → chip shows "Updated just now"; wait → label ages without a
  reload; click `↻` → label resets and data reloads; let it cross the threshold → chip softens to
  the warning tone; toggle theme → chip recolors; confirm a *failed* refresh does not reset the
  timestamp.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Every high-traffic data view shows a header chip with a live "Updated Nm ago" label and a one-click
   `↻` that re-runs the existing load command.
3. The relative label ages over time without a reload (shared timer), and softens to `WarningBrush`
   past the staleness threshold.
4. `LastLoadedAt` advances only on successful load; a failed refresh leaves the prior timestamp.
5. No data/query/business change; the chip recolors on theme toggle; Owner sees it identically.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-24-summary.md` (template `Progress/_template.md`). Include: the
per-VM `LastLoadedAt` approach (and whether an `IFreshnessAware` interface was introduced), the shared
timer mechanism and how leaks are avoided, the staleness-threshold value and warning-tone tokens, the
list of views wired, and the realization results (age-without-reload, refresh-resets, threshold tone,
failed-refresh-keeps-timestamp). Note `codebase_wiki` discrepancies (the new chip, converter, and the
per-VM property additions).

### Documentation
Add `patterns/wpf-vista-freshness-chip.md` (the read-side complement to the concurrency story: per-VM
`LastLoadedAt`, the shared relative-time timer, the staleness token rule, reuse-the-load-command rule)
per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
