# DATABASE NORMALIZATION & COMPATIBILITY AUDIT REPORT

## Executive Summary

The VISTA application is pivoting from a localized, offline-first SQLite storage model to a centralized, multi-client **MariaDB 11.4.x LTS** server architecture. This report presents a formal, dual-faceted evaluation of:
1. **Third Normal Form (3NF) Alignment**: Mathematical and relational compliance of the centralized MariaDB database schemas, examining normalization, intentional denormalization, and potential redundancies.
2. **WPF Codebase Compatibility**: Direct alignment between the database central schemas and the compiled Entity Framework Core 10 / raw ADO.NET query implementations in the `WPF_Applications` source code.

Our findings reveal that while the database successfully utilizes relational modeling to secure data integrity, there are **latent mathematical denormalizations** (mostly serving historical audit requirements) and **critical structural incompatibilities** that will lead to application crashes at runtime. Most notably, a class inheritance model in VB.NET causes EF Core to expect optimistic concurrency tokens (`RowVersion`) on twenty-two tables that omit this column, ensuring runtime exceptions on any write operations. Furthermore, the receipt archival engine contains raw SQL written in SQLite syntax that will immediately fail when executed against MariaDB.

---

> ## ⚠️ Verification Addendum — 2026-05-29 (claude-code)
>
> This report was independently verified against the live `merchsys_central` database and the
> current `master` source tree. **It was generated against a snapshot that predates commit
> `b082961` (INFRA-31/INFRA-32).** Per-section outcome:
>
> | Section | Verdict |
> |---|---|
> | **§2.1 RowVersion (19 crash sites)** | **Premise accurate, conclusion STALE.** Already resolved by **INFRA-31**, which added `IgnoreNonTokenRowVersionConvention` to `BaseDbContext.ConfigureConventions` — `RowVersion` is now centrally ignored on every non-token entity. **Do NOT implement Step 3.1** (adding 19 `builder.Ignore` calls) — it is obsolete and would regress INFRA-31's single-mechanism design. Runtime proof: the 2026-05-29 `Acc_ExpenseRecords` insert crashed only on `InputVat`, never `RowVersion`. |
> | **§2.2 SQLite syntax in `ReceiptArchivalService`** | **Verified real.** Confirmed at lines 236–258. The MariaDB trigger `tr_pos_receipts_no_delete` is already native (`SIGNAL SQLSTATE '45000'`); the bug is purely application-side. → **INFRA-33 WI-1.** |
> | **§2.3 `GetLatestAuditPerProductAsync` ToListAsync** | **Verified real.** Lines 204–210 match. → **INFRA-33 WI-2.** |
> | **§2.4 MaxLength mismatches** | **Verified real, low severity** (caps below DB capacity, not crashes). → **INFRA-33 WI-3.** |
> | **Part 1 (3NF analysis)** | All cited columns exist; findings are accurate but **descriptive only** — every "violation" is an acknowledged by-design denormalization. No action. |
>
> Remediation for the three live items is planned in
> `Plans/VISTA_Modules/Infrastructure/33-post-pivot-sql-compatibility-remediation.md`.

## Part 1: Third Normal Form (3NF) Compliance Analysis

A database schema is in **Third Normal Form (3NF)** if it is in Second Normal Form (2NF) and contains no transitive dependencies—meaning every non-key column is functionally dependent only on the primary key, the whole primary key, and nothing but the primary key.

Our audit of the MariaDB schema (`0001_initial_schema.sql` and additive migrations) identified three distinct categories of relational structures:

### 1.1 Strict First Normal Form (1NF) Violations
*   **`Pos_OfficialReceipts.Items` & `Pos_OfficialReceiptArchive.Items` (TEXT)**: These columns store a serialized list of transaction lines (representing items, quantities, and prices) as a text blob. In relational database theory, this is a direct violation of **1NF (Column Atomicity)**, as a single field contains non-atomic, composite values.
    *   *System Rationale*: While mathematically a violation, this serves as an immutable historical record of the receipt payload for Bureau of Internal Revenue (BIR) tax audit compliance (NIRC Sec. 237), protecting the receipt structure from cascading changes in transactional data.

### 1.2 Strict Third Normal Form (3NF) Violations: Transitive Dependencies
Several tables carry columns that depend on other non-key columns, establishing transitive relations:
*   **`Pur_AccountsPayable.VendorId`**: Accounts Payable maps to a purchase order via `PurchaseOrderId` (FK). Since `PurchaseOrderId` uniquely identifies a `Pur_PurchaseOrders` row, and each purchase order maps to a single `VendorId`, `VendorId` in `Pur_AccountsPayable` is transitively dependent:
    $$\text{AccountsPayable.Id} \rightarrow \text{PurchaseOrderId} \rightarrow \text{VendorId}$$
