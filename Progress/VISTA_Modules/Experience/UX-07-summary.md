---
module: MerchSys.App
agent: claude-code
date: 2026-06-03
plan-ref: Plans/VISTA_Modules/Experience/07-iconography.md
status: completed
---

## Task Summary

Implemented **UX-07 — Iconography & Visual Language**: introduced a hand-built monoline
vector icon system (`Themes/Icons.xaml`) and swept every emoji / decorative dingbat glyph out
of the 12 audited views plus the two UX-06 shell controls. Icons are keyed `Geometry` resources
drawn on a shared **24×24** viewbox, presented through one reusable `IconBase` `Path` style, and
filled with brush **tokens** via `DynamicResource` so they recolor live on the light/dark toggle.
Purely additive and cosmetic — no new NuGet, no binding/command/`x:Name`/code-behind/VM/model
change.

**Plan:** `[[07-iconography]]`
**Branch:** `master` (no feature branch; cosmetic sweep)

## What Was Done

- **Created** `Themes/Icons.xaml` — 19 keyed geometries + the `IconBase` presentation style.
- **Modified** `Application.xaml` — merged `Icons.xaml` at slot **[3]**, *before* `Components.xaml`,
  so component templates can reference icon geometries.
- **Swept** 12 views + 2 shell controls (code-behind untouched):
  - `OwnerDashboardView` — `↻ Refresh` button → icon+label; 4 module-tile headers (`📦 📊 💰 📈`)
    → `StackPanel` icon+`TextBlock` keeping `CardTitle`.
  - `Accounting/FinancialOverviewView`, `IncomeStatementView`, `SalesSummaryView` — `💡` insight
    glyph → `IconLightbulbGeometry` (`AccentBrush`); `SalesSummaryView` `⚠ Credit Alert` → icon+label.
  - `Accounting/VatReturnView` — `📋` header → `IconClipboardGeometry` (`TextSecondaryBrush`).
  - `LoginView` — both `👁` (`&#128065;`) reveal buttons → `IconEyeGeometry`.
  - `Purchasing/GoodsReceivingView` — `↻` reload button → icon-only; `⚠` discrepancy cell → Path
    with the same `HasDiscrepancy` trigger.
  - `Purchasing/APLedgerView` — `↻ Refresh` → icon+label.
  - `Purchasing/ReorderSuggestionsView` — `⚡ Generate`, `↻ Refresh`, `✓ Accept`, `✗ Dismiss`
    buttons → icon+label; `★` column header + cell; the `✗`/`★` "Seasonal" cell (Data swapped by
    the existing `IsSeasonalItem`/`IsSeasonalAdjusted` triggers).
  - `POS/SalesCartView` — `✕` line-remove → icon-only; 4 pay-method buttons (`💵 📱 🏦 📋`) →
    icon+label keeping each `Is*Selected` active trigger.
  - `POS/CreditManagementView` — `✅`/`🔴` status cell → Path (Data+Fill swapped by `IsBlocked`);
    `🔴 Blocked…` banner → icon+label.
  - `Views/Shell/EmptyStatePanel` — `📂` → `IconFolderOpenGeometry` (~48).
  - `Views/Shell/ConcurrencyConflictPrompt` — `⚠` → `IconWarningGeometry` (`WarningBrush`).
- **Created** `LLM_Wiki/agent_wiki/patterns/wpf-vista-iconography.md`; updated `index.md` + `log.md`.

## Final icon inventory (geometry key → replaced → default `Fill` role)

