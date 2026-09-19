---
module: MerchSys.Infrastructure
plan-id: INFRA-22
title: "Central MariaDB Schema + Sync Map for Inv_SaleCogs"
depends-on: [INFRA-06, INFRA-12, ACC-21]
estimated-files: 3
priority: medium
---

# Central MariaDB Schema + Sync Map for Inv_SaleCogs

## Context

ACC-21 (2026-05-27) introduced the `Inv_SaleCogs` ledger as the authoritative per-batch FIFO COGS record for every sale. It is written by `MerchSys.Inventory/Handlers/SaleCompletedHandler.PersistCogsBreakdownAsync` and queried by `MerchSys.Accounting/Handlers/SaleRevenueHandler` via `GetSaleCogsBreakdownQuery`.

Gaps discovered during the 2026-05-27 MariaDB factory-reset audit:

1. **No central MariaDB table** for `Inv_SaleCogs`. The local SQLite migration (`DatabaseInitializer.ApplyInvSaleCogs` and `MerchSys.Inventory/Migrations/20260527120000_AddInvSaleCogs.vb`) creates the table only on the local DB. `merchsys_central` has no equivalent.
2. **No sync-map registration.** `InventorySyncMap.Tables` lists seven Inv_* tables but **not** `Inv_SaleCogs`. `ToRemote` has no case for the table. Any journal row keyed on `Inv_SaleCogs` would currently fall through to `Case Else → Nothing` and be silently dropped (or surface as a `SyncOrchestrator` warning, depending on caller handling).
3. **`ISyncableRepository.SaveChangesWithJournalAsync`** in `SaleCompletedHandler` already routes Inv_SaleCogs writes through the journal pipeline — so journal rows are being produced today and have nowhere to land on the central DB. They sit in `Sync_Journal` unsent (or are sent and rejected).

If `Inv_SaleCogs` is meant to be a single-station local-only ledger, the cleanest fix is to mark it `<NoSync>` so journal rows are never produced. But that contradicts ACC-21's role as the **authoritative** post-hoc COGS audit: any recovery-machine read by Owner (deferred-backlog item 10 closure cited this as a future requirement) would lack the per-batch breakdown and produce wrong reports. Promoting `Inv_SaleCogs` to a syncable table preserves the option and matches the design intent of `Acc_RevenueRecords` (which **is** synced).

This plan adds the central schema, registers the table in the sync map, and aligns the canonical remote POCO shape.

## Prerequisites

- **INFRA-06** (MariaDB Central Schema) — `mariadb-init.sql`
- **INFRA-12** (Sync Orchestrator Transmission) — `SyncOrchestrator`, `InventorySyncMap` pattern
- **ACC-21** (Per-Batch FIFO COGS Accuracy) — `SaleCogsRecord` entity, local schema

## Wiki References

- `concepts/offline-first-sync.md` — Local and central schemas must be aligned for sync
- `concepts/fifo-costing.md` — Per-batch costing is the design contract
- `analysis/cross-module-data-flow.md` — Sync ownership: Inventory owns `Inv_*`

## Deliverables

```
Plans/VISTA_Modules/Infrastructure/sql/
└── mariadb-inv-salecogs-schema.sql                     ' NEW — CREATE TABLE + indexes

MerchSys.Infrastructure/Data/Migrations/Central/
└── AddInvSaleCogs.sql                                  ' NEW — versioned migration script for existing deploys

MerchSys.SharedKernel/Sync/SyncMaps/
└── InventorySyncMap.vb                                 ' MOD — add RemoteSaleCogs POCO, Tables entry, ToRemote case
```

No code changes inside `MerchSys.Inventory` itself — the local entity and configuration already exist from ACC-21. No `mariadb-init.sql` exists in the repo today (it was hand-built per the 2026-05-27 audit); when one is created in a future INFRA plan it must include this table.

## Specification

### Central MariaDB schema

