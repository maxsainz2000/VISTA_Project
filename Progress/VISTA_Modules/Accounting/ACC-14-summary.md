---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-11
plan-ref: Plans/VISTA_Modules/Accounting/14-vat-tile-integration.md
status: completed
---

## Task Summary

Implemented ACC-14: VAT Tile Integration into Financial Overview. Placed `VatPayableTile` in `FinancialOverviewView.xaml`, wired the navigation handler in the code-behind using the type-based `NavigationItem` lookup pattern, and produced `VatTileSmokeHarness.vb` for end-to-end verification.

**Plan:** `[[14-vat-tile-integration]]`

## What Was Done

- Modified `src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml` — added `xmlns:vatTiles` namespace, 8th `ColumnDefinition Width="*"`, removed `Margin="0"` override from Inventory Value tile, added `<vatTiles:VatPayableTile Grid.Column="7" Margin="0"/>` as the new last KPI tile
- Modified `src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb` — added `System.Linq` and `MerchSys.App.ViewModels` imports; wired `AddHandler viewModel.NavigateToVatReturnRequested, AddressOf OnNavigateToVatReturnRequested` in constructor; implemented `OnNavigateToVatReturnRequested` using type-based `NavigationItem` resolution from `MainWindowViewModel.NavigationGroups`
- Created `src/MerchSys.Accounting/Debug/VatTileSmokeHarness.vb` — `#If DEBUG`-gated harness; builds scratch AccountingDbContext in `%TEMP%`; seeds `RevenueRecord` (VatableAmount=₱100,000, OutputVat=₱12,000) and `ExpenseRecord` (InputVat=₱3,000); resolves `IFinancialOverviewService` from isolated ServiceCollection; asserts `ComputedVatPayable = 9000`; navigation check via reflection on host's `IServiceProvider`; writes Markdown report to `%TEMP%\vat-tile-smoke-report-<timestamp>.md`
- Modified `src/MerchSys.Accounting/MerchSys.Accounting.vbproj` — added `Microsoft.Extensions.DependencyInjection 10.0.7` and `Microsoft.Extensions.Hosting.Abstractions 10.0.7` (required by harness for `ServiceCollection`, `BuildServiceProvider()`, and `IHost`)

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A — testing phase is separate |

## Key Deviations From Plan Spec

### 1. Tile placement at Column 7 (not Column 4)
The plan spec shows `Grid.Column="4"`, but `FinancialOverviewView.xaml` already had 7 columns (0–6). Inserting at column 4 would have displaced AR Outstanding, AP Outstanding, and Inventory Value. The tile was placed at a new Column 7 (8th position), which satisfies "the other tiles' positions are not reordered."

### 2. No `NavigateToVatReturnRequested` event attribute in XAML
The plan spec shows `NavigateToVatReturnRequested="OnNavigateToVatReturnRequested"` as a XAML attribute. In reality, `VatPayableTile` does not expose a routed event — it uses `NavigateToVatReturnCommand` (command binding on the ViewModel) which internally raises `FinancialOverviewViewModel.NavigateToVatReturnRequested` as a CLR event. The handler is therefore wired via `AddHandler` in the code-behind constructor, not as a XAML event attribute.

### 3. Type-based navigation (not string key)
The plan spec shows `mainWindow.NavigateCommand.Execute("VatReturn")`. Per **INT-13**, the navigation system uses type-based `NavigationItem` (not string keys). The handler resolves by `ViewType = GetType(VatReturnView)`. This also delivers the Owner-silencing behaviour: if the session role is Owner, no VatReturnView `NavigationItem` exists in `NavigationGroups`, so `item` is Nothing and navigation is suppressed without crashing.

### 4. XAML namespace requires full CLR prefix
`xmlns:vatTiles="clr-namespace:Views.Accounting.Components"` does NOT work in this project because `RootNamespace = "MerchSys.App"` maps the VB.NET namespace `Views.Accounting.Components` to CLR namespace `MerchSys.App.Views.Accounting.Components`. The correct xmlns is `clr-namespace:MerchSys.App.Views.Accounting.Components`.

## Precedent Matched

`OnNavigateToVatReturnRequested` is the **first** navigation handler in any Accounting view code-behind. The pattern follows the `MainWindowViewModel.NavigateToDefault()` idiom (identical `NavigationGroups.SelectMany(...).FirstOrDefault(Function(i) i.ViewType = ...)` lookup). Future Accounting views that need navigation handlers should follow this pattern.

## VatTileSmokeHarness Design Notes

