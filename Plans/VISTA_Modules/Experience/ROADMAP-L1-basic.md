---
module: MerchSys.App
plan-id: ROADMAP-L1
title: "Road to UI/UX Perfection — Level 1: Basic (Correctness Floor)"
depends-on: [ROADMAP]
estimated-files: 0
---

# Level 1 — Basic (the WPF correctness floor)

> Master map & governance: **[ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md)**.
> These are the table-stakes WPF UX practices a competent line-of-business app is *expected* to get
> right. They are mostly small, high-certainty, and broad-reach. Several adjacent practices are
> **already shipped** (theming, state feedback, iconography, layout resilience) — see the baseline
> table in the master file. What remains below are the genuine, codebase-verified gaps.

Each entry: **What · Why · Research · Codebase grounding · Scope · Owner · Done-when.**

> **Status (2026-06-04):** all seven Basic items have implementable plans drafted —
> B1→`17-keyboard-focus-foundation.md`, B2→`18-form-input-standards.md`,
> B3→`19-actionable-empty-states.md`, B4→`20-confirmation-dialogs.md`,
> B5→`21-formatting-standards.md`, B6→`22-tooltips-affordance.md`,
> B7→`23-window-state-persistence.md`. Recommended order: **UX-17 → UX-18/UX-20 → the rest**
> (UX-18 and UX-20 depend on UX-17; UX-19/21/22/23 are independent). Items are checked off here when
> *implemented*, not when planned.

---

## B1 — Keyboard & Focus Foundation
*Proposed slot: UX-17 · Depends-on: UX-16*

- **What.** A logical, predictable keyboard layer across the app: sane `TabIndex` order on every
  form; a theme-aware **focus visual** (a visible focus adorner that uses `AccentBrush`, not the
  default dotted rectangle); `IsDefault="True"` (Enter) and `IsCancel="True"` (Esc) on every dialog's
  confirm/cancel buttons; access-key mnemonics (`_Save`, `_Cancel`) on primary actions;
  Enter-to-submit on single-purpose forms.
- **Why.** Keyboard operability is the single biggest table-stakes gap. A retail back-office is used
  all day by staff who want to keep hands on the keyboard; mouse-only forms are slow and feel unfinished.
- **Research.** WCAG 2.2 §2.1.1 (Keyboard) and §2.4.7 (Focus Visible); NN/g heuristic #7
  *Flexibility & efficiency of use*; Fitts's/Hick's Law (keyboard removes pointer travel entirely).
- **Codebase grounding.** A repo-wide search for `AutomationProperties|KeyboardNavigation|TabIndex|`
  `IsDefault|IsCancel|AccessText` returns only **21 hits across 5 files** — concentrated in
  `Themes/Controls.xaml` and a couple of views. Dialogs such as `Views/Shell/ConcurrencyConflictPrompt.xaml`
  and the AP payment / PO editor dialogs do not wire `IsDefault`/`IsCancel`. The shell already proves
  the input-binding pattern (`MainWindow.xaml` `<Window.InputBindings>` with Ctrl+1..4, Ctrl+K) — this
  extends that discipline down into forms and dialogs.
- **Scope.** A shared `FocusVisualStyle` token in `Themes/`; a sweep adding `TabIndex`,
  `IsDefault`/`IsCancel`, and mnemonics to forms and dialogs. No logic change.
- **Owner.** Read-only screens still get focus visuals and tab navigation; no submit buttons to wire.
- **Done-when.** Every dialog confirms on Enter / cancels on Esc; Tab walks every form in reading
  order; focus is always visibly indicated in both themes.

---

## B2 — Form & Input UX Standards
*Proposed slot: UX-18 · Depends-on: B1, UX-06*

- **What.** A consistent visual grammar for forms: label placement, a single convention for marking
  **required** fields, inline validation messages rendered in a fixed position beneath each field, an
  optional error **summary** at the top of long forms, and numeric input affordances (right-aligned
  money/qty, decimal discipline).
- **Why.** UX-06 delivered the *mechanism* (`ObservableValidator` + validation state) but not a
  *standardised presentation*. Forms currently differ in how they show required fields and errors.
- **Research.** NN/g heuristic #5 *Error prevention* and #9 *Help users recognise/recover from
  errors*; WCAG 2.2 §3.3.1/§3.3.2 (Error Identification, Labels/Instructions).
- **Codebase grounding.** Forms in `Views/Inventory/ProductManagementView.xaml`,
  `Views/Purchasing/APLedgerView.xaml` (payment dialog), and the PO editor each present validation
  differently; the validation source of truth already exists in the module ViewModels.
- **Scope.** A shared "field row" component (label + input + inline error slot) and a required-field
  marker style in `Themes/Components.xaml`; rebind existing validation state into it. Additive.
- **Owner.** Owner does not edit; not applicable to read-only screens.
- **Done-when.** All editable forms use the shared field row; required + error presentation is
  identical everywhere.

---

## B3 — Actionable Empty States
*Proposed slot: UX-19 · Depends-on: UX-06*

- **What.** Give `EmptyStatePanel` an optional **call-to-action**: an `ICommand` + label DP so an
  empty list can offer the next step ("Add your first product →", "Create a purchase order →")
  instead of dead-ending.
- **Why.** An empty screen with no path forward is a small but constant friction. Turning empty
  states into entry points is a classic onboarding/efficiency win.
- **Research.** NN/g *empty states as opportunities* guidance; heuristic #10 *Help & documentation*
  (the lightest form of help is a contextual next action).
- **Codebase grounding.** Confirmed: `Views/Shell/EmptyStatePanel.xaml.vb` exposes only `Title` and
  `Description` dependency properties — **no command**. It is already consumed widely (e.g. inside
  `CommandPalette.xaml`). Adding an optional CTA is purely additive and backward-compatible (panels
  that set no command render exactly as today).
