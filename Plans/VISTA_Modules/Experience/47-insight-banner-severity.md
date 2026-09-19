---
module: MerchSys.App
plan-id: UX-47
title: "Severity-Aware Insight Banners — Tone the “What This Means” Callout to the Signal"
depends-on: [UX-06, UX-07]
estimated-files: 11
---

# Severity-Aware Insight Banners — Tone the "What This Means" Callout to the Signal

> **Level 3 (Pro) — item P12** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). The plain-language "What This
> Means" callout currently renders every message — good news, neutral, and outright warnings — in the
> same calm accent blue. This plan gives it **severity**: info / positive / warning tone (icon + accent),
> driven by the signal the service has *already* computed, via one reusable `InsightBanner` control.
> Additive and presentation-only; the existing interpretation text and thresholds are untouched.

## Context

`WhatThisMeansService` already detects problems — it owns `MarginDropWarningThreshold = 3D` and
`CreditWarningThreshold = 30D`, appends warning sentences ("Warning: N customer accounts… overdue",
"…higher than normal — monitor your accounts receivable"), and even has a `GenerateMarginAlert` with a
"⚠" prefix. But the **presentation throws that signal away**: every "What This Means" banner is a
hand-rolled `Border` hard-wired to `AccentBrush` (e.g. `IncomeStatementView.xaml:143`). The result is
that a 14.6%→4.0% gross-margin collapse looks exactly as calm as "your margin is stable."

NN/g #1 *Visibility of system status* says status must be legible at a glance; WCAG **1.4.1 Use of
Color** says severity must be carried by **more than hue** — so this is a banner-*shape* change (icon +
label + accent), not a recolor. The phrase "What This Means" is duplicated across **four** views
(IncomeStatement, FinancialOverview, SalesSummary, VatReturn), so this also de-duplicates them into one
control.

This is **additive presentation**: severity is *derived* from signals the service already computes; no
threshold is invented, no interpretation text changes, no business logic moves.

## Prerequisites

- **UX-06** — the state/feedback component family (`Views/Shell/` — BusyOverlay/EmptyStatePanel/
  ErrorStatePanel pattern) the new `InsightBanner` joins.
- **UX-07** — `Themes/Icons.xaml` + `IconBase`; the banner swaps an info/positive/warning geometry
  (reuse the existing lightbulb for info and an existing warning/alert geometry; add one 24×24 geometry
  only if a warning glyph is genuinely missing).
- `WarningBrush` / `SuccessBrush` / `AccentBrush` tokens (already in `Light.xaml`/`Dark.xaml` 37-39) —
  **no new color token.**

## Scope

### A. `InsightBanner` reusable control
A presentation-only Shell control (`Views/Shell/InsightBanner.xaml` + `.xaml.vb`) that renders the
existing banner shape (icon · "What This Means" title · wrapped body text) with `DependencyProperty`
inputs: `Title`, `Text`, and `Severity`. `Severity` selects the icon geometry **and** the accent
brush — `Info`→`AccentBrush`, `Positive`→`SuccessBrush`, `Warning`→`WarningBrush` — all via
`DynamicResource` so theme swap stays live. **No behavior, no data access, no commands.**

### B. `InsightSeverity` enum in SharedKernel
Define `Public Enum InsightSeverity { Info = 0, Positive = 1, Warning = 2 }` in **`MerchSys.SharedKernel`**.
It must live here, not in `MerchSys.App`: module ViewModels (e.g. `IncomeStatementViewModel` in
`MerchSys.Accounting`) reference **only SharedKernel**, so an App-side enum would be invisible to them,
while `MerchSys.App` already references SharedKernel and can bind the control's DP to it.

### C. Additive severity on `IWhatThisMeansService`
Add a **new** method per interpreted surface (e.g. `GetIncomeStatementSeverity(dto, previousMargin)`,
`GetOverviewSeverity(dto)`, `GetSalesSummarySeverity(dto)`) returning `InsightSeverity`, computed from
the **same** thresholds/signals that already drive the warning sentences (`MarginDropWarningThreshold`,
`CreditWarningThreshold`, overdue-AR count, low-stock count). This keeps tone and text in sync from one
source of truth. **Do not change the signature of the existing `Generate…Interpretation` String
methods** — severity is purely additive.

### D. Migrate the four banners + expose VM severity
Replace each hand-rolled banner `Border` with `<shell:InsightBanner Severity="{Binding …Severity}" …/>`,
and add a read-only `…Severity As InsightSeverity` property to each backing VM, set alongside the
existing `WhatThisMeansText` in its load path.

