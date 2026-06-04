---
module: MerchSys.App
plan-id: UX-20
title: "Confirmation & Destructive-Action Dialogs — Shared Confirm Presenter"
depends-on: [UX-06, UX-17]
estimated-files: 11
---

# Confirmation & Destructive-Action Dialogs — Shared Confirm Presenter

> **Level 1 (Basic) — item B4** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).
> Mirrors the UX-06 conflict-presenter pattern; builds on B1 (UX-17) for dialog keyboard handling.

## Context

Destructive and financial actions are scattered and confirm inconsistently — or not at all. Confirmed
real destructive commands across the modules:

- `ProductManagementViewModel.DeleteCategoryAsync` (`DeleteCategoryCommand`)
- `PurchaseOrderListViewModel.DeleteSelectedAsync` (`DeletePOCommand`, gated by `CanDeleteSelected`)
- `VendorListViewModel.DeleteSelectedAsync` (`DeleteVendorCommand`)
- `VendorCatalogViewModel.DeleteEntryAsync` (`DeleteEntryCommand`, gated by `CanModify`)
- transaction **returns/void** in `TransactionHistoryViewModel`, and **PO submit** (an irreversible
  state change) in `PurchaseOrderListViewModel`/editor

These operate on soft-deleted (`IsDeleted`) financial/inventory records, so a misclick is costly. There
is already a proven presenter pattern to mirror: `IConflictPresenter` (in
`MerchSys.SharedKernel.Interfaces`) → `DefaultConflictPresenter` (`MerchSys.App/Services`) shows a modal
`Window` (`ConcurrencyConflictPrompt`) via `Application.Current.Dispatcher.Invoke` + a
`TaskCompletionSource(Of Boolean)` and returns the user's choice.

This plan adds a **single shared confirmation pattern** for irreversible/financial actions: one
`ConfirmationDialog` window + an `IConfirmationPresenter` service, with consistent button order, danger
styling on the destructive button, a clear consequence sentence, and a "type to confirm" affordance for
the most dangerous actions. Existing destructive commands are routed through it. **The underlying
commands are not changed** — only a confirm gate is added in front of them.

Read `UX-06` (the `IConflictPresenter`/`DefaultConflictPresenter` pattern), `UX-17` (dialog
`IsDefault`/`IsCancel`), and `[[feedback-vbnet-await-catch]]` first.

## Prerequisites

- **UX-06** — the `IConflictPresenter`/`DefaultConflictPresenter`/`ConcurrencyConflictPrompt` pattern to
  mirror (interface in SharedKernel, impl + dialog `Window` in App, DI-registered).
- **UX-17** — the dialog will set `IsDefault`/`IsCancel` per the keyboard floor (a real modal `Window`,
  so those Just Work).
- **CommunityToolkit.Mvvm + MediatR** only. **No new NuGet.**

## Scope

- **`IConfirmationPresenter`** in `MerchSys.SharedKernel.Interfaces` — `PromptAsync(request)` returning
  `Task(Of Boolean)`, where the request carries title, message/consequence, confirm-button text, a
  `IsDestructive` flag (danger styling), and an optional `RequireTypedConfirmation` token (e.g. the
  record name the user must type).
- **`ConfirmationDialog`** (`Views/Shell/ConfirmationDialog.xaml` + code-behind) — a real modal `Window`
  modeled on `ConcurrencyConflictPrompt`: tokenized surface, `DangerBrush` confirm button when
  destructive, `IsDefault`/`IsCancel` wired, consistent button order (Cancel left, Confirm right),
  optional "type to confirm" text box that enables Confirm only on match.
- **`DefaultConfirmationPresenter`** in `MerchSys.App/Services` — mirrors `DefaultConflictPresenter`
  (`Dispatcher.Invoke` + `ShowDialog` + `TaskCompletionSource(Of Boolean)`), DI-registered in
  `Application.xaml.vb`.
- **Route existing destructive commands** through the presenter: each confirmed command awaits
  `PromptAsync` and proceeds only on `True`.

> **Out of scope:**
> - **Undo** — that is **Advanced A6** (notification actions & undo on soft-delete). This plan adds the
>   *forward* confirm; A6 later adds the *reverse*.
> - Changing what any delete/void/submit command does once confirmed, the `IsDeleted` semantics, the
>   concurrency primitive (UX-14), or any business rule.
> - Wrapping non-destructive `Cancel*Command`s — those just dismiss a panel (verified) and must **not**
>   gain a confirm. Confirm only genuinely destructive/irreversible/financial actions.

## Deliverables

`IConfirmationPresenter`, `ConfirmationDialog` (+ code-behind), `DefaultConfirmationPresenter`, the DI
registration, the routing of the enumerated destructive commands, the implementation summary, and a wiki
update.

## Specification

### 0. Watch-items

1. **Confirm gate is additive — never alter the command's post-confirm behavior.** Insert
   `If Not Await _confirm.PromptAsync(req) Then Return` at the top of each destructive command; leave
   everything after it (including the UX-14 concurrency primitive call and any domain-error handling)
   exactly as-is. The await is inside `Try`, never a `Catch`/`Finally` (BC36943).
