---
module: Integration
plan-id: INT-03
title: "Cross-Module Contracts & Handlers"
depends-on: [INT-01]
estimated-files: 12
---

# Cross-Module Contracts & Handlers

## Context

The modular monolith communicates across module boundaries exclusively via MediatR events and queries. During module implementation, several handlers and query contracts were identified as needed but deferred because they required types or services from other modules. Now that all modules are implemented, these cross-module connections can be wired.

This plan adds the missing SharedKernel contracts and implements the handlers that make the modules work together at runtime.

**Audit sources:** `Pending_Tasks/POS-audit-2026-05-07.md`, `Pending_Tasks/Inventory-audit-2026-05-07.md`, `Pending_Tasks/Accounting-audit-2026-05-07.md`

## Prerequisites

- INT-01 (App Composition Root) — all services must be DI-registered so handlers can inject them.
- All module plans (PUR-01–13, INV-01–13, POS-01–12, ACC-01–09) — completed.

## Wiki References

- `LLM_Wiki/wiki/analysis/cross-module-data-flow.md` — full event/query dependency map
- `LLM_Wiki/wiki/concepts/modular-monolith.md` — cross-module communication rules
- `LLM_Wiki/codebase_wiki/index.md` — current codebase structure

## Deliverables

### 1. New SharedKernel Query Contracts

Create the following MediatR `IRequest<T>` contracts in `MerchSys.SharedKernel/Queries/` (verify each does not already exist before creating):

| Contract | Response Type | Purpose | Consumer |
|----------|--------------|---------|----------|
| `GetProductCatalogQuery` | `List(Of ProductCatalogItem)` | Product search for POS cart | POS-09 `SalesCartViewModel` |
| `GetProductCostQuery` | `ProductCostResult` | FIFO unit cost at sale time | ACC (for COGS accuracy) |
| `GetTotalARQuery` | `Decimal` | Total outstanding accounts receivable | ACC-03 `FinancialOverviewService` |
| `GetTotalAPQuery` | `Decimal` | Total outstanding accounts payable | ACC-03 `FinancialOverviewService` |
| `GetLowStockAlertCountQuery` | `Integer` | Active low-stock alert count | ACC-03 `FinancialOverviewService` |

Also verify `StockReturnedEvent` exists in `MerchSys.SharedKernel/Events/`. If not, create it (POS-07 publishes it).

Create corresponding response DTOs in `MerchSys.SharedKernel/Queries/` (e.g., `ProductCatalogItem`, `ProductCostResult`).

### 2. Inventory Module Handlers

Create the following handlers in `MerchSys.Inventory/Handlers/`:

| Handler | Type | Description |
|---------|------|-------------|
| `GetProductCatalogQueryHandler` | `IRequestHandler(Of GetProductCatalogQuery, ...)` | Queries `InventoryDbContext` for products matching search criteria. Returns product name, SKU, current price, and stock level. |
| `StockReturnedEventHandler` | `INotificationHandler(Of StockReturnedEvent)` | Consumes `StockReturnedEvent` published by POS-07 `SalesReturnService`. Adds returned quantity back to stock via `IStockManagementService`. |
| `GetProductCostQueryHandler` | `IRequestHandler(Of GetProductCostQuery, ...)` | Returns the current FIFO unit cost for a given product. Used by Accounting to calculate accurate COGS on `SaleCompletedEvent`. |
| `GetLowStockAlertCountQueryHandler` | `IRequestHandler(Of GetLowStockAlertCountQuery, ...)` | Returns count of active low-stock alerts from `ILowStockAlertService`. |

### 3. POS Module Handler

Create in `MerchSys.POS/Handlers/`:

| Handler | Type | Description |
|---------|------|-------------|
| `GetTotalARQueryHandler` | `IRequestHandler(Of GetTotalARQuery, Decimal)` | Queries `PosDbContext` for total outstanding credit (utang) balance across all customers. |

### 4. Purchasing Module Handler

Create in `MerchSys.Purchasing/Handlers/`:

| Handler | Type | Description |
|---------|------|-------------|
| `GetTotalAPQueryHandler` | `IRequestHandler(Of GetTotalAPQuery, Decimal)` | Queries `PurchasingDbContext` for total outstanding accounts payable across all vendors. |

### 5. Service-Level Wiring

These are modifications to existing service code:

| Change | File | Description |
|--------|------|-------------|
| Wire receipt generation into sale flow | `MerchSys.POS/Services/CartService.vb` | After `FinalizeAsync` completes a sale, call `IReceiptService.GenerateReceiptAsync` so every sale automatically produces a receipt. (POS-06 flagged this.) |
| Wire low-stock alerts into event handlers | `MerchSys.Inventory/Handlers/SaleCompletedInventoryHandler.vb` | After deducting stock on `SaleCompletedEvent`, call `ILowStockAlertService.CheckAndGenerateAlertsAsync` for the affected product. (INV-06 flagged this.) |
| Wire low-stock alerts into shrinkage handler | `MerchSys.Inventory/Handlers/ShrinkageRecordedHandler.vb` | After recording shrinkage on `ShrinkageRecordedEvent`, call `ILowStockAlertService.CheckAndGenerateAlertsAsync`. (INV-06 flagged this.) |
| Update COGS in SaleCompleted accounting handler | `MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` | Send `GetProductCostQuery` via MediatR to resolve FIFO unit cost, then populate `RevenueRecord.COGS` with the actual value instead of `0`. (ACC-02 flagged this.) |

### 6. UI Enforcement

| Change | File | Description |
|--------|------|-------------|
| Disable sale button on credit denial | `MerchSys.POS/ViewModels/SalesCartViewModel.vb` | When payment type is Credit and `CanExtendCreditAsync` returns `False`, disable the "Complete Sale" button. (POS-05 flagged this.) |

## Implementation Notes

- All new handlers must be auto-discovered by MediatR's assembly scanning (configured in INFRA-04). No manual handler registration should be needed.
- Query handler implementations should use the module's own `DbContext` — never access another module's context directly.
- The `GetProductCostQuery` handler should use the existing FIFO costing logic in `IStockManagementService` or the Inventory service layer.
- Verify exact class/interface names against the live codebase before implementing — audit reports may use simplified names.

## Acceptance Criteria

1. `dotnet build MerchSys.slnx` completes with **0 errors, 0 warnings**
2. All SharedKernel query contracts listed above exist
3. All handlers listed above exist and are discoverable by MediatR
4. `RevenueRecord.COGS` is populated with actual FIFO unit cost (not `0`) when a sale is completed
5. `StockReturnedEvent` results in inventory restock
6. `GetProductCatalogQuery` returns product data for the POS cart
7. Financial Overview dashboard shows real values for AR, AP, and low-stock alerts (not zeros)
8. Low-stock alerts fire after both sales and shrinkage events
9. Receipts are auto-generated on sale completion

## Output Requirements

### Implementation Summary
After completing all code, create a progress report at:
```
Progress/VISTA_Modules/Integration/INT-03-summary.md
```
Using the template structure from `Progress/_template.md`. Include:
- All files created/modified with full paths
- List of new SharedKernel contracts added
- List of handlers implemented
- Service modifications made
- Build status confirmation
- Any deviations from this plan
