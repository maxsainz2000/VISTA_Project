---
created: 2026-05-17
source: Pending_Tasks audit reports (2026-05-17, 2026-05-26, 2026-06-01)
last-synced: 2026-06-01
item-18-added: 2026-05-27
items-19-to-27-added: 2026-05-27
infra-19-completed: 2026-05-26
infra-20-completed: 2026-05-26
infra-23-completed: 2026-05-28 (EF Core MariaDB provider evaluation and spike)
infra-24-completed: 2026-05-28 (MariaDB schema bootstrap)
infra-25-completed: 2026-05-28 (DbContext conversion to MariaDB)
infra-26-completed: 2026-05-28 (optimistic concurrency + pessimistic FIFO locks)
infra-27-completed: 2026-05-28 (sync layer decommission — SQLite fully removed)
infra-28-completed: 2026-05-28 (connection status indicator + DisableOnOfflineBehavior)
infra-29-completed: 2026-05-28 (operational runbook + nightly backup scripts)
infra-30-completed: 2026-05-28 (Master-Detail Activity Rail sidebar)
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
item-18-promoted: 2026-05-27
item-20-completed: 2026-05-28 (INFRA-29 nightly backup runbook + PowerShell script)
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

**Status:** CLOSED — completed natively in central initial schema migration 0001 (triggers `tr_acc_tamper_no_update` and `tr_acc_tamper_no_delete` are active on `Acc_TamperAuditLog`).
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

**Status:** Checked clean 2026-05-27 — no new violations in code merged 2026-05-24 → 2026-05-27 (INFRA-19, INFRA-20, INFRA-21, ACC-19, ACC-20, POS-19). Checked clean again 2026-05-28 — INFRA-23 through INFRA-30 added no new Rule 14 violations. Remains contingent — re-check after the next merge window.
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

---

## 18. Stock Valuation, COGS Accuracy, and Duplicate Revenue Record Bugs

**Module:** Inventory / Accounting / POS (cross-cutting)
**Source:** User Observation (2026-05-27) — hands-on testing with multi-vendor purchasing
**Status:** CLOSED — completed via INV-15, ACC-21, and ACC-22. Fully verified under Test 7 of the Manager checklist (split-batch POS checkout and FIFO COGS).
- **18a → INV-15** `Plans/VISTA_Modules/Inventory/15-stock-dashboard-cost-column.md` (UI cost visibility — completed)
- **18c → ACC-22** `Plans/VISTA_Modules/Accounting/22-revenue-record-consolidation.md` (eliminate duplicate-writer race — completed)
- **18b → ACC-21** `Plans/VISTA_Modules/Accounting/21-per-batch-cogs-accuracy.md` (per-batch FIFO COGS via new `Inv_SaleCogs` ledger — completed)

### Reproduction Steps

1. **Vendor Product Catalog tab:** Added "Ammonium Sulfate" (ProductId=3) to all three vendors with different prices:
   - AgriChem Supplies (VendorId=1): ₱1,100/unit
   - FarmFresh Seed Corp. (VendorId=2): ₱1,000/unit
   - Golden Feeds Trading (VendorId=3): ₱1,200/unit
2. **Purchase Orders tab:** Created 3 POs (PO-2026-0001 to PO-2026-0003), one per vendor, each ordering 10 units.
3. **Goods Receiving tab:** Confirmed all 3 POs → 3 `StockBatch` rows created (Batch 1 = ₱1,200, Batch 2 = ₱1,100, Batch 3 = ₱1,000). Total stock: 30 units, total cost: ₱33,000.
4. **Stock Dashboard tab:** Stock quantity was correct (30 units), but the "Price" column showed ₱1,100 — the `RetailPrice` from `Inv_Products` set in the Product Management tab, not the actual per-batch purchase cost.
5. **Sales Cart tab:** Sold all 30 units in a single transaction at ₱1,100/unit (the `RetailPrice`). Revenue = ₱33,000.
6. **Expected:** Some profit or at least break-even, since the average purchase cost was ₱1,100/unit (= ₱33,000/30).
7. **Actual:** Two `RevenueRecord` rows were created with conflicting COGS values, and one shows a ₱3,000 *loss*.

