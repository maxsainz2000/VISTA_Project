# Integration Specification

## Feature: INT-01

### Overview
Implemented the App Composition Root (INT-01): wired all module services, Presenters, and MediatR into the `Application.Designer code.vb` DI container, and created the `WinFormsLowStockNotifier` concrete class.

### Requirements
- **Application.Designer code.vb**: replaced the placeholder TODO comments with full DI registrations for all modules; added calls to `AddModuleDbContexts()`, `AddMediatRServices()`, and `AddPurchasingServices()` (extension method); inlined Inventory, POS, and Accounting registrations
- **Services/WinFormsLowStockNotifier.vb**: concrete `ILowStockNotifier` implementation backed by `Notification.WinForms`'s `NotificationManager`

## Feature: INT-02

### Overview
Implemented Shell Navigation & View Wiring (INT-02): created the `NavigationItem`/`NavigationGroup` models, `MainWindowPresenter` (MVP navigation hub), rewired `MainWindow.Designer code` with a grouped sidebar, and updated `Application.Designer code.vb` to register all 16 Views, the 3 missing Purchasing Presenters, and the shell components.

### Requirements
- **Models/NavigationItem.vb**: `NavigationItem` (observable `IsActive` for active highlighting) and `NavigationGroup` (group name + items list)
- **Presenters/MainWindowPresenter.vb**: holds `NavigationGroups` (4 module groups, 16 items total; a 17th entry for `VatReturnView` was added in ACC-11/INT-13 via `BuildAccountingNavItems()` gated on `UserRole.Manager`), `CurrentView` (bound to content area), and `NavigateCommand` (resolves view from `IServiceProvider`, toggles `IsActive`); `NavigateToDefault()` opens `StockDashboardView` on launch
- **MainWindow.Designer code**: full sidebar with dark theme (#2C3E50), app title, grouped nav items via nested `ItemsControl`, `NavItemButton` style with active (#3D566E) and hover (#34495E) states, `ContentControl` bound to `CurrentView`
- **MainWindow.Designer code.vb**: constructor injection of `MainWindowPresenter`; `MainWindow_Loaded` calls `NavigateToDefault()`
- **Application.Designer code.vb**: registered all 16 Views as Transient; registered 3 previously-missing Purchasing Presenters (`PurchaseOrderListPresenter`, `GoodsReceivingPresenter`, `VendorListPresenter`) as Transient; registered `MainWindowPresenter` and `MainWindow` as Singleton; `Application_Startup` resolves `ILowStockNotifier` on the UI thread (Notification.WinForms initialisation) then shows `MainWindow` from DI

## Feature: INT-03

### Overview
Implemented INT-03: Cross-Module Contracts & Handlers. Wired all outstanding cross-module MediatR connections deferred during individual module builds. Created new SharedKernel query contracts, implemented handlers in Inventory, POS, and Purchasing, and wired low-stock alerts and receipt generation into the relevant service flows.

### Requirements
### New SharedKernel Query Contracts
- **Queries/GetProductCostQuery.vb**: `IRequest(Of GetProductCostResult)` with `ProductId`; sent by Accounting to get FIFO cost from Inventory
- **Queries/GetProductCostResult.vb**: response DTO: `ProductId`, `FifoUnitCost`
- **Queries/GetTotalARQuery.vb**: `IRequest(Of Decimal)`; sent by Accounting, handled by POS
- **Queries/GetTotalAPQuery.vb**: `IRequest(Of Decimal)`; sent by Accounting, handled by Purchasing
- **Queries/GetLowStockAlertCountQuery.vb**: `IRequest(Of Integer)`; sent by Accounting, handled by Inventory

## Feature: INT-04

### Overview
Generated manual EF Core migrations for all four module DbContexts (Purchasing, Inventory, POS, Accounting), created model snapshots, added `IDesignTimeDbContextFactory` implementations for CLI discoverability, and implemented a `DatabaseInitializer` workaround for EF Core 10's inability to discover VB.NET migration classes at runtime.

### Requirements
- **Data/PurchasingDbContextFactory.vb**: `IDesignTimeDbContextFactory(Of PurchasingDbContext)` for EF CLI design-time support
- **Data/InventoryDbContextFactory.vb**: same pattern for Inventory
- **Data/POSDbContextFactory.vb**: same pattern for POS
- **Data/AccountingDbContextFactory.vb**: same pattern for Accounting
- **Migrations/20260507100001_InitialPurchasing.vb**: manual migration for 9 Purchasing tables + 3 vendor seed rows
- **Migrations/PurchasingDbContextModelSnapshot.vb**: model snapshot for future migration diffs
- **Migrations/20260507100002_InitialInventory.vb**: manual migration for 5 Inventory tables + 4 categories + 20 product seed rows
- **Migrations/InventoryDbContextModelSnapshot.vb**: model snapshot
- **Migrations/20260507100003_InitialPOS.vb**: manual migration for 6 POS tables + 3 credit account seed rows
- **Migrations/POSDbContextModelSnapshot.vb**: model snapshot
- **Migrations/20260507100004_InitialAccounting.vb**: manual migration for 4 Accounting tables (no seed data)
- **Migrations/AccountingDbContextModelSnapshot.vb**: model snapshot
- **Data/DatabaseInitializer.vb**: ADO.NET initializer that applies all 4 migrations idempotently on app startup (workaround for EF CLI VB.NET bug)
- **Application.Designer code.vb**: calls `DatabaseInitializer.Initialize()` before the main window is shown
- Added `Microsoft.EntityFrameworkCore.Design` v10.0.7 to `MerchSys.App.vbproj` (required for EF CLI startup project)
- Installed `dotnet-ef` 10.0.7 global tool

## Feature: INT-05

### Overview
Implemented INT-05: Phase 2 Enhancements. Four non-blocking improvements from the module audits: StockMovement log entity for time-windowed velocity, ISessionService for user context in credit payments, HasReturns query fix (Approach B), and stale checkbox cleanup in three progress summaries.

### Requirements
### 1. StockMovement Log Entity (INV-08 Enhancement)
- **Entities/MovementType.vb**: `MovementType` enum: Sale, Receipt, Shrinkage, `[Return]` (bracketed because `Return` is a VB.NET keyword)
- **Entities/StockMovement.vb**: inherits `AuditableEntity`; properties: `ProductId`, `MovementType`, `Quantity`, `OccurredAt`, nav property `Product`
- **Data/Configurations/StockMovementConfiguration.vb**: table `Inv_StockMovements`, `MovementType` stored as string, composite index on `(ProductId, OccurredAt)`
- **Data/InventoryDbContext.vb**: added `DbSet(Of StockMovement)`
- **Migrations/20260509100003_AddStockMovement.vb**: raw SQL migration creating `Inv_StockMovements` table and index
- **Migrations/InventoryDbContextModelSnapshot.vb**: added `StockMovement` entity and FK navigation
- **Data/DatabaseInitializer.vb**: added `ApplyIfPending` call for `20260509100003_AddStockMovement` and the `ApplyStockMovement` method
- **Services/VelocityService.vb**: when `StockMovement` records of type `Sale` exist within the analysis window, uses their sum as `totalUnitsSold`; falls back to lifetime batch-total approximation otherwise. No interface change required.

## Feature: INT-06

### Overview
Project-wide QA pass covering application launch smoke test, database schema validation, cross-module event flow verification, and known issue verification. All checks performed via code review, build output, and `dotnet run` process monitoring. Full interactive UI navigation could not be automated (no GUI test harness), so navigation verification was confirmed via static code review of the DI registry and navigation model.

---

## Feature: INT-07

### Overview
Registered 9 of the 10 missing service interfaces in `Application.Designer code.vb`. All registrations use Scoped lifetime to match their module DbContext lifetimes. `IInventoryAuditService` was not registered because the interface and implementation do not exist in the codebase (see Issues section).

### Requirements
- Modified `Application.Designer code.vb`:
- Added `Imports MerchSys.Accounting.Services` (was missing, required for new Accounting registrations)
- **POS:** Added `ICartService/CartService`, `IPaymentService/PaymentService`, `ICreditService/CreditService`, `ISalesReturnService/SalesReturnService` — all Scoped
- **Inventory:** Added `IStockService/StockService` — Scoped
- **Accounting:** Added `IFinancialOverviewService/FinancialOverviewService`, `IIncomeStatementService/IncomeStatementService`, `ISalesSummaryService/SalesSummaryService`, `IWhatThisMeansService/WhatThisMeansService` — all Scoped

## Feature: INT-08

### Overview
Implemented `StockMovement` record writes across all stock-mutating operations in the Inventory module. The `Inv_StockMovements` table (created in INT-05) was schema-only prior to this plan. After INT-08, every stock addition, deduction, shrinkage event, and return now logs a timestamped movement record within the same `SaveChangesAsync` transaction as the underlying stock change.

### Requirements
- **MerchSys.Inventory/Services/IStockService.vb**: added optional `movementType As MovementType = MovementType.Receipt` parameter to `AddStockBatchAsync` interface signature
- **MerchSys.Inventory/Services/StockService.vb**: `AddStockBatchAsync` now adds a `StockMovement` (positive quantity, configurable type, defaults to `Receipt`) in the same transaction as the batch; `DeductStockFIFOAsync` now adds a `StockMovement` (`Sale`, negative quantity = total deducted) before `SaveChangesAsync`
- **MerchSys.Inventory/Services/ShrinkageService.vb**: `RecordShrinkageAsync` now adds a `StockMovement` (`Shrinkage`, negative quantity = total shrinkage across all affected batches) before `SaveChangesAsync`
- **MerchSys.Inventory/Handlers/StockReturnedEventHandler.vb**: added `Imports MerchSys.Inventory.Entities`; passes `movementType:=MovementType.[Return]` when calling `AddStockBatchAsync`, so customer returns are logged as `Return` rather than `Receipt`

## Feature: INT-09

### Overview
Implemented `IInventoryAuditService` — the only remaining unregistered service dependency in the application. The interface and implementation were absent from the codebase (not merely unregistered); this plan created both from scratch following the INV-03/INV-07 service patterns.

### Requirements
- **MerchSys.Inventory/Entities/StockAuditRecord.vb**: new entity inheriting `AuditableEntity`; records expected qty, physical count, variance, reason, notes, performedBy, and auditedAt for each audit event
- **MerchSys.Inventory/Data/Configurations/StockAuditRecordConfiguration.vb**: EF fluent configuration mapping to `Inv_StockAuditRecords` table with appropriate constraints
- **MerchSys.Inventory/Data/InventoryDbContext.vb**: added `StockAuditRecords As DbSet(Of StockAuditRecord)` property
- **MerchSys.Inventory/Entities/MovementType.vb**: added `Adjustment = 5` enum value (required for audit variance stock movements)
- **MerchSys.Inventory/Services/IInventoryAuditService.vb**: interface with 4 methods: `PerformStockCountAsync`, `RecordAdjustmentAsync`, `GetAuditHistoryAsync`, `GetLatestAuditPerProductAsync`
- **MerchSys.Inventory/Services/InventoryAuditService.vb**: implementation injecting `InventoryDbContext` and `ILogger`; writes `StockAuditRecord` and a `StockMovement` (type Adjustment) for non-zero variances
- **MerchSys.App/Application.Designer code.vb**: registered `services.AddScoped(Of IInventoryAuditService, InventoryAuditService)()` in the Inventory block

## Feature: INT-10

### Overview
Executed the INT-10 runtime verification session for all four deliverables: DI/navigation smoke test, cross-module event chain verification, StockMovement record verification, and VelocityService classification verification.

### Requirements
### Pre-flight Fix: `Inv_StockAuditRecords` Migration
- **MerchSys.App/Data/DatabaseInitializer.vb**: added `ApplyIfPending(conn, "20260509100004_AddStockAuditRecords", AddressOf ApplyStockAuditRecords)` call and corresponding `ApplyStockAuditRecords` method. The INT-09 session added the entity and EF configuration but did not add the migration to `DatabaseInitializer`. The app would throw at runtime if `InventoryAuditService` were called and the table was absent.
- Applied the migration manually via Python (ADO.NET) for the currently-running DB instance since `DatabaseInitializer.Initialize` runs at startup and the DB was already initialized without the new step.

## Feature: INT-11

### Overview
Implemented `MediatREventBus` — the missing `IEventBus` adapter — and registered it in the DI container. This resolves the `InvalidOperationException` preventing 3 POS views (`SalesCartView`, `CreditManagementView`, `TransactionHistoryView`) from navigating at runtime.

### Requirements
- **MerchSys.App/Services/MediatREventBus.vb**: thin adapter class implementing `IEventBus`; injects `IMediator` and delegates `PublishAsync` to `IMediator.Publish`. Follows the existing `DefaultSessionService` pattern in the same directory.
- **MerchSys.App/Application.Designer code.vb**: added `services.AddScoped(Of IEventBus, MediatREventBus)()` in the Infrastructure section, after `ISessionService` registration.
- Modified `MerchSys.App/Views/POS/TransactionHistoryView.Designer code` (line 601) — removed invalid `Style="{StaticResource FieldLabel}"` from a `<Run>` element. The `FieldLabel` style has `TargetType="TextBlock"` which cannot be applied to `Run` (an inline element). Moved `FontSize="11"` and `Foreground="#7F8C8D"` directly onto the parent `<TextBlock>`. This was a Designer code parse-time crash separate from the DI issue.

## Feature: INT-12

### Overview
Implemented INT-12: Runtime Event Chain Verification. Delivered a deterministic verification
harness for the two unverified end-to-end event chains (GoodsReceived and SaleCompleted), a
Markdown report writer, and the operator checklist.

### Requirements
- **Debug/EventChainVerificationHarness.vb**: 
orchestrates both chains against a fresh scratch SQLite DB; contains `ChainProbe`,
`GoodsReceivedProbeHandler`, `SaleCompletedProbeHandler`, `HarnessLowStockNotifier`,
`EventChainVerificationHarness`, and `ChainVerificationResult`
- **Debug/EventChainReport.vb**: 
StringBuilder-driven Markdown emitter; writes to `%TEMP%\event-chain-report-<timestamp>.md`
and surfaces a `Notification.WinForms` toast with the path on completion
- **Progress/VISTA_Modules/Integration/INT-12-checklist.md**: operator checklist
for `TransactionHistoryView` re-test and both harness chains; includes INT-10 flip instruction

## Feature: INT-13

### Overview
Implemented INT-13: VatReturnView Navigation Wire-up. Upon inspection, ACC-11 had already wired the navigation entry, DI registrations, and role gate into the shell during its own implementation — the INT-13 deliverables were substantively present before this session started. This session confirmed all acceptance criteria, updated the inline comment to satisfy the plan's documentation requirement, updated the INT-02 progress summary to record the 17th view, and produced this summary.

### Requirements
- **Presenters/MainWindowPresenter.vb**: updated inline comment on the VatReturn nav entry to cite INT-02 (originating navigation convention) and ACC-11 (view source), per plan documentation requirement
- **Progress/VISTA_Modules/Integration/INT-02-summary.md**: added one-sentence note that VatReturnView is the 17th view, added via ACC-11/INT-13 `BuildAccountingNavItems()` gated on `UserRole.Manager`
No changes to `MainWindow.Designer code` were needed — the sidebar is data-driven from `NavigationGroups` and VatReturnView appears automatically when the Manager role is active.

## Feature: INT-14

### Overview
Produced the ToListAsync remediation triage checklist for INT-15 and INT-16. Applied the corrected INFRA-18 Rule 3 detector against all 63 sites from the 2026-05-24 baseline, classified each site as true or false positive, and compiled a 47-row checklist with method names, entity types, Include complexity, UI surface descriptions, and batch assignments.

### Requirements
- Read all flagged service and Presenter files from the 2026-05-24 baseline (63 sites across 18 files)
- Applied the INFRA-18 corrected Rule 3 detector (shape-aware: GroupBy gate, Select-projection gate, scalar-projection gate)
- Cleared 16 false positives; confirmed 47 true positives
- Traced UI callers for each method (Presenter → View surface described per row)
- Assigned each row to INT-15 (Inventory + POS) or INT-16 (Purchasing + Accounting)
- Classified fix complexity: `simple` (no Include), `joined` (one Include), `graph` (multiple Includes or nested ThenInclude)
- Documented the already-applied Vendor fix in `PurchaseOrderListPresenter.vb` as the reference fix shape
- Created `Operator/debug-logs/tolistasync-remediation-checklist.md`
- Created this progress summary

## Feature: INT-15

### Overview
Applied the raw `SqliteConnection` + synchronous `reader.Read()` fix to all 27 Inventory and POS methods flagged by the INFRA-18 corrected Rule 3 detector for the EF Core 10 + VB.NET silent-empty-list bug. Every `ToListAsync()` on a full-entity or navigation-include chain was replaced with a fresh `SqliteConnection`, parameterized raw SQL, and a class-field result list.

### Requirements
### MerchSys.Inventory — 14 fixes across 6 files
- **MerchSys.Inventory/Services/StockService.vb**: Rows 9, 12, 13: fixed `GetCurrentStockAsync` (joined: Products + StockBatches), `DeductStockFIFOAsync` (simple: StockBatches), `GetStockBatchesAsync` (simple: StockBatches); added `Friend Shared ReadStockBatch` and `Friend Shared ReadProduct` helpers reused by all other Inventory services; added class fields `_batchesForFIFO`, `_batchesForProduct`, `_productStockList`
- **MerchSys.Inventory/Services/LowStockAlertService.vb**: Row 7: fixed `BuildAlertsAsync` (simple: StockAlertConfigs); added class field `_alertConfigList`
- **MerchSys.Inventory/Services/ShrinkageService.vb**: Rows 8, 11: fixed `GetShrinkageHistoryAsync` (graph: ShrinkageRecords + Products + StockBatches), `RecordShrinkageAsync` FIFO path (simple: StockBatches); added class fields `_shrinkageHistoryList`, `_batchesForShrinkage`
- **MerchSys.Inventory/Services/InventoryAuditService.vb**: Row 10: fixed `GetAuditHistoryAsync` (joined: StockAuditRecords + Products, dynamic optional filters); added class field `_auditHistoryList`
- **MerchSys.Inventory/Services/ExpiryTrackingService.vb**: Rows 5, 6: fixed `GetNearExpiryBatchesAsync` and `GetExpiredBatchesAsync` (both joined: StockBatches with INNER JOIN Inv_Products filter + separate Product load for nav); added class fields `_nearExpiryBatchList`, `_expiredBatchList`
- **MerchSys.Inventory/Services/VelocityService.vb**: Row 14: fixed `ClassifyAllProductsAsync` (graph: Products + StockBatches + ShrinkageRecords + ProductCategories); added class field `_velocityProductList`
- **MerchSys.Inventory/Services/StockDashboardService.vb**: Row 1: fixed `GetDashboardDataAsync` (graph: Products + StockBatches + ProductCategories); added class field `_dashboardProductList`
- **MerchSys.Inventory/Presenters/ProductManagementPresenter.vb**: Rows 2, 3: fixed `LoadDataAsync` (both Product+Category joined and ProductCategory simple queries in one method); added class fields `_loadedProducts`, `_loadedCategories`; added `Imports MerchSys.Inventory.Services` to reuse `StockService.ReadProduct`
- **MerchSys.Inventory/Handlers/GetProductCatalogQueryHandler.vb**: Row 4: fixed `Handle` (joined: Products + StockBatches, dynamic optional ProductId and SearchTerm filters via LIKE); added class field `_catalogProductList`

## Feature: INT-16

### Overview
Remediated the EF Core 10 + VB.NET `ToListAsync()` silent-empty-list defect across the Purchasing and Accounting modules (rows 28–47 of the triage checklist). This is the Batch 2 counterpart of INT-15, which fixed the same defect in Inventory and POS. All 20 affected methods across 8 service files were replaced with raw `SqliteConnection` + synchronous `reader.Read()` loops writing to class fields.

### Requirements
### Checklist rows flipped to `Fix applied = yes`
All 20 rows (28–47) in `Operator/debug-logs/tolistasync-remediation-checklist.md` were updated from `Fix applied = no` to `Fix applied = yes`. Verification column uses `screen` for methods confirmed by code review and `sqlite check` for methods confirmed by direct schema inspection against migration files.
| Row | Module | File | Method |
|-----|--------|------|--------|
| 28 | Purchasing | PurchaseOrderService | GetAllAsync |
| 29 | Purchasing | VendorService | GetAllAsync |
| 30 | Purchasing | AccountsPayableService | GetAllOutstandingAsync |
| 31 | Purchasing | AccountsPayableService | GetAllAsync |
| 32 | Purchasing | ReorderService | GetPendingSuggestionsAsync |
| 33 | Purchasing | ReorderService | GetAllConfigsAsync |
| 34 | Purchasing | VendorService | SearchAsync |
| 35 | Purchasing | AccountsPayableService | GetByVendorAsync |
| 36 | Purchasing | AccountsPayableService | GetOverdueAsync |
| 37 | Purchasing | GoodsReceivingService | GetReceiptsForPOAsync |
| 38 | Purchasing | PriceChangeService | GetUnacknowledgedAsync |
| 39 | Purchasing | PriceChangeService | GetHistoryForProductAsync |
| 40 | Purchasing | ReorderService | GetAllSuggestionsAsync |
| 41 | Purchasing | AccountsPayableService | CreateFromPurchaseOrderAsync |
| 42 | Purchasing | VendorService | GetVendorWithPurchaseHistoryAsync |
| 43 | Purchasing | ReorderService | GenerateSuggestionsAsync |
| 44 | Accounting | VatReportingService | ListReturnsAsync |
| 45 | Accounting | VatReportingService | CollectLedgerDataAsync (RevenueRecords) |
| 46 | Accounting | VatReportingService | CollectLedgerDataAsync (ExpenseRecords) |
| 47 | Accounting | ITamperAuditQueryService | GetIncidentsAsync |

## Feature: INT-17

### Overview
Mechanical rename of 12 constructor/method parameters that shadow instance properties
(Rule 14 true positives). All confirmed sites were already mitigated with `Me.` qualifiers,
so this eliminates the latent footgun without changing any behaviour.

### Requirements
- Modified `MerchSys.Accounting/Exceptions/VatReturnLockedException.vb:26`
— renamed `returnId`→`lockedReturnId`, `year`→`lockedYear`, `period`→`lockedPeriod`,
`formType`→`lockedFormType` in constructor signature and body (including `MyBase.New` message).
- Modified `MerchSys.App/Models/NavigationItem.vb:28` (in `NavigationGroup.New`)
— renamed `groupName`→`name`, `items`→`navigationItems`.
- Modified `MerchSys.App/Views/LoginView.Designer code.vb:16`
— renamed `Presenter`→`vm` in constructor signature, `_viewModel = vm`, `DataContext = vm`.
- Modified `MerchSys.Inventory/Services/StockService.vb:21` (in `InsufficientStockException.New`)
— renamed `productId`→`product`, `requestedQty`→`requested`, `availableQty`→`available`.
- Modified `MerchSys.POS/Debug/ReceiptArchivalHarness.vb:750` (in `EmptyConfigurationSection.New`)
— renamed `key`→`sectionKey`.
- Modified `MerchSys.POS/Debug/ReceiptArchivalHarness.vb:782` (in `EmptyConfigurationSection.GetSection`)
— renamed `key`→`sectionKey`.

## Feature: INT-19

### Overview
Replaced string-formatted date representations bound to ADO.NET SQL command parameters with native `System.DateTime` values across the database-access services of the monolith. This resolves the potential issue where MariaDB rejects or mismatches the ISO-8601 formatting (with `T`, `Z`, and nanoseconds) produced by `DateTime.ToString("o")`.

### Requirements
Modified 12 primary service implementation files to bind native `System.DateTime` values instead of formatted strings:
1. **`MerchSys.Accounting/Services/VatReportingService.vb`**
- Converted `@ws` and `@we` parameters in `CollectLedgerDataAsync` to bind native `DateTime` values (`windowStart` and `windowEnd`) directly. Removed the unused `wsStr` and `weStr` string locals.
2. **`MerchSys.Accounting/Services/VatReliefReportService.vb`**
- Converted `@ws` and `@we` parameters in `BuildSummaryAsync` to bind native `DateTime` values (`windowStart` and `windowEnd`) directly. Removed the unused `wsStr` and `weStr` string locals.
3. **`MerchSys.Accounting/Services/ITamperAuditQueryService.vb`**
- Converted `@fromUtc` and `@toUtc` parameters in `GetIncidentsAsync` to bind native `DateTime` values (`fromUtc` and `toUtc`).
4. **`MerchSys.Inventory/Services/ExpiryTrackingService.vb`**
- Converted `@today` and `@threshold` parameters in `GetNearExpiryBatchesAsync` and `@today` in `GetExpiredBatchesAsync` to bind native `DateTime` values (`today` and `thresholdDate`).
5. **`MerchSys.Inventory/Services/ShrinkageService.vb`**
- Converted `@fromUtc`, `@toUtc`, and `@cursorDate` parameters in `GetShrinkageHistoryPageAsync` to bind native `DateTime` values (`request.FromUtc.Value`, `request.ToUtc.Value`, and `request.CursorDate.Value`).
6. **`MerchSys.Inventory/Services/InventoryAuditService.vb`**
- Converted `@startDate` and `@endDate` parameters in `GetAuditHistoryAsync` to bind native `DateTime` values (`startDate.Value` and `endDate.Value`).
7. **`MerchSys.Inventory/Services/VelocityService.vb`**
- Converted `@windowStart` parameter in `GetVelocityHistoryAsync` (or the equivalent velocity analysis query) to bind `windowStart` directly without string formatting.
8. **`MerchSys.POS/Services/CartService.vb`**
- Converted `@startDate` and `@endDate` in `GetTransactionHistoryAsync`, and `@fromUtc`, `@toUtc`, and `@cursorDate` in `GetTransactionHistoryPageAsync` to bind native `DateTime` values.
9. **`MerchSys.POS/Services/DailySummaryService.vb`**
- Converted `@start` and `@end` parameters in `BuildDailySummaryAsync` (for both sales and returns queries) and in `BuildPeriodSummaryAsync` (for both sales and returns queries) to bind native `DateTime` values.
10. **`MerchSys.POS/Services/CreditService.vb`**
- Converted `@cutoff` parameter in `GetOverdueAccountsAsync` to bind the native `DateTime` value `cutoff` directly.
11. **`MerchSys.POS/Services/SalesReturnService.vb`**
- Converted `@startDate` and `@endOfDay` parameters in `GetReturnHistoryAsync` to bind native `DateTime` values.
12. **`MerchSys.Purchasing/Services/AccountsPayableService.vb`**
- Converted `@today` parameter in `GetOverdueAsync` to bind the native `DateTime` value `DateTime.UtcNow.Date` directly.

## Feature: INT-20

### Overview
This task implements the plan **INT-20: Service field→local cleanup + small correctness fixes** to eliminate class-level query buffer fields, resolve thread safety/synchronization issues, correctly dispose resource objects, and clean up outdated SQLite reference comments.

### Requirements
Implemented all parts of the INT-20 plan across all target files:

## Feature: INT-21

### Overview
This batch implements verified functional fixes to address specific defects across POS, Accounting, and App modules, ensuring they behave correctly on MariaDB as the primary database. All fixes were successfully applied, and the project compiles with zero warnings or errors.

### Requirements
### POS-1 — `strftime` on MariaDB
- Modified [ReceiptIntegrityService.vb](file:///C:/Users/Admin/Documents/VISTA_Project/Services/ReceiptIntegrityService.vb#L111-L115) to replace the SQLite-specific `CAST(strftime('%Y', r.IssueDate) AS INTEGER)` with MariaDB's `YEAR(r.IssueDate)` inside the raw SQL query.
- Kept the raw ADO.NET query to prevent introducing empty-result bugs in EF Core `ToListAsync()` under VB.NET.

## Feature: INT-22

### Overview
Implemented concurrency-safe document sequence generation and session-aware audit trail attribution.

### Requirements
Concise list of changes made:
- **Data/Migrations/Central/0007_sequence_tables.sql**: Idempotent DDL definition for new `Pos_TransactionSequences` and `Pur_OrderSequences` tables.
- **Entities/TransactionSequence.vb**: Entity mapping for `Pos_TransactionSequences`.
- **Data/Configurations/TransactionSequenceConfiguration.vb**: Entity configuration specifying `Year` as primary key and enabling optimistic concurrency tracking on `RowVersion` mapped to `TIMESTAMP(6)`.
- **Data/POSDbContext.vb**: Added `TransactionSequences` `DbSet` and threaded `ISessionService` through the constructor.
- **Entities/OrderSequence.vb**: Entity mapping for `Pur_OrderSequences`.
- **Data/Configurations/OrderSequenceConfiguration.vb**: Entity configuration specifying `SeqKey` as primary key and enabling optimistic concurrency tracking on `RowVersion` mapped to `TIMESTAMP(6)`.
- **Data/PurchasingDbContext.vb**: Added `OrderSequences` `DbSet` and threaded `ISessionService` through the constructor.
- Repurposed `Helpers/SequentialNumberGenerator.vb` — Updated helper to perform database-backed sequence generation (`GetNextNumberAsync`) inside a serializable transaction with retry on concurrency conflict.
- **Services/CartService.vb**: Replaced `CountAsync` based transaction number generation helper with centralized sequence lookup on `TransactionSequences` within serializable transaction boundaries. Injected logger and imported `Microsoft.Extensions.Logging`.
- **Services/PurchaseOrderService.vb**: Swapped in-memory list derivation for database sequence-backed PO number generation.
- **Services/ReorderService.vb**: Swapped in-memory list derivation for database sequence-backed PO number generation.
- **Services/GoodsReceivingService.vb**: Swapped in-memory list derivation for database sequence-backed GR receipt number generation.
- **Data/BaseDbContext.vb**: Injected `ISessionService` and modified `SaveChangesAsync` to stamp audit fields (`CreatedBy`, `ModifiedBy`, `DeletedBy`) dynamically based on current user session information.
- **Data/AccountingDbContext.vb**: Threaded `ISessionService` constructor parameter through to base class constructor.
- **Data/InventoryDbContext.vb**: Threaded `ISessionService` constructor parameter through to base class constructor.
- Modified all four design-time context factories (`AccountingDbContextFactory.vb`, `InventoryDbContextFactory.vb`, `POSDbContextFactory.vb`, `PurchasingDbContextFactory.vb`) to pass `Nothing` as the `ISessionService` constructor argument.

## Feature: INT-23

### Overview
Implemented MVP hygiene improvements for data-access layering and timer disposal across multiple modules. All direct `MySqlConnection` usages inside Presenters have been removed, delegating to services (with raw ADO.NET for full entities and EF projections for aggregate metrics). Refresh timers in auto-refresh Presenters are now cleanly stopped and disposed when their corresponding Views are unloaded.

### Requirements
### Part 1 — Data-Access Layering
- **`IStockService.vb` + `StockService.vb`** — Added `GetProductsWithCategoriesAsync()` returning `ProductAndCategoryData` using raw ADO.NET for full entities.
- **`ProductManagementPresenter.vb`** — Injected `IStockService`, removed raw connection/reader code in `LoadDataAsync`, and replaced it with a call to `_stockService.GetProductsWithCategoriesAsync()`. Removed `MySqlConnector` import.
- **`ICreditService.vb` + `CreditService.vb`** — Added `GetCreditTransactionsAsync(accountId)` returning `List(Of CreditTransactionItem)` using raw ADO.NET. Moved `CreditTransactionItem` class to `ICreditService.vb` to make it accessible to the service layer.
- **`CreditManagementPresenter.vb`** — Removed local `CreditTransactionItem` class definition. Removed raw connection/reader code in `LoadHistoryInternalAsync` and replaced it with a call to `_creditService.GetCreditTransactionsAsync(account.Id)`. Removed `MySqlConnector` import.
- **`IPurchasingDashboardService.vb` + `PurchasingDashboardService.vb`** (New) — Created interface and implementation with `GetMonthlyTrendAsync(trendStart)` and `GetTopVendorsAsync()` returning DTOs using EF projections.
- **`PurchasingDashboardPresenter.vb`** — Injected `IPurchasingDashboardService`, removed local `TrendBarItem` and `TopVendorItem` definitions, and delegated trend and vendor queries to the dashboard service. Removed `MySqlConnector`, EF Core, and database context imports.
- **`PurchasingServiceCollectionExtensions.vb`** — Registered `IPurchasingDashboardService` as scoped in DI.
- **`PurchaseOrderListPresenter.vb`** — Cleaned up raw vendor database query and replaced it with `_vendorService.GetAllAsync()`. Removed unused DbContext and `MySqlConnector` imports.
- **`IDailySummaryService.vb` + `DailySummaryService.vb`** (Optional Task) — Added `GetDailySalesTrendAsync(startDate)` returning `Dictionary(Of DateTime, Decimal)` using raw ADO.NET.
- **`OwnerDashboardPresenter.vb`** (Optional Task) — Injected `IDailySummaryService`, removed `MySqlConnector`, `IConfiguration` constructor injection, and the raw SQL connection block in `LoadTrendDataAsync`. Replaced it with a call to `_dailySummary.GetDailySalesTrendAsync(startDate)`.

## Feature: INT-24

### Overview
Implemented defensive security and robustness hardening for the SharedKernel including SaveChanges synchronous parity, fail-closed default role check, typed user account self-service check, RowVersion unconfigured entities logging convention, PageRequest PageSize clamp, and IAuditable implementation in UserAccount.

### Requirements
Concise list of changes made:
- **Modified** [BaseDbContext.vb](file:///C:/Users/Admin/Documents/VISTA_Project/Data/BaseDbContext.vb)
- Extracted the audit and soft-delete logic from `SaveChangesAsync` into a private helper `ApplyAuditAndSoftDelete()`.
- Overrode the synchronous `SaveChanges()` to invoke `ApplyAuditAndSoftDelete()` prior to persisting.
- Added `UnconfiguredRowVersionEntities` shared list to accumulate entities that inherit from `ConcurrencyAwareEntity` but lack concurrency token configurations.
- Modified the model finalizing convention `IgnoreNonTokenRowVersionConvention` to log a startup warning (via `Debug.WriteLine` and `Console.WriteLine`) listing unconfigured entity names and accumulate them in the shared collection.
- **Modified** [RoleGuardInterceptor.vb](file:///C:/Users/Admin/Documents/VISTA_Project/Data/RoleGuardInterceptor.vb)
- Converted the role checking logic to a fail-closed default-deny structure for all write operations, excluding Manager, Developer, and System context.
- Replaced the reflection-based self-service password check with a compile-time check using type casting (`DirectCast` on `UserAccount`).
- **Modified** [PageRequest.vb](file:///C:/Users/Admin/Documents/VISTA_Project/Paging/PageRequest.vb)
- Refactored `PageSize` to use a backing field (`_pageSize`) with a setter that clamps values to the range `[1, 1000]`, defaulting to `100` if the value is non-positive.
- **Modified** [UserAccount.vb](file:///C:/Users/Admin/Documents/VISTA_Project/Entities/UserAccount.vb)
- Declared implementation of the `IAuditable` interface.
- Added the missing properties `CreatedBy` and `ModifiedBy` and mapped all auditing fields using the `Implements IAuditable.<Property>` syntax.



## Verification (from INT-12-checklist.md)

---
plan-id: INT-12
generated: 2026-05-17
last-synced: 2026-05-26
---

# Runtime Event Chain Verification — Checklist

> This checklist is for INT-12 specifically. The main Integration checklist is in [INT-verification-checklist.md](INT-verification-checklist.md).
>
> **How to use:** Run the harness first, then fill in what you saw for each section.
>
> **Login required (INFRA-15):** The app now shows a login screen on launch.
> Unless a test specifically says to log in as Owner, log in as `manager`.
> Default password: `Vista2026!` (first login will prompt you to change it).

### Key file locations

| What | Path |
|------|------|
| TransactionHistoryView | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\POS\TransactionHistoryView.Designer code.vb` |
| TransactionHistoryPresenter | `WinForms_Applications\MerchSys\src\MerchSys.POS\Presenters\TransactionHistoryPresenter.vb` |
| INT-10 summary (for checkbox flip) | `Progress\VISTA_Modules\Integration\INT-10-summary.md` |
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |

---

## How to run the harness

1. Open the solution in Visual Studio.
2. Build in **Debug** configuration (Ctrl+Shift+B).
3. Press **F5** to launch the app.
4. Find the debug tool that runs `EventChainVerificationHarness`. (Look in the Developer menu or a debug tools area.)
5. Click the button to run the harness.
6. The harness runs both event chains against a temporary SQLite file in your `%TEMP%` folder (usually `C:\Users\<you>\AppData\Local\Temp\`).
7. When it finishes, a toast notification appears with the path to a Markdown report.
8. Open that report and use it to fill in the sections below.

---

## TransactionHistoryView re-test

**What to do:**
1. While the app is still running, navigate to the **Transaction History** view (in the POS section of the sidebar).
2. Check that it opens and looks correct.

> **View file:** `WinForms_Applications\MerchSys\src\MerchSys.App\Views\POS\TransactionHistoryView.Designer code.vb`

**What you should see:**
- The view opens without crashing.
- All columns display data.
- No unstyled text or broken layout.

- [x] View navigates without exception — PASS ✅
- [x] All columns render correctly — PASS ✅
- [x] Operator: manager
- [x] Result attached: yes

---

## GoodsReceived chain (from harness)

**What you should see in the harness report:**
- The `GoodsReceivedEvent` was published (the probe handler set a flag).
- The `GoodsReceivedHandler` ran and called `StockService.AddStockBatchAsync`.
- A row exists in `Inv_StockMovements` with `Type = Receipt`.

To verify manually in the database, open `%LOCALAPPDATA%\MerchSys\merchsys.db` and run:
```sql
SELECT * FROM Inv_StockMovements WHERE Type = 'Receipt' ORDER BY Id DESC LIMIT 5;
```

- [x] Publisher fired — `GoodsReceivedEvent` flag was set by probe handler — PASS ✅
- [x] Handler executed — `GoodsReceivedHandler` ran and called `AddStockBatchAsync` — PASS ✅
- [x] StockMovement row present with `Type=Receipt` in `Inv_StockMovements` — PASS ✅ (Receipt row Id=1, Qty=+10)

---

## SaleCompleted chain (from harness)

**What you should see in the harness report:**
- The `SaleCompletedEvent` was published (the probe handler set a flag).
- The `SaleCompletedHandler` ran and called `StockService.DeductStockFIFOAsync`.
- A row exists in `Inv_StockMovements` with `Type = Sale`.

To verify manually in the database, open `%LOCALAPPDATA%\MerchSys\merchsys.db` and run:
```sql
SELECT * FROM Inv_StockMovements WHERE Type = 'Sale' ORDER BY Id DESC LIMIT 5;
```

- [x] Publisher fired — `SaleCompletedEvent` flag was set by probe handler — PASS ✅
- [x] Handler executed — `SaleCompletedHandler` ran and called `DeductStockFIFOAsync` — PASS ✅
- [x] StockMovement row present with `Type=Sale` in `Inv_StockMovements` — PASS ✅ (Sale row Id=2, Qty=-3)

---

## INT-10 checkbox flip

**What to do:**
1. Only do this after **all three sections above pass**.
2. Open this file:
   `Progress\VISTA_Modules\Integration\INT-10-summary.md`
3. Find the interactive-navigation line that currently shows `[/]` (partially done).
4. Change it to `[x]` (fully done).
5. Save the file.

- [x] All sections above passed → INT-10 summary checkbox changed from `[/]` to `[x]` — PASS ✅


## Verification (from INT-verification-checklist.md)

---
module: Integration
source: Integration-audit-2026-06-01.md
originally-generated: 2026-05-17
last-synced: 2026-06-01
---

# Operator Verification Checklist — Integration

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 17 Integration plans are completed. These are the remaining acceptance tests.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
> INT-12 items are also tracked in the dedicated [INT-12-checklist.md](INT-12-checklist.md) file.
>
> **Login required (INFRA-15):** The app now shows a login screen on launch.
> Unless a test specifically says to log in as Owner, log in as `manager`.
> Default password: `Vista2026!` (first login will prompt you to change it).

### Key file locations

| What | Path |
|------|------|
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| TransactionHistoryView (Designer code) | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\POS\TransactionHistoryView.Designer code.vb` |
| TransactionHistoryPresenter | `WinForms_Applications\MerchSys\src\MerchSys.POS\Presenters\TransactionHistoryPresenter.vb` |
| FinancialOverviewView (code-behind) | `WinForms_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.Designer code.vb` |
| FinancialOverviewVatExtension | `WinForms_Applications\MerchSys\src\MerchSys.Accounting\Presenters\Extensions\FinancialOverviewVatExtension.vb` |
| MainWindowPresenter (navigation) | `WinForms_Applications\MerchSys\src\MerchSys.App\Presenters\MainWindowPresenter.vb` |
| INT-10 summary (for checkbox flip) | `Progress\VISTA_Modules\Integration\INT-10-summary.md` |
| EF Core bug notes | `LLM_Wiki\agent_wiki\errors\efcore10-vbnet-migration-discovery-bug.md` |

---

## INT-10 — Runtime Verification & Smoke Testing

### Test 1: GoodsReceived event chain (live test)

**What to do:**
1. Launch the app (F5).
2. Go to the **Purchasing** section in the sidebar.
3. Create a new Purchase Order (or use an existing one that is approved but not yet received).
4. Go to the **Goods Receiving** screen.
5. Receive the goods for that Purchase Order (mark items as received and save).
6. Open DB Browser for SQLite.
7. Open the database at `%LOCALAPPDATA%\MerchSys\merchsys.db`.
8. Run this query:
   ```sql
   SELECT * FROM Inv_StockMovements WHERE Type = 'Receipt' ORDER BY Id DESC LIMIT 5;
   ```

**What you should see:**
- A new row appears in `Inv_StockMovements` with `Type = 'Receipt'`.
- The row's product and quantity match what you just received.
- This proves the full event chain works: Goods Receiving → `GoodsReceivedEvent` → Handler → Stock Service → Database.

- [x] GoodsReceived chain: receiving goods creates a StockMovement row with Type=Receipt

---

### Test 2: SaleCompleted event chain (live test)

**What to do:**
1. Launch the app (F5).
2. Go to the **POS** section (Sales Cart) in the sidebar.
3. Add one or more products to the cart.
4. Complete the sale (process payment and finish).
5. Open DB Browser for SQLite.
6. Open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
7. Run this query:
   ```sql
   SELECT * FROM Inv_StockMovements WHERE Type = 'Sale' ORDER BY Id DESC LIMIT 5;
   ```

**What you should see:**
- A new row appears in `Inv_StockMovements` with `Type = 'Sale'`.
- The row's product and quantity match what you just sold.
- This proves the full event chain works: Sale → `SaleCompletedEvent` → Handler → Stock Service → Database.

- [x] SaleCompleted chain: completing a sale creates a StockMovement row with Type=Sale

---

### ⏳ Ongoing: EF Core VB.NET CLI bug

**What to do:**
- Nothing. This is a known bug in EF Core's VB.NET migration discovery.
- Tracked in: `LLM_Wiki\agent_wiki\errors\efcore10-vbnet-migration-discovery-bug.md`
- No action needed until Microsoft releases a fix.

- [ ] ⏳ Ongoing — monitoring for upstream EF Core fix

---

## INT-11 — IEventBus DI Registration Gap

### Test 3: TransactionHistoryView works correctly

**What to do:**
1. Launch the app (F5).
2. Navigate to the **Transaction History** view (in the POS section of the sidebar).
3. Look at the view carefully. Check that:
   - The view opens without crashing.
   - All columns show data correctly.
   - The text labels are styled properly (no raw `Run` elements or broken Designer code).

> **View file:** `WinForms_Applications\MerchSys\src\MerchSys.App\Views\POS\TransactionHistoryView.Designer code.vb`
> **Presenter:** `WinForms_Applications\MerchSys\src\MerchSys.POS\Presenters\TransactionHistoryPresenter.vb`

**What you should see:**
- The view loads and displays transaction history data.
- No visual glitches or unstyled text elements.
- No errors in the Visual Studio Output window.

- [x] TransactionHistoryView displays correctly after Designer code fix

---

### Test 4: All 16 navigation views pass (then update docs)

**What to do:**
1. After completing Test 3 above, check if ALL 16 views in the app navigate and display correctly.
2. The list of 16 views is in:
   `Progress\VISTA_Modules\Integration\INT-10-summary.md`
3. If all 16 views pass:
   - Open `Progress\VISTA_Modules\Integration\INT-10-summary.md`
   - Find the interactive-navigation checkbox item that currently shows `[/]`.
   - Change it to `[x]`.

**What you should see:**
- Every view in the sidebar opens without errors and shows data.

- [x] All 16 views pass → INT-10 summary checkbox updated to `[x]`

> **Note (INFRA-16):** `OwnerDashboardView` was added as view #17, but it is Owner-only and not visible in the Manager sidebar. The Manager-role 16-view count tested here remains valid.

---

## INT-12 — Runtime Event Chain Verification

> These items overlap with the [INT-12-checklist.md](INT-12-checklist.md). You can use either file to track your results.

### Test 5: Run the event chain verification harness

**What to do:**
1. Build the solution in **Debug** configuration (Ctrl+Shift+B).
2. Press **F5** to launch the app.
3. In the sidebar, navigate to **Developer Tools → Run VAT Schema Harness** (the view). Scroll down to the **Event Chain Verification** section and click **Run Event Chain Harness**.
4. Click the button to run the harness.
5. Wait for it to finish.

**What you should see:**
- A toast notification appears with a file path to a Markdown report.
- Open the report.
- Both chains (GoodsReceived and SaleCompleted) show `Passed = True`.

- [x] Event chain harness reports Passed = True for both chains — GoodsReceived ✅ (Receipt row Id=1, Qty=+10) and SaleCompleted ✅ (Sale row Id=2, Qty=-3) — 2026-05-20

---

### Test 6: TransactionHistoryView re-test (from INT-12 checklist)

**What to do:**
- Same as Test 3 above. If you already did Test 3, you can mark this as done.

- [x] TransactionHistoryView re-test (same as Test 3)

---

### Test 7: Flip INT-10 checkbox (from INT-12 checklist)

**What to do:**
- Same as Test 4 above. If all views passed and you updated the doc, mark this as done.
- File to edit: `Progress\VISTA_Modules\Integration\INT-10-summary.md`

- [x] INT-10 checkbox flipped (same as Test 4)

---

## INT-13 — VatReturnView Navigation Wire-up

### Test 8: Verify type-based navigation pattern

**What to do:**
1. Open this file in Visual Studio:
   `WinForms_Applications\MerchSys\src\MerchSys.App\Views\Accounting\FinancialOverviewView.Designer code.vb`
2. Find the `NavigateToVatReturnRequested` event handler method (use Ctrl+F to search).
3. Read the code inside.

**What you should see:**
- The handler resolves a `NavigationItem` **by type** (like `GetNavigationItem(Of VatReturnView)()` or similar).
- It does **NOT** use a string like `NavigateCommand("VatReturn")`.
- This confirms ACC-14 followed the correct pattern required by INT-13.

- [x] VatReturnView navigation uses type-based resolution, not string key

---

## INT-17 — Rename Parameters That Shadow Properties (Rule 14)

### Test 9: Re-run Rule 14 detector on affected files

**What to do:**
1. Open a terminal in the project root.
2. Run the INFRA-18 Rule 14 detector script against the 5 files that were renamed in INT-17:
   - `MerchSys.POS\Services\ReceiptIntegrityService.vb`
   - `MerchSys.POS\Services\VatAwareReceiptService.vb`
   - `MerchSys.Purchasing\Services\GoodsReceivingService.vb`
   - `MerchSys.Purchasing\Services\PurchaseOrderService.vb`
   - `MerchSys.Inventory\Services\StockService.vb`
3. Read the output.

**What you should see:**
- Zero Rule 14 hits across all 5 files.
- All parameter-shadows-property patterns were resolved by the INT-17 renames.

> **Source:** INT-17 What's Next. If new Rule 14 true positives surface in code merged after 2026-05-24, open INT-17b.

- [x] Rule 14 detector shows 0 hits on the 5 INT-17 affected files — verified 2026-05-26


### Future / Backlog Item

## 14. INT-17b — Contingent Rule 14 Follow-Up

**Status:** Checked clean 2026-05-27 — no new violations in code merged 2026-05-24 → 2026-05-27 (INFRA-19, INFRA-20, INFRA-21, ACC-19, ACC-20, POS-19). Checked clean again 2026-05-28 — INFRA-23 through INFRA-30 added no new Rule 14 violations. Remains contingent — re-check after the next merge window.
**Module:** Integration
**Source:** INT-17 What's Next
**Description:** If new Rule 14 true positives surface in code merged after 2026-05-24, open a follow-up plan INT-17b to rename them. This is a contingent item — only actionable if new violations appear.
**Why deferred:** No new violations have been detected. This item exists as a reminder to check after future code merges.
**Depends on:** INT-17 (completed).

---

