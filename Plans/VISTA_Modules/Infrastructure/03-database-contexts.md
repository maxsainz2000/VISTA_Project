---
module: Infrastructure
plan-id: INFRA-03
title: "Database Contexts"
depends-on: [INFRA-01, INFRA-02]
estimated-files: 8
---

# Database Contexts

## Context

VISTA uses **one DbContext per module** to enforce data isolation. Each module owns its tables — no cross-module EF navigation properties. All DbContexts connect to a local SQLite database for offline-first operation. This plan creates the four module DbContexts, a shared base DbContext, and the automatic audit column + soft-delete infrastructure.

## Prerequisites

- **INFRA-01** (Solution Scaffold) — all module projects exist
- **INFRA-02** (Shared Kernel) — `AuditableEntity`, `SoftDeletableEntity`, `ISoftDeletable` exist

## Wiki References

- `analysis/tech-stack-reference.md` — "One DbContext per module", "SQLite", "3NF", audit columns, soft deletes
- `concepts/offline-first-sync.md` — SQLite as local always-available DB
- `concepts/modular-monolith.md` — "No shared EF DbContext"

## Deliverables

```
MerchSys.SharedKernel/
├── Data/
│   ├── BaseDbContext.vb                    ← Abstract base with audit/soft-delete logic
│   └── AuditInterceptor.vb                ← SaveChanges interceptor for audit columns

MerchSys.Purchasing/
└── Data/
    └── PurchasingDbContext.vb

MerchSys.Inventory/
└── Data/
    └── InventoryDbContext.vb

MerchSys.POS/
└── Data/
    └── POSDbContext.vb

MerchSys.Accounting/
└── Data/
    └── AccountingDbContext.vb

MerchSys.App/
└── Data/
    └── DatabaseConfig.vb                   ← SQLite connection string + DI registration
```

## Specification

### BaseDbContext (`SharedKernel/Data/BaseDbContext.vb`)

Abstract `DbContext` subclass that all module DbContexts inherit from:

```
Public MustInherit Class BaseDbContext
    Inherits DbContext

    ' Override OnModelCreating to:
    ' 1. Apply global query filter: entity.IsDeleted = False for all ISoftDeletable entities
    ' 2. Call ConfigureConventions to set default string max length (256)

    ' Override SaveChangesAsync to:
    ' 1. For Added entities implementing IAuditable:
    '    - Set CreatedAt = DateTime.UtcNow
    '    - Set CreatedBy = current user (passed via constructor or ambient)
    ' 2. For Modified entities implementing IAuditable:
    '    - Set ModifiedAt = DateTime.UtcNow
    '    - Set ModifiedBy = current user
    ' 3. For Deleted entities implementing ISoftDeletable:
    '    - Cancel the hard delete
    '    - Set IsDeleted = True, DeletedAt = DateTime.UtcNow, DeletedBy = current user
    '    - Change state from Deleted to Modified
End Class
```

### AuditInterceptor (`SharedKernel/Data/AuditInterceptor.vb`)

An EF Core `SaveChangesInterceptor` that handles the audit column population. This is an alternative approach — implement whichever pattern is cleaner. The key requirement is that audit columns are **automatically** populated on every save without manual code in services.

### Module DbContexts

Each module DbContext:
- Inherits from `BaseDbContext`
- Has **no DbSet properties yet** (those are added by each module's data-access plan)
- Configures SQLite as the provider
- Uses a shared SQLite database file: `merchsys.db` in the application's data directory
- Each context maps to a **table prefix** to avoid collisions:
  - Purchasing: `Pur_` prefix
  - Inventory: `Inv_` prefix
  - POS: `Pos_` prefix
  - Accounting: `Acc_` prefix

Example pattern for `PurchasingDbContext`:
```
Public Class PurchasingDbContext
    Inherits BaseDbContext

    Public Sub New(options As DbContextOptions(Of PurchasingDbContext))
        MyBase.New(options)
    End Sub

    Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
        MyBase.OnModelCreating(modelBuilder)
        ' Apply entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(GetType(PurchasingDbContext).Assembly)
    End Sub
End Class
```

### DatabaseConfig (`App/Data/DatabaseConfig.vb`)

A static helper class that:
1. Determines the SQLite database file path (`%LOCALAPPDATA%/MerchSys/merchsys.db`)
2. Creates the directory if it doesn't exist
3. Provides an extension method for `IServiceCollection` to register all four DbContexts with SQLite

```
' Example registration:
services.AddDbContext(Of PurchasingDbContext)(Sub(options)
    options.UseSqlite($"Data Source={dbPath}")
End Sub)
```

## Implementation Notes

- **Single SQLite file** shared by all modules — table prefixes prevent collisions
- The soft-delete query filter must be applied globally so that `context.Set(Of T).ToList()` automatically excludes deleted records
- To query deleted records (admin), use `.IgnoreQueryFilters()`
- The current user identity will be a simple string for now (hardcoded "Manager" placeholder) — proper auth is out of scope
- Each DbContext must accept `DbContextOptions(Of T)` in its constructor for DI

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors
2. All 4 module DbContexts inherit from `BaseDbContext`
3. Audit columns are auto-populated on `SaveChangesAsync` (verifiable by inspection)
4. Soft-delete query filter is configured globally
5. `DatabaseConfig` registers all 4 contexts with SQLite provider
6. Table prefix convention is documented in each DbContext

## Output Requirements

### Implementation Summary
After completing all code, create a progress report at:
```
Progress/VISTA_Modules/Infrastructure/INFRA-03-summary.md
```
Using the template structure from `Progress/_template.md`.

### Documentation
- XML doc comments on `BaseDbContext` explaining the audit + soft-delete behavior
- XML doc comments on `DatabaseConfig` explaining the SQLite path and registration
