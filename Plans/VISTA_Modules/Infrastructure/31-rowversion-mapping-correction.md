---
module: MerchSys.Infrastructure
plan-id: INFRA-31
title: "RowVersion Mapping Correction — Restore INFRA-26 Concurrency-Token Scope"
depends-on: [INFRA-26]
estimated-files: 8
priority: critical
amendment-ref: AMD-2026-05-28-01
origin: operator-testing (manager verification, 2026-05-29)
---

# INFRA-31: RowVersion Mapping Correction

## Context

During operator testing on MariaDB (2026-05-29), the Purchasing "Save Draft" flow
crashed with:

```
MySqlException: Unknown column 'RowVersion' in 'field list'
```

Investigation traced this to a **deviation from the INFRA-26 design**. INFRA-26
(§"Optimistic concurrency tokens", lines 51–58) explicitly states:

> Append-only entities **do not** inherit `ConcurrencyAwareEntity`:
> `Inv_StockMovements`, `Inv_SaleCogs`, `Pos_ReceiptIntegrity`, `Acc_RevenueRecords`,
> `Acc_ExpenseRecords`, `Acc_TamperAuditLog`, `Pur_PriceChangeAlerts`.

The implementation did the opposite: `AuditableEntity` was made to inherit
`ConcurrencyAwareEntity`, so **every** auditable entity (and every soft-deletable
entity, since `SoftDeletableEntity : AuditableEntity`) gained a `RowVersion`
property. EF Core maps that inherited property to a `RowVersion` column **by
convention**. For the ~16 mutable tables that actually have the column this is
fine; for every append-only / child / line / log table that does **not** have the
column, the first EF insert or update throws `Unknown column 'RowVersion'`.

Concurrency-awareness is **orthogonal** to the audit/soft-delete hierarchy (some
mutable entities are `AuditableEntity`-direct, some are `SoftDeletableEntity`;
some append-only entities are also `AuditableEntity`). A pure single-inheritance
redesign therefore cannot model it cleanly — which is why the shortcut was taken.
This plan keeps the inherited property but corrects the **EF mapping** so that
`RowVersion` is only treated as a real column where the table actually has one.

### Why this is critical

The crash is not isolated to Purchase Orders. Every EF write path to a table
without a `RowVersion` column is currently broken. Confirmed crashers:

| Module | Entity → table | Triggered by |
|---|---|---|
| Purchasing | `PurchaseOrderLine` → `Pur_PurchaseOrderLines` | Save/Update PO (already patched on `debug/PUR-savedraft-rowversion`) |
| Purchasing | `GoodsReceipt` + `GoodsReceiptLine` | Receiving a PO (`GoodsReceivingService:92`) |
| Purchasing | `ReorderSuggestion` | Reorder engine (`ReorderService:141`) |
| Purchasing | `PriceChangeAlert` | `PriceChangeService:71` |
| Inventory | `StockMovement` → `Inv_StockMovements` | Almost every stock change (StockService, ShrinkageService, InventoryAuditService) |
| Inventory | `ShrinkageRecord` | `ShrinkageService:75,120`, `ExpiryTrackingService:245` |
| Inventory | `StockAuditRecord` | `InventoryAuditService:60,117` |
| POS | `SalesTransactionLine` → `Pos_SalesTransactionLines` | Every sale (`CartService:126` cascade) |
| POS | `OfficialReceipt` | Every receipt (`ReceiptService:84`) |
| POS | `ReceiptIntegrity` | `ReceiptIntegrityService:60` |
| POS | `CreditPayment` | `CreditService:158` |
| POS | `SalesReturn` | `SalesReturnService:84` |
| Accounting | `ExpenseRecord` → `Acc_ExpenseRecords` | 5 handlers (goods receipt, sale COGS, shrinkage, credit payment, goods-received-VAT) |
| Accounting | `RevenueRecord` | Every sale (`SaleRevenueHandler:106`) |
| Accounting | `FinancialSnapshot` | `FinancialOverviewService:194` |
| Accounting | `VatReturnLine` | VAT return generation (cascade via `VatReturns.Lines`) |

## Prerequisites

- **INFRA-26** — Optimistic concurrency tokens introduced (this plan corrects its
  implementation; INFRA-26 itself stays the authoritative design intent).

## Wiki References

- `LLM_Wiki/agent_wiki/errors/efcore-inherited-rowversion-unmapped-column.md` (root-cause write-up from the 2026-05-29 debug session)
- `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md` (concurrency control)
- `Plans/VISTA_Modules/Infrastructure/26-optimistic-concurrency-and-fifo-lock.md` (original design intent)

## Authoritative entity classification

Implementers must treat the two lists below as the source of truth. They are
derived from `MerchSys.Infrastructure/Data/Migrations/Central/0001_initial_schema.sql`
(which tables physically have a `RowVersion` column) cross-referenced with the
entity inheritance graph.

