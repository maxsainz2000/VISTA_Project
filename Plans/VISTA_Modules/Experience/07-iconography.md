---
module: MerchSys.App
plan-id: UX-07
title: "Iconography & Visual Language — Vector Icon System Replacing Emoji Glyphs"
depends-on: [UX-05, UX-06]
estimated-files: 20
---

# Iconography & Visual Language — Vector Icon System Replacing Emoji Glyphs

## Context

UX-01..06 delivered tokens, a light/dark shell, implicit control styles, a token-migrated view set,
a shared component library, and a state/feedback layer. One conspicuous gap remains: **the app has no
vector iconography at all.** A grep for `PathGeometry` / `Path Data` / `PathIcon` / `StreamGeometry`
across `MerchSys.App` returns **zero** matches. Every icon in the product today is an **emoji or a
text glyph** baked inline into a `Content=`/`Text=` string.

The audit found pictographic glyphs in **12 view files**:

| Glyph(s) | Where (representative) | Role |
|---|---|---|
| `↻` | `OwnerDashboardView`, `APLedgerView`, `ReorderSuggestionsView`, `GoodsReceivingView` | Refresh action |
| `⚡` | `ReorderSuggestionsView` (`⚡ Generate Suggestions`) | Generate/run action |
| `✓` / `✗` | `ReorderSuggestionsView` (`✓ Accept` / `✗ Dismiss`) | Row accept / dismiss |
| `✕` | `SalesCartView` (cart line remove) | Remove/close |
| `★` | `ReorderSuggestionsView` (priority column + cell setter) | Priority star |
| `⚠` | `GoodsReceivingView` cell, `ConcurrencyConflictPrompt`, `SalesSummaryView` credit alert | Warning |
| `🔴` / `✅` | `CreditManagementView` (status column + blocked banner) | Blocked / OK status |
| `💡` | `FinancialOverviewView`, `SalesSummaryView`, `IncomeStatementView` | "What this means" insight callout |
| `📋` | `VatReturnView` (`&#x1F4CB;`) | Form/report header |
| `📦 📊 💰 📈` | `OwnerDashboardView` module tiles | Module section headers |
| `💵 📱 🏦 📋` | `SalesCartView` pay-method buttons | Cash / GCash / Bank / Credit |
| `👁` (`&#128065;`) | `LoginView` (×2) | Password reveal toggle |
| `📂` | `EmptyStatePanel` | Empty-state placeholder art |

Emoji are the wrong primitive for a "classic + premium" macOS skin:

- **They render inconsistently** across machines and font fallbacks (color-emoji vs. monochrome,
  different metrics per Windows build) — the one thing a controlled design system must not allow.
- **They ignore the theme.** A color-emoji `🔴` cannot recolor for dark mode or resolve to
  `DangerBrush`; it fights the token system UX-01 established.
- **They read as unfinished.** Against Inter + soft radii + the token palette, a `💰` is the single
  detail that breaks the premium illusion.

This plan introduces a **hand-built monoline vector icon system** — `Geometry` resources resolved to
theme brushes via `DynamicResource` — and sweeps the 12 views (plus the two UX-06 shell controls) to
use it. It is **additive and cosmetic**: no new NuGet, no behavior change, no VM/model edits.

Read `UX-00` (token contract + cross-cutting rules), `UX-05` (component library — icons live beside
it and are consumed by its buttons), and `UX-06` (added `EmptyStatePanel` + `ConcurrencyConflictPrompt`,
both of which carry glyphs this plan replaces) first.

## Prerequisites

- **UX-05** — `Themes/Components.xaml` and the `Application.xaml` merge order. Icons are referenced
  *inside* component buttons (`PrimaryButtonStyle` etc.); `Icons.xaml` merges **before** `Components.xaml`
  so component templates can reference icon geometries.
- **UX-06** — `EmptyStatePanel.xaml` (`📂`) and `ConcurrencyConflictPrompt.xaml` (`⚠`) must already
  exist to be swept.
- **UX-04** — token discipline: every icon `Fill` resolves to a brush token via `DynamicResource`.

