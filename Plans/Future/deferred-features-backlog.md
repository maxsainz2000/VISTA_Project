---
created: 2026-05-17
source: Pending_Tasks audit reports (2026-05-17, 2026-05-26)
last-synced: 2026-05-27
infra-19-completed: 2026-05-26
infra-20-completed: 2026-05-26
item-2-completed: 2026-05-26
item-3-completed: 2026-05-26
item-9-completed: 2026-05-27
item-10-closed: 2026-05-27 (out-of-scope, single-branch)
item-11-mooted: 2026-05-26
item-12-completed: 2026-05-26
item-13-completed: 2026-05-26
item-14-checked-clean: 2026-05-27
item-16-promoted: 2026-05-27
item-17-promoted: 2026-05-27
---

# Deferred Features Backlog

> These features were identified during the 2026-05-17 module audit as explicitly deferred.
> They are **not** in scope for any current plan. When the time comes, create a proper plan file in `Plans/VISTA_Modules/<module>/` for each one.

---

## 4. IDbContextFactory Registration for Harnesses

**Module:** Accounting / Infrastructure
**Source:** ACC-13 What's Next
**Description:** Consider adding `IDbContextFactory(Of AccountingDbContext)` registration to `DatabaseConfig.AddModuleDbContexts` so that future verification harnesses can use factory-based multi-instance patterns instead of the single-instance `DbContext`.
**Why deferred:** The current harness (ACC-13) works fine with the existing pattern. This is only needed if future harnesses require concurrent database access.
**Depends on:** ACC-13 (VAT Ledger Schema Verification — completed).

---

## 5. MariaDB Immutability Triggers for Acc_TamperAuditLog

**Module:** Accounting / Infrastructure
**Source:** ACC-15 What's Next
**Description:** The `Acc_TamperAuditLog` table has SQLite immutability triggers (deployed by ACC-15), but no MariaDB equivalents for the central replica. This is a SQL-only task similar to INFRA-08 (which added MariaDB triggers for POS tables, but not Acc_* tables).
**Why deferred:** The central MariaDB deployment is not yet live, and a formal DB admin account hasn't been established yet.
**Depends on:** ACC-15 (completed), INFRA-08 (completed), DB admin account (not yet created).

---

## 8. MariaDB Trigger DEFINER Fix

**Module:** Infrastructure
**Source:** INFRA-08 What's Next
**Description:** The INFRA-06 immutability triggers use the anonymous default DEFINER. Once a formal DB admin account is established, these triggers should be recreated with `DEFINER = <admin_account>` to follow MariaDB security best practices.
**Why deferred:** No formal DB admin account exists yet. The triggers work correctly with the current DEFINER.
**Depends on:** INFRA-06 (completed), INFRA-08 (completed), DB admin account (not yet created).

---

## 9. ISyncableRepository Write-Path Migration — Accounting Handlers

**Status:** CLOSED — completed by INFRA-21.
**Module:** Infrastructure
**Source:** INFRA-13 What's Next
**Description:** Migrate `Accounting/Handlers` write paths to use `ISyncableRepository` so that writes from accounting event handlers are captured in the `Sync_Journal` for central replication.
**Scope decision (2026-05-27):** Approved for migration. Six in-scope handlers (`SaleCompletedAccountingHandler`, `GoodsReceivedAccountingHandler`, `CreditPaymentAccountingHandler`, `ShrinkageAccountingHandler`, `SaleCompletedWithVatHandler`, `GoodsReceivedWithVatHandler`); `ReceiptTamperDetectedHandler` excluded (writes `<NoSync>` `Acc_TamperAuditLog`). Rationale: sibling Accounting services already journal, so leaving handlers un-migrated produces a half-mirrored central DB that silently misleads BIR audits and any Owner read from a recovery machine. Handlers are insert-only (and idempotent UPDATE on the two VAT variants), so conflict surface is near-zero.
**Depends on:** INFRA-13 (completed).

---

## 10. ISyncableRepository Write-Path Migration — ProductManagementViewModel