- **Scope.** Add `ActionCommand`/`ActionText` DPs + a button (hidden when no command); wire CTAs on
  the highest-traffic Manager lists. The CTA is **role-gated** — bound to a command the VM only
  exposes for Manager.
- **Owner.** No CTA rendered (command is null for Owner) — the panel stays informational.
- **Done-when.** Empty Manager lists offer a relevant next action; Owner sees the informational panel
  unchanged.

---

## B4 — Confirmation & Destructive-Action Dialogs
*Proposed slot: UX-20 · Depends-on: B1*

- **What.** One standard confirmation dialog for irreversible/financial actions (delete, void,
  submit-PO, record-payment): consistent button order, danger styling on the destructive button
  (`DangerBrush`), a clear consequence sentence, and "type to confirm" for the most dangerous actions.
- **Why.** Destructive actions are scattered and confirm inconsistently (or not at all). A single
  pattern prevents costly mistakes on financial/inventory records that are soft-deleted (`IsDeleted`).
- **Research.** NN/g heuristic #3 *User control & freedom* and #5 *Error prevention*; the
  confirm-before-destroy convention.
- **Codebase grounding.** Soft-delete (`IsDeleted`) and void paths exist across modules; the conflict
  dialog (`ConcurrencyConflictPrompt`) already proves the modal-over-shell presentation pattern to reuse.
- **Scope.** A shared `ConfirmationDialog` component + a small presenter service (mirrors
  `IConflictPresenter`). Route existing destructive commands through it. No change to the underlying
  commands.
- **Owner.** Owner has no destructive commands; never sees these dialogs.
- **Done-when.** Every delete/void/submit/payment routes through the shared confirm; the dialog
  styling is identical app-wide.

---

## B5 — Centralised Number / Date / Currency Formatting
*Proposed slot: UX-21 · Depends-on: —*

- **What.** A single source of truth for formatting: shared `StringFormat` tokens / value converters
  for peso currency, dates, and quantities; **right-aligned** numeric columns in every `DataGrid`;
  consistent date format app-wide.
- **Why.** Money and dates are the most-read data in the app; inconsistent or left-aligned numbers
  read as unfinished and slow comparison.
- **Research.** Data-ink / scannability (Tufte, applied in the UX-08 rubric); NN/g tabular-data
  alignment guidance (align numerals on the decimal, right-justify).
- **Codebase grounding.** Confirmed: currency is formatted as inline literals
  (`StringFormat='₱{0:N2}'`) repeated ~14× in `Views/POS/SalesCartView.xaml` alone, and similarly
  across Accounting/Purchasing views — there is no central convention. One place to define it does
  not exist yet.
- **Scope.** Define currency/date/qty formats as shared resources (or a `PesoConverter`); sweep views
  to reference them; set `ElementStyle`/alignment on numeric `DataGridColumn`s. Cosmetic only.
- **Owner.** Same formatting on all read-only screens (improves the reports Owner reads most).
- **Done-when.** No inline `₱{0:N2}` literals remain; all numeric grid columns are right-aligned;
  changing the peso/date format is a one-line edit.

---

## B6 — Tooltips & Affordance
*Proposed slot: UX-22 · Depends-on: UX-07*

- **What.** Tooltips on every icon-only control (activity-rail module buttons, toolbar icon buttons,
  status indicators) and short helper hints where an input's purpose isn't self-evident.
- **Why.** UX-07 made the app icon-rich; icon-only controls without labels are guesswork until
  tooltips make them learnable.
- **Research.** NN/g heuristic #6 *Recognition rather than recall*; icon-usability findings (icons
  need text support to be unambiguous).
- **Codebase grounding.** `Views/Shell/ActivityRail.xaml` renders icon-only module buttons;
  `Themes/Icons.xaml` is the vector set from UX-07. Tooltips are largely absent.
- **Scope.** Add `ToolTip` to icon-only controls (text sourced from the existing nav display names);
  optional shared tooltip style. Cosmetic only.
- **Owner.** Same — tooltips aid the read-only user too.
- **Done-when.** Every icon-only control has a tooltip; no control's purpose is guesswork.

---

## B7 — Window-State Persistence
*Proposed slot: UX-23 · Depends-on: UX-01*

- **What.** Remember the window's size, position, and maximized/normal state per laptop and restore
  it on next launch (instead of forcing `WindowState="Maximized"` every time).
- **Why.** Respecting where the user left the window is a small courtesy users expect from a desktop
  app, especially across the 4 client laptops with different screens.
- **Research.** Apple HIG / Microsoft desktop conventions — restore the user's last window
  arrangement; NN/g heuristic #7 *Flexibility & efficiency*.
- **Codebase grounding.** `MainWindow.xaml` hardcodes `WindowState="Maximized"`. The per-laptop
  persistence pattern already exists for the theme choice in `Services/Theming/ThemeService.vb` —
  reuse that storage approach.
- **Scope.** A tiny window-placement service that saves on close / restores on load, persisted the
  same way the theme preference is. No layout change.
- **Owner.** Same behaviour for both roles.
- **Done-when.** Relaunching restores the prior window size/position/state; first run still opens
  sensibly maximized.

---

## Exit criteria for Level 1

Basic is "done" when B1–B7 are shipped: the app is fully keyboard-operable with visible focus,
forms validate and confirm consistently, empty states lead somewhere, numbers and dates read
uniformly, icons are learnable, and the window remembers itself. At that point the app clears the
correctness floor and **[Level 2 — Advanced](ROADMAP-L2-advanced.md)** becomes the active level.
</content>