```sql
CREATE TABLE IF NOT EXISTS `Inv_SaleCogs` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `TransactionId` INT NOT NULL,
    `ProductId` INT NOT NULL,
    `BatchId` INT NOT NULL,
    `QuantityDeducted` INT NOT NULL,
    `UnitCost` DECIMAL(18, 4) NOT NULL,
    `Cogs` DECIMAL(18, 4) NOT NULL,
    `DeductedAt` DATETIME(6) NOT NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_Inv_SaleCogs_Tx_Product` (`TransactionId`, `ProductId`),
    INDEX `IX_Inv_SaleCogs_Batch` (`BatchId`),
    CONSTRAINT `FK_Inv_SaleCogs_Inv_StockBatches_BatchId`
        FOREIGN KEY (`BatchId`) REFERENCES `Inv_StockBatches` (`Id`)
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

Match the local SQLite shape exactly:
- `UnitCost` and `Cogs` use `DECIMAL(18, 4)` — same precision as the EF configuration in `SaleCogsRecordConfiguration.vb:15-16`.
- `DeductedAt` uses `DATETIME(6)` for microsecond precision (consistent with INFRA-14's pattern for `Pos_OfficialReceipts.IssuedAt`).
- FK to `Inv_StockBatches(Id)` with `RESTRICT` — matches the local FK from the EF configuration.
- `utf8mb4_unicode_ci` collation matches the rest of `merchsys_central`'s Inv_* tables.

### `AddInvSaleCogs.sql` migration script

A standalone, idempotent script for existing MariaDB deployments. Same `CREATE TABLE IF NOT EXISTS` as above. Header comment:

```sql
-- INFRA-22: Central MariaDB schema for Inv_SaleCogs (per-batch FIFO COGS ledger).
-- Companion to ACC-21's local SQLite migration 20260527120000_AddInvSaleCogs.vb.
-- Idempotent — safe to re-run.
```

### `InventorySyncMap.vb` modifications

Add a new remote POCO:

```vb
Public Class RemoteSaleCogs
    Public Property Id As Integer
    Public Property TransactionId As Integer
    Public Property ProductId As Integer
    Public Property BatchId As Integer
    Public Property QuantityDeducted As Integer
    Public Property UnitCost As Decimal
    Public Property Cogs As Decimal
    Public Property DeductedAt As DateTime
End Class
```

No audit-column properties (`CreatedBy`/`CreatedAt`/`ModifiedBy`/`ModifiedAt`) because `SaleCogsRecord` inherits `BaseEntity`, not `AuditableEntity` — append-only design intent from ACC-21.

Append `"Inv_SaleCogs"` to `InventorySyncMap.Tables`:

```vb
Public Shared ReadOnly Property Tables As IReadOnlyList(Of String) = New String() {
    "Inv_ProductCategories", "Inv_Products", "Inv_StockBatches",
    "Inv_ShrinkageRecords", "Inv_StockAlertConfigs", "Inv_StockMovements",
    "Inv_StockAuditRecords", "Inv_SaleCogs"
}
```

Add a case to `ToRemote`:

```vb
Case "Inv_SaleCogs"
    Return JsonSerializer.Deserialize(Of RemoteSaleCogs)(entry.Payload, _options)
```

### Conflict policy

`Inv_SaleCogs` is **append-only** by ACC-21's design — there is no UPDATE code path. The default `LastWriteWins` policy that the rest of Inventory uses is therefore moot; in practice every row arrives as an INSERT. Document this in the class comment and do not add a special-case in `ConflictResolver` — append-only is enforced upstream.

### Idempotency on re-sync

If a `Sync_Journal` row for `Inv_SaleCogs` arrives twice (e.g., a transmitter retry after a transient network failure), the central INSERT will fail on the PRIMARY KEY (`Id`). `SyncOrchestrator`'s existing retry handling treats PK violations as "already applied" — verify this is true for the current orchestrator and document; do not weaken the constraint to handle a malformed retry.

## Implementation Notes

- The local EF migration class already has both an `Up` and `Down` method. The central script in this plan is one-way (`Up` only) because we never want to drop the table in production. The local `Down` exists for development convenience.
- No `MariaDb` schema initializer is invoked by the app today; the central schema is hand-maintained. Until that changes (likely a future INFRA plan to introduce automated central migrations), `AddInvSaleCogs.sql` must be applied manually to every MariaDB deployment.
- **VB.NET trap — `entry` shadow:** the existing `InventorySyncMap.ToRemote(entry As SyncJournal)` parameter is named `entry`. This is fine here because the method is not on a DbContext — the `entry`/`DbContext.Entry()` shadow trap from CLAUDE.md applies only inside DbContext subclasses. Leave the parameter name as-is.
- **Verify `Sync_Journal` rows already exist for past `Inv_SaleCogs` writes** before this plan ships. If they do, transmission of those backlog rows will succeed automatically once the central table exists. If pre-INFRA-22 journal rows surfaced errors, document them in the Progress summary.

## Acceptance Criteria

1. `mariadb-inv-salecogs-schema.sql` applies cleanly against the existing `merchsys_central` MariaDB instance and is idempotent.
2. After the schema is applied, `SHOW CREATE TABLE Inv_SaleCogs;` on MariaDB shows the same column types, FK, and indexes as the local SQLite `Inv_SaleCogs`.
3. `InventorySyncMap.Tables` includes `"Inv_SaleCogs"` and `ToRemote` deserializes a sample journal payload into a `RemoteSaleCogs` instance with all eight properties populated.
4. After a fresh sale (3-batch FIFO span, e.g., the ACC-21 reproduction case), three rows appear in **both** local `Inv_SaleCogs` **and** central `Inv_SaleCogs` after the sync cycle completes.
5. Build clean: 0 errors / 0 warnings.
6. Re-running `AddInvSaleCogs.sql` against a DB that already has the table produces no errors (idempotent).

## Out of Scope (Defer)

- **Automated central-DB migration runner.** Adding the table by hand for now matches the project's current operational posture (no `mariadb-init.sql` exists in the repo). A future INFRA plan can introduce a versioned central-migration runner that consumes `MerchSys.Infrastructure/Data/Migrations/Central/*.sql` files at startup.
- **Backfill of pre-ACC-21 sales.** Historical sales committed before ACC-21 shipped have no local `Inv_SaleCogs` rows and therefore nothing to sync. Backfill is out of scope per ACC-21's own out-of-scope note.
- **Immutability triggers** on the central `Inv_SaleCogs` table (mirroring ACC-15's pattern for `Acc_TamperAuditLog`). Out of scope unless requirements escalate; `Inv_SaleCogs` is operational data, not BIR-tamper-protected.
- **Cross-batch COGS attribution for returns/refunds.** Already deferred by ACC-21; whichever plan introduces the POS return path must also handle central-DB negative-quantity sync.

## Output Requirements

### Implementation Summary
Create at `Progress/VISTA_Modules/Infrastructure/INFRA-22-summary.md` using `Progress/_template.md`. Include:

- `SHOW CREATE TABLE Inv_SaleCogs;` output from MariaDB after applying the script, confirming column types, FK, and indexes.
- A `git diff` excerpt of the `InventorySyncMap.vb` additions (POCO + Tables entry + ToRemote case).
- A SQLite + MariaDB row count comparison after running the ACC-21 reproduction sale, confirming `local.Inv_SaleCogs.COUNT(*) = central.Inv_SaleCogs.COUNT(*)` after the sync cycle.
- A note confirming the idempotency property: running the SQL script a second time produces no errors and no schema drift.
- If any pre-INFRA-22 `Sync_Journal` rows existed for `Inv_SaleCogs` and were rejected before this plan, list their journal IDs in a `## Backlog Rows Drained` section.

### Documentation
- Header comment in `mariadb-inv-salecogs-schema.sql` referencing INFRA-22, ACC-21, and the local migration filename.
- XML doc on `RemoteSaleCogs` describing its append-only nature and citing ACC-21 + INFRA-22.
- Inline comment in `InventorySyncMap` next to the new Tables entry: `' INFRA-22: Inv_SaleCogs is append-only — LastWriteWins is moot in practice.`
