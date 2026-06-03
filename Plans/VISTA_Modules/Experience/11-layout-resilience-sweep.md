---
module: MerchSys.App
plan-id: UX-11
title: "Layout-Resilience Sweep — Overflow Safety & Responsive Filter Toolbars"
depends-on: [UX-10]
estimated-files: 14
---

# Layout-Resilience Sweep — Overflow Safety & Responsive Filter Toolbars

## Context

UX-08 found two resilience defects that affect many views regardless of their KPI hierarchy:

- **Overflow safety is inconsistent.** Some bodies wrap in a `ScrollViewer` (Daily Summary, Sales
  Summary, Income Statement, Transaction detail); many don't — **Financial Overview, VAT Return, VAT
  Relief, Owner Dashboard, Product Management, VAT Settings** stack content in a `DockPanel`/`Grid`
  with no scroll, so on a small or zoomed window the lower content **clips silently**.
- **Filter toolbars are rigid.** **Stock (11-col), Product Management (15-col), PO List**, and others
  use fixed multi-column `Grid`s that don't reflow; on a narrow window controls are cut off. The fix —
  `WrapPanel` — is already used correctly in Transaction History and Daily Summary's input row.

UX-09 shipped the standards (`FilterBarStyle`, the `WrapPanel` usage, the `ScrollViewer` overflow
rule); UX-10 re-weighted the dashboards. This plan applies the **one consistent resilience
transformation** across every affected view. Because the transformation is identical everywhere, doing
it as a single concern-focused sweep (rather than per-module) maximizes accuracy.

It runs **after UX-10** so that within shared dashboard files the two plans edit disjoint regions:
UX-10 owns the KPI-card region; UX-11 owns the **root container scroll wrap** and the **filter
toolbar**.

This is **Tier 1 — behavior frozen.**

Read `UX-08` (standards) and `UX-09` (the `FilterBar` + `ScrollViewer` snippets to reuse) first.

## Prerequisites

- **UX-09** — `FilterBarStyle`/`FilterBarItemStyle` and the documented overflow snippet exist.
- **UX-10** — the dashboards are recomposed; their KPI regions are final, so wrapping their root in a
  `ScrollViewer` won't fight in-flight card edits.

## Scope

Two concerns, applied per-file from the tables below. Code-behind (`.xaml.vb`) is **not** modified.

### A. Overflow safety — wrap the scrolling body in a `ScrollViewer`

| View | Why it needs it |
|---|---|
| `Accounting/FinancialOverviewView` | tall stack (KPIs + insight + alerts + chart), no scroll today |
| `Accounting/VatReturnView` | toolbar + insight + controls + 5 cards + lines grid, no scroll |
| `Accounting/VatReliefReportView` | net band + 2 cards + 12-month grid, no scroll |
| `OwnerDashboardView` | 2×2 grid can exceed short/zoomed viewports |
| `Inventory/ProductManagementView` | tabbed grids + tall modal editor form |
| `POS/VatSettingsView` | stacked settings cards, no scroll |
| *(verify during sweep)* `Inventory/ShrinkageView`, `Accounting/TamperAuditReportView` | confirm whether the existing grid already provides scroll; wrap only the non-grid stack if needed |

> Wrap **only** the scrolling body. Toolbars, status bars, and `BusyOverlay` stay **outside** the
> `ScrollViewer`. Do not double-wrap a `DataGrid` that already virtualizes/scrolls — for grid-dominant
> views the grid keeps its own scrolling; the `ScrollViewer` is for the **stacked card/section**
> content above/around it.

### B. Responsive filter toolbars — fixed `Grid` → `FilterBar` + `WrapPanel`

| View | Current | Convert to |
|---|---|---|
| `Inventory/StockDashboardView` | 11-column fixed `Grid` filter bar | `FilterBarStyle` border + `WrapPanel` |
| `Inventory/ProductManagementView` | 15-column fixed `Grid` toolbar (Products tab) | `FilterBarStyle` + `WrapPanel` |
| `Purchasing/PurchaseOrderListView` | fixed `Grid` filter/action bar | `FilterBarStyle` + `WrapPanel` |
| `Inventory/ShrinkageView` | `DockPanel` filter row with many fixed children | `WrapPanel` for the filter group |
| `Inventory/ExpiryMonitorView` | mixed `Grid` toolbar | `WrapPanel` where it won't reflow |
| `POS/TransactionHistoryView` | already `WrapPanel` | **reference implementation — leave as-is** |
| *(verify)* other views with a 1-row fixed filter `Grid` surfaced during the sweep | — | apply the same pattern; list them in the summary |

