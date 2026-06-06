---
module: MerchSys.App
plan-id: UX-36
title: "Full Accessibility / WCAG 2.2 AA — AutomationProperties & Modal Focus Traps"
depends-on: [UX-17, UX-20, UX-16]
estimated-files: 6
---

# Full Accessibility / WCAG 2.2 AA — AutomationProperties & Modal Focus Traps

> **Level 3 (Pro) — item P1** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). Takes accessibility from the
> Basic *floor* (UX-17's keyboard + focus) to **complete**: assistive-tech naming and modal focus traps.
> Additive — no business logic, query, or write-path change.
>
> **Scope amended 2026-06-06:** a high-contrast theme variant was originally part of this plan but was
> dropped at user request (judged not beneficial / added UI noise). Light/Dark remain the only themes.

## Context

UX-17 (B1) established the keyboard/focus floor: tab order, focus visuals, `IsDefault`/`IsCancel`,
mnemonics. What remains for genuine operability by assistive technology:

- **Screen-reader naming is sparse.** Only ~21 `AutomationProperties`-class hits exist today (mostly
  `Themes/Controls.xaml`). Icon-only buttons, list rows, KPI tiles, and the nav rail announce poorly or
  not at all.
- **Modals don't trap focus.** `CommandPalette` (UX-16), `ConcurrencyConflictPrompt` (UX-14), and the
  `ConfirmationDialog` (UX-20) are overlays; keyboard focus can tab *behind* them into the obscured
  shell, breaking the modal contract (WCAG 2.4.3 / 2.1.2).

This is an accessibility-completeness item, not a feature. **All changes are additive and presentation-
layer** — new attached properties and focus-scope wiring on existing modals.

## Prerequisites

- **UX-17** — the keyboard/focus foundation (tab order, focus visuals, default/cancel) these names and
  traps build on.
- **UX-20 / UX-16 / UX-14** — the three modal overlays that receive focus traps
  (`ConfirmationDialog`, `CommandPalette`, `ConcurrencyConflictPrompt`).
- Existing deps only. **No new NuGet** (`AutomationProperties`, `KeyboardNavigation`, `FocusManager`
  are built into WPF).

## Scope

### A. AutomationProperties sweep
Add `AutomationProperties.Name` (and `HelpText` where the control's purpose isn't obvious from its
content) to every interactive control that currently announces poorly: icon-only buttons (the
action-button glyphs from UX-07), the activity-rail nav items, list/grid rows, KPI tiles, the password
reveal button, and toolbar/refresh affordances. Where a control's accessible name should track a bound
value, bind `AutomationProperties.Name`; otherwise use a literal (localization comes later in UX-44).

### B. Modal focus traps
On the three modal overlays, contain focus for the lifetime of the modal: move initial focus into the
modal on open, prevent Tab/Shift+Tab from leaving it (cycle within), and restore focus to the invoking
control on close. Esc-to-cancel (already present) must keep working. No new modal infrastructure —
attach to the existing overlay roots.

> **Out of scope:**
> - Re-architecting tab order or focus visuals (that is UX-17 — only fill gaps it left).
> - Any business-logic, query, or write-path change; KPI/data values are read-only as-is.
> - A high-contrast theme (dropped 2026-06-06 — not wanted).

## Specification

### 0. Watch-items

1. **Additive only.** New attached properties and focus-scope wiring — no existing binding, command, or
   layout changes meaning. No business/query/write change.
2. **Names are tokens or bindings, never colors.** `AutomationProperties.Name` is a string DP; bind it
   or set a literal. Never feed a brush into a color property.
3. **Focus trap must restore.** On modal close, focus returns to the invoker — never left orphaned on a
   disposed visual. Subscribe/unsubscribe symmetrically; no leaked handlers.
4. **Don't set `FocusManager.IsFocusScope=True` for the trap.** A focus scope is for menus/toolbars and
   diverges logical from keyboard focus; `KeyboardNavigation.TabNavigation=Cycle` (plus
   `ControlTabNavigation=Cycle`) is what actually contains focus.
5. **Don't regress keyboard floor.** Traps must not break Esc-cancel, Enter-confirm, mnemonics, or the
   UX-17 tab order outside the modal.
6. **VB traps** — full `clr-namespace` root prefix (MC3074); `<Setter>` targets a DP only; reserved-
   keyword-safe names; no `Await` in `Catch`/`Finally` (BC36943); `System.Console` if logging.

### 1. Constraints

- No new NuGet. No inline hex on any touched view.
- **Role model** — Owner benefits equally (read-only screens still get names and traps).
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both existing themes):** a screen reader (Narrator) announces each swept control
  meaningfully; Tab cannot escape any of the three modals and focus restores on close — correct in both
  Light and Dark.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Every interactive control in the swept surfaces exposes a meaningful `AutomationProperties.Name`
   (and `HelpText` where needed); icon-only controls are no longer anonymous to a screen reader.
3. `CommandPalette`, `ConcurrencyConflictPrompt`, and `ConfirmationDialog` trap focus while open and
   restore focus to the invoker on close.
4. No business/query/write change; the UX-17 keyboard floor is intact.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-36-summary.md` (template `Progress/_template.md`). Include: the
surfaces swept and naming convention, the focus-trap mechanism and restore proof, and both-theme
realization. Note `codebase_wiki` discrepancies (the `AccessibilityHelper` attached property and the
`AutomationProperties` / modal focus-scope wiring) for Antigravity.

### Documentation
Add `patterns/wpf-vista-accessibility.md` (the naming convention and the focus-trap recipe) per
`workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
