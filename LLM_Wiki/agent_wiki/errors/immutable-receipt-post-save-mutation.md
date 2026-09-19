---
type: error-fix
module: MerchSys.POS
agent: claude-code
date: 2026-06-11
tags: [ef-core, vb-net, mariadb, interceptor, immutability, official-receipt, decorator, shared-dbcontext, runtime-error]
error-code: ImmutableEntityException
severity: runtime-error
---

## Problem

`VatAwareReceiptService.GenerateReceiptAsync` threw `ImmutableEntityException` on **every** receipt generation (i.e. every VAT-aware checkout — it is the registered `IReceiptService`). The build was green; the fault only manifests at runtime.

The offending sequence (introduced by INT-21 / POS-8 to reconcile centavo drift between the line-level and three-bucket VAT totals):

```vb
' Step 5 — VatAwareReceiptService
Dim receipt = Await _inner.GenerateReceiptAsync(transactionId)  ' creates + SaveChanges → receipt tracked
receipt.VatAmount = totals.OutputVat                            ' tracked entity → state = Modified
Await _context.SaveChangesAsync()                               ' ← ImmutableEntityException thrown here
```

## Root Cause

`OfficialReceipt` is immutable at the ORM layer: `POSDbContext` registers `ImmutableReceiptInterceptor`, whose `SavingChanges`/`SavingChangesAsync` throw `ImmutableEntityException` on **any** `OfficialReceipt` entry in `Modified` or `Deleted` state (NIRC §235, BIR 10-year retention).

The non-obvious part is **why the mutation lands on a tracked, persisted entity**: `ReceiptService` (inner) and `VatAwareReceiptService` (decorator) both inject `POSDbContext`, both are `AddScoped`, and in this app all services resolve from the **root** provider (see `wpf-mainwindow-not-shell-window` / the root-provider DI note), so they share **one** `POSDbContext` instance. The inner service `Add`s the receipt and saves it (now tracked, `Unchanged`); the decorator then sets `receipt.VatAmount` (→ `Modified`) and calls `SaveChangesAsync` on the *same* context → the interceptor fires.

Patching an immutable entity *after* its first save is structurally impossible in this design. The value has to be correct on the **first** insert.

## Fix

Source the receipt's VAT amount from the `SalesTransaction`, which the decorator already stamps with the authoritative three-bucket `OutputVat` (and persists) in Step 4 **before** the inner service runs. The receipt is then created correct on its first (and only) save; no post-hoc mutation.

```vb
' ReceiptService.GenerateReceiptAsync — before (recomputed, then overwritten by the decorator)
Dim vatAmount = transaction.TotalAmount - Math.Round(transaction.TotalAmount / (1D + vatRate), 2, MidpointRounding.ToEven)
Dim receipt As New OfficialReceipt() With { ... .VatAmount = vatAmount, ... }

' After — single source of truth, set at creation
Dim receipt As New OfficialReceipt() With { ... .VatAmount = transaction.VatAmount, ... }
```

```vb
' VatAwareReceiptService — before
Dim receipt = Await _inner.GenerateReceiptAsync(transactionId)
receipt.VatAmount = totals.OutputVat
Await _context.SaveChangesAsync()

' After — no mutation of the immutable receipt
Dim receipt = Await _inner.GenerateReceiptAsync(transactionId)
```

The integrity hash (Step 6) still sees the correct `VatAmount`, because it is now present at creation.

## Prevention

- **Never write to an `OfficialReceipt`/`ReceiptIntegrity` after its first `SaveChanges`.** They are immutable (`ImmutableReceiptInterceptor`). Any "patch it afterward" pattern will throw `ImmutableEntityException`. Set every field at construction.
- When a decorator needs to influence a value produced by an inner service, push the value **upstream** (stamp it on the source entity the inner service reads from) rather than mutating the inner service's **output**.
- Remember that two services sharing a module `DbContext` (the norm here, since everything resolves from the root provider) share the **change tracker** — an "output" entity from one is a tracked, mutation-sensitive entity to the other.

## Related

- `[[efcore-inherited-rowversion-unmapped-column]]` — other POS/EF concurrency-and-persistence trap
- `[[owner-readonly-kpi-write-on-read]]` — another interceptor-driven write rejection (RoleGuard) with the same "writes you didn't expect" shape
- Domain: `[[bir-compliance]]` (NIRC §235 immutability), `[[client-server-wpf]]`
