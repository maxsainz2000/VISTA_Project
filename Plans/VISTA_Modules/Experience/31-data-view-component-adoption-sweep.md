---
module: MerchSys.App
plan-id: UX-31
title: "Data-View Component Adoption Sweep — Finish FilterSummaryBar, FreshnessChip & SkeletonPanel Rollout"
depends-on: [UX-24, UX-26, UX-28, UX-35]
estimated-files: 20
---

# Data-View Component Adoption Sweep — Finish FilterSummaryBar, FreshnessChip & SkeletonPanel Rollout

> **Completion sweep** of shipped roadmap items **A1 (UX-24)**, **A3 (UX-26)** and **A5 (UX-28)** of
> [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md). Not a new roadmap slot — it closes the
> per-view coverage gaps recorded in `ux_review_report.md` §1.3, §1.4, §1.5, §2.4.

## Context

Three shared data-view components shipped but were only mounted on the highest-traffic views, leaving a
user-visible inconsistency the review flagged in its Cross-Cutting Consistency tables:

- **FilterSummaryBar (UX-28)** — adopted on 3 of 11 filterable views (StockDashboard,
  ProductManagement, PurchaseOrderList).
- **FreshnessChip (UX-24)** — mounted on 8 views; missing from several concurrency-sensitive lists.
- **SkeletonPanel (UX-26)** — on 7 views; the rest still show a bare spinner on first load.

This plan finishes the rollout to the remaining **eligible** views. It is **purely additive and
read-only** — it mounts existing components onto existing VM state. No new component, no query change,
no business logic, no write path.

> **§2.3 decision — RESOLVED (UX-35, 2026-06-05): Option B / B-minimal.** The presentation contracts
> (`IFreshnessAware`, `FilterChipItem`, `NotificationAction`, `ConfirmationRequest`,
> `IConfirmationPresenter`) **stay in `SharedKernel`** — all four module libraries consume them, so
> moving them to `MerchSys.App` would break the module-boundary rule. The convention is documented in
> `agent_wiki/patterns/wpf-vista-presentation-contracts.md`. **Folded into this plan:** the optional
> *B-tidy* relocation — grouping those five contracts under a `SharedKernel/Presentation/` namespace
> for visual separation from domain events/queries — is done here (see Scope item **D**), because this
> sweep already edits the same consumer ViewModels and can absorb the `Imports` change in one pass.

## Prerequisites

- **UX-24** — `FreshnessChip` + `IFreshnessAware` + `FreshnessTimer`, and the `RefreshCommand`/
  `LastLoadedAt` pattern proven on the 8 adopted views.
- **UX-26** — `SkeletonPanel` (`Kind` = Cards/Rows) and the first-load discriminator.
- **UX-28** — `FilterSummaryBar` (count/chips/clear-all) and the in-VM session-memory pattern.
- **UX-11** — the `WrapPanel` filter bars / filter controls on the target views (the state the
  FilterSummaryBar binds to).
- Existing deps only. **No new NuGet.**

## Scope

### FilterSummaryBar → 8 remaining filterable views (review §1.3)
`TransactionHistoryView`, `APLedgerView`, `VendorDirectoryView`, `ShrinkageView`,
`ExpiryMonitorView`, `TamperAuditReportView`, `CreditManagementView`, `ReorderSuggestionsView`.
Each needs the count source-of-truth (`{shown} of {total}`), an active-chips collection, and a
clear-all command wired to its existing filter VM state, plus the per-view session memory already
proven in UX-28.

### FreshnessChip → concurrency-sensitive data views (review §1.4, §2.4)
**Tier 1 (do):** `APLedgerView`, `VendorDirectoryView`, `CreditManagementView` — multi-user,
financially material. **Tier 2 (do, low effort):** `DailySummaryView`, `ShrinkageView`,
`ExpiryMonitorView`, `ReorderSuggestionsView`, `GoodsReceivingView`, `VendorCatalogView`. Each VM
implements `IFreshnessAware` (`LastLoadedAt` stamp + `RefreshCommand`); each view mounts the chip.

### SkeletonPanel → remaining list/dashboard views (review §1.5)
**Do (Rows):** `APLedgerView`, `VendorDirectoryView`, `ShrinkageView`, `ExpiryMonitorView`,
`CreditManagementView`, `ReorderSuggestionsView`, `VendorCatalogView`, `GoodsReceivingView`,
`DailySummaryView`. **Cards:** `SalesSummaryView`.

### D. Presentation-contract namespace tidy-up (folded from UX-35 / review §2.3)
Group the five presentation contracts under a dedicated namespace so they are visually distinct from the
domain events/queries in `SharedKernel`. Move `IFreshnessAware`, `FilterChipItem`, `NotificationAction`,
`ConfirmationRequest`, `IConfirmationPresenter` from `SharedKernel/Interfaces/` to
`SharedKernel/Presentation/`; change their `Namespace Interfaces` → `Namespace Presentation`; add
`Imports MerchSys.SharedKernel.Presentation` to every consumer (the module VMs this sweep already edits,
plus `INotificationService`, `DefaultNotificationService`, `DefaultConfirmationPresenter`,
`ConfirmationDialog.xaml.vb`, `Application.xaml.vb`, `OwnerDashboardViewModel`). **Pure structure/
namespace move — zero behavior change.** Do this as the **first** step so the new view/VM work in items
above adopts the final namespace. The decision and rationale are fixed (UX-35, Option B) and recorded in
`agent_wiki/patterns/wpf-vista-presentation-contracts.md` — do not re-open the location question; this is
the legibility tidy-up only. **Do not introduce any module → `MerchSys.App` reference.**

