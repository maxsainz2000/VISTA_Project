# Purchasing Specification

## Feature: PUR-01

### Overview
Implemented all six Purchasing domain entity classes as specified in PUR-01. These entities model the upstream supply chain from vendor management through goods receipt and accounts payable tracking.

### Requirements
- **Entities/Vendor.vb**: vendor directory entity; inherits `SoftDeletableEntity`
- **Entities/PurchaseOrder.vb**: PO header with lifecycle status; inherits `SoftDeletableEntity`; imports `PurchaseOrderStatus` enum from SharedKernel
- **Entities/PurchaseOrderLine.vb**: per-product PO line with denormalized `ProductName`; inherits `AuditableEntity`
- **Entities/GoodsReceipt.vb**: physical delivery record against a PO; inherits `AuditableEntity`
- **Entities/GoodsReceiptLine.vb**: per-product receipt line with `ExpiryDate` for FIFO batches and `HasDiscrepancy` flag; inherits `AuditableEntity`
- **Entities/AccountsPayableEntry.vb**: vendor invoice obligation with running payment tracking; inherits `AuditableEntity`

## Feature: PUR-02

### Overview
Implemented PUR-02: Purchasing Data Access. Added DbSets to `PurchasingDbContext`, created EF Core entity type configurations for all six Purchasing entities, and seeded three sample vendors for development.

### Requirements
- **Data/PurchasingDbContext.vb**: added six DbSet properties and wired `PurchasingSeedData.Seed()` into `OnModelCreating`
- **Data/Configurations/VendorConfiguration.vb**: `Pur_Vendors` table, unique index on Name, cascade Restrict to PurchaseOrders
- **Data/Configurations/PurchaseOrderConfiguration.vb**: `Pur_PurchaseOrders` table, unique index on OrderNumber, cascade delete to Lines and GoodsReceipts
- **Data/Configurations/PurchaseOrderLineConfiguration.vb**: `Pur_PurchaseOrderLines` table, precision(18,4) on UnitCost, precision(18,2) on LineTotal
- **Data/Configurations/GoodsReceiptConfiguration.vb**: `Pur_GoodsReceipts` table, unique index on ReceiptNumber, cascade delete to Lines
- **Data/Configurations/GoodsReceiptLineConfiguration.vb**: `Pur_GoodsReceiptLines` table, precision(18,4) on UnitCost
- **Data/Configurations/AccountsPayableConfiguration.vb**: `Pur_AccountsPayable` table, precision(18,2) on monetary columns, composite index on VendorId+IsPaid
- **Data/SeedData/PurchasingSeedData.vb**: 3 sample vendors: AgriChem Supplies (5-day lead), FarmFresh Seeds Corp. (7-day lead), Golden Feeds Trading (3-day lead)

## Feature: PUR-03

### Overview
Retrofitted `IPurchaseOrderService` (PUR-03) to persist `Notes` and `ExpectedDeliveryDate` on draft create and update. This gap was identified during PUR-09 (PO Management View) where the editor UI collected both fields but had no service path to save them.

### Requirements
- **Services/IPurchaseOrderService.vb**: Added `Optional notes As String = Nothing` and `Optional expectedDeliveryDate As DateTime? = Nothing` to `CreateDraftAsync` and `UpdateDraftAsync` signatures.
- **Services/PurchaseOrderService.vb**: `CreateDraftAsync`: assigns both fields directly in the object initializer. `UpdateDraftAsync`: conditionally updates `po.Notes` when non-Nothing, and `po.ExpectedDeliveryDate` when `HasValue`, preserving existing values when callers omit the params.
- **Presenters/PurchaseOrderListPresenter.vb**: Updated `SaveDraftAsync` and `SubmitFromEditorAsync` to pass `Editor.Notes` and `Editor.ExpectedDeliveryDate` through to the service calls.

## Feature: PUR-03

### Overview
Implemented PUR-03: PO Lifecycle Service. Created the `IPurchaseOrderService` interface with `CreatePOLineDto`, the `PurchaseOrderService` implementation enforcing the Draft → Submitted → Received → Verified → Closed state machine, the `SequentialNumberGenerator` helper for PO-YYYY-XXXX number generation, and a module-level DI extension method.

