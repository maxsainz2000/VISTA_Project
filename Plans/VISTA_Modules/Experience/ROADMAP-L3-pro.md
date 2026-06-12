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

## P11 — Comparative Income Statement & Report Readability
*Slot: **UX-46** (plan generated) · Depends-on: UX-12*

- **What.** Turn the single-column Income Statement into a **comparative** statement — a prior-period
  amount column and a period-over-period Δ beside each line (reusing the UX-12 `DeltaIndicator` with
  `InvertSemantics` so a rising COGS reads as bad) — plus a readability pass on the same screen:
  un-mute the COGS line (today the faintest text on the page, yet it drives the whole margin story),
  disambiguate the Operating-Expenses / Shrinkage roll-up, give the statement a centered
  document-width layout instead of stranding it top-left, and show "June" instead of "6" in the
  month picker.
- **Why.** The "What This Means" narrative already asserts "lower than the previous period (14.6%)"
  but the statement never shows that period — the reader cannot verify the claim, and ~70% of the
  canvas is empty. A comparative P&L is the standard accounting presentation and the single
  highest-value, lowest-cost change here: the prior-period figures are **already computed and then
  discarded.**
- **Research.** Comparative financial-statement convention (two-period P&L); Tufte (comparison /
  small-multiples — a number means little without a baseline); NN/g #6 *Recognition over recall* and
  the F-pattern (don't mute the line that carries the story).
- **Codebase grounding.** `IncomeStatementViewModel.LoadDataAsync` already fetches the **entire**
  prior-period `IncomeStatementDto` (`prevResult`, ~line 318/325/329) and uses only
  `GrossMarginPercent` (~line 332), discarding the rest. The P&L grid is
  `MaxWidth="580" HorizontalAlignment="Left"` (`IncomeStatementView.xaml:185`); COGS uses
  `LineLabelMuted`/`LineAmountMuted` (xaml:229-234); the service rolls shrinkage **into** OpEx
  (`IncomeStatementService.vb:129`) but the UI shows the parent total first then one child;
  `AvailableMonths` is raw `Enumerable.Range(1,12)` (`IncomeStatementViewModel.vb:133`). UX-12's
  `DeltaIndicator` (`Views/Shell/`) is the Δ primitive.
- **Scope.** Additive read-only display properties for the prior column + Δ (from the already-fetched
  `prevResult`); a derived "Other Operating = OperatingExpenses − ShrinkageLoss" line; restyle +
  relayout in the view; friendly month labels without breaking the underlying `SelectedMonth` int
  round-trip; export parity **if** the UX-42 export is cleanly row-driven (else defer + document). No
  service, contract, or write change.
- **Owner.** Owner is the primary reader of this report (read-only) and the main beneficiary of the
  comparison.
- **Done-when.** The statement shows current vs prior amounts with correctly-signed Δ in both themes;
  COGS is legible; the OpEx/Shrinkage math is unambiguous; the report reads as a centered document.

---

## P12 — Severity-Aware Insight Banners
*Slot: **UX-47** (plan generated) · Depends-on: UX-06, UX-07*

- **What.** Make the plain-language **"What This Means"** callout change tone with the signal it is
  reporting: calm **info** (accent) for neutral/positive narratives, **warning** (amber) when the
  service has already detected a problem (margin drop past threshold, overdue AR, high credit %, low
  stock). Replace the four hand-rolled banners with one reusable `InsightBanner` (icon + accent swap
  driven by a `Severity` value) so tone is consistent and defined in one place.
- **Why.** A 14.6%→4.0% margin collapse currently renders in the same calm blue as good news — the
  most important signal on the screen is visually indistinguishable from reassurance. The machinery to
  detect severity already exists; only the presentation ignores it.
- **Research.** NN/g #1 *Visibility of system status* (status must read at a glance); WCAG **1.4.1 Use
  of Color** (severity must also change icon/label, never hue alone) — which is why this is a
  banner-shape change, not merely a recolor.
- **Codebase grounding.** `WhatThisMeansService` already owns the thresholds
  (`MarginDropWarningThreshold = 3D`, `CreditWarningThreshold = 30D`) and a `GenerateMarginAlert` with
  a "⚠" prefix — but `GenerateIncomeStatementInterpretation` only appends a sentence and every banner
  border is hard-wired to `AccentBrush` (e.g. `IncomeStatementView.xaml:143`). The phrase "What This
  Means" is hand-rolled in four views (IncomeStatement, FinancialOverview, SalesSummary, VatReturn).
  `WarningBrush`/`SuccessBrush`/`AccentBrush` tokens already exist (`Light.xaml`/`Dark.xaml` 37-39);
  `IconBase` (UX-07) supplies the icon.
- **Scope.** A new `InsightBanner` Shell control (DP-driven, presentation-only); an `InsightSeverity`
  enum in **SharedKernel** (so module VMs, which reference only SharedKernel, can expose it); an
  **additive** severity method on `IWhatThisMeansService` that reuses the existing thresholds (tone and
  warning-sentence share one source of truth — the String methods are unchanged); migrate the four
  banners. Default **Info** wherever no signal is computed — invent no new thresholds.
- **Owner.** Owner reads these reports and benefits most from at-a-glance severity.
- **Done-when.** Each "What This Means" banner renders info/positive/warning by the computed signal,
  with a matching icon (not color alone), in both themes; a margin drop past threshold shows amber.

## P13 — Animated Login Experience
*Slot: **UX-48** (plan generated) · Depends-on: UX-01, UX-07, UX-25*

- **What.** Turn the static login card into VISTA's signature moment: a pure-vector **Philippine
  farm panorama that follows the real local time of day** (five fixed bands — dawn/day/golden/
  dusk/night, crossfaded; equatorial PH makes fixed bands honest), with ambient motion (drifting
  clouds, swaying palay, fireflies at night, a glowing Villon storefront sign) and parallax; plus a
  **reactive carabao mascot** ("Tanod", wearing a salakot) on the card that watches the username
  caret, tips its hat over its eyes for the password, peeks on reveal, thinks while authenticating,
  celebrates success, and shakes off rejection — alongside two functional wins: the app's **first
  CapsLock warning** and the macOS-style **wrong-password card shake**.
- **Why.** Every prior level is shipped; the usability floor is paid for. Emotional-design research
  places exactly one well-crafted delight feature above that floor, and the login — first
  touchpoint of every session (peak-end rule) — is its highest-leverage surface. The brand fit
  (carabao + salakot + rice fields for an agri-retail store) makes it identity, not decoration.
- **Research.** Aarron Walter's hierarchy of user needs (pleasurable atop usable); Don Norman,
  visceral level of emotional design; Darin Senneff's "Yeti" sign-in form (canonical reactive-avatar
  login, adapted); Apple macOS *dynamic desktop* (time-of-day environment precedent) + HIG motion
  restraint; NN/g animation-usability guidance; WCAG 2.2.2 / 2.3.3 honored via the existing UX-25
  OS reduced-motion mechanism (no in-app toggle, per the lean decision record).