*   **`Inv_ShrinkageRecords.ProductId` & `UnitCost`**: Shrinkage records link to a stock batch via `StockBatchId` (FK). Each `StockBatch` is bound to a single `ProductId` and carries a specific `UnitCost`. This creates a transitive dependency:
    $$\text{ShrinkageRecord.Id} \rightarrow \text{StockBatchId} \rightarrow \text{ProductId / UnitCost}$$
*   **`Inv_SaleCogs.ProductId` & `UnitCost`**: This ledger tracks FIFO COGS deductions per batch. Since the batch is specified by `BatchId` (FK to `Inv_StockBatches`), the product and unit cost are transitively determined:
    $$\text{SaleCogs.Id} \rightarrow \text{BatchId} \rightarrow \text{ProductId / UnitCost}$$
*   **`Acc_RevenueRecords.ProductId` & `GrossAmount / NetAmount / VatAmount`**: These rows represent transaction lines. The product details and amounts are transitively dependent on `SourceTransactionId` and the specific transaction line item.

### 1.3 Strict Third Normal Form (3NF) Violations: Derived / Calculated Values
Calculated fields depend functionally on other fields in the same or external tables, introducing relational redundancies:
*   **`Pur_AccountsPayable.Balance` & `IsPaid`**: `Balance` is computed as `TotalAmount - AmountPaid`, and `IsPaid` is a boolean representing `Balance == 0`.
*   **`Inv_ShrinkageRecords.TotalValue`**: Computed as `QuantityLost * UnitCost`.
*   **`Inv_StockAuditRecords.Variance`**: Computed as `PhysicalCount - ExpectedQuantity`.
*   **`Inv_SaleCogs.Cogs`**: Computed as `QuantityDeducted * UnitCost`.
*   **`Pos_CreditAccounts.CurrentBalance`**: Represents the aggregate sum `TotalCreditExtended - TotalPaymentsReceived`.
*   **`Pos_SalesTransactions.SubTotal`, `VatAmount`, `TotalAmount`, `ChangeAmount`**: All are derived arithmetic aggregates of sales lines, VAT rates, and payment inputs.
*   **`Pos_SalesTransactionLines.LineTotal` & `Pos_SalesReturns.RefundAmount`**: Derived line totals calculated as `Quantity * UnitPrice` (minus discounts).
*   **`Pos_SalesReturns.RefundAmount`**: Derived refund totals calculated as `QuantityReturned * UnitPrice`.
*   **`Acc_RevenueRecords.GrossProfit`**: Computed as `NetAmount - COGS`.
*   **`Acc_FinancialPeriods` & `Acc_FinancialSnapshots`**: Entire tables composed of pre-calculated financial aggregate totals (Revenue, COGS, Expenses, AR, AP, Inventory Values).

### 1.4 Legitimate Normalization Deviations
Not all normalization violations are design flaws. The VISTA system intentionally employs two classes of denormalization:
1.  **Temporal Snapshots (Historical Integrity)**: Storing `ProductName`, `VendorName`, or `UnitPrice` directly in line tables (`Pur_PurchaseOrderLines`, `Pur_GoodsReceiptLines`, `Pur_VendorProducts`, `Pos_SalesTransactionLines`, `Pos_SalesReturns`, `Acc_RevenueRecords`). This mathematically violates 3NF (since the ID determines the Name/Price), but practically it freezes historical records. If a product's name or price changes in the master catalog tomorrow, past invoices and tax records remain legally unaltered.
2.  **Performance Cacheing**: Aggregated KPIs in `Pos_CreditAccounts` and Accounting snapshot tables prevent expensive, slow runtime aggregates over hundreds of thousands of ledger rows.

---

## Part 2: WPF Codebase Compatibility Audit

Our audit cross-referenced the central MariaDB schema against the Entity Framework Core 10 mapping configurations (`IEntityTypeConfiguration(Of T)`) and service layers implemented in `WPF_Applications\MerchSys\src`. We identified **four severe compatibility failures**.

### 2.1 Critical Crash Risk: Inherited `RowVersion` Concurrency Column Mismatch

> **✅ RESOLVED by INFRA-31 (2026-05-29).** The analysis below is accurate for the
> pre-INFRA-31 codebase but no longer reflects `master`. `BaseDbContext` now centrally
> ignores `RowVersion` on all non-token entities via a model-finalizing convention. This
> section and Step 3.1 are retained for historical context only — **do not act on them.**