| View | VM (verify exact name) | Severity source | Warning when… |
|---|---|---|---|
| `Accounting/IncomeStatementView` | `IncomeStatementViewModel` | `GetIncomeStatementSeverity` | margin drop ≥ `MarginDropWarningThreshold`, or net loss |
| `Accounting/FinancialOverviewView` | `FinancialOverviewViewModel` | `GetOverviewSeverity` | overdue AR > 0, or low-stock alerts > 0 |
| `Accounting/SalesSummaryView` | (verify) `SalesSummaryViewModel` | `GetSalesSummarySeverity` | credit share ≥ `CreditWarningThreshold` |
| `Accounting/VatReturnView` | (verify) `VatReturnViewModel` | **Info only** | — (no existing signal; do not invent one) |

> **Out of scope:**
> - Any new threshold, KPI, or change to the interpretation **text** — severity reads existing signals.
> - A new color token, or a `Positive`/`Warning` mapping for surfaces that compute no such signal
>   (default `Info`).
> - Toasts/notifications, animation of the banner, or dismissing it — tone change only.
> - Touching write/concurrency paths or any non-Accounting surface.

## Specification

### 0. Watch-items

1. **Module layering is the main trap.** `InsightSeverity` **must** be in SharedKernel — a module VM
   cannot reference an App enum (the boundary rule: modules reference SharedKernel only). The control in
   `MerchSys.App` binds its `Severity` DP to that SharedKernel enum.
2. **Contract discipline.** Add **new** severity methods; the existing String-returning interpretation
   methods keep their signatures (a return-type change would be a breaking contract edit).
3. **WCAG 1.4.1 — not color alone.** `Severity` changes the **icon** (and optionally a short label),
   not just the accent brush, so the state survives for color-blind users and in grayscale.
4. **No invented signals.** Severity derives only from data/thresholds the service already uses; any
   surface without such a signal defaults to `Info`. VatReturn stays `Info`.
5. **Theme-safe + no inline hex.** All three accents resolve via `DynamicResource`; theme toggle must
   recolor a warning banner live. UX-04 hex sweep stays at zero.
6. **VB/XAML traps** — full `clr-namespace` root prefix on the new control (MC3074); correct
   `DependencyProperty` registration with a `PropertyChangedCallback` if the icon/brush is swapped in
   code; no `Await` in `Catch`/`Finally` (BC36943); `<Setter>` → `DependencyProperty` only; never feed a
   brush token into a `Color` property.

### 1. Constraints

- One `InsightBanner` replaces all four hand-rolled banners — no per-view banner variants.
- No new NuGet, no new color token, no inline hex.
- **MVVM/architecture** — the control holds no data logic; severity is computed in the Accounting
  service / exposed by the module VM; cross-module reads (if any) stay on MediatR query contracts.
- **Role model** — Owner reads all four surfaces (read-only) and is the primary beneficiary; severity is
  observe-only, no write affordance.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both themes):** force each surface to a warning condition (e.g. select a period
  whose margin dropped past threshold) and confirm the banner shows amber + warning icon in Light **and**
  Dark; confirm neutral periods show calm info; confirm a positive/improving period shows the positive
  tone where applicable.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. `InsightBanner` is a reusable Shell control with `Title`/`Text`/`Severity` DPs, theme-reactive
   brushes, and **no** behavior or data access; `InsightSeverity` lives in SharedKernel.
3. All four "What This Means" banners are migrated to `InsightBanner`; a margin drop past
   `MarginDropWarningThreshold` (and the other existing signals) renders **warning** tone, neutral
   renders **info**, and severity is conveyed by **icon + accent**, not hue alone.
4. Severity is exposed by an **additive** service method + read-only VM property; the existing
   interpretation **text methods and all thresholds are unchanged**.
5. VatReturn (no computed signal) defaults to Info; no new threshold/KPI/color token introduced.
6. Hex sweep clean; theme toggle recolors a warning banner live in both themes.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-47-summary.md` (template `Progress/_template.md`). Include: the
`InsightBanner` DP API; where `InsightSeverity` was placed and **why SharedKernel** (the layering
reason); the new severity methods and the exact existing signal/threshold each maps to; the
icon-per-severity choices (and any geometry added); the per-view severity wiring (VM property + binding);
and both-theme realization including a forced-warning screenshot-equivalent description. Note
`codebase_wiki` discrepancies (new Shell control, new SharedKernel enum, additive service methods).

### Documentation
Add `patterns/wpf-vista-insight-banner.md` (severity-by-signal callout: SharedKernel enum for the
module-boundary, additive service method so tone and text share one source of truth, WCAG-1.4.1
icon-not-hue rule, token-only theming) per `workflow-agent-wiki-update.md`; update `agent_wiki/index.md`
+ `log.md`.
