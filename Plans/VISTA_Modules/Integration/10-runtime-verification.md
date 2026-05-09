---
module: Integration
plan-id: INT-10
title: "Runtime Verification & Smoke Testing"
depends-on: [INT-09]
estimated-files: 0
---

# Runtime Verification & Smoke Testing

## Context

All Integration plans (INT-01 → INT-09) are code-complete, and the solution compiles with 0 errors and 0 warnings. However, **no interactive runtime verification** has been performed. Every progress summary from INT-06 onward deferred runtime testing items to a future session because no GUI test harness exists.

This plan consolidates the 6 genuine runtime-dependent items from INT-06, INT-07, and INT-08 into a single supervised testing session.

**Audit sources:** `Pending_Tasks/Integration-audit-2026-05-09.md` (Pending Tasks section), `Progress/VISTA_Modules/Integration/INT-06-summary.md`, `INT-07-summary.md`, `INT-08-summary.md`

## Prerequisites

- INT-09 (IInventoryAuditService) — `IInventoryAuditService` must exist and be registered before all 16 views can be navigated without exceptions.
- INT-08 (StockMovement Writes) — completed. Movement records are now written on every stock mutation.
- INT-07 (DI Registration Gaps) — completed. All services except `IInventoryAuditService` are registered (covered by INT-09).
- Application must launch via `dotnet run` or Visual Studio debugger.

## Deliverables

### 1. Full Navigation Smoke Test (All 16 Views)

Launch the application and navigate to each of the 16 registered views. Verify:

- No `InvalidOperationException` on navigation (DI resolution succeeds)
- Each view renders its bound ViewModel data (or empty-state placeholder if no seed data)
- Navigation sidebar correctly groups views by module

| Module | Views to Verify |
|--------|----------------|
| POS | SalesCartView, CreditManagementView, TransactionHistoryView, DailySummaryView |
| Purchasing | PurchaseOrderListView, GoodsReceivingView, VendorDirectoryView, APLedgerView, ReorderSuggestionsView |
| Inventory | StockDashboardView, ProductManagementView, ExpiryMonitorView, ShrinkageView |
| Accounting | FinancialOverviewView, IncomeStatementView, SalesSummaryView |

**Source:** INT-06 (line 168), INT-07 (line 42)

### 2. Cross-Module Event Flow Runtime Verification

Execute at least one end-to-end cross-module event chain at runtime and verify the full pipeline:

**Option A — GoodsReceived chain:**
1. Create a Purchase Order
2. Receive goods against the PO
3. Verify `GoodsReceivedHandler` fires → `StockService.AddStockBatchAsync` creates a `StockBatch`
4. Verify `GoodsReceivedAccountingHandler` fires → `ExpenseRecord` created (category=Purchase)
5. Verify a `StockMovement` record (type=Receipt) appears in `Inv_StockMovements`

**Option B — SaleCompleted chain:**
1. Add items to cart and complete a sale
2. Verify `SaleCompletedHandler` fires → `StockService.DeductStockFIFOAsync` runs FIFO deduction
3. Verify `SaleCompletedAccountingHandler` fires → `RevenueRecord` + COGS `ExpenseRecord` created
4. Verify a `StockMovement` record (type=Sale) appears in `Inv_StockMovements`

**Source:** INT-06 (line 169), INT-07 (line 43)

### 3. StockMovement Record Verification

After performing the operations in Deliverable 2, query the `Inv_StockMovements` table directly to verify:

- Records exist with correct `MovementType` values (`Receipt`, `Sale`, `Shrinkage`, `Return`)
- `Quantity` sign convention is correct (positive for inbound, negative for outbound)
- `OccurredAt` timestamps are populated
- Audit columns (`CreatedBy`, `CreatedAt`) are populated

**Source:** INT-08 (line 52)

### 4. VelocityService Classification Verification

After accumulating movement data from Deliverables 2–3:

- Query `VelocityService.ClassifyAllProductsAsync` or `GetVelocityForProductAsync`
- Verify that velocity classifications use time-windowed `StockMovement` data rather than the batch-total fallback
- Confirm the classification shifts correctly (e.g., a product with recent sales shows higher velocity)

**Source:** INT-08 (line 53)

## Implementation Notes

- This plan requires a **supervised runtime session** — either via Visual Studio debugger or `dotnet run` with manual UI interaction.
- If VS MCP `debugger_launch_without_debugging` continues to fail (as observed in INT-06), use `dotnet run --project WPF_Applications/MerchSys/src/MerchSys.App` from PowerShell.
- Database queries can be performed via SQLite CLI (`sqlite3 merchsys.db`) or via the application's own DbContext.
- Consider using VS debugger breakpoints in event handlers to confirm they fire during runtime chains.

## Acceptance Criteria

1. All 16 views navigable without runtime exceptions
2. At least one cross-module event chain verified end-to-end at runtime
3. `Inv_StockMovements` table contains records after stock operations
4. `VelocityService` returns classifications based on movement data (not fallback)
5. All findings documented in the progress summary

## Output Requirements

Create progress report at `Progress/VISTA_Modules/Integration/INT-10-summary.md`.
