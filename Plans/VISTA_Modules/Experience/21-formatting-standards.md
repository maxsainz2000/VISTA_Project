---
module: MerchSys.App
plan-id: UX-21
title: "Centralised Number / Date / Currency Formatting — Peso, Dates, Grid Alignment"
depends-on: [UX-04]
estimated-files: 14
---

# Centralised Number / Date / Currency Formatting — Peso, Dates, Grid Alignment

> **Level 1 (Basic) — item B5** of [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md).

## Context

Money and dates are the most-read data in VISTA, and they are formatted as **inline literals** scattered
across the views. Confirmed: `Views/POS/SalesCartView.xaml` alone repeats `StringFormat='₱{0:N2}'` ~14×
(`UnitPrice`, `SubTotal`, `DiscountTotal`, `VatAmount`, `GrandTotal`, `LineTotal`, `ChangeAmount`, …),
plus date formats like `StringFormat='MM/dd/yyyy HH:mm'`; the same peso/date literals recur across
Accounting (`IncomeStatementView`, `VatReturnView`, `VatReliefReportView`), Purchasing (`APLedgerView`),
and the dashboards. There is **no central convention** — the peso glyph, decimal precision, and date
format are duplicated, so a change means hunting every view, and numeric grid columns are left-aligned
(harder to compare).

This plan establishes a **single source of truth** for currency / date / quantity formatting: shared
`StringFormat` resources / value converters, **right-aligned** numeric `DataGrid` columns, and one date
format app-wide. It is **cosmetic-only** — display formatting changes; no value, calculation, binding
source, or business logic changes.

Read `UX-04` (the hex-sweep / token-migration discipline — this is the same idea applied to formatting)
and the UX-08 data-ink rationale first.

## Prerequisites

- **UX-04** — the migrated views (formatting literals live alongside the now-tokenized colors).
- Existing deps only. **No new NuGet.** Converters are hand-written.

## Scope

- **Shared formatting resources / converters:** a peso currency presenter (`₱` + 2-decimal, culture-safe
  separators), a standard date format, and a quantity format — defined once (as `x:Key` `String`
  resources for `StringFormat`, and/or `IValueConverter`s where `StringFormat` can't reach, e.g.
  `Run.Text`, multi-binding, or conditional sign like the discount's `− ₱`).
- **Sweep the views** to reference the shared formats instead of inline literals.
- **Right-align numeric `DataGrid` columns** (currency/qty/count) via a shared column `ElementStyle` /
  cell style; align headers to match.

> **Out of scope:**
> - Full localization / i18n (translating labels, switching locale) — out of scope; the UI stays
>   English-only (Pro P9 / UX-44 was dropped 2026-06-07).
> - Changing any computed value, rounding rule, VAT math, or what a field means. Only its **display**
>   changes. (If on-screen rounding currently differs from the stored/computed value, preserve the existing
>   displayed result — match current output, don't "fix" it here.)
> - New columns, new fields, or layout restructure beyond alignment.

## Deliverables

The shared formatting resources/converters, the per-view sweep onto them, the right-aligned numeric grid
columns, the implementation summary, and a wiki update.

## Specification

### 0. Watch-items

1. **Match current displayed output exactly.** This is a cosmetic consolidation, not a correctness fix.
   The peso format is `₱` + `N2` today; the centralized format must produce the *same* string for the
   same value. Spot-check a few values (e.g. a cart total) before/after to confirm identical output.
2. **`StringFormat` can't cover every case — converters fill the gaps.** Plain bindings use a shared
   `StringFormat` resource. But some sites need a converter: `Run.Text` (no `StringFormat`), the signed
   discount (`StringFormat='− ₱{0:N2}'`), and any `TextBox` two-way money entry (`AmountTendered` is
   `StringFormat='N2'` two-way — changing its format must not break parse-back). Identify these and use a
   converter or keep a documented exception; never break two-way input.
3. **Culture safety.** Don't hardcode `.`/`,` separators; rely on `N2`/the converter honoring the
   running culture (the app currently relies on the default culture's separators — preserve that).
4. **Right-alignment must not break virtualization or selection.** Apply alignment via a shared
   `ElementStyle`/cell style on numeric `DataGridColumn`s; don't replace `DataGridTextColumn`s with
   custom templates that defeat the UX-03 `DataGrid` styling or virtualization. Header alignment follows
   the column.
5. **No inline hex / token discipline preserved.** This sweep touches the same views as UX-04 — keep the
   hex sweep at zero; do not introduce literals while removing format literals.
6. **Owner reads these most.** The reports Owner consumes (Income Statement, VAT, dashboards) are prime
   beneficiaries — verify their numbers read uniformly and right-aligned after the sweep.
7. **VB/XAML traps** — full root prefix on the converters' `clr-namespace` (MC3074); a `<Setter>` targets
   a `DependencyProperty` only.

### 1. The shared formats

Define currency/date/qty as shared resources (e.g. in `Themes/Tokens.xaml` or a new
`Themes/Formats.xaml` merged in `Application.xaml`), plus a `Converters/` folder for the converter cases
(peso, signed-peso). Document each format string and when to use the resource vs the converter.

### 2. The sweep

View-by-view (POS → Accounting → Purchasing → Inventory → dashboards), replace inline `₱{0:N2}` /
date-format literals with the shared format/converter. Preserve two-way money inputs (watch-item #2).
Build 0/0 between modules. Provide a coverage table (view → sites replaced) in the summary.

### 3. Grid alignment

Apply right-alignment to numeric columns via a shared column style; verify against the UX-03 `DataGrid`
look and virtualization.

### 4. Constraints

- **Cosmetic-only** — identical displayed output; no value/calculation/binding-source/business change;
  two-way inputs still parse.
- **No new NuGet** — converters hand-written.
- **Tokens / no inline hex** preserved (UX-04 sweep stays at zero).
- **VB/XAML traps** — MC3074 full root prefix; `<Setter>` targets a `DependencyProperty` only.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — in **both** themes: spot-check that key money/date values render identically to
  before, numeric grid columns are right-aligned, and the `AmountTendered` (and any other two-way money
  input) still accepts and commits entry correctly.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Currency / date / quantity formatting is defined **once** and referenced everywhere; no inline
   `₱{0:N2}` (or duplicated date-format) literals remain in `Views/`.
3. Displayed values are byte-identical to before the sweep (spot-checked); changing the peso/date format
   is now a one-place edit.
4. Numeric `DataGrid` columns (currency/qty/count) are right-aligned with matching header alignment,
   without breaking the UX-03 grid styling or virtualization.
5. Two-way money inputs still parse and commit; no value/calculation/business change.
6. Hex sweep clean; theme toggle unaffected (formatting is theme-agnostic but must not regress tokens).

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-21-summary.md` (template `Progress/_template.md`). Include: the
format definitions, the resource-vs-converter decision per case, the per-view sweep coverage table, the
two-way-input handling, the before/after value spot-check, and the grid-alignment approach. Note
`codebase_wiki` discrepancies (the new formats/converters).

### Documentation
Add `patterns/wpf-vista-formatting.md` (the central format resources, the `StringFormat`-vs-converter
rule, the two-way-input caveat, grid right-alignment) per `workflow-agent-wiki-update.md`; update
`agent_wiki/index.md` + `log.md`.
</content>
