---
module: MerchSys.App
plan-id: UX-04
title: "View Migration — Inline Hex → Tokens Across All Views"
depends-on: [UX-02, UX-03]
estimated-files: 35
---

# View Migration — Inline Hex → Tokens Across All Views

## Context

With the foundation (UX-01), shell (UX-02), and implicit control styles (UX-03) in place, this plan
finishes the job: it walks every remaining view and replaces inline hardcoded colors and ad-hoc
chrome with token references, so the **entire app is consistent in both light and dark** and there
is **zero literal hex** outside the two palette dictionaries.

This is the most repetitive plan but the lowest-conceptual-risk: behavior is frozen, the design
language is already decided (UX-00), and most controls already inherit the right look from UX-03.
The work per view is mostly: delete inline `Background`/`Foreground`/`BorderBrush` hex, replace with
`DynamicResource` tokens, apply card chrome (`SurfaceBrush` + `RadiusMedium` + `CardShadow`) to
panels, fix spacing to the `Spacing*` ramp, and route semantic colors (low-stock, overdue, expiry,
positive/negative money) to `Warning`/`Danger`/`Success`/`Accent` tokens.

Because of the file count, **execute in batches by module** (see Specification). Each batch builds
and is verified in both themes before the next begins.

Read `UX-00` (token contract), `UX-01`, `UX-02`, `UX-03` first.

## Prerequisites

- **UX-02** — shell reskinned; tokens proven in light + dark.
- **UX-03** — implicit control styles app-wide; keyed `AccentButtonStyle`/`LinkButtonStyle`.

## Scope — views to migrate (≈35)

Grouped as the recommended batches:

**Batch A — POS** (`Views/POS/`): SalesCartView, CreditManagementView, TransactionHistoryView,
DailySummaryView, VatSettingsView.

**Batch B — Inventory** (`Views/Inventory/`): StockDashboardView, ProductManagementView,
ExpiryMonitorView, ShrinkageView, ProductPriceHistoryView.

**Batch C — Purchasing** (`Views/Purchasing/`): PurchaseOrderListView, GoodsReceivingView,
VendorDirectoryView, VendorCatalogView, APLedgerView, ReorderSuggestionsView.

**Batch D — Accounting** (`Views/Accounting/` incl. `Components/`): FinancialOverviewView,
IncomeStatementView, SalesSummaryView, VatReturnView, VatReliefReportView, TamperAuditReportView,
Components/VatPayableTile.

**Batch E — Top-level & dialogs**: OwnerDashboardView, LoginView, SessionTimeoutWarningView, and the
`Views/Shell/Modules/*Panel.xaml` module panels (PurchasingPanel, InventoryPanel, PosPanel,
AccountingPanel, DeveloperToolsPanel, ModuleDetailPanel if not fully done in UX-02).

> The shell files reskinned in UX-02 (`MainWindow`, `ActivityRail`, `ModuleDetailPanel`,
> `ConnectionStatusIndicator`) are **excluded** — already migrated.
>
> **Two known shell carry-forwards from UX-02 — fix these as part of Batch E (the only shell
> exception):**
> 1. **`ConnectionStatusIndicator.xaml` — two `FontSize="11"` literals** (the `RetryButtonStyle`
>    setter and the state-label `TextBlock`). Replace both with `{DynamicResource FontSizeCaption}`
>    (= 11). Not a color defect, which is why UX-02 deferred it; it's a type-ramp cleanup.
> 2. **`ConnectionStatusIndicator.xaml` — local `HoverBackgroundBrush` shadow.** It defines its own
>    `<SolidColorBrush x:Key="HoverBackgroundBrush" Color="White" Opacity="0.2"/>` in
>    `UserControl.Resources`, which now collides by name with the real palette token. The pill's
>    retry button intentionally needs a *white* hover over a saturated background, so the fix is to
>    **rename the local brush** (e.g. `PillHoverBrush`) and update its single consumer — not to delete
>    it and inherit the palette token (that would be the wrong color on the colored pill). Eliminates
>    the same-name-different-meaning trap.

## Deliverables

All `.xaml` files listed above, modified to use tokens. `estimated-files: 35` ≈ the view count.
Code-behind `.xaml.vb` files are **not** modified (behavior frozen).

## Specification

### Mechanical migration rules (apply per view)

1. **Replace every literal color** (`Background`, `Foreground`, `BorderBrush`, `Fill`, `Stroke`,
   gradient stops) with the matching `DynamicResource` token from UX-00:
   - page/section backgrounds → `WindowBackgroundBrush`
   - cards/panels/tiles → `SurfaceBrush`
   - primary text → `TextPrimaryBrush`; labels/captions/secondary → `TextSecondaryBrush`
   - dividers/borders → `SeparatorBrush`
   - primary action accents → `AccentBrush`
   - semantic: low-stock/caution → `WarningBrush`; overdue/error/expiry/negative → `DangerBrush`;
     paid/positive/in-stock → `SuccessBrush`
