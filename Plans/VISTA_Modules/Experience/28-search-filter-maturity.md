---
module: MerchSys.App
plan-id: UX-28
title: "Search & Filter UX Maturity — Result Count, Filter Chips, Clear-All, No-Results State"
depends-on: [UX-11, UX-16]
estimated-files: 8
---

# Search & Filter UX Maturity — Result Count, Filter Chips, Clear-All, No-Results State

> **Level 2 (Advanced) — item A5** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).

## Context

UX-11 made the per-view filter toolbars responsive (`WrapPanel` filter bars) and UX-16 added global
search, but per-view filtering is still bare: users can't see how much a filter narrowed results or
clear it in one move, and a filtered-empty list looks identical to a genuinely empty dataset. This
plan matures the list controls: a live **result count** ("12 of 48"), removable **filter chips**, a
**clear-all** affordance, a distinct **"no results for *X*"** empty state, and filters **remembered
per view within a session**.

This is **additive and read-only** — it surfaces and manages existing filter VM state; no query or
data-model change.

Read `UX-11` (the `WrapPanel` filter bars), `UX-16` (global search), and `UX-19`/B3
(`EmptyStatePanel` with the CTA) first.

## Prerequisites

- **UX-11** — the responsive filter toolbars on the target lists (`StockDashboardView`,
  `ProductManagementView`, `PurchaseOrderListView`, and peers). The filter VM state already exists.
- **UX-16** — global search (the search-term concept this builds the per-view count/chips around).
- **UX-19 (B3)** — `EmptyStatePanel` (now with optional CTA) reused for the differentiated
  "no results" state.
- Existing deps only. **No new NuGet.**

## Scope

- **A shared filter-bar header** (`Views/Shell/FilterSummaryBar.xaml` + `.xaml.vb`): a "{shown} of
  {total}" count, removable **chips** for each active filter/facet, and a **clear-all** control,
  bound to existing per-view filter state.
- **Per-view filter memory (session)**: remember the last-applied filter/search per view in the VM so
  navigating away and back restores it (in-memory, per session — not persisted to disk/DB).
- **Differentiated empty state**: when a filter yields zero rows, show "No results for *X* · Clear
  filters" (reusing `EmptyStatePanel` + clear-all as its CTA), visually distinct from the genuine
  "No data yet" empty state.
- **Adoption** on the highest-traffic filterable lists first.

> **Out of scope:**
> - Cross-session/persisted filters (persisting to `ui-settings.json`/DB is a later/Pro concern;
>   this remembers **within the session** only).
> - New filter *capabilities* (new facets/columns) — this matures the **presentation/management** of
>   the filters that already exist.
> - Server-side/paged search; the lists are small (~50 SKUs domain).

## Specification

### 0. Watch-items

1. **Count reflects filtered vs total honestly.** "{shown} of {total}" must use the unfiltered total
   and the post-filter count from the same source the grid binds — don't recompute on a stale copy.
   When no filter is active, either show the plain total or hide the "of" — be consistent across views.
2. **Chips mirror real filter state two-way.** Each chip represents one active filter/facet; removing
   a chip clears exactly that filter (and updates the count and grid). Clear-all resets every filter
   **and** the search term in one action. Chips must not drift from the actual VM filter state.
3. **"No results" ≠ "No data".** Reuse `EmptyStatePanel` but with different copy and an actionable
   **Clear filters** CTA for the filtered-empty case; the genuine empty dataset keeps its existing
   (B3) message/CTA. Pick the right one based on "is any filter active".
4. **Session memory is in-VM, not persisted.** Remember last filter/search in the ViewModel (or a
   lightweight session holder) — **do not** write filters to `ui-settings.json` or MariaDB. Restoring
   must re-apply cleanly without firing redundant loads.
5. **Tokens + theme.** Chips, count text, clear-all use `DynamicResource` tokens (chip background,
   separator, accent for removable affordance); recolor on theme toggle. No inline hex.
6. **Read-only / additive.** No write paths, no query-shape change; filtering remains client-side over
   already-loaded data. Owner filters read-only reports the same way (no role gating).
7. **VB traps** — reserved-keyword-safe names (avoid `filter` shadowing; avoid `List.Count(predicate)`
   binding to the property — use `Enumerable.Count(list, pred)`, BC32016); parameter names must not
   shadow same-named properties (case-insensitive, silent bug); `.` continuation at line-end; no
   `Await` in `Catch`/`Finally` (BC36943); full `clr-namespace` root prefix (MC3074).

### 1. The FilterSummaryBar

A header strip showing the result count, a `chips` `ItemsControl` of active filters (each with a
remove "×"), and a clear-all button (collapsed when nothing is active). Bound to existing filter VM
properties; emits chip-remove / clear-all back to the VM commands.

### 2. Per-view session memory + no-results state

Store the active filter/search in the VM so re-entry restores it. Drive the empty surface off "any
filter active": filtered-empty → "No results for *X*" + Clear-filters CTA; otherwise the existing
genuine-empty panel.

### 3. Constraints

- **Additive / read-only** — no data, query, or business change; client-side filtering only.
- **Session-scoped memory** — never persisted to disk/DB.
- **Reuse `EmptyStatePanel`** for the no-results state (don't author a parallel panel).
- **No new NuGet.**
- **Tokens + theme** — chips/count/clear-all recolor on toggle; no inline hex.
- **VB traps** — `Enumerable.Count(list, pred)`; no parameter/property shadowing; full root prefix.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — apply filters → count shows "{n} of {total}", chips appear; remove a chip →
  just that filter clears and count updates; clear-all → all filters + search reset; filter to zero
  rows → "No results for *X*" with Clear-filters CTA (distinct from genuine empty); navigate away and
  back → filter restored; toggle theme → chips recolor; Owner filters a report identically.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Filterable lists show a live "{shown} of {total}" count, removable filter chips, and a clear-all.
3. A filtered-empty result renders a distinct "No results for *X*" state (with Clear-filters CTA),
   different from the genuine "No data yet" empty state.
4. The active filter/search is remembered per view within the session (not persisted to disk/DB) and
   restores on re-entry.
5. No data/query/business change; chips recolor on theme toggle; Owner filters read-only reports the
   same way.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-28-summary.md` (template `Progress/_template.md`). Include: the
count source-of-truth, the chip↔filter-state binding approach, the session-memory mechanism (and proof
it isn't persisted), the no-results-vs-no-data discriminator, which lists adopted the bar, and the
realization results. Note `codebase_wiki` discrepancies (new `FilterSummaryBar`, per-VM session state).

### Documentation
Add `patterns/wpf-vista-filter-summary.md` (the count/chips/clear-all bar, the no-results-vs-empty
rule reusing `EmptyStatePanel`, the in-VM session-memory rule, the `Enumerable.Count` trap) per
`workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
