# Experience Specification

## Feature: UX-01

### Overview
Implemented the theme foundation for the VISTA macOS-inspired reskin. Created design tokens (font family, corner radii, spacing, drop shadow effect, and type scale font sizes), light and dark color palettes, and integrated the Inter font family. Developed a live-swapping, JSON-persisted theme toggle service, registered it in dependency injection, applied it on startup, and added a temporary developer toggle to verify behavior.

### Requirements
- Copied SIL OFL-licensed Inter font files (`Inter-Regular.ttf`, `Inter-Medium.ttf`, `Inter-SemiBold.ttf`) and `OFL.txt` license to `Fonts/`.
- Modified `MerchSys.App.vbproj` to include font files as resources.
- Created `Themes/Tokens.Designer code` containing theme-agnostic structure tokens.
- Created `Themes/Light.Designer code` defining the light color palette keys and values.
- Created `Themes/Dark.Designer code` defining the dark color palette keys and values.
- Created `Services/Theming/AppTheme.vb` defining `AppTheme` (Light/Dark) enum.
- Created `Services/Theming/IThemeService.vb` defining `IThemeService` interface.
- Created `Services/Theming/ThemeService.vb` implementing `IThemeService` with MergedDictionaries hot-swapping and local JSON persistence.
- Modified `Application.Designer code` to merge `Tokens.Designer code` and `Light.Designer code` into resources.
- Modified `Application.Designer code.vb` to register `IThemeService` in DI and apply the persisted theme on startup.
- Modified `Views/Shell/ModuleDetailPanel.Designer code` to load `DeveloperToolsPanel` when active.
- Modified `Views/Shell/Modules/DeveloperToolsPanel.Designer code` to display the "Toggle Theme" button.
- Modified `Views/Shell/Modules/DeveloperToolsPanel.Designer code.vb` to bind the button to `IThemeService.Toggle()`.

## Feature: UX-02

### Overview
This report documents the implementation of the shell visual reskin of the VISTA application (`MerchSys.App`). The VS Code-style dark chrome has been replaced with a macOS-inspired, token-based premium layout utilizing colors, typography, radii, and spacing tokens established in `UX-01`.

### Requirements
- **Modified** [MainWindowPresenter.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Presenters/MainWindowPresenter.vb) — Injected `IThemeService`, exposed `IsDarkTheme` property and `ToggleThemeCommand` for the permanent switch.
- **Modified** [MainWindow.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/MainWindow.Designer code) — Set root `FontFamily` to use the `AppFontFamily` token (Inter) and wrapped the content area in a padded `Border` with `WindowBackgroundBrush` to float the canvas.
- **Modified** [ActivityRail.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/ActivityRail.Designer code) — Updated background to `SidebarBackgroundBrush`, added a right-side separator hairline, updated typography/spacings, and applied the single-template opacity-animated selection pill button style.
- **Modified** [ModuleDetailPanel.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/ModuleDetailPanel.Designer code) — Applied `SidebarBackgroundBrush` and right-side separator hairline. Restyled nav list items with soft selection pills. Restyled "Log Out" button. Added the animated macOS-style theme toggle switch to the footer grid.
- **Modified** [ConnectionStatusIndicator.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/ConnectionStatusIndicator.Designer code) — Removed hardcoded hex values, bound retry button hover to a transparent white local brush, and mapped states to `SuccessBrush`/`WarningBrush`/`DangerBrush` using Designer code `DataTrigger`s.
- **Modified** [DeveloperToolsPanel.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/Modules/DeveloperToolsPanel.Designer code) — Removed temporary theme toggle button.
- **Modified** [DeveloperToolsPanel.Designer code.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/Modules/DeveloperToolsPanel.Designer code.vb) — Removed temporary code-behind event handler.

## Feature: UX-03

### Overview
Implemented implicit, theme-aware WinForms styles and templates for all common controls to achieve a macOS-inspired aesthetic app-wide without requiring per-view edits. Integrated the custom styles into the central application resources.

### Requirements
- Created `Themes/Controls.Designer code` defining implicit styles for:
- `ScrollBar` (Custom template: 8px slim scrollbars with overlay rounded thumbs, no arrow buttons).
- `Button` (Custom template: rounded corner flat style, interaction overlay support for hover and pressed states).
- `TextBox` / `PasswordBox` (Custom template: comfortable padding, rounded borders, and active accent border on focus).
- `CheckBox` / `RadioButton` (Custom templates: rounded checkbox, dot radio button, utilizing system accent colors).
- `ListBox` / `ListBoxItem` (Custom template: full-width rounded rows with subtle selection/hover overlays).
- `ComboBox` / `ComboBoxItem` (Custom template: clean dropdown chevrons, popup container with medium corner radius and drop shadow).
- `TabControl` / `TabItem` (Custom template: Safari-style flat tabs with thin accent underline active indicator).
- Created keyed button styles:
- `AccentButtonStyle` (Keyed style for primary actions utilizing `AccentBrush` and `AccentHoverBrush`).
- `LinkButtonStyle` (Keyed flat style for tertiary actions).
- Created `Themes/Controls.DataGrid.Designer code` definingimplicit styles for:
- `DataGrid` (Horizontal hairlines, surface backgrounds, alternating row canvas backgrounds).
- `DataGridColumnHeader` (Left-aligned, flat headers, semi-bold text, bottom border line).
- `DataGridRow` (Hover overlay, selection highlights, comfortable spacing).
- `DataGridCell` (Padding adjustments, no focus rectangle border harshness).
- Modified `Application.Designer code` to merge the new control dictionaries in the correct order after design tokens and active theme palettes.

## Feature: UX-04

### Overview
Completed the UX-04 View Migration, converting all remaining view files in the WinForms application (`MerchSys.App`) from hardcoded hex colors, ad-hoc font sizes/margins, and custom buttons/layouts to VISTA design tokens. Enabled full dynamic light and dark theme adaptivity across the entire application interface.

### Requirements
- **Batch A, B, C (POS, Inventory, Purchasing Views):** Migrated views under `Views/POS/`, `Views/Inventory/`, and `Views/Purchasing/` to eliminate literal hex colors and align status tags to semantic tokens. (Verified by prior execution/checkpoints).
- **Batch D (Accounting Views):** Tokenized Report views under `Views/Accounting/` (`FinancialOverviewView.Designer code`, `IncomeStatementView.Designer code`, `SalesSummaryView.Designer code`, `VatReturnView.Designer code`, `VatReliefReportView.Designer code`, `TamperAuditReportView.Designer code`, and `Components/VatPayableTile.Designer code`). Cleaned up DataGrid overrides to leverage the implicit theme.
- **Batch E (Top-Level & Shell Views):**
- `OwnerDashboardView.Designer code`: Tokenized dashboard cards, alerts, interpretation boxes, and buttons.
- `LoginView.Designer code`: Wrapped login screen in a premium centered card styling with macOS shadows and rounded corners. Removed custom `InputBox` / `PasswordInput` styles in favor of implicit textbox styles from `Controls.Designer code`. Changed primary buttons to `AccentButtonStyle`.
- `SessionTimeoutWarningView.Designer code`: Converted overlay dialog into card styling with soft depth shadows. Updated countdown text to `DangerBrush` and buttons to tokenized styles.
- Shell module panels (`PurchasingPanel.Designer code`, `InventoryPanel.Designer code`, `PosPanel.Designer code`, `AccountingPanel.Designer code`): Standardized item templates with `NavItemStyle` command buttons for uniform sub-navigation styling.
- `ConnectionStatusIndicator.Designer code`: Renamed local resource conflict `HoverBackgroundBrush` to `PillHoverBrush` and tokenized font size using `FontSizeCaption`.
- **Deliberables & Sweeps:**
- Ran a global regex hex search across the `Views/` directory, verifying that **zero literal hex colors** remain.
- Created a wiki entry `LLM_Wiki/agent_wiki/patterns/WinForms-vista-theming-conventions.md` documenting theming rules, primary buttons, and card container layouts.
- Verified that `dotnet build` succeeds with 0 warnings and 0 errors.

## Feature: UX-05

### Overview
Completed the UX-05 Component System consolidation, merging all local style resource definitions across all modular views (Purchasing, POS, Inventory, Accounting, Shell/top-level) into a centralized, shared stylesheet (`Themes/Components.Designer code`).

### Requirements
- **Created Components Styling Library:**
- Defined centralized components in `Themes/Components.Designer code` including:
- Button styles: `PrimaryButtonStyle`, `SuccessButtonStyle`, `DangerButtonStyle`, `WarningButtonStyle`, `SecondaryButtonStyle`, `SubtleButtonStyle`.
- Toggle style: `SegmentToggleStyle`.
- Card styles: `CardStyle`, `AlertCardStyle`.
- Typography styles: `SectionHeaderStyle`, `PageTitleStyle`, `SubtitleStyle`, `CaptionLabelStyle`, `MetricValueStyle`, `MetricValueLargeStyle`.
- DataGrid row/cell helpers: `SemanticRowStyle`, `NumericCellStyle`.
- **Integrated Central Library:**
- Merged `Themes/Components.Designer code` into `Application.Designer code` directly after data grid styling resources.
- **View Styling Cleanup (Global Sweep):**
- Removed local duplicates of buttons, cards, headers, labels, and cell/row styling from all 20+ view files.
- Pointed all buttons, cards, and labels to the new components dictionary styles.
- Repointed all custom right-aligned DataGridTextColumns to `ElementStyle="{StaticResource NumericCellStyle}"`.
- Repointed DataGrid rows to use `RowStyle="{StaticResource SemanticRowStyle}"`.

## Feature: UX-06

### Overview
Completed the UX-06 State and Feedback implementation, introducing busy overlay spinner states, empty list/grid placeholders, modal concurrency prompt dialogs on database edit conflicts, and form-field validation tooltips with real-time error notifications.

### Requirements
- **State and Feedback Foundation:**
- Extended `INotificationService` with `ShowInfo` and `ShowWarning` methods.
- Created `IConflictPresenter` and the concrete `DefaultConflictPresenter` utilizing the new custom `ConcurrencyConflictPrompt` dialog.
- Registered `IConflictPresenter` in the application DI configuration.
- Created reusable custom UI controls:
- `BusyOverlay`: Layered visual spinner bound to VM `IsLoading` / `IsBusy` states.
- `EmptyStatePanel`: Visual placeholder (folder icon, custom title and description) bound to collection empty states.
- **Optimistic Concurrency Write-Paths:**
- Wrapped write-paths in Presenters (`SalesCartPresenter`, `APLedgerPresenter`, `GoodsReceivingPresenter`, `CreditManagementPresenter`, `VatSettingsPresenter`) to catch `DbUpdateConcurrencyException`.
- Prompted operators with `ConcurrencyConflictPrompt` to either Refresh (refresh from DB and drop local edits) or Cancel.
- **Form Data Validation:**
- Converted targeted Presenters to inherit from `ObservableValidator` from CommunityToolkit.MVP.
- Added data annotation attributes (e.g. `Required`, `Range`, `MinLength`) to properties in `GoodsReceivingPresenter`, `CreditManagementPresenter`, `VatSettingsPresenter`, `ProductManagementPresenter`, and `ShrinkagePresenter`.
- Replaced local validation warning text labels in Designer code forms with automatic red borders and red-dot error tooltips.
- **Layout Overlays Integration:**
- Integrated `<views:BusyOverlay>` and `<views:EmptyStatePanel>` overlays across major dashboards, reports, and list-heavy views (including POS, Purchasing, Inventory, and Accounting views).

## Feature: UX-07

### Overview
Implemented **UX-07 — Iconography & Visual Language**: introduced a hand-built monoline
vector icon system (`Themes/Icons.Designer code`) and swept every emoji / decorative dingbat glyph out
of the 12 audited views plus the two UX-06 shell controls. Icons are keyed `Geometry` resources
drawn on a shared **24×24** viewbox, presented through one reusable `IconBase` `Path` style, and
filled with brush **tokens** via `DynamicResource` so they recolor live on the light/dark toggle.
Purely additive and cosmetic — no new NuGet, no binding/command/`x:Name`/code-behind/VM/model
change.

### Requirements
- **Created** `Themes/Icons.Designer code` — 19 keyed geometries + the `IconBase` presentation style.
- **Modified** `Application.Designer code` — merged `Icons.Designer code` at slot **[3]**, *before* `Components.Designer code`,
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
- **Created** `LLM_Wiki/agent_wiki/patterns/WinForms-vista-iconography.md`; updated `index.md` + `log.md`.

## Feature: UX-09

### Overview
Implemented the foundational styles for the metric hierarchy, responsive FilterBar, and overflow standard in the shared component ResourceDictionary.

### Requirements
- Modified `Themes/Components.Designer code` to add the following style keys:
- `PrimaryMetricCardStyle` — the single hero card (Border based on `CardStyle` with `AccentBrush` border brush and SpacingXL padding)
- `SecondaryMetricCardStyle` — supporting cards (Border based on `CardStyle` with SpacingL padding)
- `TertiaryMetricStyle` — demoted strip items (Border based on `CardStyle` with compact padding and RadiusSmall corner radius)
- `PrimaryMetricValueStyle` — hero value text (TextBlock based on `MetricValueLargeStyle`)
- `SecondaryMetricValueStyle` — secondary value text (TextBlock using `FontSizeTitle` and bold weight)
- `MetricCaptionStyle` — label underneath values (TextBlock based on `CaptionLabelStyle`)
- `FilterBarStyle` — container Border for wrapping search/filter toolbars
- `FilterBarItemStyle` — horizontal spacing margin for controls nested inside the filter bar
- Created `LLM_Wiki/agent_wiki/patterns/WinForms-vista-dashboard-layout.md` containing Layout Standards:
- Defining the metric hierarchy vocabulary (40-30-20-10 rule)
- Canonical `FilterBar` wrapping code snippet
- ScrollViewer auto vertical/disabled horizontal overflow rules
- Updated `LLM_Wiki/agent_wiki/index.md` & `LLM_Wiki/agent_wiki/log.md` to register layout patterns.
- Verified style key resolution and responsive WrapPanel wrap on a temporary scratch layout in `OwnerDashboardView.Designer code` (switched themes live to check brush dynamic resource updates) and reverted view file.

## Feature: UX-10

### Overview
Implemented dashboard layout compositions across 10 KPI-band views in `MerchSys.App` to establish a clear primary-metric hierarchy, correct reading order, and trim data-ink. Applied the `PrimaryMetricCardStyle`, `SecondaryMetricCardStyle`, and `TertiaryMetricStyle` shared component styles introduced in UX-09, ensuring all dynamic triggers, converters, bindings, and code-behind remain fully frozen.

