---
module: MerchSys.App
plan-id: UX-08
title: "Dashboard & Tab Information Architecture — Sub-Epic Overview & Shared Standards"
depends-on: [UX-05, UX-06, UX-07]
estimated-files: 0
---

# Dashboard & Tab Information Architecture — Sub-Epic Overview & Shared Standards

> **This is an index/standards document, not an implementable batch.** It defines the
> research-derived layout rules, the phase DAG, and the cross-cutting constraints that every
> `UX-09 … UX-13` plan inherits. Read this first, then execute the sub-plans in dependency order.

## Context

UX-01 → UX-07 re-skinned the app (tokens, light/dark shell, control styles, view migration, a shared
component library, a state/feedback layer, and a vector icon system). They were **deliberately
chrome-only** — UX-00 rule #3 froze layout. As a result, **every dashboard and tab still has its
original information architecture**; only color, type, and icons changed. The owner's observation —
"the dashboards still look the same after all the changes" — is correct *by design*.

A data-driven audit graded all 22 navigable content views against a research-backed rubric (NN/g
F-pattern eye-tracking; the 5–9 working-memory ceiling / >12-KPI engagement cliff; the
40-30-20-10 space rule; progressive-disclosure cognitive-load reduction; the 5-second comprehension
test; data-ink ratio). Headline results:

| View | Grade | View | Grade |
|---|--:|---|--:|
| `SalesCartView` (POS) | 84 | `VatReliefReportView` (Acc) | 76 |
| `TransactionHistoryView` (POS) | 83 | `ProductManagementView` (Inv) | 76 |
| `StockDashboardView` (Inv) | 83 | `VendorCatalogView` (Pur) | 76 |
| `ExpiryMonitorView` (Inv) | 82 | `ShrinkageView` (Inv) | 75 |
| `IncomeStatementView` (Acc) | 80 | `ProductPriceHistoryView` (Inv) | 74 |
| `SalesSummaryView` (Acc) | 80 | `PurchaseOrderListView` (Pur) | 74 |
| `CreditManagementView` (POS) | 80 | `VatReturnView` (Acc) | 73 |
| `APLedgerView` (Pur) | 80 | `TamperAuditReportView` (Acc) | 72 |
| `VendorDirectoryView` (Pur) | 80 | `FinancialOverviewView` (Acc) | 68 |
| `ReorderSuggestionsView` (Pur) | 79 | `OwnerDashboardView` | 58 |
| `DailySummaryView` (POS) | 79 | `VatSettingsView` (POS) | 78 |
| `GoodsReceivingView` (Pur) | 78 | | |

**Module averages:** POS ~81, Inventory ~78, Purchasing ~78 (operational tabs only), Accounting ~75,
Owner 58. The four dashboards average **~72**; the operational tabs average **~78** — the dashboards
trail, and their deficits are *structural*, not cosmetic.

### The seven cross-cutting defects this sub-epic fixes

1. **No primary-metric dominance anywhere.** Every KPI row (Owner, Financial Overview, Sales Summary,
   Daily Summary, Stock, Expiry, Credit, Shrinkage, VAT Return, VAT Relief) uses **equal-weight
   cards** — violating the 40-30-20-10 rule app-wide. *(→ UX-09 + UX-10)*
2. **Inconsistent overflow safety.** Some bodies wrap in a `ScrollViewer`; many don't (Financial
   Overview, VAT Return, VAT Relief, Owner, Product Management, VAT Settings) and clip on small/zoomed
   windows. *(→ UX-11)*
3. **Fixed-width filter toolbars.** Stock (11-col), Product Management (15-col), PO List use rigid
   `Grid` columns that don't reflow; `WrapPanel` (already used in Transaction History) is the fix.
   *(→ UX-11)*
4. **Reading-order error.** Financial Overview docks its Alerts panel *below* the chart (exceptions
   under the fold); they belong above it. *(→ UX-10)*
5. **Data-ink waste.** "Last Refreshed" occupying a metric card (Shrinkage); triple status encoding
   in Stock (row color + badge + expiry column); redundant legends. *(→ UX-10)*
6. **No trend / direction.** No dashboard shows movement vs a prior period; Owner has no time-series
   at all. *(→ UX-12)*
7. **Module asymmetry.** Inventory, Accounting, and POS each have a dashboard; **Purchasing has none**
   (its tabs are fine, but there is no roll-up). *(→ UX-13)*

## The two tiers

- **Tier 1 — layout-only, zero logic risk** (UX-09, UX-10, UX-11): restructure containers, weight, and
  reading order by **rebinding existing VM properties only**. No VM/service/query/command/`x:Name`
  changes. This is the same discipline as UX-01 → UX-07, extended to *layout* (which UX-00 had frozen).
- **Tier 2 — additive read-only data** (UX-12, UX-13): the two high-value items that genuinely need
  data the ViewModels don't expose yet — period-over-period **deltas/sparklines**, and a **new
  Purchasing dashboard**. These add **read-only** computed VM properties / MediatR query contracts.
  They must not alter any existing behavior, write path, or business rule.

## Phase DAG