#### The Bug Mechanism
In `MerchSys.SharedKernel.Entities`, the entity hierarchy is structured as follows:
*   `ConcurrencyAwareEntity` declares a public property: `RowVersion As DateTime`.
*   `AuditableEntity` inherits `ConcurrencyAwareEntity`.
*   `SoftDeletableEntity` inherits `AuditableEntity`.

Because EF Core maps all public properties by convention, **every class inheriting from `AuditableEntity` or `SoftDeletableEntity` is expected by the ORM to have a corresponding `RowVersion` column** in its database table.
If a table lacks this column and the EF configuration does not explicitly ignore the property using `builder.Ignore(Function(e) e.RowVersion)`, EF Core will generate SQL queries that include the `RowVersion` column in every `INSERT` or `UPDATE` operation, throwing a fatal runtime crash:
```
MySqlException: Unknown column 'RowVersion' in 'field list'
```

#### Affected Entities (Latent Crash Sites)
Our audit confirmed that the database tables for the following entities **do not** have a `RowVersion` column, yet their EF configurations **do not** ignore it. Any write operation on these entities will crash:

| Module | Entity | DB Table | Inherits From | Status |
| :--- | :--- | :--- | :--- | :--- |
| **Purchasing** | `GoodsReceipt` | `Pur_GoodsReceipts` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `GoodsReceiptLine` | `Pur_GoodsReceiptLines` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `ReorderSuggestion` | `Pur_ReorderSuggestions` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `PriceChangeAlert` | `Pur_PriceChangeAlerts` | `AuditableEntity` | ❌ **CRASH SITE** |
| **Inventory** | `StockMovement` | `Inv_StockMovements` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `ShrinkageRecord` | `Inv_ShrinkageRecords` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `StockAuditRecord` | `Inv_StockAuditRecords` | `AuditableEntity` | ❌ **CRASH SITE** |
| **POS** | `SalesTransactionLine` | `Pos_SalesTransactionLines` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `OfficialReceipt` | `Pos_OfficialReceipts` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `CreditPayment` | `Pos_CreditPayments` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `SalesReturn` | `Pos_SalesReturns` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `ReceiptIntegrity` | `Pos_ReceiptIntegrity` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `OfficialReceiptArchive` | `Pos_OfficialReceiptArchive` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `ReceiptIntegrityArchive` | `Pos_ReceiptIntegrityArchive` | `AuditableEntity` | ❌ **CRASH SITE** |
| **Accounting**| `RevenueRecord` | `Acc_RevenueRecords` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `ExpenseRecord` | `Acc_ExpenseRecords` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `FinancialSnapshot` | `Acc_FinancialSnapshots` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `TamperAuditEntry` | `Acc_TamperAuditLog` | `AuditableEntity` | ❌ **CRASH SITE** |
| | `VatReturnLine` | `Acc_VatReturnLines` | `AuditableEntity` | ❌ **CRASH SITE** |

*Note: In the Purchasing module, the developers successfully patched `PurchaseOrderLineConfiguration` by adding `builder.Ignore(Function(l) l.RowVersion)`, but missed all other append-only and child entities.*

### 2.2 SQLite Syntax Leakage in `ReceiptArchivalService.vb`
`ReceiptArchivalService.vb` performs database cleanup and compliance archiving by executing raw SQL queries via `ExecuteSqlRawAsync`. However, the raw queries are written using SQLite SQL syntax and functions, which will **fail immediately** on MariaDB:

1.  **SQLite Specific `INSERT OR REPLACE` Statement**:
    ```vb
    ' Line 235:
    "INSERT OR REPLACE INTO ""Pos_ArchivalSession"" (key, value, expires_at) " &
    "VALUES ('archival_in_progress', 1, datetime('now', '+5 minutes'))"
    ```
    *   `INSERT OR REPLACE` is invalid in MariaDB. MariaDB requires `REPLACE INTO` or standard `INSERT ... ON DUPLICATE KEY UPDATE` syntax.
    *   `datetime('now', '+5 minutes')` is a SQLite function. MariaDB requires `NOW() + INTERVAL 5 MINUTE`.
