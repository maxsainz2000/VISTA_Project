# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**VISTA** — Villon Integrated Supply and Trade Application. A WPF desktop system for Villon Farm Supply (agricultural retail, ~50 SKUs, Philippines). Built in **Visual Basic .NET 10** targeting .NET 10, following a **modular monolith** architecture.

The repository is in **active implementation phase**. All source code lives under `WPF_Applications/MerchSys/`. The `Plans/` directory contains the detailed implementation plans, `Progress/` contains implementation summaries, and `LLM_Wiki/` is the authoritative three-tier knowledge base.

> **⚠️ Architecture pivot — 2026-05-28.** SQLite and the entire sync layer are being removed in favour of pure client-server against a single centralized **MariaDB 11.4.x LTS** instance. See `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` and `LLM_Wiki/wiki/concepts/centralized-database-architecture.md`. New code must target MariaDB only — do not add to SQLite code paths or the sync infrastructure. SQLite-era code is being torn out in INFRA-23 through INFRA-30; until those plans land, the codebase still contains SQLite + `Sync_Journal` + `SyncOrchestrator` etc. as historical scaffolding.

## Language

All source code is **Visual Basic .NET (VB.NET)**. Every `dotnet new` command must include `--language VB`. All files use `.vb` extension. Do not generate C# syntax.

## VB.NET Build Traps (read before writing any code)

These are validated antipatterns from `LLM_Wiki/agent_wiki/` that every agent re-discovers. Check this list first.

| Trap | Rule | Error |
|------|------|-------|
| **Namespace doubling** | `Namespace` statements must use only the relative suffix — never repeat the `<RootNamespace>` prefix. `Namespace Entities` not `Namespace MerchSys.Purchasing.Entities`. | BC30002 |
| **Fluent chain leading dot** | Multi-line fluent chains must place the `.` at the **end** of the preceding line, not the start of the continuation. | BC30157 |
| **`entry` in DbContext** | Never use `entry` as a loop variable inside a `DbContext` subclass — it shadows `DbContext.Entry()`. Use `dbEntry`. | BC30516 |
| **Lambda param shadows local** | Lambda parameters must not match any local variable anywhere in the same method body. Use short unambiguous names (`p`, `l`). | BC36641 |
| **`List.Count(predicate)`** | `.Count(Function(x) ...)` on a `List(Of T)` binds to the integer property, not the LINQ extension. Use `Enumerable.Count(list, pred)`. | BC32016 |
| **Reserved keyword variable names** | `err`, `cstr`, `cint`, `cdbl`, `now`, `date` etc. collide with VB.NET built-ins. Use `errMsg`, `connStr`, etc. | BC30068 / BC30183 |
| **Reserved keyword enum members** | Enum members named `Return`, `End`, `Stop`, `Error`, `New` must be escaped: `[Return] = 4`. | BC31001 |
| **XAML `clr-namespace` root prefix** | `xmlns:x="clr-namespace:Views.Foo"` is wrong. Must be `clr-namespace:MerchSys.App.Views.Foo` — VB.NET root namespace is not applied automatically by the XAML parser. `x:Class` is unaffected. | MC3074 |
| **`Console` namespace shadow** | When `Imports Microsoft.Extensions.Logging` is present, `Console` resolves to MEL's class. Always use `System.Console.WriteLine()`. | BC30456 |
| **`Await` in Catch/Finally** | `Await` is illegal inside `Catch`/`Finally` blocks (BC36943). Capture error state before the block, await after. | BC36943 |
| **SQLite trigger + temp table** *(historical)* | SQLite triggers cannot reference `temp.*`. SQLite is being removed post-2026-05-28; this trap only applies to legacy code still on SQLite. | SQLite Error 1 |
| **Parameter shadows property** | A parameter named `vendors` shadows a property `Vendors` (VB.NET is case-insensitive). `Vendors.Clear()` clears the parameter, not the property — silent logic bug. Name parameters distinctly: `vendorList`, `inputItems`, etc. | silent |
| **EF Core 10 VB.NET ToListAsync empty** | `ToListAsync()` on a full entity query silently returns an empty list. `CountAsync()` and scalar projections work. **Pre-pivot workaround:** raw `SqliteConnection` + synchronous `reader.Read()`. **Post-pivot:** raw `MySqlConnector.MySqlConnection` reader loop — same bug class, different connection type. | silent |

Full docs in `LLM_Wiki/agent_wiki/antipatterns/` and `LLM_Wiki/agent_wiki/errors/`.

## Build & Run Commands

