# Inventory Specification

## Feature: INV-01

### Overview
Implemented the five core domain entity classes for the Inventory module as specified in INV-01. These entities form the central data model for FIFO stock management, expiry tracking, shrinkage recording, and alert configuration.

### Requirements
- **Entities/ProductCategory.vb**: `SoftDeletableEntity` subclass; groups products into categories (Fertilizers, Pesticides, Seeds, Animal Feeds)
- **Entities/Product.vb**: `SoftDeletableEntity` subclass; catalog entry with SKU, retail price, unit, HasExpiry flag, and MinimumThreshold; `CurrentStock` and `TotalValue` are service-computed (not stored)
- **Entities/StockBatch.vb**: `AuditableEntity` subclass; FIFO core with QuantityReceived/Remaining, UnitCost, ReceiptDate, ExpiryDate; `IsExpired` and `IsFullyConsumed` are computed read-only properties; `SourcePurchaseOrderId` is a plain integer (no EF nav)
- **Entities/ShrinkageRecord.vb**: `AuditableEntity` subclass; captures financial impact of losses with Reason, QuantityLost, UnitCost, TotalValue
- **Entities/StockAlertConfig.vb**: `AuditableEntity` subclass; per-product alert thresholds with ExpiryAlertDays defaulting to 30

## Feature: INV-02

### Overview
Implemented the Inventory data access layer: `InventoryDbContext` DbSets, five EF Core `IEntityTypeConfiguration` classes, and seed data for 4 product categories and 20 sample products.

### Requirements
- **Data/InventoryDbContext.vb**: added `DbSet` properties for all five entity types and wired in `InventorySeedData.Seed()`
- **Data/Configurations/ProductCategoryConfiguration.vb**: `Inv_ProductCategories`, Name max 100 unique index, FK to Products (Restrict)
- **Data/Configurations/ProductConfiguration.vb**: `Inv_Products`, Name max 200, Sku max 50 unique index, RetailPrice precision(18,2), FK to StockBatches and ShrinkageRecords (Restrict)
- **Data/Configurations/StockBatchConfiguration.vb**: `Inv_StockBatches`, UnitCost precision(18,4), composite index on (ProductId, ReceiptDate) for FIFO ordering, ignores computed properties `IsExpired`/`IsFullyConsumed`
- **Data/Configurations/ShrinkageRecordConfiguration.vb**: `Inv_ShrinkageRecords`, UnitCost precision(18,4), TotalValue precision(18,2), Reason max 50 required, optional FK to StockBatch (SetNull)
- **Data/Configurations/StockAlertConfigConfiguration.vb**: `Inv_StockAlertConfigs`, FK to Product (Cascade)
- **Data/SeedData/InventorySeedData.vb**: 4 categories (Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds) and 20 products (5 per category) with fixed IDs for deterministic migrations

## Feature: INV-03

### Overview
Implemented the FIFO stock management service and MediatR handlers for the Inventory module. This is the core FIFO deduction engine that consumes `GoodsReceivedEvent` to create stock batches and `SaleCompletedEvent` to deduct stock oldest-first. Also wires up `GetCurrentStockQuery` for cross-module stock level reads.

### Requirements
- **Services/IStockService.vb**: interface with `FIFODeductionResult` and `StockLevelDto` DTOs in the same file
- **Services/StockService.vb**: full implementation; includes `InsufficientStockException`
- **Handlers/GoodsReceivedHandler.vb**: `INotificationHandler(Of GoodsReceivedEvent)`; creates one `StockBatch` per line item via `AddStockBatchAsync`
- **Handlers/SaleCompletedHandler.vb**: `INotificationHandler(Of SaleCompletedEvent)`; deducts via `DeductStockFIFOAsync` per line item; re-throws `InsufficientStockException` after logging
- **Handlers/GetCurrentStockHandler.vb**: `IRequestHandler(Of GetCurrentStockQuery, GetCurrentStockResult)`; maps `StockLevelDto` to `GetCurrentStockResult.StockLevel`

## Feature: INV-04

### Overview
Implemented batch-level expiry date tracking for perishable products (pesticides, seeds, animal feeds). Introduces `IExpiryTrackingService` and `ExpiryTrackingService` with near-expiry alerts, expired batch identification, product-level expiry status, and write-off to shrinkage.