**Status:** CLOSED (2026-05-27) — out-of-scope for single-branch deployment.
**Module:** Infrastructure / Inventory
**Source:** INFRA-13 What's Next
**Description:** Migrate `Inventory/ViewModels/ProductManagementViewModel.vb` write paths to use `ISyncableRepository` so that product edits are captured in the `Sync_Journal`.
**Scope decision (2026-05-27):** Closed without migration. Rationale:
1. Villon Farm Supply is single-location (`LLM_Wiki/wiki/entities/villon-farm-supply.md`); there is no second branch needing the same catalog.
2. Product CRUD is a Manager-only function performed on a single terminal — no second writer to converge.
3. Owner is read-only and never edits products. Financial reports denormalize `ProductName` into `RevenueRecord` at write time (`SaleCompletedAccountingHandler.vb:50`), so report rendering does not depend on a replicated product master.
4. Product operations are UPDATE-heavy (price, `IsActive`, soft-delete), so syncing would introduce real multi-master conflict liability for no current use case.
5. YAGNI — if multi-branch is ever planned, it requires a far larger redesign than product-table sync; defer to that hypothetical plan rather than pre-paying the cost now.
**Depends on:** INFRA-13 (completed). Reopen only if a multi-branch deployment is approved.

---

## 14. INT-17b — Contingent Rule 14 Follow-Up

**Status:** Checked clean 2026-05-27 — no new violations in code merged 2026-05-24 → 2026-05-27 (INFRA-19, INFRA-20, INFRA-21, ACC-19, ACC-20, POS-19). Remains contingent — re-check after the next merge window.
**Module:** Integration
**Source:** INT-17 What's Next
**Description:** If new Rule 14 true positives surface in code merged after 2026-05-24, open a follow-up plan INT-17b to rename them. This is a contingent item — only actionable if new violations appear.
**Why deferred:** No new violations have been detected. This item exists as a reminder to check after future code merges.
**Depends on:** INT-17 (completed).

---

## 15. ESC/POS Thermal Receipt Renderer

**Module:** POS
**Source:** POS-19 scope decision (2026-05-26)
**Description:** Add an `EscPosThermalRenderer` implementation of `IReceiptRenderer` that emits raw ESC/POS bytes through the Windows print spooler to a configured thermal printer (80mm ≈ 48 chars, 58mm ≈ 32 chars). Includes:
- ESC/POS init / encoding / paper feed / partial cut opcodes
- Width-limited line wrapping (`ReceiptWidthFormatter`)
- "₱" → "PHP " substitution for ASCII-only code pages (`ReceiptCurrencyTransform`)
- P/Invoke wrapper (`RawPrinterHelper`) for `OpenPrinter`/`WritePrinter`/`ClosePrinter`
- Configuration under `POS:Receipt:Thermal` (printer name, paper width, code page, auto-cut)
- Add `Thermal` value to `ReceiptRenderTarget` enum and wire selection in `PosServiceRegistration`
**Why deferred:** No physical thermal printer is available at Villon Farm Supply for hardware verification. Shipping ESC/POS bytes unverified risks deploy-time bugs (wrong opcodes, code-page mismatches, P/Invoke handle leaks) that cannot be caught at build time. POS-19 ships the `IReceiptRenderer` abstraction and a PDF renderer; thermal slots in cleanly when a printer arrives.
**Depends on:** POS-19 (in progress).

---

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

## 17. Price Change History Tracking

**Status:** PROMOTED (2026-05-27) — see `Plans/VISTA_Modules/Inventory/14-price-change-history.md` (INV-14). Scope narrowed to retail price only; vendor unit cost history remains deferred (implicit via `StockBatch.UnitCost` + `Pur_PriceChangeAlerts`).
**Module:** Inventory / Purchasing
**Source:** User Question (2026-05-27)
**Description:** Add a formal history log or tracking system for changes to `RetailPrice` and Vendor Unit Costs over time. Currently, `RetailPrice` is a simple mutable field on the `Product` entity, and `UnitCost` is locked into individual `StockBatch` records. 
**Why deferred:** The current system handles Cost of Goods using a FIFO deduction engine out-of-the-box (by looking at individual stock batches), meaning historical cost is inherently preserved per batch. Retail prices are updated ad-hoc without historical tracking. A dedicated price change history table/UI is a nice-to-have but not critical for MVP.