## Scope

- **New file:** `Themes/Icons.xaml` — the keyed `Geometry` library + the reusable presentation style(s).
- **Modified:** `Application.xaml` — merge `Icons.xaml` **before** `Components.xaml`.
- **Swept:** the 12 view files above + `Views/Shell/EmptyStatePanel.xaml` +
  `Views/Shell/ConcurrencyConflictPrompt.xaml`. Code-behind (`.xaml.vb`) is **not** modified.

> **Out of scope:**
> - The **ActivityRail** nav rail. It uses bound `Abbreviation` text + the `V` wordmark, not glyphs.
>   Icon-ifying the rail would require a per-module icon mapping keyed off the navigation identity;
>   that is a worthwhile *stretch* but only if it can be done **without touching the VM/`NavigationItem`
>   model** (e.g. a XAML-side `Style`/`DataTrigger` map on the existing identity binding). If it cannot
>   be done purely in markup, leave the rail as-is and note it for a future plan. Do **not** add or
>   change a VM property to carry an icon.
> - Pure **typographic** characters that are not pictographs and read correctly in Inter — e.g. the
>   `←` axis label "← Last 6 Months" and the `×` cart-remove **may** be replaced with the vector
>   `IconChevronLeft` / `IconClose` for consistency, but a bare typographic `×`/`→` is not a defect.
>   The mandatory targets are the **emoji and decorative dingbat glyphs** in the table above.
> - Replacing currency/number formatting, status **text**, or any wording. Icons sit *beside* existing
>   labels; the labels stay.

## Deliverables

`Themes/Icons.xaml`, the `Application.xaml` merge edit, the swept views, the implementation summary,
and a wiki update.

## Specification

### 1. Mechanism (XAML-first, no new dependency)

Define each icon **once** as a theme-agnostic `Geometry` (path data drawn on a **24×24** viewbox so
all icons share a coordinate system and optical weight):

```xml
<!-- Themes/Icons.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Geometry library (24x24, monoline, ~1.5–2px visual stroke as filled paths) -->
    <Geometry x:Key="IconRefreshGeometry">M ...</Geometry>
    <Geometry x:Key="IconCheckGeometry">M ...</Geometry>
    <!-- … one per icon … -->

    <!-- Base presentation style: a Path that fills to TextPrimary and scales uniformly. -->
    <Style x:Key="IconBase" TargetType="Path">
        <Setter Property="Stretch" Value="Uniform"/>
        <Setter Property="Width" Value="16"/>
        <Setter Property="Height" Value="16"/>
        <Setter Property="Fill" Value="{DynamicResource TextPrimaryBrush}"/>
        <Setter Property="VerticalAlignment" Value="Center"/>
        <Setter Property="SnapsToDevicePixels" Value="True"/>
    </Style>
</ResourceDictionary>
```

Consumption is then a `Path` with a geometry key + a `Fill` token override where the role differs from
the default:

```xml
<!-- Inline action-button icon (inherits the button's foreground role) -->
<Path Style="{StaticResource IconBase}" Data="{StaticResource IconRefreshGeometry}"/>

<!-- Semantic status icon -->
<Path Style="{StaticResource IconBase}" Data="{StaticResource IconWarningGeometry}"
      Fill="{DynamicResource WarningBrush}"/>
```

**Decision to make and document in the summary:** whether to also ship a tiny reusable
`IconPresenter` (a `Path`-wrapping `Control` or `UserControl` with `Geometry` + `Brush` + `Size`
dependency properties) to cut boilerplate. The `Style="{StaticResource IconBase}"` + `Data=` +
optional `Fill=` form is sufficient and lowest-risk; only add a control if the repetition in buttons
warrants it. **If you add a control, it is presentation-layer only and must carry no behavior.**

> **Icons must be theme-reactive.** Every `Fill` is a `DynamicResource` brush token. Never hardcode an
> icon color; never feed a `SolidColorBrush` token into a `Color` property
> (`[[wpf-dynamicresource-brush-into-color-property]]`). The light/dark toggle must recolor every icon
> live, exactly as UX-01 mandates for all brushes.

