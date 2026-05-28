---
type: error-fix
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-20
tags: [ef-core, vb-net, sqlite, mariadb, runtime-error, materialization, tolistasync]
error-code: (none — silent empty result, no exception)
severity: runtime-error
status: partially-historical
post-pivot-note: 2026-05-28
---

> **⚠️ Post-pivot note (2026-05-28).** This is a VB.NET ↔ EF Core 10 materialization bug, not a SQLite bug — the same failure mode is expected against the MariaDB-only architecture. The raw-`SqliteConnection` workaround pattern carries over; substitute `MySqlConnector.MySqlConnection` and use a synchronous `reader.Read()` loop writing to a class field. Re-validate this bug against MariaDB during INFRA-24 and update this entry with confirmed reproduction status.

# EF Core 10 VB.NET — ToListAsync silently returns empty list for full entity queries

## Problem

`_db.Vendors.ToListAsync()` (and all variants: `AsNoTracking()`, `IgnoreQueryFilters()`,
`Where(...).ToListAsync()`) return an empty `List(Of Vendor)` at runtime. No exception
is thrown. The generated SQL is correct; raw ADO.NET on the same connection returns the
expected rows.

Diagnostic evidence that isolates the failure to EF's entity materializer:

| Query form | Result |
|---|---|
| `CountAsync()` | ✅ correct count |
| `Select(Function(v) v.Id).ToListAsync()` | ✅ correct IDs |
| `ToListAsync()` (full entity) | ❌ empty list, no exception |
| Raw `SqliteCommand` + `reader.Read()` | ✅ correct rows |

## Root Cause

EF Core 10's entity materializer generates compiled delegates that construct entity
objects from the `DbDataReader`. In VB.NET on .NET 10, this compiled delegate silently
returns no results for full entity projections, while scalar projections and aggregate
functions continue to work. This appears to be a VB.NET-specific issue with EF Core 10's
compiled model or materializer; C# projects are unaffected.

The exact internal failure point is within EF Core's reflection-based or compiled
materializer infrastructure and is not surfaced as a user-visible exception.

## Fix

Bypass EF's materializer for the affected query by opening a fresh `SqliteConnection`
directly and reading rows via `SqliteDataReader`. Write to a class field (not a local
`As New` variable) inside the reader loop to ensure persistence across async state
machine resume points.

```vb
' Before (broken — ToListAsync returns empty)
_vendorList = Await _db.Vendors.AsNoTracking().IgnoreQueryFilters().ToListAsync()

' After (fixed — raw SqliteConnection bypasses EF materializer)
_vendorList = New List(Of Vendor)()
Dim connStr = _db.Database.GetConnectionString()
Using conn As New SqliteConnection(connStr)
    Await conn.OpenAsync()
    Using cmd = conn.CreateCommand()
        cmd.CommandText = "SELECT Id, Name, ContactPerson, Phone, Email, " &
                          "Address, DefaultLeadTimeDays, Notes " &
                          "FROM Pur_Vendors WHERE IsDeleted = 0 ORDER BY Name"
        Using reader = cmd.ExecuteReader()   ' synchronous Read() — not ReadAsync()
            While reader.Read()
                _vendorList.Add(New Vendor With {
                    .Id = reader.GetInt32(0),
                    .Name = reader.GetString(1),
                    .ContactPerson = reader.GetString(2),
                    .Phone = reader.GetString(3),
                    .Email = If(reader.IsDBNull(4), Nothing, reader.GetString(4)),
                    .Address = reader.GetString(5),
                    .DefaultLeadTimeDays = reader.GetInt32(6),
                    .Notes = If(reader.IsDBNull(7), Nothing, reader.GetString(7))
                })
            End While
        End Using
    End Using
End Using
```

Key details:
- Use `New SqliteConnection(connStr)` — a completely fresh connection, not `_db.Database.GetDbConnection()` (EF's managed connection has unpredictable open/close lifecycle).
- Use synchronous `cmd.ExecuteReader()` + `reader.Read()`, not the async variants (`ReadAsync()` also showed 0 results in this context).
- Write results to a **class field** (`_vendorList`), not a local `Dim x As New List(Of Vendor)()`. Local `As New` initializations declared before an `Await` can behave unexpectedly in VB.NET async state machines.
- Requires `Imports Microsoft.Data.Sqlite` (available as a transitive dependency of `Microsoft.EntityFrameworkCore.Sqlite`).

## Prevention

- When loading full entity lists via EF Core 10 in a VB.NET module, verify results with
  `CountAsync()` first. If count > 0 but `ToListAsync()` returns empty, apply the raw
  `SqliteConnection` workaround above.
- Scalar projections (`Select(Function(e) e.Id).ToListAsync()`) and aggregates
  (`CountAsync()`, `SumAsync()`) are not affected — only full entity materialization.

## Detector Contract

> Added 2026-05-24 after the agent-wiki audit (`Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md`)
> flagged 63 sites for this rule, of which ~25 were false positives matched by literal `.ToListAsync(`
> grep against query shapes the rule itself excludes. See
> `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md`.

Any audit that detects this rule MUST match the **shape** of the LINQ chain, not the literal
`.ToListAsync(` token. The bug affects **full-entity materialisation only**.

### Flag (positive patterns)

The chain ends in `.ToListAsync(...)` and the last call before `.ToListAsync` is one of:

- a bare `DbSet` reference (`_db.Vendors.ToListAsync()`)
- `.Where(...)`, `.OrderBy(...)`, `.OrderByDescending(...)`, `.ThenBy(...)`, `.ThenByDescending(...)`
- `.Include(...)`, `.ThenInclude(...)`
- `.AsNoTracking()`, `.IgnoreQueryFilters()`
- `.Take(...)`, `.Skip(...)`

### Do NOT flag (negative patterns — wiki-excluded)

- `.Select(Function(x) New With { ... }).ToListAsync()` — anonymous-type projection
- `.Select(Function(x) x.ScalarMember).ToListAsync()` — scalar projection
- `.GroupBy(...).Select(...).ToListAsync()` — group projection
- Any chain containing `.Select(...)` between the last `.Where`/`.Include`/`DbSet` and `.ToListAsync`
- `.CountAsync()`, `.SumAsync()`, `.AnyAsync()`, `.FirstOrDefaultAsync()` — different terminator

The corpus that exercises these patterns lives at `Operator/audit-tests/rule-03/` (see INFRA-18).

## Related

- `[[efcore10-vbnet-migration-discovery-bug]]` — another EF Core 10 + VB.NET incompatibility
- `[[efcore-hasdefaultvalue-enum-type-mismatch]]` — related EF Core configuration pitfall
