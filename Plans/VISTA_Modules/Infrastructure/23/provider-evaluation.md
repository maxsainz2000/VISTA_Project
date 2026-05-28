# INFRA-23: EF Core 10 ↔ MariaDB Provider Evaluation

This document evaluates candidate ORM providers for VISTA's transition to a centralized MariaDB 11.4.x database.

## Candidates Evaluated

### 1. Oracle's Official `MySql.EntityFrameworkCore` (10.0.7)
*   **Description:** Oracle's official Entity Framework Core provider for MySQL/MariaDB.
*   **Pros:** Native and explicit support for EF Core 10.0.x and .NET 10 out-of-the-box. Fully compatible with basic CRUD, LINQ queries, transactional pessimistic locking (`FOR UPDATE`), and optimistic concurrency using `RowVersion TIMESTAMP(6)`.
*   **Cons:** Historically has fewer community contributions than Pomelo, but it is currently actively maintained by Oracle.
*   **Verdict:** **CHOSEN**. Meets all decision criteria flawlessly with zero runtime warning flags.

### 2. Pomelo's `Pomelo.EntityFrameworkCore.MySql` (9.0.0)
*   **Description:** Community-driven provider for MySQL/MariaDB.
*   **Pros:** Highly popular, historically very robust LINQ translation.
*   **Cons:** Severely blocked by binary incompatibilities with EF Core 10 (crashes at runtime with `MissingMethodException` on `AbstractionsStrings.ArgumentIsEmpty` due to breaking changes in EF Core 10 abstractions). No stable 10.x release exists yet.
*   **Verdict:** **REJECTED**. Blocked by EF Core 10 runtime crash.

### 3. EF Core 9.x Downgrade + Pomelo 9.0.0
*   **Description:** Downgrading the system's entire EF Core stack to 9.x.
*   **Pros:** Resolves Pomelo binary incompatibilities.
*   **Cons:** Loses important EF Core 10 features, requires downgrading all module and shared libraries, and goes against the project's .NET 10/EF Core 10 target mandate.
*   **Verdict:** **REJECTED**. Unnecessary downgrade effort given Oracle provider availability.

### 4. Raw `MySqlConnector` only (Nuclear Option)
*   **Description:** Bypassing EF Core altogether and rewriting the entire data access layer with ADO.NET.
*   **Pros:** Utterly immune to EF Core version incompatibilities.
*   **Cons:** Requires rewriting hundreds of LINQ queries, entities, and tracking handlers. Pervasive change to the codebase.
*   **Verdict:** **REJECTED**. Nuclear option; only to be used as a last resort.

## Decision Matrix

| Capability | Oracle `MySql.EntityFrameworkCore` 10.0.7 | Pomelo 9.0.0 | EF Core 9.x Downgrade | Raw MySqlConnector |
|---|---|---|---|---|
| Compiles against EF Core 10 | **YES** | **YES** (with warning) | NO | **YES** |
| Connects to MariaDB 11.4.x | **YES** | **YES** | **YES** | **YES** |
| Basic CRUD round-trip | **YES** | NO (crash) | **YES** | **YES** |
| LINQ `Where` + `OrderBy` | **YES** | NO (crash) | **YES** | NO (requires manual SQL) |
| `Include` (nav property load) | **YES** | NO (crash) | **YES** | NO (requires manual SQL joins) |
| `ToListAsync()` on full entity | **YES** (fixed) | NO (crash) | **YES** | **YES** |
| `RowVersion` / `TIMESTAMP(6)` | **YES** | **YES** | **YES** | NO (manual SQL) |
| `BeginTransactionAsync` + `FOR UPDATE` | **YES** | NO (crash) | **YES** | **YES** |
| NuGet vulnerability flags | **NONE** | **NONE** | **NONE** | **NONE** |
| Active maintenance (last 12 mo) | **YES** | NO (stale) | **YES** | **YES** |

## Conclusion
We proceed with **`MySql.EntityFrameworkCore` version 10.0.7** as the official VISTA MariaDB provider.
