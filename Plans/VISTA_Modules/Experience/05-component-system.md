---
module: MerchSys.App
plan-id: UX-05
title: "Component System — Consolidate Per-View Styles Into a Shared Library"
depends-on: [UX-03, UX-04]
estimated-files: 36
---

# Component System — Consolidate Per-View Styles Into a Shared Library

## Context

UX-04 centralized **colors** (every view now resolves brushes from tokens). It did **not** centralize
**components**: each view still declares its own keyed `Style`s for buttons, cards, labels, and
DataGrid row coloring. An audit of `Views/*.xaml` after UX-04 found massive duplication and drift:

| Recurring keyed style | Copies | Notes |
|---|---|---|
| `SectionHeader` | ~13 | same intent, slightly different `FontSize`/`Margin` per view |
| `ActionButton` | ~11 | two incompatible shapes — a primary-accent variant **and** a neutral base |
| `FieldLabel` / `FilterLabel` | ~9 / ~6 | caption label, redefined everywhere |
| `SecondaryButton` | ~7 | surface + separator border outline button |
| `SectionCard` / `SummaryCard` / `KpiCard` | ~6 / ~4 / ~4 | the `SurfaceBrush` + `RadiusMedium` + `CardShadow` card |
| `DangerButton` / `SuccessButton` | ~6 / ~5 | semantic fill buttons |
| `RefreshButton` / `PrimaryButton` | ~5 / ~4 | accent action buttons |
| semantic row styles (`StockRowStyle`, `GRRowStyle`, `SuggestionRowStyle`, `APRowStyle`, `ProductRowStyle`, `NearExpiryRowStyle`, `ExpiredRowStyle`, `ConfigRowStyle`) | 8 | all the same: `DataGridRow` + `DataTrigger` → semantic `Foreground` |
| right-aligned numeric cells (`RightAlignCell`, `DataGridCellRight`, `TableCellRight`) | ~6 | identical |

This duplication is not cosmetic debt — it is an **active defect generator**. The UX-04 review found
the *same* semantic-button hover bug copy-pasted into 6 views (a green/red button reverting to
accent-blue on hover) precisely because there was no single definition to fix. The next button anyone
adds will re-introduce the same class of bug.

This plan promotes the recurring elements into **one shared, keyed component library** under
`Themes/`, then sweeps every view to reference it. After UX-05 there is **one** definition of "primary
button", "card", "section header", "semantic row" — fixed once, correct everywhere.

Read `UX-00` (token contract), `UX-03` (implicit control styles — the implicit `Button`/`TextBox`/
`DataGrid` templates this library builds on), and `UX-04` (token migration) first.

## Prerequisites

- **UX-03** — implicit control styles (`Themes/Controls.xaml`, `Themes/Controls.DataGrid.xaml`) and the
  existing keyed `AccentButtonStyle` / `LinkButtonStyle`. The new semantic buttons **build on** the
  implicit `Button` template, not the OS default.
- **UX-04** — all views resolve colors via tokens; zero inline hex. This plan must not re-introduce any.

## Scope

- **New file:** `Themes/Components.xaml` — the shared keyed component library.
- **Modified:** `Application.xaml` — merge `Components.xaml` **after** `Controls.xaml` and
  `Controls.DataGrid.xaml` (so component styles can `BasedOn` the implicit control styles).
- **Swept:** all ~34 views under `Views/` (including `Views/Shell/` panels and dialogs) that currently
  declare local copies of the consolidated styles. Code-behind (`.xaml.vb`) is **not** modified.

> **Out of scope:** genuinely view-specific one-offs (e.g. `EyeButton`, `CountdownText`,
> `PayMethodButtonActive`, `ThemeToggleSwitchStyle`, `RailButtonStyle`). Leave these local — do not
> force them into the shared library. The library is for the **recurring** elements only. When in
> doubt, a style used in **≥3 views** is a library candidate; a style in 1 view stays local.

