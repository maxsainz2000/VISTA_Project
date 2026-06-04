---
module: MerchSys.App
plan-id: UX-17
title: "Keyboard & Focus Foundation — Tab Order, Focus Visuals, Default/Cancel, Mnemonics"
depends-on: [UX-03, UX-05]
estimated-files: 16
---

# Keyboard & Focus Foundation — Tab Order, Focus Visuals, Default/Cancel, Mnemonics

> **Level 1 (Basic) — item B1** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md). This is
> the first plan of the Basic correctness floor; B2 (forms) and B4 (confirm dialogs) build on it.

## Context

VISTA is mouse-driven. A repo-wide search for `AutomationProperties | KeyboardNavigation | TabIndex |
IsDefault | IsCancel | AccessText` returns only ~21 hits across 5 files — and the `KeyboardNavigation.*`
ones in `Themes/Controls.xaml` are *inside* control templates (ComboBox/ScrollBar internals), not an
app-wide policy. There is **no shared `FocusVisualStyle`**, so focus is shown with WPF's default dotted
rectangle (or nothing) rather than a theme-aware ring. Dialogs do not wire Enter/Esc: the real modal
`Window` (`Views/Shell/ConcurrencyConflictPrompt.xaml`) and the many **in-view overlay "dialogs"**
(payment, return, PO/vendor/category editors — toggled by `IsXxxDialogVisible` flags, dismissed by
`Cancel*Command`s that merely flip a bool) all require a mouse click to confirm or cancel.

This plan establishes the **keyboard & focus floor**: a theme-aware focus visual, sane tab order on
every form, Enter-to-confirm / Esc-to-cancel on every dialog (both shapes), and access-key mnemonics on
primary actions. It is **chrome + input wiring only** — no business logic, binding, command, or
navigation behavior changes. The shell already proves the window-level input-binding pattern
(`MainWindow.xaml` `<Window.InputBindings>`: Ctrl+1..4, Ctrl+D0, Ctrl+K from UX-16); this extends that
discipline down into forms and dialogs.

Read `UX-03` (implicit control styles), `UX-16` (the window input-binding + `PreviewKeyDown` gating
pattern), `[[wpf-mainwindow-not-shell-window]]`, and `[[feedback-vbnet-await-catch]]` first.

## Prerequisites

- **UX-03** — implicit control styles in `Themes/Controls*.xaml` (the `FocusVisualStyle` attaches here
  and is inherited app-wide).
- **UX-05 / UX-00** — components + tokens (`AccentBrush`, radii) for the focus ring.
- **UX-16** — the established `<Window.InputBindings>` + gated `MainWindow_PreviewKeyDown` precedent for
  in-view modal key handling.
- Existing deps only. **No new NuGet.**

## Scope

- **Shared focus visual:** one tokenized `FocusVisualStyle` (a theme-aware ring using `AccentBrush`,
  `RadiusSmall`) applied via the implicit control styles so every focusable control inherits it.
- **Tab order:** verify/assign logical `TabIndex` (reading order: top→bottom, left→right) on every form
  and dialog; ensure non-interactive decoration is `IsTabStop="False"`.