### Requirements
- **Services/IExpiryTrackingService.vb**: interface `IExpiryTrackingService` plus `ExpiryAlertDto` and `ProductExpiryStatusDto`
- **Services/ExpiryTrackingService.vb**: concrete implementation

## Feature: INV-05

### Overview
Implemented the Stock Dashboard Service (INV-05): a read-only aggregation layer that provides the real-time stock dashboard with per-product summaries, FIFO valuation, low-stock/expiry counts, and product-level batch detail.

### Requirements
- **Services/IStockDashboardService.vb**: interface + all DTOs (`StockDashboardDto`, `ProductSummaryDto`, `ProductDetailDto`, `StockBatchSummaryDto`, `ShrinkageSummaryDto`, `StockMovementDto`)
- **Services/StockDashboardService.vb**: concrete implementation with two methods

## Feature: INV-06

### Overview
Implemented threshold-based low-stock alerts for the Inventory module. Products with current stock at or below their minimum threshold appear in the alert list, sorted by deficit (most critical first). A desktop toast notification fires when `CheckAndGenerateAlertsAsync` detects active alerts.

### Requirements
- **Services/ILowStockAlertService.vb**: defines `ILowStockAlertService` (3 methods), `ILowStockNotifier` abstraction, and `LowStockAlertDto`
- **Services/LowStockAlertService.vb**: full implementation of all three interface methods

## Feature: INV-07

### Overview
Implemented inventory shrinkage recording — damage, spoilage, expiry write-off, and administrative discrepancies. Each record captures the financial impact and publishes a `ShrinkageRecordedEvent` to Accounting via MediatR.

### Requirements
- **Services/IShrinkageService.vb**: defines `IShrinkageService` with `RecordShrinkageAsync`, `GetShrinkageHistoryAsync`, and `GetTotalShrinkageValueAsync`
- **Services/ShrinkageService.vb**: full implementation with batch-targeted or FIFO deduction, reason validation, MediatR event publish, and audit logging

## Feature: INV-08

### Overview
Implements product velocity classification for the Inventory module. Products are categorised as Fast, Moderate, Slow, or Dead based on average daily sales derived from StockBatch deduction history.

### Requirements
- **Services/IVelocityService.vb**: `IVelocityService` interface and `ProductVelocityDto`
- **Services/VelocityService.vb**: `VelocityService` implementation

## Feature: INV-09

### Overview
Implemented predictive stockout estimation for the Inventory module. Calculates days-until-stockout per product using average daily velocity, assigns risk levels, and exposes a critical-products query.

### Requirements
- **Services/IStockoutEstimationService.vb**: interface (`EstimateAllAsync`, `EstimateForProductAsync`, `GetCriticalProductsAsync`) and `StockoutEstimateDto` (ProductId, ProductName, CurrentStock, AvgDailySales, EstimatedDaysUntilStockout, RiskLevel, EstimatedStockoutDate)
- **Services/StockoutEstimationService.vb**: implementation delegating velocity calculation to `IVelocityService` with a 30-day default analysis window; maps `ProductVelocityDto` to `StockoutEstimateDto` with risk classification and stockout date projection

## Feature: INV-10

### Overview
Implemented the Stock Dashboard View — the primary Inventory screen. Provides real-time stock visibility across all products with FIFO-costed values, color-coded status, expiry alerts, and predictive stockout estimates. Driven by the existing `IStockDashboardService` (INV-05) and `IStockoutEstimationService` (INV-09).

### Requirements
- **Presenters/StockDashboardPresenter.vb**: ObservableObject Presenter combining `StockDashboardDto` and `StockoutEstimateDto` into `ProductRowItem` rows. Implements category/status/text filtering, product detail drill-down, and 60-second auto-refresh via `System.Timers.Timer` + captured `SynchronizationContext`.
- **Views/Inventory/StockDashboardView.Designer code**: WinForms UserControl with five summary cards, filter toolbar, color-coded DataGrid (DataTrigger row styles), color-coded status/expiry badge columns, and a bottom detail panel for batch list + recent movements.
- **Views/Inventory/StockDashboardView.Designer code.vb**: Code-behind with constructor injection, `SelectionChanged` handler that fires `SelectProductCommand`, and Escape key handler to clear the search box.

## Feature: INV-11

