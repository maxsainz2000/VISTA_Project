---
type: error-fix
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-29
tags: [ef-core, mariadb, vb-net, concurrency, rowversion, inheritance, runtime-error]
error-code: Unknown column 'RowVersion' in 'field list'
severity: runtime-error
---

## Problem

Clicking **Save Draft** in the Purchase Orders tab threw:

```
Microsoft.EntityFrameworkCore.DbUpdateException
  Message: Could not save changes. Please configure your entity type accordingly.
  Source:  MySql.EntityFrameworkCore
Inner Exception 1:
  MySqlException: Unknown column 'RowVersion' in 'field list'
```

Origin: `PurchaseOrderService.CreateDraftAsync` → `_db.SaveChangesAsync()`. The PO
parent row inserted fine; the failure was on the `Pur_PurchaseOrderLines` insert.

## Root Cause

`AuditableEntity` inherits `ConcurrencyAwareEntity`, which declares a public
`RowVersion As DateTime` property. EF Core maps **all** public properties — including
inherited ones — by convention. So every AuditableEntity-derived entity silently
gets a `RowVersion` column expectation.

- `Pur_PurchaseOrders` HAS a `RowVersion TIMESTAMP(6)` column and its config calls
  `IsRowVersion()` → parent insert works.
- `Pur_PurchaseOrderLines` is an **append-only child table** and (correctly, per the
  Migrations README) has **no** `RowVersion` column. But `PurchaseOrderLineConfiguration`
  neither mapped nor ignored the inherited property, so EF emitted SQL referencing a
  column that does not exist → "Unknown column 'RowVersion' in 'field list'".

The non-obvious part: the property is **inherited**, so it never appears in the
entity's own source file or its configuration — easy to miss.

## Fix

Ignore the inherited property in the line entity's configuration:

```vb
' PurchaseOrderLineConfiguration.Configure
builder.ToTable("Pur_PurchaseOrderLines")
builder.HasKey(Function(l) l.Id)

' Append-only child table: no RowVersion column.
builder.Ignore(Function(l) l.RowVersion)
```

Build: 0 errors, 0 warnings.

## Prevention

- Any entity that inherits `AuditableEntity` (→ `ConcurrencyAwareEntity`) but maps to a
  table **without** a `RowVersion` column MUST call `builder.Ignore(Function(e) e.RowVersion)`
  in its `IEntityTypeConfiguration`, OR the table must add the column + `IsRowVersion()`.
- When you see "Unknown column 'X' in 'field list'" on insert, check for an **inherited**
  property X mapped by convention before assuming a missing migration.
- **Latent across the codebase:** the same bug class affects every other AuditableEntity-derived
  entity whose table omits RowVersion — e.g. `Pur_GoodsReceiptLines`, `Pos_SalesTransactionLines`,
  `Pos_CreditPayments`, and similar child tables. Flagged for separate review.

## Related

- `[[mariadb-pure-client-server-architecture]]` — concurrency-token (RowVersion) design
- `[[vbnet-parameter-shadows-property]]` — another silent VB.NET mapping pitfall
