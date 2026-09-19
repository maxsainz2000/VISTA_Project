---
module: Integration
plan-id: INT-09
title: "IInventoryAuditService Implementation"
depends-on: [INT-07]
estimated-files: 3
---

# IInventoryAuditService Implementation

## Context

During the INT-07 DI registration pass, 9 of 10 missing service interfaces were successfully registered in `Application.xaml.vb`. The 10th — `IInventoryAuditService` — was skipped because **the interface and implementation do not exist in the codebase**. The original source plan INV-03 (Stock Management) was expected to create this service, but its deliverables focused exclusively on `IStockService`/`StockService` and the FIFO deduction engine.

The `codebase_wiki/schemas/di-registry.md` entry currently reads `*Pending* / Not yet registered`, which is misleading — the interface itself is absent, not merely unregistered. This is the **only remaining unregistered service dependency** in the application.

**Audit sources:** `Pending_Tasks/Integration-audit-2026-05-09.md` (Summary & Recommendations), `Progress/VISTA_Modules/Integration/INT-07-summary.md` (Issues section)

## Prerequisites

- INT-07 (DI Registration Gaps) — completed. All other services are registered; only `IInventoryAuditService` remains.
- INV-03 (Stock Management) — completed. Provides the Inventory service infrastructure pattern to follow.

## Deliverables

### 1. Create `IInventoryAuditService` Interface

**File:** `MerchSys.Inventory/Services/IInventoryAuditService.vb`

Define the service contract for inventory audit operations. The interface should provide methods for:

- Performing a stock count comparison (expected vs. actual quantities)
- Recording stock count adjustments
- Generating an audit trail for a given product or date range
- Retrieving audit history

Follow the existing service interface pattern established by `IStockService.vb`, `IShrinkageService.vb`, etc.

### 2. Create `InventoryAuditService` Implementation

**File:** `MerchSys.Inventory/Services/InventoryAuditService.vb`

Implement the interface with:

- Constructor injection of `InventoryDbContext`
- Audit column population (`CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt`)
- Soft-delete support where applicable
- Async/Await pattern matching existing services

### 3. Register in DI Container

**File:** `MerchSys.App/Application.xaml.vb`

Add Scoped registration:

```vb
services.AddScoped(Of IInventoryAuditService, InventoryAuditService)()
```

Group with the existing Inventory service registrations block.

### 4. Codebase Wiki Update

Update `LLM_Wiki/codebase_wiki/schemas/di-registry.md`:

| Entry | Current | Correct |
|---|---|---|
| `IInventoryAuditService` | *Pending* / Not yet registered | Scoped — registered |

## Implementation Notes

- Follow the service patterns established in INV-03 (`StockService`), INV-06 (`AlertConfigService`), and INV-07 (`ShrinkageService`).
- The `InventoryAuditService` may need to query `StockBatch` and `Product` entities to compare expected vs. actual stock levels.
- Consider referencing `StockMovement` records (INT-08) for building the audit trail.

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` — **0 errors, 0 warnings**
2. `IInventoryAuditService.vb` exists with a well-defined contract
3. `InventoryAuditService.vb` implements the interface with `InventoryDbContext` injection
4. `Application.xaml.vb` registers `IInventoryAuditService → InventoryAuditService` as Scoped
5. All 16 views navigable without `InvalidOperationException` (no remaining DI gaps)

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-09-summary.md`.