### Overview
Implemented the Product Management view — a manager-only screen for full product catalog CRUD and category management. Based on INV-11.

### Requirements
- **Presenters/ProductManagementPresenter.vb**: Presenter with product CRUD, category CRUD, filtering, and overlay dialog state. Injects `InventoryDbContext` directly. Exposes `ProductManagementRowItem` and `CategoryManagementItem` row types.
- **Views/Inventory/ProductManagementView.Designer code**: TabControl with Products tab (DataGrid + toolbar + overlay product editor) and Categories sub-tab (DataGrid + overlay category editor). Deactivate/Reactivate button toggles using `Style.Triggers` on `SelectedProductIsActive`.
- **Views/Inventory/ProductManagementView.Designer code.vb**: Code-behind; constructor-injected Presenter, Escape key clears search.

## Feature: INV-12

### Overview
Implemented the Expiry Monitor screen (INV-12) — a dedicated view for monitoring batch expiry dates, with near-expiry alerts, expired batch listing, and one-click write-off to shrinkage.

### Requirements
- **MerchSys.Inventory/Presenters/ExpiryMonitorPresenter.vb**: Presenter with two ObservableCollections (`NearExpiryBatches`, `ExpiredBatches`), three summary properties (`NearExpiryCount`, `ExpiredCount`, `TotalValueAtRisk`), a configurable `DaysThreshold` (default 30, clamped 1–365), `WriteOffCommand` (AsyncRelayCommand(Of ExpiryRowItem)), `RefreshCommand`, status feedback properties (`StatusMessage`, `IsStatusError`), and 60-second auto-refresh timer using the `classlib-Presenter-auto-refresh-timer` pattern.
- Created `MerchSys.Inventory/Presenters/ExpiryRowItem.vb` (nested in the Presenter file) — flat bindable row with `UrgencyLevel` ("Red" ≤7 days, "Orange" 8–14 days, "Yellow" 15–30 days) driving DataGrid row coloring.
- **MerchSys.App/Views/Inventory/ExpiryMonitorView.Designer code**: three summary cards (NearExpiryCount, ExpiredCount, TotalValueAtRisk), threshold spinner (TextBox + RepeatButtons), TabControl with Near-Expiry and Expired tabs; row styles keyed to `UrgencyLevel`; Expired tab includes a per-row "Write Off" button in a DataGridTemplateColumn.
- **MerchSys.App/Views/Inventory/ExpiryMonitorView.Designer code.vb**: constructor-injected Presenter, `WriteOffButton_Click` handler (shows MessageBox confirmation before invoking `WriteOffCommand`), threshold spinner handlers, and numeric-only input filter for the threshold TextBox.

## Feature: INV-13

### Overview
Implemented the Shrinkage View — a WinForms screen for recording inventory losses and viewing their history with financial impact. Depends on INV-07 (ShrinkageService, IStockService).

### Requirements
- **MerchSys.Inventory/Presenters/ShrinkagePresenter.vb**: Presenter with three helper classes (`ShrinkageRowItem`, `ShrinkageProductItem`, `ShrinkageBatchItem`) and the main `ShrinkagePresenter`
- **MerchSys.App/Views/Inventory/ShrinkageView.Designer code**: UserControl with summary cards, filter toolbar, history DataGrid, and MVP overlay dialog
- **MerchSys.App/Views/Inventory/ShrinkageView.Designer code.vb**: Code-behind with DI constructor injection, DatePicker sync, reason filter handler, dialog confirmation/validation, and quantity input guard

## Feature: INV-14

### Overview
Implemented the Product RetailPrice Change History feature — a secure, append-only database transaction history tracking changes to a product's retail price (who changed it, when it changed, previous/current prices, and the change reason), coupled with an interactive read-only popup viewer launched from the main Product Management panel.