### Root Causes (verified via database inspection)

#### 18a. Stock Dashboard "Price" column shows `RetailPrice`, not actual purchase cost

The `StockDashboardService.GetDashboardDataAsync()` correctly computes `StockValue` using FIFO `UnitCost` from `Inv_StockBatches` (line 116: `b.QuantityRemaining * b.UnitCost`), but the `ProductSummaryDto.RetailPrice` field is sourced from `Inv_Products.RetailPrice` — the selling price set in Product Management, **not** the purchase cost. The dashboard's "Price" column therefore doesn't reflect what was actually paid to vendors.

**DB evidence:**
- `Inv_Products` (ProductId=3): `RetailPrice` = 1100
- `Inv_StockBatches`: Batch 1 `UnitCost` = 1200, Batch 2 `UnitCost` = 1100, Batch 3 `UnitCost` = 1000

**Impact:** User cannot tell the actual purchase cost per product from the Stock Dashboard. The `StockValue` sum card is correct (it uses batch `UnitCost`), but the per-row "Price" column is misleading because it shows the retail selling price, not the cost.

**Fix scope:** The dashboard could show a **weighted-average cost** column alongside `RetailPrice`, computed as `SUM(QuantityRemaining × UnitCost) / SUM(QuantityRemaining)` across non-expired batches. Alternatively, show the FIFO cost (oldest batch's `UnitCost`), matching what Accounting uses for COGS.

#### 18b. COGS uses only the FIFO-oldest batch's `UnitCost` for the *entire* sale quantity

`SaleCompletedAccountingHandler.Handle()` calls `GetProductCostQuery` to get the COGS unit cost. The handler (`GetProductCostQueryHandler`) returns the `UnitCost` of the **single** oldest non-expired batch with remaining stock, then multiplies it by the full `item.Quantity`:

```
costResult.FifoUnitCost * item.Quantity   ← line 45, SaleCompletedAccountingHandler.vb
```

This is incorrect when a sale quantity spans multiple FIFO batches at different costs. The `SaleCompletedHandler` in Inventory *correctly* deducts stock across multiple batches via `DeductStockFIFOAsync()` and returns per-batch `FIFODeductionResult.COGS` values, but Accounting **ignores this** and re-queries cost independently using `GetProductCostQuery`, which only returns a single unit cost.

**DB evidence:**
- `Acc_RevenueRecords` (Id=1): `COGS` = 36000, `GrossProfit` = -3000 (used ₱1,200 × 30 = ₱36,000 — the oldest batch price, despite only 10 of those 30 units came from that batch).
- Correct COGS should be: (10 × ₱1,200) + (10 × ₱1,100) + (10 × ₱1,000) = ₱33,000, yielding ₱0 profit (break-even since `RetailPrice` = average cost = ₱1,100).

**Impact:** **Financial statements show phantom losses (or inflated profits, depending on batch price ordering)**. The reported COGS is wrong any time a sale spans multiple batches with different unit costs. This is an accounting accuracy bug — not cosmetic.

**Fix scope:** The Accounting handler should receive the actual per-batch COGS breakdown from the Inventory module's FIFO deduction engine (the `FIFODeductionResult` list from `StockService.DeductStockFIFOAsync`) instead of independently re-querying a single unit cost. Options:
1. Include the per-batch COGS breakdown directly in the `SaleCompletedEvent` (requires the Inventory handler to run first and pass data downstream).
2. Replace `GetProductCostQuery` with a new query that sums COGS across the batches that were *actually consumed* during this specific sale deduction.

#### 18c. Duplicate `RevenueRecord` rows — race between legacy and VAT accounting handlers

Two `Acc_RevenueRecords` exist for the same `(SourceTransactionId=1, ProductId=3)`:
- Record Id=1: `COGS` = 36000, `GrossProfit` = -3000 (created by `SaleCompletedAccountingHandler`)
- Record Id=2: `COGS` = 0, `GrossProfit` = 33000 (created by `SaleCompletedWithVatHandler` in its `Else` branch)

The `SaleCompletedWithVatHandler` is designed to be idempotent: it first queries for an existing `RevenueRecord` matching `(SourceTransactionId, ProductId)` and updates VAT columns if found. However, **the existing record was not found** — the VAT handler's `Else` branch executed, creating a second record. This happened because:
- `SaleCompletedAccountingHandler` was called first and added the record to the EF `DbContext`, but `SaveChangesWithJournalAsync` may not have flushed before `SaleCompletedWithVatHandler.Handle()` ran its `FirstOrDefaultAsync` query on the **same** `DbContext` instance — resulting in the query not finding the record.
- The second record has `COGS = 0` because by the time `GetProductCostQuery` ran for it, all 30 units had already been deducted by the Inventory handler, leaving zero remaining stock.

**DB evidence:**
- `Acc_ExpenseRecords` (Id=4): `Category` = "COGS", `Amount` = 0 (the COGS expense from the second handler)
- Only one `Pos_SalesTransactions` row exists (Id=1), confirming only one sale happened, but two revenue records exist.

**Impact:** Financial reports double-count revenue, and one of the two records has zero COGS. The duplicate must be prevented.

**Fix scope:** The `SaleCompletedAccountingHandler` should call `SaveChangesWithJournalAsync` *before* the VAT handler runs, so the `FirstOrDefaultAsync` in the VAT handler sees the committed record. Alternatively, combine both handlers into a single handler, or ensure handler ordering via MediatR pipeline configuration.

### Dependencies
- 18a: Independent — UI/display change only.
- 18b: Requires rethinking the COGS data flow between Inventory's FIFO deduction engine and Accounting's revenue recording. May require changes to `SaleCompletedEvent` or a new cross-module query.
- 18c: Requires fixing MediatR handler ordering or merging the two accounting handlers. Related to the dual-event design from POS-14.

### Source Document Evidence

The bugs documented above violate requirements and promises stated in the original project source documents. The following passages confirm that **per-batch COGS accuracy** is not a nice-to-have — it is a core design commitment.

**system_plan.md (§6.2 — Inventory Module):**
> "FIFO Costing: Oldest batch cost used first when valuing sold items, per client confirmation. **Batch-level purchase records maintained for accurate COGS calculation.**"

**system_plan.md (§9 — Database Design Principles):**
> "FIFO Batch-Level Records: Purchase price recorded at the batch/lot level per product to support accurate FIFO COGS calculation. **Oldest unreserved batch cost consumed first on each sale.**"

**system_plan.md (§10/§11 — Risk Register, Risk P5, rated High/High):**
> "Price Volatility & Costing Error: Retail price not updated after supplier cost change — confirmed ongoing problem — **leading to incorrect COGS and distorted profit margins.**"
>
> Mitigation: "FIFO batch-level costing ensures historical sales are not retroactively affected by new purchase prices."

Bug 18b is the *exact manifestation* of P5's predicted failure mode: when a single product is purchased from multiple vendors at different prices, COGS is calculated using only the oldest batch's `UnitCost` for the entire quantity, producing distorted profit margins.

**Inventory-Module_AcademicPaper.md (§2 — FIFO Costing and Inventory Valuation Methods):**
> "When a sale occurs, the system deducts units from the oldest batch first, **calculating the cost of goods sold based on that batch's unit cost.** This approach ensures that the FIFO assumption is applied precisely at the batch level, producing accurate cost-of-goods-sold figures." — (Kieso et al., 2023)

The Inventory module (`StockService.DeductStockFIFOAsync`) correctly implements this: it walks batches oldest-first and computes per-batch COGS in `FIFODeductionResult`. The bug is that Accounting ignores this result and re-queries a single unit cost via `GetProductCostQuery`.

**Accounting-Module_AcademicPaper.md (§1.5 — Scope, V1 Features):**
> "The cost of goods sold will be calculated using the FIFO (First-In, First-Out) costing method, as confirmed by the client, with costs recorded at the batch and lot level per product at the time of receiving goods. **The oldest unreserved batch cost will be consumed first on each sale** to ensure that reported COGS accurately reflects the cost of goods actually sold."

**Accounting-Module_AcademicPaper.md (§2 — Theme 3, FIFO Costing):**
> "Without disciplined batch-level cost tracking, the FIFO method degrades into an approximation that may not accurately reflect the actual flow of costs through inventory." — (Garcia & Lim, 2023)
>
> "Without precise product-level costing, businesses cannot determine which products generate the highest margins, which are unprofitable, and where pricing adjustments are needed." — (Villanueva & Reyes, 2022)

**Purchasing-Module_AcademicPaper.md (§1.2 — Conceptual Framework):**
> P5 identifies "price volatility that causes COGS distortions" as an independent variable the module must address.

**Conclusion:** Bug 18b (single-batch COGS for multi-batch sales) directly contradicts the documented design intent of batch-level FIFO COGS. Bugs 18a and 18c are consequential — 18a prevents the user from *seeing* the cost discrepancy before a sale, and 18c corrupts the accounting output after a sale. Together, these three bugs undermine the system's stated goal of "producing COGS figures that accurately represent the cost of goods actually sold" and its P5 risk mitigation.

---

## 19. Symmetric Configuration Encryption (DA4)

**Module:** Infrastructure / Security
**Source:** `system_plan.md` Security Requirements (§8 - DA4)
**Description:** Implement symmetric encryption for sensitive configuration values (such as MariaDB passwords and sync server credentials) in the production configuration overlay (`appsettings.Production.json`). Use AES-256 in Galois/Counter Mode (GCM) for encryption/decryption, and ensure the decryption key is securely loaded at runtime from environment variables or an external secure store instead of being hardcoded or placed in standard files.
**Why deferred:** The prototype deployment is single-station and runs on localized, physically secure desktop systems at Villon Farm Supply. Plaintext production configuration files are secure against local attacks via standard Windows OS user-level ACLs, making advanced key management deferred until a multi-workstation or cloud infrastructure is deployed.

---

## 20. Automated Database Nightly Backup

**Status:** COMPLETED (2026-05-28) — delivered by INFRA-29. See `Plans/VISTA_Modules/Infrastructure/runbooks/03-nightly-backup.md` and `runbooks/scripts/backup-mysqldump.ps1`.
**Module:** Infrastructure / Database
**Source:** `system_plan.md` Risk Mitigation Strategies (§11 - Data Loss)
**Description:** Developed and configured a database utility script (PowerShell) that automatically dumps the central XAMPP MariaDB database (`merchsys_central`) nightly, compresses the output, and retains 7 daily / 4 weekly / 6 monthly copies. A Windows Task Scheduler XML (`vista-nightly-backup.xml`) automates execution at 02:00 daily even without a logged-in user.
**Why deferred:** Was deferred pending a formal deployment; now addressed by INFRA-29 as part of the MariaDB client-server architecture rollout.
**Depends on:** INFRA-29 (completed).

---

## 21. Merchandising-Format Balance Sheet & Cash Flow Statement

**Module:** Accounting & Financial Reporting
**Source:** `Accounting-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Implement the formal merchandising Balance Sheet (detailing Assets at FIFO merchandise cost, Accounts Payable liabilities, and Owner's Equity) and a cash-basis Cash Flow Statement to track actual cash movements. The Cash Flow Statement is critical for anticipating Lanao del Norte's local market cycles where credit sales delay cash inflows while supplier invoices fall due.
**Why deferred:** Deferred to V2 scope per the academic study constraints. The V1 scope was deliberately bounded to the P&L statement, Sales summaries, and VAT reporting dashboards to match the manager's immediate financial literacy constraints.

---

## 22. General Ledger and Integration Journals (Sales, AR Aging, AP Aging)

**Module:** Accounting & Financial Reporting
**Source:** `Accounting-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Implement formal accounting ledger structures, including the master General Ledger, Sales Journal, Accounts Receivable (AR) Aging reports (categorizing outstanding customer credit by 30/60/90 days), and Accounts Payable (AP) Aging reports.
**Why deferred:** Highly visual dashboard cards (e.g. outstanding AP alerts, overdue AR collection flags) provide immediate management utility. Comprehensive multi-row ledger grids are deferred to the V2 auditing phase.

---

## 23. Barcode & RFID Scanning Integration

**Module:** Inventory Management
**Source:** `Inventory-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Integrate hardware barcode and RFID scanners (USB keyboard wedge or Bluetooth HID profiles) into the POS transaction cart and the Purchasing goods receiving views. Scanning product codes should automatically fetch matching products, check stock, and append/receive quantities.
**Why deferred:** Villon Farm Supply manages a compact inventory mix of approximately 50 distinct SKUs, making keyboard search and dropdown selection highly efficient. Physical hardware integration is deferred to V2 when transaction velocity justifies the hardware expense.

---

## 24. Direct Electronic Fund Transfer Gateway Integration

**Module:** Point of Sale
**Source:** `POS-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Wire live API integrations for digital payment processors (such as Maya, GCash Merchant APIs, or PayMongo) to process electronic transactions.
**Why deferred:** The POS currently records GCash and bank transfer reference codes manually for bookkeeping reconciliation. Live fund transfers are deferred until a formal merchant account and steady internet connections are established at the physical store.

---

## 25. Machine Learning / AI-Based Seasonal Demand Forecasting

**Module:** Purchasing / Procurement
**Source:** `Purchasing-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Replace simple statistical multipliers and moving averages with formal machine learning time-series forecasting (e.g., ARIMA or ML.NET prediction pipelines) to proactively model palay planting and growing season demand peaks.
**Why deferred:** The store's limited historical transaction volume in pre-live stages makes complex ML models highly prone to overfitting. The implemented `ReorderService.vb` uses a deterministic `SeasonalMultiplier` and `MinimumThreshold` trigger which is more stable.

---

## 26. Comprehensive System-Wide Audit Trail

**Module:** Infrastructure / Security
**Source:** `system_plan.md` Security Requirements (§8 - DA10) / `Accounting-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Implement a unified, tamper-proof system audit log that tracks all database transactions. The log must record "who" (user account and role), "what" (inserted, modified, or soft-deleted fields), and "when" (UTC timestamp) for all changes, specifically securing financial and inventory tables for BIR audit compliance.
**Why deferred:** The application currently relies on discrete `modified_by` and `modified_at` entity properties. A unified database-wide tamper-proof audit journal is deferred to V2.

---

## 27. Mobile & Web Deployment Platforms

**Module:** Infrastructure / Operations
**Source:** `Inventory-Module_AcademicPaper.md` / `POS-Module_AcademicPaper.md` / `Purchasing-Module_AcademicPaper.md` Scope and Delimitation (§1.5)
**Description:** Migrate the desktop-only WPF application architecture into cross-platform mobile frameworks (such as .NET MAUI or React Native) and web frontends (Next.js) to allow remote access for the Owner and offsite monitoring of store KPIs.
**Why deferred:** The business operates from a single, dedicated local POS workstation with intermittent connectivity. A mobile/web deployment would introduce ongoing hosting fees and internet dependency that are currently outside Villon Farm Supply's operational budget.