- **Default / Cancel on dialogs (both shapes):**
  - *Real modal windows* (`ConcurrencyConflictPrompt`, and B4's future confirm dialog): set
    `IsDefault="True"` on the confirm button and `IsCancel="True"` on cancel — WPF then routes Enter/Esc.
  - *In-view overlay "dialogs"* (payment / return / editor panels): add scoped key handling so Enter
    invokes the panel's confirm command and Esc invokes its `Cancel*Command`, **without** the keys
    bubbling to the window-level bindings or the content beneath (mark `Handled`, mirror UX-16's gating).
- **Mnemonics & Enter-to-submit:** access keys (`_Save`, `_Cancel`, `_Delete`) on primary action
  buttons; single-purpose forms submit on Enter where unambiguous.

> **Out of scope:**
> - Full `AutomationProperties`/screen-reader coverage and a high-contrast theme — that is **Pro P1**
>   (a later, larger accessibility plan). This plan delivers the *keyboard + visible-focus* floor only.
> - Changing what any command does, any validation logic, or any layout beyond focusability attributes.
> - The POS keyboard fast-path (Advanced A7) — this plan makes forms keyboard-operable generally; the
>   cart-specific hotkey flow is a separate, later plan that builds on this foundation.

## Deliverables

The shared `FocusVisualStyle`, the tab-order/`IsDefault`/`IsCancel`/mnemonic sweep across forms and
dialogs, the in-view-dialog Enter/Esc handling, the implementation summary, and a wiki update.

## Specification

### 0. Watch-items (the risks)

1. **Two dialog shapes, two mechanisms — do not assume all dialogs are `Window`s.** Only
   `ConcurrencyConflictPrompt` is a real modal `Window` where `IsDefault`/`IsCancel` Just Work. The
   payment (`APLedgerViewModel`/`CreditManagementViewModel`), return (`TransactionHistoryViewModel`),
   and editor (`PurchaseOrderListViewModel`, `VendorListViewModel`, `ProductManagementViewModel`)
   dialogs are **overlay panels inside a view**, shown/hidden by an `IsXxxDialogVisible` bool and
   dismissed by a `Cancel*Command` that flips that bool. For these, `IsCancel`/`IsDefault` do nothing
   useful — wire Enter/Esc via a scoped `KeyBinding`/`PreviewKeyDown` that targets the panel's existing
   confirm/cancel commands, and mark the event `Handled` so it does not reach the window-level
   Ctrl-bindings or the list underneath. Audit each dialog and classify it before wiring.
2. **`Cancel*Command` ≠ destructive.** Most `Cancel*Command`s just close a panel (verified). Esc must
   map to *that* (dismiss), never to a delete/void. Do not confuse panel-dismiss with B4's destructive
   confirm.
3. **Focus visual must be theme-aware and realize in both palettes.** Author the ring with
   `DynamicResource AccentBrush` — never feed a brush token into a `Color` property
   (`[[wpf-dynamicresource-brush-into-color-property]]`). A clean build does not prove the style
   realizes; boot both Light and Dark and tab through a form to confirm the ring is visible on each.
4. **Don't trap focus or break existing shortcuts.** Adding `TabIndex` must not create a focus loop or
   skip a control; the new in-view Esc/Enter handlers must not swallow keys when no dialog is open
   (mirror UX-16's `If IsXxxDialogVisible Then …` gate). Re-verify Ctrl+1..4 / Ctrl+K still fire from a
   form field.
5. **Mnemonic collisions.** Access keys within one screen/dialog must be unique (no two `_S`). Pick
   non-colliding letters; verify Alt+letter reaches the intended button.
6. **Owner read-only.** Owner screens still get focus visuals + tab navigation (they read/scroll), but
   have no submit/cancel dialogs to wire. No write affordances appear.
7. **No `Await` in `Catch`/`Finally`** in any code-behind key handler that calls async commands
   (BC36943) — invoke the command (fire-and-forget via the command's own async) rather than awaiting
   inside a catch.

### 1. The shared focus visual

Define one `Style x:Key="AppFocusVisual"` (a `Control` template drawing a 1–2px `AccentBrush` ring with
`RadiusSmall`, inset slightly) in `Themes/Controls.xaml`, and set `FocusVisualStyle` on the implicit
Button/TextBox/ListBoxItem/ComboBox/etc. styles so it is inherited app-wide. Tokens only; honors the
live light/dark swap.

### 2. Tab-order & focusability sweep

Module-by-module (Inventory → Purchasing → POS → Accounting → shell), walk each form/dialog: assign
`TabIndex` in reading order, set decorative elements `IsTabStop="False"`, confirm the first meaningful
field receives focus when a dialog opens (`FocusManager.FocusedElement` or code-behind `.Focus()` on
show). Build 0/0 between modules.

### 3. Default / Cancel / Enter-to-submit

For each dialog, apply the shape-appropriate mechanism from watch-item #1. For real windows: `IsDefault`
/`IsCancel`. For in-view panels: scoped Enter→confirm-command, Esc→`Cancel*Command`, `Handled=True`,
gated on the panel's visibility flag. Single-field forms (e.g. a search/SKU box) submit on Enter where
the action is unambiguous.

### 4. Mnemonics

Add `_`-prefixed access keys to primary action buttons (Save/Cancel/Delete/Submit/Record Payment),
unique within each surface.

### 5. Constraints

- **Behavior-frozen** — only focusability attributes, the focus visual, mnemonics, and Enter/Esc
  routing change. No command/validation/binding/navigation/business-logic change.
- **Tokens only** — UX-00 tokens; no inline hex; theme-reactive.
- **VB/XAML traps** — full root prefix on any `clr-namespace` (MC3074); a `<Setter>` targets a
  `DependencyProperty` only (`[[wpf-setter-targets-clr-property-not-dependencyproperty]]`); no brush
  token into a `Color` property; no `Await` in `Catch`/`Finally` (BC36943).
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — in **both** themes: tab through one form per module (focus ring visible);
  open each dialog class and confirm Enter confirms / Esc cancels; Alt+mnemonic reaches its button;
  Ctrl+1..4 / Ctrl+K still fire from inside a form.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A shared theme-aware focus ring is visible on every focusable control in both Light and Dark.
3. Every form/dialog has a logical tab order; the first meaningful field is focused on open.
4. Every dialog confirms on Enter and cancels on Esc — real windows via `IsDefault`/`IsCancel`, in-view
   panels via scoped key handling that does not leak to window bindings or the underlying content.
5. Primary actions have unique access-key mnemonics; existing Ctrl shortcuts still work from form fields.
6. No business-logic/command/validation/navigation change; Owner read-only screens gain focus/tab but no
   write affordances; hex sweep clean; theme toggle recolors the focus ring.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-17-summary.md` (template `Progress/_template.md`). Include: the
dialog-shape classification table (window vs in-view) and how each got Enter/Esc, the focus-visual
definition, the per-module tab-order coverage, the mnemonic map (collision check), and the both-themes
realization result. Note any `codebase_wiki` discrepancies.

### Documentation
Add `patterns/wpf-vista-keyboard-focus.md` (the shared focus visual, the two-dialog-shape Enter/Esc
recipe, the gate-on-visibility rule, mnemonic uniqueness) per `workflow-agent-wiki-update.md`; update
`agent_wiki/index.md` + `log.md`.
</content>
