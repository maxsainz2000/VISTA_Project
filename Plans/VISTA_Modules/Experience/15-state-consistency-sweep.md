---
module: MerchSys.App
plan-id: UX-15
title: "State Consistency Sweep — Error State + Full Loading/Empty Coverage"
depends-on: [UX-06, UX-11]
estimated-files: 18
---

# State Consistency Sweep — Error State + Full Loading/Empty Coverage

## Context

UX-06 built two reusable state controls — `BusyOverlay` (bound to `IsBusy`) and `EmptyStatePanel`
(bound to a loaded-and-empty condition) — and rolled them across ~23 list/grid/report/dashboard views.
That covered "still loading" vs "loaded, no rows". It left a **third** state unhandled: **load
failure**. When a query throws, the current views either (a) leave the busy overlay off and show the
`EmptyStatePanel` — which *lies* ("no data" when the truth is "couldn't load"), or (b) surface only a
transient error toast and then sit blank, with no way to retry except re-navigating.

This plan finishes the state model so every data view distinguishes **loading / empty / error**, and
sweeps the remaining views UX-06 missed. It is **Tier 2** for the new control (an additive
`ErrorStatePanel`) and **Tier 1 / behavior-frozen** for the sweep (wiring existing flags, no logic
change).

Read `UX-06` (`BusyOverlay`/`EmptyStatePanel`), `[[wpf-vista-state-feedback]]`, and `UX-11`
(overflow/ScrollViewer rules — the new panel sits inside the same layouts) first.

## Prerequisites

- **UX-06** — `BusyOverlay`, `EmptyStatePanel`, `INotificationService`, and the `IsBusy`/`IsEmpty` flag
  convention exist and are proven.
- **UX-11** — the overflow/responsive standards (`ScrollViewer` body, persistent chrome outside it,
  `MinHeight` on fill children, no double-wrapped grids). The error panel must obey them.
- Existing deps only. **No new NuGet.**

## Scope

- **New control:** `Views/Shell/ErrorStatePanel.xaml` (+ minimal code-behind) — a tokenized
  icon + headline + message + **Retry** button, mirroring `EmptyStatePanel`'s structure. Retry binds to
  the view's existing load/refresh command (read-only, idempotent re-query).
- **State model:** introduce a consistent, mutually-exclusive trio on data VMs —
  `IsBusy`, `IsEmpty`, `IsError` (+ an `ErrorMessage` string) — so exactly one of busy / error / empty /
  content shows at a time.
- **Sweep:** audit **every** list/grid/report/dashboard view; ensure each has all three states wired,
  and add `BusyOverlay`/`EmptyStatePanel` to any data view UX-06 missed (operational work screens are
  the likely gaps — e.g. transaction history, vendor catalog, price history, income statement).

> **Out of scope:**
> - Changing what any query returns, or any business logic / totals / write path. This plan adds
>   *state presentation* around existing loads.
> - Re-theming or restructuring views beyond inserting the state panels and the three flags.
> - Replacing the toast feedback from UX-06 — toasts stay for *action* outcomes (save/delete). The
>   error **panel** is for *load* failure of the view's primary data. (A failed load may still also
>   toast; the panel is what stays on screen with Retry.)

## Deliverables

`ErrorStatePanel` (+ code-behind), the three-flag wiring on each data VM, the swept views, the
implementation summary, and a wiki update.

## Specification

### 0. Carry-forward watch-items

1. **`IsEmpty` must exclude both `IsBusy` and `IsError`.** The three states are mutually exclusive:
   `IsBusy` (loading) > `IsError` (load threw) > `IsEmpty` (loaded, count = 0) > content. A load that
   throws must set `IsError = True` and **not** leave `IsEmpty` true — that was the whole bug. Encode
   the precedence once (computed read-only props) so no view can show two states at once.
2. **Set the error state inside the existing `Try/Catch`, reset it on retry — and mind BC36943.** The
   load command's `Catch` captures the message and sets `IsError`; a successful (re)load clears it. Do
   not `Await` inside the `Catch`/`Finally` (`[[feedback-vbnet-await-catch]]`). Many of these VMs
   currently set a `StatusMessage = "Load failed: …"` in the catch and otherwise carry on — convert
   that to the `IsError`/`ErrorMessage` state.
3. **Load-error handling is currently inconsistent — some load methods have *no* `Catch` at all
   (verified in the UX-14 review).** Watch-item #2 assumes a `Catch` exists to hook into; it does not,
   uniformly. Today's `LoadDataAsync` paths range across three shapes: (a) catch into a `StatusMessage`
   string (e.g. `APLedgerViewModel`, `PurchaseOrderListViewModel` — "Load failed: …" / "[ERROR] …",
   invisible beyond a status line); (b) catch and swallow; and (c) **no `Catch` whatsoever** — only
   `Try … Finally IsBusy = False` — so a failed load throws **unhandled (crash)** rather than surfacing
   any state. `ProductManagementViewModel.LoadDataAsync` is the confirmed no-catch example. The sweep
   must therefore **normalize every load path** onto `IsBusy`/`IsError`/`ErrorMessage` — *adding* a
   `Catch` where one is missing, not merely re-pointing an existing one. Do not assume a view already
   degrades gracefully; audit each `LoadDataAsync`/load command individually.