> Group each label+input as a small inner `StackPanel Orientation="Horizontal"` inside the `WrapPanel`
> so a label never wraps away from its control. Preserve every `x:Name`, `Binding`,
> `UpdateSourceTrigger`, command, and the control order. The only change is the **panel** that lays
> them out (fixed `Grid` → wrapping) and the toolbar's container style.

## Deliverables

The swept views (sets A + B, ~12–14 files; some appear in both sets), the implementation summary, and
a wiki update.

> **Out of scope:** any KPI-card weighting (UX-10), trend/sparkline content (UX-12), and views that are
> already resilient (Transaction History, Sales Summary, Income Statement, Daily Summary, Sales Cart —
> confirm and skip). Do not restructure master-detail column splits or grid columns.

## Specification

### 1. Overflow wrap (set A)

Insert the UX-09 canonical wrapper around the scrolling body:

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
    <!-- existing stacked body (KPI band, insight, alerts, chart, sections) -->
</ScrollViewer>
```

Keep `DockPanel.Dock="Top"` toolbars and `Grid.Row` status bars / `BusyOverlay` **outside** it. Where a
view's root is a `DockPanel`, the `ScrollViewer` becomes the last (fill) child wrapping what was the
fill content. Verify the busy overlay still covers the full view (it should remain a sibling at the
root `Grid` level).

### 2. Filter-bar reflow (set B)

Replace the fixed-column `Grid` with:

```xml
<Border Style="{StaticResource FilterBarStyle}">
    <WrapPanel Orientation="Horizontal">
        <StackPanel Orientation="Horizontal" Style="{StaticResource FilterBarItemStyle}">
            <TextBlock Text="Category:" Style="{StaticResource CaptionLabelStyle}" .../>
            <ComboBox .../>   <!-- same bindings/x:Name -->
        </StackPanel>
        <!-- … one inner StackPanel per label+control group … -->
        <!-- action buttons (Search/Refresh/Add) as trailing WrapPanel children -->
    </WrapPanel>
</Border>
```

Carry over every attribute on the moved controls **verbatim**. Fixed pixel widths that were only there
to fit the `Grid` columns may be relaxed to `MinWidth` so controls breathe when wrapping — note any
width changed.

### 3. Constraints

- **Behavior frozen** — no binding/command/`x:Name`/code-behind/navigation/VM changes. Only the layout
  panel and container style change.
- **Disjoint from UX-10** — do not alter KPI-card styles or hero placement; if a card edit seems
  needed, it belongs in UX-10 (already merged) — stop and note it, don't reach into it here.
- **No inline hex** — UX-04 sweep stays clean.
- **XAML/VB traps** — MC3074 full root prefix; `<Setter>` → `DependencyProperty` only; no brush token
  into a `Color` property.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization + resize check** — boot each swept view in **both** themes, then **narrow the window**
  and confirm (a) lower content becomes scrollable rather than clipped, and (b) filter toolbars wrap
  rather than truncate.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Every set-A view scrolls its body when the window is shorter than its content; no content clips;
   toolbars/status bars/`BusyOverlay` stay pinned (don't scroll away).
3. Every set-B filter toolbar reflows onto multiple rows when narrowed; no control is cut off; each
   label stays beside its control.
4. **Hex sweep still clean.** **All behavior unchanged** — every filter still filters, every search
   still searches, every command still fires; bindings and `x:Name`s intact.
5. Transaction History (the reference `WrapPanel`) and already-scrolling views are confirmed unchanged.
6. Theme toggle still re-colors every swept view live.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-11-summary.md` (template `Progress/_template.md`). Include: the
final set-A list (views wrapped) and set-B list (toolbars reflowed), any views found during the sweep
that also needed the treatment (beyond the tables), any fixed widths relaxed to `MinWidth`, screens
confirmed already-resilient and skipped, and `codebase_wiki` discrepancies.

### Documentation
Update `patterns/wpf-vista-dashboard-layout.md` with the final overflow + responsive-filter rollout
(the canonical snippets and the "chrome stays outside the ScrollViewer / never double-wrap a grid"
rule), per `workflow-agent-wiki-update.md`. Update `agent_wiki/index.md` + `log.md`.