2.  **SQLite Identifier Quoting**:
    The queries enclose identifiers in double quotes (e.g. `""Pos_ReceiptIntegrity""`, `""ReceiptId""`, `""Pos_OfficialReceipts""`). In MariaDB, double quotes are interpreted as string literals (unless standard ANSI SQL mode is active), causing database syntax errors. MariaDB expects backticks (`` ` ``) or no quotes.

### 2.3 Latent EF Core 10 VB.NET materialization bug in `InventoryAuditService`
In VB.NET on .NET 10, EF Core 10's Compiled Model/Entity Materializer contains a bug where queries attempting full-entity materialization ending in `.ToListAsync()` silently return an empty list. The developers bypassed this successfully in most repositories by writing raw ADO.NET connection and reader loops (e.g., in `PurchaseOrderService`, `VendorService`, `AccountsPayableService`).

However, we detected a **latent bug** in `InventoryAuditService.vb`:
```vb
' Line 204:
Public Async Function GetLatestAuditPerProductAsync() As Task(Of List(Of StockAuditRecord)) Implements IInventoryAuditService.GetLatestAuditPerProductAsync
    Return Await _db.StockAuditRecords _
        .Include(Function(a) a.Product) _
        .GroupBy(Function(a) a.ProductId) _
        .Select(Function(g) g.OrderByDescending(Function(a) a.AuditedAt).First()) _
        .ToListAsync()
End Function
```
Because this query projects a full entity (`StockAuditRecord` and its included `Product`) directly to `.ToListAsync()`, it will **silently return an empty list at runtime** in a VB.NET build, breaking the stock audit dashboard view.

### 2.4 Length Constraint Mismatches (Validation vs Database Schema)
There are several string length discrepancies where the EF Core model imposes tighter constraints than the MariaDB database tables:

*   **`PurchaseOrder.OrderNumber`**:
    *   EF Configuration (`PurchaseOrderConfiguration.vb`): `.HasMaxLength(20)`
    *   MariaDB Table (`Pur_PurchaseOrders`): `OrderNumber VARCHAR(128)`
    *   *Risk*: If the service layer generates a sequential OrderNumber exceeding 20 characters, it will fail at the application/validation level, despite the database table supporting up to 128 characters.
*   **`GoodsReceipt.ReceiptNumber`**:
    *   EF Configuration (`GoodsReceiptConfiguration.vb`): `.HasMaxLength(20)`
    *   MariaDB Table (`Pur_GoodsReceipts`): `ReceiptNumber VARCHAR(128)`
*   **`ReorderSuggestion.ProductName`**:
    *   EF Configuration (`ReorderSuggestionConfiguration.vb`): `.HasMaxLength(200)`
    *   MariaDB Table (`Pur_ReorderSuggestions`): `ProductName VARCHAR(255)`

---

## Part 3: Actionable Resolution Plan

To achieve **100% compatibility** against the centralized MariaDB database central schema and maintain correct transactional processing, the following remediations must be implemented:

### Step 3.1: Add `RowVersion` Ignores to Child & Append-only Configurations

> **❌ OBSOLETE — DO NOT IMPLEMENT.** Superseded by INFRA-31's centralized convention.
> Adding 19 per-config `builder.Ignore` calls would duplicate and regress that mechanism.

For all entity configurations mapping to tables without a `RowVersion` column, add:
```vb
builder.Ignore(Function(e) e.RowVersion)
```
Specifically, this edit must be made to:
1.  `GoodsReceiptConfiguration.vb`
2.  `GoodsReceiptLineConfiguration.vb`
3.  `ReorderSuggestionConfiguration.vb`
4.  `PriceChangeAlertConfiguration.vb`
5.  `StockMovementConfiguration.vb`
6.  `ShrinkageRecordConfiguration.vb`
7.  `StockAuditRecordConfiguration.vb`
8.  `SalesTransactionLineConfiguration.vb`
9.  `OfficialReceiptConfiguration.vb`
10. `CreditPaymentConfiguration.vb`
11. `SalesReturnConfiguration.vb`
12. `ReceiptIntegrityConfiguration.vb`
13. `OfficialReceiptArchiveConfiguration.vb`
14. `ReceiptIntegrityArchiveConfiguration.vb`
15. `RevenueRecordConfiguration.vb`
16. `ExpenseRecordConfiguration.vb`
17. `FinancialSnapshotConfiguration.vb`
18. `TamperAuditEntryConfiguration.vb`
19. `VatReturnLineConfiguration` (inside `VatReturnMap.vb`)

### Step 3.2: Refactor `ReceiptArchivalService.vb` SQL Dialect
Rewrite the raw SQL commands in `ReceiptArchivalService.vb` to target MariaDB syntax:

1.  **Replace Session Flag Write**:
    ```vb
    ' Refactored for MariaDB syntax (REPLACE and NOW() + INTERVAL):
    Await db.Database.ExecuteSqlRawAsync(
        "REPLACE INTO `Pos_ArchivalSession` (`key`, `value`, `expires_at`) " &
        "VALUES ('archival_in_progress', 1, DATE_ADD(NOW(6), INTERVAL 5 MINUTE))",
        cancellationToken)
    ```
2.  **Replace Table and Column Deletions**:
    ```vb
    ' Refactored backtick quotes:
    Await db.Database.ExecuteSqlRawAsync(
        "DELETE FROM `Pos_ReceiptIntegrity` WHERE `ReceiptId` = {0}",
        {item.Receipt.Id},
        cancellationToken)

    Await db.Database.ExecuteSqlRawAsync(
        "DELETE FROM `Pos_OfficialReceipts` WHERE `Id` = {0}",
        {item.Receipt.Id},
        cancellationToken)
    ```

### Step 3.3: Refactor `InventoryAuditService.GetLatestAuditPerProductAsync` Query
Bypass the VB.NET EF Core compiled materializer bug in `GetLatestAuditPerProductAsync` by rewriting it to use a direct raw `MySqlConnection` reader loop, matching the pattern successfully implemented in other services:
```vb
Public Async Function GetLatestAuditPerProductAsync() As Task(Of List(Of StockAuditRecord)) Implements IInventoryAuditService.GetLatestAuditPerProductAsync
    Dim auditList As New List(Of StockAuditRecord)()
    Dim connStr = _db.Database.GetConnectionString()
    Using conn As New MySqlConnection(connStr)
        Await conn.OpenAsync()
        Using cmd = conn.CreateCommand()
            cmd.CommandText = "SELECT a.Id, a.ProductId, a.ExpectedQuantity, a.PhysicalCount, a.Variance, " &
                              "a.Reason, a.Notes, a.PerformedBy, a.AuditedAt, a.CreatedBy, a.CreatedAt, " &
                              "a.ModifiedBy, a.ModifiedAt, p.Id, p.Name, p.Sku, p.CategoryId, p.RetailPrice, p.Unit " &
                              "FROM Inv_StockAuditRecords a " &
                              "INNER JOIN Inv_Products p ON a.ProductId = p.Id " &
                              "WHERE a.Id IN (SELECT MAX(Id) FROM Inv_StockAuditRecords GROUP BY ProductId)"
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim rec As New StockAuditRecord With {
                        .Id = reader.GetInt32(0),
                        .ProductId = reader.GetInt32(1),
                        .ExpectedQuantity = reader.GetInt32(2),
                        .PhysicalCount = reader.GetInt32(3),
                        .Variance = reader.GetInt32(4),
                        .Reason = reader.GetString(5),
                        .Notes = If(reader.IsDBNull(6), Nothing, reader.GetString(6)),
                        .PerformedBy = reader.GetString(7),
                        .AuditedAt = reader.GetDateTime(8),
                        .CreatedBy = reader.GetString(9),
                        .CreatedAt = reader.GetDateTime(10),
                        .ModifiedBy = If(reader.IsDBNull(11), Nothing, reader.GetString(11)),
                        .ModifiedAt = If(reader.IsDBNull(12), Nothing, CType(reader.GetDateTime(12), DateTime?))
                    }
                    Dim prod As New Product With {
                        .Id = reader.GetInt32(13),
                        .Name = reader.GetString(14),
                        .Sku = reader.GetString(15),
                        .CategoryId = reader.GetInt32(16),
                        .RetailPrice = reader.GetDecimal(17),
                        .Unit = If(reader.IsDBNull(18), Nothing, reader.GetString(18))
                    }
                    rec.Product = prod
                    auditList.Add(rec)
                End While
            End Using
        End Using
    End Using
    Return auditList
End Function
```

### Step 3.4: Align MaxLength Constraints in EF Configurations
Update EF model settings to mirror the maximum field capacity of the DB:
1.  `PurchaseOrderConfiguration.vb`:
    Change: `po.OrderNumber).IsRequired().HasMaxLength(20)`
    To: `po.OrderNumber).IsRequired().HasMaxLength(128)`
2.  `GoodsReceiptConfiguration.vb`:
    Change: `gr.ReceiptNumber).IsRequired().HasMaxLength(20)`
    To: `gr.ReceiptNumber).IsRequired().HasMaxLength(128)`
3.  `ReorderSuggestionConfiguration.vb`:
    Change: `s.ProductName).IsRequired().HasMaxLength(200)`
    To: `s.ProductName).IsRequired().HasMaxLength(255)`