### Group A — Concurrency-token entities (table HAS `RowVersion`; KEEP it)

Already configured with `IsRowVersion()` — verify only:
`Product`, `ProductCategory`, `StockBatch`, `CreditAccount`, `ReceiptSequence`,
`SalesTransaction`, `VendorProduct`, `Vendor`, `PurchaseOrder`, `AccountsPayableEntry`.

**Column exists + property exists but `IsRowVersion()` is MISSING — must be ADDED**
(currently working only because the column happens to exist; they are not real
concurrency tokens, contrary to INFRA-26 intent):
`ReorderConfig` (`Pur_ReorderConfigs`), `StockAlertConfig` (`Inv_StockAlertConfigs`),
`VatConfiguration` (`Pos_VatConfiguration`), `FinancialPeriod` (`Acc_FinancialPeriods`),
`VatReturn` (`Acc_VatReturns`).

> **Verify** `Sys_UserAccounts` / its entity: the table has a `RowVersion` column.
> Confirm whether the `UserAccount` entity carries a `RowVersion` property (it does
> not appear to inherit `AuditableEntity`). If it has no property, no action — the
> DB default fills the column. If it does, classify into Group A and add `IsRowVersion()`.

### Group B — Non-concurrency entities (table has NO `RowVersion`; IGNORE the property)

Purchasing: `PurchaseOrderLine`, `GoodsReceipt`, `GoodsReceiptLine`,
`ReorderSuggestion`, `PriceChangeAlert`.
Inventory: `StockMovement`, `ShrinkageRecord`, `StockAuditRecord`.
POS: `SalesTransactionLine`, `SalesReturn`, `OfficialReceipt`, `ReceiptIntegrity`,
`ReceiptIntegrityArchive`, `OfficialReceiptArchive`, `CreditPayment`.
Accounting: `ExpenseRecord`, `RevenueRecord`, `FinancialSnapshot`, `VatReturnLine`.

Already-safe (inherit `BaseEntity`, no `RowVersion` property — no action):
`ProductPriceHistory`, `SaleCogsRecord`, `TamperAuditEntry`.

## Deliverables

### 1. Make Group A authoritative (add the 5 missing `IsRowVersion()` configs)

```
MerchSys.Purchasing/Data/Configurations/ReorderConfigConfiguration.vb       ' MOD
MerchSys.Inventory/Data/Configurations/StockAlertConfigConfiguration.vb     ' MOD
MerchSys.POS/Data/Configurations/VatConfigurationConfiguration.vb           ' MOD
MerchSys.Accounting/Data/Configurations/FinancialPeriodConfiguration.vb     ' MOD
MerchSys.Accounting/Data/Configurations/VatReturnConfiguration.vb           ' MOD
```

Use the same pattern already present on the working Group A configs:

```vb
builder.Property(Function(e) e.RowVersion).
    IsRowVersion().
    HasColumnType("TIMESTAMP(6)").
    ValueGeneratedOnAddOrUpdate()
```

(Match whatever exact form the existing 10 Group A configs use — copy it verbatim
so the provider behavior is identical.)

### 2. Central convention to ignore `RowVersion` on Group B (the actual fix)

Add a single mechanism in `MerchSys.SharedKernel/Data/BaseDbContext.vb` that, for
every entity type whose `RowVersion` property is **not** a concurrency token,
ignores (removes) the property from the model. This eliminates all ~19 Group B
crashers at once and is self-maintaining as new entities are added.

```
MerchSys.SharedKernel/Data/BaseDbContext.vb                                 ' MOD
```

**Critical ordering constraint.** Module DbContexts currently do:

```vb
Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
    MyBase.OnModelCreating(modelBuilder)                                   ' base runs FIRST
    modelBuilder.ApplyConfigurationsFromAssembly(GetType(...).Assembly)    ' configs run AFTER
```

So any ignore-loop placed directly in `BaseDbContext.OnModelCreating` would run
**before** the `IsRowVersion()` configs are applied and would therefore see zero
concurrency tokens and wrongly strip `RowVersion` from Group A too.

**Recommended implementation:** register an EF Core `IModelFinalizingConvention`
(via `ConfigureConventions`) that runs after all entity configuration. In the
finalizing pass, for each entity type, find the `RowVersion` property; if it exists
and `IsConcurrencyToken = False`, remove it. This is order-independent and the
idiomatic EF approach.

```vb
' Sketch — implementer to adapt to the exact EF Core 10 convention API.
Private NotInheritable Class IgnoreNonTokenRowVersionConvention
    Implements IModelFinalizingConvention

    Public Sub ProcessModelFinalizing(modelBuilder As IConventionModelBuilder,
                                      context As IConventionContext(Of IConventionModelBuilder)) _
                                      Implements IModelFinalizingConvention.ProcessModelFinalizing
        For Each et In modelBuilder.Metadata.GetEntityTypes()
            Dim rv = et.FindProperty("RowVersion")
            If rv IsNot Nothing AndAlso Not rv.IsConcurrencyToken() Then
                et.Builder.Ignore("RowVersion", fromDataAnnotation:=False)
            End If
        Next
    End Sub
End Class
```