- **Codebase grounding.** `Views/LoginView.xaml` is a static 420px card; `LoginViewModel` already
  exposes every needed hook (`Username` with `PropertyChanged`, `IsLoggingIn`, `ShowPassword`,
  `ShowPasswordChange`, `LoginSucceeded`) — the only VM delta is one additive `LoginAttemptFailed`
  event. UX-25's `MotionEnabled` propagation (`Application.xaml.vb → UpdateMotionSettings`) is the
  static-mode switch; `CardStyle`/tokens (UX-01) and `Icons.xaml` (UX-07) carry the card and badge.
  Grep confirms **no CapsLock handling exists anywhere**. `LoginView` is DI-transient and *hidden*
  (not closed) on success — animation lifecycle teardown is therefore a hard requirement, specced.
- **Scope.** New `Views/Login/` controls (`DynamicSceneCanvas`, `CarabaoAvatar`,
  `LoginScenePhase`) + `Themes/LoginScene.xaml` (all scene hex tokenized there); LoginView
  recomposition to a fixed ~900×660 stage; view-side wiring; capped ≤700ms success beat in
  `Application.xaml.vb`. Stock WPF only — no NuGet, no bitmaps, no new settings; auth flow and DA6
  semantics byte-identical.
- **Owner.** Pre-auth surface — identical for Manager, Owner, and Developer.
- **Done-when.** All five phases render with ambient motion in both themes; the nine avatar states
  fire per the wiring matrix; CapsLock badge + reject shake work; reduced-motion/low-tier yields a
  fully functional static login; no storyboard or timer survives on a hidden login instance.

