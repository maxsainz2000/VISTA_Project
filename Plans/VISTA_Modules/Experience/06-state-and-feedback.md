---
module: MerchSys.App
plan-id: UX-06
title: "State & Feedback — Loading, Empty, Error, Validation & Concurrency UX"
depends-on: [UX-05]
estimated-files: 22
---

# State & Feedback — Loading, Empty, Error, Validation & Concurrency UX

## Context

The theme epic (UX-00 → UX-05) made VISTA *look* consistent and gave it a reusable component library —
but every plan in it **froze behavior**. The app still doesn't reliably tell the operator *what is
happening*: grids show nothing while loading and nothing when empty (indistinguishable from a hang),
save failures are silent or raw-exception, forms accept bad input without inline guidance, and — most
importantly — the **concurrency-conflict UX mandated by `CLAUDE.md` is not implemented**.

This is the first **behavior-changing** UX plan. It is also the most operationally important before the
testing phase: VISTA runs up to 4 client laptops against one centralized MariaDB instance, so write
conflicts are a *normal* runtime condition, not an edge case.

> Unlike UX-04/UX-05, this plan **modifies ViewModels and code-behind.** Apply the VB.NET async traps:
> never `Await` inside a `Catch`/`Finally` (BC36943, `[[feedback-vbnet-await-catch]]`) — capture error
> state in the block, await after it. Keep all cross-module communication on MediatR; do not add
> cross-module project references.

Read `UX-05` (the component library these states plug into), `[[centralized-database-architecture]]`
(the concurrency model), and `[[mariadb-pure-client-server-architecture]]` first.

## Prerequisites

- **UX-05** — the shared component library (`Themes/Components.xaml`). New state UI (empty panels, busy
  overlays, the conflict dialog, validation error templates) is authored as library components / styled
  with tokens, not bespoke per view.
- Existing deps only: `CommunityToolkit.Mvvm` (for `ObservableValidator`, `[ObservableProperty]`,
  `IRelayCommand` busy state), `Notification.Wpf` (already referenced — the toast surface),
  `MediatR`, EF Core 10 + `MySqlConnector`. **No new NuGet packages.**

## Scope (three tracks)

### Track 1 — Concurrency-conflict UX (the `CLAUDE.md` compliance gap) — *highest priority*

On `DbUpdateConcurrencyException` (raised when an optimistic-concurrency token —
`Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`,
`Pos_CreditAccounts.OutstandingBalance`, etc. — was changed by another client), surface the mandated
message **"Data changed elsewhere — refresh and retry."** and **never silently overwrite**.

- Add a shared `ConcurrencyConflictPrompt` (a tokenized modal or a non-dismissable toast) offering
  **Refresh** (reload current values) and **Cancel**. No "force overwrite" option.
- Wire it into the mutation commands of the write-path ViewModels — start with the heaviest:
  SalesCart checkout, AP payment recording, stock adjustment / goods receiving, credit (utang) payment,
  VAT settings save. Locate the exact VMs via `codebase_wiki/index.md` + the module index pages.
- The handler is a thin shared helper (e.g. an `IUserInteraction`/`IConflictPresenter` service injected
  into VMs) so it is wired the same way everywhere and is unit-testable later.

### Track 2 — Loading / empty / error states

- **`BusyOverlay`** — a reusable component bound to a VM `IsBusy` flag (use the `IRelayCommand`
  execution state or an `[ObservableProperty] IsBusy`), shown over DataGrid-heavy views (dashboards,
  ledgers, reports) during async loads. Tokenized spinner + dimmed `OverlayBrush`.
- **`EmptyStatePanel`** — a reusable component (icon/glyph + headline + optional hint) shown when a
  loaded collection is empty, so "no data" is visually distinct from "still loading". Bound to an
  `IsEmpty` (count == 0 && !IsBusy) condition.
- **Standardized toasts** — a thin `NotificationService` wrapper over `Notification.Wpf` exposing
  `Success/Info/Warning/Error(message)` with token-aligned styling, replacing ad-hoc/missing feedback
  on save/delete/export. Route success of the major write paths through it.

### Track 3 — Inline form validation