### Requirements
- **Services/IPurchaseOrderService.vb**: service interface with all nine methods; also defines `CreatePOLineDto`
- **Services/PurchaseOrderService.vb**: full state machine implementation; `CloseAsync` creates an `AccountsPayableEntry`; `RecalculateTotal` recalculates `TotalAmount` on every line change; all invalid transitions throw `InvalidOperationException`
- **Helpers/SequentialNumberGenerator.vb**: static `Generate(prefix, year, existingNumbers)` method; produces PREFIX-YYYY-XXXX; designed for reuse as GR number generator in PUR-04
- **Extensions/PurchasingServiceCollectionExtensions.vb**: `AddPurchasingServices` extension method on `IServiceCollection`; registers `IPurchaseOrderService` / `PurchaseOrderService` as scoped

## Feature: PUR-04

### Overview
Implemented the Goods Receiving service layer for the Purchasing module. When goods arrive the manager records quantities received (which may differ from what was ordered), captures expiry dates for perishable products, and the system publishes a `GoodsReceivedEvent` so Inventory and Accounting can react.

### Requirements
- **Dtos/ReceiveGoodsDto.vb**: `ReceiveGoodsLineDto` with `ProductId`, `ProductName`, `QuantityOrdered`, `QuantityReceived`, `UnitCost`, `ExpiryDate?`, and `DiscrepancyNotes`.
- **Services/IGoodsReceivingService.vb**: interface declaring `ReceiveGoodsAsync`, `GetReceiptByIdAsync`, `GetReceiptsForPOAsync`.
- **Services/GoodsReceivingService.vb**: implementation:
- Validates PO is in `Submitted` status before proceeding.
- Requires `DiscrepancyNotes` on any line where `QuantityReceived ≠ QuantityOrdered`.
- Generates `GR-YYYY-XXXX` receipt number via existing `SequentialNumberGenerator`.
- Saves `GoodsReceipt` + `GoodsReceiptLine` records and transitions PO to `Received` in a single `SaveChangesAsync` call.
- Publishes `GoodsReceivedEvent` via `IMediator.Publish()` after the database save.
- Returns the reloaded `GoodsReceipt` with lines included.
- **Extensions/PurchasingServiceCollectionExtensions.vb**: registered `IGoodsReceivingService` → `GoodsReceivingService` as Scoped.

## Feature: PUR-05

### Overview
Implemented the Vendor Directory service layer for the Purchasing module. Provides full CRUD, soft delete, case-insensitive search, and purchase history aggregation for vendors.

### Requirements
- **Services/IVendorService.vb**: defines `CreateVendorDto`, `UpdateVendorDto`, `VendorDetailDto`, and the `IVendorService` interface with 7 methods
- **Services/VendorService.vb**: full implementation of `IVendorService` backed by `PurchasingDbContext`

## Feature: PUR-06

### Overview
Implemented the Accounts Payable tracking service layer for the Purchasing module. Provides creation, payment recording, and querying of AP entries against vendor invoices.

### Requirements
- **Services/IAccountsPayableService.vb**: interface defining six AP operations
- **Services/AccountsPayableService.vb**: full implementation

## Feature: PUR-07

### Overview
Implemented the threshold-based Reorder Suggestion Engine (PUR-07). Adds two new entities, a service interface and implementation, two EF Core configurations, and expands `PurchasingDbContext` with the corresponding DbSets.

### Requirements
- **Entities/ReorderConfig.vb**: per-product reorder configuration; stores reorder threshold, safety stock, lead time, seasonal multiplier, and optional preferred vendor FK
- **Entities/ReorderSuggestion.vb**: generated suggestion record with workflow status ("Pending" / "Accepted" / "Dismissed") and optional link to the resulting PO
- **Services/IReorderService.vb**: interface defining `GenerateSuggestionsAsync`, `GetPendingSuggestionsAsync`, `AcceptSuggestionAsync`, `DismissSuggestionAsync`, `UpdateConfigAsync`, `GetAllConfigsAsync`
- **Services/ReorderService.vb**: implementation; sends `GetCurrentStockQuery` via MediatR, computes effective reorder point with optional seasonal multiplier, deduplicates against existing Pending suggestions, and creates a draft PO via `PurchasingDbContext` when a suggestion is accepted
- **Data/Configurations/ReorderConfigConfiguration.vb**: maps to `Pur_ReorderConfigs`; unique index on `ProductId`; nullable FK to `Pur_Vendors` with `SetNull` delete behaviour
- **Data/Configurations/ReorderSuggestionConfiguration.vb**: maps to `Pur_ReorderSuggestions`; composite index on `(ProductId, Status)`
- **Data/PurchasingDbContext.vb**: added `DbSet(Of ReorderConfig)` and `DbSet(Of ReorderSuggestion)`