## P13a — Login Scene Redesign (storefront hero shot)
*Slot: **UX-49** (plan generated) · Depends-on: UX-48*

- **What.** Replace UX-48's artwork wholesale while keeping its engineering: a **dusk storefront
  hero shot** (the Villon facade at blue hour — lit bracket sign, glowing doorway and display
  window, scalloped awning, bulb string, tricycle/sack/cat silhouettes, wet-pavement reflections)
  rendered as three lighting moods (Dusk hero / Evening / LateNight) instead of five literal time
  bands; a **Filipina tindera avatar** ("Aling Vi") replacing the carabao with the identical pose
  API (hands-over-eyes for the password); and a **frosted-glass login card** (in-app
  VisualBrush + blur — not the OS Acrylic/Mica declined for the shell) with a one-shot entrance
  choreography ("the store lights up for you").
- **Why.** Owner review of UX-48 (2026-06-12, with screenshot): no "wow" — flat single-color
  layers, EvenOdd-holed clouds, a crop-clipped sign, a mascot that reads childish. The login is
  being graded as a design showpiece; the artwork must match the quality of the machinery.
- **Research.** Same emotional-design basis as P13, plus: atmospheric/value-grouped silhouette
  illustration practice; game title-screen art direction (fixed hero hour beats honest-but-bland
  hours); glassmorphism-with-fallback per the existing UX-25 reduced-motion contract.
- **Codebase grounding.** UX-48's phase timer, crossfade pipeline, tracked-clock lifecycle,
  parallax, static mode, CapsLock badge, and shake all carry over byte-compatible;
  `LoginViewModel` / `Application.xaml.vb` are untouched. Defect record D1–D8 in the plan binds
  the quality gates (FillRule=Nonzero, safe-area x∈[260,1340], light contracts, silhouette-first
  props, eye catchlights).
- **Scope.** Rewrite `LoginScene.xaml`, `LoginScenePhase.vb`, `DynamicSceneCanvas.xaml(.vb)`;
  new `TinderaAvatar.xaml(.vb)` (delete `CarabaoAvatar`); glass-card surgery in `LoginView`;
  two new theme tokens. No NuGet, no bitmaps, no settings, no VM/auth change.
- **Owner.** Pre-auth surface — identical for Manager, Owner, Developer.
- **Done-when.** Plan UX-49 acceptance criteria 1–9 (three moods each demo-grade, glass card with
  contrast + static fallback, 8 tindera poses on the UX-48 wiring matrix, teardown clean).

---

## Exit criteria for Level 3

Pro has **no hard finish line** — it is the standing tail of excellence. Practically, the app reaches
"UI/UX perfection" for VISTA's purpose when P1 (accessibility), P2 (performance), and P3
(personalization) are shipped; P6–P8 are high-value refinements pursued as appetite and need dictate.
When an item here is genuinely complete and no open slot remains worth doing, the roadmap has served
its purpose — and any new idea must be **added here, with grounding, before it is built.**
</content>