## Deliverables

`Themes/Components.xaml`, the `Application.xaml` merge edit, and the swept views. Plus the
implementation summary and a wiki update.

## Specification

### The canonical component set (define in `Themes/Components.xaml`)

All keyed, all named `…Style` per the existing `AccentButtonStyle` convention, all `BasedOn` the
implicit type style where one exists, all resolving colors via `DynamicResource` tokens only.

**Buttons** (all `BasedOn="{StaticResource {x:Type Button}}"` so they inherit the UX-03 rounded
template + transparent hover ink; set only `Background`/`Foreground`/`BorderBrush` + a correct
disabled trigger):

| New key | Replaces | Fill |
|---|---|---|
| `PrimaryButtonStyle` *(reuse existing `AccentButtonStyle`)* | `ActionButton` (accent variant), `PrimaryButton`, `RefreshButton`, `ApplyButton`, `ReloadButton`, `ConfirmButton`/`SaveButton`/`RecordButton` (accent ones) | `AccentBrush` |
| `SuccessButtonStyle` | `SuccessButton`, `ConfirmButton`/`SaveButton`/`RecordButton`/`AcceptRowButton`/`FileButton` (green ones) | `SuccessBrush` |
| `DangerButtonStyle` | `DangerButton`, `WriteOffButton`, `DismissRowButton`, `RemoveButton` | `DangerBrush` |
| `WarningButtonStyle` | `WarningButton`, `AmendButton` | `WarningBrush` |
| `SecondaryButtonStyle` | `SecondaryButton`, `NeutralButton`, `DialogCancelButton` | `SurfaceBrush` + `SeparatorBrush` outline |
| `SubtleButtonStyle` | `SmallButton`, `NavButton` | transparent / `HoverBackgroundBrush` on hover |

> **The hover rule is the whole point of this plan (see the UX-04 review).** Every semantic button's
> hover/pressed state must keep its **own** color family — never inherit an accent-blue hover. The
> agreed pattern (`patterns/wpf-vista-theming-conventions.md` §3) is: keep the semantic `Background`
> on `IsMouseOver` and apply a subtle `Opacity="0.9"` for feedback (or a dedicated
> `*HoverBrush` token if UX-00 is extended). Encode this **once** in each semantic style here.

**Segmented / toggle controls** — `FilterButton`/`FilterButtonActive`, `TabButton`, `ModeButton`,
`PeriodToggle`, `PayMethodButton(+Active)` are all "pill in a row, active one filled" controls. Provide
one `SegmentToggleStyle` (a `ToggleButton` or `RadioButton`-based style with an `IsChecked`/active
trigger) and migrate the regular cases. Leave a view's bespoke active-state visuals local only if they
genuinely differ.

**Cards & containers:**

| New key | Replaces |
|---|---|
| `CardStyle` (`Border`: `SurfaceBrush`, `RadiusMedium`, `CardShadow`, `SeparatorBrush` 1px) | `SectionCard`, `SummaryCard`, `KpiCard`, `CardPanel`, `DialogCard` |
| `AlertCardStyle` (card + left `AccentBrush`/`WarningBrush` rule) | `AlertCard`, `InterpretationBox` ("What This Means" container) |

**Typography / labels:**

| New key | Replaces |
|---|---|
| `SectionHeaderStyle` | `SectionHeader` (~13×) |
| `PageTitleStyle` / `SubtitleStyle` | `PageTitle`, `SubTitle`, `CardTitle` |
| `CaptionLabelStyle` | `FieldLabel`, `FilterLabel`, `FormLabel`, `CardLabel`, `KpiLabel`, `StatLabel`, `SectionLabel` |
| `MetricValueStyle` / `MetricValueLargeStyle` | `KpiValue`, `KpiValueSmall`, `CardValue`, `StatValue`, `TotalValue`, `GrandTotalValue`, `AlertValue` |