### Requirements
- Modified Designer code layouts for 10 Views in `Views/`:
- **`OwnerDashboardView.Designer code`**: Promoted `TodayRevenue` to a standalone Hero card at the top (`PrimaryMetricCardStyle` + `PrimaryMetricValueStyle`). Demoted the 4 module cards to `SecondaryMetricCardStyle` and chunked their vertical stacks of values into compact 2x2 grids (Col 0 and Col 1 definitions).
- **`FinancialOverviewView.Designer code`**: Promoted `MonthToDateRevenue` to Hero card at Column 0. Demoted Today's Revenue, Gross Margin %, AR Outstanding, and AP Outstanding to secondary. Grouped YTD Revenue, Inventory Value, and the clickable VAT tile into a compact sub-row styled as `TertiaryMetricStyle`. Docked the Alerts panel above the chart (`DockPanel.Dock="Top"`) to align with reading order requirements.
- **`DailySummaryView.Designer code`**: Promoted `TotalSalesDisplay` to Hero (retaining dynamic color styling). Demoted Transactions, Avg Transaction, and Returns to secondary. Removed inline border properties in favor of shared styles.
- **`SalesSummaryView.Designer code`**: Promoted `TotalSalesDisplay` to Hero in Column 0. Demoted other KPIs to secondary.
- **`StockDashboardView.Designer code`**: Promoted `CriticalStockoutCount` to Hero in Column 0 (retaining dynamic red semantic styling). Demoted all other cards to secondary. Data-Ink Trim: Removed explicit foreground overrides from the Expiry column template triggers for "Near Expiry", "Expired!", and "OK" states to avoid redundant status encoding, letting row styling determine standard text colors.
- **`ExpiryMonitorView.Designer code`**: Promoted `TotalValueAtRisk` to Hero in Column 0. Demoted remaining cards to secondary.
- **`CreditManagementView.Designer code`**: Promoted `TotalOutstanding` to Hero in Column 0. Replaced `UniformGrid` with a standard `Grid` utilizing a `1.3*` column for the Hero and `1*` columns for the secondary cards to create visual hierarchy. Demoted others to secondary.
- **`ShrinkageView.Designer code`**: Promoted `PeriodTotalValue` to Hero in Column 0. Demoted Filtered Records. Data-Ink Trim: Removed the "Last Refreshed" KPI card and relocated the `LastRefreshed` binding to a TextBlock caption in the top-right toolbar next to the Refresh button. Reduced KPI columns from 3 to 2.
- **`VatReturnView.Designer code`**: Promoted `VatPayableDisplay` to Hero in Column 0 (with `1.3*` grid column width and dynamic background triggers). Demoted other KPIs to secondary.
- **`VatReliefReportView.Designer code`**: Promoted Net VAT Payable band Border to `PrimaryMetricCardStyle` and set its value TextBlock's base style to `PrimaryMetricValueStyle` while preserving its dynamic color triggers.
- Updated `patterns/WinForms-vista-dashboard-layout.md` in `LLM_Wiki/agent_wiki/` with the "promote-don't-add" rule and the complete canonical dashboard Hero mapping table.
- Appended a changelog entry to `agent_wiki/log.md`.

## Feature: UX-11

### Overview
Implemented the UX-11 Layout-Resilience Sweep across all affected VISTA views in the `MerchSys.App` project. This includes wrapping stacked card/section bodies in `ScrollViewer` elements (Set A) to prevent silent clipping on short or zoomed windows, and converting rigid, fixed-column `Grid` toolbars to responsive `WrapPanel` layouts using the `FilterBarStyle` and `FilterBarItemStyle` shared components (Set B) to prevent control truncation on narrow screens. All bindings, commands, event handlers, and styles remain fully intact, maintaining strict "behavior frozen" compliance.

### Requirements
- Modified Designer code layouts for 11 Views in `Views/`:
- **`Accounting/FinancialOverviewView.Designer code`**: Wrapped the main dashboard body (KPI cards, What This Means, Alerts panel, and top products cards/charts) in a `ScrollViewer` + `StackPanel` container. Kept the toolbar, header, and `BusyOverlay` outside the scroll wrap. Relieved zero-height collapse risk by giving the trend chart `Border` and Top Products `Border` explicit `MinHeight="240"`.
- **`Accounting/VatReturnView.Designer code`**: Wrapped upper elements (What This Means, Period/Filing Controls Panel, Status Message, and Summary Cards row) in a `ScrollViewer` + `StackPanel` container. Kept the toolbar, header, and `BusyOverlay` outside. Kept the `LINES DETAIL` container (containing the virtualized `DataGrid` and `EmptyStatePanel`) outside the scroll wrap to prevent double-wrapping and maintain grid virtualization.
- **`Accounting/VatReliefReportView.Designer code`**: Wrapped Net VAT Payable card and Sales/Purchases Summary cards in a `ScrollViewer` + `StackPanel` container. Kept the toolbar, header, and `BusyOverlay` outside. Kept the `TRAILING 12 MONTHS GRID` border (containing the `DataGrid` and `EmptyStatePanel`) outside to maintain independent grid scrolling.
- **`OwnerDashboardView.Designer code`**: Wrapped the main KPI card grid in a `ScrollViewer` and changed its row definitions from `*` to `Auto` to support natural expansion and scrolling on short windows. Kept the header and loading overlay outside.
- **`Inventory/ProductManagementView.Designer code`**:
- **Products Tab Toolbar**: Converted the 15-column fixed `Grid` toolbar to a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped the Category and Search label-input controls in horizontal `StackPanel` elements styled with `FilterBarItemStyle` to keep labels paired with inputs.
- **Product Editor Overlay**: Wrapped the `Border` dialog card in a `ScrollViewer` with stretch alignments, adding a `Margin="20"` to the card to prevent modal clipping/cutoff on short viewport heights.
- **`POS/VatSettingsView.Designer code`**: Wrapped the entire stacked settings card list (Validation Errors, Registration Status, Tax Rates, Business Info, and Status Message) in a `ScrollViewer` + `StackPanel` container, keeping the toolbar and `BusyOverlay` outside.
- **`Inventory/StockDashboardView.Designer code`**: Converted the 11-column fixed `Grid` filter bar to a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped Category, Status, and Search controls in horizontal `StackPanel` elements styled with `FilterBarItemStyle`.
- **`Purchasing/PurchaseOrderListView.Designer code`**: Converted the fixed-column `Grid` toolbar to a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped Status and Search filters in `StackPanel` elements styled with `FilterBarItemStyle`, and wrapped the manager action buttons `StackPanel` in `FilterBarItemStyle` so the entire action group wraps as a single unit.
- **`Inventory/ShrinkageView.Designer code`**: Converted the `DockPanel`/`StackPanel` filter toolbar into a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped From Date, To Date, Product, and Reason controls in horizontal `StackPanel` elements styled with `FilterBarItemStyle`.
- **`Inventory/ExpiryMonitorView.Designer code`**: Converted the mixed `Grid` toolbar into a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped the threshold days input and buttons in a horizontal `StackPanel` styled with `FilterBarItemStyle`.
- **`Accounting/TamperAuditReportView.Designer code`**: Converted the horizontal `StackPanel` filter bar to a responsive `WrapPanel` utilizing `FilterBarStyle`. Wrapped From and To Date pickers in horizontal `StackPanel` elements styled with `FilterBarItemStyle`.

## Feature: UX-12

### Overview
Implemented period-over-period direction indicators and inline sparkline trends for primary dashboards in the MerchSys suite (UX-12). This includes creating two reusable theme-reactive presentation controls (`DeltaIndicator` and `Sparkline`), exposing additive read-only Presenter properties, and wiring them into the dashboards without modifying write paths, concurrency, or core business rules.

### Requirements
- **Themes/Icons.Designer code**: Added `IconArrowUpGeometry` and `IconArrowDownGeometry` geometries for indicators.
- **Views/Shell/DeltaIndicator.Designer code**: Designed Designer code markup with triggers mapping Direction and `InvertSemantics` to appropriate theme brushes (`SuccessBrush`/`DangerBrush`/`TextSecondaryBrush`).
- **Views/Shell/DeltaIndicator.Designer code.vb**: Created VB.NET code-behind to support `Percent`, `InvertSemantics`, and read-only `Direction` Dependency Properties.
- **Views/Shell/Sparkline.Designer code**: Developed a lightweight, NuGet-free Designer code interface rendering vertical trend bars inside an `ItemsControl`.
- **Views/Shell/Sparkline.Designer code.vb**: Added code-behind to calculate bar heights dynamically relative to the maximum series value.
- **FinancialOverviewPresenter.vb**: Added additive read-only `RevenueDeltaPercent` and `RevenueSparkPoints` properties, deriving the delta from the previous month in `MonthlyTrend`.
- **OwnerDashboardPresenter.vb**: Added additive `TodayRevenueDelta` and `WeekRevenueDelta` properties, querying yesterday's and last week's sales via the existing `IDailySummaryService`.
- **DailySummaryPresenter.vb**: Added `SalesDeltaPercent` and computed it dynamically in `LoadAsync` by loading the previous comparable period's sales.
- **Views/Accounting/FinancialOverviewView.Designer code**: Wired `DeltaIndicator` and `Sparkline` inside the MTD Revenue card.
- **Views/OwnerDashboardView.Designer code**: Wired `DeltaIndicator` controls beside Today's Revenue and Week's Revenue.
- **Views/POS/DailySummaryView.Designer code**: Wired `DeltaIndicator` next to Total Sales.

## Feature: UX-13

### Overview
Implemented a new read-only **`PurchasingDashboardView`** and **`PurchasingDashboardPresenter`** to serve as the default landing view for the Purchasing module. The dashboard aggregates existing data from multiple entities/services within the Purchasing module (Outstanding AP, Overdue AP, Pending Deliveries, Reorder Suggestions, Active Vendors, and Average Lead Time) and renders a 6-month spend trend chart, a top 5 vendors by spend table, and a detailed pending deliveries grid.

### Requirements
- **Presenters/PurchasingDashboardPresenter.vb**: Presenter aggregating KPIs, status counts, spend trend, top vendors, and pending PO detail rows. Includes raw MySQL ADO.NET reader loop logic to fetch monthly trends and top vendors safely without triggering the EF Core 10 VB.NET `ToListAsync` empty-list bug.
- **Extensions/PurchasingServiceCollectionExtensions.vb**: Registered the new Presenter in DI as transient.
- **Views/Purchasing/PurchasingDashboardView.Designer code**: Designer code view using themed design tokens, responsive FilterBar container, scroll rules, and layout overflow safety ScrollViewers.
- **Views/Purchasing/PurchasingDashboardView.Designer code.vb**: Code-behind establishing DataContext and wiring the Presenter's abstract string-based `NavigateToViewRequested` event to the shell navigation system to prevent circular reference compilation errors.
- **Application.Designer code.vb**: Registered the new View in DI as transient.
- **Presenters/MainWindowPresenter.vb**: Added `PurchasingDashboardView` as the first/default navigation item in the Purchasing module for both Owner and Manager roles.
- Created Agent Wiki pattern documentation at `LLM_Wiki/agent_wiki/patterns/WinForms-vista-purchasing-dashboard.md` and updated `index.md` + `log.md`.

## Feature: UX-14

### Overview
Consolidated all five inline `DbUpdateConcurrencyException` catch blocks in write-path Presenters
onto `ConcurrencyHelper.ExecuteWithConflictPromptAsync`, and extended concurrency-conflict coverage
to every Presenter that mutates a RowVersion-protected table.

### Requirements
### Consolidation (5 wired VMs — zero inline catches remain)
- **MerchSys.POS/Presenters/SalesCartPresenter.vb**: replaced inline concurrency flag/catch/prompt with primitive; replaced `Imports Microsoft.EntityFrameworkCore` with `Imports MerchSys.SharedKernel.Persistence`
- **MerchSys.Purchasing/Presenters/APLedgerPresenter.vb**: same import swap; `work` = `RecordPaymentAsync`; `onRefresh` = close dialog + reload; success path in `If saved Then`
- **MerchSys.Purchasing/Presenters/GoodsReceivingPresenter.vb**: same import swap; receipt captured via closure; `onRefresh` resets form + reloads POs
- **MerchSys.POS/Presenters/CreditManagementPresenter.vb**: kept `Imports Microsoft.EntityFrameworkCore` (still used for `_context.Database.GetConnectionString()`); added `Imports MerchSys.SharedKernel.Persistence`; `targetId` captured pre-lambda for closure safety
- **MerchSys.POS/Presenters/VatSettingsPresenter.vb**: same import swap; `result` captured via closure; `onRefresh = AddressOf ReloadAsync`

## Feature: UX-15

### Overview
Completed the three-state load model (loading / empty / error) across every data view and Presenter.
Created the `ErrorStatePanel` control, added `IsError`/`ErrorMessage`/`IsEmpty` to 25 data VMs,
normalized all load paths to catch into `IsError`, and wired all 24 data views with `ErrorStatePanel`.

### Requirements
### New Control
- **Created** `Views/Shell/ErrorStatePanel.Designer code` — warning icon (DangerBrush), headline, message, Retry button. Mirrors `EmptyStatePanel` structure. DependencyProperties: `Title` (string), `Message` (string), `RetryCommand` (ICommand).
- **Created** `Views/Shell/ErrorStatePanel.Designer code.vb` — code-behind with 3 DependencyProperty registrations.

## Feature: UX-16

### Overview
Implemented a Spotlight-style command palette overlay centered over the shell content in the `MerchSys.App` workspace. It is activated via the `Ctrl+K` global keyboard shortcut or a clickable search affordance button located in the sidebar (Module Detail Panel). The palette aggregates all role-visible navigation items using a unified read-only aggregator on `MainWindowPresenter` and queries active products asynchronously using existing MediatR queries.

### Requirements
- **Aggregated Nav Source of Truth**: Added a single aggregator `AllNavigableItems` in `MainWindowPresenter.vb` built from the role-aware per-module collections.
- **Repointed Dashboard Navigation**: Repointed both `PurchasingDashboardView.Designer code.vb` and `FinancialOverviewView.Designer code.vb` to resolve navigation items via `AllNavigableItems` instead of the legacy `NavigationGroups` collection, preventing data drifts.
- **Command Palette Model**: Created `CommandPaletteItem.vb` to unify screen results and product results under a single structure.
- **Debounced Async Search**: Created `CommandPalettePresenter.vb` managing:
- debounced lookup using a 250ms delay Task (to prevent typing lag/lockups).
- synchronous case-insensitive matching over navigable screen display and module names.
- asynchronous lookup of products via `GetProductsForCatalogQuery` capped at 5 results (using MediatR).
- selection index tracking and wrapping keyboard navigation handlers.
- set-module-then-navigate execution (pre-selecting `ActiveModule` before navigating to avoid Activity Rail desync).
- **Spotlight Interface overlay**: Created `CommandPalette.Designer code` and `CommandPalette.Designer code.vb`:
- centered light-box panel with a dim backdrop (`OverlayBrush`) and dynamic drop shadow.
- custom grouped ListBox displaying Screens and Products under distinct section headers using native CollectionViewSource.
- keyboard hooks capturing navigation keys (`Esc`, `↑`, `↓`, `Enter`) locally to prevent bubbling.
- backdrop click-away dismissal and focus restoration to the previously focused control on close.
- integration of `BusyOverlay` for async lookups and `EmptyStatePanel` for zero matches.
- **Affordance & Key Binding**: Embedded the palette in `MainWindow.Designer code`, bound `Ctrl+K` inside the window input bindings, and added a clickable search affordance below the header in `ModuleDetailPanel.Designer code`.
- **DI Registration**: Registered `CommandPalette` and `CommandPalettePresenter` as singletons in `Application.Designer code.vb`.

## Feature: UX-17

### Overview
Implemented the Keyboard & Focus Foundation (`UX-17`) across `MerchSys.App`. This establishes an application-wide correct visible-focus indicator, configures sequential keyboard-tab navigation on all core views and dialogs, introduces Alt access-key mnemonics on primary button actions, and routes Enter-to-confirm and Escape-to-cancel hotkeys locally within custom in-view modal overlays.