### Requirements
- **MerchSys.Inventory/Entities/ProductPriceHistory.vb**: Append-only domain model with `ProductId`, `OldPrice`, `NewPrice`, `ChangedAt`, `ChangedBy`, and `Reason`.
- **MerchSys.Inventory/Data/Configurations/ProductPriceHistoryConfiguration.vb**: Maps to `Inv_ProductPriceHistory`, applies precision/scale controls, configures composite index, and restricts hard deletion.
- **MerchSys.Inventory/Data/InventoryDbContext.vb**: Registered new `DbSet(Of ProductPriceHistory)`.
- Created manual migration baseline `MerchSys.Inventory/Migrations/20260527100000_AddProductPriceHistory.vb`.
- **MerchSys.App/Data/DatabaseInitializer.vb**: Registered the baseline migration and wrote the idempotent ADO.NET SQL migration script to apply the schema (table and index) automatically on app startup.
- **MerchSys.Inventory/Presenters/ProductPriceHistoryPresenter.vb**: Popup viewer Presenter exposing list loading via explicit DTO projection (`PriceHistoryRowItem`) to circumvent the VB.NET full-entity query silent empty bug.
- **MerchSys.Inventory/Presenters/ProductManagementPresenter.vb**: Injected `ISessionService` to fetch user credentials, added change reason binding `EditorPriceChangeReason`, and transactionally wrote price histories inside `SaveProductAsync` if prices changed.
- **MerchSys.App/Application.Designer code.vb**: Registered the view model and popup window view in the startup DI container.
- **MerchSys.App/Views/Inventory/ProductManagementView.Designer code**: Wired the "Price History" button next to existing toolbar actions and added the "Price Change Reason (optional)" textbox in the product edit card overlay.
- **MerchSys.App/Views/Inventory/ProductManagementView.Designer code.vb**: Injected `IServiceProvider` and wrote the button click handler to dynamically instantiate, initialize, and display the history dialog.
- Created `MerchSys.App/Views/Inventory/ProductPriceHistoryView.Designer code` & `ProductPriceHistoryView.Designer code.vb` — WinForms Window popup showcasing a premium DataGrid ledger with colored price deltas (green for increase, red for decrease) and operating logs.

## Feature: INV-15

### Overview
Enhanced the Stock Dashboard product list grid by replacing the single "Price" column (which rendered the retail price) with three distinct columns: `Retail Price`, `Avg Cost`, and `FIFO Cost`. This provides store managers with direct visibility into the purchase cost spread and next-sale unit costs per SKU.

### Requirements
- **Modified:** `Services/IStockDashboardService.vb` — added properties `AverageUnitCost As Decimal` and `FifoOldestUnitCost As Decimal` to `ProductSummaryDto` class inside the file.
- **Modified:** `Services/StockDashboardService.vb` — calculated weighted average cost across remaining non-expired batches, retrieved FIFO oldest non-expired batch unit cost, and populated the DTO. Avoided variable shadowing inside lambdas.
- **Modified:** `Views/Inventory/StockDashboardView.Designer code` — replaced the single Price column with three columns incorporating bindings, currency formatting `₱{0:N2}`, right-alignment, and helpful tooltips for each header.



### Future / Backlog Item

## 10. ISyncableRepository Write-Path Migration — ProductManagementPresenter

**Status:** CLOSED (2026-05-27) — out-of-scope for single-branch deployment.
**Module:** Infrastructure / Inventory
**Source:** INFRA-13 What's Next
**Description:** Migrate `Inventory/Presenters/ProductManagementPresenter.vb` write paths to use `ISyncableRepository` so that product edits are captured in the `Sync_Journal`.
**Scope decision (2026-05-27):** Closed without migration. Rationale:
1. Villon Farm Supply is single-location (`LLM_Wiki/wiki/entities/villon-farm-supply.md`); there is no second branch needing the same catalog.
2. Product CRUD is a Manager-only function performed on a single terminal — no second writer to converge.
3. Owner is read-only and never edits products. Financial reports denormalize `ProductName` into `RevenueRecord` at write time (`SaleCompletedAccountingHandler.vb:50`), so report rendering does not depend on a replicated product master.
4. Product operations are UPDATE-heavy (price, `IsActive`, soft-delete), so syncing would introduce real multi-master conflict liability for no current use case.
5. YAGNI — if multi-branch is ever planned, it requires a far larger redesign than product-table sync; defer to that hypothetical plan rather than pre-paying the cost now.
**Depends on:** INFRA-13 (completed). Reopen only if a multi-branch deployment is approved.

---


### Future / Backlog Item

## 17. Price Change History Tracking

