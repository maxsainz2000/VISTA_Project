---
module: MerchSys.Purchasing
plan-id: PUR-16
title: "Vendor-Product Catalog & PO Line Auto-configuration"
depends-on: [PUR-03, PUR-05, PUR-09]
estimated-files: 9
priority: medium
---

# Vendor-Product Catalog & PO Line Auto-configuration

## Context

The PO editor (`PurchaseOrderEditorViewModel.vb`) currently has no concept of which products a vendor actually supplies. Users add PO lines by manually typing the integer `ProductId` into a `DataGridTextColumn`, then manually typing the `UnitCost`. Two problems flow from this:

1. **Operational friction** — the Manager has to remember product IDs and last-agreed costs for each vendor. Nothing prevents typing a product the vendor doesn't supply, and nothing remembers prior unit costs.
2. **Latent bug** — `POLineItem` initializes `ProductId = 0`, and `AddEmptyLine()` creates a fresh line each time the button is clicked without resetting/validating. If the user clicks "Add Line" multiple times and forgets to fill `ProductId`, lines persist with `ProductId = 0` and are silently saved.

This plan introduces a `VendorProduct` catalog table (Vendor ↔ Product with a remembered `LastUnitCost`), replaces the PO editor's free-text `ProductId` column with a vendor-filtered `ComboBox`, auto-fills `UnitCost` from the catalog, and adds a pre-save validator that rejects any line with `ProductId = 0`. A new Vendor Catalog view lets Managers maintain the catalog.

Promoted from `Plans/Future/deferred-features-backlog.md` item 16 (2026-05-27). The earlier "fold or split the bug fix" decision: **fold** — the ComboBox structurally replaces the broken control, so a separate bug-fix plan would duplicate scope.

## Prerequisites

- **PUR-03** (PO Lifecycle Service) — `PurchaseOrder`, `PurchaseOrderLine`, save pipeline
- **PUR-05** (Vendor Directory) — `Vendor` entity, `Pur_Vendors` table
- **PUR-09** (PO Management View) — `PurchaseOrderListView.xaml`, `PurchaseOrderEditorViewModel`

## Wiki References

- `concepts/modular-monolith.md` — cross-module reference rules (no EF navigation across modules; use `ProductId` + denormalized name)
- `concepts/client-server-wpf.md` — MVVM and `DatabaseInitializer` migration pattern
- `agent_wiki/antipatterns/` — VB.NET build traps (namespace doubling, `entry` in DbContext, lambda param shadowing)

## Deliverables

```
MerchSys.Purchasing/Entities/
└── VendorProduct.vb                                       ' NEW — VendorId, ProductId, ProductName (denorm), LastUnitCost, Notes, audit cols, soft-delete

MerchSys.Purchasing/Data/Configurations/
└── VendorProductConfiguration.vb                          ' NEW — table Pur_VendorProducts, unique index (VendorId, ProductId) where IsDeleted = 0

MerchSys.Purchasing/Data/
└── PurchasingDbContext.vb                                 ' MOD — add VendorProducts DbSet

MerchSys.Purchasing/Migrations/
└── 2026XXXX_AddVendorProductCatalog.vb                    ' NEW — manual SQL migration (CREATE TABLE + unique index)

MerchSys.Purchasing/Services/
└── VendorProductService.vb                                ' NEW — Add/Update/Remove, GetCatalogForVendorAsync, UpdateLastUnitCostAsync

MerchSys.Purchasing/ViewModels/
├── PurchaseOrderEditorViewModel.vb                        ' MOD — VendorCatalog property, AddEmptyLine validator, SelectedCatalogEntry handler
└── VendorCatalogViewModel.vb                              ' NEW — CRUD for VendorProduct rows

MerchSys.SharedKernel/Queries/
└── GetProductsForCatalogQuery.vb                          ' NEW — returns Id, Name, Sku — handled by Inventory module

MerchSys.App/Views/Purchasing/
├── PurchaseOrderListView.xaml(.vb)                        ' MOD — swap ProductId TextBox column for vendor-filtered ComboBox
└── VendorCatalogView.xaml(.vb)                            ' NEW — Manager-only catalog editor
```

## Specification

### `VendorProduct` entity

