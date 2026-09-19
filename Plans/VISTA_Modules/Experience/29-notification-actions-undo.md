---
module: MerchSys.App
plan-id: UX-29
title: "Notification Actions & Undo — Toast Action Buttons + Soft-Delete Undo Window"
depends-on: [UX-06]
estimated-files: 7
---

# Notification Actions & Undo — Toast Action Buttons + Soft-Delete Undo Window

> **Level 2 (Advanced) — item A6** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).

## Context

`INotificationService` (UX-06) raises success/error/info/warning toasts but they're inert — no
actions. Meanwhile the schema's `IsDeleted` soft-delete means a delete/void can be **reversed by
clearing the flag**: the data substrate for undo already exists. This plan adds **action buttons to
toasts** (e.g. "View", "Undo") and a short **undo window** for reversible operations — most importantly
soft-deletes and voids. Undo is the safety net that lets users move fast without fear, pairing with
B4's destructive-action confirms (confirm the dangerous, undo the routine).

This is **additive** — it extends the notification contract and wires undo onto existing soft-delete/
void commands; it does not change what those commands do on the happy path.

Read `UX-06` (`INotificationService` in `SharedKernel`, `DefaultNotificationService` in the App,
`Notification.Wpf` usage), `UX-20`/B4 (confirmation presenter), and the relevant module command paths
first.

## Prerequisites

- **UX-06** — `INotificationService` (`MerchSys.SharedKernel/Interfaces/INotificationService.vb`) and
  `DefaultNotificationService` (`MerchSys.App/Services/DefaultNotificationService.vb`), backed by
  `Notification.Wpf`.
- Soft-delete/void commands across modules that set `IsDeleted = True` (Inventory products, POS voids,
  Purchasing records) — the reversible operations to wire.
- **Architecture:** undo reversal is a **data write** → it must go through the owning module's
  command/repository path (parameterized, audit columns updated, MariaDB), **not** a shortcut from the
  App layer. Cross-module signalling stays on **MediatR**. **No new NuGet.**

## Scope

- **Extend the notification contract** (`INotificationService`, SharedKernel) with an **optional**
  action: a label + callback (and optionally a separate undo concept), in a way that is
  backward-compatible — existing `ShowSuccess/Error/Info/Warning` calls keep working unchanged.
- **Implement the action toast** in `DefaultNotificationService` using `Notification.Wpf`'s action/
  button support: render the action button, invoke the callback, auto-dismiss after the window.
- **Wire undo on routine reversible commands**: after a soft-delete/void succeeds, raise an
  "Undo"/"View" toast; Undo reverses within a time window by clearing `IsDeleted` **through the owning
  module's command path** (audit-logged), then confirms.

> **Out of scope:**
> - Undo for hard deletes or irreversible financial postings — only `IsDeleted`-reversible operations.
> - A global multi-step undo stack / history — this is a **single, time-boxed** undo per action.
> - Cross-client undo (undoing another laptop's action) — undo applies to the action you just took,
>   within your session's window.

## Specification

### 0. Watch-items

1. **Contract change must stay backward-compatible.** Add an **overload / optional parameter** (e.g.
   `ShowSuccess(message, action As NotificationAction)` where `NotificationAction` carries label +
   `Action` callback), not a breaking signature change. Every existing call site must compile and
   behave unchanged. Keep the new type in **SharedKernel** so all modules can pass an action.
2. **Undo reversal goes through the module command path — not a raw write from the App.** Clearing
   `IsDeleted` is a real mutation: route it through the owning module's repository/command (EF Core
   parameterized, `ModifiedBy/ModifiedAt` audit columns set, optimistic-concurrency token respected).
   Surface `DbUpdateConcurrencyException` as "couldn't undo — data changed elsewhere" rather than
   silently overwriting (consistent with the centralized-DB concurrency mandate).
3. **Time-boxed and idempotent.** The undo is valid only within the window (e.g. ~5–8 s, matching the
   toast lifetime); after it expires or after one successful undo, the callback is inert. Undoing
   twice must not double-restore.
4. **No `Await` in `Catch`/`Finally`.** The undo callback is likely async (DB write). Capture any
   error state inside the `Try`, then surface it **after** the block (BC36943). Keep the callback's
   structure await-safe.
5. **Owner has no reversible writes.** Owner is read-only → never triggers undoable actions; toasts
   remain informational for Owner. No action button appears where the role can't perform the action.
6. **Cross-module stays on MediatR.** If an undo must inform other modules (e.g. inventory restored →
   accounting), use the existing event contracts — no cross-module service calls or direct refs.
7. **Tokens + theme** for any custom toast chrome (if the action button is styled beyond
   `Notification.Wpf` defaults) — `DynamicResource`, no inline hex.
8. **VB traps** — reserved-keyword-safe names; lambda params must not shadow locals (BC36641); `.`
   continuation at line-end; no `Await` in `Catch`/`Finally` (BC36943).

### 1. Contract + service

Define a small `NotificationAction` (label + callback) in SharedKernel; add optional-action overloads
to `INotificationService`. `DefaultNotificationService` renders the button via `Notification.Wpf` and
invokes the callback, dismissing the toast on action or timeout.

### 2. Undo wiring on reversible commands

For each soft-delete/void command, on success raise the action toast whose Undo callback re-opens the
module command to clear `IsDeleted` (audit-logged, concurrency-checked) and then confirms with a
follow-up success toast. "View" actions (where useful) deep-link/navigate to the relevant screen.

### 3. Constraints

- **Additive / backward-compatible contract** — existing toast calls unchanged.
- **Undo = clear `IsDeleted` via the owning module command** — parameterized, audited,
  concurrency-safe; never a raw App-layer write.
- **Time-boxed, idempotent single undo.**
- **Cross-module via MediatR only.**
- **No new NuGet.**
- **VB traps** — no `Await` in `Catch`/`Finally`; no lambda/local shadowing.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — soft-delete/void an item → "Undone? · Undo" toast appears; click Undo within
  the window → item restored (and visible again), confirmed by a follow-up toast; let the toast expire
  then there's no undo; simulate a concurrent change → undo surfaces the conflict message, doesn't
  silently overwrite; Owner performs no undoable action (toasts informational); existing toasts
  elsewhere still render unchanged.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Routine reversible actions (soft-delete/void) show an Undo toast that restores state within a time
   window; existing non-action toasts are unaffected (backward-compatible contract).
3. Undo clears `IsDeleted` through the owning module's command path with audit columns updated and
   optimistic-concurrency respected (conflict surfaced, never silently overwritten).
4. Undo is time-boxed and idempotent; expired/used callbacks are inert.
5. Owner (read-only) triggers no undoable actions; cross-module effects use MediatR; any styled chrome
   recolors on theme toggle.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-29-summary.md` (template `Progress/_template.md`). Include: the
backward-compatible contract change (the `NotificationAction` shape + overloads), the modules/commands
that got undo, how the reversal routes through the command path with audit + concurrency handling, the
time-box/idempotency mechanism, and the realization results (restore, expiry, conflict, Owner). Note
`codebase_wiki` discrepancies (contract change, service change, per-command undo wiring).

### Documentation
Add `patterns/wpf-vista-notification-undo.md` (the optional-action contract pattern, the
undo-via-module-command + audit + concurrency rule, time-box/idempotency, the `Await`-in-`Catch`
avoidance in the async callback) per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md` +
`log.md`.
