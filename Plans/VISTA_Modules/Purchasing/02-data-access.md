---
module: MerchSys.Purchasing
plan-id: PUR-02
title: "Purchasing Data Access"
depends-on: [INFRA-03, PUR-01]
estimated-files: 8
---

# Purchasing Data Access

## Context

Configures the `PurchasingDbContext` with EF Core entity configurations for all Purchasing entities. Creates seed data for development and defines repository-style data access methods.

## Prerequisites

- **INFRA-03** (Database Contexts) — `PurchasingDbContext` shell exists
- **PUR-01** (Domain Models) — all Purchasing entities exist

## Wiki References

- `analysis/tech-stack-reference.md` — "3NF", audit columns, soft deletes, EF Core 10
- `entities/module-purchasing.md` — data flow, entity relationships

## Deliverables

```
MerchSys.Purchasing/
├── Data/
│   ├── PurchasingDbContext.vb             ← Update: add DbSets
│   ├── Configurations/
│   │   ├── VendorConfiguration.vb
│   │   ├── PurchaseOrderConfiguration.vb
│   │   ├── PurchaseOrderLineConfiguration.vb
│   │   ├── GoodsReceiptConfiguration.vb
│   │   ├── GoodsReceiptLineConfiguration.vb
│   │   └── AccountsPayableConfiguration.vb
│   └── SeedData/
│       └── PurchasingSeedData.vb          ← Dev seed: sample vendors
```

## Specification

### DbContext Update

Add DbSets to `PurchasingDbContext`:
```
Public DbSet(Of Vendor) Vendors
Public DbSet(Of PurchaseOrder) PurchaseOrders
Public DbSet(Of PurchaseOrderLine) PurchaseOrderLines
Public DbSet(Of GoodsReceipt) GoodsReceipts
Public DbSet(Of GoodsReceiptLine) GoodsReceiptLines
Public DbSet(Of AccountsPayableEntry) AccountsPayableEntries
```

### Entity Configurations

Each configuration implements `IEntityTypeConfiguration(Of T)`:

**VendorConfiguration:**
- Table name: `Pur_Vendors`
- `Name` — required, max 200
- `ContactPerson` — required, max 100
- `Phone` — required, max 20
- `Email` — optional, max 100
- `Address` — required, max 500
- Index on `Name` (unique)

**PurchaseOrderConfiguration:**
- Table name: `Pur_PurchaseOrders`
- `OrderNumber` — required, max 20, unique index
- `VendorId` — required FK to `Pur_Vendors`
- `Status` — stored as integer
- `TotalAmount` — precision(18, 2)
- Cascade delete to Lines and GoodsReceipts

**PurchaseOrderLineConfiguration:**
- Table name: `Pur_PurchaseOrderLines`
- `PurchaseOrderId` — required FK
- `ProductName` — required, max 200
- `UnitCost` — precision(18, 4) (4 decimal places for purchase costs)
- `LineTotal` — precision(18, 2)

**GoodsReceiptConfiguration:**
- Table name: `Pur_GoodsReceipts`
- `ReceiptNumber` — required, max 20, unique index
- `PurchaseOrderId` — required FK

**GoodsReceiptLineConfiguration:**
- Table name: `Pur_GoodsReceiptLines`
- `GoodsReceiptId` — required FK
- `UnitCost` — precision(18, 4)

**AccountsPayableConfiguration:**
- Table name: `Pur_AccountsPayable`
- `PurchaseOrderId` — required FK
- `VendorId` — required FK
- `TotalAmount`, `AmountPaid`, `Balance` — precision(18, 2)
- Index on `VendorId` + `IsPaid`

### Seed Data

Create 3 sample vendors for development:
1. "AgriChem Supplies" — pesticides/chemicals, 5-day lead time
2. "FarmFresh Seeds Corp." — seeds, 7-day lead time
3. "Golden Feeds Trading" — animal feeds, 3-day lead time

## Implementation Notes

- All table names use the `Pur_` prefix to avoid collisions in the shared SQLite database
- Use `precision(18, 4)` for cost prices (4 decimal places needed for unit costs)
- Use `precision(18, 2)` for totals and monetary summaries
- Cascade delete from PO → Lines and PO → GoodsReceipts
- Seed data should use `HasData()` in the configuration or a separate seeding mechanism

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors
2. All tables use `Pur_` prefix
3. All FK relationships are properly configured
4. `dotnet ef migrations add InitialPurchasing --project src/MerchSys.Purchasing` succeeds (or equivalent)
5. Seed data creates 3 vendors

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Purchasing/PUR-02-summary.md`

### Documentation
- XML doc comments on the DbContext explaining table prefix convention
