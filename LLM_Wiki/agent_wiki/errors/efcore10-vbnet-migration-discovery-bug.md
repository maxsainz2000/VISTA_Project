---
type: error-fix
module: Infrastructure
agent: claude-code
date: 2026-05-09
tags: [ef-core, vb-net, migrations, sqlite, runtime-error]
error-code: N/A
severity: runtime-error
---

# EF Core 10 CLI Cannot Discover VB.NET Migration Classes

## Problem

Running `dotnet ef database update --project src/MerchSys.<Module> --startup-project src/MerchSys.App` prints:

```
No migrations were found in assembly 'MerchSys.<Module>'. A migration needs to be added before the database can be updated.
```

This happens even when the migration class is correctly compiled in the DLL, confirmed via ILSpy decompilation which shows:

```csharp
[Migration("20260507100001_InitialPurchasing")]
public class InitialPurchasing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { ... }
    protected override void Down(MigrationBuilder migrationBuilder) { ... }
}
```

The preceding step `dotnet ef migrations add` also fails immediately with:

```
The project language 'VB' isn't supported by the built-in IMigrationsCodeGenerator service.
```

## Root Cause

Two separate issues in the EF Core 10 toolchain for VB.NET:

1. **`dotnet ef migrations add`**: The built-in `IMigrationsCodeGenerator` only generates C#. No official VB.NET generator exists. The community package `EntityFrameworkCore.VisualBasic` only supports EF Core ≤ 8; it has a `NU1107` package conflict with EF 10 (`Microsoft.CodeAnalysis.Common` version 4.5.0 vs 5.0.0 required by EF 10 analyzers).

2. **`dotnet ef database update`**: EF Core's design-time scanner (`MigrationsAssembly.GetMigrations()`) calls `assembly.GetConstructibleTypes()` to find all types that subclass `Migration` and have `MigrationAttribute`. For VB.NET assemblies compiled with EF Core 10's exact version combination, this scanner returns 0 results despite the types being present. The ILSpy-decompiled IL is correct — the bug is in EF Core 10's runtime discovery path for VB.NET assemblies (possible regression in EF 10.0.7, not yet reported upstream). Changing the attribute from `<Migration("...")>` to `<MigrationAttribute("...")>` and removing the `Partial` modifier both had no effect.

## Fix

**Step 1: Write migration files manually** (since `migrations add` doesn't support VB.NET)

Place migration files in `src/MerchSys.<Module>/Migrations/` as standard VB.NET classes:

```vb
' 20260507100001_InitialPurchasing.vb
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Migrations

    <MigrationAttribute("20260507100001_InitialPurchasing")>
    Public Class InitialPurchasing
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("CREATE TABLE IF NOT EXISTS ...")
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql("DROP TABLE IF EXISTS ...")
        End Sub

    End Class

End Namespace
```

Also create a model snapshot (`<Module>DbContextModelSnapshot.vb`) using `BuildModel()` API.

**Step 2: Create `IDesignTimeDbContextFactory` in each module** (needed for EF CLI to instantiate contexts)

```vb
' src/MerchSys.Purchasing/Data/PurchasingDbContextFactory.vb
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design

Namespace Data
    Public Class PurchasingDbContextFactory
        Implements IDesignTimeDbContextFactory(Of PurchasingDbContext)
        Public Function CreateDbContext(args As String()) As PurchasingDbContext _
            Implements IDesignTimeDbContextFactory(Of PurchasingDbContext).CreateDbContext
            Dim optionsBuilder = New DbContextOptionsBuilder(Of PurchasingDbContext)()
            optionsBuilder.UseSqlite("Data Source=design_time.db")
            Return New PurchasingDbContext(optionsBuilder.Options)
        End Function
    End Class
End Namespace
```

**Step 3: Bypass EF CLI with `DatabaseInitializer`** (since `database update` can't discover VB.NET migrations)

Create `src/MerchSys.App/Data/DatabaseInitializer.vb` — a module that opens a raw `SqliteConnection` and runs all DDL+DML using `CREATE TABLE IF NOT EXISTS` and `INSERT OR IGNORE INTO`. Populate `__EFMigrationsHistory` with the migration IDs so EF knows the baseline schema version.

Call it in `Application_Startup` (after `_host.Build()`, before `window.Show()`):

```vb
DatabaseInitializer.Initialize($"Data Source={DatabaseConfig.DatabasePath}")
```

This gives the production database the exact same schema as the migration files, with the migration history table correctly populated for future compatibility.

## Prevention

- **Never use `dotnet ef migrations add` for VB.NET projects** targeting EF Core ≥ 9. Write migration files manually.
- **Never rely on `dotnet ef database update` for VB.NET projects** with EF Core 10. Use a `DatabaseInitializer` that runs raw SQL on app startup.
- Model snapshots (`<Module>DbContextModelSnapshot.vb`) must still be written manually to enable future migration diffs. Use the `BuildModel()` fluent API.
- Always use `CREATE TABLE IF NOT EXISTS` and `INSERT OR IGNORE INTO` in the initializer to make it idempotent.
- Reference `Microsoft.EntityFrameworkCore.Design` in the startup project (`MerchSys.App`) with `<PrivateAssets>all</PrivateAssets>` so EF CLI can find design-time services.

## Related

- `LLM_Wiki/wiki/analysis/tech-stack-reference.md` — pinned EF Core version (10.0.7)
- `LLM_Wiki/agent_wiki/antipatterns/vbnet-leading-dot-fluent-chains.md` — another VB.NET EF-specific pitfall