These apply once `WPF_Applications/MerchSys/` is scaffolded (INFRA-01):

```powershell
# Build the entire solution
dotnet build WPF_Applications/MerchSys/MerchSys.slnx

# Run the application
dotnet run --project WPF_Applications/MerchSys/src/MerchSys.App

# Restore packages
dotnet restore WPF_Applications/MerchSys/MerchSys.slnx

# Scaffold new projects (always --language VB, --framework net10.0)
dotnet new wpf --language VB --framework net10.0 -n MerchSys.App
dotnet new classlib --language VB --framework net10.0 -n MerchSys.<Module>

# EF Core migrations — DO NOT USE for VB.NET (see note below)
# dotnet ef migrations add ... is NOT supported for VB.NET in EF Core 10
# dotnet ef database update ... cannot discover VB.NET migration classes
# See agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md for the full workaround
```

## MariaDB CLI

MariaDB is hosted via **XAMPP** on the designated host laptop. From PowerShell on any client:

```powershell
# Local query (when running on the host laptop)
& "C:\xampp\mysql\bin\mysql.exe" -u root merchsys_central -e "SELECT ... FROM ...;"

# Remote query (from a client laptop on the LAN)
& "C:\xampp\mysql\bin\mysql.exe" -h <host-ip-or-name> -u <user> -p merchsys_central -e "SELECT ...;"

# Interactive shell
& "C:\xampp\mysql\bin\mysql.exe" -u root merchsys_central
```

Connection string format (lives in `appsettings.json` per client):

```
Server=<host-ip-or-name>;Port=3306;Database=merchsys_central;User Id=<user>;Password=<password>;
```

### SQLite CLI *(historical — being removed)*

Until the SQLite-removal plans (INFRA-23 → INFRA-30) land, the legacy local DB still exists at `%LOCALAPPDATA%\MerchSys\merchsys.db`. Query with:

```powershell
$sqlite3 = "C:\Users\Admin\AppData\Local\Microsoft\WinGet\Packages\SQLite.SQLite_Microsoft.Winget.Source_8wekyb3d8bbwe\sqlite3.exe"
$db = "$env:LOCALAPPDATA\MerchSys\merchsys.db"
& $sqlite3 "-header" "-column" $db "SELECT ... FROM ...;"
```

Do not write new code that depends on this file — it is going away.

---

**EF Core CLI is broken for VB.NET + EF Core 10.** Never use `dotnet ef migrations add` or `dotnet ef database update` in this project. Schema changes must be written as raw SQL applied at app startup via a startup-time initializer (raw `MySqlConnector.MySqlConnection` + `CREATE TABLE IF NOT EXISTS`). See `agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md` for the discovery-bug background; the post-pivot pattern uses the same raw-connection approach against MariaDB instead of SQLite.

Build must complete with **0 errors, 0 warnings**. No test projects exist yet. (Testing and complex troubleshooting will be conducted in a separate phase after the modules are fully built).
If the build fails, do not directly fix the error. Instead document all the errors in the Progress folder - the troubleshooting will be on a separated session.

## Solution Structure

```
WPF_Applications/MerchSys/
├── MerchSys.slnx
└── src/
    ├── MerchSys.App/          ← WPF startup project (.exe)
    ├── MerchSys.SharedKernel/ ← Base types, events, interfaces, enums
    ├── MerchSys.Purchasing/   ← Vendor, PO lifecycle, AP tracking, reorder engine
    ├── MerchSys.Inventory/    ← Stock management, FIFO costing, expiry, alerts
    ├── MerchSys.POS/          ← Transactions, credit (utang), receipts, returns
    └── MerchSys.Accounting/   ← Financial reports, KPIs, plain-language summaries
```

Each module library has: `Entities/`, `Services/`, `Data/`, `Handlers/`, `ViewModels/`

## Architecture Rules

**Module boundaries are strict:**
- `MerchSys.App` references all 5 libraries
- Each module references `MerchSys.SharedKernel` only — **no cross-module project references**
- Cross-module communication uses **MediatR events and queries exclusively** — never direct service calls or shared EF navigation properties