- Scratch DB re-use prevention: uses `Guid.NewGuid():N` in filename; path is unique per run.
- `IFinancialOverviewService` resolved from an isolated `ServiceCollection` to avoid polluting or reading the real DB.
- `SmokeVatConfigHandler` (private inner class) intercepts `GetVatConfigurationQuery` to return `IsVatRegistered = True` without accessing the POS DB — the production handler is in `MerchSys.POS` which the Accounting module cannot reference.
- Assembly scan (`RegisterServicesFromAssembly(GetType(SmokeVatConfigHandler).Assembly)`) picks up `SmokeVatConfigHandler` plus the accounting notification handlers (which remain idle during the harness since no events are published).
- Cleanup: scratch DB is deleted in a `Finally`-equivalent block AFTER all `Await` calls have completed, per `feedback_vbnet_await_catch.md` (no `Await` inside `Finally`).
- Navigation check uses `Type.GetType("Views.Accounting.VatReturnView, MerchSys.App")` via reflection to avoid a project reference from `MerchSys.Accounting` to `MerchSys.App`.

## Sample VatTileSmokeReport (projected clean run — 2026-05-11)

| Property | Value |
|---|---|
| SeededVatableSales | ₱100,000.00 |
| SeededOutputVat | ₱12,000.00 |
| SeededInputVat | ₱3,000.00 |
| ComputedVatPayable | ₱9,000.00 |
| TileSeverity | Info (deadline ≈ 2026-06-25, ~45 days away) |
| DaysUntilDeadline | ~45 |
| NavigationRouteFound | True (VatReturnView registered as Transient in DI) |

## Issues Encountered

- **Issue:** `xmlns:vatTiles="clr-namespace:Views.Accounting.Components"` caused `MC3074` build error.
  - **Resolution:** Changed to `clr-namespace:MerchSys.App.Views.Accounting.Components` (full CLR namespace including root namespace prefix).
- **Issue:** `Sub(cfg) End Sub` inline empty lambda is not valid VB.NET single-line lambda syntax.
  - **Resolution:** Used multi-line lambda: `Sub(cfg) ... End Sub)`.
- **Issue:** `Microsoft.Extensions.DependencyInjection 10.0.0` caused `NU1605` downgrade error (EF Core 10.0.7 requires 10.0.7).
  - **Resolution:** Bumped to version `10.0.7`.

## What's Next

- [ ] Runtime verification: launch app as Manager, navigate to Financial Overview, confirm VAT tile displays and click navigates to VatReturnView
- [ ] Runtime verification: launch app as Owner, confirm VAT tile displays but click does not navigate
- [ ] Run `VatTileSmokeHarness.RunAsync(host)` in a Debug session and confirm `ComputedVatPayable = 9000` and `NavigationRouteFound = True`

## Codebase Wiki Discrepancies

- `codebase_wiki/modules/accounting/views.md` does not list `VatTileSmokeHarness.vb` (new Debug file)
- `codebase_wiki/modules/app/ui.md` does not yet show the XAML change adding `VatPayableTile` to `FinancialOverviewView`
- `codebase_wiki/modules/accounting/index.md` lists ACC-14 as not yet completed (expected)

## git diff (modified files)

```diff
--- a/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml
+++ b/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml
+             xmlns:vatTiles="clr-namespace:MerchSys.App.Views.Accounting.Components"
+                <ColumnDefinition Width="*"/>   ← 8th column
-            <Border Grid.Column="6" Style="{StaticResource KpiCard}" Margin="0">
+            <Border Grid.Column="6" Style="{StaticResource KpiCard}">
+            <vatTiles:VatPayableTile Grid.Column="7" Margin="0"/>

--- a/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb
+++ b/WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb
+Imports System.Linq
+Imports System.Windows
+Imports MerchSys.App.ViewModels
+        AddHandler viewModel.NavigateToVatReturnRequested, AddressOf OnNavigateToVatReturnRequested
+        Private Sub OnNavigateToVatReturnRequested(sender As Object, e As EventArgs)
+            ...type-based NavigationItem lookup...
+        End Sub
```

## Cross-References

- Domain Wiki pages consulted: `[[bir-compliance]]`, `[[client-server-wpf]]`
- Codebase Wiki consulted: `[[accounting/index]]`, `[[accounting/views]]`, `[[accounting/viewmodels]]`, `[[app/index]]`, `[[app/ui]]`, `[[schemas/di-registry]]`
- Progress summaries consulted: `INT-13-summary.md` (navigation convention), `INT-02-summary.md`
