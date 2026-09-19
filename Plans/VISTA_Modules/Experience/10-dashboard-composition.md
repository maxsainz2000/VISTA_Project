---
module: MerchSys.App
plan-id: UX-10
title: "Dashboard Composition Rollout — Primary-Metric Hierarchy, Reading Order, Data-Ink"
depends-on: [UX-09]
estimated-files: 11
---

# Dashboard Composition Rollout — Primary-Metric Hierarchy, Reading Order, Data-Ink

## Context

UX-09 shipped the metric-hierarchy card family (`PrimaryMetricCardStyle` / `SecondaryMetricCardStyle` /
`TertiaryMetricStyle` + matching value/label roles). This plan **applies** it: every KPI-band view gets
a single hero metric (40-30-20-10), a corrected reading order, and data-ink trims. It is the single
biggest lever on the audit scores — Owner (58) and Financial Overview (68) are expected to rise into
the low-80s purely from composition.

This is **Tier 1 — behavior frozen.** Every change is layout/weighting/ordering or rebinding a control
to a property the VM already exposes. No VM/service/query/command/`x:Name` edits.

The KPI-band views in scope (from the UX-08 audit):

| View | Current band | Hero metric to promote | Composition fix |
|---|---|---|---|
| `OwnerDashboardView` | 2×2 of 4 equal module cards (~15 equal values) | Today's Revenue (or Net Income) | Hero strip + demote module cards to secondary; chunk each card's 3–4 values. |
| `Accounting/FinancialOverviewView` | 8 equal cards in one row | Revenue | 1 hero + 2–3 secondary + compact tertiary strip for the rest; **move Alerts above the chart**. |
| `POS/DailySummaryView` | 4 equal cards | Total Sales | Promote Total Sales to hero; others secondary. |
| `Accounting/SalesSummaryView` | 4 equal cards | Total Sales | Promote hero; others secondary. |
| `Inventory/StockDashboardView` | 5 equal cards | Critical Stockout Risk | Promote the risk card to hero; **de-dup** triple status encoding. |
| `Inventory/ExpiryMonitorView` | 3 equal cards | Total Value at Risk | Promote hero; others secondary. |
| `POS/CreditManagementView` | 3 equal cards | Total Outstanding AR | Promote hero; others secondary. |
| `Inventory/ShrinkageView` | 3 cards incl. "Last Refreshed" | Period Shrinkage Value | Promote hero; **remove the "Last Refreshed" card** (move to a caption). |
| `Accounting/VatReturnView` | 5 equal cards | VAT Payable | Promote VAT Payable to hero; others secondary. |
| `Accounting/VatReliefReportView` | net-VAT band + 2 bucket cards | Net VAT Payable (already emphasized) | Confirm hero styling via the shared style; light touch. |

Read `UX-08` (standards) and `UX-09` (the card vocabulary you are applying) first.

## Prerequisites

- **UX-09** — the metric-hierarchy styles exist and resolve in both themes.
- **UX-05/UX-07** — the component library and icon system the cards already use.

## Scope

- **Swept (10 view files above):** repoint each view's KPI band to the UX-09 hierarchy styles; promote
  the named hero; reorder Financial Overview's Alerts; trim data-ink. Code-behind (`.xaml.vb`) is
  **not** modified.
- **No new files.** No VM edits.