**Database (post-2026-05-28 pivot):**
- One `DbContext` per module (`PurchasingDbContext`, `InventoryDbContext`, etc.) — all bound to the **same** MariaDB connection string.
- Single centralized **MariaDB 11.4.x LTS** instance (via XAMPP) on a designated host laptop on the LAN. **No local DB on any client. No sync layer.**
- Up to 4 concurrent client laptops connect directly via TCP 3306.
- Table prefixes: `Pur_`, `Inv_`, `Pos_`, `Acc_` — no cross-module foreign keys at DB level
- All tables in 3NF; audit columns (`CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt`) on every table; soft deletes (`IsDeleted`) on all financial/inventory records
- **Optimistic concurrency tokens** (`TIMESTAMP(6) ON UPDATE` or `RowVersion`) on all mutable rows (`Inv_StockBatches.QuantityRemaining`, `Pur_AccountsPayable.OutstandingBalance`, `Pos_CreditAccounts.OutstandingBalance`).
- **`SELECT ... FOR UPDATE`** inside the FIFO inventory decrement transaction to serialize concurrent client writes.
- On `DbUpdateConcurrencyException`, surface "Data changed elsewhere — refresh and retry" in the UI. **Never silently overwrite.**

**Legacy SQLite + sync code (being removed):** The codebase still contains `Sync_Journal`, `SyncOrchestrator`, `SyncWorker`, `MariaDbSyncTransmitter`, `ISyncableRepository`, all `*SyncMap` classes, `ConflictResolver`, `NetworkAvailabilityChanged`, `SyncStatusIndicator`, and the SQLite `DatabaseInitializer`. All of this is targeted for deletion by INFRA-23 → INFRA-30. Do not extend any of it.

**MediatR event contracts** (defined in `SharedKernel`):
- `GoodsReceivedEvent` — Purchasing → Inventory, Accounting
- `SaleCompletedEvent` — POS → Inventory, Accounting
- `ShrinkageRecordedEvent` — Inventory → Accounting
- `CreditPaymentEvent` — POS → Accounting
- Cross-module reads use query contracts: `GetCurrentStockQuery`, `GetInventoryValuationQuery`

**UI pattern:** MVVM via `CommunityToolkit.Mvvm`. ViewModels live in each module library; Views (XAML) live in `MerchSys.App/Views/`.

## Key NuGet Packages

| Package | Used in |
|---------|---------|
| `MediatR` (latest stable) | SharedKernel + all modules |
| `Microsoft.EntityFrameworkCore` 10.x | All module libraries |
| `MySqlConnector` 2.x | All module libraries (raw ADO.NET adapter to MariaDB) |
| EF Core ↔ MySQL provider | **To be selected in INFRA-23.** Pomelo 9.x has a binary incompat with EF Core 10 (see `Operator/debug-logs/INFRA-test-5.md`); Pomelo 10.x is unreleased. Candidates: `MySql.EntityFrameworkCore` (Oracle official), staying on raw `MySqlConnector` for write paths + a thin query helper, or downgrading EF Core. **Do not assume Pomelo.** |
| `CommunityToolkit.Mvvm` (latest stable) | All module libraries |
| `Microsoft.Extensions.DependencyInjection` (latest) | MerchSys.App |
| `Microsoft.Extensions.Hosting` (latest) | MerchSys.App |
| `Notification.Wpf` (latest stable) | MerchSys.App |
| ~~`Microsoft.EntityFrameworkCore.Sqlite` 10.x~~ | **Removed post-pivot** (INFRA-23) |

## Planning & Progress Workflow

The project is driven by 56 independent batch plans located in `Plans/VISTA_Modules/`. 
When tasked with "implement this plan" and given a plan file, you must focus **only on writing the code and documentation** specified in that plan. **Testing and deep troubleshooting will be conducted in separate future sessions.** Do not create test projects.

Each plan specifies:
- `depends-on` — plans that must be completed first (ensuring a clean DAG execution)
- `estimated-files` — expected file count
- Deliverables, acceptance criteria, and output requirements

After completing a plan, you must **generate an implementation summary** at `Progress/VISTA_Modules/<Module>/<PLAN-ID>-summary.md` using the template at `Progress/_template.md`.

**Implementation order:** INFRA-01 → INFRA-02 → INFRA-03 → INFRA-04 → then module plans in dependency order → then INT-01 → INT-02 → INT-03 → INT-04 → INT-05.

## Testing Phase Rules

The project is in the **testing phase**. A debugging skill and enforcement hooks are configured:

- **Skill:** `.claude/skills/debug-test/SKILL.md` — auto-activates when debugging test failures. Contains the full step-by-step protocol.
- **Hooks:** `.claude/hooks/` — enforces debug branch requirement and logs all file edits automatically.
- **Session logs:** `Operator/debug-logs/` — template at `_template.md`.
- **Full protocol:** `Operator/testing-session-protocol.md`

When debugging a test failure, the skill will load automatically. Follow it exactly.

