# Error Fix: EF Core Soft-Delete vs Database Unique-Key Collision

---

```yaml
---
type: error-fix
module: Infrastructure
agent: antigravity
date: 2026-05-29
tags: [ef-core, soft-delete, unique-key, duplicate-key, mariadb, vb-net, runtime-error]
error-code: MySqlException 1062 (Duplicate entry)
severity: runtime-error
---
```

## Problem

During deletion and re-creation workflows on soft-deletable entities (e.g. Purchase Orders, Vendors, Products), saving a newly created record throws:

```
MySqlException: Duplicate entry '...' for key '...'
```

This occurred despite checking for existence using EF Core (e.g. `_db.Vendors.AnyAsync(Function(v) v.Name.ToLower() = name)` returning `False`).

## Root Cause

EF Core soft-delete implementation intercepts hard deletes and updates the row with `IsDeleted = True`. EF Core queries hide soft-deleted rows using a global query filter:

```vb
modelBuilder.Entity(entityType.ClrType).HasQueryFilter(e => Not e.IsDeleted)
```

However, **database-level unique indexes (constraints) span every row, including soft-deleted ones**. 

When a new entity is created with the same unique value (e.g. Vendor Name, Product SKU, or PO sequence number):
1. The standard EF Core query is filtered, so it does **not** see the soft-deleted row and erroneously concludes the unique value is available.
2. EF Core attempts to INSERT the new row, but the database unique index collides with the soft-deleted row, causing a `Duplicate entry` crash.

## Fix

We adopted two complementary, project-wide policies to resolve this:

### Policy A: Sequence Skips Deleted (for generated keys)
For auto-generated monotonic sequence keys (e.g. `OrderNumber` on `PurchaseOrder`), use `.IgnoreQueryFilters()` on the database query before generating the sequence to ensure we never reuse a soft-deleted row's value.

```vb
' Before (reused deleted number, collided)
Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
    Select(Function(p) p.OrderNumber).
    ToListAsync()

' After (safely ignores soft-deleted rows and skips)
Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
    IgnoreQueryFilters().
    Select(Function(p) p.OrderNumber).
    ToListAsync()
```

### Policy B: Friendly Duplicate Handling (for user-entered keys)
For user-entered unique keys, query the database using `.IgnoreQueryFilters()` first.
- **For entities where a deleted record should not be re-created but can be restored** (e.g., `VendorProduct` catalog linkage), automatically restore the soft-deleted row by resetting its deletion flags and updating its values instead of inserting a new row.
- **For entities where a descriptive error is required** (e.g., `Vendor.Name` or `Product.Sku`), surface a clean, user-readable message explaining that the item exists in a deleted state, rather than crashing on save.

```vb
' Example of Policy B auto-restore (VendorProduct):
Dim existingEntry = Await _db.VendorProducts.
    IgnoreQueryFilters().
    FirstOrDefaultAsync(Function(vp) vp.VendorId = vendorId AndAlso vp.ProductId = productId)

If existingEntry IsNot Nothing Then
    If Not existingEntry.IsDeleted Then
        Throw New InvalidOperationException("This product is already in the vendor's catalog.")
    Else
        ' Restore the deleted entry
        existingEntry.IsDeleted = False
        existingEntry.ProductName = productName
        existingEntry.LastUnitCost = unitCost
        existingEntry.Notes = notes
        existingEntry.DeletedBy = Nothing
        existingEntry.DeletedAt = Nothing
        existingEntry.ModifiedBy = _session.CurrentUsername
        existingEntry.ModifiedAt = DateTime.UtcNow
        Await _db.SaveChangesAsync()
        Return existingEntry
    End If
End If
```

## Prevention

1. **Scan for UNIQUE constraints:** Check your `0001_initial_schema.sql` (or active migrations) for any `UNIQUE KEY` or unique indexes.
2. **Determine if soft-deletable:** Cross-reference unique columns with the entity inheritance graph. If the entity inherits `SoftDeletableEntity` / `ISoftDeletable`, it is susceptible to this collision.
3. **Always use `.IgnoreQueryFilters()`:** Never query unique check keys using default filters if there are soft-deletable records in that table. Use Policy A or B to handle collisions gracefully.

## Related

- Links to related Agent Wiki entries: `[[efcore-inherited-rowversion-unmapped-column]]`
- Links to Domain Wiki pages: `[[centralized-database-architecture]]`