2. **Confirm only the destructive set; leave panel-dismiss commands alone.** The many `Cancel*Command`s
   (e.g. `CancelPaymentCommand`, `CancelEditorCommand`, `CancelReturnCommand`) merely hide a dialog and
   are **not** destructive (verified). Do not route them through the presenter. Confirm the enumerated
   delete/void/submit set; if uncertain whether an action is destructive, list it in the summary as a
   judgment call rather than guessing.
3. **Respect existing `CanExecute` gates.** `DeletePOCommand`/`DeleteVendorCommand`/`DeleteEntryCommand`
   already gate on selection/`CanModify`. The confirm runs *after* `CanExecute` allows the command — do
   not duplicate or weaken those gates.
4. **"Type to confirm" only for the most dangerous.** Use the typed-token affordance sparingly (e.g.
   deleting a vendor/category with dependents, voiding a completed sale). Routine single-row deletes get
   a plain confirm. Document which actions use which level.
5. **Mirror the conflict presenter exactly — including the dispatcher marshaling.** Use
   `Application.Current.Dispatcher.Invoke` + `ShowDialog` + `TaskCompletionSource(Of Boolean)` as
   `DefaultConflictPresenter` does, so it works when called from a non-UI context. Register it as a
   singleton in `Application.xaml.vb` alongside the conflict presenter; inject it into VMs the same way
   `IConflictPresenter` is injected.
6. **Owner has no destructive commands** — Owner never sees these dialogs. Confirm on an Owner login that
   no confirm UI is reachable.
7. **Theme + tokens + keyboard.** `DangerBrush` for the destructive confirm button; tokens only; Enter
   confirms / Esc cancels via `IsDefault`/`IsCancel` (UX-17); recolors on theme swap.

### 1. The presenter contract

Define `IConfirmationPresenter.PromptAsync(request As ConfirmationRequest) As Task(Of Boolean)` in
SharedKernel.Interfaces, with `ConfirmationRequest` (title, message, confirmText, isDestructive,
optional requiredTypedToken). Keep it dependency-free (no WPF types in SharedKernel).

### 2. The dialog

`ConfirmationDialog` modeled on `ConcurrencyConflictPrompt`: tokenized card, message/consequence text,
Cancel (`IsCancel`) + Confirm (`IsDefault`, `DangerBrush` when destructive) in consistent order, optional
typed-confirmation `TextBox` that enables Confirm only when the entry matches the token.

### 3. The presenter impl + DI

`DefaultConfirmationPresenter` in App/Services mirrors `DefaultConflictPresenter`; register it in
`Application.xaml.vb`.

### 4. Routing

Add the confirm gate to each enumerated destructive command. Provide a table (command → confirm level
[plain / typed] → consequence text) in the summary.

### 5. Constraints

- **Additive** — confirm gate only; no change to post-confirm behavior, `IsDeleted` semantics,
  concurrency, or business rules.
- **Architecture** — interface in SharedKernel (WPF-free); dialog + presenter in App; DI via
  `Application.xaml.vb` matching the conflict-presenter registration.
- **Tokens only** — UX-00/UX-05; `DangerBrush` for destructive; no inline hex; theme-reactive.
- **VB/XAML traps** — full root prefix on `clr-namespace`/`x:Class` (MC3074); no `Await` in
  `Catch`/`Finally` (BC36943); reserved-keyword-safe names; a `<Setter>` targets a `DependencyProperty`
  only.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — in **both** themes: trigger each routed command, confirm the dialog shows with
  danger styling + correct consequence, Confirm proceeds / Cancel (and Esc) aborts with no change, the
  typed-confirmation actions block Confirm until matched; Owner reaches none of it.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A shared `ConfirmationDialog` + `IConfirmationPresenter`/`DefaultConfirmationPresenter` exist,
   DI-registered, mirroring the UX-06 conflict-presenter pattern.
3. Every enumerated destructive/irreversible/financial command routes through the confirm gate; Cancel/
   Esc aborts with zero side effects; Confirm proceeds with unchanged post-confirm behavior.
4. The most dangerous actions require typed confirmation; routine deletes use a plain confirm; button
   order and danger styling are identical everywhere.
5. Non-destructive `Cancel*`/dismiss commands are **not** wrapped; existing `CanExecute` gates intact.
6. Owner reaches no confirm UI; tokens only / hex sweep clean; Enter/Esc work; theme toggle recolors.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-20-summary.md` (template `Progress/_template.md`). Include: the
presenter contract, the routed-command table (confirm level + consequence text), which actions use typed
confirmation and why, the destructive-vs-dismiss classification, and the both-themes realization result.
Note `codebase_wiki` discrepancies (the new interface, dialog, presenter, DI entry).

### Documentation
Add `patterns/wpf-vista-confirmation-presenter.md` (the presenter-mirrors-conflict pattern, additive
confirm-gate rule, destructive-vs-dismiss classification, typed-confirmation guidance) per
`workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
</content>