```vb
Public Class VendorProduct
    Inherits SoftDeletableEntity

    Public Property VendorId As Integer
    Public Property ProductId As Integer                ' Cross-module ref (no EF FK)
    Public Property ProductName As String               ' Denormalized for display
    Public Property LastUnitCost As Decimal
    Public Property Notes As String

    Public Overridable Property Vendor As Vendor
End Class
```

- Inherits `SoftDeletableEntity` (audit columns + `IsDeleted`).
- No EF navigation to `Product` — matches the existing `PurchaseOrderLine` pattern (`ProductId` + denormalized `ProductName`).

### `VendorProductConfiguration`

- Table: `Pur_VendorProducts`.
- Unique composite index on (`VendorId`, `ProductId`) **filtered** to `IsDeleted = 0` (allows re-adding a previously soft-deleted catalog row).
- FK `VendorId → Pur_Vendors(Id)` with `DeleteBehavior.Restrict`.
- `ProductName` required, `MaxLength = 200` (mirror `PurchaseOrderLine` configuration).

### Migration

Manual SQL (per CLAUDE.md EF CLI ban):

```sql
CREATE TABLE IF NOT EXISTS Pur_VendorProducts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VendorId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    ProductName TEXT NOT NULL,
    LastUnitCost NUMERIC NOT NULL DEFAULT 0,
    Notes TEXT,
    CreatedBy TEXT, CreatedAt TEXT,
    ModifiedBy TEXT, ModifiedAt TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    DeletedBy TEXT, DeletedAt TEXT,
    FOREIGN KEY (VendorId) REFERENCES Pur_Vendors(Id)
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Pur_VendorProducts_Vendor_Product
    ON Pur_VendorProducts (VendorId, ProductId)
    WHERE IsDeleted = 0;
```

Applied by `DatabaseInitializer` at `Application_Startup`. No `dotnet ef`.

### `VendorProductService`

```vb
Public Interface IVendorProductService
    Function GetCatalogForVendorAsync(vendorId As Integer) As Task(Of IReadOnlyList(Of VendorProductDto))
    Function AddCatalogEntryAsync(vendorId As Integer, productId As Integer, productName As String, unitCost As Decimal, notes As String) As Task(Of VendorProduct)
    Function UpdateCatalogEntryAsync(id As Integer, unitCost As Decimal, notes As String) As Task
    Function RemoveCatalogEntryAsync(id As Integer) As Task
    Function UpdateLastUnitCostAsync(vendorId As Integer, productId As Integer, newCost As Decimal) As Task
End Interface
```

- Plain `_db.SaveChangesAsync()` — no `ISyncableRepository` (single-branch, per backlog item 10 closure 2026-05-27).
- Role enforcement at the data layer per CLAUDE.md: `AddCatalogEntryAsync`/`UpdateCatalogEntryAsync`/`RemoveCatalogEntryAsync` must reject non-Manager callers using the existing session-user accessor.

### `GetProductsForCatalogQuery` (SharedKernel)

```vb
Public Class GetProductsForCatalogQuery
    Implements IRequest(Of IReadOnlyList(Of ProductLookupDto))

    Public Property SearchTerm As String      ' nullable; empty => return all
End Class

Public Class ProductLookupDto
    Public Property Id As Integer
    Public Property Name As String
    Public Property Sku As String
End Class
```

Handler lives in `MerchSys.Inventory/Handlers/GetProductsForCatalogQueryHandler.vb`. Mirrors the existing `GetCurrentStockQuery` pattern.

### `PurchaseOrderEditorViewModel` changes