### 2. The icon set (define in `Themes/Icons.xaml`)

Draw clean, geometric, monoline glyphs (SF-Symbols-adjacent in spirit, **not** copied — original path
data). Minimum set required to retire every glyph in the audit:

| Geometry key | Replaces | Default `Fill` role |
|---|---|---|
| `IconRefreshGeometry` | `↻` Refresh (4 views) | button foreground |
| `IconBoltGeometry` | `⚡` Generate (`ReorderSuggestionsView`) | button foreground |
| `IconCheckGeometry` | `✓` Accept, `✅` OK-status | foreground / `SuccessBrush` |
| `IconXMarkGeometry` | `✗` Dismiss, `✕` Remove/close | foreground / `DangerBrush` |
| `IconStarGeometry` | `★` priority | `WarningBrush` (filled) / `TextSecondaryBrush` (empty) |
| `IconWarningGeometry` | `⚠` (3 places) | `WarningBrush` |
| `IconCircleFillGeometry` | `🔴` Blocked status | `DangerBrush` |
| `IconLightbulbGeometry` | `💡` insight callout (3 views) | `AccentBrush` |
| `IconClipboardGeometry` | `📋` VAT-return header, `📋 Credit` pay method | `TextSecondaryBrush` / foreground |
| `IconBoxGeometry` | `📦` Purchasing tile | `TextSecondaryBrush` |
| `IconChartBarGeometry` | `📊` Inventory tile | `TextSecondaryBrush` |
| `IconCashGeometry` | `💰` Sales tile, `💵 Cash` pay method | `TextSecondaryBrush` / foreground |
| `IconChartLineGeometry` | `📈` Accounting tile | `TextSecondaryBrush` |
| `IconPhoneGeometry` | `📱 GCash` pay method | foreground |
| `IconBankGeometry` | `🏦 Bank` pay method | foreground |
| `IconEyeGeometry` / `IconEyeOffGeometry` | `👁` password reveal (`LoginView` ×2) | `TextSecondaryBrush` |
| `IconFolderOpenGeometry` | `📂` empty-state art | `TextSecondaryBrush` |
| `IconChevronLeftGeometry` *(optional)* | `←` axis label | `TextSecondaryBrush` |

> The pay-method tiles (`💵 📱 🏦 📋`) and dashboard module tiles (`📦 📊 💰 📈`) are **icon + label**
> layouts: place the `Path` before the existing text in the button/`TextBlock`'s content. Wrap the
> button content in a `StackPanel`/`DockPanel` as needed — this is a content-presentation change, not
> a behavior change. Preserve the button's command, `x:Name`, and bindings exactly.

### 3. Replacement patterns

- **Action button** (`↻ Refresh`, `⚡ Generate Suggestions`, `✓ Accept`, `✗ Dismiss`): replace the
  glyph-in-string with a `StackPanel Orientation="Horizontal"` holding a `Path` icon + the existing
  label `TextBlock`. The icon inherits the button's foreground (which the semantic button styles from
  UX-05 already set), so `✓ Accept` on a `SuccessButtonStyle` shows a white check on green.
- **DataGrid cell status** (`★`, `⚠`, `🔴`/`✅`, `✗`): the current `Setter Property="Text" Value="★"`
  inside cell templates must become a `Path` swap. Convert the `TextBlock`-with-`Style.Triggers` into a
  small `DataTemplate`/`ContentControl` (or keep a `Path` whose `Data`/`Fill` is driven by the same
  `DataTrigger`s that drive the text today). **Do not change the bound property names or the trigger
  conditions** — only what they render. This is the most delicate part of the sweep; do one column
  fully, build, eyeball both themes, then proceed.
- **Insight callout** (`💡`): replace the leading `TextBlock Text="💡"` with `IconLightbulbGeometry`
  at the same size/alignment, `Fill="{DynamicResource AccentBrush}"`.
