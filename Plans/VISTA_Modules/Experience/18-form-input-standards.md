---
module: MerchSys.App
plan-id: UX-18
title: "Form & Input UX Standards — Field Rows, Required Marking, Inline Errors, Numeric Input"
depends-on: [UX-06, UX-17]
estimated-files: 12
---

# Form & Input UX Standards — Field Rows, Required Marking, Inline Errors, Numeric Input

> **Level 1 (Basic) — item B2** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).
> Builds on B1 (UX-17 keyboard/focus) and the UX-06 validation mechanism.

## Context

UX-06 delivered the validation *mechanism* — `ObservableValidator`-based VMs with validation state and
error tracking — but **not** a consistent visual presentation. Across the editable forms
(`ProductManagementView`'s product/category editors, the AP payment dialog in `APLedgerView`, the credit
payment / add-account dialogs in `CreditManagementView`, the PO editor, the vendor editor, the shrinkage
dialog, VAT settings) each surface shows labels, required-field marking, and validation errors its own
way. There is no shared "field row" and no single required-field convention, so forms read as
inconsistent and errors appear in different places (or only as a status line / toast).

This plan standardizes the **visual grammar of forms**: a shared field-row component (label + input +
fixed inline-error slot), one required-field marker, an optional top-of-form error summary for long
forms, and numeric-input affordances (right-aligned money/qty, decimal discipline). It is **additive and
presentation-only** — it rebinds *existing* validation state into a consistent layout; it does not add,
remove, or change any validation rule, command, or business logic.

Read `UX-06` + `[[wpf-vista-state-feedback]]` (the validation mechanism), `UX-05` (component library),
and `UX-17` (tab order / focus — field rows must preserve it) first.

## Prerequisites

- **UX-06** — the `ObservableValidator` validation state on the editable VMs and the
  `INotificationService` (toasts stay for *action* outcomes; this plan governs *inline* field errors).
- **UX-17** — the keyboard/focus floor; field rows must keep tab order and focus visuals intact.
- **UX-05** — `Themes/Components.xaml` is where the shared field-row + required marker live.
- Existing deps only. **No new NuGet.**

## Scope

- **Shared field-row component / style** in `Themes/Components.xaml`: a label, the input slot, a
  consistent **required** marker, and a **fixed-position inline-error** slot beneath the input that shows
  the field's validation message when invalid.
- **Required-field convention:** one visual marker (e.g. a trailing accent asterisk or "Required" hint),
  applied uniformly to genuinely-required fields.
- **Inline error presentation:** errors render in the field-row's error slot (tokenized `DangerBrush`),
  bound to the existing per-field validation state — not as a one-off status string per form.
- **Error summary (long forms only):** an optional compact summary at the top of multi-section forms
  (PO editor, product editor) listing outstanding errors, each focusing its field on click.
