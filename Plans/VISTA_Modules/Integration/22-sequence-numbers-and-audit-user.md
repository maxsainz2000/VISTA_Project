---
module: MerchSys.Integration
plan-id: INT-22
title: "Concurrency-safe sequence numbers + session-aware audit attribution"
depends-on: [INT-18]
estimated-files: 9
priority: high
---

# INT-22: Concurrency-safe sequence numbers + session-aware audit attribution

## Context

Two verified data-integrity defects that share a theme (correct multi-client behaviour):

- **POS-4 / PUR-3 — duplicate document numbers under concurrency.** Transaction numbers (`CartService.GenerateTransactionNumberAsync`) use `CountAsync() + 1`, and PO numbers (`PurchaseOrderService.CreateDraftAsync`, also `ReorderService`/`GoodsReceivingService`) compute max+1 from an in-memory list. Two of the four client laptops checking out / drafting at the same time can read the same value and collide on the unique index, aborting one operation. This is a **genuine cross-terminal race** (unlike the class-field findings).
- **PUR-4 — audit trail always says "Manager".** `BaseDbContext.SaveChangesAsync` unconditionally stamps `CreatedBy/ModifiedBy = DefaultUser` ("Manager"), discarding values services already set from `_session.CurrentUsername`. The "until the auth module is implemented" comment is stale — auth and session tracking exist.

The codebase already contains the correct concurrency pattern to copy: `ReceiptIntegrityService.GetNextReceiptNumberAsync` uses a `ReceiptSequence` table inside a `Serializable` transaction with bounded retry on `DbUpdateConcurrencyException`.

## Prerequisites

- **INT-18** — remediation index.

## Wiki References

- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` — optimistic concurrency + serialized writes.
- `CLAUDE.md` — schema changes are raw SQL at startup; audit columns on every table.

## Deliverables

```
' --- Sequence numbers (POS-4 / PUR-3) ---
MerchSys.POS/Services/CartService.vb                       ' Modified — GenerateTransactionNumberAsync -> sequence
MerchSys.Purchasing/Services/PurchaseOrderService.vb       ' Modified — PO number -> sequence
MerchSys.Purchasing/Services/ReorderService.vb             ' Modified — PO number generation path
MerchSys.Purchasing/Services/GoodsReceivingService.vb      ' Modified — GR number generation path
MerchSys.Purchasing/Helpers/SequentialNumberGenerator.vb   ' Reviewed/retired or repurposed
MerchSys.App/Data/MariaDbSchemaInitializer.vb              ' Modified — new sequence tables DDL

' --- Audit attribution (PUR-4) ---
MerchSys.SharedKernel/Data/BaseDbContext.vb                ' Modified — session-aware stamping
MerchSys.App/Data/DatabaseConfig.vb                        ' Modified — inject ISessionService into DbContexts
MerchSys.<each module>/Data/<Module>DbContext.vb           ' Modified — ctor passes ISessionService through
```

## Specification

### Part 1 — sequence tables (POS-4 / PUR-3)

Add per-domain sequence tables to the startup schema initializer (raw SQL, `CREATE TABLE IF NOT EXISTS`), mirroring `Pos_ReceiptSequences`:

```
Pos_TransactionSequences ( Year INT PRIMARY KEY, NextValue INT NOT NULL )
Pur_OrderSequences       ( SeqKey VARCHAR(16) PRIMARY KEY, NextValue INT NOT NULL )   -- SeqKey e.g. "PO-2026"
```

Implement a sequence accessor that follows `ReceiptIntegrityService.GetNextReceiptNumberAsync` exactly:

- `BeginTransactionAsync(IsolationLevel.Serializable)`.
- `FirstOrDefaultAsync` the row (scalar/entity by PK is safe — not the `ToListAsync` shape); insert with `NextValue = 1` if missing, else increment.
- `SaveChangesAsync` + `CommitAsync`.
- Catch `DbUpdateConcurrencyException`, `ChangeTracker.Clear()`, retry up to N times.

Then:

- **POS-4:** replace `GenerateTransactionNumberAsync` body with a call to the transaction sequence; format `TX-{year}-{nextValue:D4}` (preserve the existing string shape).
- **PUR-3:** replace the `existingNumbers = ...Select(OrderNumber).ToListAsync()` + `SequentialNumberGenerator.Generate(...)` path in `PurchaseOrderService.CreateDraftAsync` (and the equivalent in `ReorderService.AcceptSuggestionAsync` and `GoodsReceivingService.ReceiveGoodsAsync`) with the `Pur_OrderSequences` accessor keyed by `"PO-{year}"` / `"GR-{year}"`. Preserve the existing number format.

Ensure the relevant unique indexes exist (`Pos_SalesTransactions.TransactionNumber`, `Pur_PurchaseOrders.OrderNumber`) in the schema initializer so a residual collision still fails loudly rather than duplicating.

`SequentialNumberGenerator` may be retired once no caller remains, or kept only if a non-sequence caller exists — note the decision in the summary.

> Keep all existing raw-ADO read loops as-is. The sequence accessor uses EF entity-by-PK + `SaveChanges`, which is safe (not the full-entity `ToListAsync` shape).

### Part 2 — session-aware audit attribution (PUR-4)

Make the audit stamp use the logged-in user:

1. **`BaseDbContext`** — add a constructor parameter `session As ISessionService` (nullable-tolerant), store it, and replace the hardcoded `DefaultUser` stamping with a resolver that prefers (a) a value the caller already set, then (b) the session user, then (c) `DefaultUser`:

```vb
Private Function CurrentUser() As String
    Dim u = _session?.CurrentUsername
    Return If(String.IsNullOrWhiteSpace(u), DefaultUser, u)
