---
module: MerchSys.Infrastructure
plan-id: INFRA-25
title: "Convert Module DbContexts from SQLite to MariaDB"
depends-on: [INFRA-23, INFRA-24]
estimated-files: 15
priority: critical
amendment-ref: AMD-2026-05-28-01
---

# INFRA-25: Convert Module DbContexts from SQLite to MariaDB

## Context

With the EF Core ↔ MariaDB provider chosen (INFRA-23) and the MariaDB schema bootstrap in place (INFRA-24), every module `DbContext` must be repointed from `UseSqlite(...)` to the MariaDB provider against the centralized `merchsys_central` instance.

Affected contexts:
- `MerchSys.Purchasing/Data/PurchasingDbContext.vb`
- `MerchSys.Inventory/Data/InventoryDbContext.vb`
- `MerchSys.POS/Data/PosDbContext.vb`
- `MerchSys.Accounting/Data/AccountingDbContext.vb`

Plus the design-time factories (`*DbContextFactory.vb`) in each module, the DI registrations in `MerchSys.App/Startup/`, and `appsettings.json`.

This plan **does not** delete the sync infrastructure — that's INFRA-27. The sync code can continue to exist as dead code during this plan; it just no longer has a SQLite source to pull from. To avoid confusion, the `SyncWorker` is **disabled** (DI registration commented out) during this plan and fully removed in INFRA-27.

## Prerequisites

- **INFRA-23** — EF Core ↔ MariaDB provider chosen, version pinned, connection string template documented.
- **INFRA-24** — MariaDB schema exists and bootstraps cleanly on app startup.

## Wiki References

- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md`
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md`
- `Progress/VISTA_Modules/Infrastructure/INFRA-23-summary.md` — provider name + version
- `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md`

## Deliverables

```
MerchSys.Purchasing/Data/PurchasingDbContext.vb           ' MOD — UseSqlite → UseMySql/UseMySQL (per INFRA-23)
MerchSys.Inventory/Data/InventoryDbContext.vb             ' MOD
MerchSys.POS/Data/PosDbContext.vb                         ' MOD
MerchSys.Accounting/Data/AccountingDbContext.vb           ' MOD

MerchSys.Purchasing/Data/PurchasingDbContextFactory.vb    ' MOD — design-time factory uses MariaDB
MerchSys.Inventory/Data/InventoryDbContextFactory.vb      ' MOD
MerchSys.POS/Data/PosDbContextFactory.vb                  ' MOD
MerchSys.Accounting/Data/AccountingDbContextFactory.vb    ' MOD

MerchSys.App/Startup/PurchasingRegistration.vb            ' MOD — AddDbContext options
MerchSys.App/Startup/InventoryRegistration.vb             ' MOD
MerchSys.App/Startup/PosRegistration.vb                   ' MOD
MerchSys.App/Startup/AccountingRegistration.vb            ' MOD

MerchSys.App/appsettings.json                             ' MOD — ConnectionStrings:MerchSysCentral
MerchSys.App/Startup/SyncConfig.vb                        ' MOD — disable SyncWorker registration (commented)

MerchSys.Purchasing/MerchSys.Purchasing.vbproj            ' MOD — remove Sqlite package, add MariaDB provider
MerchSys.Inventory/MerchSys.Inventory.vbproj              ' MOD
MerchSys.POS/MerchSys.POS.vbproj                          ' MOD
MerchSys.Accounting/MerchSys.Accounting.vbproj            ' MOD
```

## Specification

### Connection string

All four contexts read the same connection string `ConnectionStrings:MerchSysCentral` from `appsettings.json`. Single source of truth.

### `OnConfiguring` removal

The current SQLite contexts likely have an `OnConfiguring` override hardcoding the SQLite path for design-time. Remove all hardcoded connection strings from `OnConfiguring`. Design-time factories supply their own.

### Per-context module wiring

In each `Startup/<Module>Registration.vb`:

```vb
Public Module InventoryRegistration
    Public Sub Register(services As IServiceCollection, configuration As IConfiguration)
        Dim connStr = configuration.GetConnectionString("MerchSysCentral")
        services.AddDbContext(Of InventoryDbContext)(
            Sub(options)
                options.UseMySql(connStr, ServerVersion.AutoDetect(connStr))   ' provider-dependent — see INFRA-23
                ' OR: options.UseMySQL(connStr) — Oracle provider
            End Sub)
        ' ... handler/service registrations unchanged
    End Sub
End Module
```

The exact `Use*` extension method comes from INFRA-23's decision. Reference the resolved package name explicitly in the implementation summary.

### Design-time factory

```vb
Public Class InventoryDbContextFactory
    Implements IDesignTimeDbContextFactory(Of InventoryDbContext)

    Public Function CreateDbContext(args As String()) As InventoryDbContext _
        Implements IDesignTimeDbContextFactory(Of InventoryDbContext).CreateDbContext
        Dim optionsBuilder = New DbContextOptionsBuilder(Of InventoryDbContext)()
        ' Design-time only — does NOT need to hit a live DB.
        ' Provide a syntactically valid MariaDB connection string with a known server version.
        optionsBuilder.UseMySql("Server=localhost;Database=design_time;User Id=root;",
                                New MySqlServerVersion(New Version(11, 4, 0)))
        Return New InventoryDbContext(optionsBuilder.Options)
    End Function
End Class
```

