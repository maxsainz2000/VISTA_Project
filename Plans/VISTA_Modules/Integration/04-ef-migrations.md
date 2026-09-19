---
module: Integration
plan-id: INT-04
title: "EF Core Migrations & Data Layer Finalization"
depends-on: [INT-03]
estimated-files: 8
---

# EF Core Migrations & Data Layer Finalization

## Context

All four module DbContexts have entity mappings and DbSet properties, but no EF Core migrations have been generated or applied. The SQLite database file does not exist yet. This plan creates initial migrations for all modules and verifies the database schema.

**Audit sources:** Purchasing, Inventory, and Infrastructure audit reports in `Pending_Tasks/`.

## Prerequisites

- INT-03 (Cross-Module Handlers) — all entities finalized before generating migrations.
- All module data-access plans (PUR-02, INV-02, POS-02, ACC-02) — completed.
- INFRA-03 (Database Contexts) — completed.

## Wiki References

- `LLM_Wiki/wiki/analysis/tech-stack-reference.md` — EF Core version, SQLite provider
- `LLM_Wiki/wiki/concepts/modular-monolith.md` — one DbContext per module, single SQLite file

## Deliverables

### 1. Generate Migrations (one per module)

Run from `WPF_Applications/MerchSys/`:

```powershell
dotnet ef migrations add InitialPurchasing --project src/MerchSys.Purchasing --startup-project src/MerchSys.App
dotnet ef migrations add InitialInventory --project src/MerchSys.Inventory --startup-project src/MerchSys.App
dotnet ef migrations add InitialPOS --project src/MerchSys.POS --startup-project src/MerchSys.App
dotnet ef migrations add InitialAccounting --project src/MerchSys.Accounting --startup-project src/MerchSys.App
```

### 2. Apply All Migrations

```powershell
dotnet ef database update --project src/MerchSys.Purchasing --startup-project src/MerchSys.App
dotnet ef database update --project src/MerchSys.Inventory --startup-project src/MerchSys.App
dotnet ef database update --project src/MerchSys.POS --startup-project src/MerchSys.App
dotnet ef database update --project src/MerchSys.Accounting --startup-project src/MerchSys.App
```

### 3. Verify Database Schema

- Confirm `merchsys.db` exists
- All expected tables present with `Pur_`, `Inv_`, `Pos_`, `Acc_` prefixes
- Audit columns (`CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt`) on every table
- `IsDeleted` on all financial/inventory records

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` — **0 errors, 0 warnings**
2. All four migration commands succeed
3. All four database update commands succeed
4. `merchsys.db` created with all expected tables
5. Audit and soft-delete columns verified

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-04-summary.md`.