### Requirements
- **Theme-Aware Focus Ring**: Created `AppFocusVisual` in `Themes/Controls.Designer code` dynamically drawing a `1.5px` border of `AccentBrush` inside elements (`Margin="1"`), honoring Light and Dark themes. Set it as the implicit `FocusVisualStyle` on all common controls (`Button`, `TextBox`, `PasswordBox`, `CheckBox`, `RadioButton`, `ListBoxItem`, `ComboBox`, `TabItem`).
- **Core Windows Tab Navigation**:
- `ConcurrencyConflictPrompt.Designer code`: Set `TabIndex` sequence and Alt-mnemonics (`_Cancel`, `_Refresh`).
- `SessionTimeoutWarningView.Designer code`: Linked native `IsDefault`/`IsCancel` buttons and set `TabIndex`/mnemonics (`_Stay signed in`, `_Sign out now`).
- `LoginView.Designer code`: Configured sequential `TabIndex` mapping across login and password-reset fields. Set `IsDefault="True"` on buttons, focused the `UsernameTextBox` on load, and configured mnemonics (`_LOG IN`, `_SET PASSWORD AND CONTINUE`).
- **Purchasing View Navigation & Overlays**:
- `APLedgerView.Designer code` & `.Designer code.vb`: Set sequential `TabIndex` on all inputs, named payment overlay, registered `IsVisibleChanged` handler to focus the payment textbox on toggle, and routed `PreviewKeyDown` locally (Esc -> Cancel, Enter -> Confirm). Added mnemonics to main/dialog buttons.
- `PurchaseOrderListView.Designer code` & `.Designer code.vb`: Added sequential tab sequence, named order editor overlay, added `IsVisibleChanged` focus target to the vendor combo box, and routed `PreviewKeyDown` (Esc -> Cancel, Enter -> Save Draft). Added mnemonics.
- `VendorDirectoryView.Designer code` & `.Designer code.vb`: Added sequential `TabIndex`, named vendor editor, mapped `IsVisibleChanged` focus dispatcher to name textbox, and routed `PreviewKeyDown` keys. Mapped mnemonics.
- `ReorderSuggestionsView.Designer code` & `.Designer code.vb`: Added `TabIndex` mapping, named suggestion editor, mapped focus dispatcher to min threshold, and routed `PreviewKeyDown` hotkeys. Mapped mnemonics (including `AccessText` for icon buttons).
- `GoodsReceivingView.Designer code`: Configured layout tab order and added access key mnemonic (`_Confirm Receipt`).
- **POS View Navigation & Overlays**:
- `CreditManagementView.Designer code` & `.Designer code.vb`: Wired sequential `TabIndex` sequence, named add account and payment overlays, registered `IsVisibleChanged` focus targeting on textboxes, and routed `PreviewKeyDown` (Enter -> Confirm, Esc -> Cancel). Mapped mnemonics (`_Confirm`, `Ca_ncel`, `_Pay`, `_Confirm Payment`, `_Cancel`).
- `TransactionHistoryView.Designer code` & `.Designer code.vb`: Set up sequential `TabIndex` for filters and action buttons, named return overlay, registered `IsVisibleChanged` focus handler, and routed `PreviewKeyDown` (Enter -> Confirm Return, Esc -> Cancel). Mapped mnemonics (`_Search`, `_Clear Filters`, `_View Receipt`, `_Process Return`, `_Cancel`, `_Confirm Return`).
- `SalesCartView.Designer code`: Set up sequential tab indices, added `<KeyBinding Key="Return" Command="{Binding SearchProductCommand}"/>` inside `ProductSearchBox.InputBindings` to trigger search on Enter, and assigned sequential `TabIndex` values.
- **Inventory View Navigation & Overlays**:
- `ProductManagementView.Designer code` & `.Designer code.vb`: Set up sequential tab navigation, named product and category editor overlays, mapped `IsVisibleChanged` focus dispatchers to inputs, and routed `PreviewKeyDown` keys (Enter -> Save, Esc -> Cancel) for both panels. Mapped mnemonics (`_Add Product`, `_Edit`, `_Price History`, `Re_fresh`, `Add _Category`, `Ca_ncel`, `_Save Product`, `_Save Category`).
- `ShrinkageView.Designer code` & `.Designer code.vb`: Set sequential `TabIndex` sequence, named record shrinkage overlay, mapped `IsVisibleChanged` focus handler, and routed `PreviewKeyDown` hotkeys (Enter -> Confirm, Esc -> Cancel) directly triggering validation and commands in the code-behind. Mapped mnemonics (`_Record Shrinkage`, `Re_fresh`, `_Cancel`, `_Confirm`).

## Feature: UX-18

### Overview
Standardized the visual presentation of forms and input controls (`UX-18`) across `MerchSys.App`. Implemented a shared `FieldRowStyle` for `HeaderedContentControl` in `Themes/Components.Designer code` that displays mnemonic Labels, red required markers, and fixed-height inline validation errors. Developed a reusable `ErrorSummary` control for multi-section screens (Product Management and Purchase Order editors) with focus dispatching. Configured right-alignment, positive integer/decimal input constraints, and lost-focus formatting discipline via `FormHelper.InputMode`.

### Requirements
- **Form Helper Attached Properties (`FormHelper.vb`)**:
- Implemented `IsRequired` attached property to show/hide required field asterisks.
- Implemented `InputMode` attached property (`PositiveInteger`, `PositiveDecimal`) enforcing character input filtering (via `PreviewTextInput` regex checks), pasting interception, right-alignment, and lost-focus format correction (`F2` format for decimals).
- **Shared Field Row Style (`Components.Designer code`)**:
- Defined `FieldRowStyle` for `HeaderedContentControl` that displays the header as a target-linked Label (preserving Alt-mnemonic focus), a red required asterisk, the input control, and a fixed `18px` validation error presenter to prevent vertical layout shifts.
- **Reusable Error Summary Control (`ErrorSummary.Designer code`/`.Designer code.vb`)**:
- Created a collapsible control that aggregates all validation errors within a target container, displaying them as clickable links that focus the invalid control when clicked.
- **Forms Migration Sweep**:
- `ProductManagementView.Designer code`: Wrapped Product (Name, SKU, Category, Price, Min Threshold, Description) and Category (Name, Description) editors in `FieldRowStyle`. Applied numeric input modes and added the `ErrorSummary` control.
- `PurchaseOrderListView.Designer code`: Mapped PO editor fields (Vendor, Expected Delivery, Notes) to `FieldRowStyle` and added `ErrorSummary`.
- `VendorDirectoryView.Designer code`: Wrapped Vendor fields (Name, Contact Person, Lead Time, Phone, Email, Address, Notes) in `FieldRowStyle`. Mapped Lead Time input to `PositiveInteger`.
- `ShrinkageView.Designer code`: Wrapped dialog inputs in `FieldRowStyle` and applied `PositiveInteger` to Quantity.
- `CreditManagementView.Designer code`: Wrapped Add-Account inputs in `FieldRowStyle`, and applied `PositiveDecimal` to Payment Amount.
- `APLedgerView.Designer code`: Wrapped Payment Amount in `FieldRowStyle` with `PositiveDecimal`.
- `VatSettingsView.Designer code`: Migrated two-column grid layouts to standard `StackPanel` vertical flows. Wrapped fields (TIN, VAT Rate, Percentage Tax Rate, Business Name, Address) in `FieldRowStyle`, applying `PositiveDecimal` to rates.
- **VM Validation Updates**:
- `PurchaseOrderEditorPresenter.vb`: Converted to inherit from `ObservableValidator`, added validation constraints for `SelectedVendor` and `ExpectedDeliveryDate`, and implemented clean error management/gating helpers.
- `PurchaseOrderListPresenter.vb`: Wired validation checks and error clears into save/submit workflows.

## Feature: UX-19

### Overview
Actionable Empty States has been fully implemented. An optional primary Call-to-Action (CTA) button has been added to the reusable `EmptyStatePanel` control. When a view model exposes a command to create/add records, it is bound to `ActionCommand` and the button is rendered. When no command is set (null/Nothing), the button collapses automatically, maintaining perfect backward compatibility for untouched views. The commands are role-gated (instantiated only for Manager/Developer, null for Owner) to ensure the CTAs never render for unauthorized roles.

### Requirements
- **Created** `Converters/NullToVisibilityConverter.vb` — Maps object (ICommand) nullability to Visibility.
- **Modified** `Views/Shell/EmptyStatePanel.Designer code.vb` & `.Designer code` — Extended control with `ActionCommand` and `ActionText` dependency properties, styled button, and collapsed when command is null.
- **Modified** `Presenters/ProductManagementPresenter.vb`, `ProductManagementView.Designer code`, & `.Designer code.vb` — Role-gated `AddProductCommand` and `AddCategoryCommand` in Presenter constructor; wired CTA in Designer code; added null-guards in key handlers.
- **Modified** `Presenters/VendorListPresenter.vb`, `VendorDirectoryView.Designer code`, & `.Designer code.vb` — Injected `ISessionService` in Presenter constructor; role-gated `AddVendorCommand`, `EditVendorCommand`, `DeleteVendorCommand`, etc.; wired CTA in Designer code; added null-guards in key handlers.
- **Modified** `Presenters/PurchaseOrderListPresenter.vb`, `PurchaseOrderListView.Designer code`, & `.Designer code.vb` — Role-gated `NewPOCommand`, `EditPOCommand`, `SubmitPOCommand`, etc.; wired CTA in Designer code; added null-guards in key handlers.
- **Modified** `LLM_Wiki/codebase_wiki/modules/app/ui.md` — Updated the `EmptyStatePanel` manifestation entry to include the new dependency properties.
- **Modified** `LLM_Wiki/agent_wiki/patterns/WinForms-vista-state-feedback.md`, `index.md`, & `log.md` — Documented the new Actionable Empty State design pattern recipe, log entry, and indices.

## Feature: UX-20

### Overview
Implemented a shared confirmation dialog pattern to prevent costly misclicks on destructive, irreversible, or financial actions across all active modules. Built on the proven presenter pattern of `IConflictPresenter`, adding a modal `ConfirmationDialog` window supporting danger styling and an optional typed confirmation affordance.

### Requirements
### 1. Presenter & Request Contracts (SharedKernel)
- **[ConfirmationRequest.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Interfaces/ConfirmationRequest.vb) **: Data carrying request DTO containing Title, Message, ConfirmButtonText, IsDestructive flag, and optional RequireTypedConfirmation token.
- **[IConfirmationPresenter.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Interfaces/IConfirmationPresenter.vb) **: Defined `PromptAsync` interface method.

## Feature: UX-21

### Overview
Established a single source of truth for currency (Philippine Peso ₱), date/time, percentage, and quantity/decimal formatting across all views in the MerchSys.App WinForms application. This cosmetic-only consolidation sweep right-aligns all numeric and currency data grid columns along with their column headers using shared styles, preserving two-way binding capability and keeping virtualization intact.

### Requirements
### 1. Centralised Format Resources & Converters
- **Created** [FormattingConverters.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Converters/FormattingConverters.vb) containing formatting value converters:
- `PesoConverter`: formats decimals to `₱{0:N2}` and supports parse-back to `Decimal`.
- `PesoNoDecimalConverter`: formats decimals to `₱{0:N0}` and supports parse-back.
- `SignedPesoConverter`: formats decimals to `− ₱{0:N2}` (negative sign prefix) and supports parse-back.
- `DateFormatter`: formats `DateTime` to `MM/dd/yyyy`.
- `DateTimeFormatter`: formats `DateTime` to `MM/dd/yyyy HH:mm`.
- `QuantityConverter`: formats numbers to `{0:N2}` or custom formats.
- **Created** [Formats.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Themes/Formats.Designer code) to store all standard format strings (`FormatCurrency`, `FormatCurrencyNoDecimal`, `FormatQuantity`, `FormatQuantityInt`, `FormatDate`, `FormatDateShort`, `FormatDateTime`, `FormatDateTimeShort`, `FormatPercent`, `FormatPercentSigned`) and value converter instances.
- **Modified** [Application.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Application.Designer code) to merge `Formats.Designer code` into the application resource dictionary.
- **Modified** [Controls.DataGrid.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Themes/Controls.DataGrid.Designer code) to add `RightAlignedHeaderStyle` targeting `DataGridColumnHeader` for right-aligned headers.

## Feature: UX-22

### Overview
Implemented UX-22 — tooltips and affordance for every icon-only control across VISTA. Added a shared implicit `ToolTip` style to `Components.Designer code` and attached tooltips to the three remaining icon-only controls: the password-visibility eye buttons in `LoginView`, the remove-line X button in `SalesCartView`, and the dark-mode toggle switch in `ModuleDetailPanel`.

### Requirements
- **Themes/Components.Designer code**: added implicit `ToolTip` style (tokenized background, foreground, border, radius, Inter font) with `ControlTemplate` override for `RadiusSmall` corners
- **Views/LoginView.Designer code**: added `ToolTip="Show / hide password"` to the current-password eye button; `ToolTip="Show / hide new password"` to the new-password eye button
- **Views/POS/SalesCartView.Designer code**: added `ToolTip="Remove from cart"` to the X-mark remove-line button inside the cart DataGrid template
- **Views/Shell/ModuleDetailPanel.Designer code**: added `ToolTip="Toggle dark mode"` to the `ThemeToggle` ToggleButton

## Feature: UX-23

### Overview
Implemented per-laptop window-placement persistence (size, position, maximized state). On close the
window's `RestoreBounds` + maximized flag are saved; on launch they are restored with an off-screen
clamp. `ui-settings.json` was upgraded from a single-key theme file to a multi-key typed store
shared by `ThemeService` and the new `WindowPlacementService`.

### Requirements
- **Services/UiSettingsStore.vb**: singleton JSON store for all per-laptop UI preferences.
Reads/writes `%LOCALAPPDATA%\MerchSys\ui-settings.json` with all known keys (`theme`,
`windowLeft`, `windowTop`, `windowWidth`, `windowHeight`, `windowMaximized`). Uses
`System.Text.Json.JsonDocument` (BCL — no new NuGet) for parsing; hand-rolled JSON string for
writing (matching existing approach). `LoadFromDisk()` runs eagerly in the constructor.
`Save()` is called by both `ThemeService` and `WindowPlacementService` after updating their
respective properties, so neither save clobbers the other's keys.
- **Services/WindowPlacementService.vb**: contains `WindowPlacement` (data class) and
`WindowPlacementService`. `LoadPlacement()` reads from the store, validates the saved rectangle
against `SystemParameters.VirtualScreenLeft/Top/Width/Height`, requires ≥ 100×30 px of the
window to be on-screen, clamps the top-left so the title bar remains reachable, and returns
`Nothing` if off-screen (triggering first-run defaults). `SavePlacement(window)` captures
`window.RestoreBounds` when maximized (not the full-screen extent) and the normal
`Left/Top/Width/Height` otherwise, then calls `_store.Save()`.
- **Services/Theming/ThemeService.vb**: constructor now injects `UiSettingsStore`.
`LoadPersisted()` reads `_store.Theme` (set by `UiSettingsStore.LoadFromDisk` at construction)
instead of reading the file directly. `SavePersisted()` updates `_store.Theme` and calls
`_store.Save()`. File I/O removed from this class. Theme persistence continues to work
identically from the caller's perspective.
- **MainWindow.Designer code.vb**: constructor now receives `WindowPlacementService` as a fourth
parameter. After `InitializeComponent()`, calls `LoadPlacement()`:
- Saved placement found → `WindowStartupLocation = Manual`, sets `Left/Top/Width/Height`,
stores `_pendingMaximize`.
- No placement (first run or off-screen fallback) → `WindowStartupLocation = CenterScreen`,
`_pendingMaximize = True`.
Added `MainWindow_Loaded` handler: applies `WindowState.Maximized` if `_pendingMaximize` is
set (deferred from constructor so WinForms sets `RestoreBounds` to the normal bounds we assigned,
enabling sensible un-maximize). Added `MainWindow_Closing` handler: calls
`_placementService.SavePlacement(Me)`.
- **MainWindow.Designer code**: removed `WindowState="Maximized"` and
`WindowStartupLocation="CenterScreen"`. These are now set exclusively from the code-behind
(first-run defaults or restored placement), removing the unconditional maximize-on-every-launch
behavior. `MinHeight/MinWidth/Height/Width` remain in Designer code.
- **Application.Designer code.vb**: registered `UiSettingsStore` and `WindowPlacementService` as
singletons before the `ThemeService` line so the DI container can resolve them as dependencies.

## Feature: UX-24

### Overview
Implemented the standardized manual refresh and real-time ticking data freshness header chip pattern across 8 high-traffic dashboard and list views in the VISTA WinForms application. This ensures consistent "Updated Nm ago · ↻" feedback to the users, dynamic warning coloration past a 5-minute staleness threshold, single ticking dispatcher clock optimization, and failed-refresh load safety.

