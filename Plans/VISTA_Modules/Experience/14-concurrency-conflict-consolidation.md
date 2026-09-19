---
module: MerchSys.App
plan-id: UX-14
title: "Concurrency-Conflict UX — Consolidation & Full Write-Path Coverage"
depends-on: [UX-06]
estimated-files: 8
---

# Concurrency-Conflict UX — Consolidation & Full Write-Path Coverage

## Context

UX-06 implemented the `CLAUDE.md`-mandated optimistic-concurrency UX — `IConflictPresenter` +
`ConcurrencyConflictPrompt` ("Data changed elsewhere — refresh and retry", Refresh/Cancel, **no
force-overwrite**) — and wired it into the **five heaviest write paths**: `SalesCartViewModel`,
`APLedgerViewModel`, `GoodsReceivingViewModel`, `CreditManagementViewModel`, `VatSettingsViewModel`.

A post-UX-06 review found two gaps that this plan closes:

1. **The conflict pattern is duplicated inline across the five VMs** while the shared primitive
   `SharedKernel.Persistence.ConcurrencyHelper.ExecuteWithConflictPromptAsync` sits unused. Five copies
   of a safety-critical "never silently overwrite" flow is exactly the class of duplication UX-05 was
   created to kill — the next person who adds a write path will copy a sixth.
2. **Coverage is incomplete.** Only five write paths are guarded. VISTA runs up to 4 client laptops
   against one MariaDB instance, so *any* unguarded mutation of a concurrency-token row
   (`Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`,
   `Pos_CreditAccounts.OutstandingBalance`, PO status, product/vendor edits, …) can still throw an
   unhandled `DbUpdateConcurrencyException` and crash the operator's screen.

> A pre-work fix already landed: `ConcurrencyHelper` had a second, **dangerous** method
> (`ExecuteWithConcurrencyRetryAsync`) that auto-refreshed with only a toast and re-threw — **no
> prompt** — which violates the never-silently-overwrite mandate. It was dead code and has been
> removed; only the correct `ExecuteWithConflictPromptAsync` primitive remains. Do not re-introduce an
> auto-refresh-without-prompt path.

This is a **behavior-changing** plan (it modifies write-path ViewModels). It changes only *failure
handling* — successful flows, totals, FIFO, VAT, and receipts are untouched.

Read `UX-06` (the mechanism), `[[wpf-vista-state-feedback]]` (the conventions),
`[[centralized-database-architecture]]`, and `[[feedback-vbnet-await-catch]]` first.

## Prerequisites

- **UX-06** — `IConflictPresenter`/`DefaultConflictPresenter`/`ConcurrencyConflictPrompt`,
  `INotificationService`, and the five wired VMs all exist and are proven.
- The canonical primitive `ConcurrencyHelper.ExecuteWithConflictPromptAsync(work, onRefresh, presenter)`
  in `MerchSys.SharedKernel/Persistence/`.
- Existing deps only: `CommunityToolkit.Mvvm`, `MediatR`, EF Core 10 + `MySqlConnector`. **No new
  NuGet.**

## Scope

- **Consolidate** the five wired VMs onto the shared primitive (or a thin shared wrapper), removing the
  duplicated inline catch/flag/prompt/refresh blocks — *without* changing observable behavior.
- **Enumerate every remaining write-path ViewModel** and guard each one that mutates a
  concurrency-token row, using the same primitive.
- **Standardize** the success/refresh/toast shape so all write paths behave identically.

> **Out of scope:**
> - Any change to business logic, totals, FIFO costing, VAT computation, receipt generation, or the
>   service/data layer. This plan touches **VM failure handling only**.
> - The `ConcurrencyConflictPrompt` visual (UX-06/UX-05 own it). Reuse as-is.
> - Adding a force-overwrite / last-writer-wins option. Forbidden by `CLAUDE.md`.
> - Server-side locking (`SELECT … FOR UPDATE` in the FIFO decrement) — that is INFRA-26's concern, not
>   a UX plan.

## Deliverables

The consolidated five VMs, the newly-guarded write-path VMs, any minimal extension to
`ConcurrencyHelper` needed for clean adoption, the implementation summary, and a wiki update.

## Specification

### 0. Carry-forward watch-items (from the UX-06 review)

1. **The primitive only catches `DbUpdateConcurrencyException` — preserve each VM's *other* error
   handling.** `APLedgerViewModel`, for example, also catches `InvalidOperationException` (domain
   validation: "amount exceeds balance") and shows a success toast on the happy path. Consolidation
   must keep those. Two acceptable shapes — pick one and apply it consistently:
   - Call the primitive for the conflict mechanics and keep a thin VM-side `Try` around it for the
     non-concurrency exceptions + success toast; **or**
   - Extend `ConcurrencyHelper` with optional `onSuccess`/`onError` callbacks (still
     prompt-only on conflict) and route everything through it.
   Either way: the primitive must **not** swallow non-concurrency exceptions — they propagate so the
   VM keeps its domain-error UX. Document the chosen shape.