## Feature: PUR-08

### Overview
Implemented Price Change Detection (PUR-08). When goods are received, the service compares each receipt line's unit cost against the originating PO line and creates a `PriceChangeAlert` for every discrepancy, leaving it unacknowledged until a manager reviews it.

### Requirements
- **Entities/PriceChangeAlert.vb**: new `AuditableEntity` with ProductId, VendorId, PreviousUnitCost, NewUnitCost, ChangePercent, ChangeDirection, GoodsReceiptId, IsAcknowledged, AcknowledgedAt
- **Services/IPriceChangeService.vb**: interface with `DetectChangesAsync`, `GetUnacknowledgedAsync`, `AcknowledgeAsync`, `GetHistoryForProductAsync`
- **Services/PriceChangeService.vb**: implementation; loads the receipt and PO with their lines, skips lines with matching cost, calculates `ChangePercent = ((New - Previous) / Previous) × 100` rounded to 4 dp, sets `ChangeDirection` to "Increase" or "Decrease", bulk-inserts alerts in a single `SaveChangesAsync` call
- **Data/Configurations/PriceChangeAlertConfiguration.vb**: table `Pur_PriceChangeAlerts`, precision(18,4) on all cost/percent columns, indexes on `IsAcknowledged` and `ProductId`
- **Data/PurchasingDbContext.vb**: added `PriceChangeAlerts As DbSet(Of PriceChangeAlert)`
- **Services/GoodsReceivingService.vb**: injected `IPriceChangeService`; calls `DetectChangesAsync(receipt.Id)` immediately after publishing `GoodsReceivedEvent`
- **Extensions/PurchasingServiceCollectionExtensions.vb**: registered `IPriceChangeService → PriceChangeService` as scoped; registered before `IGoodsReceivingService` so the dependency chain resolves

## Feature: PUR-09

### Overview
Implemented the PO Management WinForms Views and Presenters (PUR-09). Provides the Manager with a full-featured list and inline editor for Purchase Orders, including status filtering, search, and role-based button visibility for Owner read-only access.

### Requirements
- **Presenters/PurchaseOrderEditorPresenter.vb**: Editor VM with `POLineItem` class (auto-recalculating `LineTotal`), vendor dropdown, line items `ObservableCollection`, running total, `AddLineCommand`, `RemoveLineCommand`. PropertyChanged handlers wire/unwire on each `POLineItem` to propagate `TotalAmount` changes. `PrepareForNew` / `LoadFromPO` / `ToLineDtos` public API used by the list VM.
- **Presenters/PurchaseOrderListPresenter.vb**: List VM with `PORowItem` class, status/search filtering, `AsyncRelayCommand`s for New/Edit/Submit/Delete/SaveDraft/SubmitFromEditor/Cancel. Holds an `Editor` (PurchaseOrderEditorPresenter) instance and toggles `IsEditorOpen`. `IsManager` boolean controls role visibility.
- **Views/Purchasing/PurchaseOrderListView.Designer code**: UserControl with filter toolbar (Status ComboBox + Search TextBox + action buttons), status legend, main PO DataGrid with status-colored rows and badge column, and a bottom editor panel (gated by `IsEditorOpen`). Editor panel contains vendor ComboBox, ExpectedDeliveryDate DatePicker, Notes TextBox, editable line items DataGrid with per-row Remove button (via `RelativeSource` to UserControl DataContext), running total, and Save Draft / Submit PO / Cancel buttons.
- **Views/Purchasing/PurchaseOrderListView.Designer code.vb**: Code-behind wires `SelectionChanged` to `vm.SelectedOrder`, `MouseDoubleClick` to `EditPOCommand.ExecuteAsync`, and Escape key to clear `SearchText`.
- **Extensions/PurchasingServiceCollectionExtensions.vb**: Added `IVendorService` / `VendorService` scoped registration (previously unregistered per codebase_wiki DI registry note).

## Feature: PUR-10

### Overview
Implemented the Goods Receiving WinForms View and Presenter (PUR-10). Manager selects a Submitted PO from a dropdown, edits actual quantities received, unit costs, and expiry dates, notes discrepancies, and confirms receipt — which calls `IGoodsReceivingService.ReceiveGoodsAsync` and transitions the PO to Received.

