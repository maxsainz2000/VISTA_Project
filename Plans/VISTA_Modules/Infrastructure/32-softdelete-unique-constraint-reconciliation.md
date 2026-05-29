---
module: MerchSys.Infrastructure
plan-id: INFRA-32
title: "Soft-Delete vs Unique-Constraint Reconciliation"
depends-on: [INFRA-26]
related: [INFRA-31]
estimated-files: 5
priority: high
origin: operator-testing (manager verification, 2026-05-29)
---

# INFRA-32: Soft-Delete vs Unique-Constraint Reconciliation

## Context

During operator testing on MariaDB (2026-05-29), deleting a Purchase Order draft
and then creating a new one crashed with:

```
MySqlException: Duplicate entry 'PO-2026-0001' for key 'IX_Pur_PurchaseOrders_OrderNumber'
```

Root cause: deletes on soft-deletable entities are **converted to soft deletes**
by `BaseDbContext.SaveChangesAsync` (`IsDeleted = True`), and EF queries hide
soft-deleted rows via the global query filter registered in
`BaseDbContext.ApplySoftDeleteFilters`. But the **unique index spans every row,
including soft-deleted ones**. Any code that derives a unique business key from an
EF query (which is soft-delete-filtered) will regenerate a value that still exists
on a soft-deleted row, and the insert collides.

This is a general tension: **soft delete + a unique business key + an
EF-filtered uniqueness check**. MariaDB has no filtered/partial unique index, so
the constraint cannot simply be scoped to `IsDeleted = 0` without a schema-level
generated-column trick (out of scope; append-only migration rules apply).

### Confirmed and suspected sites

| Status | Site | Key | Soft-deletable? |
|---|---|---|---|
| Patched on `debug/PUR-savedraft-rowversion` | `PurchaseOrderService.CreateDraftAsync` | `OrderNumber` | yes |
| **Confirmed latent — same bug** | `ReorderService.vb:183` (PO from reorder suggestion) | `OrderNumber` | yes |
| Safe | `GoodsReceivingService:56` (GR number) | `ReceiptNumber` | `GoodsReceipt` is NOT soft-deletable |
| Review | `VendorService` (vendor create) | `Vendor.Name` / any unique vendor key | `Vendor` is soft-deletable |
| Review | `ProductManagementViewModel` (product create) | `Product.Sku` | `Product` is soft-deletable |
| Review | `VendorProductService` (catalog link) | `(VendorId, ProductId)` unique | `VendorProduct` is soft-deletable |
| Review | `CreditService` (credit account create) | any unique customer key | `CreditAccount` is soft-deletable |

The first two are **auto-generated sequence** collisions (deterministic, will recur).
The "Review" rows are **user-entered** unique values — they collide only if a user
re-enters a value that a soft-deleted row still holds, but the failure mode is the
same opaque duplicate-key crash.

## Prerequisites

- **INFRA-26** — soft-delete interception + global query filter in place.
- Best sequenced **after INFRA-31** (so the RowVersion crashes are gone and these
  paths can actually reach the duplicate-key check).

## Wiki References

- `LLM_Wiki/agent_wiki/errors/efcore-inherited-rowversion-unmapped-column.md` (sibling debug session; cross-link the new entry to it)
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` (soft-delete + concurrency model)

## Decision required: project-wide policy

Pick ONE policy and apply it consistently (document the choice in the summary):

- **Policy A — Sequence skips deleted (generated keys):** uniqueness/sequence
  derivation queries with `.IgnoreQueryFilters()` so generated numbers never reuse
  a soft-deleted row's value. (This is what the debug-branch `CreateDraftAsync` fix
  already does.) Simple; numbers are monotonic and never reused. **Recommended for
  the auto-generated keys (`OrderNumber`).**
- **Policy B — Friendly duplicate handling (user-entered keys):** on insert of a
  user-entered unique value, first check `.IgnoreQueryFilters()` for an existing
  row (deleted or not). If a soft-deleted row holds the value, either restore it
  (`IsDeleted = False` + update) or surface a clear "this <thing> already exists
  (was previously deleted)" message instead of an opaque duplicate-key exception.
  **Recommended for the user-entered keys.**

These are complementary: A for generated sequences, B for user-entered values.

## Deliverables

### 1. Fix the confirmed generated-key sites (Policy A)

```
MerchSys.Purchasing/Services/ReorderService.vb            ' MOD — IgnoreQueryFilters() on the OrderNumber lookup (line ~183)
```

```vb
' Before
Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
    Select(Function(p) p.OrderNumber).
    ToListAsync()