2. **`Await` is illegal in `Catch`/`Finally` (BC36943).** The primitive already captures a flag and
   awaits the prompt after the block — any VM-side wrapper you add must do the same. This is the single
   most-repeated trap on this code path.
3. **The Cancel path must not destroy the operator's input.** On Cancel (prompt returns `False`), the
   form/dialog and the values the operator typed should remain so they can copy them before refreshing.
   Verify the five existing VMs still behave this way after consolidation; apply the same to new ones.
4. **Owner is read-only — do not add conflict UI to Owner-only paths.** Owner sees dashboards/reports
   with no writes; the data layer (OWASP-DA5 `RoleGuardInterceptor`) already rejects Owner writes. Guard
   only genuine Manager write paths.

### 1. Consolidation (the five wired VMs)

Replace each VM's inline `Dim concurrencyError…/Catch DbUpdateConcurrencyException…/If concurrencyError
Then Await _conflictPresenter.PromptAsync()` block with the shared primitive (per watch-item #1).
After this step a grep for `Catch ex As DbUpdateConcurrencyException` inside `…/ViewModels/` should
return **only** the primitive's own usage site (and any VM wrapper that deliberately keeps a local
catch for a documented reason). Provide the before/after grep counts in the summary.

### 2. Coverage sweep (remaining write paths)

Enumerate **every** ViewModel that triggers a mutation of a concurrency-token row. Use
`codebase_wiki/index.md` + the per-module index pages + a grep for the mutating service calls
(`…Async` methods that ultimately `SaveChangesAsync`) — do not rely on this list being complete:

- Inventory: `ProductManagementViewModel` (product create/edit/soft-delete), `ShrinkageViewModel`
  (record shrinkage → `Inv_StockBatches`).
- Purchasing: the PO create/submit/save-draft path, `ReorderSuggestionsViewModel`
  (accept → PO creation), `VendorDirectoryViewModel` / vendor catalog edits.
- POS: sales return / void path, any credit-account edit not already covered.

For each genuine write path, guard the mutation with the primitive (prompt → refresh-on-confirm →
never re-save stale). If a candidate VM turns out **not** to mutate a concurrency-token row (pure
read, or append-only child rows with `RowVersion` ignored — see
`[[efcore-inherited-rowversion-unmapped-column]]`), note it as "no token, not guarded" and move on.

### 3. Constraints

- **Behavior-preserving for success** — a successful save/payment/receive must do exactly what it did
  before (same toast, same refresh, same navigation). Only the conflict branch is standardized.
- **Never silently overwrite** — every guarded path prompts on conflict and drops the stale write; no
  path re-saves with stale original values; no force-overwrite option.
- **Architecture** — VMs stay in their module libraries; cross-module reads via MediatR queries only;
  no new cross-module project references; `IConflictPresenter`/`INotificationService` injected via the
  existing DI pattern.
- **VB/XAML traps** — no `Await` in `Catch`/`Finally` (BC36943); `errMsg`-style names (never `err`);
  `Enumerable.Count(list, pred)` not `.Count(pred)` on a `List`.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — the prompt is a runtime dialog; boot the app and trigger at least one
  consolidated path and one newly-guarded path (a forced/simulated conflict) in **both** themes.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. The five UX-06 VMs no longer inline the conflict block — they route through
   `ConcurrencyHelper.ExecuteWithConflictPromptAsync` (or the agreed wrapper); before/after grep counts
   provided; their success/cancel/validation behavior is unchanged.
3. Every write-path VM that mutates a concurrency-token row is guarded; the enumeration (guarded vs
   "no token, not guarded") is documented.
4. A simulated concurrent update on any guarded path shows "Data changed elsewhere — refresh and retry"
   with Refresh/Cancel; Refresh reloads live values; **no path silently overwrites**; Cancel preserves
   the operator's input. (Document the manual repro.)
5. No business logic, totals, FIFO, VAT, receipt, or service-layer change; Owner read-only paths show
   no conflict UI.
6. Light/dark unaffected; theme toggle recolors the prompt.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-14-summary.md` (template `Progress/_template.md`). Include: the
chosen consolidation shape (watch-item #1) and why, the before/after inline-catch grep counts, the full
write-path enumeration (guarded vs not, with the "no token" reason for each skip), the conflict manual
repro + result, any `ConcurrencyHelper` extension made, and `codebase_wiki` discrepancies.

### Documentation
Update `patterns/wpf-vista-state-feedback.md` (the consolidation outcome + the canonical-primitive
adoption rule) per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