> **Out of scope:**
> - Accounting **report** views with complex fixed layouts (`IncomeStatement`, `VatReturn`,
>   `VatRelief`, `TamperAudit`) for **SkeletonPanel** only — content-shaped skeletons add little there;
>   they keep the spinner. (They still get FilterSummaryBar where they filter.) Record the rationale.
> - Any new filter capability, new query, paging, or persisted (cross-session) filters.
> - FreshnessChip on pure static/config screens with no shared data load.

## Specification

### 0. Watch-items

1. **Additive read-only only.** Every binding targets existing VM state or an additive read-only
   property/timer. No `DbContext`, MediatR contract, concurrency, or write-path change. Owner keeps
   read-only behavior — refresh/clear-all are reads, allowed for Owner.
2. **FilterSummaryBar count honesty.** `{shown} of {total}` uses the same source the grid binds; chips
   mirror real filter state two-way; clear-all resets every filter **and** the search term. Reuse the
   UX-28 `FilterSummaryBar` control verbatim — do not fork a parallel bar.
3. **Freshness stamping is single-sourced.** `LastLoadedAt` is stamped once per successful load (not on
   error, not per row). `RefreshCommand` re-runs the existing load and re-stamps. Reuse `FreshnessTimer`
   — do not start a second timer per view.
4. **Skeleton discriminator.** Skeleton shows only on **first** load (empty + busy), not on refresh of
   already-shown content. Pick `Kind` per the Scope (Rows for lists, Cards for summary tiles).
5. **Tokens + theme.** All three components already token-bind; confirm each newly-touched view recolors
   on theme toggle. No inline hex introduced.
6. **VB traps.** `Enumerable.Count(list, pred)` not `List.Count(predicate)` (BC32016); no parameter
   shadowing a same-named property (silent); `.` at line-end on fluent chains (BC30157); no `Await` in
   `Catch`/`Finally` (BC36943); full `clr-namespace` root prefix `MerchSys.App…` (MC3074); use the raw
   `MySqlConnector` reader for any new read (none expected — this is presentation-only).
7. **Namespace move (Scope D).** Do the contract relocation **first**, as one atomic step, then build
   before touching the view/VM work — keep it reviewable. New `Namespace Presentation` uses the relative
   suffix only, never `Namespace MerchSys.SharedKernel.Presentation` (BC30002 namespace doubling). The
   move must add **no** module → `MerchSys.App` reference. Decision is fixed (UX-35 / Option B) — do not
   re-open the location question.

### 1. Per-view work pattern

For each target view: (a) ensure the VM exposes the additive read-only properties the component needs
(`FilteredCount`/`TotalCount` + chips + `ClearFiltersCommand` for the bar; `LastLoadedAt` +
`RefreshCommand` for the chip), reusing the established patterns; (b) mount the shared control in the
view's header/load region; (c) confirm session-memory (filters) and first-load discriminator
(skeleton) behave as on the reference views.

### 2. Constraints

- Reuse the three shipped components unchanged; this plan adds **consumers**, not new shared controls.
- Additive read-only; no data/query/business change; client-side filtering only.
- Session-scoped filter memory; never persisted to disk/DB.
- No new NuGet. Tokens + theme; no inline hex.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — boot every touched view in **both** themes: filters show count + chips +
  clear-all and survive navigate-away/return; freshness chip stamps on load and updates on refresh;
  skeleton shows on first load only.

## Acceptance Criteria

1. `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` succeeds with **0 errors, 0 warnings**.
2. FilterSummaryBar is mounted on all 8 remaining filterable views with honest count, removable chips,
   clear-all, and session memory.
3. FreshnessChip is mounted on the Tier-1 and Tier-2 views; each stamps `LastLoadedAt` on load and
   refreshes via `RefreshCommand`.
4. SkeletonPanel renders first-load placeholders on the listed views with the correct `Kind`; excluded
   report views keep the spinner with the rationale recorded.
5. No data/query/business/write change; every touched view recolors on theme toggle; Owner retains
   read-only access.
6. The five presentation contracts live under `SharedKernel/Presentation/` (Scope D); all consumers
   compile against the new namespace; no module → `MerchSys.App` reference was introduced; runtime
   behavior of freshness/filters/undo/confirmation is unchanged.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-31-summary.md` (template `Progress/_template.md`). Include: the
final per-view adoption matrix for all three components (so it supersedes the review's gap tables), the
Scope-D relocation result (contracts now under `SharedKernel/Presentation/`; consumer list updated; no
module→App reference introduced), the spinner-retained report views with rationale, and the both-theme
realization results. Note `codebase_wiki` discrepancies (new component consumers; new additive VM
properties; relocated SharedKernel contracts) for Antigravity to sync.

### Documentation
No new agent-wiki pattern required (reuses UX-24/26/28 patterns and the UX-35
`wpf-vista-presentation-contracts` convention). When the Scope-D relocation lands, update
`patterns/wpf-vista-presentation-contracts.md` to note the contracts now resolve under
`SharedKernel/Presentation/`. Update `agent_wiki/log.md` only if a new trap is discovered.