| Geometry key | Replaced | Default `Fill` |
|---|---|---|
| `IconRefreshGeometry` | `↻` (4 views) | bound to host `Button.Foreground` |
| `IconBoltGeometry` | `⚡` Generate | host `Button.Foreground` |
| `IconCheckGeometry` | `✓` Accept, `✅` OK status | host fg (white on green) / `SuccessBrush` |
| `IconXMarkGeometry` | `✗` Dismiss, `✕` Remove, `✗` not-seasonal | host fg / `SeparatorBrush` |
| `IconStarGeometry` | `★` priority/seasonal (header + 2 cells) | `WarningBrush` / `TextSecondaryBrush` |
| `IconWarningGeometry` | `⚠` (3 places) | `WarningBrush` |
| `IconCircleFillGeometry` | `🔴` Blocked (cell + banner) | `DangerBrush` |
| `IconLightbulbGeometry` | `💡` insight (3 views) | `AccentBrush` |
| `IconClipboardGeometry` | `📋` VAT header, `📋 Credit` pay method | `TextSecondaryBrush` / host fg |
| `IconBoxGeometry` | `📦` Purchasing tile | `TextSecondaryBrush` |
| `IconChartBarGeometry` | `📊` Inventory tile | `TextSecondaryBrush` |
| `IconCashGeometry` | `💰` Sales tile, `💵 Cash` pay method | `TextSecondaryBrush` / host fg |
| `IconChartLineGeometry` | `📈` Accounting tile | `TextSecondaryBrush` |
| `IconPhoneGeometry` | `📱 GCash` | host fg |
| `IconBankGeometry` | `🏦 Bank` | host fg |
| `IconEyeGeometry` | `👁` reveal (×2) | `TextSecondaryBrush` |
| `IconEyeOffGeometry` | *(defined, not consumed — see below)* | — |
| `IconFolderOpenGeometry` | `📂` empty-state | `TextSecondaryBrush` |
| `IconChevronLeftGeometry` | *(defined, not consumed — see below)* | — |

## Glyph-sweep before/after counts

| | Files with pictographic glyphs |
|---|---|
| **Before** | 13 (`OwnerDashboardView`, `FinancialOverviewView`, `IncomeStatementView`, `SalesSummaryView`, `VatReturnView`, `LoginView`, `GoodsReceivingView`, `APLedgerView`, `ReorderSuggestionsView`, `SalesCartView`, `CreditManagementView`, `EmptyStatePanel`, `ConcurrencyConflictPrompt`) |
| **After** | **0** (`Grep` over `Views/**.xaml` for the emoji code points + named entities `&#128065;`/`&#x1F4CB;`/`&#x1F4A1;` returns no matches) |