- On the input-heavy forms (Goods Receiving, Product Management, VAT Settings, Vendor add/edit,
  Shrinkage record dialog), adopt `CommunityToolkit.Mvvm.ObservableValidator` + data-annotation
  attributes so invalid fields report via `INotifyDataErrorInfo`.
- Provide one tokenized `Validation.ErrorTemplate` + an error-text style in the component library
  (red `DangerBrush` adorner + message), and enable it on the implicit `TextBox`/`ComboBox` or via a
  `ValidatingInput` style. Disable the primary action while the form is invalid (`HasErrors`).
- Replace any existing ad-hoc `ErrorText` blocks (seen in `ProductManagementView`, `VatSettingsView`)
  with the shared mechanism.

## Deliverables

Shared state components (`BusyOverlay`, `EmptyStatePanel`, `ConcurrencyConflictPrompt`, the validation
error template/style), the `NotificationService` + `IConflictPresenter` services and their DI
registration in `MerchSys.App`, the VM/code-behind wiring for the write-path and input-path views, the
implementation summary, and wiki updates.

## Specification

- **Service registration:** register `NotificationService` and `IConflictPresenter` in the existing
  `Microsoft.Extensions.DependencyInjection` container in `MerchSys.App`; inject into VMs via
  constructor (match the existing VM DI pattern — check `codebase_wiki`).
- **Async correctness:** load/save commands are `Async`. Set `IsBusy = True` before `Await`, reset in a
  `Finally`; capture any exception into a local inside the `Try`/`Catch` and do the toast/await **after**
  the `Catch` block (BC36943). Catch `DbUpdateConcurrencyException` specifically and route it to Track 1,
  distinct from generic errors (Track 2 error toast).
- **Concurrency reload:** "Refresh" must re-query current DB values and re-bind — not just dismiss. Use
  the existing query path (MediatR query or the VM's load command). Never call `SaveChanges` again with
  stale original values.
- **Role awareness:** Owner is read-only. State UI on Owner-facing views (OwnerDashboard, reports) is
  loading/empty only — no save/conflict/validation paths (there are no writes). Enforce at the data
  layer as already required (OWASP-DA5); the UI simply has nothing to validate.
- **Tokens only** — all new UI uses the UX-00 tokens and UX-05 components; no inline hex; honors light/
  dark and the live theme swap.
- **Behavior changes are additive** — existing successful flows must work exactly as before; this plan
  adds feedback/guards around them, it does not alter business logic, totals, FIFO, VAT, or receipts.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. **Concurrency:** simulating a conflicting concurrent update on a guarded write path shows
   "Data changed elsewhere — refresh and retry" with Refresh/Cancel; choosing Refresh reloads live
   values; **no path silently overwrites** another client's change. (Document the manual repro.)
3. **States:** DataGrid-heavy views show a busy indicator during load and a distinct empty-state when a
   loaded result set is empty; the two are never confused.
4. **Feedback:** major write actions (sale, payment, receive, save settings, delete) produce a
   success/error toast via the shared `NotificationService`.
5. **Validation:** the targeted forms block submission while invalid and show inline, token-styled,
   per-field errors; valid input submits exactly as before.
6. Light/dark unaffected; theme toggle re-colors all new UI; Owner read-only paths show no write UI.
7. No regression to existing bindings, commands, navigation, or business logic.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-06-summary.md` (template `Progress/_template.md`). Include: which
VMs/views were wired for each track, the concurrency-conflict manual repro + result, the list of forms
given inline validation, services registered, any VB.NET async-trap fixes applied, and `codebase_wiki`
discrepancies.

### Documentation
- A `patterns/` entry on the VISTA state/feedback conventions: the `IBusy`/`IsEmpty` flag pattern, the
  `IConflictPresenter` contract + the never-overwrite rule, the `NotificationService` API, and the
  `ObservableValidator` + tokenized error-template recipe. Log per `workflow-agent-wiki-update.md`;
  update `agent_wiki/index.md` + `log.md`.
- If a reusable concurrency-handling helper emerges, cross-link it from
  `[[mariadb-pure-client-server-architecture]]` (agent wiki only — the domain `wiki/` is read-only).