- New observable property `VendorCatalog As ObservableCollection(Of VendorProductDto)`.
- `SelectedVendor` setter (or partial method on the existing setter) calls `_vendorProductService.GetCatalogForVendorAsync` and refreshes `VendorCatalog`. Lines whose `ProductId` is not in the new catalog get a non-blocking warning flag (UI hint only — don't auto-delete).
- `POLineItem` gains a `SelectedCatalogEntry As VendorProductDto` setter that assigns `ProductId`, `ProductName`, and `UnitCost` in one operation.
- `AddEmptyLine()` keeps creating a line with `ProductId = 0` (user must pick from the ComboBox). Add a pre-save validator: any line with `ProductId = 0` blocks `SaveAsync` and surfaces a `Notification.Wpf` error listing the offending row numbers.
- After successful save, for each line where the user typed a `UnitCost` different from the catalog's `LastUnitCost`, call `_vendorProductService.UpdateLastUnitCostAsync` so the catalog learns from the latest agreement.

### `PurchaseOrderListView.xaml` change

Replace the existing `DataGridTextColumn` for `ProductId` (currently around line 299) with:

```xml
<DataGridTemplateColumn Header="Product" Width="*">
  <DataGridTemplateColumn.CellTemplate>
    <DataTemplate>
      <TextBlock Text="{Binding ProductName}" />
    </DataTemplate>
  </DataGridTemplateColumn.CellTemplate>
  <DataGridTemplateColumn.CellEditingTemplate>
    <DataTemplate>
      <ComboBox
        ItemsSource="{Binding DataContext.VendorCatalog,
                      RelativeSource={RelativeSource AncestorType=DataGrid}}"
        SelectedValue="{Binding ProductId, Mode=TwoWay,
                                UpdateSourceTrigger=PropertyChanged}"
        SelectedValuePath="ProductId"
        DisplayMemberPath="ProductName"
        IsSynchronizedWithCurrentItem="False" />
    </DataTemplate>
  </DataGridTemplateColumn.CellEditingTemplate>
</DataGridTemplateColumn>
```

Note the namespace-prefix trap from CLAUDE.md (`xmlns:x="clr-namespace:MerchSys.App.Views.Purchasing"`, not `clr-namespace:Views.Purchasing`).

The `UnitCost` column stays as a `DataGridTextColumn` but is pre-populated by `POLineItem.SelectedCatalogEntry`. Manager may override.

### `VendorCatalogView`

- Master/detail: vendor list (left), catalog rows for the selected vendor (right).
- "Add Product" button opens a search dialog driven by `GetProductsForCatalogQuery`.
- Edit `LastUnitCost`, `Notes`; remove rows (soft delete via `IsDeleted`).
- Manager-only — service-layer enforced (CLAUDE.md security rule). Hide menu entry for Owner role at the App shell as a secondary control.

## Acceptance Criteria

- `Pur_VendorProducts` table created on first app launch after deployment (no `dotnet ef` invocation).
- Selecting a vendor in the PO editor repopulates the product `ComboBox` with only that vendor's catalog.
- Selecting a catalog product auto-fills `UnitCost` from `VendorProduct.LastUnitCost`; manual override is allowed.
- On PO save, any overridden `UnitCost` writes back to `VendorProduct.LastUnitCost`.
- Saving a PO with any line where `ProductId = 0` is blocked with a clear error notification — the latent bug is gone.
- Manager can add/edit/remove catalog entries from `VendorCatalogView`. Owner sees the catalog view as read-only or hidden.
- Build clean (0 errors / 0 warnings) per CLAUDE.md.

## Out of Scope (Defer)

- Bulk catalog import from CSV / vendor price lists.
- Vendor cost history table (cost history is implicit via `StockBatch.UnitCost` + `Pur_PriceChangeAlerts` — see INV-14 "Out of Scope").
- Sync_Journal participation for `VendorProduct` writes (single-branch deployment — see backlog item 10 closure 2026-05-27). Reopen only if multi-branch is approved.
- Per-vendor "preferred" flag on catalog entries (today there's `ReorderConfig.PreferredVendorId`; revisit if conflict arises).

## Notes for Implementers

- `POLineItem` is currently a nested class inside `PurchaseOrderEditorViewModel.vb` (lines 14-67) — consider extracting it to `MerchSys.Purchasing/ViewModels/POLineItem.vb` if the new setter logic crosses ~30 lines.
- The denormalized `ProductName` on `VendorProduct` is a snapshot at catalog-entry time. If a product is renamed via `ProductManagementViewModel`, catalog rows will show the old name until they're edited. This matches the project's existing tolerance for denormalized cross-module text (`PurchaseOrderLine.ProductName` has the same property). Don't add a sync mechanism — it's YAGNI for ~50 SKUs at one branch.
- VB.NET build traps relevant here: namespace doubling, lambda parameter shadowing in any LINQ over `VendorCatalog`, and XAML `clr-namespace` prefix on the new `VendorCatalogView`.