| Plan | Title | depends-on | Tier | Outcome |
|------|-------|-----------|------|---------|
| **UX-09** | Metric-Hierarchy & Layout Foundation | UX-08 | 1 | Shared `PrimaryMetricCard`/`SecondaryMetricCard`/`FilterBar` styles + the scroll & responsive standards. **No view edits.** |
| **UX-10** | Dashboard Composition Rollout | UX-09 | 1 | Every KPI-band view gets a hero metric (40-30-20-10), corrected reading order, and data-ink trims. |
| **UX-11** | Layout-Resilience Sweep | UX-10 | 1 | `ScrollViewer` overflow safety + `Grid`→`WrapPanel` filter toolbars across all affected views. |
| **UX-12** | Trend & Delta Indicators | UX-10 | 2 | A reusable sparkline + ▲/▼ delta presenter; additive read-only VM data; wired onto the dashboards. |
| **UX-13** | Purchasing Dashboard | UX-12 | 2 | New `PurchasingDashboardView` + VM + aggregated read-only queries + nav registration. |

UX-10 must precede UX-11 because both touch the same view files (UX-10 edits the KPI-card region; UX-11
wraps the root container and the filter toolbar). Serializing them keeps each edit unambiguous.
UX-12 depends on UX-10 (it decorates the restructured hero cards). UX-13 depends on UX-12 (it reuses
the hierarchy cards and the delta/sparkline presenter).

## Shared Design Language (authoritative — inherited by every sub-plan)

These rules are derived from the research and are binding on UX-09 → UX-13.

1. **40-30-20-10 weighting.** Each dashboard names **one** primary metric — largest type, highest
   contrast, top-left (the F-pattern landing zone). 2–3 secondary metrics get medium emphasis;
   everything else is tertiary. Implemented via the shared card styles from UX-09, **not** ad-hoc
   per-view font sizes.
2. **Primary metric is role-chosen, not position-chosen.** Owner → today's revenue **or** net income;
   Financial Overview → revenue; Daily Summary → total sales; Sales Summary → total sales; Stock →
   critical-stockout risk; Expiry → total value at risk; Credit → total outstanding AR; VAT Return →
   VAT payable. (Confirm each in the sub-plan; bind to the property already on the VM.)
3. **5–9 elements per band.** Keep a KPI row within the working-memory ceiling. Where a band exceeds
   it (Financial Overview's 8 cards), demote the overflow into a compact secondary strip — never a
   second equal row.
4. **Layer order = KPI band → trend/insight → detail.** Exceptions/alerts belong **above** the fold,
   never beneath a chart.
5. **Overflow safety is mandatory.** Every scrolling-eligible body is wrapped so content never clips on
   a small or zoomed window. (UX-11 owns the rollout; UX-09 documents the standard.)
6. **Responsive filter toolbars.** Filter/action bars use `WrapPanel` (or a shared `FilterBar`
   container), never fixed multi-column `Grid`s, so they reflow on narrow windows.
7. **Data-ink ratio.** No pixel without information: no "Last Refreshed" KPI cards, no triple-encoded
   status, no decorative duplicate legends.

## Cross-cutting constraints (inherited by every sub-plan)

1. **`DynamicResource` for all color/brush references.** Structure tokens (radii/spacing/font) may be
   `StaticResource`; brushes never. (UX-00 rule #1.)
2. **No inline hex** — the UX-04 hex sweep must stay at zero in `Views/`.
3. **Tier 1 = behavior frozen.** No binding/command/`x:Name`/code-behind/navigation/VM/model/service
   changes — only layout containers, weighting, ordering, and which existing property a control binds
   to. **Tier 2 = additive only** — new read-only VM properties / query contracts; no change to any
   existing property, write path, concurrency path, or business rule.
4. **No new NuGet packages.** Charts/sparklines are hand-built (the existing dashboards already draw
   bars with `Rectangle` + bound heights; reuse that approach).
5. **XAML/VB traps.** Full root prefix on any `clr-namespace` (MC3074); a `<Setter>` targets a
   `DependencyProperty` only (`[[wpf-setter-targets-clr-property-not-dependencyproperty]]`); never feed
   a brush token into a `Color` property (`[[wpf-dynamicresource-brush-into-color-property]]`); no
   `Await` in `Catch`/`Finally` in any Tier-2 async VM code (BC36943, `[[feedback-vbnet-await-catch]]`);
   the EF Core 10 VB.NET `ToListAsync` empty-list bug applies to any new Tier-2 read path — use the
   raw `MySqlConnector` reader pattern, not `ToListAsync` on entity queries.
6. **Build gate.** `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` must finish **0 errors,
   0 warnings**. Per CLAUDE.md: if it fails, **document every error in `Progress/` and stop** — do not
   hand-patch.
7. **Realization check.** A clean build does not prove a `Style`/`Geometry`/template resolves. Each
   sub-plan's summary must confirm the touched screens were booted in **both** themes.

## Output Requirements

Each `UX-09 … UX-13` plan produces its own summary at
`Progress/VISTA_Modules/Experience/UX-NN-summary.md` (template `Progress/_template.md`) and the
agent-wiki updates its Output Requirements specify.

This overview (`UX-08`) has **no code deliverable** and no summary of its own; it is referenced by the
five sub-plan summaries.

## Non-Goals (explicitly out of scope for this sub-epic)

- Keyboard/accessibility work (focus visuals, `AutomationProperties`, accelerators) — a separate epic.
- Motion/animation polish beyond what already exists in the shell.
- Any new report **content** or KPI definition not already computed by an existing service (Tier 2 only
  *aggregates/derives* from existing data; it does not invent new business metrics).
- A BIR Official-Receipt print template (a feature, not a layout pass).
- Touching `LLM_Wiki/wiki/` or `LLM_Wiki/codebase_wiki/` (read-only per CLAUDE.md).
