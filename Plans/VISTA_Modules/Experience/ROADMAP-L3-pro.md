---
module: MerchSys.App
plan-id: ROADMAP-L3
title: "Road to UI/UX Perfection — Level 3: Pro (Differentiators & Depth)"
depends-on: [ROADMAP, ROADMAP-L2]
estimated-files: 0
---

# Level 3 — Pro (differentiators & deep refinement)

> Master map & governance: **[ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md)**.
> Level 3 is the "perfection" tier — accessibility completeness, performance at scale,
> personalization, and the craft details that separate a good app from an exceptional one. These are
> larger and more ambitious; each builds on a Basic/Advanced primitive (Pro accessibility extends the
> Basic keyboard floor; advanced viz extends the sparklines).
> This level can be re-prioritised freely — it is the long tail of excellence, not a fixed sequence.

Each entry: **What · Why · Research · Codebase grounding · Scope · Owner · Done-when.**

> **Dropped 2026-06-06 (user decision — "adds noise, not an improvement"):** the high-contrast theme
> (formerly part of P1) and three preference/polish items — **P4 System-Aware Theming (UX-39)**,
> **P5 Density Modes (UX-40)**, and **P10 Live Motion Propagation (UX-45)** — were removed. Their plan
> files are deleted; the P-numbers are intentionally left as gaps so the remaining plans' `item PN`
> references (P6–P8) stay valid. Light/Dark remain the only themes.
>
> **Dropped 2026-06-07 (user decision):** **P9 Localization Framework (UX-44)** was also removed — the
> UI stays English-only. Its plan file and implementation were deleted; the P9 number is left as a gap.

---

## P1 — Full Accessibility / WCAG 2.2 AA
*Slot: **UX-36** (plan generated) · Depends-on: B1*

> **Scope amended (2026-06-06):** the high-contrast theme variant was dropped at user request (judged
> not beneficial / added UI noise). This item is now naming + modal focus traps only; Light/Dark remain
> the only themes. The naming + focus-trap work shipped; the contrast theme was reverted.