### Requirements
- **Presenters/GoodsReceivingPresenter.vb**: Defines two helper classes (`POSelectorItem` for the dropdown, `GRLineItem` for the editable receiving grid with `HasDiscrepancy` auto-recalculation). `GoodsReceivingPresenter` loads Submitted POs on init, pre-fills `QtyReceived = QtyOrdered` when a PO is selected, validates discrepancy notes are present when qty differs, calls `ReceiveGoodsAsync`, and resets state using the backing field directly (bypasses setter to avoid re-triggering `LoadPOLinesAsync`).
- **Views/Purchasing/GoodsReceivingView.Designer code**: PO selector ComboBox + Confirm Receipt button toolbar; status bar; placeholder panel when no PO is selected; receiving DataGrid with `GRRowStyle` (amber highlight on discrepancy rows), read-only product/qty-ordered columns, editable qty-received/unit-cost columns, `DatePicker` for expiry date via `CellEditingTemplate`, editable discrepancy notes column with red styling when `HasDiscrepancy = True`, and a ⚠ indicator badge column.
- **Views/Purchasing/GoodsReceivingView.Designer code.vb**: Minimal code-behind; Presenter injected via constructor and set as `DataContext`.

## Feature: PUR-11

### Overview
Implemented the Vendor Directory View and Presenters (PUR-11). Provides a full vendor management screen with real-time search, inline create/edit panel, soft delete, and a purchase history detail panel for the selected vendor.

### Requirements
- **MerchSys.Purchasing/Presenters/VendorEditorPresenter.vb**: form state VM for create/edit; holds all vendor fields, client-side validation (name required, phone required, lead time > 0), and `ToCreateDto`/`ToUpdateDto` converters
- **MerchSys.Purchasing/Presenters/VendorListPresenter.vb**: main screen VM; vendor list with real-time search filtering, async load/save/delete via `IVendorService`, selection-driven detail panel load, `IsEditorOpen` panel toggle, `IsManager` role guard, `POSummaryRow` row class for recent POs grid
- **MerchSys.App/Views/Purchasing/VendorDirectoryView.Designer code**: UserControl with toolbar (search + Add/Edit/Delete/Refresh), collapsible bottom editor panel (8-field form grid), split main area (vendor DataGrid left, 340px detail panel right with stats + recent POs DataGrid)
- **MerchSys.App/Views/Purchasing/VendorDirectoryView.Designer code.vb**: code-behind wiring `VendorGrid.SelectionChanged` → `vm.SelectedVendor`, double-click to edit, Escape to clear search; Presenter injected via constructor

## Feature: PUR-12

### Overview
Implemented the AP Ledger screen (PUR-12): WinForms View and Presenter for accounts payable management.
Covers outstanding balance summary, full invoice grid with overdue highlighting, status/vendor filters,
and an inline payment recording dialog.

### Requirements
- **MerchSys.Purchasing/Services/IAccountsPayableService.vb**: added `GetAllAsync()` method (needed for All and Paid filters; existing service only exposed outstanding/overdue queries)
- **MerchSys.Purchasing/Services/AccountsPayableService.vb**: implemented `GetAllAsync()`: returns all AP entries with Vendor and PurchaseOrder navigation, ordered by InvoiceDate descending
- **MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb**: registered `IAccountsPayableService → AccountsPayableService` (Scoped) and `APLedgerPresenter` (Transient)
- **MerchSys.Purchasing/Presenters/APLedgerPresenter.vb**: includes `APLedgerRow` and `VendorSelectorItem` helper classes; full filter logic (All/Outstanding/Overdue/Paid + vendor); inline payment dialog state and commands; `IsAllFilterActive` / `IsOutstandingFilterActive` / `IsOverdueFilterActive` / `IsPaidFilterActive` boolean properties for Designer code DataTrigger binding without converters
- **MerchSys.App/Views/Purchasing/APLedgerView.Designer code**: summary header with total outstanding; filter toolbar with four toggle-style status buttons (active state via DataTrigger on `IsXxxFilterActive`); vendor ComboBox; DataGrid with all plan-specified columns (VendorName, InvoiceNumber, InvoiceDate, DueDate, TotalAmount, AmountPaid, Balance, IsPaid, IsOverdue); overdue row highlighting (amber), paid row highlighting (green); payment dialog overlay using Grid + Rectangle backdrop + centered Border card
- **MerchSys.App/Views/Purchasing/APLedgerView.Designer code.vb**: code-behind with DI constructor injection

## Feature: PUR-13

### Overview
Implemented the Reorder Suggestions view and Presenter (PUR-13). A two-tab WinForms screen lets the manager generate, review, accept, and dismiss stock reorder suggestions, and edit per-product reorder configuration thresholds.

