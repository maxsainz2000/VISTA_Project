---
module: MerchSys.Inventory
agent: antigravity
date: 2026-05-27
plan-ref: Plans/VISTA_Modules/Inventory/14-price-change-history.md
status: completed
---

## Task Summary

Implemented the Product RetailPrice Change History feature — a secure, append-only database transaction history tracking changes to a product's retail price (who changed it, when it changed, previous/current prices, and the change reason), coupled with an interactive read-only popup viewer launched from the main Product Management panel.

**Plan:** `[[14-price-change-history]]`

## What Was Done

- Created `MerchSys.Inventory/Entities/ProductPriceHistory.vb` — Append-only domain model with `ProductId`, `OldPrice`, `NewPrice`, `ChangedAt`, `ChangedBy`, and `Reason`.
- Created `MerchSys.Inventory/Data/Configurations/ProductPriceHistoryConfiguration.vb` — Maps to `Inv_ProductPriceHistory`, applies precision/scale controls, configures composite index, and restricts hard deletion.
- Modified `MerchSys.Inventory/Data/InventoryDbContext.vb` — Registered new `DbSet(Of ProductPriceHistory)`.
- Created manual migration baseline `MerchSys.Inventory/Migrations/20260527100000_AddProductPriceHistory.vb`.
- Modified `MerchSys.App/Data/DatabaseInitializer.vb` — Registered the baseline migration and wrote the idempotent ADO.NET SQL migration script to apply the schema (table and index) automatically on app startup.
- Created `MerchSys.Inventory/ViewModels/ProductPriceHistoryViewModel.vb` — Popup viewer ViewModel exposing list loading via explicit DTO projection (`PriceHistoryRowItem`) to circumvent the VB.NET full-entity query silent empty bug.
- Modified `MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb` — Injected `ISessionService` to fetch user credentials, added change reason binding `EditorPriceChangeReason`, and transactionally wrote price histories inside `SaveProductAsync` if prices changed.
- Modified `MerchSys.App/Application.xaml.vb` — Registered the view model and popup window view in the startup DI container.
- Modified `MerchSys.App/Views/Inventory/ProductManagementView.xaml` — Wired the "Price History" button next to existing toolbar actions and added the "Price Change Reason (optional)" textbox in the product edit card overlay.
- Modified `MerchSys.App/Views/Inventory/ProductManagementView.xaml.vb` — Injected `IServiceProvider` and wrote the button click handler to dynamically instantiate, initialize, and display the history dialog.
- Created `MerchSys.App/Views/Inventory/ProductPriceHistoryView.xaml` & `ProductPriceHistoryView.xaml.vb` — WPF Window popup showcasing a premium DataGrid ledger with colored price deltas (green for increase, red for decrease) and operating logs.

## Architecture Notes

**Database Ledger Schema:**
- In order to prevent rounding errors or standard floating-point discrepancies, price fields (`OldPrice` and `NewPrice`) are stored as `TEXT NOT NULL` inside SQLite database definitions, aligning with all other financial decimal representations in the VISTA system.
- Hard delete is guarded by `DeleteBehavior.Restrict` in Entity Framework Core, meaning a product with active historical price updates cannot be hard-deleted.

**MVVM & UI Integration:**
- The history window uses WPF `DataTrigger`s mapped to `IsIncrease` and `IsDecrease` Boolean properties inside `PriceHistoryRowItem` to color-code delta results.
- `ProductPriceHistoryViewModel` queries and maps inputs asynchronously inside `LoadHistoryAsync()`. It selects directly into DTOs before running `.ToListAsync()`, complying with the VB.NET VB-trap guidelines.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | ✅ |

## Issues Encountered

None. Solution compiles cleanly with exactly 0 errors and 0 warnings.

## What's Next

- INV-15 or other future Inventory / POS module integrations.

## Cross-References

- Domain Wiki pages consulted: `[[modular-monolith]]`, `[[bir-compliance]]`
- Agent Wiki entries consulted: `[[efcore10-vbnet-migration-discovery-bug]]`
