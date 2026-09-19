---
module: MerchSys.App
plan-id: UX-37
title: "Performance & Perceived Performance — Virtualization, Async Coverage & Optimistic UI"
depends-on: [UX-15, UX-14, UX-16]
estimated-files: 8
---

# Performance & Perceived Performance — Virtualization, Async Coverage & Optimistic UI

> **Level 3 (Pro) — item P2** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). Keeps the UI fluid as data
> grows (transaction history, audit logs, price history are unbounded) and makes writes *feel* instant
> while still surfacing conflicts. Builds on UX-15's async-load standard and UX-14's concurrency flow.

## Context

~50 SKUs is small today, but the lists that grow without bound — sales/transaction history, audit
logs, price history, AP/AR ledgers — will eventually make naïve `ItemsControl`s and synchronous loads
janky. Three levers close this:

- **UI virtualization** — long lists/grids should recycle containers (`VirtualizingStackPanel`,
  `VirtualizationMode=Recycling`) rather than realize every row.
- **Async coverage** — UX-15 standardised async load (`LoadDataAsync`, `IsBusy`); audit that *every*
  growable list loads off the UI thread, and that nothing blocks on a synchronous DB call.
- **Optimistic UI** — reflect a safe write immediately, reconcile on confirm, so the user doesn't wait
  on a round-trip — layered on the existing `DbUpdateConcurrencyException` flow (reconcile, never
  silently overwrite).

This is a **performance/perceived-performance** item. It must not change business rules, contracts, or
the concurrency *semantics* established in UX-14.

## Prerequisites

- **UX-15** — the three-state load model and async `LoadDataAsync`/`IsBusy` standard this extends.
- **UX-14** — `SharedKernel/Persistence/ConcurrencyHelper.vb` and the `DbUpdateConcurrencyException`
  reconcile path that optimistic UI must respect.
- **UX-16** — `CommandPaletteViewModel.OnQueryChanged` already debounces search at ~250 ms; reuse that
  debounce pattern where incremental filtering is added.
- Existing deps only. **No new NuGet.** Read paths use the raw `MySqlConnector` reader (EF Core 10
  `ToListAsync`-on-entity bug, per CLAUDE.md), never `ToListAsync` on a full entity query.

## Scope

### A. Virtualization audit + enablement
Identify every list/grid that can grow unbounded (transaction history, audit log, price history,
AP/AR ledgers, inventory batch lists) and confirm/enable container recycling: ensure an
`ItemsControl`/`DataGrid` virtualizing panel is active, `ScrollViewer.CanContentScroll=True`, and no
ancestor (e.g. an outer `ScrollViewer` wrapping the whole list) silently defeats virtualization.

### B. Async-coverage sweep
Audit the growable views against UX-15's pattern: every initial load and refresh runs through an async
`LoadDataAsync` with `IsBusy`, and no read path blocks the UI thread. Where incremental filtering
exists, debounce it with the UX-16 250 ms pattern. Read paths stay on the raw `MySqlConnector` reader.

### C. Optimistic-UI pilot (one safe write path)
Pick **one** low-risk, reversible write path and reflect the change in the VM immediately, then
reconcile against the server result: on success, keep; on `DbUpdateConcurrencyException`, roll the
optimistic change back and surface the existing UX-14 "data changed elsewhere — refresh and retry"
prompt. **Pilot only** — do not blanket-apply to financial postings.

> **Out of scope:**
> - Changing any business rule, MediatR contract, FIFO/concurrency *semantics*, or DB schema.
> - Blanket optimistic UI on financial/inventory-decrement write paths (the FIFO `FOR UPDATE` path
>   stays authoritative).
> - Adding a charting/paging library or server-side paging redesign.

## Specification

### 0. Watch-items

1. **No semantic change.** Virtualization, async, and the optimistic pilot must not alter what gets
   written or the concurrency contract — only *when/how* it's presented.
2. **Never silently overwrite.** The optimistic pilot reconciles on `DbUpdateConcurrencyException` via
   the UX-14 path; on conflict it rolls back the optimistic state and prompts. No last-writer-wins.
3. **Virtualization not defeated.** Watch for an outer `ScrollViewer`, `Height="Auto"` in an
   unconstrained parent, or grouping that forces full realization — these silently disable recycling.
4. **Raw reader for reads.** Any new/audited read path uses `MySqlConnector` + `reader.Read()`, never
   `ToListAsync` on an entity query (silent-empty bug).
5. **Don't regress selection/focus/motion.** Recycling must keep selection, keyboard focus (UX-17), and
   the UX-25 row transitions correct.
6. **VB traps** — no `Await` in `Catch`/`Finally` (BC36943) in the optimistic reconcile (capture error
   state, await after the block); reserved-keyword-safe names; `System.Console` if logging.

### 1. Constraints

- Reuse UX-15 async primitives and the UX-14 concurrency helper; add no parallel infrastructure.
- No new NuGet. **Role model** — Owner's large reports load faster too; no write affordances added for
  Owner.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both themes):** a deliberately long list scrolls smoothly without realizing every
  row; no UI freeze on load; the optimistic pilot reflects instantly and still surfaces a conflict
  correctly when one is forced.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Every unbounded list/grid recycles containers; an outer scroll/parent no longer defeats it.
3. All growable views load asynchronously with `IsBusy`; no synchronous DB call blocks the UI thread;
   incremental filters are debounced.
4. One safe write path reflects optimistically and reconciles via the UX-14 conflict flow (rollback +
   prompt on `DbUpdateConcurrencyException`).
5. No business-rule, contract, concurrency-semantics, or schema change; no selection/focus/motion
   regression.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-37-summary.md` (template `Progress/_template.md`). Include: the
lists audited and virtualization state of each, the async-coverage findings, the chosen optimistic-UI
pilot path and its reconcile/rollback proof, and both-theme realization (smooth scroll, instant write,
forced-conflict behaviour). Note `codebase_wiki` discrepancies for Antigravity.

### Documentation
Add `patterns/wpf-vista-performance.md` (virtualization checklist, the async-load + debounce reuse, the
optimistic-UI-with-reconcile recipe) per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md`
+ `log.md`.