### Requirements
- **MerchSys.Purchasing/Presenters/ReorderSuggestionsPresenter.vb**: MVP Presenter with two-tab state (Suggestions / Configuration), filter tabs (Pending / Accepted / Dismissed), generate/accept/dismiss commands, and a config edit dialog
- **MerchSys.App/Views/Purchasing/ReorderSuggestionsView.Designer code**: UserControl with suggestions DataGrid (per-row Accept/Dismiss buttons, seasonal star indicator), configuration DataGrid (Edit button per row), config edit dialog overlay
- **MerchSys.App/Views/Purchasing/ReorderSuggestionsView.Designer code.vb**: DI-injected code-behind
- **MerchSys.Purchasing/Services/IReorderService.vb**: added `GetAllSuggestionsAsync` (required to populate Accepted and Dismissed filter tabs)
- **MerchSys.Purchasing/Services/ReorderService.vb**: implemented `GetAllSuggestionsAsync` (queries all statuses, ordered by `CreatedAt` descending)
- **MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb**: registered `IReorderService → ReorderService` (previously missing) and `ReorderSuggestionsPresenter`

## Feature: PUR-14

### Overview
Implemented the `GoodsReceivedWithVatEvent` publisher for the Purchasing module (PUR-14).
The existing `GoodsReceivedEvent` emission in `GoodsReceivingService.ReceiveGoodsAsync` was
left intact; the new VAT-aware event fires immediately after it so legacy consumers are
unaffected and ACC-10 / ACC-11 now receive input-VAT data on every goods receipt.

### Requirements
- **Services/Vat/GoodsReceiptVatCalculator.vb**: pure-function
calculator; produces `GoodsReceiptVatBreakdown` (VatableInputs, VatExemptInputs, ZeroRatedInputs,
InputVat, VendorInvoiceTotal) using the Option-2 simplification (all lines vatable at 12 %).
- **Services/GoodsReceivingService.vb**: added
`GoodsReceiptVatCalculator` constructor dependency; publishes `GoodsReceivedWithVatEvent`
back-to-back with the legacy `GoodsReceivedEvent` after `SaveChangesAsync`.
- **Extensions/PurchasingServiceCollectionExtensions.vb**: 
registered `GoodsReceiptVatCalculator` as `AddScoped`.

## Feature: PUR-15

### Overview
Implemented per-line VAT classification on `GoodsReceiptLine`, replacing the PUR-14 Option-2
aggregate simplification. Each receipt line now carries its own `VatClassification`, `VatAmount`,
and `VatableSales` computed at receiving time. The goods-receiving UI exposes a VAT classification
ComboBox column and a read-only VAT amount column. `GoodsReceivedWithVatEvent` now carries true
per-line VAT data.

### Requirements
- **Entities/GoodsReceiptLine.vb**: added `VatClassification As VatTreatment`, `VatAmount As Decimal`, and `VatableSales As Decimal` with BIR XML doc referencing NIRC Sec. 106/110.
- **Data/Configurations/GoodsReceiptLineConfiguration.vb**: configured `HasDefaultValue(0)`, `HasPrecision(18,2)`, and `HasDefaultValue(0D)` for the three new columns.
- **Data/Migrations/AddGoodsReceiptLineVatColumns.vb**: manual EF migration class (documentation artifact; actual runtime migration is in DatabaseInitializer per `efcore10-vbnet-migration-discovery-bug`).
- **Data/DatabaseInitializer.vb**: added `20260516140000_AddGoodsReceiptLineVatColumns` migration that `ALTER TABLE`s `Pur_GoodsReceiptLines` to add the three columns with safe defaults.
- **Dtos/ReceiveGoodsDto.vb**: added `VatClassification As VatTreatment` property.
- **Services/GoodsReceivingService.vb**: computes `VatAmount` and `VatableSales` per line during receipt creation; event item loop now reads `grLine.VatClassification` and `grLine.VatAmount` instead of hardcoding `Vatable`.
- **Services/Vat/GoodsReceiptVatCalculator.vb**: replaced Option-2 single-bucket logic with per-classification `Select Case` that reads `line.VatableSales` and `line.VatAmount` to fill the three aggregate buckets.
- **Presenters/GoodsReceivingPresenter.vb**: added `VatClassification` to `GRLineItem` (defaults to `Vatable`); auto-computes `VatAmount` when `VatClassification`, `QtyReceived`, or `UnitCost` changes; added `VatTreatmentValues` list to Presenter for ComboBox binding; `ConfirmReceiptAsync` passes `VatClassification` in DTO.
- **Views/Purchasing/GoodsReceivingView.Designer code**: added `DataGridTemplateColumn` with `ComboBox` bound to `VatClassification` (ItemsSource via `RelativeSource` to Presenter `VatTreatmentValues`); added read-only `DataGridTextColumn` for `VatAmount` (N2 format, green foreground).