4. **`IsError` is for *load* failure only — never for a write/concurrency conflict.** UX-14 routes
   `DbUpdateConcurrencyException` and domain write-errors through the `IConflictPresenter` prompt /
   toasts; those must **not** trip the error panel. Set `IsError` exclusively in the *read/load* path's
   catch. When sweeping a VM that UX-14 just touched, leave its write-path `Try`/primitive calls alone —
   only the load/refresh path gains the error state.
5. **Obey UX-11 inside the panels (this is the proven layout risk).** The error/empty panels overlay the
   *body* and must sit **inside** the same `ScrollViewer`/`Grid` structure UX-11 established — overlay
   as the last child of the root `Grid`, never docked, never double-wrapping a virtualizing `DataGrid`,
   and never collapsing a fill child to zero height (give it `MinHeight` if it shares a row). Do not
   disturb the UX-10 hero region or UX-11 chrome placement.
6. **Owner read-only is fine — Owner views still need loading/error/empty.** A read-only KPI view can
   still fail to load or be empty; wire all three. There is simply no write/validation/conflict UI on
   them.
7. **Retry is read-only and idempotent.** The Retry button re-invokes the existing
   load/`RefreshCommand` — it must not mutate anything and must be safe to press repeatedly.

### 1. The `ErrorStatePanel` control

Model it on `EmptyStatePanel` (UX-06): a `UserControl` in `Views/Shell/` with dependency properties
for `Title`, `Message`, and a `RetryCommand` (or expose the command via binding from the host view).
Tokenized warning icon (reuse `Themes/Icons.xaml`), `DangerBrush`/`WarningBrush` accent, surface card.
Full root prefix on `clr-namespace`/`x:Class` (MC3074). No inline hex.

### 2. The three-flag state model

On each data VM, ensure:
- `IsBusy` — set True before the first `Await`, reset in `Finally`.
- `IsError` (+ `ErrorMessage`) — set in the load `Catch`, cleared on a successful (re)load.
- `IsEmpty` — computed: `collection.Count = 0 AndAlso Not IsBusy AndAlso Not IsError`.

Bind the three panels' visibility to these. Where a VM already has only `IsBusy`/empty, add `IsError`.

### 3. The sweep

Audit every view that loads a collection or report (use `codebase_wiki` + the existing
`BusyOverlay`/`EmptyStatePanel` usage list as the starting set). For each: confirm all three states are
present and correct; add any missing panel. Work module-by-module (Inventory → Purchasing → POS →
Accounting → shell) and build 0/0 between modules. Provide a coverage table (view → busy/empty/error)
in the summary.

### 4. Constraints

- **Additive / behavior-frozen for data** — no query, total, business rule, write path, binding,
  command, or navigation changes beyond adding the state flags + panels.
- **Tokens only** — UX-00 tokens / UX-05 components; no inline hex; honors the live light/dark swap.
- **UX-11 layout rules** preserved (watch-item #3); UX-10 hero regions untouched.
- **VB/XAML traps** — no `Await` in `Catch`/`Finally` (BC36943); full root prefix on `clr-namespace`
  (MC3074); a `<Setter>` targets a `DependencyProperty` only; no brush token into a `Color` property.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — boot and force a load failure (e.g. point a client at an unreachable DB) to
  see the `ErrorStatePanel` + Retry on at least one view per module, in **both** themes.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A new `ErrorStatePanel` exists and shows on load failure with a working **Retry** that re-queries;
   it is never shown simultaneously with the busy or empty panels.
3. Every data view distinguishes loading / empty / error (coverage table provided); views UX-06 missed
   now have all three.
4. A failed load shows the error panel — **not** the empty panel — and Retry recovers when the cause is
   cleared. (Document the manual repro.)
5. No query/business-logic/write-path change; existing successful loads and empty states behave exactly
   as before; UX-10/UX-11 layouts intact (hex sweep clean).
6. Owner read-only views show loading/empty/error but no write/validation UI; theme toggle recolors all
   new UI.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-15-summary.md` (template `Progress/_template.md`). Include: the
state-precedence rule as implemented, the per-view coverage table (busy/empty/error), the views newly
covered vs already-covered, the load-failure manual repro + result, and `codebase_wiki` discrepancies
(the new `ErrorStatePanel`).

### Documentation
Update `patterns/wpf-vista-state-feedback.md` with the three-state model (busy/empty/error precedence)
and the `ErrorStatePanel` + Retry recipe, per `workflow-agent-wiki-update.md`; update
`agent_wiki/index.md` + `log.md`.