End Function
' Added:
auditable.CreatedBy = If(String.IsNullOrWhiteSpace(auditable.CreatedBy), CurrentUser(), auditable.CreatedBy)
auditable.ModifiedBy = CurrentUser()   ' on Added + Modified
```

(On `Added`, respect a pre-set `CreatedBy` such as `"System"` from `GoodsReceivingService`; always refresh `ModifiedBy` to the acting user.)

2. **Each module `DbContext`** — thread the new parameter through the constructor to `MyBase.New(options, session)`.

3. **`DatabaseConfig`** — `AddDbContext(Of XDbContext)` resolves the extra ctor parameter from the container automatically because `ISessionService` is registered (Singleton). Confirm each `AddDbContext` lambda compiles with the new ctor; no explicit factory change is usually needed, but verify.

Leave `RoleGuardInterceptor` (already session-aware) and the soft-delete logic unchanged.

> Out of scope: consolidating audit stamping into an interceptor and reviving `AuditInterceptor` (a larger refactor that would also close SK-1). Note it as a Phase-2 option in the summary.

## Implementation Notes

- The sequence accessor is the riskier change; copy `GetNextReceiptNumberAsync` structure precisely, including the `Serializable` isolation and retry loop.
- `ISessionService` may be unauthenticated during startup/seed writes; `CurrentUser()` falls back to `DefaultUser`, so seeding still works.
- Build after each part (`dotnet build MerchSys.slnx`). Per `CLAUDE.md`, document non-trivial build errors in the summary and stop.
- Do not change document-number string formats — downstream parsing and the unique indexes depend on them.

## Acceptance Criteria

1. `Pos_TransactionSequences` and `Pur_OrderSequences` are created at startup; unique indexes on `TransactionNumber` and `OrderNumber` are present.
2. Transaction and PO/GR numbers are generated via the serialized sequence accessor (no `CountAsync()+1`, no load-all-numbers).
3. `BaseDbContext` stamps `CreatedBy/ModifiedBy` from the session user, respects a pre-set `CreatedBy`, and falls back to `DefaultUser` when unauthenticated.
4. All four module DbContexts construct with the session parameter; the app starts and writes succeed.
5. No raw-ADO read loop or `ToListAsync` shape was changed.
6. `dotnet build MerchSys.slnx` — target 0 errors, 0 warnings (or documented per protocol).

## Output Requirements

Create a progress report at `Progress/VISTA_Modules/Integration/INT-22-summary.md` using `Progress/_template.md`. Record the new tables, the sequence accessor location, the `SequentialNumberGenerator` decision, and confirm seed/startup writes still attribute correctly when unauthenticated.