> Keep distinct *type roles*, not every micro-variation. If two old styles differ only by a 1px margin,
> unify them and accept the harmless shift. Document any value that changed.

**DataGrid helpers:**

| New key | Replaces |
|---|---|
| `SemanticRowStyle` (`DataGridRow` `BasedOn` the implicit row style; `DataTrigger`s map a bound status enum/bool → `Warning`/`Danger`/`Success` `Foreground`) | the 8 per-view row styles |
| `NumericCellStyle` (right-aligned cell) | `RightAlignCell`, `DataGridCellRight`, `TableCellRight*` |

> `SemanticRowStyle` reconciliation: the 8 row styles trigger off **different bound property names**
> (`ExpiryStatus`, `StockStatus`, `HasDiscrepancy`, `IsSeasonalAdjusted`, …). Do **not** force a single
> binding. Either (a) provide one base `SemanticRowStyle` with no triggers and let each view add its
> 1–2 `DataTrigger`s in a tiny `BasedOn` local style, or (b) provide a small family
> (`WarningRowStyle`, `DangerRowStyle`) the views compose. Pick whichever yields the least per-view
> code; document the choice.

### Sweep procedure (per view)

1. Delete the view's local `<Style x:Key="…">` that a library style now covers.
2. Repoint every `Style="{StaticResource OldKey}"` / `BasedOn="{StaticResource OldKey}"` reference to
   the library key.
3. Leave genuinely view-specific styles in place.
4. Build (0/0) and eyeball the view in **both** themes before moving on. Work module-by-module
   (Inventory → Purchasing → POS → Accounting → top-level/shell) to isolate regressions.

### Constraints

- **Behavior frozen** — no binding/command/`x:Name`/code-behind/navigation changes. Pure presentation.
- **No inline hex re-introduced** — the UX-04 hex sweep must still pass.
- VB.NET / XAML build traps still apply: full root prefix on any `clr-namespace` (MC3074); a `<Setter>`
  may target a `DependencyProperty` only — never a CLR property (the ScrollBar/Track trap,
  `[[wpf-setter-targets-clr-property-not-dependencyproperty]]`); never feed a `SolidColorBrush` token
  into a `Color`-typed property (`[[wpf-dynamicresource-brush-into-color-property]]`).
- **Style-sealing is a runtime check.** A clean build does not prove the library loads — boot the app
  and realize each control type (button, card, grid row) at least once.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. **De-duplication gate:** a grep for `<Style x:Key="…"` inside `Views/` returns **only** genuinely
   view-specific one-offs — none of the consolidated keys (`ActionButton`, `SectionHeader`, `FieldLabel`,
   `SecondaryButton`, `DangerButton`, `SuccessButton`, `SectionCard`/`SummaryCard`/`KpiCard`,
   `RightAlignCell`, the 8 row styles, …) remain defined in any view. Provide the before/after counts.
3. **Hex sweep still clean** (UX-04 criterion #2 unbroken).
4. Every swept view renders identically (intent-wise) in **both** light and dark; semantic buttons keep
   their color on hover (the UX-04 bug stays fixed and cannot recur — there is one definition).
5. All behavior unchanged (spot-check SalesCartView, StockDashboardView, APLedgerView,
   FinancialOverviewView, OwnerDashboardView, GoodsReceivingView).
6. Theme toggle still re-colors every view live.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-05-summary.md` (template `Progress/_template.md`). Include: the
final library inventory (key → what it replaced), the de-dup before/after grep counts, any styles
deliberately left local (and why), any values that intentionally changed during unification, views that
needed manual adjustment, and `codebase_wiki` discrepancies.

### Documentation
Update `patterns/wpf-vista-theming-conventions.md` (or add a sibling) with the component catalogue and
the "use a library style; only define local for true one-offs (≥3 views ⇒ promote)" rule, per
`workflow-agent-wiki-update.md`. Update `agent_wiki/index.md` + `log.md`.