**Status:** PROMOTED (2026-05-27) — see `Plans/VISTA_Modules/Inventory/14-price-change-history.md` (INV-14). Scope narrowed to retail price only; vendor unit cost history remains deferred (implicit via `StockBatch.UnitCost` + `Pur_PriceChangeAlerts`).
**Module:** Inventory / Purchasing
**Source:** User Question (2026-05-27)
**Description:** Add a formal history log or tracking system for changes to `RetailPrice` and Vendor Unit Costs over time. Currently, `RetailPrice` is a simple mutable field on the `Product` entity, and `UnitCost` is locked into individual `StockBatch` records. 
**Why deferred:** The current system handles Cost of Goods using a FIFO deduction engine out-of-the-box (by looking at individual stock batches), meaning historical cost is inherently preserved per batch. Retail prices are updated ad-hoc without historical tracking. A dedicated price change history table/UI is a nice-to-have but not critical for MVP.

---


### Future / Backlog Item

## 18. Stock Valuation, COGS Accuracy, and Duplicate Revenue Record Bugs

**Module:** Inventory / Accounting / POS (cross-cutting)
**Source:** User Observation (2026-05-27) — hands-on testing with multi-vendor purchasing
**Status:** CLOSED — completed via INV-15, ACC-21, and ACC-22. Fully verified under Test 7 of the Manager checklist (split-batch POS checkout and FIFO COGS).
- **18a → INV-15** `Plans/VISTA_Modules/Inventory/15-stock-dashboard-cost-column.md` (UI cost visibility — completed)
- **18c → ACC-22** `Plans/VISTA_Modules/Accounting/22-revenue-record-consolidation.md` (eliminate duplicate-writer race — completed)
- **18b → ACC-21** `Plans/VISTA_Modules/Accounting/21-per-batch-cogs-accuracy.md` (per-batch FIFO COGS via new `Inv_SaleCogs` ledger — completed)

### Reproduction Steps

1. **Vendor Product Catalog tab:** Added "Ammonium Sulfate" (ProductId=3) to all three vendors with different prices:
   - AgriChem Supplies (VendorId=1): ₱1,100/unit
   - FarmFresh Seed Corp. (VendorId=2): ₱1,000/unit
   - Golden Feeds Trading (VendorId=3): ₱1,200/unit
2. **Purchase Orders tab:** Created 3 POs (PO-2026-0001 to PO-2026-0003), one per vendor, each ordering 10 units.
3. **Goods Receiving tab:** Confirmed all 3 POs → 3 `StockBatch` rows created (Batch 1 = ₱1,200, Batch 2 = ₱1,100, Batch 3 = ₱1,000). Total stock: 30 units, total cost: ₱33,000.
4. **Stock Dashboard tab:** Stock quantity was correct (30 units), but the "Price" column showed ₱1,100 — the `RetailPrice` from `Inv_Products` set in the Product Management tab, not the actual per-batch purchase cost.
5. **Sales Cart tab:** Sold all 30 units in a single transaction at ₱1,100/unit (the `RetailPrice`). Revenue = ₱33,000.
6. **Expected:** Some profit or at least break-even, since the average purchase cost was ₱1,100/unit (= ₱33,000/30).
7. **Actual:** Two `RevenueRecord` rows were created with conflicting COGS values, and one shows a ₱3,000 *loss*.

### Root Causes (verified via database inspection)

#### 18a. Stock Dashboard "Price" column shows `RetailPrice`, not actual purchase cost

The `StockDashboardService.GetDashboardDataAsync()` correctly computes `StockValue` using FIFO `UnitCost` from `Inv_StockBatches` (line 116: `b.QuantityRemaining * b.UnitCost`), but the `ProductSummaryDto.RetailPrice` field is sourced from `Inv_Products.RetailPrice` — the selling price set in Product Management, **not** the purchase cost. The dashboard's "Price" column therefore doesn't reflect what was actually paid to vendors.

**DB evidence:**
- `Inv_Products` (ProductId=3): `RetailPrice` = 1100
- `Inv_StockBatches`: Batch 1 `UnitCost` = 1200, Batch 2 `UnitCost` = 1100, Batch 3 `UnitCost` = 1000

**Impact:** User cannot tell the actual purchase cost per product from the Stock Dashboard. The `StockValue` sum card is correct (it uses batch `UnitCost`), but the per-row "Price" column is misleading because it shows the retail selling price, not the cost.