- **Empty state** (`📂`): replace `EmptyStatePanel`'s `📂` `TextBlock` with `IconFolderOpenGeometry`
  sized ~48, `Fill="{DynamicResource TextSecondaryBrush}"`.
- **Concurrency prompt** (`⚠`): replace with `IconWarningGeometry`, `Fill="{DynamicResource WarningBrush}"`.
- **Password reveal** (`👁`): the `LoginView` reveal button toggles `HidePassword`; swap the two
  `Content="&#128065;"` for `IconEyeGeometry` / `IconEyeOffGeometry`. **Leave the toggle logic and
  bindings untouched** — only the visual content changes. (If a single button currently shows one glyph
  for both states, keep that behavior; do not invent a state the VM doesn't expose.)

### 4. Constraints

- **Behavior frozen** — no binding/command/`x:Name`/code-behind/navigation/VM/model changes. Pure
  presentation. Wrapping content in a layout panel to fit an icon + label is allowed; rewiring is not.
- **No new NuGet package.** Icons are hand-drawn `Geometry` path data. No icon-font, no MahApps,
  no FluentIcons dependency.
- **No inline hex re-introduced** — the UX-04 hex sweep must still pass; icon `Fill`s are tokens.
- **Theme-reactive** — every icon recolors on the light/dark toggle (all `Fill` via `DynamicResource`).
- **XAML/VB traps still apply:** full root prefix on any `clr-namespace` (MC3074); a `<Setter>` may
  target a `DependencyProperty` only (`[[wpf-setter-targets-clr-property-not-dependencyproperty]]`);
  never a brush token into a `Color` property (`[[wpf-dynamicresource-brush-into-color-property]]`).
- **Build gate** — `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` must finish **0 errors,
  0 warnings**. Per CLAUDE.md: if it fails, **document every error in `Progress/` and stop** — do not
  hand-patch.
- **Realization check** — a clean build does not prove a `Geometry`/`Style` resource loads. Boot the
  app and visit every swept screen (both themes) so each icon is realized at least once.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. **Glyph sweep gate:** a grep of `Views/**.xaml` for emoji/pictographic code points and the named
   entities (`&#128065;`, `&#x1F4CB;`, `&#x1F4A1;`, etc.) returns **zero** matches outside comments —
   except any pure-typographic `←`/`×`/`→` deliberately retained (list them in the summary). Provide
   the before/after counts (before = the 12 files above).
3. **Hex sweep still clean** (UX-04 criterion #2 unbroken); icon `Fill`s are tokens only.
4. Every icon recolors correctly under the **light/dark toggle** (no stuck or mismatched color); each
   semantic icon resolves to its intended token (`⚠`→`WarningBrush`, `🔴`→`DangerBrush`,
   `✅`/`✓`→`SuccessBrush`, `💡`→`AccentBrush`).
5. All swept views render with crisp icons at the same layout positions and sizes (intent-wise) in
   **both** themes; DataGrid status columns still flip with the same trigger conditions.
6. **All behavior unchanged** — spot-check `LoginView` password reveal, `SalesCartView` pay-method
   selection + line remove, `ReorderSuggestionsView` accept/dismiss, `CreditManagementView` blocked
   status, `OwnerDashboardView` refresh, `EmptyStatePanel`, `ConcurrencyConflictPrompt`.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-07-summary.md` (template `Progress/_template.md`). Include: the
final icon inventory (geometry key → what it replaced → default `Fill` role), the glyph-sweep
before/after counts, the `IconPresenter`-vs-`Path+Style` decision and why, any glyphs deliberately
left as typographic characters (and why), the ActivityRail decision (icon-ified in markup, or deferred
because it would need a VM change), views that needed content-layout wrapping, and any `codebase_wiki`
discrepancies.

### Documentation
Add `patterns/wpf-vista-iconography.md` documenting the icon mechanism (24×24 `Geometry` + `IconBase`
style + `DynamicResource` `Fill`), the "no emoji in views — vector geometry only" rule, and the
semantic-icon → token mapping, per `workflow-agent-wiki-update.md`. Update `agent_wiki/index.md` +
`log.md`.
