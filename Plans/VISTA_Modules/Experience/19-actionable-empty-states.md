---
module: MerchSys.App
plan-id: UX-19
title: "Actionable Empty States — Optional CTA on EmptyStatePanel (Role-Aware)"
depends-on: [UX-06]
estimated-files: 8
---

# Actionable Empty States — Optional CTA on EmptyStatePanel (Role-Aware)

> **Level 1 (Basic) — item B3** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).

## Context

UX-06 introduced `Views/Shell/EmptyStatePanel.xaml` and UX-15 rolled it across every list/grid/report.
Confirmed shape: the panel exposes only two dependency properties — `Title` and `Description` (see
`EmptyStatePanel.xaml.vb`) — and its XAML is a centered `StackPanel` of icon + title + description. So an
empty list is a **dead-end**: it tells the user there's nothing here but offers no way forward. The
classic fix is a contextual call-to-action ("Add your first product →", "Create a purchase order →")
that turns the empty state into an entry point.

This plan adds an **optional, role-aware CTA** to `EmptyStatePanel` and wires it on the
highest-traffic Manager lists. It is **additive and backward-compatible**: panels that set no command
render exactly as today, so no existing usage changes behavior.

Read `UX-06`/`UX-15` + `[[wpf-vista-state-feedback]]` (the three-state model: the CTA appears only in the
*empty* state, never during busy/error) first.

## Prerequisites

- **UX-06 / UX-15** — `EmptyStatePanel` exists and is bound to the `IsEmpty` state in the
  busy/empty/error trio. The CTA shows only when `IsEmpty` is true.
- Role-aware command exposure already exists on the module VMs (the same gating that drives role-aware
  navigation). The CTA binds to a command the VM exposes **only** for Manager.
- Existing deps only. **No new NuGet.**

## Scope

- **Extend `EmptyStatePanel`** with two optional dependency properties: `ActionCommand` (`ICommand`) and
  `ActionText` (`String`), plus a button rendered **only** when `ActionCommand` is non-null
  (collapsed otherwise). Tokenized primary-button style; full root prefix on `x:Class` (already correct).
- **Wire CTAs** on the highest-value Manager lists where "create the first record" is the obvious next
  step — e.g. Product Management (add product / add category), Vendor Directory (add vendor), Purchase
  Order List (create PO). The CTA reuses the view's **existing** create/new command — it invents none.
- **Role gating:** the CTA command is the Manager-only create command; for Owner the bound command is
  null, so the button never renders and the panel stays purely informational.

> **Out of scope:**
> - Any new create/edit logic — the CTA invokes an **existing** command (the same one a toolbar "New"
>   button already triggers). If a list has no existing create command (it's inherently read-only, e.g.
>   an audit report), it gets **no** CTA — leave it informational.
> - Differentiating "no data" from "no search results" — that nuance is **Advanced A5** (search/filter
>   maturity). This plan handles the genuinely-empty dataset CTA only.
> - Touching the busy/error states or the three-state precedence from UX-15.

## Deliverables

The extended `EmptyStatePanel` (`.xaml` + `.xaml.vb`), the CTA wiring on the chosen Manager lists, the
implementation summary, and a wiki update.

## Specification

### 0. Watch-items

1. **Backward compatibility is non-negotiable.** Every current `EmptyStatePanel` usage sets only
   `Title`/`Description`. The new button must be `Collapsed` when `ActionCommand Is Nothing`, so all
   existing panels look identical to today. Verify a sampling of untouched usages after the change.
2. **CTA is Manager-only and binds an existing command.** Bind `ActionCommand` to the VM's existing
   create/new command (role-gated so it's null/absent for Owner). Do **not** add a parallel command or a
   second create path. For Owner the button is absent (null command) — confirm on an Owner login.
3. **CTA belongs to the empty state only.** Because `EmptyStatePanel` is shown only when `IsEmpty` is
   true (UX-15 precedence: busy > error > empty > content), the CTA cannot appear during loading or after
   a load failure. Do not bind the CTA anywhere that bypasses that precedence.
4. **Don't double up affordances confusingly.** If a view already has a prominent "New" button in its
   chrome, the empty-state CTA is the *same action* surfaced contextually — fine, but use consistent
   wording. Keep labels imperative and specific ("Add your first product", not "OK").
5. **Tokens + theme.** Button uses the UX-05 primary-button style and tokens; recolors on theme swap; no
   inline hex.
6. **VB/XAML traps** — DP registration mirrors the existing `Title`/`Description` pattern in
   `EmptyStatePanel.xaml.vb`; full root prefix on any `clr-namespace` (MC3074); a `<Setter>` targets a
   `DependencyProperty` only.

### 1. Extending the control

Add `ActionCommandProperty` and `ActionTextProperty` dependency properties (same `DependencyProperty.
Register` pattern already used for `Title`/`Description`). In the XAML, append a button beneath the
description bound via `RelativeSource AncestorType=UserControl` to `ActionCommand`/`ActionText`, with
`Visibility` driven by a null→`Collapsed` converter on `ActionCommand`.

### 2. Wiring the CTAs

For each chosen Manager list, set `ActionCommand`/`ActionText` on its `EmptyStatePanel` to the existing
create command. Provide a table (view → CTA text → bound command → Owner behavior) in the summary.

### 3. Constraints

- **Additive / backward-compatible** — existing panels unchanged; no new business logic; CTA reuses
  existing commands.
- **Role-aware** — CTA absent for Owner; Manager-only create commands only.
- **Tokens only** — UX-00/UX-05; no inline hex; theme-reactive.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — in **both** themes and as **both** roles: empty a Manager list and confirm the
  CTA shows and invokes the create flow; on Owner the same panel shows no button; an inherently
  read-only empty report shows no CTA.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. `EmptyStatePanel` gains optional `ActionCommand`/`ActionText`; with no command set it renders exactly
   as before (existing usages visually unchanged).
3. Chosen Manager lists show a contextual CTA when empty that invokes the existing create command.
4. Owner sees the informational panel with **no** CTA (null command); read-only reports get no CTA.
5. The CTA appears only in the empty state (never busy/error), per UX-15 precedence.
6. No new create/edit logic; tokens only / hex sweep clean; theme toggle recolors the CTA.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-19-summary.md` (template `Progress/_template.md`). Include: the new
DPs, the backward-compatibility verification, the per-view CTA table (text/command/Owner behavior), and
the both-themes/both-roles realization result. Note `codebase_wiki` discrepancies (the extended
`EmptyStatePanel`).

### Documentation
Extend `patterns/wpf-vista-state-feedback.md` with the actionable-empty-state recipe (optional CTA,
null-command collapse, role gating) per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md` +
`log.md`.
</content>