## Three-Tier Knowledge Base (`LLM_Wiki/`)

The repository uses a strict three-tier wiki system. You must adhere to these boundaries:

### 1. Domain Wiki (`LLM_Wiki/wiki/`) — **READ ONLY**
This is the authoritative domain knowledge base maintained by Antigravity. **You must not modify these files.** Read them to understand business rules and architecture. Key references:
- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` — **authoritative architecture amendment** (supersedes the original §5.3, §5.4, §10, §11, §12 of `system_plan.md`)
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` — current data-access and concurrency model
- `LLM_Wiki/wiki/concepts/offline-first-sync.md` — **superseded** historical record only
- `LLM_Wiki/wiki/concepts/modular-monolith.md` — architecture pattern details
- `LLM_Wiki/wiki/concepts/client-server-wpf.md` — WPF stack
- `LLM_Wiki/wiki/analysis/tech-stack-reference.md` — pinned versions
- `LLM_Wiki/wiki/concepts/fifo-costing.md` — inventory valuation method
- `LLM_Wiki/wiki/concepts/utang-credit-system.md` — informal credit (AR) model
- `LLM_Wiki/wiki/concepts/bir-compliance.md` — Official Receipt rules, VAT handling
- `LLM_Wiki/wiki/analysis/cross-module-data-flow.md` — full event/query dependency map
- `LLM_Wiki/wiki/analysis/problem-feature-matrix.md` — traceability from 25 problems to 25 features

### 2. Agent Wiki (`LLM_Wiki/agent_wiki/`) — **READ & WRITE**
This is the shared engineering log for all coding agents.
- **MANDATORY:** Before starting any complex debugging or fixing an error, you **MUST** check `agent_wiki/index.md` and the `errors/` or `patterns/` directories for previous solutions.
- **MANDATORY:** After resolving a significant bug or establishing a new code pattern, you **MUST** log it in the Agent Wiki following the `LLM_Wiki/_system/workflow-agent-wiki-update.md` workflow. Use `agent_wiki/_templates/error-fix.md` or `agent_wiki/_templates/pattern.md` as your starting point. Update `agent_wiki/index.md` and `agent_wiki/log.md` when you add a new entry.

### 3. Codebase Wiki (`LLM_Wiki/codebase_wiki/`) — **READ ONLY**
This is the pre-digested codebase intelligence layer mapped by Antigravity. It contains the live architecture, file indices, and class signatures.
- **MANDATORY PRE-TASK READING:** Before writing any code for a plan, you **MUST** read `LLM_Wiki/codebase_wiki/index.md` and the specific module index page for your target module. Use this instead of scanning the entire codebase. **Do not write to this folder.**
- **DISCREPANCIES:** You can log any noticed `codebase_wiki` discrepancies in your implementation summary, but you cannot touch the contents of `codebase_wiki`.

## Hybrid Commit Workflow

To keep the `codebase_wiki` up to date without wasting agent tokens during coding tasks, we use a hybrid commit workflow:
1. You (the coding agent) implement the code and write your progress summary (e.g., `Progress/VISTA_Modules/<Module>/<PLAN-ID>-summary.md`).
2. You stop here.
3. The user will ask Antigravity to parse your summary and sync the `codebase_wiki/`.
4. The user commits the code, summary, and wiki updates together using the `[wiki-synced]` tag.

## Security Requirements

All code must implement the OWASP DA Top 10 controls documented in `LLM_Wiki/wiki/concepts/owasp-da-top10.md`. Critical points:

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

## Module Audit Skill (`/vista-audit`)

A custom slash command is registered at `.claude/commands/vista-audit.md`. It performs a full mirror-check between `Plans/VISTA_Modules/<module>/` and `Progress/VISTA_Modules/<module>/`, extracts all pending `[ ]` tasks from existing progress summaries, and writes a report to `Pending_Tasks/`.

**Trigger phrase:** When the user says *"Does the Progress folder directly mirrors what's in the Plans folder [module name]"*, run `/vista-audit [module name]` immediately.

**Usage:**
```
/vista-audit Accounting
/vista-audit Purchasing
/vista-audit Inventory
/vista-audit POS
/vista-audit Infrastructure
/vista-audit Integration
```

**Output:** `Pending_Tasks/<MODULE>-audit-<YYYY-MM-DD>.md`

The report contains:
- Mirror check table (Completed / In Progress / Blocked / Missing per plan)
- All unchecked `[ ]` tasks extracted from "What's Next" sections
- List of plans with no progress file yet
- Amendment files noted separately
- Summary and priority recommendations