> **Out of scope:**
> - `ScrollViewer` wrapping and filter-toolbar reflow — those are **UX-11** (this plan must not touch
>   the root container or filter `Grid`s, to keep the two plans' edits disjoint within shared files).
> - Trend/delta indicators and sparklines — **UX-12**.
> - The non-KPI tabs (lists, forms, plain reports: PO List, Vendor*, Goods Receiving, Product
>   Management, VAT Settings, Tamper Audit, Income Statement, Transaction History, Sales Cart, Price
>   History). They have no KPI band to re-weight; UX-11 handles their resilience.
> - Inventing a hero metric the VM doesn't already expose. Every hero binds to an **existing**
>   property. If a view's "best" hero isn't on the VM, pick the most decision-relevant property that
>   **is**, and note the ideal in the summary for UX-12/UX-13.

## Deliverables

The 10 swept views, the implementation summary, and a wiki update.

## Specification

### 1. Hero promotion (per view)

For each view: choose the hero card (table above), change its `Border` to `PrimaryMetricCardStyle` and
its value `TextBlock` to `PrimaryMetricValueStyle`; place it **first in reading order** (top-left of the
band). Re-style the remaining cards as `SecondaryMetricCardStyle` + `SecondaryMetricValueStyle`. Keep
every existing binding, `StringFormat`, semantic `Foreground` override, and command exactly as-is —
only the wrapping style and the card's position change.

### 2. Density correction — Financial Overview's 8-card row

The single row of 8 equal cards exceeds the 5–9 comfort band and has zero hierarchy. Recompose as:

- **Hero:** Revenue (Today or MTD — pick the one already emphasized) as `PrimaryMetricCardStyle`.
- **Secondary (2–3):** Gross Margin %, AR Outstanding, AP Outstanding.
- **Tertiary strip:** YTD Revenue, Inventory Value, and the VAT tile as compact `TertiaryMetricStyle`
  items in a single sub-row (smaller, lighter) — present but clearly subordinate.

Preserve the `VatPayableTile`'s existing click-to-navigate binding verbatim; only its size/weight
changes.

### 3. Reading-order fix — Financial Overview

The Alerts panel is currently `DockPanel.Dock="Bottom"`, rendering **below** the 6-month chart. Move it
to dock **above** the chart (directly under the KPI band / "What This Means" box) so exceptions sit
above the fold, per the UX-08 layer-order rule. This is a re-dock of an existing `Border` — no binding
change.

### 4. Data-ink trims

- **Stock dashboard:** the stock status is encoded three times (row `Foreground` color **and** a Status
  badge column **and** the Expiry column). Keep the **badge column** (most legible) and the row color;
  remove the **redundant** third encoding only where it duplicates the same signal — document exactly
  what was removed. Do not remove the Expiry column's *distinct* near-expiry/expired information.
- **Shrinkage:** delete the "Last Refreshed" **metric card**; relocate `LastRefreshed` to a small
  caption in the toolbar/status area (the binding already exists elsewhere on the VM — reuse it; do not
  add a property).
- Remove any legend that merely restates the now-singular encoding.

### 5. Constraints

- **Behavior frozen** — no binding/command/`x:Name`/code-behind/navigation/VM/model changes. Promoting
  a card = changing its `Style` and sibling order; re-docking = moving an existing element. Rewiring is
  not allowed.
- **Do not touch** the root container's scroll behavior or any filter-toolbar `Grid` — that is UX-11's
  territory (prevents overlapping edits in shared files).
- **No inline hex** — UX-04 sweep stays clean; hero accent comes from tokens via the UX-09 styles.
- **Semantic colors preserved** — a hero value that was `DangerBrush`/`WarningBrush`/`SuccessBrush`
  keeps that `Foreground` override.
- **XAML/VB traps** — MC3074 full root prefix; `<Setter>` → `DependencyProperty` only; no brush token
  into a `Color` property.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — boot and visit all 10 views in **both** themes; confirm each hero renders
  larger/first and recolors on toggle.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Each of the 10 views shows **one** visually dominant hero metric (largest, first/top-left) backed by
   `PrimaryMetricCardStyle`; remaining cards are `SecondaryMetricCardStyle`/`TertiaryMetricStyle`.
3. Financial Overview's KPI band is no longer 8 equal cards; the overflow is demoted to a compact
   tertiary strip, and the **Alerts panel renders above the chart**.
4. Data-ink trims applied: no "Last Refreshed" KPI card; Stock's redundant third status encoding
   removed (documented); no orphaned legends.
5. **Hex sweep still clean.** **All behavior unchanged** — spot-check that every card still binds the
   same value, the `VatPayableTile` still navigates, and every semantic value keeps its color.
6. Theme toggle re-colors every restructured view live, in both themes.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-10-summary.md` (template `Progress/_template.md`). Include: the
per-view hero chosen (and the binding it uses), the Financial Overview hero/secondary/tertiary split,
exactly what data-ink was removed (with before/after), any view whose ideal hero isn't on the VM yet
(flag for UX-12/UX-13), and `codebase_wiki` discrepancies.

### Documentation
Update `patterns/wpf-vista-dashboard-layout.md` with the applied hero-per-view map and the
"promote-don't-add" rule (hero must bind an existing property), per `workflow-agent-wiki-update.md`.
Update `agent_wiki/index.md` + `log.md`.