## Feature: PUR-16

### Overview
Implemented the Vendor-Product Catalog & PO Line Auto-configuration feature. Replaced the primitive free-text Product ID column in the Purchase Order line editor with a vendor-filtered combobox product selector. On selection, line costs and names are auto-populated from the vendor catalog. A Manager-only master-detail Vendor Product Catalog management interface allows adding, updating, and removing vendor products, backed by secure role checks at the service layer and pre-save blockers preventing PO saving with empty lines.

### Requirements
- **MerchSys.Purchasing/Entities/VendorProduct.vb**: Entity tracking products supplied by specific vendors, inheriting from `SoftDeletableEntity`.
- **MerchSys.Purchasing/Data/Configurations/VendorProductConfiguration.vb**: Configures mapping for table `Pur_VendorProducts`, adds foreign key restraint, and composite unique indexing on `(VendorId, ProductId)` where `IsDeleted = 0`.
- **MerchSys.Purchasing/Data/PurchasingDbContext.vb**: Registered the `VendorProducts` DbSet.
- Created snapshot migration `MerchSys.Purchasing/Migrations/20260527110000_AddVendorProductCatalog.vb`.
- **MerchSys.App/Data/DatabaseInitializer.vb**: Added step to run the raw SQL DDL script for table `Pur_VendorProducts` and index `UX_Pur_VendorProducts_Vendor_Product` on application startup.
- **MerchSys.Purchasing/Dtos/VendorProductDto.vb**: DTO carrying catalog entries across layers.
- Created `MerchSys.Purchasing/Services/IVendorProductService.vb` & `VendorProductService.vb` — Injected `ISessionService` and implemented vendor product catalog lookup and mutation routines (Manager-only mutations enforced at data-layer).
- **MerchSys.SharedKernel/Queries/GetProductsForCatalogQuery.vb**: Cross-module MediatR query to fetch active products matching search inputs.
- **MerchSys.Inventory/Handlers/GetProductsForCatalogQueryHandler.vb**: MediatR handler querying active inventory items using raw SQLite connection.
- **MerchSys.Purchasing/Extensions/PurchasingServiceCollectionExtensions.vb**: Registered catalog service and Presenter transient dependencies.
- **MerchSys.Purchasing/Presenters/PurchaseOrderEditorPresenter.vb**: Added `VendorCatalog` observable collection, and extended nested class `POLineItem` to automatically populate details and trigger updates.
- **MerchSys.Purchasing/Presenters/PurchaseOrderListPresenter.vb**: Injected notifications and catalog services, hooked async catalog reloading, added pre-save line validator (raises toast errors on zero ProductId), and cost write-backs.
- **MerchSys.Purchasing/Presenters/VendorCatalogPresenter.vb**: Master-detail Presenter for catalog management, supporting adds, updates, soft-deletes, and role gates.
- **MerchSys.App/Views/Purchasing/PurchaseOrderListView.Designer code**: Swapped the textbox Product ID column for a premium vendor-filtered Combobox column.
- Created `MerchSys.App/Views/Purchasing/VendorCatalogView.Designer code` & `VendorCatalogView.Designer code.vb` — Full master-detail catalog editor interface with Manager actions, premium styling, and search dialog modals.
- **MerchSys.App/Presenters/MainWindowPresenter.vb**: Inserted "Vendor Product Catalog" navigation item for Manager role.
- **MerchSys.App/Application.Designer code.vb**: Registered the catalog view in the Generic Host DI setup.



## Verification (from PUR-verification-checklist.md)

---
module: Purchasing
source: Purchasing-audit-2026-06-01.md
originally-generated: 2026-05-17
last-synced: 2026-06-01
---

# Operator Verification Checklist — Purchasing

> Extracted from the 2026-05-17 module audit. Only operator/manual verification tasks are listed here.
> All 15 Purchasing plans are completed. These are the remaining acceptance tests.
>
> **How to use:** Do each step in order. Check the box when done. Write what you saw next to each item.
>
> **Login required (INFRA-15):** The app now shows a login screen on launch.
> Unless a test specifically says to log in as Owner, log in as `manager`.
> Default password: `Vista2026!` (first login will prompt you to change it).


