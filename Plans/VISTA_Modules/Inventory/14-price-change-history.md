---
module: MerchSys.Inventory
plan-id: INV-14
title: "Product RetailPrice Change History"
depends-on: [INV-01, INV-11]
estimated-files: 6
priority: medium
---

# Product RetailPrice Change History

## Context

`Product.RetailPrice` is a mutable `Decimal` field on `Inv_Products`. Today the only write path is `ProductManagementViewModel.SaveProductAsync` (`MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb:559-566`), which assigns `existing.RetailPrice = price` and calls `_db.SaveChangesAsync()`. There is no audit log, no event, and no way for a Manager or Owner to answer the question "when did the price of this product last change, and to what?".

Vendor unit cost is **not** in scope: `StockBatch.UnitCost` (`MerchSys.Inventory/Entities/StockBatch.vb:22`) is immutable per batch and FIFO-deduced, so per-batch cost history already exists implicitly. `Pur_PriceChangeAlerts` covers PO-agreed vs receipt-actual variance. Retail price is the only true gap.

This plan introduces an append-only `Inv_ProductPriceHistory` table, a write hook in `ProductManagementViewModel.SaveProductAsync` that records every retail-price change, and a read-only `ProductPriceHistoryView` accessible from the product management screen.

Promoted from `Plans/Future/deferred-features-backlog.md` item 17 (2026-05-27). Scope decision: retail price only (vendor cost history deferred indefinitely — implicit via `StockBatch`).

## Prerequisites

- **INV-01** (Inventory Domain Models) — `Product` entity, `Inv_Products` table
- **INV-11** (Product Management View) — `ProductManagementViewModel`, `SaveProductAsync` write path

## Wiki References

- `concepts/modular-monolith.md` — single DbContext per module, audit columns convention
- `concepts/bir-compliance.md` — operational data vs. tamper-proof audit (retail price changes are operational, not BIR-tamper-protected — no immutability triggers in this plan)

## Deliverables

```
MerchSys.Inventory/Entities/
└── ProductPriceHistory.vb                                 ' NEW — ProductId, OldPrice, NewPrice, ChangedAt, ChangedBy, Reason

MerchSys.Inventory/Data/Configurations/
└── ProductPriceHistoryConfiguration.vb                    ' NEW — table Inv_ProductPriceHistory, index (ProductId, ChangedAt DESC)

MerchSys.Inventory/Data/
└── InventoryDbContext.vb                                  ' MOD — add ProductPriceHistory DbSet

MerchSys.Inventory/Migrations/
└── 2026XXXX_AddProductPriceHistory.vb                     ' NEW — manual SQL migration

MerchSys.Inventory/ViewModels/
└── ProductManagementViewModel.vb                          ' MOD — SaveProductAsync captures old price, writes history row in same SaveChanges

MerchSys.App/Views/Inventory/
└── ProductPriceHistoryView.xaml(.vb)                      ' NEW — read-only history viewer launched from ProductManagementView
```

## Specification

### `ProductPriceHistory` entity

```vb
Public Class ProductPriceHistory
    Inherits Entity                              ' NOT AuditableEntity — the row IS the audit record

    Public Property ProductId As Integer         ' Cross-row ref within Inventory; can be EF FK
    Public Property OldPrice As Decimal
    Public Property NewPrice As Decimal
    Public Property ChangedAt As DateTime
    Public Property ChangedBy As String
    Public Property Reason As String             ' nullable; optional UI field
End Class
```

- Plain `Entity` (Id + nothing else inherited). No `ModifiedBy`/`ModifiedAt` — append-only by design.
- No `IsDeleted` either; rows are never deleted at the application layer.
- Append-only is enforced at the **service / VM layer** (no code paths call `Update`/`Remove` on `ProductPriceHistory`). DB-level immutability triggers are **out of scope** — retail price changes are operational data, not BIR tamper-protected. A follow-up plan can add SQLite triggers à la ACC-15 if that requirement ever escalates.

### `ProductPriceHistoryConfiguration`

