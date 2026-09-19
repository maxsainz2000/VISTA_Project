---
module: MerchSys.App
plan-id: UX-46
title: "Comparative Income Statement & Report Readability — Prior-Period Column, Δ, and P&L Legibility"
depends-on: [UX-12, UX-42]
estimated-files: 5
---

# Comparative Income Statement & Report Readability — Prior-Period Column, Δ, and P&L Legibility

> **Level 3 (Pro) — item P11** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). Turns the single-column
> Income Statement into a **comparative** statement (prior-period amounts + period-over-period Δ) and
> fixes the four readability defects on the same screen. The prior-period figures are **already
> computed and discarded** by the VM, so the headline change is near-free; everything else is restyle
> + relayout. Additive read-only only — no service, contract, or write change.

## Context

The Income Statement (`Views/Accounting/IncomeStatementView.xaml` + `IncomeStatementViewModel`) renders
one period in a narrow column pinned to the top-left of a full-width canvas. Four problems compound:

1. **No comparison.** The "What This Means" banner asserts *"This is lower than the previous period
   (14.6%)"*, but the statement never shows that period — the claim is unverifiable on-screen, and the
   prior-period numbers that *would* show it are computed and thrown away (see grounding).
2. **Stranded layout.** The P&L grid is `MaxWidth="580" HorizontalAlignment="Left"`
   (`IncomeStatementView.xaml:185`); on a normal window ~70% of the card is empty.
3. **The story-line is the faintest text.** COGS — the number that drives the entire margin story —
   uses `LineLabelMuted`/`LineAmountMuted` → `TextSecondaryBrush` (`xaml:229-234`), while a ₱0 row is
   styled identically. Material magnitude should not be the lightest ink on the page.
4. **Ambiguous OpEx roll-up.** The service folds shrinkage **into** operating expenses
   (`IncomeStatementService.vb:129 — operatingExpenses = shrinkageLoss + otherOpEx`), but the UI shows
   the parent total first then a single indented child, with no "Other Operating" line. With real data
   (OpEx ₱500 / Shrinkage ₱300) a reader cannot tell whether the total is 500 or 800.
5. **Unfriendly month picker** shows "6", not "June" (`AvailableMonths` is raw `Enumerable.Range(1,12)`,
   `IncomeStatementViewModel.vb:133`).

This is a **comparative financial-statement** convention (two-period P&L) plus a Tufte/NN/g readability
pass. Everything is **additive read-only**: derived display properties off data the VM already has, plus
markup. No metric is recomputed, stored, or written.

## Prerequisites

- **UX-12** — the `DeltaIndicator` presentation control (`Views/Shell/`) with `InvertSemantics`. The Δ
  column reuses it; **verify its exact DP names** (`Percent` / `Direction` / `InvertSemantics`) from the
  UX-12 implementation before wiring.
- **UX-42** — the existing CSV/PDF export on this view (`IncomeStatementView.xaml.vb`
  `ExportCsv_Click` / `ExportPdf_Click`). Export parity is in scope only if that builder is cleanly
  row-driven (see Scope C).
- **UX-21 (B5)** — centralised currency/date formatting; reuse it for the new prior/Δ cells so printed
  and on-screen numbers match.
- Existing deps only. **No new NuGet.**

## Scope

