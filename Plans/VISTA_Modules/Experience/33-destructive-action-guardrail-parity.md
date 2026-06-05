---
module: MerchSys.App
plan-id: UX-33
title: "Destructive-Action Guardrail Parity — Confirmation & Undo Coverage Across All Write Paths"
depends-on: [UX-20, UX-29]
estimated-files: 9
---

# Destructive-Action Guardrail Parity — Confirmation & Undo Coverage Across All Write Paths

> **Completion sweep** of shipped roadmap items **B4 (UX-20)** and **A6 (UX-29)** of
> [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md). Closes the coverage gaps recorded in
> `ux_review_report.md` §1.7 and §1.8.

## Context

The two safety guardrails shipped but cover an inconsistent subset of destructive actions:

- **Confirmation dialogs (UX-20)** — 7 commands route through `IConfirmationPresenter`, but several
  irreversible paths are ungated: product deactivation (`ToggleActiveAsync` in
  `ProductManagementViewModel`), credit-account blocking, and shrinkage recording (an irreversible
  inventory reduction).
- **Undo (UX-29)** — wired on only 3 soft-delete paths (vendor delete, vendor-catalog-entry delete,
  product-category delete). Product soft-delete, PO-draft delete, credit-account blocking, and
  transaction void have no undo.

The result is uneven: some irreversible financial actions confirm, some offer undo, some offer neither.
This plan establishes **parity** — every destructive write path lands on a deliberate guard level.

> **Risk note:** unlike UX-31/32, this plan touches **write paths**. It does **not** change what those
> operations do — it only adds a confirm gate and/or a restore (undo) affordance around existing
> operations. Soft-delete/void semantics, MediatR events, and concurrency handling are unchanged. This
> is the medium-risk plan of the set; treat the inventory step (§1) as a required gate before any edits.

## Prerequisites

- **UX-20** — `IConfirmationPresenter` / `DefaultConfirmationPresenter` / `ConfirmationDialog`
  (`ConfirmationRequest`, `IsDefault`/`IsCancel`, typed-confirmation match).
- **UX-29** — the toast `Undo` action + time-boxed soft-delete restore pattern (`NotificationAction`,
  `RestoreAsync`/`RestoreCatalogEntryAsync`/category-restore, the ~8-second window + idempotency).
- The existing soft-delete (`IsDeleted`) and void operations on the target entities.
- Existing deps only. **No new NuGet.**

## Scope

### 1. Write-path guard inventory (do this first — it is a deliverable)
Produce a table of **every** destructive/irreversible write command: the command, its entity, whether
it is reversible (soft-delete/void) or hard, its financial materiality, its **current** guard
(confirm / undo / neither), and its **target** guard. This table drives §2–§3 and ships in the summary.
Known entries to classify (from the review): product deactivation (`ToggleActiveAsync`), PO-draft
delete, credit-account block/unblock, transaction void, shrinkage recording, plus the already-guarded
vendor/catalog/category deletes.

### 2. Confirmation parity (review §1.8)
Route the ungated irreversible commands through `IConfirmationPresenter`:
- **Shrinkage recording** — irreversible inventory reduction → **typed confirmation** (high materiality).
- **Credit-account blocking** — confirm (financial impact on a customer's utang line).
- **Transaction void** — confirm (BIR-sensitive; likely typed confirmation).
- **Product deactivation** — confirm **or** rely on undo (decide in §1; deactivation is reversible, so
  undo may be the better fit than a modal — see §3).

### 3. Undo parity (review §1.7)
Add the UX-29 toast-Undo + time-boxed restore to the reversible paths that lack it:
- **Product deactivation** (`ToggleActiveAsync` soft-deactivate) → Undo restores active state.
- **PO-draft delete** → Undo restores the draft.
- **Credit-account block** → Undo unblocks (if modeled as reversible state).
Each reuses the UX-29 restore mechanism (idempotent, single-fire within the window).

> **Guard-level rule (decide per row in §1):** **reversible** actions favor **Undo** (low-friction,
> matches NN/g "user control & freedom"); **irreversible / high-materiality** actions get
> **Confirmation** (typed where BIR/financial). A few may warrant **both**. No destructive write path
> may end up with **neither**.

> **Out of scope:**
> - Changing soft-delete/void semantics, retention rules, or MediatR contracts.
> - Hard-deleting anything, or making an irreversible op reversible at the data layer.
> - Bulk/multi-select undo (single-action undo only, as in UX-29).

## Specification

### 0. Watch-items

1. **Behavior-preserving.** The underlying operation is unchanged; only a pre-confirm gate and/or a
   post-action restore affordance is added. No new business rule, event, or concurrency change.
2. **Reuse, don't re-implement.** Confirm via the existing `IConfirmationPresenter`; undo via the
   existing UX-29 toast-action + restore. No parallel dialog or parallel restore path.
3. **Undo idempotency + window.** Restore fires at most once within the time box; expiry finalizes the
   soft-delete. A second click, a timeout, or a navigation must not double-restore or error.
4. **Confirmation correctness.** Typed-confirmation requires exact text match to enable confirm;
   `IsDefault`/`IsCancel` wired; cancel performs **no** state change.
5. **Role + audit.** Manager only (Owner has no write affordances). Existing structured audit logging on
   the financial state changes is preserved — confirm/undo must not bypass it.
6. **Concurrency.** A confirmed write still flows through the existing optimistic-concurrency path; on
   `DbUpdateConcurrencyException` the existing "data changed elsewhere — refresh and retry" surfaces.
7. **VB traps.** `Await` only outside `Catch`/`Finally` (capture state, await after — BC36943); no
   parameter/property shadowing; full root prefix; reserved-keyword-safe names.

### 1. Constraints

- Write paths touched **only** to add guards; operation semantics unchanged.
- Reuse UX-20 and UX-29 mechanisms verbatim.
- No new NuGet. Tokens + theme on any new dialog copy; no inline hex.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — for each newly-guarded path: trigger it → confirm gate appears (typed match
  where specified) and cancel is a no-op; for undo paths → perform the action, click Undo within the
  window → state restored; let the window lapse → action finalizes. Verify audit rows still written.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. The §1 guard inventory exists and **every** destructive write path has a deliberate guard
   (confirm / undo / both) — none left at "neither".
3. Shrinkage recording, credit-account blocking, and transaction void are confirmation-gated (typed
   where specified); product deactivation, PO-draft delete (and credit block, if reversible) offer Undo.
4. Operation semantics, MediatR events, concurrency handling, and audit logging are unchanged; cancel is
   a true no-op; undo is idempotent within its window.
5. Manager-only; Owner has no write affordances.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-33-summary.md` (template `Progress/_template.md`). Include: the
full write-path guard inventory (command → reversibility → materiality → old guard → new guard) as the
centerpiece, the confirm-vs-undo decision per row, proof undo stays idempotent/time-boxed and audit
rows still write, and realization results. Note `codebase_wiki` discrepancies (new
`IConfirmationPresenter` call sites; new restore/undo wiring) for Antigravity.

### Documentation
Add `patterns/wpf-vista-destructive-action-guard.md` per `workflow-agent-wiki-update.md` — the
reversible→undo / irreversible→confirm decision rule, the idempotent time-boxed restore, and the
"never neither" invariant. Update `agent_wiki/index.md` + `log.md`.