### Requirements
- Created `Interfaces/IFreshnessAware.vb` exposing `Property LastLoadedAt As DateTime?`.
- Created `Converters/RelativeTimeConverter.vb` implementing relative time formatting ("just now", "Nm ago", "Nh ago") with VB.NET direct value-type checks.
- Created `Helpers/FreshnessTimer.vb` running a single static 30-second low-frequency `DispatcherTimer` to broadcast updates.
- Created `Views/Shell/FreshnessChip.Designer code` and `.vb` user control with automated event subscription on load and unsubscription on unload to prevent leaks.
- Modified 8 Presenters to implement `IFreshnessAware` and update `LastLoadedAt` only on successful query resolution:
- `StockDashboardPresenter.vb`
- `PurchasingDashboardPresenter.vb`
- `OwnerDashboardPresenter.vb`
- `FinancialOverviewPresenter.vb`
- `SalesSummaryPresenter.vb`
- `PurchaseOrderListPresenter.vb`
- `ProductManagementPresenter.vb`
- `TransactionHistoryPresenter.vb`
- Modified 8 Designer code Views to mount `<views:FreshnessChip>` and bind them to Presenter properties (replacing old textblocks and/or buttons):
- `StockDashboardView.Designer code`
- `PurchasingDashboardView.Designer code`
- `OwnerDashboardView.Designer code`
- `FinancialOverviewView.Designer code`
- `SalesSummaryView.Designer code`
- `PurchaseOrderListView.Designer code`
- `ProductManagementView.Designer code`
- `TransactionHistoryView.Designer code`

## Feature: UX-25

### Overview
Implemented tasteful, restrained transitions and micro-interactions on the shell content views and primary controls (buttons, lists, and grids) following the UX-25 specification. Centralized motion durations and easing functions in `Tokens.Designer code` and added automatic OS-level reduced motion detection to degrade all transitions to instant when animations are disabled in Windows.

### Requirements
- **Centralized Motion Tokens** in `Themes/Tokens.Designer code`:
- Added `MotionEnabled` (Boolean resource, defaults to `True`).
- Added standard durations: `MotionDurationFast` (150 ms) and `MotionDurationStd` (220 ms).
- Added `MotionEasing` (CubicEase with EaseOut).
- **Reduced Motion Support** in `Application.Designer code.vb`:
- Added logic in `Application_Startup` checking `SystemParameters.ClientAreaAnimation`.
- Set the central `MotionEnabled` resource flag.
- Dynamically overrode `MotionDurationFast` and `MotionDurationStd` to `0:0:0` (TimeSpan.Zero) when system animations are disabled, ensuring transitions degrade instantly.
- **Content Host view transitions** in `MainWindow.Designer code`:
- Configured content `Binding` with `NotifyOnTargetUpdated=True`.
- Added `EventTrigger` for `Binding.TargetUpdated` that runs a Storyboard on content view changes, animating `Opacity` (0.0 to 1.0) and `TranslateTransform.Y` (8px to 0px) over `MotionDurationStd` using `MotionEasing`.
- **Eased Control States** in templates:
- Modified implicit `Button` style in `Themes/Controls.Designer code` to use `VisualStateManager` (VSM) for `CommonStates` (`Normal`, `MouseOver`, `Pressed`, `Disabled`), animating hover/press overlays with `MotionDurationFast` and `MotionEasing`.
- Modified `AccentButtonStyle` in `Themes/Controls.Designer code` to use VSM, fading in an `AccentHoverBrush` overlay on hover, and `HoverBackgroundBrush` overlay on press.
- Modified implicit `ListBoxItem` style in `Themes/Controls.Designer code` to use VSM for selection/hover transitions, incorporating a new `SelectionOverlay` for smooth select easing.
- Modified implicit `DataGridRow` style in `Themes/Controls.DataGrid.Designer code` to use VSM for selection/hover transitions, incorporating selection and hover overlays to prevent layout jumps or raw color flashes.
- **Refined Shell Components**:
- Replaced inline durations in `Views/Shell/ActivityRail.Designer code` with `{StaticResource MotionDurationFast}` and applied `MotionEasing`.
- Replaced inline durations in `Views/Shell/ModuleDetailPanel.Designer code` (for nav sub-items and the theme dark-mode toggle switch) with `{StaticResource MotionDurationFast}` and applied `MotionEasing`.

## Feature: UX-26

### Overview
Implemented content-shaped skeleton loaders (`SkeletonBlock` and `SkeletonPanel`) in `MerchSys.App` to replace the blanket full-screen busy overlay during first-load states on high-traffic dashboards and large lists. Skeletons provide content silhouettes (Cards for dashboards, Rows for lists) with a gentle horizontal shimmer, resolving layout jumps and reducing perceived loading latency.

### Requirements
- **Theme Additions:** Added `SkeletonBaseBrush` (SolidColorBrush) and `SkeletonShimmerHighlightColor` (Color) to [Light.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Themes/Light.Designer code) and [Dark.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Themes/Dark.Designer code) to ensure skeletons color-match their active theme.
- **SkeletonBlock Component:** Created [SkeletonBlock.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/SkeletonBlock.Designer code) and [SkeletonBlock.Designer code.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/SkeletonBlock.Designer code.vb) which renders a rounded rectangle skeleton shape with a linear gradient shimmer overlay.
- **SkeletonPanel Component:** Created [SkeletonPanel.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/SkeletonPanel.Designer code) and [SkeletonPanel.Designer code.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/SkeletonPanel.Designer code.vb) offering pre-defined dashboard metric cards (`Kind="Cards"`) and data grid table rows (`Kind="Rows"`) layouts.
- **BusyOverlay Improvements:** Enhanced [BusyOverlay.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/BusyOverlay.Designer code) and [BusyOverlay.Designer code.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/BusyOverlay.Designer code.vb) with `IsBusy` and `LastLoadedAt` dependency properties and style triggers. It automatically collapses itself during first-load to let the skeleton render, and shows during reloads/refreshes.
- **View Integration:** Integrated skeletons and conditional layout toggles into 7 major screens:
- **Dashboards (Cards):** `StockDashboardView`, `PurchasingDashboardView`, `OwnerDashboardView` (uses `IsLoading`), `FinancialOverviewView`
- **Lists (Rows):** `PurchaseOrderListView`, `ProductManagementView`, `TransactionHistoryView`

## Feature: UX-27

### Overview
Implemented the Keyboard-Shortcut Discoverability Overlay (`UX-27`) cheat sheet for `MerchSys.App`. The component lists all active global shortcut keys, dialog/form hotkeys, and cheatsheet commands. It is opened by pressing `?` or `Ctrl+/` and closed by pressing `Esc` or clicking the overlay backdrop. It honors user roles by conditionally displaying developer-only shortcuts, coordinates with the command palette overlay to ensure mutual exclusion, and guards the bare `?` gesture to prevent text entry hijacking in input controls.

### Requirements
- **ShortcutsOverlay View & VM**:
- Created `ShortcutsOverlay.Designer code` and `ShortcutsOverlay.Designer code.vb` under `Views/Shell/`. It maps a modal-style layout reusing the command palette design (dimmed backdrop, centered panel with large corner radius, and drop shadow).
- Created `ShortcutsOverlayPresenter.vb` under `Presenters/Shell/`. It manages `IsOpen` and exposes `IsDeveloper` checking if the active role is `Developer`.
- Registered both components as Singletons in the DI container in `Application.Designer code.vb`.
- **Keyboard Hooking & Guards**:
- Extended `MainWindow_PreviewKeyDown` in `MainWindow.Designer code.vb` to catch `Escape` to close the shortcuts overlay if visible.
- Implemented the `?` guard: if the user types a bare `?` (Shift+OemQuestion) while focus is inside a `TextBoxBase` or `PasswordBox`, the event is bypassed to allow text input. If pressed outside a text field, it toggles the shortcuts overlay.
- Wired `Ctrl+/` (Ctrl+OemQuestion) to toggle the shortcuts overlay unconditionally.
- **Mutual Exclusion Overlay Sync**:
- Wired property-change listeners in `MainWindowPresenter.vb` such that opening the shortcuts overlay automatically closes the command palette, and opening the command palette automatically closes the shortcuts overlay.
- Updated `CanNavigate` and `CanSelectModule` inside `MainWindowPresenter.vb` to block commands whenever either overlay is open.
- **Cheatsheet Content Layout**:
- Grouped shortcut descriptions into Global Navigation (Ctrl+K, Ctrl+1..4, and developer-only Ctrl+D0) and Dialogs/Forms (Enter to confirm, Esc to cancel/close, Alt+Letter mnemonics, and overlay controls).
- Styled hotkey labels as round theme-resilient chips using dynamic border and text secondary color tokens.

## Feature: UX-28

### Overview
Implemented Search & Filter UX Maturity (**UX-28**) to introduce live "{shown} of {total}" count indicators, removable filter chips, a "Clear all" action, a differentiated empty state when filters yield zero results, and in-memory session filter persistence across views.

### Requirements
- **Shared Kernel Changes:**
- **[FilterChipItem.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Interfaces/FilterChipItem.vb) **: models active filter chips with a display text, filter key, and a deletion callback command.
- **Shared UI Component Changes:**
- **[FilterSummaryBar.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/FilterSummaryBar.Designer code) **: custom user control layout utilizing a responsive `WrapPanel` showing the count, active filter chips, and the "Clear all" button.
- **[FilterSummaryBar.Designer code.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/FilterSummaryBar.Designer code.vb) **: code-behind managing the shown count, total count, active chips list, clear-all command, and formatting logic.
- **Stock Inventory Dashboard Changes:**
- **[StockDashboardPresenter.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Presenters/StockDashboardPresenter.vb) **: injected `ISessionService`, added session persistence backing fields, and integrated dynamic empty state properties (`EmptyStateTitle`, `EmptyStateDescription`, `EmptyStateActionCommand`, `EmptyStateActionText`).
- **[StockDashboardView.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Inventory/StockDashboardView.Designer code) **: added `FilterSummaryBar` below the toolbar and bound `EmptyStatePanel` to the dynamic VM properties.
- **Product Management Changes:**
- **[ProductManagementPresenter.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Presenters/ProductManagementPresenter.vb) **: added total count, session filters memory, active chips collection, and dynamic empty state properties.
- **[ProductManagementView.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Inventory/ProductManagementView.Designer code) **: added `FilterSummaryBar` and updated `EmptyStatePanel` bindings.
- **Purchase Order Management Changes:**
- **[PurchaseOrderListPresenter.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Presenters/PurchaseOrderListPresenter.vb) **: implemented session memory filters, active chips, clear-all, total count, and dynamic empty states.
- **[PurchaseOrderListView.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Purchasing/PurchaseOrderListView.Designer code) **: added `FilterSummaryBar` and updated `EmptyStatePanel` bindings.

## Feature: UX-29

### Overview
Implemented **Notification Actions & Undo** (toast action buttons + soft-delete undo window) to provide a safety net for routine reversible deletions across VISTA modules, extending the cross-cutting notification service in a backward-compatible manner.

### Requirements
- **Created** `Interfaces/NotificationAction.vb` — core model carrying the action button label and callback delegate.
- **Modified** `Interfaces/INotificationService.vb` — added `Optional action As NotificationAction = Nothing` parameters to all toast notification signatures (`ShowSuccess`, `ShowError`, `ShowInfo`, `ShowWarning`) to maintain backward compatibility.
- **Modified** `Services/DefaultNotificationService.vb` — implemented optional action parameters to render action buttons via `Notification.WinForms` using its built-in `LeftButtonContent` and `LeftButtonAction` properties.
- **Modified** `Services/IVendorService.vb` & `VendorService.vb` — added and implemented `RestoreAsync(id)` to reverse vendor soft-deletes by clearing `IsDeleted` and `DeletedAt`.
- **Modified** `Services/IVendorProductService.vb` & `VendorProductService.vb` — added and implemented `RestoreCatalogEntryAsync(id)` to reverse vendor catalog entry soft-deletes by clearing `IsDeleted`, `DeletedBy`, and `DeletedAt`.
- **Modified** `Presenters/VendorListPresenter.vb` — injected `INotificationService` and wired the "Undo" success toast action after vendor soft-delete. The callback uses a timestamp/closure mechanism for a time-box (8s) and `hasUndone` flag to guarantee idempotency.
- **Modified** `Presenters/VendorCatalogPresenter.vb` — wired the "Undo" success toast action after vendor product catalog entry removal with 8s time-box and idempotency constraints.
- **Modified** `Presenters/ProductManagementPresenter.vb` — injected `INotificationService` and wired the "Undo" success toast action after product category soft-delete. The callback restores the category via the DbContext inside a concurrency-handled block (`ConcurrencyHelper.ExecuteWithConflictPromptAsync`).

## Feature: UX-30

### Overview
Implemented the POS Keyboard-First Fast Path (`UX-30`) in the `SalesCartView` and `SalesCartPresenter`. This provides a fully keyboard-driveable cart experience: scan-hot focus discipline (keeping the SKU/scan field focused), cart-scoped hotkeys for quantity and discount adjustment, a local hold/recall cart mechanism, and Enter-to-commit on tender.

### Requirements
- **Focus Discipline**:
- Implemented automatic refocusing of `ProductSearchBox` on view load, after any addition/removal/clear of items (via `CartLines.CollectionChanged`), after inline edits commit or cancel (via `CartDataGrid.CellEditEnding`), and after successful sale completion (via `TransactionCompleted` event).
- Used `Dispatcher.BeginInvoke` with `DispatcherPriority.Input` priority to ensure focus calls execute reliably after the WinForms layout cycles complete.
- **Cart-Scoped Hotkeys**:
- Wired `PreviewKeyDown` on `SalesCartView` to capture key shortcuts locally:
- `Ctrl+Q`: Focus the Quantity column in the selected grid row and begin editing.
- `Ctrl+D`: Focus the Discount column in the selected grid row and begin editing.
- `Ctrl+T`: Focus the `AmountTenderedTextBox` and select its text.
- Added native mnemonics using underscores:
- `Set _Quantity (Ctrl+Q)` button
- `Apply _Discount (Ctrl+D)` button
- `_Hold` button (Alt+H)
- `_Recall` button (Alt+R)
- `Amount _Tendered (₱)` label pointing to `AmountTenderedTextBox` (Alt+T focuses the text box)
- Bound `Enter` key on `ProductList` to trigger `AddToCartCommand`.
- Bound `Enter` key on `CreditCustomerSearch` to trigger `SearchCreditCustomerCommand`.
- **Enter-to-Commit on Tender**:
- Set `IsDefault="True"` on the `PAY` button.
- Intercepted `Enter` on `ProductSearchBox` to trigger `SearchAndAddProductCommand` and marked it handled to prevent it from triggering the default `PAY` button.
- **In-Memory Hold/Recall**:
- Added `GetCartAsync` to `ICartService`/`CartService` to retrieve cached carts by Guid.
- Added `HeldCartId` and `HasHeldCart` to `SalesCartPresenter`.
- Added `HoldCartCommand` and `RecallCartCommand` to `SalesCartPresenter` supporting local swap of active cart and held cart GUIDs without dropping cart state from memory.
- **Realization & Concurrency Guarding**:
- Checked `IsBusy` inside `CanPay` and set it synchronously in `ProcessPaymentAsync` before any asynchronous yields to completely block double-firing on key repeat.

## Feature: UX-31

### Overview
Completed the rollout of `FilterSummaryBar`, `FreshnessChip`, and `SkeletonPanel` components to all remaining target data views (UX-31). This sweep ensures a consistent, high-fidelity experience when loading and filtering data. Additionally, successfully relocated core presentation contracts from `SharedKernel/Interfaces/` to a dedicated `SharedKernel/Presentation/` folder and namespace (Scope D) to maintain modular monolith boundary integrity.

### Requirements
### 1. Presentation-Contract Relocation (Scope D)
- Relocated contracts `IFreshnessAware`, `FilterChipItem`, `NotificationAction`, `ConfirmationRequest`, and `IConfirmationPresenter` to `SharedKernel/Presentation/`.
- Updated their declarations to `Namespace Presentation`.
- Updated all consumer imports and DI references in all module libraries and `MerchSys.App`.
- Verified no prohibited module-to-app dependencies were created.

## Feature: UX-32