### Key file locations

| What | Path |
|------|------|
| SQLite database | `%LOCALAPPDATA%\MerchSys\merchsys.db` |
| GoodsReceivingService | `WinForms_Applications\MerchSys\src\MerchSys.Purchasing\Services\GoodsReceivingService.vb` |
| GoodsReceivedWithVatEvent | `WinForms_Applications\MerchSys\src\MerchSys.SharedKernel\Events\GoodsReceivedWithVatEvent.vb` |
| GoodsReceivedWithVatHandler | `WinForms_Applications\MerchSys\src\MerchSys.Accounting\Handlers\GoodsReceivedWithVatHandler.vb` |
| GoodsReceiptVatCalculator | `WinForms_Applications\MerchSys\src\MerchSys.Purchasing\Services\Vat\GoodsReceiptVatCalculator.vb` |

---

## PUR-14 + PUR-15 — VAT Event Pipeline (combined)

> PUR-14 and PUR-15 share the same idempotency check. The tests below cover both plans.

### Test 1: Single-classification receipt creates VAT entries

**What to do:**
1. Launch the app (F5).
2. Go to the **Purchasing** section in the sidebar.
3. Create a Purchase Order with items that all have the **same** VAT classification (for example, all "Vatable"). Use a VAT-inclusive unit cost such as ₱1,120 (= ₱1,000 net + ₱120 VAT).
4. Submit the PO, then go to **Goods Receiving** and receive the goods for that PO.
5. Open DB Browser for SQLite and open `%LOCALAPPDATA%\MerchSys\merchsys.db`.
6. Run this query (replace `<PO_ID>` with the actual Purchase Order ID — you can find it in `Pur_PurchaseOrders`):
   ```sql
   SELECT Id, Description, Amount, VatableAmount, VatExemptAmount, InputVat, VatTreatment
   FROM Acc_ExpenseRecords
   WHERE SourceModule = 'Purchasing' AND SourceReferenceId = <PO_ID>;
   ```

> **Note:** `GoodsReceivedWithVatHandler` writes VAT data to `Acc_ExpenseRecords` (adding/updating the
> `VatableAmount`, `VatExemptAmount`, `InputVat`, and `VatTreatment` columns added by migration
> `20260510100000_AddVatLedgerColumns`). `Acc_VatReturnLines` is the BIR period-filing table and is
> only populated when you formally generate a VAT return — it is **not** written during goods receiving.

> **Event publisher:** `WinForms_Applications\MerchSys\src\MerchSys.Purchasing\Services\GoodsReceivingService.vb`
> **Event handler:** `WinForms_Applications\MerchSys\src\MerchSys.Accounting\Handlers\GoodsReceivedWithVatHandler.vb`

**What you should see:**
- One row per line item on the PO appears in `Acc_ExpenseRecords`.
- `VatableAmount` = net cost (VAT-inclusive price ÷ 1.12), e.g. ₱1,000 for a ₱1,120 item.
- `InputVat` = VAT portion, e.g. ₱120.
- `VatExemptAmount` = 0.
- `VatTreatment` = 0 (the integer value for the `Vatable` enum member).

- [x] Single-classification receipt: input-VAT row appears in Acc_ExpenseRecords with correct VatableAmount and InputVat — PO-2026-0001 (id=1): VatableAmount=1000.0, InputVat=120.0, VatExemptAmount=0, VatTreatment=0 ✅ 2026-05-20

> **⚠️ Post-pivot regression fixed 2026-05-29 (`debug/PUR-vat-ledger-columns`):** Confirm Receipt threw
> `MySqlException: Unknown column 'InputVat' in 'field list'`. The VAT columns existed only in the SQLite-era
> migration `20260510100000_AddVatLedgerColumns`, which was never ported to the central MariaDB schema during
> the 2026-05-28 pivot. Fixed by new migration `Migrations/Central/AddAccVatLedgerColumns.sql`
> (`ADD COLUMN IF NOT EXISTS` on `Acc_ExpenseRecords` + `Acc_RevenueRecords`). See
> `agent_wiki/errors/efcore-vat-ledger-columns-missing-central-schema.md`.

---

### Test 2: Mixed-classification receipt sums correctly

**What to do:**
1. Create a new Purchase Order with items that have **different** VAT classifications. For example:
   - Line 1: "Vatable" item, unit cost ₱1,120 VAT-inclusive (₱1,000 net + ₱120 VAT)
   - Line 2: "VAT-Exempt" item, unit cost ₱500