- Table: `Inv_ProductPriceHistory`.
- Index on (`ProductId`, `ChangedAt DESC`) — supports "most recent change first" history view.
- FK `ProductId → Inv_Products(Id)` with `DeleteBehavior.Restrict` (a product with history cannot be hard-deleted; soft delete on `Inv_Products` is unaffected since soft delete doesn't touch the FK).

### Migration

Manual SQL per CLAUDE.md EF CLI ban:

```sql
CREATE TABLE IF NOT EXISTS Inv_ProductPriceHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId INTEGER NOT NULL,
    OldPrice NUMERIC NOT NULL,
    NewPrice NUMERIC NOT NULL,
    ChangedAt TEXT NOT NULL,
    ChangedBy TEXT NOT NULL,
    Reason TEXT,
    FOREIGN KEY (ProductId) REFERENCES Inv_Products(Id)
);
CREATE INDEX IF NOT EXISTS IX_Inv_ProductPriceHistory_Product_Date
    ON Inv_ProductPriceHistory (ProductId, ChangedAt DESC);
```

Applied by `DatabaseInitializer` at `Application_Startup`.

### `ProductManagementViewModel.SaveProductAsync` modification

Insert the history-capture block immediately before the existing `existing.RetailPrice = price` assignment (currently around line 559):

```vb
Dim oldPrice As Decimal = existing.RetailPrice
If oldPrice <> price Then
    Dim historyRow As New ProductPriceHistory With {
        .ProductId = existing.Id,
        .OldPrice = oldPrice,
        .NewPrice = price,
        .ChangedAt = DateTime.UtcNow,
        .ChangedBy = _sessionUser.Username,        ' use the existing session accessor in the VM
        .Reason = capturedReason                    ' nullable; from optional dialog or Nothing
    }
    _db.ProductPriceHistory.Add(historyRow)
End If

existing.RetailPrice = price
' ... rest of method unchanged
Await _db.SaveChangesAsync()
```

Key constraints:
- **Atomic commit:** the product UPDATE and the history INSERT must commit in the same `SaveChangesAsync` so a crash between them is impossible.
- **No-op skip:** when `oldPrice = price`, no row is written. Editing a product's name without changing price must not pollute history.
- **No `ISyncableRepository`:** match the existing `ProductManagementViewModel` pattern (backlog item 10 closure 2026-05-27).
- **Session user:** reuse whatever session-user accessor the VM constructor already injects. Do not invent a new one. If none exists yet at the VM, fall back to `Environment.UserName` only as a last resort and flag it in the progress summary.
- **Reason capture:** optional. If the existing edit dialog has space, add a single-line text input "Reason (optional)". If not, skip the dialog change and store `Nothing` — the history table is still useful without it.

### `ProductPriceHistoryView`

- Read-only `DataGrid` with columns: `ChangedAt` (formatted local time), `OldPrice → NewPrice`, `Delta` (computed: `NewPrice - OldPrice`), `ChangedBy`, `Reason`.
- Bound to a `ProductPriceHistoryViewModel` that loads `_db.ProductPriceHistory.Where(p => p.ProductId = selectedId).OrderByDescending(p => p.ChangedAt).ToListAsync()`.
- **VB.NET trap warning:** the codebase wiki notes a known issue where `ToListAsync()` on a full-entity query in EF Core 10 VB.NET silently returns empty. If it triggers here, project to an explicit DTO inside the query: `Select(Function(p) New PriceHistoryDto With { ... }).ToListAsync()`, or fall back to the raw `SqliteConnection` synchronous pattern documented in `agent_wiki/errors/`.
- Launched from `ProductManagementView` via a new "View Price History" button next to the selected product.
- Manager and Owner both have read access. No edit/delete bindings exist anywhere in the view.

## Acceptance Criteria

- `Inv_ProductPriceHistory` table created at first launch after deployment.
- Every `ProductManagementViewModel.SaveProductAsync` call with a changed `RetailPrice` produces exactly one row.
- No-op edits (`oldPrice = price`) produce zero rows.
- Products with no history show an empty grid, not an error.
- The product UPDATE and the history INSERT are atomic — no partial state survives a `SaveChangesAsync` failure.
- `ProductPriceHistoryView` is read-only — no edit/delete bindings anywhere.
- Build clean (0 errors / 0 warnings) per CLAUDE.md.

## Out of Scope (Defer)

- **Vendor unit-cost history** — already implicit via per-batch `StockBatch.UnitCost` (FIFO) plus `Pur_PriceChangeAlerts` for PO-vs-receipt variance.
- **DB-level immutability triggers** — retail price history is operational, not BIR tamper-protected. Add a follow-up plan (modeled after ACC-15 SQLite immutability triggers) if requirements escalate.
- **Backfill** — there is no source of truth for historical retail prices before this plan ships. Existing products simply have no history rows until their next edit.
- **Bulk price edits** — if a bulk-update tool is added later, it must also write history rows; that scope belongs to whichever plan introduces the tool.

## Notes for Implementers

- Confirm the exact line numbers in `ProductManagementViewModel.SaveProductAsync` at the time of implementation — `559-566` is from the 2026-05-27 audit and may have drifted.
- The `Reason` column is nullable. Don't index it. Don't validate it server-side beyond a sane `MaxLength` (e.g., 500).
- Append-only is a code-level invariant in this plan. Any future code that calls `Update`/`Remove` on `ProductPriceHistory` is a defect; a one-line comment on the entity makes that explicit.
- Common VB.NET traps to watch: parameter named `oldprice` would shadow a property if one existed (use `oldPrice` as shown), and `ToListAsync()` on full entities — see "VB.NET trap warning" above.