### Overview
Completed the Interaction Completeness Sweep (UX-32). This includes establishing role-aware empty states with targeted call-to-actions (CTAs) for Manager roles (hiding CTAs for Owner roles), and implementing logical keyboard navigation (TabIndex order, Alt mnemonics, and dialog keyboard hooks) on remaining views.

### Requirements
### 1. Role-Aware Empty-State Panels
We implemented/updated `EmptyStatePanel` instances across the list views as follows:
| View | CTA Command | Role Behavior | Description |
|---|---|---|---|
| `StockDashboardView` | None | Informational | Standard informational state |
| `TransactionHistoryView` | None | Informational | Standard informational state |
| `ShrinkageView` | `OpenDialogCommand` | Gated (Manager/Dev only) | Manager sees "Record shrinkage", Owner sees info only |
| `ExpiryMonitorView` | None | Informational | Standard informational state |
| `APLedgerView` | None | Informational | Standard informational state |
| `ReorderSuggestionsView` | `GenerateCommand` | Gated (Manager/Dev only) | Manager sees "Generate suggestions", Owner sees info only |
| `GoodsReceivingView` | None | Informational | "No submitted purchase orders awaiting receipt" info text |

## Feature: UX-33

### Overview
Implemented comprehensive guardrails (Confirmation dialogs and/or Undo notifications) for all destructive/data-altering write paths across the Inventory, Purchasing, and POS modules to achieve complete UX guardrail parity.

### Requirements
- **Services/IPurchaseOrderService.vb**: Added `RestoreDraftAsync(id As Integer)` interface signature.
- **Services/PurchaseOrderService.vb**: Implemented `RestoreDraftAsync` to ignore query filters and restore deleted draft purchase orders.
- **Presenters/PurchaseOrderListPresenter.vb**: Wired confirmation prompt and time-boxed Undo notification on PO draft deletion.
- **Presenters/ShrinkagePresenter.vb**: Injected `IConfirmationPresenter` and routed `ExecuteRecordAsync` through typed confirmation ("RECORD").
- **Presenters/ProductManagementPresenter.vb**: Added Undo toast notification when deactivating a product in `ToggleActiveAsync`.
- **Presenters/CreditManagementPresenter.vb**: Injected `IConfirmationPresenter`, defined `ToggleBlockCommand`, and implemented `ToggleBlockAsync`/`CanToggleBlock` with typed confirmation and Undo toast notifications.
- **Views/POS/CreditManagementView.Designer code**: Added the "Block Credit" / "Unblock Credit" toggle button to the customer detail panel with appropriate style/text triggers.
- **Presenters/TransactionHistoryPresenter.vb**: Defined `VoidTransactionCommand` and implemented `VoidTransactionAsync` using typed confirmation ("VOID").
- **Views/POS/TransactionHistoryView.Designer code**: Added the "Void Transaction" button bound to `VoidTransactionCommand` with `IsEnabled="{Binding CanEdit}"`.

## Feature: UX-34