Hex sweep (UX-04 #2) re-verified: `#RRGGBB` in `Views/` → **0 matches**, unbroken.

## Decisions documented

- **`IconPresenter` vs `Path` + `IconBase` style** → chose **`Path` + `Style="{StaticResource IconBase}"` + `Data=` + optional `Fill=`**.
  Lowest-risk, zero code-behind, no new control to maintain, and the per-use boilerplate (a single
  `<Path .../>` line, or a tiny `BasedOn` style for trigger-driven cells) was not heavy enough to
  justify a `Control`/`UserControl`. A `DependencyProperty`-bearing presenter would also have edged
  toward "behavior," which the plan forbids.
- **Inline action-button icons bind `Fill` to the host `Button.Foreground`** (`{Binding Foreground,
  RelativeSource={RelativeSource AncestorType=Button}}`). A `Path.Fill` cannot truly *inherit*
  `Foreground`, so binding to it gives the plan's "icon inherits the button's foreground" behavior
  while staying theme-reactive: `✓ Accept` on `SuccessButtonStyle` shows a white check on green; the
  pay-method icons flip to white when their `Is*Selected` trigger fills the button accent-blue.
- **Glyphs deliberately left as typographic characters:**
  - `← Last 6 Months` axis label in `FinancialOverviewView` — a directional arrow that renders
    correctly in Inter; the plan explicitly permits leaving a bare typographic `←`. `IconChevronLeftGeometry`
    is defined for future use but not consumed here.
  - The `×` in `Season ×` column header (`ReorderSuggestionsView`) is a multiplication sign in a
    label, not a remove affordance — left as text.
- **`IconEyeOffGeometry` defined but unused:** `LoginView`'s reveal buttons show a *single static
  glyph* regardless of state (the toggle commands swap `PasswordBox`/`TextBox` visibility, not the
  button content), so per the plan I kept one-glyph behavior and consumed only `IconEyeGeometry`.
  The off-state geometry is shipped for a future plan that exposes a two-state reveal.
- **ActivityRail — deferred (not icon-ified).** The rail binds `Content="{Binding Abbreviation}"`
  text and `CommandParameter="{Binding ModuleId}"`. A markup-only `ModuleId → Geometry` `DataTrigger`
  map is *technically* possible without a VM change, but the rail's two-letter abbreviations + the
  `V` wordmark are a deliberate identity treatment, and the plan lists the rail as out of scope.
  Left as-is; noted for a future plan if a per-module icon mapping is wanted.

## Views that needed content-layout wrapping

Buttons/labels whose `Content="glyph text"` string became a `StackPanel Orientation="Horizontal"`
(icon `Path` + existing label `TextBlock`), command/`x:Name`/triggers preserved exactly:
`OwnerDashboardView` (refresh + 4 tiles), `ReorderSuggestionsView` (generate/refresh/accept/dismiss),
`APLedgerView` (refresh), `GoodsReceivingView` (reload, icon-only), `SalesCartView` (4 pay methods +
icon-only remove), `SalesSummaryView` (credit-alert), `CreditManagementView` (blocked banner).
Trigger-driven DataGrid cells were converted from `TextBlock`+`Text` setters to a `Path` whose
`Data`/`Fill`/`Visibility` is driven by the **same** `DataTrigger`s on the **same** bound properties.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors, 0 warnings |
| Glyph sweep (Views) | ✅ 0 pictographic glyphs remain |
| Hex sweep (Views) | ✅ 0 inline `#RRGGBB` |
| Manual verification (boot + visit every swept screen, both themes) | ⏳ pending — see below |

## Issues Encountered

- None blocking. The most delicate items were the four trigger-driven DataGrid status cells
  (`★` seasonal, `⚠` discrepancy, `✅`/`🔴` credit, `✗`/`★` seasonal). Resolved by keeping the
  original `DataTrigger` bindings/values verbatim and only changing the rendered target from a
  `TextBlock.Text` to a `Path.Data`/`Fill`/`Visibility`. Built one column first, confirmed the
  pattern, then applied it to the rest.

## What's Next

- [ ] **Realization check (manual, this is the acceptance gate a clean build cannot prove):** boot
  the app and visit every swept screen in **both** light and dark — Login (reveal), OwnerDashboard
  (refresh + 4 tiles), Reorder Suggestions (generate/refresh/accept/dismiss + seasonal columns),
  Goods Receiving (reload + discrepancy badge), AP Ledger (refresh), Sales Cart (pay methods + line
  remove), Credit Management (status cell + blocked banner), the three insight callouts, VAT Return
  header, EmptyStatePanel, ConcurrencyConflictPrompt — confirm each icon realizes, recolors on the
  toggle, and the semantic icons resolve to the intended token.
- [ ] (Optional, future) two-state password reveal consuming `IconEyeOffGeometry`; ActivityRail
  per-module icon map.

## Cross-References

- Domain/Plan: `[[07-iconography]]`, `[[00-macos-theme-overview]]` (token contract), `[[05-component-system]]`,
  `[[06-state-and-feedback]]`.
- Agent Wiki: `[[wpf-vista-iconography]]` (new), `[[wpf-vista-theming-conventions]]`,
  `[[wpf-dynamicresource-brush-into-color-property]]`, `[[wpf-setter-targets-clr-property-not-dependencyproperty]]`.

## codebase_wiki discrepancies

None noted — `Themes/Icons.xaml` is a new file the codebase wiki will pick up on the next sync; no
existing mapped signatures were contradicted.
