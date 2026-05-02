---
module: MerchSys.Inventory
plan-id: INV-02
title: "Inventory Data Access"
depends-on: [INFRA-03, INV-01]
estimated-files: 7
---

# Inventory Data Access

## Context

Configures the `InventoryDbContext` with EF Core entity configurations for all Inventory entities, seed data for product categories and sample products.

## Prerequisites

- **INFRA-03** (Database Contexts) — `InventoryDbContext` shell exists
- **INV-01** (Domain Models) — all Inventory entities exist

## Wiki References

- `analysis/tech-stack-reference.md` — 3NF, EF Core 10, table prefixes
- `entities/villon-farm-supply.md` — "~50 SKUs", 4 product categories

## Deliverables

```
MerchSys.Inventory/Data/
├── InventoryDbContext.vb                  ← Update: add DbSets
├── Configurations/
│   ├── ProductConfiguration.vb
│   ├── StockBatchConfiguration.vb
│   ├── ShrinkageRecordConfiguration.vb
│   ├── StockAlertConfigConfiguration.vb
│   └── ProductCategoryConfiguration.vb
└── SeedData/
    └── InventorySeedData.vb
```

## Specification

### DbSets
Products, StockBatches, ShrinkageRecords, StockAlertConfigs, ProductCategories

### Table Names (Inv_ prefix)
- `Inv_Products`, `Inv_StockBatches`, `Inv_ShrinkageRecords`, `Inv_StockAlertConfigs`, `Inv_ProductCategories`

### Key Configurations
- **Product:** Name required max 200, Sku required max 50 unique index, RetailPrice precision(18,2)
- **StockBatch:** UnitCost precision(18,4), Index on ProductId + ReceiptDate (for FIFO ordering)
- **ShrinkageRecord:** UnitCost precision(18,4), TotalValue precision(18,2), Reason required max 50
- **ProductCategory:** Name required max 100, unique index

### Seed Data

4 product categories: Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds

Sample products (5 per category, ~20 total):
- Fertilizers: Complete Fertilizer 14-14-14, Urea 46-0-0, Ammonium Sulfate, etc.
- Pesticides: Malathion, Cypermethrin, Lambda-cyhalothrin, etc.
- Seeds: Hybrid Rice RC222, Corn Yellow, Eggplant seeds, etc.
- Feeds: Hog Grower Pellets, Poultry Layer Mash, etc.

## Acceptance Criteria

1. `dotnet build` succeeds
2. All tables use `Inv_` prefix
3. FIFO index exists on StockBatch (ProductId + ReceiptDate)
4. Seed data creates categories and sample products
5. All FK relationships properly configured

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Inventory/INV-02-summary.md`