### Overview
Implemented visual polish and loose-end features for UX finishing touches (UX-34):
1. **Two-state password reveal**: The reveal buttons for password fields toggle their iconography between `IconEyeGeometry` and `IconEyeOffGeometry` based on visual state. Removed the dead `IconChevronLeftGeometry` resource.
2. **Reduced-motion listener (restart-required scope)**: Added a `SystemParameters.StaticPropertyChanged` listener that re-runs `UpdateMotionSettings()` and rewrites the motion tokens (150ms/220ms ↔ zero) in the app resource dictionary when the user changes the Windows "show animations" setting mid-session. **Scope note (corrected during review):** this updates only runtime-read consumers — the `MotionEnabled` gate (live, e.g. `SkeletonBlock`) and content resolved after the change. The actual view/control animations consume `MotionDuration*` via `StaticResource` inside sealed template/trigger `Storyboard`s and `VisualTransition.GeneratedDuration`, which are baked when the template is sealed and cannot carry `DynamicResource`. So **already-open windows keep their startup durations until the app restarts** (the plan's Option 2 outcome for the durations). Listener is detached cleanly in `Application_Exit` (no leak).
3. **Shortcuts overlay confirmation group**: Added the "Confirmation Dialogs" category to the shortcuts cheat sheet overlay, detailing `Enter`, `Esc`, and `Type Match` interactions.
4. **ConfirmationDialog DI convention**: Retained direct `New` instantiation for `ConfirmationDialog` and documented this convention in the dependency injection registry.

### Requirements
- **Modified** [Icons.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Themes/Icons.Designer code) — Removed unused `IconChevronLeftGeometry` resource to eliminate dead-resource warnings.
- **Modified** [LoginView.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/LoginView.Designer code) — Applied `DataTrigger` styles on the `Path` inside the password reveal buttons to swap icons dynamically based on `ShowPassword` and `ShowNewPassword` bindings.
- **Modified** [Application.Designer code.vb](file:///c:/Users/Admin/Documents/VISTA_Project/Application.Designer code.vb) — Implemented `UpdateMotionSettings()` and hooked `SystemParameters.StaticPropertyChanged` on startup to handle mid-session OS animation settings changes. Unsubscribed the listener in `Application_Exit`.
- **Modified** [ShortcutsOverlay.Designer code](file:///c:/Users/Admin/Documents/VISTA_Project/Views/Shell/ShortcutsOverlay.Designer code) — Added the "Confirmation Dialogs" help group.
- **Modified** [di-registry.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/codebase_wiki/schemas/di-registry.md) — Documented the `ConfirmationDialog` direct instantiation convention.
- **Modified** [WinForms-vista-motion.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/agent_wiki/patterns/WinForms-vista-motion.md) — Updated the reduced motion design pattern documentation with live property tracking.
- **Modified** [log.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/agent_wiki/log.md) — Added chronological log entry for UX-34.

## Feature: UX-35

### Overview
Resolved the SharedKernel presentation-contract boundary question raised in `ux_review_report.md` §2.3.
This is a **decision + documentation** item with **no code change** (the optional relocation is folded
into UX-31). The decision: the five presentation contracts (`IFreshnessAware`, `FilterChipItem`,
`NotificationAction`, `ConfirmationRequest`, `IConfirmationPresenter`) **stay in `SharedKernel`**.

### Requirements
- **Assessment (the decision gate):** grepped the consumer graph over `src/`. Result — all five
contracts are consumed by **all four module libraries** (Inventory, Purchasing, POS, Accounting), not
only `MerchSys.App`.
- **Decision: Option B (keep in SharedKernel), variant B-minimal (document now).** Options A
(relocate to `MerchSys.App`) and C (split `FilterChipItem` out) are **rejected** — they would force a
module → `MerchSys.App` reference, violating the modular-monolith rule ("each module references
`SharedKernel` only"). `SharedKernel` is the shared kernel, so it legitimately hosts the cross-cutting
presentation contracts every module's Presenters need. §2.3 is a *clarity* concern, not a *location*
defect.
- **LLM_Wiki/agent_wiki/patterns/WinForms-vista-presentation-contracts.md**: the convention, the
consumer-graph evidence, the boundary rationale, and the "new presentation contracts go in SharedKernel"
rule.
- Updated `LLM_Wiki/agent_wiki/index.md` (new row) and `log.md` (new entry).
- Folded the optional **B-tidy** relocation (group the five contracts under a `SharedKernel/Presentation/`
namespace for legibility) into **UX-31** as Scope item D — UX-31 already edits the same consumer
Presenters, so it absorbs the `Imports` change in one pass. Updated UX-31's frontmatter
(`depends-on` += UX-35; `estimated-files` 14→20), prerequisite note, Scope, watch-items, and
acceptance criteria accordingly.

## Feature: UX-36

### Overview
Implemented WCAG 2.2 AA accessibility requirements for `MerchSys.App`. Swept interactive controls, icon-only buttons, grids, and KPI tiles with descriptive screen-reader names (`AutomationProperties.Name` and `AutomationProperties.HelpText`). Implemented modal focus-trapping behavior on the three application overlays: `CommandPalette`, `ConcurrencyConflictPrompt`, and `ConfirmationDialog` using a new attached dependency property behavior `AccessibilityHelper.IsFocusTrap`.

> **Scope note (2026-06-06, claude-code):** UX-36 originally also added a `HighContrast` theme (third palette + 3-way theme selector). That part was **reverted at user request** — it was judged not beneficial and added UI noise. Light/Dark remain the only themes. The screen-reader naming and modal focus-trap work documented below was kept.

### Requirements
- Created `Helpers/AccessibilityHelper.vb` with the `IsFocusTrap` attached dependency property to cycle tab navigation within a modal container.
- Applied focus traps to:
- `Views/Shell/ConfirmationDialog.Designer code` (on the root Window)
- `Views/Shell/ConcurrencyConflictPrompt.Designer code` (on the root Window)
- `Views/Shell/CommandPalette.Designer code` (on the centered search panel border)
- Named the primary button in `Views/Shell/ConcurrencyConflictPrompt.Designer code` as `RefreshButton` and focused it on the window `Loaded` event in `ConcurrencyConflictPrompt.Designer code.vb`.
- Completed an accessibility sweep by adding `AutomationProperties.Name` and `AutomationProperties.HelpText` to:
- Navigation buttons in `Views/Shell/ActivityRail.Designer code`
- Password reveal buttons in `Views/LoginView.Designer code`
- Refresh button in `Views/Shell/FreshnessChip.Designer code`
- Reload POs button in `Views/Purchasing/GoodsReceivingView.Designer code`
- `Views/Accounting/Components/VatPayableTile.Designer code` (making the border focusable and adding a KeyDown handler to execute the navigation command in `VatPayableTile.Designer code.vb`)
- Cart row styles, payment select buttons, and item removal buttons in `Views/POS/SalesCartView.Designer code`
- Ledger row styles in `Views/Purchasing/APLedgerView.Designer code`
- Suggestion and config row styles in `Views/Purchasing/ReorderSuggestionsView.Designer code`

## Feature: UX-37

### Overview
UX-37 — Performance & Perceived Performance: Virtualization, Async Coverage & Optimistic UI.

Three levers were applied: (A) container-recycling virtualization on all unbounded DataGrids,
(B) async-coverage audit of every growable view against the UX-15 standard, and (C) an
optimistic-UI pilot on the VAT settings write path.

### Requirements
### A. Virtualization — `VirtualizingStackPanel.VirtualizationMode="Recycling"` enabled
Added the attribute to all unbounded/growable DataGrids:
| File | DataGrid | Collection |
|---|---|---|
| `Views/POS/TransactionHistoryView.Designer code` | `TransactionGrid` | `Transactions` |
| `Views/Accounting/TamperAuditReportView.Designer code` | *(unnamed)* | `Entries` |
| `Views/Purchasing/APLedgerView.Designer code` | `APGrid` | `Entries` |
| `Views/Inventory/ProductPriceHistoryView.Designer code` | `HistoryGrid` | `HistoryItems` |
| `Views/POS/CreditManagementView.Designer code` | `AccountsDataGrid` | `Accounts` |
| `Views/Purchasing/PurchaseOrderListView.Designer code` | `POGrid` | `Orders` |
| `Views/Purchasing/VendorDirectoryView.Designer code` | `VendorGrid` | `Vendors` |
| `Views/Inventory/ShrinkageView.Designer code` | *(unnamed)* | `HistoryItems` (shrinkage history) |
| `Views/Accounting/VatReturnView.Designer code` | *(unnamed)* | `Lines` (VAT return line items) |
**Virtualization-killer audit (all growable transactional grids):** None of the nine grids above is wrapped in an outer `ScrollViewer`; each has a constrained height (parent Grid row `Height="*"`, DockPanel last-child fill, or fixed-size Window), so virtualization is active. No `Height="Auto"` ancestor defeats it.
**Report/dashboard grids — surveyed, intentionally skipped:** `SalesSummaryView.DailyBreakdown`, `IncomeStatementView.ProductMargins`, `FinancialOverviewView.TopProducts`, and `PurchasingDashboardView.TopVendors`/`.PendingPurchaseOrders` *do* sit inside a page-level `ScrollViewer` (the whole report scrolls as one surface — `CanContentScroll` is the default `False`, so these realize all rows). Recycling was **not** applied because their row counts are bounded by the report period (≤31 days) or a top-N projection, so full realization is acceptable. If any is rebound to an unbounded source, lift it out of the page `ScrollViewer` (or set `CanContentScroll="True"`) and enable recycling.

## Feature: UX-38

### Overview
Implements UX-38: Personalization & Workspace Memory. A small `IUserPreferencesService` (backed by the existing `UiSettingsStore`) persists per-laptop user preferences—last-viewed screen, favorites list, and bounded recents list—without touching any business data or query paths.

### Requirements
### New Files
- `Services/IUserPreferencesService.vb` — interface with `GetLastViewKey`, `RecordNavigation`, `GetFavorites`, `GetRecents`, `ToggleFavorite`, `IsFavorite`, `SyncFavoriteFlagsToItems`
- `Services/UserPreferencesService.vb` — implementation; depends on `UiSettingsStore`; recents capped at 10, de-duplicated, most-recent-first

## Feature: UX-41

### Overview
Implemented UX-41: Advanced Data Visualization — hover tooltips with period labels on sparkline chart
points, drill-down from KPI cards to module detail views via `NavigateCommand`, and 7/30/90-day
period selectors on a new Revenue Trend card backed by an additive read-only raw `MySqlConnector`
query.

### Requirements
### A. Hover Tooltips on Chart Points
- Modified `Views/Shell/Sparkline.Designer code.vb`:
- Added `Label As String` property to `SparklineBarItem`
- Added `Labels As IEnumerable(Of String)` dependency property to `Sparkline`
- Added `OnLabelsChanged` DP callback that re-runs `UpdateBars()`
- `UpdateBars()` now zips `Points` with `Labels` (by index) to populate each bar's `Label`
- Modified `Views/Shell/Sparkline.Designer code`:
- Updated `Rectangle.ToolTip` from a plain `TextBlock` to a `StackPanel` showing:
- Period label `TextBlock` (collapses via `Trigger Property="Text" Value=""` when label is empty — preserves backward compatibility with existing unlabelled sparklines)
- Value `TextBlock` with `FormatCurrencyNoDecimal` — unchanged from UX-12

## Feature: UX-42

### Overview
Implemented UX-42: Print & Export UX — BIR Official-Receipt print template (FlowDocument/PrintDialog) and CSV/text-based report export with print preview for Accounting reports.


---

### Requirements
### Part A — BIR Official-Receipt Print Template
- Modified `Presenters/SalesCartPresenter.vb`
- Added `Private _lastCartLines As New List(Of CartLineItem)()` backing field
- **Public ReadOnly Property LastCartLines As IReadOnlyList(Of CartLineItem)**: snapshot available for print
- Added `_lastCartLines = CartLines.ToList()` in `ProcessPaymentAsync` before `CartLines.Clear()`
- Modified `Views/POS/SalesCartView.Designer code`
- Added "Print Official Receipt" button inside receipt preview section (`IsReceiptVisible`-gated `Border`), `TabIndex=15`
- Modified `Views/POS/SalesCartView.Designer code.vb`
- Added `Imports System.Windows.Documents`, `System.Windows.Media`, `MerchSys.POS.Entities`
- **PrintOrButton_Click**: builds FlowDocument from `CurrentReceipt` + `LastCartLines`, calls `PrintDialog`
- Added `BuildOrFlowDocument` (Shared) — full BIR layout: header (name/address/TIN/VAT status), OR No., date, item table, totals, VAT disclosure block (VATable Sales/VAT-Exempt/Zero-Rated/Output VAT), footer

## Feature: UX-43

### Overview
Implemented the Living Design-System Gallery — a Developer-role-only view that catalogues every design token (color/brush, type ramp, spacing, radii, motion), every shared component in its states (BusyOverlay, EmptyStatePanel, ErrorStatePanel, DeltaIndicator, Sparkline, FieldRow, ConfirmationDialog preview), and all 20 icons from Icons.Designer code. Pure Designer code presentation — no Presenter, no commands, no data contracts.

### Requirements
- **Views/DeveloperTools/DesignGalleryView.Designer code**: scrollable gallery UserControl with three labelled sections (A: Tokens, B: Components, C: Icons). All color swatches, component renders, and icon fills use `{DynamicResource}` so the gallery recolors live on the Light ↔ Dark toggle (Ctrl+T). No inline hex anywhere.
- **Views/DeveloperTools/DesignGalleryView.Designer code.vb**: minimal code-behind; only `InitializeComponent()`.
- **Startup/DebugServiceRegistration.vb**: added `services.AddTransient(Of Views.DeveloperTools.DesignGalleryView)()`.
- **Presenters/MainWindowPresenter.vb**: added `"Design Gallery"` nav item as the first entry in `BuildDeveloperToolsItems()`. Role gate is already in place (`If _session.CurrentRole <> UserRole.Developer Then Return New List …`), so the gallery is invisible to Manager and Owner.

## Feature: UX-46

### Overview
Implemented a comparative layout for the Income Statement (Profit & Loss) view and fixed several readability defects on the same screen:
1. **Prior-period comparison column + Δ**: Added an extra column in the UI for the prior period's numbers, which are already computed by the view model, and wired standard delta indicators (percent change + direction) for each line.
2. **COGS legibility**: Un-muted the Cost of Goods Sold line to make its magnitude clear.
3. **Disambiguated OpEx roll-up**: Structured operating expenses by showing the components first (indented "Other Operating Expenses" and "Shrinkage Loss"), followed by the subtotal "Total Operating Expenses".
4. **Document-width layout**: Centered and constrained the P&L grid to a beautiful paper layout (`MaxWidth="800" HorizontalAlignment="Center"`), preserving responsiveness.
5. **Friendly month labels**: Exposed a `{Number, Name}` list (`MonthOption`) to combo-box bindings, displaying month names (e.g., "June") instead of raw numbers without breaking the integer-value round-trip.
6. **Export parity**: Extended both CSV and PDF exports to output the comparative prior-period values and percentage changes.

### Requirements
- Modified [IncomeStatementPresenter.vb](file:///C:/Users/Admin/Documents/VISTA_Project/Presenters/IncomeStatementPresenter.vb):
- Defined nested `MonthOption` class.
- Updated `AvailableMonths` to return list of `MonthOption` options.
- Added new read-only properties for prior values: `PrevPeriodLabel`, `PrevNetSalesDisplay`, `PrevCOGSDisplay`, `PrevGrossProfitDisplay`, `PrevOtherOperatingExpensesDisplay`, `PrevShrinkageLossDisplay`, `PrevOperatingExpensesDisplay`, `PrevNetIncomeDisplay`, `OtherOperatingExpensesDisplay`, `PrevGrossMarginPercentDisplay`, `PrevNetMarginPercentDisplay`.
- Added delta values and visibility properties: `NetSalesDelta`, `ShowNetSalesDelta`, `COGSDelta`, `ShowCOGSDelta`, `GrossProfitDelta`, `ShowGrossProfitDelta`, `OtherOperatingExpensesDelta`, `ShowOtherOperatingExpensesDelta`, `ShrinkageLossDelta`, `ShowShrinkageLossDelta`, `OperatingExpensesDelta`, `ShowOperatingExpensesDelta`, `NetIncomeDelta`, `ShowNetIncomeDelta`.
- Implemented `CalculateDelta` helper function to compute percentage changes relative to the baseline, defaulting to hidden delta when the prior baseline is zero/missing.
- Updated `LoadDataAsync` to extract values from `prevResult` and populate the new properties.
- Modified [IncomeStatementView.Designer code](file:///C:/Users/Admin/Documents/VISTA_Project/Views/Accounting/IncomeStatementView.Designer code):
- Replaced the month selector combo box with one utilizing `SelectedValuePath="Number"` and `DisplayMemberPath="Name"`.
- Redesigned the main P&L grid to center and constrain to a paper width (`MaxWidth="800"` inside a centered parent container).
- Re-mapped the grid columns to four columns (**Line Item | Current | Prior | Change**).
- Un-muted the COGS row styling by switching from `Muted` styles to regular `LineLabel`/`LineAmount` styles.
- Re-structured the Operating Expenses section to show "Other Operating Expenses" and "Shrinkage Loss" first, followed by the "Total Operating Expenses" subtotal.
- Wired `views:DeltaIndicator` controls for Net Sales, COGS, Gross Profit, Other Operating Expenses, Shrinkage Loss, Total Operating Expenses, and Net Income, mapping `InvertSemantics` correctly for expense lines.
- Modified [IncomeStatementView.Designer code.vb](file:///C:/Users/Admin/Documents/VISTA_Project/Views/Accounting/IncomeStatementView.Designer code.vb):
- Updated the CSV builder `BuildIncomeStatementCsv` to include the prior-period amounts and delta percentages.
- Redesigned the plain-text PDF report builder `BuildIncomeStatementReport` to support a wider comparative layout (72-column grid) with dynamic headers, prior columns, and aligned delta values.
- Added `FormatDeltaPercent` and `FormatReportLine` formatting helpers.

## Feature: UX-47

### Overview
This task implements severity-aware plain-language "What This Means" insight callouts on the dashboard and accounting reports. Instead of displaying all interpretations in a static accent blue, callouts now dynamically shift colors and icon shapes based on severity (Info, Positive, Warning).

### Requirements
- **Enums**: Created `Enums/InsightSeverity.vb` to support crossing the module boundary since module Presenters (e.g. `IncomeStatementPresenter` in `MerchSys.Accounting`) reference only `SharedKernel`.
- **Services**: Modified `IWhatThisMeansService.vb` and `WhatThisMeansService.vb` to add additive methods (`GetOverviewSeverity`, `GetIncomeStatementSeverity`, `GetSalesSummarySeverity`) which map existing data signals to `InsightSeverity` using identical thresholds:
- `IncomeStatement`: `Warning` if net income is negative (takes priority) or, against a prior baseline, gross margin dropped by $\ge 3\%$ (`MarginDropWarningThreshold`); `Positive` only when a prior baseline exists and gross margin improved; `Info` otherwise — including the no-baseline (first-period) case, so tone stays neutral to match the interpretation text, which makes no comparison claim without a baseline.
- `FinancialOverview`: `Warning` if overdue AR count $> 0$ or low stock alerts $> 0$; `Positive` if MTD revenue is higher than prior month MTD; `Info` otherwise.
- `SalesSummary`: `Warning` if credit (utang) percentage $\ge 30\%$ (`CreditWarningThreshold`); `Info` otherwise.
- **Presenters**: Added `WhatThisMeansSeverity As InsightSeverity` to `FinancialOverviewPresenter`, `IncomeStatementPresenter`, `SalesSummaryPresenter`, and `VatReturnPresenter` (VatReturn defaults to `Info` only per plan requirements). Updated the data loading paths to populate the severity.
- **UI Control**: Created the reusable `InsightBanner` control (`Views/Shell/InsightBanner.Designer code` + `.Designer code.vb`) with `Title`, `Text`, and `Severity` dependency properties. Styling is driven by Designer code `DataTrigger` styles applying `DynamicResource` brushes (`AccentBrush` for Info, `SuccessBrush` for Positive, `WarningBrush` for Warning) and distinct shapes (`IconLightbulbGeometry` for Info, `IconCheckGeometry` for Positive, `IconWarningGeometry` for Warning) to support accessibility (WCAG 1.4.1 compliance - color is not the sole indicator).
- **Views**: Migrated the four hand-rolled borders in `FinancialOverviewView.Designer code`, `IncomeStatementView.Designer code`, `SalesSummaryView.Designer code`, and `VatReturnView.Designer code` to the unified `<views:InsightBanner>` control.

## Feature: UX-48

### Overview
Implemented roadmap item **P13** (`ROADMAP-L3-pro.md`): the login window now renders a pure-vector
Philippine farm panorama that follows the real local time of day (five fixed bands, crossfaded),
with ambient motion (cloud drift, palay sway, star twinkle, fireflies at dusk/night, maya birds by
day, cursor parallax, a storefront sign that glows after dark) behind the existing login card — and
**Tanod the carabao**, a salakot-wearing mascot in a macOS-style circular badge that blinks and
breathes when idle, follows the username caret with its pupils, slides its salakot over its eyes on
password focus (lifts it to peek on show-password), raises its brows on CapsLock, chews while
authenticating, bounces on success, and droops with a macOS-style card shake on rejection, falling
asleep after 45 s of idle. Functional wins shipped alongside: the app's **first CapsLock warning
badge** and the **wrong-password card shake**. Presentation-only: auth flow, DA6, and
`IAuthenticationService` semantics are unchanged; the Presenter delta is one additive event.

### Requirements
- **Themes/LoginScene.Designer code**: all 55 per-phase `Color` tokens
(`Scene{Phase}{Element}Color`, 5 phases × 11 elements), phase-independent colors, and every
mascot/badge brush. **Every hex of the feature lives here**; the Views/ hex sweep stays at zero.
- **Views/Login/LoginScenePhase.vb**: `LoginScenePhase` enum +
`LoginScenePhaseProvider.GetPhase(TimeSpan)` pure band lookup (Dawn 05:00–06:29, Day –16:29,
Golden –17:59, Dusk –19:29, Night otherwise; equatorial PH ⇒ fixed bands, no solar math).
- **Views/Login/DynamicSceneCanvas.Designer code(.vb)**: the layered 1600×900
panorama in a `UniformToFill` viewbox. Animated fills are **local unfrozen brushes built in
code** (sky `LinearGradientBrush` stops + 8 `SolidColorBrush`es) seeded from the tokens;
crossfades are 4 s `ColorAnimation`/`DoubleAnimation` clocks. All loops are tracked
`AnimationClock`s in four lists (ambient / fireflies / birds / crossfade) with
`Start(staticMode)` / `Pause()` / `[Resume]()` / `StopAll()` lifecycle, 60 s phase
`DispatcherTimer`, throttled (~30 Hz) cursor parallax on three depth groups (±4/±7/±12 px),
`Timeline.SetDesiredFrameRate 30` on every ambient loop, and a `#If DEBUG`
**`VISTA_LOGIN_SCENE_HOUR`** override (mirrors `VISTA_BYPASS_LOGIN`) to force any phase.
Element caps per plan: 3 cloud groups, 18 stars (8 twinkling), 10 fireflies, 6 sway clusters,
2 birds.
- **Views/Login/CarabaoAvatar.Designer code(.vb)**: badge + bust geometry and the
eight VSM pose states (Idle, Watching, Shy, Peeking, Thinking, Happy, Rejected, Sleeping) as
zero-duration pose storyboards tweened by a `VisualTransition` bound to **`MotionDurationStd` +
`MotionEasing`** (UX-25 tokens). Pose changes go through `GoToElementState` (the
`[[WinForms-vsm-foreground-on-non-control-template-root]]` family of VSM traps; `GoToState` no-ops on
a UserControl). Code-driven motion (gaze, caps-alert brow raise, blink, ear flick, bounce,
breath, chew, zzz-float) uses **separate transforms composed with the VSM-driven ones** so the
two systems never fight over a property; idle loops are code-managed clocks so static mode runs
zero animations. API: `Initialize/Shutdown/Pause/[Resume]`, `GoToPose`, `SetGaze/ResetGaze`,
`SetCapsAlert`, `PlayRejected(thenPose)`, `PlaySuccessBeatAsync()`.
- **Views/LoginView.Designer code**: fixed 900×700 window (was 420×auto;
`NoResize`/`CenterScreen` kept), full-bleed scene + theme scrim + centered 384px card with the
avatar badge overlapping its top edge. Existing form markup, bindings, `PasswordBoxHelper`,
tab order, `IsDefault`, KeyBindings and the DA6 panel preserved; added `x:Name`s on the five
password inputs, the **CapsLock badge** (warning icon + caption, `WarningBrush`,
`AutomationProperties.LiveSetting="Assertive"`) beside the Password label, the same LiveSetting
on the error text, and a named `TranslateTransform` for the card shake.
- **Views/LoginView.Designer code.vb**: the full wiring matrix from the plan:
username focus/caret → `SetGaze` (username **only**; reveal boxes deliberately unwired); password
focus kind (Main/New/Confirm) → Shy/Peeking; `ShowPassword`/`ShowNewPassword` → peek toggle;
`IsLoggingIn` → Thinking; `LoginAttemptFailed` → shake + `PlayRejected` (skipped in static mode);
CapsLock re-checked on focus + window-level PreviewKey events; 45 s sleep timer with throttled
activity reset and wake; `Activated`/`Deactivated` → pause/resume. **Lifecycle:**
`IsVisibleChanged(False)`/`Closed`/success-beat completion all funnel into `FullStop()`
(scene `StopAll` + avatar `Shutdown` + sleep timer stop) — required because LoginView is
DI-transient and *hidden*, never closed, after success. Static mode =
`SystemParameters.ClientAreaAnimation`/`MotionEnabled` false **or** `RenderCapability.Tier < 2`.
- **Presenters/LoginPresenter.vb**: **additive only**: new
`LoginAttemptFailed` event raised in the six reject branches (login exception, `Not
result.Success`, password-change mismatch, lost pending user, change exception, `Not
changeResult.Success`). The DA6 redirect does **not** raise it. No other VM change.
- **Application.Designer code.vb**: `HandleLoginSucceeded` is now `Async Sub`:
`Await Task.WhenAny(_loginView.PlaySuccessBeatAsync(), Task.Delay(700))` inside `Try…Catch`
(Await in the Try body only — BC36943) before `Hide()`. Static mode returns a completed task ⇒
instant swap exactly as before.
- Modified `Themes/Light.Designer code` + `Themes/Dark.Designer code` — new token `LoginSceneScrimBrush`
(Light: opacity 0; Dark: black 12 %) so the dark card keeps contrast over a bright scene; keys
stay in parity per the UX-00 token contract.
No NuGet packages added; no bitmap/Lottie assets — stock WinForms vector + storyboards/clocks only.
Zero new user-facing settings (lean decision record): reduced motion rides the UX-25 OS gate.

## Feature: UX-49

### Overview
Replaced every pixel of the UX-48 login presentation after the owner's review ("no wow"; carabao
out, Filipino human avatar in) while keeping the UX-48 engineering contracts byte-compatible. The
login now renders a **dusk storefront hero shot** — the Villon Farm Supply facade at blue hour:
hanging lit bracket sign (swaying ±1.2°), scalloped green/cream awning, a 9-bulb string whose
middle bulbs glow as bokeh through the card, an open doorway and display window spilling warm light
(each with wall gradient, pavement pool, and wet reflection), distant town + coconut-palm
silhouettes, parked tricycle / sack stack / cat / potted plant / walis as rim-lit silhouettes,
fireflies, and a wet-pavement sheen. Real time of day survives as **three lighting moods** (Dusk
hero 05:00–18:59 · Evening 19:00–22:59 · LateNight 23:00–04:59 — game-title-screen art direction:
no clock hour can render a weak palette). **Aling Vi the tindera** replaces Tanod with the
identical pose API — she covers her eyes with both hands during password entry and peeks between
fingers on reveal. The card is now **live frosted glass** (blurred VisualBrush of the scene under a
theme tint), with an **entrance choreography**: the scene brightens, the bulb string lights up
left-to-right, the card rises, the badge pops. Login success additionally pulses the doorway glow
("welcome in"). Auth flow, DA6, `LoginPresenter`, and `Application.Designer code.vb` are untouched.

### Requirements
- Rewrote `Themes/LoginScene.Designer code` — 48 mood tokens (`Scene{Dusk|Evening|LateNight}{Element}Color`,
16 elements × 3 moods), 18 mood-independent scene brushes (bulb/moon halos, sheen, awning shadow,
silhouette fills), 24 avatar/badge tokens. **Every hex of the feature lives here.**
- Rewrote `Views/Login/LoginScenePhase.vb` — enum members now `Dusk / Evening / LateNight` with the
hero-hour bands; doc comment records the art-direction decision. `VISTA_LOGIN_SCENE_HOUR`
override unchanged (10 → Dusk, 20 → Evening, 1 → LateNight).
- Rewrote `Views/Login/DynamicSceneCanvas.Designer code` — full storefront layer tree per the plan's
composition map (sky → celestial → cloud streaks → town strip → facade → pavement/reflections →
foreground props → fireflies → dim overlay). Defect-record gates applied: `F1`/`Nonzero` on every
multi-figure filled path (D1), all readable elements inside the x∈[260,1340] safe area (D2 — the
sign is fully readable left of the card), every light source ships halo + cast gradient + ground
pool + reflection (D4), props are silhouettes + ≤2 warm rim strokes (D5).
- Rewrote `Views/Login/DynamicSceneCanvas.Designer code.vb` — 14 unfrozen code-built brushes (4-stop sky,
wall/pavement/glow gradients, pool/spill/halo radials, shared reflection gradient); ~25 color +
~24 double crossfade targets per mood change; **base values re-set after starting crossfades** so
`FillBehavior.Stop` one-shots never snap (new pattern §8); ambient set = 3 cloud oscillations
(±28px AutoReverse — no wrap), sign sway, 6 bulb twinkles, 6 star twinkles, 2 pavement sheens,
8 fireflies × 3 clocks (42 loops ≈ UX-48 budget; birds/palay retired); cat tail-flick on a
12–21s `DispatcherTimer` (suppressed in LateNight where the cat sleeps); 5-layer parallax
(±2/±2/±4/±7/±13); new public surface: `SceneVisual` (glass sampling), `PlayEntrance()`,
`PulseDoorGlow()`. Lifecycle (`Start/Pause/[Resume]/StopAll`), 60s phase timer, clock
bookkeeping, and static mode are UX-48's verbatim.
- Created `Views/Login/TinderaAvatar.Designer code(.vb)` / **deleted `CarabaoAvatar.Designer code(.vb)`** — same
public API, `AvatarPose` enum, VSM state names, and transform-ownership discipline, so
`LoginView` wiring compiled unchanged. New geometry: warm-tan face with wide-set eyes +
**catchlights** (D8), side-parted hair with low bun + sampaguita dots, gold hoop earrings, cream
blouse + green gingham apron + checkered panuelo, mitten hands (VSM-only, parked below the badge
clip at Y=78). Pose deltas: Shy = both hands rise over the eyes; Peeking = right hand slides
down/out 16°; Happy = hands raise beside cheeks + bounce; Thinking = head-bob loop (reuses the
chew-clock slot); brow-bounce every 3rd blink (replaces ear flick; suppressed while the CapsLock
alert holds the brows). Badge: warm amber→rose gradient ring + white inner ring + theme outer.
- **Views/LoginView.Designer code**: glass card stack (`CardChrome` border 1px
`LoginGlassHighlightBrush` + `RadiusLarge` + `CardShadow`; inside: `GlassBackdrop` rect with
`BlurEffect 18/Performance`, `GlassTint` rect, then the form). Form markup, bindings,
`PasswordBoxHelper`, tab order, CapsLock badge, error LiveSetting, and the DA6 panel are
byte-identical. Avatar swapped to `TinderaAvatar` (x:Name `Avatar` kept); entrance transforms
added (`CardEntranceTr`; unnamed `ScaleTransform` on the avatar — MC3093, see Issues).
- **Views/LoginView.Designer code.vb**: `SetupGlassCard()` (VisualBrush absolute-viewbox of
`SceneCanvas.SceneVisual`; static mode collapses the backdrop and swaps tint/border to
`SurfaceBrush`/`SeparatorBrush` = the old solid card); `UpdateGlassViewbox()` on `CardChrome.
SizeChanged` (covers the DA6 panel growing the card; never per-frame); `PlayCardEntrance()`
(keyframed card rise/fade + badge pop, all `FillBehavior.Stop` over final base values);
`PlaySuccessBeatAsync` now also fires `PulseDoorGlow()`. All UX-48 wiring (gaze, poses, caps,
shake, sleep, pause/resume, `FullStop()` teardown) untouched.
- Modified `Themes/Light.Designer code` + `Themes/Dark.Designer code` — `LoginGlassTintBrush` (Light: white 78%;
Dark: #23252E 74%) and `LoginGlassHighlightBrush` (Light: white 65%; Dark: white 14%), key
parity per the UX-00 contract. `LoginSceneScrimBrush` kept.
- Registered **P13a** in `ROADMAP-L3-pro.md` (full entry) and the consolidated map in
`ROADMAP-ui-ux-perfection.md`; extended agent-wiki pattern `WinForms-vista-animated-scene` (§7–§10)
+ index/log.
No NuGet packages, no bitmaps, no new user-facing settings. The plan's **optional greeting accent**
("Magandang umaga/hapon/gabi!") was **cut** per the lean rule and the P9 English-only descope —
it is a one-TextBlock add if the owner wants it back.

## Feature: UX-50

### Overview
The owner rejected UX-49's hand-coded **vector** storefront as unable to reach the reference
`Expectation.jpg`. Root cause was the **medium**: procedural shapes + UX-49's own D5 "silhouettes,
zero interior detail" rule are the opposite of a dense, warm, *smooth painterly* illustration. UX-50
switches the login hero to a **raster painterly illustration** rendered via `<Image>` (WinForms draws it
at native fidelity → the login looks exactly as good as the source art), then **removes** the entire
UX-48/49 procedural engine now that it is dead weight.

Art lane was decided after exploring three: CC0 pixel (pixelmateai Market Stalls — verified but too
cute as a hero), richer pixel (guttykreum Japanese Corner Store / LimeZu Modern Exteriors — still
pixel), and **generated painterly** — chosen, because the reference is smooth painterly lo-fi.

### Requirements
- **Generated the hero via Canva.** Adobe's MCP connector reports text-to-image is disabled in this
environment; **Canva `generate-design`** (design_type `desktop_wallpaper`) works. Pipeline:
`generate-design` → `create-design-from-candidate` → `export-design` (png 1600×900) → download the
presigned `export-download.canva.com` URL. Four demo-grade candidates resulted; the owner chose
**Candidate 1, "Cozy Sari-Sari Store at Dusk"** (Canva design `DAHMb96QyQM`) — warm bulb-lit
storefront on the right, blue-hour sky + skyline + crosswalk on the left, clear center for the card.
- **Placed the asset:** `Assets/Login/login-hero-dusk.png` (1600×900) + `Assets/Login/CREDITS.md`
(Canva Content License, design IDs, prompt). Alternates kept in `Screenshots/Login_Hero_Candidate*.png`.
`.vbproj` gained `<Resource Include="Assets\Login\*.png"/>` (mirrors the existing Inter-font pattern).
- **Wired it (v1):** inserted one opaque `<Image x:Name="HeroImage">` (`BitmapScalingMode=HighQuality`,
`UniformToFill`) as the scene, beneath `DimOverlay`. The frosted-glass `VisualBrush` now samples the
painting (its lit windows blur into real bokeh); the entrance scene-fade and sleep-dim still apply.
- **Pruned the dead engine.** With the painting carrying the scene, the UX-48/49 machinery was
removed: `DynamicSceneCanvas.Designer code` is now `HeroImage` + `DimOverlay`; `DynamicSceneCanvas.Designer code.vb`
shrank **~700 → ~95 lines** — the public surface (`Start`/`Pause`/`[Resume]`/`StopAll`/
`PlayEntrance`/`PulseDoorGlow`/`SetDimmed`/`SceneVisual`) is byte-preserved so **`LoginView` is
untouched**; `Views/Login/LoginScenePhase.vb` was deleted. The 60s mood clock, 4s palette
crossfades, ~40 ambient animation clocks, the cat-tail timer, and the cursor parallax (all of which
had been running on now-hidden layers) are gone — CPU reclaimed.
- `TinderaAvatar`, `LoginView.Designer code(.vb)`, the glass-card stack, DA6, auth, and `Application.Designer code.vb`
are unchanged. The avatar (Aling Vi) and all UX-48/49 mascot/CapsLock/shake/sleep wiring still fire.



## Verification (from UX-verification-checklist.md)

---
module: MerchSys.App
source: ux_review_report.md
originally-generated: 2026-06-05
last-synced: 2026-06-08
scope-extended: 2026-06-08 (broadened beyond the report into the single UX operator checklist; UX-33 → UX-43 appended)
---

# Operator Verification Checklist — UX (Experience)

> Originally extracted from `ux_review_report.md` (Verification & Testing Debt, §1.1, §1.2, §1.10);
> **broadened 2026-06-08** into the single UX operator checklist — it now also covers the manual
> verification owed by the post-report plans **UX-33 → UX-43** (their summaries each marked operator
> verification "pending"). Only **operator / manual** verification tasks are listed here — items that
> need a human to boot the app and look at the screen. The code for every item below is already
> implemented; this is the realization/verification pass, not a code backlog.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
>
> **Login required (INFRA-15):** The app shows a login screen on launch. Unless a test says otherwise,
> log in as `manager` (default password `Vista2026!`; first login forces a change).
>
> **Architecture note (2026-05-28 pivot):** the app now talks to centralized **MariaDB** (XAMPP on the
> host laptop), not local SQLite. "DB down" tests mean stopping MySQL in the XAMPP Control Panel on the
> host (or disconnecting this client from the LAN), **not** deleting a local `.db` file.

### Key file locations

| What | Path |
|------|------|
| UI settings (window state + theme + personalization) | `%LOCALAPPDATA%\MerchSys\ui-settings.json` — now also holds `lastViewKey`, `favoriteKeys`, `recentKeys` (UX-38) |
| WindowPlacementService | `WinForms_Applications\MerchSys\src\MerchSys.App\Services\WindowPlacementService.vb` |
| UiSettingsStore | `WinForms_Applications\MerchSys\src\MerchSys.App\Services\UiSettingsStore.vb` |
| MainWindow (placement hook-up) | `WinForms_Applications\MerchSys\src\MerchSys.App\MainWindow.Designer code.vb` |
| Reduced-motion (startup + live) | `WinForms_Applications\MerchSys\src\MerchSys.App\Application.Designer code.vb` — `Application_Startup` reads `SystemParameters.ClientAreaAnimation`; a `SystemParameters.StaticPropertyChanged` listener re-runs `UpdateMotionSettings()` mid-session (UX-34), detached in `Application_Exit` |
| Icon dictionary | `WinForms_Applications\MerchSys\src\MerchSys.App\Themes\Icons.Designer code` |
| Theme dictionaries | `WinForms_Applications\MerchSys\src\MerchSys.App\Themes\Light.Designer code`, `Dark.Designer code` |
| Theme toggle service | `WinForms_Applications\MerchSys\src\MerchSys.App\Services\Theming\ThemeService.vb` |
| Personalization (favorites/recents/last-view) | `WinForms_Applications\MerchSys\src\MerchSys.App\Services\UserPreferencesService.vb` |
| Design Gallery (Developer-only) | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\DeveloperTools\DesignGalleryView.Designer code` |

---

## UX-22 — Tooltips & Affordance

### Test 1: Tooltip chrome recolors on theme toggle

**What to do:**
1. Launch the app (F5) and log in as `manager`. Make sure the app is in **Light** theme.
2. Hover the mouse over every **icon-only** control you can find (toolbar icon buttons, the search/
   filter icons, refresh chips, the activity-rail module icons, password reveal eye button on login)
   and wait ~1 second for the tooltip to appear. Note the text and the tooltip's background/border.
3. Switch to **Dark** theme (theme toggle in the shell).
4. Hover the same controls again.

**What you should see:**
- Every icon-only control shows a tooltip with **correct, readable text** (no blank, no "PART_…",
  no binding error text).
- In Light theme the tooltip uses the light surface/foreground; in Dark theme the tooltip background
  and text **recolor** to the dark surface/foreground — it does **not** stay light-on-light or render
  with a stale hard-coded color.

- [ ] Tooltips show correct text on all icon-only controls (Light) —
- [ ] Tooltip chrome (background + text) recolors correctly in Dark —

---

## UX-23 — Window-State Persistence

> These five scenarios are the unchecked operator realization tests from the UX-23 summary.
> Tip: you can watch `%LOCALAPPDATA%\MerchSys\ui-settings.json` change between runs.

### Test 2: Resize / move → close → relaunch

**What to do:**
1. Launch the app. Resize the window to a clearly non-default size and drag it to a distinctive
   position (e.g. lower-right quadrant).
2. Close the app normally.
3. Relaunch.

**What you should see:**
- The window reopens at the **same size and position** you left it.

- [ ] Window restores last size + position after relaunch —

### Test 3: Maximize → close → relaunch

**What to do:**
1. Maximize the window. Close the app. Relaunch.

**What you should see:**
- The window reopens **maximized** (not restored to a floating size).

- [ ] Maximized state is restored after relaunch —

### Test 4: Off-screen position → fallback

**What to do:**
1. With the app closed, open `%LOCALAPPDATA%\MerchSys\ui-settings.json` in a text editor.
2. Edit the saved window position to coordinates that are off any current monitor (e.g. `Left` /
   `Top` set to a large value like `99999`). Save the file. (This simulates unplugging a second
   monitor the window was on.)
3. Launch the app.

**What you should see:**
- The window appears **on-screen** (clamped back onto a visible monitor) — it is **not** lost off the
  edge where you can't reach it.

- [ ] Off-screen saved position falls back to a visible location —

### Test 5: Delete ui-settings.json → first-run defaults

**What to do:**
1. Close the app. Delete `%LOCALAPPDATA%\MerchSys\ui-settings.json`.
2. Launch the app.

**What you should see:**
- The app launches cleanly at its **default** size/position (no crash, no error about a missing file).
- A fresh `ui-settings.json` is recreated after you move/resize and close again.

- [ ] Missing settings file → clean first-run defaults, no crash —

### Test 6: Theme toggle after window-move → both keys survive

**What to do:**
1. Launch, move/resize the window, then toggle the theme (Light↔Dark).
2. Close and relaunch.

**What you should see:**
- **Both** the window placement **and** the selected theme are restored together — toggling the theme
  did not wipe the window-state key (and vice versa). They share `ui-settings.json` without clobbering
  each other.

- [ ] Window placement and theme both persist together —

---

## UX-07 — Iconography

### Test 7: Swept icon-only controls render in both themes

**What to do:**
1. Launch in **Light** theme. Walk through every view that has icon-only buttons (toolbars, list
   action buttons, dashboard refresh chips, the activity rail).
2. Switch to **Dark** theme and repeat.

**What you should see:**
- Every icon renders as a **crisp vector glyph** — no empty squares, no missing-resource boxes, no
  clipped paths.
- Icons recolor with the theme (they use the foreground token, not a baked-in color).

> Note: the two-state password-reveal icon (`IconEyeOffGeometry`) is now consumed and the dead
> `IconChevronLeftGeometry` was removed (both by **UX-34**) — verify the reveal toggle in **Test 15**.

- [ ] All swept icon-only controls render correctly in Light —
- [ ] All swept icon-only controls render correctly in Dark —

---

## UX-15 — Error State Panel (DB down)

### Test 8: ErrorStatePanel appears when the database is unreachable

**What to do:**
1. On the **host** laptop, open the XAMPP Control Panel and **Stop** MySQL (or, on a client laptop,
   disconnect from the LAN / Tailscale so MariaDB can't be reached).
2. Launch the app (or, if already running, navigate to a data view and trigger a refresh).
3. Open a data-loading view (e.g. Stock Dashboard, Product Management, Transaction History).

**What you should see:**
- The view shows the **ErrorStatePanel** — a clear "couldn't load / connection problem" message with a
  **Retry** affordance — **not** a blank screen, an infinite spinner, or an unhandled-exception crash.
4. Restart MySQL (or reconnect), then click **Retry**.
- The view recovers and loads data without needing an app restart.

- [ ] Data view shows ErrorStatePanel (not blank/spinner/crash) when DB is down —
- [ ] Retry recovers the view after DB is back —

---

## UX-25 — Motion & Micro-interactions (Reduced Motion)

### Test 9: Reduced-motion is honored at startup

**What to do:**
1. Close the app. In Windows, turn **off** animations: Settings → Accessibility → Visual effects →
   **Animation effects = Off** (this drives `SystemParameters.ClientAreaAnimation`).
2. Launch the app and navigate between several views; open the command palette and a dialog.

**What you should see:**
- View transitions and micro-interactions are **disabled or minimal** (no slide/fade easing) — the app
  honors the OS "reduce motion" preference chosen before launch.

- [ ] Animations are suppressed when OS reduced-motion was on at launch —

### Test 10: Mid-session reduced-motion toggle (now partially live — UX-34)

**What to do:**
1. With the app **running** (and animations currently on), turn OS animation effects **off** without
   restarting the app. Navigate between views; open a data view that fetches (to trigger a skeleton).

**What you should see / record:**
- **UX-34 added a live `SystemParameters.StaticPropertyChanged` listener**, so the change is now
  **partially live**: runtime-read consumers react immediately — the `MotionEnabled` gate and the
  skeleton shimmer stop animating without a restart.
- **Sealed template/trigger animations** (view-transition fades, `VisualTransition` durations) are baked
  when their template is sealed, so they **keep their startup durations until the app restarts**. This
  split is the expected behavior — record which interactions stopped live and which needed a restart.

- [ ] Skeleton shimmer / `MotionEnabled` gate stop animating immediately on the mid-session OS change —
- [ ] Sealed template transitions keep animating until restart (expected) —

---

## UX-31 / UX-32 — Touched-View Boot Check (post-implementation regression pass)

> **Why this section exists.** UX-31 (FilterSummaryBar / FreshnessChip / SkeletonPanel adoption sweep)
> and UX-32 (empty-state CTAs + keyboard nav) each touched ~14 views. Their implementation summaries
> claimed *"verified all views in both themes,"* but `ExpiryMonitorView` crashed on navigation with a
> `XamlParseException` (a `TextBlock` style applied to an `<AccessText>` element). That bug is **fixed**,
> and a static sweep of all 664 `Style="{StaticResource …}"` applications found no siblings — but the
> *runtime* claim for the other touched views is now unverified by assertion. **This pass re-checks each
> touched view by actually booting it.** A `Style`/`TargetType` mismatch is a **runtime** error that
> sails through the 0/0 build gate, so "it compiled" is not evidence the view opens.
>
> Do every view in **both Light and Dark**. The first column is the must-pass: *does the view open at
> all without an exception dialog?*

### Test 11: Every UX-31/UX-32 touched view opens without a Designer code/parse crash

**What to do:** Log in as `manager`, then navigate to each view below in **Light** theme, then toggle to
**Dark** and revisit. For each, confirm it **opens cleanly** (no exception dialog, no blank red error
surface), then eyeball the per-view items in the last column.

| View | Opens (Light) | Opens (Dark) | Also confirm |
|------|:---:|:---:|------|
| **ExpiryMonitorView** ⚠️ *(was the crash)* | [ ] | [ ] | Opens at all; "Near-expiry threshold:" label reads correctly + its `_t` Alt key works; filter bar count + chips; freshness chip stamps |
| StockDashboardView | [ ] | [ ] | Empty-state panel (UX-32) shows when no rows |
| TransactionHistoryView | [ ] | [ ] | FilterSummaryBar count + removable chips + clear-all; skeleton on first load only |
| APLedgerView | [ ] | [ ] | FilterSummaryBar; FreshnessChip stamps on load + updates on Refresh; skeleton on first load |
| VendorDirectoryView | [ ] | [ ] | FilterSummaryBar; FreshnessChip; skeleton on first load |
| ShrinkageView | [ ] | [ ] | FilterSummaryBar + FreshnessChip + skeleton; empty-state CTA "Record shrinkage" shows for Manager |
| TamperAuditReportView | [ ] | [ ] | FilterSummaryBar present; report keeps spinner (skeleton intentionally excluded) |
| CreditManagementView | [ ] | [ ] | FilterSummaryBar + FreshnessChip + skeleton |
| ReorderSuggestionsView | [ ] | [ ] | FilterSummaryBar + FreshnessChip + skeleton; empty-state CTA "Generate suggestions" shows for Manager |
| GoodsReceivingView | [ ] | [ ] | FreshnessChip + skeleton; empty-state info text when nothing awaiting receipt |
| DailySummaryView | [ ] | [ ] | FreshnessChip + skeleton; Alt keys (`_Daily`/`_Weekly`/`_Monthly`/`_Refresh`); Enter reloads |
| VendorCatalogView | [ ] | [ ] | FreshnessChip + skeleton; Alt keys; Enter-in-search adds, Esc closes search modal |
| SalesSummaryView | [ ] | [ ] | SkeletonPanel renders as **Cards** (not Rows) on first load |
| VatSettingsView | [ ] | [ ] | Alt keys (`_Reload`/`_Save`); Enter saves the form |

**What you should see overall:**
- Not one view throws an exception dialog on open or on theme toggle.
- Every component **recolors** with the theme (no stale light-on-light / baked color).
- Skeleton appears on **first** load only — navigating away and back to already-loaded content shows the
  data, not the skeleton.
- FilterSummaryBar count is honest (`{shown} of {total}` matches the grid); removing a chip or clear-all
  actually changes the list; the filter state **survives navigate-away-and-return** (session memory).

### Test 12: Owner role keeps read-only on the swept views

**What to do:** Log out, log back in as the **Owner** account, and open the views above that have CTAs
(ShrinkageView, ReorderSuggestionsView).

**What you should see:**
- The Manager-only empty-state CTAs (**"Record shrinkage"**, **"Generate suggestions"**) are **hidden**
  for Owner — Owner sees the informational empty state only.
- Refresh / clear-all (reads) still work for Owner; no write affordance is exposed.

- [ ] Owner sees no write CTAs on the gated views; refresh/clear-all still available —

---

# Post-report plans (UX-33 → UX-43) — appended 2026-06-08

> The sections above cover the original `ux_review_report` debt (UX-07 … UX-32). The sections below
> cover the manual verification owed by the plans that landed **after** the report. All code is
> implemented; each plan's summary marked operator verification "pending." Same rules: log in as
> `manager` unless a test says otherwise, and do each check in **both Light and Dark**.

## UX-33 — Destructive-Action Guardrail Parity

### Test 13: Typed-confirmation on irreversible actions

**What to do:**
1. **Void a sale:** POS → Transaction History → select a transaction → **Void Transaction**. Try
   clicking confirm without typing; then type `VOID` exactly.
2. **Record shrinkage:** Inventory → Shrinkage → record a shrinkage; in the dialog type `RECORD`.

**What you should see:**
- The confirm button stays **disabled** until the typed text matches exactly (`VOID` / `RECORD`);
  wrong or partial text keeps it disabled. Esc cancels with no change.
- After confirming, the action applies (the transaction shows voided; stock is reduced).

- [ ] Void requires typing `VOID`; confirm disabled until exact match —
- [ ] Shrinkage record requires typing `RECORD`; confirm disabled until exact match —

### Test 14: Confirm + Undo on reversible actions

**What to do:**
1. **Product deactivate:** Inventory → Product Management → deactivate a product → watch for the Undo
   toast → click **Undo** within the window.
2. **Credit block:** POS → Credit Management → select an account → **Block Credit** (confirm) → Undo
   toast → Undo. (Unblock requires the account balance = 0.)
3. **PO draft delete:** Purchasing → Purchase Orders → delete a *draft* PO (confirm) → Undo toast → Undo.

**What you should see:**
- Each shows a confirmation first (block / PO) or an immediate Undo toast (deactivate); clicking
  **Undo** within the time window restores the prior state (product re-activated, account unblocked,
  draft restored). Letting the toast expire keeps the change.

- [ ] Product deactivate → Undo restores active state —
- [ ] Credit block → confirm, then Undo restores unblocked —
- [ ] PO draft delete → confirm, then Undo restores the draft —

---

## UX-34 — Finishing Touches

### Test 15: Two-state password reveal

**What to do:** On the login screen, click the eye button in a password field; click it again. Repeat
on the change-password flow's new-password field.

**What you should see:** The icon toggles between the eye and eye-off glyph in sync with whether the
password text is shown or hidden.

- [ ] Reveal eye icon toggles eye ↔ eye-off in sync with visibility (both fields) —

### Test 16: Shortcuts overlay — Confirmation Dialogs group

**What to do:** Open the shortcuts overlay (the shortcuts hotkey / `?`). Scroll to the **Confirmation
Dialogs** group.

**What you should see:** A "Confirmation Dialogs" section explaining Enter (confirm), Esc (cancel), and
the type-to-confirm exact-match behavior (matches Test 13).

- [ ] Shortcuts overlay shows a "Confirmation Dialogs" group with Enter / Esc / type-match —

> Reduced-motion mid-session behavior (also UX-34) is verified in **Test 10** above.

---

## UX-36 — Accessibility (WCAG 2.2 AA)

### Test 17: Screen-reader naming

**What to do:** Start **Narrator** (Ctrl+Win+Enter). Tab through: the activity-rail module icons, the
login password-reveal button, the freshness Refresh chip, the SalesCart payment/remove buttons, the AP
ledger rows, and the VAT Payable KPI tile. Turn Narrator off when done.

**What you should see:** Narrator announces a **meaningful name** for each icon-only / row control — not
just "button," not the raw glyph.

- [ ] Narrator announces meaningful names on swept icon-only controls & rows —

### Test 18: Modal focus traps

**What to do:** Open each modal and press **Tab / Shift+Tab** repeatedly: the Command Palette (Ctrl+K),
a Confirmation Dialog (e.g. start a void), and the Concurrency Conflict prompt (if reproducible).

**What you should see:** Focus **cycles within** the modal — Tab never lands on a control behind the
overlay. Esc still cancels. On close, focus returns to the control that opened it.

- [ ] Tab cannot escape any of the 3 modals; focus restores to the invoker on close —

---

## UX-37 — Performance & Perceived Performance

### Test 19: Virtualized smooth scrolling

**What to do:** Open a long grid (Transaction History or AP Ledger with many rows) and scroll fast from
top to bottom.

**What you should see:** Scrolling is smooth — no per-row realization stutter, no blank-then-pop rows.

- [ ] Long grids scroll smoothly (no virtualization stutter) —

### Test 20: Optimistic VAT save + concurrency rollback

**What to do:**
1. Accounting/POS → VAT Settings → change a value → **Save**: status shows "Saving…" immediately, then
   "VAT settings saved."
2. **Conflict path (2 clients):** open VAT Settings on two clients; save on client A, then save a
   different value on client B → on B, choose **Refresh** at the conflict prompt.

**What you should see:** On B, the form **reverts to its pre-save values**, the conflict prompt appears
("Data changed elsewhere…"), and Refresh loads the authoritative DB values — no silent overwrite.
Choosing Cancel at the conflict leaves "Not saved — data changed elsewhere" (status is not stuck on
"Saving…").

- [ ] Optimistic "Saving…" appears instantly; success reconciles to "saved" —
- [ ] Concurrency conflict → form rolls back, prompt shown, Refresh loads DB values —

---

## UX-38 — Personalization & Workspace Memory

### Test 21: Restore last view

**What to do:** Navigate to a non-default screen (e.g. AP Ledger). Close the app. Relaunch and log in.

**What you should see:** After login the app reopens on the **last-viewed screen** (AP Ledger), not the
default dashboard. If that screen is role-excluded or removed, it falls back to the default cleanly.

- [ ] App restores the last-viewed screen after relaunch —
- [ ] Stale / role-excluded last-view falls back to default without error —

### Test 22: Favorites pin persistence

**What to do:** Pin a couple of screens via the sidebar ☆ button. Open the command palette (empty
query) → check the **Favorites** group. Log out and back in.

**What you should see:** ★ state persists across logout/restart; favorites appear in the palette
zero-state Favorites group and navigate correctly.

- [ ] Pinned favorites persist across logout/restart and show in the palette —

### Test 23: Recents

**What to do:** Navigate through several screens. Open the command palette (empty query) → **Recent**
group.

**What you should see:** Recently-viewed screens, most-recent-first, de-duplicated, bounded (~10),
excluding the screen you're currently on; survives restart.

- [ ] Recent list is correct, bounded, de-duplicated, and survives restart —

---

## UX-41 — Advanced Data Visualization

### Test 24: Sparkline hover tooltips

**What to do:** Hover the sparkline bars on the Owner Dashboard **Revenue Trend** card, and on a
Financial Overview sparkline.

**What you should see:** Revenue Trend bars show a "MMM d" period label **above** the value; the
unlabeled Financial Overview sparkline shows the value only (the label row collapses).

- [ ] Revenue Trend tooltip shows date label + value; plain sparkline shows value only —

### Test 25: KPI drill-down

**What to do:** On the Owner Dashboard, click **View →** on each of the four secondary KPI cards
(Purchasing, Inventory, Sales, Accounting). Test as both Owner and Manager.

**What you should see:** Each navigates to the correct detail view (PurchasingDashboard / StockDashboard
/ SalesSummary / FinancialOverview) — works in both roles.

- [ ] Each "View →" navigates to the correct detail view (Owner + Manager) —

### Test 26: Period selector

**What to do:** On the Revenue Trend card, switch 7d / 30d / 90d.

**What you should see:** The sparkline re-renders for the chosen window with daily date labels and a
zero-filled (gap-free) x-axis; recolors correctly in both themes.

- [ ] 7 / 30 / 90-day toggle re-renders the trend correctly in both themes —

---

## UX-42 — Print & Export

### Test 27: BIR Official-Receipt print

**What to do:** Complete a POS sale → in the receipt preview click **Print Official Receipt** → use the
print dialog (a real printer, or "Microsoft Print to PDF").

**What you should see:** The OR shows business name / address / TIN, VAT status, OR number, date,
itemized lines, totals, and the VAT disclosure block (VATable / VAT-Exempt / Zero-Rated / Output VAT).

- [ ] OR prints with all BIR-required fields and the correct VAT block —

### Test 28: Income Statement CSV / PDF export

**What to do:** Accounting → Income Statement → **Export CSV** (open in Excel/LibreOffice) and **Export
PDF** (preview window → Print / Save As).

**What you should see:** CSV opens with clean columns — a product name containing a `"` (e.g. `2" PVC
pipe`) does not break alignment; figures match the on-screen values. The PDF/preview text matches the
report.

- [ ] Income Statement CSV opens cleanly; values match the screen —
- [ ] Export PDF preview shows the correct report; Print / Save As work —

### Test 29: VAT Relief Report preview

**What to do:** Accounting → VAT Relief Report → **Export PDF** (preview) and **Export CSV**. Also try
clicking export immediately on open, before data loads.

**What you should see:** The preview shows the current-month three-bucket sales/purchases summary plus
the trailing 12-month trend; clicking export before load is null-guarded (no crash).

- [ ] VAT Relief preview / CSV show correct data; no crash when exported pre-load —

---

## UX-43 — Living Design-System Gallery (Developer only)

### Test 30: Gallery renders and recolors

**What to do:** Log in with the **Developer** account (the gallery is hidden from Manager/Owner). Open
Developer Tools → **Design Gallery**. Toggle the theme (Ctrl+T).

**What you should see:** Section A (tokens: color swatches, type ramp, spacing, radii, motion), Section
B (shared components in their states), Section C (all 20 icons). Every swatch and component **recolors
live** on the theme toggle — any swatch that fails to recolor flags a token-key mismatch.

- [ ] Gallery is visible only to the Developer role; all three sections render —
- [ ] Every swatch / component recolors live on the Light ↔ Dark toggle —

---

## Not an operator test (noted for completeness)

- **Verification Debt #6 — automated unit tests for UX Presenter logic** (filter session memory, undo
  time-boxing, freshness stamping, confirmation gating). This is **automated testing**, deferred to the
  dedicated testing phase per CLAUDE.md — it is not a manual operator task and is intentionally out of
  scope for this checklist.

