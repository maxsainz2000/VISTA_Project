# Jules AI Instructions for VISTA_Project

## CRITICAL RULE: Windows Forms ONLY (WPF Migration in Progress)
- **DO NOT USE WPF.** Per the professor's strict instructions, this project must use **Windows Forms (MVP)** exclusively. Any existing WPF code or folders must be migrated to or replaced by Windows Forms equivalents. Do not generate XAML or WPF window classes.
- **WARNING - OUTDATED SPECS:** The `Specs/` folder and `README.md` are currently written for WPF/MVVM. Do NOT follow the UI instructions in the Specs folder literally; you must translate any XAML/WPF/MVVM instructions into WinForms/MVP on the fly while we work on rewriting the specs.

## Architecture
- **Language**: Visual Basic .NET 10 (VB.NET). All code must use `.vb`.
- **Database**: Centralized MariaDB 11.4.x LTS. Do not use SQLite or sync infrastructure (as per the 2026-05-28 pivot).

## Build & Test
- Run `dotnet restore` to restore dependencies.
- Run `dotnet build` to compile the solution.

## VB.NET Build Traps (read before writing any code)

| Trap | Rule | Error |
|------|------|-------|
| **Namespace doubling** | `Namespace` statements must use only the relative suffix — never repeat the `<RootNamespace>` prefix. `Namespace Entities` not `Namespace MerchSys.Purchasing.Entities`. | BC30002 |
| **Fluent chain leading dot** | Multi-line fluent chains must place the `.` at the **end** of the preceding line, not the start of the continuation. | BC30157 |
| **`entry` in DbContext** | Never use `entry` as a loop variable inside a `DbContext` subclass — it shadows `DbContext.Entry()`. Use `dbEntry`. | BC30516 |
| **Lambda param shadows local** | Lambda parameters must not match any local variable anywhere in the same method body. Use short unambiguous names (`p`, `l`). | BC36641 |
| **`List.Count(predicate)`** | `.Count(Function(x) ...)` on a `List(Of T)` binds to the integer property, not the LINQ extension. Use `Enumerable.Count(list, pred)`. | BC32016 |
| **Reserved keyword variable names** | `err`, `cstr`, `cint`, `cdbl`, `now`, `date` etc. collide with VB.NET built-ins. Use `errMsg`, `connStr`, etc. | BC30068 / BC30183 |
| **Reserved keyword enum members** | Enum members named `Return`, `End`, `Stop`, `Error`, `New` must be escaped: `[Return] = 4`. | BC31001 |
| **`Console` namespace shadow** | When `Imports Microsoft.Extensions.Logging` is present, `Console` resolves to MEL's class. Always use `System.Console.WriteLine()`. | BC30456 |
| **`Await` in Catch/Finally** | `Await` is illegal inside `Catch`/`Finally` blocks (BC36943). Capture error state before the block, await after. | BC36943 |
| **Parameter shadows property** | A parameter named `vendors` shadows a property `Vendors`. Name parameters distinctly: `vendorList`, `inputItems`, etc. | silent |
| **EF Core 10 VB.NET ToListAsync empty** | `ToListAsync()` on a full entity query silently returns an empty list. `CountAsync()` and scalar projections work. Use raw `MySqlConnector.MySqlConnection` reader loop. | silent |

## Additional Architecture Rules

**Module boundaries are strict:**
- `MerchSys.App` references all 5 libraries
- Each module references `MerchSys.SharedKernel` only — **no cross-module project references**
- Cross-module communication uses **MediatR events and queries exclusively** — never direct service calls or shared EF navigation properties

**Database:**
- One `DbContext` per module (`PurchasingDbContext`, `InventoryDbContext`, etc.) — all bound to the **same** MariaDB connection string.
- Single centralized **MariaDB 11.4.x LTS** instance (via XAMPP) on a designated host laptop on the LAN. **No local DB on any client. No sync layer.**
- Up to 4 concurrent client laptops connect directly via TCP 3306.
- Table prefixes: `Pur_`, `Inv_`, `Pos_`, `Acc_` — no cross-module foreign keys at DB level
- All tables in 3NF; audit columns (`CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt`) on every table; soft deletes (`IsDeleted`) on all financial/inventory records
- **Optimistic concurrency tokens** (`TIMESTAMP(6) ON UPDATE` or `RowVersion`) on all mutable rows (`Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`, `Pos_CreditAccounts.OutstandingBalance`).
- **`SELECT ... FOR UPDATE`** inside the FIFO inventory decrement transaction to serialize concurrent client writes.
- On `DbUpdateConcurrencyException`, surface "Data changed elsewhere — refresh and retry" in the UI. **Never silently overwrite.**

**MediatR event contracts** (defined in `SharedKernel`):
- `GoodsReceivedEvent` — Purchasing → Inventory, Accounting
- `SaleCompletedEvent` — POS → Inventory, Accounting
- `ShrinkageRecordedEvent` — Inventory → Accounting
- `CreditPaymentEvent` — POS → Accounting
- Cross-module reads use query contracts: `GetCurrentStockQuery`, `GetInventoryValuationQuery`

## Security Requirements

All code must implement the OWASP DA Top 10 controls:
- Use EF Core parameterized queries only — no string-concatenated SQL
- Password hashing: Argon2id
- Sessions: in-memory tokens, 15–30 min timeout, cleared on logout
- Role enforcement at the **data layer**, not just UI (Manager = full, Owner = read-only)
- Structured audit logging on all financial state changes
- Pinned NuGet versions; audit before release

## Users & Roles

- **Manager** — full access to all modules and operations
- **Owner** — read-only access to KPIs, financial reports, and notifications
- **Developer** — Manager superset (full CRUD) plus exclusive access to the Developer Tools module. Reserved for the single internal developer account (seeded idempotently at startup by `MariaDbSchemaInitializer.EnsureDeveloperAccount`). `UserRole.Developer = 3`; enforced at the data layer via `RoleGuardInterceptor` (treated as Manager for write access), and Developer Tools nav/rail/palette entries are gated to this role only.

## Documentation & Architectural Rules (MUST READ)

The core architectural rules and domain concepts are located in the `docs/` folder. While the inline rules above serve as a quick-reference checklist, the `docs/` folder is the **single source of truth** for architectural rationale. You MUST read relevant files from this folder before proposing architectural changes or domain logic.

### UI Architecture
- **[MVP Pattern](docs/rules/architecture-winforms-mvp.md)**: All Windows Forms UI MUST strictly adhere to the Model-View-Presenter pattern. Read this rule before creating or modifying any forms.

### Domain Knowledge
Before touching business logic, check the `docs/domain/` directory for context:
- **Concepts**: `docs/domain/concepts/` (e.g., FIFO costing, centralized database architecture)
- **Entities**: `docs/domain/entities/` (e.g., module definitions, business rules)
- **Sources**: `docs/domain/sources/` (system plan and amendments)

### Feature Specifications
- **[Specs/](Specs/)**: Contains the detailed requirements and business logic for each module (e.g., Purchasing, Inventory). You MUST read the relevant module specs before implementing features.
  - **Reminder**: As noted above, the UI instructions (WPF/MVVM) in the `Specs/` folder are currently outdated. Follow the business logic and feature requirements, but translate any UI instructions into WinForms/MVP on the fly.