- **Numeric input affordances:** right-align currency/quantity inputs and enforce sensible decimal
  formatting on entry (pairs with UX-21's display formatting).

> **Out of scope:**
> - Changing any validation *rule*, what counts as valid, or any save/command logic. This plan changes
>   *how* existing validation is shown, not *what* is validated.
> - Replacing UX-06 toasts for action results (save succeeded/failed) — those stay. Inline errors are
>   for *field-level* validity; the toast is for the *operation* outcome.
> - Re-laying-out forms beyond swapping fields onto the shared field row and adding the error slot/
>   summary. No new fields, no reordered business sections.

## Deliverables

The shared field-row component + required marker in `Components.xaml`, the per-form migration onto it,
the error-summary control for long forms, the numeric-input affordances, the implementation summary, and
a wiki update.

## Specification

### 0. Watch-items

1. **Rebind existing validation, don't re-author it.** Each editable VM already exposes validation state
   (UX-06). The field row binds to that state (`Validation.Errors` / the VM's error properties) — do not
   introduce new validators, change rule severity, or alter the save command's gating. If a form
   currently surfaces errors only via a `StatusMessage`/toast, *move the field-level part* into the
   inline slot; the operation-level toast stays.
2. **Preserve UX-17 tab order & focus.** Wrapping a field in the shared row must not change its
   `TabIndex`, break the focus ring, or make the label a tab stop (labels are `IsTabStop="False"`,
   ideally `Target`-linked to their input for mnemonic focus).
3. **Required marking must be truthful.** Mark a field required only if its validator actually requires
   it. Don't decorate optional fields. Confirm each against the VM's rules.
4. **Fixed error slot prevents layout jump.** Reserve the error-row's vertical space (or use a
   non-reflowing presenter) so showing/hiding an error doesn't shift the rest of the form — a common
   form-jank bug.
5. **Numeric inputs honor culture + UX-21.** Right-alignment and decimal handling must not hardcode a
   separator; align with the centralized formatting from UX-21 (B5). Money inputs use the peso/decimal
   convention; quantities use the qty precision.
6. **Owner read-only.** Owner does not edit; forms are a Manager surface. No Owner-facing change.
7. **No `Await` in `Catch`/`Finally`** in any code-behind helper (BC36943).

### 1. The shared field row

In `Themes/Components.xaml`, define a reusable field-row (a `Style`/template or a small `UserControl`)
exposing: label text, a required flag, the input content, and the bound error message. The error slot
sits directly beneath the input, tokenized `DangerBrush`, `FontSizeCaption`. Label `Target`-links to the
input for Alt-mnemonic focus (ties to UX-17).

### 2. Required-field convention

One marker, applied uniformly. Document the chosen treatment in the summary so future forms match.

### 3. Error summary (long forms)

For the PO editor and product editor (multi-section), add a collapsible summary that appears only when
errors exist, lists them, and focuses the offending field on click. Bind to the existing aggregate
validation state.

### 4. Numeric input

Right-align money/qty `TextBox`es; constrain to valid numeric entry; reference UX-21's formatting.

### 5. Migration sweep

Form-by-form (product/category editors → AP payment → credit payment/add-account → PO editor → vendor
editor → shrinkage dialog → VAT settings), move fields onto the shared row, wire the error slot, and add
required markers. Build 0/0 between forms. Provide a coverage table in the summary.

### 6. Constraints

- **Additive / presentation-only** — no validation-rule, command, save-gating, binding-source, or
  business change.
- **Tokens only** — UX-00 tokens / UX-05 components; no inline hex; theme-reactive.
- **VB/XAML traps** — full root prefix on `clr-namespace` (MC3074); `<Setter>` targets a
  `DependencyProperty` only; no brush token into a `Color` property; no `Await` in `Catch`/`Finally`
  (BC36943).
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — in **both** themes, open each migrated form, trigger a validation error, and
  confirm the inline error shows in the fixed slot (no layout jump), the required markers are correct,
  and tab order/focus from UX-17 still hold.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. All editable forms use the shared field row; labels, required marking, and inline-error placement are
   identical across forms.
3. Field-level validation errors render in the fixed inline slot (bound to existing UX-06 state) without
   shifting the form; long forms show an error summary that focuses fields on click.
4. Money/qty inputs are right-aligned and decimal-correct, consistent with UX-21.
5. No validation rule, save gating, or business logic changed; UX-06 action toasts still fire; UX-17 tab
   order/focus intact.
6. Owner unaffected (no edit surfaces); hex sweep clean; theme toggle recolors error text and markers.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-18-summary.md` (template `Progress/_template.md`). Include: the
field-row design, the required-marker convention, the per-form migration coverage table, where the
error-summary was added, the numeric-input approach, and the both-themes validation-error repro. Note
`codebase_wiki` discrepancies (the new field-row/error-summary components).

### Documentation
Add (or extend `wpf-vista-state-feedback.md` with) the form-standards recipe — field row, required
marker, fixed inline-error slot, error summary, numeric input — per `workflow-agent-wiki-update.md`;
update `agent_wiki/index.md` + `log.md`.
</content>
