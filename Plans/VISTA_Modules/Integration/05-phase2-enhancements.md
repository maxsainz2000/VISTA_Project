---
module: Integration
plan-id: INT-05
title: "Phase 2 Enhancements"
depends-on: [INT-04]
estimated-files: 5
---

# Phase 2 Enhancements

## Context

These are non-blocking improvements identified during the module audits. They are not required for the application to function end-to-end but improve data accuracy, code quality, and user experience. They were separated from the core integration plans (INT-01 through INT-04) because they can be deferred without impacting runtime functionality.

**Audit sources:** `Pending_Tasks/Inventory-audit-2026-05-07.md`, `Pending_Tasks/POS-audit-2026-05-07.md`

## Prerequisites

- INT-04 (EF Core Migrations) — database must be operational before adding new entities or running queries.

## Deliverables

### 1. StockMovement Log Entity (INV-08 Enhancement)

**Source:** INV-08 velocity classification audit finding.

Currently, velocity classification uses lifetime batch totals to approximate daily sales. This gives imprecise stockout estimates. Add a `StockMovement` entity to `MerchSys.Inventory/Entities/` that logs individual stock changes (sale, receipt, shrinkage, return) with timestamps, enabling time-windowed velocity queries.

- Create `StockMovement` entity with: `Id`, `ProductId`, `MovementType` (enum: Sale, Receipt, Shrinkage, Return), `Quantity`, `OccurredAt`, audit columns
- Add `DbSet(Of StockMovement)` to `InventoryDbContext`
- Update `IVelocityService` to optionally use time-windowed data when `StockMovement` records exist
- Generate and apply an EF Core migration for the new table

### 2. ISessionService for User Context (POS-10 Enhancement)

**Source:** POS-10 credit management audit finding.

`CreditManagementViewModel` hardcodes `"Manager"` as the `receivedBy` parameter in `RecordPaymentAsync`. Create an `ISessionService` interface in `MerchSys.SharedKernel/Interfaces/` and a concrete implementation that provides the current user's identity.

- `ISessionService.CurrentUsername As String`
- `ISessionService.CurrentRole As UserRole`
- Register in DI (INT-01 would need amendment, or register here)
- Update `CreditManagementViewModel` to inject and use `ISessionService`

### 3. HasReturns Pre-Computation (POS-11 Investigation)

**Source:** POS-11 transaction history audit finding.

The `TransactionHistoryViewModel` determines `HasReturns` by searching returns scoped to `DateFrom` → `DateTime.Now`, which may miss returns processed for old transactions outside the search window. Investigate and implement one of:

- (A) Add a `HasReturns` boolean column to `Pos_Transactions` and update it when a return is recorded
- (B) Change the return query to search by `OriginalTransactionId` regardless of date range

Document the chosen approach and rationale in the progress summary.

### 4. Stale Checkbox Cleanup

**Source:** Purchasing and Inventory audit findings.

Update the following progress summaries to mark stale `[ ]` items as `[x]` with a note that the referenced plans are now complete:

- `Progress/VISTA_Modules/Purchasing/PUR-02-summary.md` — PUR-03 item
- `Progress/VISTA_Modules/Purchasing/PUR-03-summary.md` — PUR-04, PUR-05 items
- `Progress/VISTA_Modules/Inventory/INV-02-summary.md` — INV-03 item

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` — **0 errors, 0 warnings**
2. `StockMovement` entity exists and has a corresponding migration
3. `ISessionService` is defined and used in `CreditManagementViewModel`
4. `HasReturns` logic is corrected with documented rationale
5. Stale checkboxes in progress summaries are updated

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-05-summary.md`.