### Existing entity configurations

Every `*Configuration.vb` (`SaleCogsRecordConfiguration.vb`, etc.) should compile unchanged against the new provider. **Verify** any uses of SQLite-specific column-type strings (`TEXT`, `INTEGER`, `REAL`) and replace with provider-agnostic forms or MariaDB equivalents:

- `HasColumnType("TEXT")` → omit (default `VARCHAR`/`LONGTEXT`) or specify `VARCHAR(N)` explicitly
- `HasColumnType("INTEGER")` → omit (default `INT`) or `BIGINT`
- `HasColumnType("REAL")` → `DECIMAL(18,4)`
- `HasColumnType("BLOB")` → `LONGBLOB`

`DECIMAL(18, 4)` already used in configurations like `SaleCogsRecordConfiguration` carries over verbatim.

### `Pos_OfficialReceipts.IssuedAt` microsecond precision

Existing config sets `DATETIME(6)` via `HasColumnType("DATETIME(6)")`. Keep this — it works against MariaDB natively (was a SQLite-Mariadb-compat hack).

### Package changes

Remove from each module `.vbproj`:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.7" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="..." />
```

Add (exact package per INFRA-23):
```xml
<PackageReference Include="<ChosenProvider>" Version="<PinnedVersion>" />
```

`MerchSys.App` keeps `MySqlConnector` for the schema bootstrap (INFRA-24).

### Disabling SyncWorker

In `MerchSys.App/Startup/SyncConfig.vb`, comment out the `SyncWorker` `IHostedService` registration with a `' INFRA-25: SyncWorker disabled — sync layer scheduled for removal in INFRA-27` note. Do **not** delete the code yet; INFRA-27 handles deletion.

Also comment out (or skip) `SyncJournalDbContext` registration and the `SyncableRepositoryRegistration` call. With SQLite gone, those contexts have no DB to point at.

### `Sync_Journal` rows generated by handlers

Module handlers (`SaleCompletedHandler.PersistCogsBreakdownAsync` etc.) currently call `ISyncableRepository.SaveChangesWithJournalAsync`. This plan does **not** change those calls — they'll fail at runtime until INFRA-27 replaces them with `SaveChangesAsync`. To prevent runtime crashes during this transitional plan, `SyncableRepositoryCore.SaveChangesWithJournalAsync` is temporarily rewritten to a no-op pass-through to `_db.SaveChangesAsync()` (one-line change in `SyncableRepositoryCore.vb`). INFRA-27 will remove the interface and all call sites.

### Smoke test

After implementation, manual launch + click through:
1. App starts, MariaDB schema bootstrap from INFRA-24 runs, no errors.
2. Login as `manager` works (writes session record? if applicable).
3. Stock Dashboard loads 20 seeded products from MariaDB.
4. Create a PO → save → reload from MariaDB confirms persistence.
5. Sales cart → checkout → OR issued → Stock Dashboard reflects decrement.

These are smoke tests, not formal acceptance. The full operator verification checklist re-runs after INFRA-29 (concurrency) and INFRA-30 (UI).

## Acceptance Criteria

1. All four module `.vbproj`s have **no** SQLite package references.
2. All four `DbContext`s configure against MariaDB via the INFRA-23 provider.
3. `appsettings.json` contains a single `ConnectionStrings:MerchSysCentral` entry; no hardcoded SQLite paths anywhere outside the (still-existing-but-dead) sync code.
4. Build: 0 errors / 0 warnings.
5. App launches against a fresh MariaDB (bootstrapped by INFRA-24) and the smoke test list above passes.
6. `SyncWorker` no longer starts (no log entries from it during a 60s idle).
7. No runtime exception attributable to a missing SQLite file or connection.

## Out of Scope (Defer)

- Deleting sync infrastructure code (`SyncOrchestrator`, `SyncWorker`, transmitter, maps, journal context, etc.) → **INFRA-27**.
- Optimistic concurrency tokens on entity classes (the schema already has the columns from INFRA-24) → **INFRA-26**.
- Connection-status UI in the shell → **INFRA-28**.
- Tearing out the local SQLite `DatabaseInitializer.vb` and per-module `Migrations/` folders → **INFRA-27**.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-25-summary.md` per `Progress/_template.md`. Include:

- Diff summary of each `.vbproj` (Sqlite removed, MariaDB provider added).
- Diff summary of each `DbContext` (provider switch).
- `appsettings.json` content (with password redacted).
- Smoke-test transcript: 5 steps with timestamps.
- Confirmation that `SyncWorker` is disabled (no startup log line).
- Any provider-specific gotchas encountered (e.g., `ServerVersion.AutoDetect` vs explicit `MySqlServerVersion`).

### Documentation

- Inline comment in each `*Registration.vb` noting "INFRA-25: switched from SQLite to MariaDB; provider per INFRA-23".
- Comment in `SyncConfig.vb` explaining why `SyncWorker` is commented out (INFRA-25 transitional; full deletion in INFRA-27).
- Comment in `SyncableRepositoryCore.vb` flagging the temporary no-op nature of `SaveChangesWithJournalAsync`.