2. **Card chrome:** wrap logical groupings/tiles in a `Border` with `SurfaceBrush` background,
   `RadiusMedium` corners, and `Effect="{DynamicResource CardShadow}"`. This is the core of the
   "premium" macOS card-on-canvas feel — apply it to dashboard tiles, report sections, and form
   groups.
3. **Spacing:** normalize margins/padding to the `Spacing*` ramp (`4/8/12/16/24`). Increase
   cramped layouts toward macOS-style breathing room where it doesn't break alignment.
4. **Type:** ensure the view inherits `AppFontFamily` (it does, from the shell root) and uses the
   `FontSize*` ramp for headings vs. body; replace ad-hoc `FontSize`/`FontWeight` with the ramp.
5. **Controls:** delete inline control styling that UX-03 now provides implicitly; switch primary
   buttons to `AccentButtonStyle`, tertiary to `LinkButtonStyle`.
6. **Reconcile keyed styles:** where a view defined its own keyed style with inline hex, re-point its
   setters to tokens (or delete it if UX-03's implicit style now suffices). Document deletions.

### Emoji/status indicators

Several views use emoji + color for status (e.g. `⚠️`, `🔴`). Keep the glyphs but route their
**color** to the semantic tokens so they stay legible in dark mode. Don't redesign the indicators —
just tokenize their color.

### Plain-language "What This Means" boxes

Accounting/Owner views carry mandatory "What This Means" boxes. These are **content, not chrome** —
do not alter their text or logic. Only restyle their container (e.g. a `SurfaceBrush` card with a
left `AccentBrush` rule) and text colors.

### Per-batch loop

For each batch A–E: migrate the views → `dotnet build` (0/0) → run the app, toggle light/dark, visit
each migrated view, confirm legibility and no layout breakage → only then proceed to the next batch.
This keeps regressions isolated to one module at a time.

## Implementation Notes

- **Behavior is frozen.** No changes to bindings, commands, `x:Name`, converters, `DataContext`,
  code-behind, or navigation. If a color is bound via a converter (e.g. a value-to-brush converter
  returning hardcoded brushes), update the **converter's output** to resolve tokens
  (`Application.Current.TryFindResource("DangerBrush")`) rather than leaving hex in code — document
  any such converter touched.
- **Contrast check both themes.** The most common defect will be text that was dark-on-light now
  unreadable in dark mode because a stray literal slipped through. The acceptance gate is a hex
  sweep.
- **DataGrid-heavy views** (ledgers, reports) should now look right largely for free from UX-03;
  mostly just remove inline grid styling and let the implicit style apply.
- MC3074: full root prefix on any `clr-namespace` in views (several views already map VM/model
  namespaces — leave those correct ones alone).
- VB.NET BC36943: only relevant if a touched converter/code-behind is `Async` (rare here) — do not
  `Await` in `Catch`/`Finally`.
- No new NuGet packages.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. **Hex sweep:** no literal `#RRGGBB`/`#AARRGGBB` color remains in any view under `Views/`
   (excluding the two palette dictionaries `Themes/Light.xaml`/`Dark.xaml`). Brush colors set in
   code-behind/converters resolve tokens, not literals.
3. Every view renders correctly in **both** light and dark — text legible (AA contrast), cards have
   `SurfaceBrush` + soft shadow on the `WindowBackgroundBrush` canvas, semantic colors map to the
   Warning/Danger/Success/Accent tokens.
4. All behavior unchanged: every view's bindings, commands, navigation, and data still work exactly
   as before (spot-check the heaviest: SalesCartView, StockDashboardView, APLedgerView,
   FinancialOverviewView, OwnerDashboardView, LoginView).
5. "What This Means" boxes are restyled but their text/logic is unchanged.
6. Theme toggle re-colors every view live (no view stuck in the old palette).

## Output Requirements

### Implementation Summary
Create `Progress/VISTA_Modules/Experience/UX-04-summary.md` (template `Progress/_template.md`).
Include: per-batch completion status, the hex-sweep result (command used + zero-match evidence), any
converters touched, any keyed styles deleted/repointed, and a list of views that needed manual layout
adjustment beyond mechanical token swaps. Note `codebase_wiki` discrepancies.

### Documentation
- A short "theming conventions" note (where tokens live, the `DynamicResource` rule, how to add a
  new themed view) — suitable for the agent wiki `patterns/` directory so future view work stays
  consistent. Log it per the `workflow-agent-wiki-update.md` workflow.