Register it in `BaseDbContext.ConfigureConventions`:

```vb
Protected Overrides Sub ConfigureConventions(configurationBuilder As ModelConfigurationBuilder)
    MyBase.ConfigureConventions(configurationBuilder)
    configurationBuilder.Conventions.Add(Function(sp) New IgnoreNonTokenRowVersionConvention())
End Sub
```

> If the convention API proves awkward under EF Core 10 + the chosen MySQL provider,
> the documented fallback is: reorder every module `DbContext.OnModelCreating` so
> `ApplyConfigurationsFromAssembly` runs **before** `MyBase.OnModelCreating`, and put
> the ignore-loop at the end of `BaseDbContext.OnModelCreating`. If you use the
> fallback, edit ALL module contexts (Purchasing, Inventory, POS, Accounting, plus
> any others) and confirm `ApplySoftDeleteFilters` still behaves correctly under the
> new order. Document which approach was used in the summary.

### 3. Reconcile the debug-branch hotfix

Branch `debug/PUR-savedraft-rowversion` already added an explicit
`builder.Ignore(Function(l) l.RowVersion)` to `PurchaseOrderLineConfiguration.vb`.
Once the central convention (Deliverable 2) lands, that explicit ignore is
**redundant but harmless**. Either:
- remove it for cleanliness (preferred — keep one mechanism), or
- leave it (the convention will no-op on an already-ignored property).

State the choice in the summary. Do **not** leave both mechanisms undocumented.

## Specification — verification matrix

The implementer must, after the change, confirm via `dotnet ef`-free runtime
introspection or a smoke run that each Group B table no longer has `RowVersion`
in its generated INSERT/UPDATE SQL, and each Group A table still does. A practical
check: exercise one EF write per module and confirm no `Unknown column 'RowVersion'`:
- Purchasing: create + receive a PO (PurchaseOrderLine, GoodsReceipt/Line).
- Inventory: add stock / record shrinkage (StockMovement, ShrinkageRecord).
- POS: complete a sale (SalesTransactionLine, OfficialReceipt, ReceiptIntegrity).
- Accounting: let the sale/goods-receipt handlers fire (RevenueRecord, ExpenseRecord).

## Acceptance Criteria

1. The 5 Group A entities missing `IsRowVersion()` now have it; all 15 (±UserAccount)
   Group A entities retain a mapped `RowVersion` concurrency token.
2. Every Group B entity has its `RowVersion` property ignored in the EF model
   (verified by absence of the column in generated SQL).
3. The fix is centralized (single convention/mechanism in `BaseDbContext`), not 19
   hand-written `Ignore` calls — except the pre-existing PurchaseOrderLine one, which
   is reconciled per Deliverable 3.
4. Smoke run: create+receive a PO, complete a sale, record shrinkage, and let the
   accounting handlers fire — **no** `Unknown column 'RowVersion'` errors.
5. Optimistic concurrency still works on a Group A entity (two-DbContext sequence
   still raises `DbUpdateConcurrencyException` — INFRA-26 AC #2 must not regress).
6. Build: 0 errors / 0 warnings.

## Out of Scope (Defer)

- The soft-delete vs unique-constraint reuse bug (PO-number regeneration after
  delete) → **INFRA-32**.
- Re-evaluating whether `AuditableEntity` should inherit `ConcurrencyAwareEntity`
  at all (a deeper model redesign). This plan deliberately keeps the inherited
  property and fixes the mapping instead, because concurrency-awareness cross-cuts
  the audit/soft-delete hierarchy. Revisit only if a future requirement demands it.
- Adding `RowVersion` columns to any Group B table (rejected — INFRA-26 intends
  these to be append-only/non-tracked).

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-31-summary.md` per `Progress/_template.md`. Include:

- Which mechanism was used (model-finalizing convention vs. reorder fallback) and why.
- The final Group A list (confirm all carry `IsRowVersion()`), including the
  `UserAccount` determination.
- Confirmation that all Group B entities are ignored, with the per-module smoke-run
  evidence (which write path was exercised, that it succeeded).
- The PurchaseOrderLine reconciliation decision (removed vs. kept).
- Confirmation that INFRA-26 AC #2 (concurrency exception) did not regress.

### Documentation

- XML doc on the new convention explaining the rule ("`RowVersion` is mapped only
  where the table physically has the column, signalled by `IsRowVersion()`").
- If the fallback reorder was used, an inline comment in each module `DbContext`
  explaining why configs must be applied before the base hook.