2. Submit the PO, then receive the goods for this PO.
3. Run this query with the new PO ID:
   ```sql
   SELECT Id, Description, Amount, VatableAmount, VatExemptAmount, InputVat, VatTreatment
   FROM Acc_ExpenseRecords
   WHERE SourceModule = 'Purchasing' AND SourceReferenceId = <PO_ID>;
   ```

> **VAT calculator:** `WinForms_Applications\MerchSys\src\MerchSys.Purchasing\Services\Vat\GoodsReceiptVatCalculator.vb`

**What you should see:**
- Two rows in `Acc_ExpenseRecords` — one per line item.
- Vatable row: `VatableAmount` = ₱1,000, `InputVat` = ₱120, `VatTreatment` = 0.
- Exempt row: `VatExemptAmount` = ₱500, `InputVat` = 0, `VatTreatment` = 1.
- The Exempt line contributes nothing to `InputVat`.

- [x] Mixed-classification receipt: VAT sums only from Vatable lines, Exempt excluded — PO-2026-0002 (id=2): Vatable line VatableAmount=1000.0 InputVat=120.0; Exempt line VatExemptAmount=500.0 InputVat=0.0 ✅ 2026-05-20

---

### Test 3: Handler is idempotent (no duplicate VAT entries)

> This single test covers both PUR-14 and PUR-15 idempotency requirements.

**What to do:**
1. Use one of the POs from Test 1 or Test 2.
2. Find a way to re-publish the `GoodsReceivedWithVatEvent` for the same PO. You can do this by:
   - Setting a breakpoint in `GoodsReceivingService.vb` where the event is published, OR
   - Calling the event publisher manually from the Immediate Window, OR
   - Simply receiving the same PO again if the UI allows it.
3. After triggering the event a second time, run the query again:
   ```sql
   SELECT COUNT(*) FROM Acc_ExpenseRecords
   WHERE SourceModule = 'Purchasing' AND SourceReferenceId = <PO_ID>;
   ```

> **Handler (checks for duplicates):** `WinForms_Applications\MerchSys\src\MerchSys.Accounting\Handlers\GoodsReceivedWithVatHandler.vb`

**What you should see:**
- The count is the **same** as before. No new rows were created.
- The handler finds the existing `Acc_ExpenseRecords` row(s) by `(SourceModule='Purchasing', SourceReferenceId, Description.Contains(ProductName))` and **updates** them in place rather than inserting duplicates.

- [x] Re-publishing the event for the same PO does NOT create duplicate VAT entries — PO-2026-0003 (id=3): COUNT=1 after second Immediate Window publish, EF log shows SELECT→UPDATE (no INSERT), row detail: VatableAmount=6696.0 InputVat=803.57 VatTreatment=0 ✅ 2026-05-21


### Future / Backlog Item

## 16. Vendor-Product Catalog & PO Auto-configuration

**Status:** PROMOTED (2026-05-27) — see `Plans/VISTA_Modules/Purchasing/16-vendor-product-catalog.md` (PUR-16). ProductId=0 bug fix folded into the same plan.
**Module:** Purchasing
**Source:** User Observation (2026-05-27)
**Description:** Implement a relationship between Vendors and Products (Vendor Catalog).
- During Purchase Order creation, clicking "Add Line" should present a dropdown of products filtered specifically to what the selected Vendor supplies, instead of requiring manual text input.
- Upon selecting a product, the unit cost should automatically configure based on the most recent vendor price or agreed pricing list, so the user only needs to input the quantity.
- Bug fix required: The Product ID currently stays 0 when clicking "Add Line" multiple times in the PO screen.
**Why deferred:** The current Purchasing module focuses on a simple, unstructured PO flow. A formal vendor-product relationship requires new DB tables (e.g., `VendorProducts`), UI redesign for vendor catalogs, and more complex PO line creation logic.

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

## 25. Machine Learning / AI-Based Seasonal Demand Forecasting

**Module:** Purchasing / Procurement
**Source:** `Purchasing-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Replace simple statistical multipliers and moving averages with formal machine learning time-series forecasting (e.g., ARIMA or ML.NET prediction pipelines) to proactively model palay planting and growing season demand peaks.
**Why deferred:** The store's limited historical transaction volume in pre-live stages makes complex ML models highly prone to overfitting. The implemented `ReorderService.vb` uses a deterministic `SeasonalMultiplier` and `MinimumThreshold` trigger which is more stable.

---