- **What.** Take accessibility from "floor" (B1's keyboard + focus) to **complete**:
  `AutomationProperties.Name`/`HelpText` on every interactive control, correct UI Automation
  roles/labels for screen readers, and focus **traps** inside modal dialogs (focus can't escape behind
  the overlay).
- **Why.** Full operability by assistive tech is both an ethical baseline and a
  procurement/compliance differentiator.
- **Research.** WCAG 2.2 Level AA (full), Section 508; Microsoft UI Automation guidance.
- **Codebase grounding.** Only ~21 `AutomationProperties`-class hits exist today (mostly
  `Themes/Controls.xaml`). Modal overlays (`CommandPalette`, `ConcurrencyConflictPrompt`, B4's confirm
  dialog) are where focus traps attach.
- **Scope.** An `AutomationProperties` sweep; focus-scope/trap on modals. Largely additive.
- **Owner.** Same — Owner benefits equally.
- **Done-when.** A screen reader announces every control meaningfully; modals trap focus.

---

## P2 — Performance & Perceived Performance
*Slot: **UX-37** (plan generated) · Depends-on: UX-15*

- **What.** UI **virtualization** on long lists/grids (recycling `VirtualizingStackPanel`),
  incremental/async loading everywhere a list can grow, and **optimistic UI** on writes (reflect the
  change immediately, reconcile on confirm) layered on the existing concurrency model.
- **Why.** ~50 SKUs is small today, but transaction history, audit logs, and price history grow
  unbounded; virtualization and async keep the UI fluid as data scales.
- **Research.** Microsoft WPF virtualization guidance; perceived-performance / optimistic-UI research
  (show the result before the round-trip completes).
- **Codebase grounding.** UX-15 standardised async load (`LoadDataAsync`, `IsBusy`); the command
  palette already debounces search at 250ms (`CommandPaletteViewModel.OnQueryChanged`) — a reusable
  pattern. Optimistic UI must respect the `DbUpdateConcurrencyException` flow from UX-14 (reconcile,
  never silently overwrite).
- **Scope.** Confirm/enable virtualization on big grids; audit async coverage; pilot optimistic UI on
  one safe write path. Read paths use the raw `MySqlConnector` reader (EF Core 10 `ToListAsync` bug).
- **Owner.** Same — large reports load faster for Owner too.
- **Done-when.** Long lists scroll without lag; no UI freeze on load; optimistic writes feel instant
  while still surfacing conflicts correctly.

---

## P3 — Personalization & Workspace Memory
*Slot: **UX-38** (plan generated) · Depends-on: UX-01, B7*

- **What.** Remember the **user's** workspace: last-viewed screen restored on relaunch, a
  recently-viewed list, favorite/pinned screens, and persisted personal preferences (theme already;
  add density and default module).
- **Why.** A daily-use tool should feel personal and pick up where the user left off — reduces the
  cold-start tax every morning.
- **Research.** NN/g heuristic #7 *Flexibility & efficiency* (accelerators for frequent users); Apple
  HIG state-restoration conventions.
- **Codebase grounding.** Per-laptop persistence exists for theme (`Services/Theming/ThemeService.vb`)
  and will exist for window placement (B7) — extend the same storage to a small user-prefs service.
  Recently-viewed/favorites integrate with `MainWindowViewModel.AllNavigableItems` (the nav
  source-of-truth from UX-16) and could feed the command palette.
- **Scope.** A user-preferences service; restore-last-view on load; favorites surfaced in the palette
  and/or sidebar. Additive.
- **Owner.** Owner gets their own remembered workspace (read-only screens).
- **Done-when.** Relaunch restores the last screen; favorites/recents are available and persist.

---

## P6 — Advanced Data Visualization
*Slot: **UX-41** (plan generated) · Depends-on: UX-12*

- **What.** Make the dashboards' charts **interactive**: hover tooltips on sparkline/bar points,
  drill-down from a KPI to its detail view, and period selectors (7/30/90-day) on trend cards.
- **Why.** UX-12 added static sparklines/deltas; the next step is letting the Owner *explore* trends
  rather than only glance at them.
- **Research.** Tufte data-ink + interaction-on-demand; NN/g dashboard drill-down patterns;
  Shneiderman's "overview first, zoom and filter, details on demand."
- **Codebase grounding.** `Views/Shell/Sparkline.xaml` and `DeltaIndicator.xaml` (UX-12) are the
  base; charts are hand-built with `Rectangle`/`Path` (per UX-08 — no charting NuGet). Interactivity
  is additive markup + read-only VM queries; drill-down reuses `NavigateCommand`.
- **Scope.** Add hover/tooltip + click-to-drill to the existing chart primitives; period selectors
  bound to additive read-only queries. No new packages.
- **Owner.** Primary beneficiary — Owner is the read-only KPI consumer.
- **Done-when.** Trend cards respond to hover, support a period switch, and drill into detail.

---

## P7 — Print & Export UX
*Slot: **UX-42** (plan generated) · Depends-on: B5*

- **What.** A proper **BIR Official-Receipt** print template, plus report **export** (PDF/CSV) with a
  print-preview for the Accounting reports and POS receipts.
- **Why.** Printing/export is a real operational need (BIR compliance, owner record-keeping) and the
  current presentation isn't print-shaped.
- **Research.** In-repo `LLM_Wiki/wiki/concepts/bir-compliance.md` (OR rules, VAT handling); standard
  print-preview conventions. *Note:* UX-08 listed a BIR OR template as a non-goal **for that sub-epic**
  — the roadmap reclaims it here as a deliberate, scoped Pro item.
- **Codebase grounding.** POS already renders a receipt block (`Views/POS/SalesCartView.xaml` —
  `CurrentReceipt.BusinessTIN/IssueDate/TotalAmount/VatAmount`); Accounting reports
  (`IncomeStatementView`, `VatReturnView`, `VatReliefReportView`) are the export targets. Builds on
  B5's centralised currency/date formatting so printed numbers match on-screen.
- **Scope.** A print/flow-document template for the OR; export commands for reports. This is the most
  "feature-like" roadmap item — confirm scope before generating its plan.
- **Owner.** Owner can print/export read-only reports; OR printing is a Manager/POS action.
- **Done-when.** A compliant OR prints; key reports export to PDF/CSV with a preview.

---

## P8 — Living Design-System Gallery
*Slot: **UX-43** (plan generated) · Depends-on: UX-05, UX-07*

- **What.** A Developer-Tools screen that renders **every** design token, component, and state — a
  Storybook-equivalent: all colors, the type ramp, every shared component (BusyOverlay, EmptyState,
  ErrorState, DeltaIndicator, Sparkline, the B-level field row/confirm dialog) shown live in both
  themes.
- **Why.** A living gallery is how a design system stays coherent: it documents what exists (killing
  the duplication this roadmap fights), and it's a visual regression surface — boot it in both themes
  to catch a token that fails to realize (the UX-00 realization-check, automated into one screen).
- **Research.** Design-system / component-library best practice (Storybook, Fluent/Material component
  galleries).
- **Codebase grounding.** `Views/Shell/Modules/DeveloperToolsPanel.xaml` is the existing host;
  components live in `Themes/Components.xaml` and `Views/Shell/`; tokens in `Themes/Tokens.xaml`/
  `Light.xaml`/`Dark.xaml`. Pure presentation — renders what already exists.
- **Scope.** One new Developer-Tools view enumerating tokens/components/states. No logic.
- **Owner.** Developer Tools is not an Owner surface.
- **Done-when.** A single screen shows every token and component in both themes; it's the canonical
  reference for "does this already exist?"

---

## Exit criteria for Level 3

Pro has **no hard finish line** — it is the standing tail of excellence. Practically, the app reaches
"UI/UX perfection" for VISTA's purpose when P1 (accessibility), P2 (performance), and P3
(personalization) are shipped; P6–P8 are high-value refinements pursued as appetite and need dictate.
When an item here is genuinely complete and no open slot remains worth doing, the roadmap has served
its purpose — and any new idea must be **added here, with grounding, before it is built.**
</content>