**Fix scope:** The dashboard could show a **weighted-average cost** column alongside `RetailPrice`, computed as `SUM(QuantityRemaining × UnitCost) / SUM(QuantityRemaining)` across non-expired batches. Alternatively, show the FIFO cost (oldest batch's `UnitCost`), matching what Accounting uses for COGS.

#### 18b. COGS uses only the FIFO-oldest batch's `UnitCost` for the *entire* sale quantity

`SaleCompletedAccountingHandler.Handle()` calls `GetProductCostQuery` to get the COGS unit cost. The handler (`GetProductCostQueryHandler`) returns the `UnitCost` of the **single** oldest non-expired batch with remaining stock, then multiplies it by the full `item.Quantity`:

```
costResult.FifoUnitCost * item.Quantity   ← line 45, SaleCompletedAccountingHandler.vb
```

This is incorrect when a sale quantity spans multiple FIFO batches at different costs. The `SaleCompletedHandler` in Inventory *correctly* deducts stock across multiple batches via `DeductStockFIFOAsync()` and returns per-batch `FIFODeductionResult.COGS` values, but Accounting **ignores this** and re-queries cost independently using `GetProductCostQuery`, which only returns a single unit cost.

**DB evidence:**
- `Acc_RevenueRecords` (Id=1): `COGS` = 36000, `GrossProfit` = -3000 (used ₱1,200 × 30 = ₱36,000 — the oldest batch price, despite only 10 of those 30 units came from that batch).
- Correct COGS should be: (10 × ₱1,200) + (10 × ₱1,100) + (10 × ₱1,000) = ₱33,000, yielding ₱0 profit (break-even since `RetailPrice` = average cost = ₱1,100).

**Impact:** **Financial statements show phantom losses (or inflated profits, depending on batch price ordering)**. The reported COGS is wrong any time a sale spans multiple batches with different unit costs. This is an accounting accuracy bug — not cosmetic.

**Fix scope:** The Accounting handler should receive the actual per-batch COGS breakdown from the Inventory module's FIFO deduction engine (the `FIFODeductionResult` list from `StockService.DeductStockFIFOAsync`) instead of independently re-querying a single unit cost. Options:
1. Include the per-batch COGS breakdown directly in the `SaleCompletedEvent` (requires the Inventory handler to run first and pass data downstream).
2. Replace `GetProductCostQuery` with a new query that sums COGS across the batches that were *actually consumed* during this specific sale deduction.

#### 18c. Duplicate `RevenueRecord` rows — race between legacy and VAT accounting handlers

Two `Acc_RevenueRecords` exist for the same `(SourceTransactionId=1, ProductId=3)`:
- Record Id=1: `COGS` = 36000, `GrossProfit` = -3000 (created by `SaleCompletedAccountingHandler`)
- Record Id=2: `COGS` = 0, `GrossProfit` = 33000 (created by `SaleCompletedWithVatHandler` in its `Else` branch)

The `SaleCompletedWithVatHandler` is designed to be idempotent: it first queries for an existing `RevenueRecord` matching `(SourceTransactionId, ProductId)` and updates VAT columns if found. However, **the existing record was not found** — the VAT handler's `Else` branch executed, creating a second record. This happened because:
- `SaleCompletedAccountingHandler` was called first and added the record to the EF `DbContext`, but `SaveChangesWithJournalAsync` may not have flushed before `SaleCompletedWithVatHandler.Handle()` ran its `FirstOrDefaultAsync` query on the **same** `DbContext` instance — resulting in the query not finding the record.
- The second record has `COGS = 0` because by the time `GetProductCostQuery` ran for it, all 30 units had already been deducted by the Inventory handler, leaving zero remaining stock.

**DB evidence:**
- `Acc_ExpenseRecords` (Id=4): `Category` = "COGS", `Amount` = 0 (the COGS expense from the second handler)
- Only one `Pos_SalesTransactions` row exists (Id=1), confirming only one sale happened, but two revenue records exist.

**Impact:** Financial reports double-count revenue, and one of the two records has zero COGS. The duplicate must be prevented.

**Fix scope:** The `SaleCompletedAccountingHandler` should call `SaveChangesWithJournalAsync` *before* the VAT handler runs, so the `FirstOrDefaultAsync` in the VAT handler sees the committed record. Alternatively, combine both handlers into a single handler, or ensure handler ordering via MediatR pipeline configuration.

### Dependencies
- 18a: Independent — UI/display change only.
- 18b: Requires rethinking the COGS data flow between Inventory's FIFO deduction engine and Accounting's revenue recording. May require changes to `SaleCompletedEvent` or a new cross-module query.
- 18c: Requires fixing MediatR handler ordering or merging the two accounting handlers. Related to the dual-event design from POS-14.

### Source Document Evidence

The bugs documented above violate requirements and promises stated in the original project source documents. The following passages confirm that **per-batch COGS accuracy** is not a nice-to-have — it is a core design commitment.

**system_plan.md (§6.2 — Inventory Module):**
> "FIFO Costing: Oldest batch cost used first when valuing sold items, per client confirmation. **Batch-level purchase records maintained for accurate COGS calculation.**"

**system_plan.md (§9 — Database Design Principles):**
> "FIFO Batch-Level Records: Purchase price recorded at the batch/lot level per product to support accurate FIFO COGS calculation. **Oldest unreserved batch cost consumed first on each sale.**"

**system_plan.md (§10/§11 — Risk Register, Risk P5, rated High/High):**
> "Price Volatility & Costing Error: Retail price not updated after supplier cost change — confirmed ongoing problem — **leading to incorrect COGS and distorted profit margins.**"
>
> Mitigation: "FIFO batch-level costing ensures historical sales are not retroactively affected by new purchase prices."

Bug 18b is the *exact manifestation* of P5's predicted failure mode: when a single product is purchased from multiple vendors at different prices, COGS is calculated using only the oldest batch's `UnitCost` for the entire quantity, producing distorted profit margins.

**Inventory-Module_AcademicPaper.md (§2 — FIFO Costing and Inventory Valuation Methods):**
> "When a sale occurs, the system deducts units from the oldest batch first, **calculating the cost of goods sold based on that batch's unit cost.** This approach ensures that the FIFO assumption is applied precisely at the batch level, producing accurate cost-of-goods-sold figures." — (Kieso et al., 2023)

The Inventory module (`StockService.DeductStockFIFOAsync`) correctly implements this: it walks batches oldest-first and computes per-batch COGS in `FIFODeductionResult`. The bug is that Accounting ignores this result and re-queries a single unit cost via `GetProductCostQuery`.

**Accounting-Module_AcademicPaper.md (§1.5 — Scope, V1 Features):**
> "The cost of goods sold will be calculated using the FIFO (First-In, First-Out) costing method, as confirmed by the client, with costs recorded at the batch and lot level per product at the time of receiving goods. **The oldest unreserved batch cost will be consumed first on each sale** to ensure that reported COGS accurately reflects the cost of goods actually sold."

**Accounting-Module_AcademicPaper.md (§2 — Theme 3, FIFO Costing):**
> "Without disciplined batch-level cost tracking, the FIFO method degrades into an approximation that may not accurately reflect the actual flow of costs through inventory." — (Garcia & Lim, 2023)
>
> "Without precise product-level costing, businesses cannot determine which products generate the highest margins, which are unprofitable, and where pricing adjustments are needed." — (Villanueva & Reyes, 2022)

**Purchasing-Module_AcademicPaper.md (§1.2 — Conceptual Framework):**
> P5 identifies "price volatility that causes COGS distortions" as an independent variable the module must address.

**Conclusion:** Bug 18b (single-batch COGS for multi-batch sales) directly contradicts the documented design intent of batch-level FIFO COGS. Bugs 18a and 18c are consequential — 18a prevents the user from *seeing* the cost discrepancy before a sale, and 18c corrupts the accounting output after a sale. Together, these three bugs undermine the system's stated goal of "producing COGS figures that accurately represent the cost of goods actually sold" and its P5 risk mitigation.

---


### Future / Backlog Item

## 23. Barcode & RFID Scanning Integration

**Module:** Inventory Management
**Source:** `Inventory-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Integrate hardware barcode and RFID scanners (USB keyboard wedge or Bluetooth HID profiles) into the POS transaction cart and the Purchasing goods receiving views. Scanning product codes should automatically fetch matching products, check stock, and append/receive quantities.
**Why deferred:** Villon Farm Supply manages a compact inventory mix of approximately 50 distinct SKUs, making keyboard search and dropdown selection highly efficient. Physical hardware integration is deferred to V2 when transaction velocity justifies the hardware expense.

---