' After
Dim existingNumbers As List(Of String) = Await _db.PurchaseOrders.
    IgnoreQueryFilters().
    Select(Function(p) p.OrderNumber).
    ToListAsync()
```

> `PurchaseOrderService.CreateDraftAsync` is already fixed on
> `debug/PUR-savedraft-rowversion`; ensure that branch is merged or re-apply the
> identical change so both PO-number generators behave consistently.

### 2. Audit & harden user-entered unique-key inserts (Policy B)

For each soft-deletable entity with a unique business key, make the create path
detect a collision against soft-deleted rows and respond gracefully (restore or
clear message). Candidate sites to audit:

```
MerchSys.Purchasing/Services/VendorService.vb             ' Vendor.Name / unique vendor key
MerchSys.Purchasing/Services/VendorProductService.vb      ' (VendorId, ProductId)
MerchSys.Inventory/ViewModels/ProductManagementViewModel.vb ' Product.Sku
MerchSys.POS/Services/CreditService.vb                    ' CreditAccount unique key (if any)
```

First confirm which of these actually have a UNIQUE index (check
`0001_initial_schema.sql` for `UNIQUE KEY` / `UX_*`). Only the ones with a unique
constraint need the treatment. `Pur_VendorProducts` has
`UX_Pur_VendorProducts_Vendor_Product` (confirmed). Verify the others before
editing — do not add handling where no unique constraint exists.

## Acceptance Criteria

1. Create a PO draft, delete it, create another → no duplicate-key crash; new PO
   gets the next sequential number (e.g. `PO-2026-0002`). Repeat via the reorder
   suggestion path (`ReorderService`) with the same result.
2. For each soft-deletable entity with a confirmed unique constraint: deleting a
   row then re-creating one with the same unique value yields either a successful
   restore or a clear, user-readable message — never an opaque
   `Duplicate entry ... for key` exception bubbling to the UI.
3. The chosen policy (A for generated, B for user-entered) is applied consistently
   across all confirmed sites.
4. Build: 0 errors / 0 warnings.

## Out of Scope (Defer)

- Schema-level filtered uniqueness (generated-column + unique index trick) — only
  if a future requirement makes restore/skip insufficient.
- Hard-delete (purge) tooling for soft-deleted rows — separate concern.
- Reusing/recycling generated numbers — Policy A intentionally never reuses.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-32-summary.md` per `Progress/_template.md`. Include:

- The policy decision (A / B / both) and rationale.
- The authoritative list of unique constraints on soft-deletable tables (from the
  schema), and which create paths were hardened vs. confirmed not-applicable.
- Evidence for AC #1 (the delete→recreate cycle) for both PO generators.
- Confirmation of how the debug-branch `CreateDraftAsync` fix was reconciled
  (merged vs. re-applied).

### Documentation

- An agent-wiki error-fix entry under `LLM_Wiki/agent_wiki/errors/` documenting the
  soft-delete vs unique-constraint pattern, cross-linked to
  `efcore-inherited-rowversion-unmapped-column.md`. Update `agent_wiki/index.md`
  and `log.md` per the wiki workflow.
- Inline comment at each `IgnoreQueryFilters()` sequence site explaining that the
  unique index spans soft-deleted rows.