### A. Prior-period comparison column + Δ
The VM already computes the full prior-period `IncomeStatementDto` for **all three** period types
(`prevResult` in `LoadDataAsync`, ~lines 318/325/329) and currently keeps only its `GrossMarginPercent`.
Surface the rest as **additive read-only** display properties — `PrevNetSalesDisplay`, `PrevCOGSDisplay`,
`PrevGrossProfitDisplay`, `PrevOperatingExpensesDisplay`, `PrevShrinkageLossDisplay`,
`PrevNetIncomeDisplay`, and a `PrevPeriodLabel` (e.g. "May 2026" / "Q1 2026" / "FY 2025") — plus a
per-line Δ (`Percent` + `Direction`) for the `DeltaIndicator`. Lay the grid out as
**Label | Current | Prior | Δ**. Where the prior period is empty/zero (no baseline), render the Δ cell
blank rather than a misleading ∞/0% (mirror UX-12's "skip when no cheap prior data" discipline).

### B. P&L legibility pass (same view)
1. **Un-mute COGS.** Promote the COGS line to the primary label/amount styles (its magnitude is
   material). Keep muting for the genuinely-zero structural rows.
2. **Disambiguate the OpEx roll-up.** Present components first, then the subtotal: an indented
   **Shrinkage Loss** and a derived **Other Operating** (`= OperatingExpenses − ShrinkageLoss`,
   computed read-only in the VM), then a **Total Operating Expenses** subtotal — or, if kept inline,
   relabel the parent "Operating Expenses (incl. shrinkage)". Either way the parent/child math must be
   unambiguous on screen.
3. **Document-width layout.** Replace `MaxWidth="580" HorizontalAlignment="Left"` with a centered,
   constrained "paper" treatment that absorbs the now-wider comparative grid; keep the existing
   `ScrollViewer` and the UX-11 layout-resilience behaviour at small widths.
4. **Friendly month labels.** Show month **names** in the picker without breaking the underlying
   `SelectedMonth` **int** round-trip that drive loading and export (e.g. expose an options list of
   `{Number, Name}` and bind `SelectedValue`/`SelectedValuePath` to the int).

### C. Export parity (conditional)
If the UX-42 CSV/PDF builder is row-driven and cleanly extensible, add the prior-period column + Δ so the
export matches the on-screen statement (B5 "printed = on-screen" principle). **If it would require
structural rework, defer it and document the deferral** in the summary — do not half-break export.

> **Out of scope:**
> - Any change to how a metric is computed, stored, or written; the prior column is **derived
>   read-only** from the DTO the service already returns.
> - Changing `IncomeStatementService`, the DTOs, or any contract/signature (the prior statement is
>   already produced by `GenerateMonthly/Quarterly/AnnualAsync`).
> - The Per-Product Margins tab, and any new charting NuGet.
> - Adding a third comparison period, YTD columns, or budget/variance — two-period only.

## Specification

### 0. Watch-items

1. **Additive read-only.** New VM members are read-only and sourced from the already-fetched
   `prevResult` / `statement` DTOs. **No existing VM property, command, or the service is modified.**
   The diff is new members + markup.
2. **No new query.** `prevResult` already covers monthly/quarterly/annual — do **not** add a data call.
   No raw-reader work is needed here (the service already returns the prior DTO).
3. **Δ direction semantics.** Set `DeltaIndicator.InvertSemantics` per line: **rising is bad** for COGS,
   Operating Expenses, Shrinkage (invert); **rising is good** for Net Sales, Gross Profit, Net Income.
4. **Don't break the month round-trip.** The friendly label must not change the `SelectedMonth` integer
   the loader and the CSV/PDF export consume.
5. **Theme-safe + no inline hex.** New cells/labels recolor via tokens (`TextPrimaryBrush`,
   `SuccessBrush`/`DangerBrush` via the indicator) in both themes; the UX-04 hex sweep stays at zero.
6. **VB/XAML traps** — no `Await` in `Catch`/`Finally` (BC36943); full `clr-namespace` root prefix on
   any new reference (MC3074); reserved-keyword-safe names; never feed a brush token into a `Color`
   property; `<Setter>` targets a `DependencyProperty` only.

### 1. Constraints

- Reuse the UX-12 `DeltaIndicator`; add no parallel delta primitive.
- No new NuGet. No inline hex. **Role model** — Owner is the primary read-only reader; nothing here adds
  a write affordance.
- **Build gate** — `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` must finish **0 errors,
  0 warnings**. On failure, document in `Progress/` and stop (per CLAUDE.md).
- **Realization check (both themes):** boot the Income Statement in Light **and** Dark; confirm the
  prior column + Δ render with correct sign/color (inverted for COGS/OpEx/Shrinkage), COGS is legible,
  the OpEx/Shrinkage subtotal math reads unambiguously, the card is centered/document-width, and the
  month picker shows names while still loading the correct month.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. The statement shows **Current | Prior | Δ** for every line, sourced from the already-computed
   `prevResult` (no new data call), with `DeltaIndicator` direction/color correct — **inverted** for
   COGS, Operating Expenses, and Shrinkage. No baseline → blank Δ (no misleading 0%/∞).
3. COGS is rendered in primary (non-muted) text; the Operating-Expenses / Shrinkage relationship is
   unambiguous (components-then-subtotal or an explicit "incl. shrinkage" label, with a derived "Other
   Operating" line).
4. The statement is laid out as a centered, constrained document (no longer `MaxWidth=580` left-pinned),
   and the month picker shows month **names** while `SelectedMonth` still drives load/export correctly.
5. Export reflects the prior column + Δ **or** the deferral is documented with the reason.
6. Diff is **additive only** — no existing VM member, the service, any DTO, or any write/concurrency path
   changed; hex sweep stays clean; theme toggle recolors the Δ live.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-46-summary.md` (template `Progress/_template.md`). Include: the new
read-only VM members and which DTO field each derives from; the `InvertSemantics` mapping per line; the
OpEx/Shrinkage presentation chosen (components-then-subtotal vs relabel) and the "Other Operating"
derivation; the layout change; the month-label mechanism (and proof the int round-trip survives); the
export decision (done vs deferred + why); and both-theme realization. Note any `codebase_wiki`
discrepancies for Antigravity.

### Documentation
Add `patterns/wpf-vista-comparative-report.md` (surfacing an already-computed prior-period DTO as a
comparison column, reusing `DeltaIndicator`/`InvertSemantics` for per-line deltas, the friendly-label-
without-breaking-the-value-binding technique) per `workflow-agent-wiki-update.md`; update
`agent_wiki/index.md` + `log.md`.
