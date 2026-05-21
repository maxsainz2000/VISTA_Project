# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**VISTA** — Villon Integrated Supply and Trade Application. A WPF desktop system for Villon Farm Supply (agricultural retail, ~50 SKUs, Philippines). Built in **Visual Basic .NET 10** targeting .NET 10, following a **modular monolith** architecture.

The repository is in **active implementation phase**. All source code lives under `WPF_Applications/MerchSys/`. The `Plans/` directory contains the detailed implementation plans, `Progress/` contains implementation summaries, and `LLM_Wiki/` is the authoritative three-tier knowledge base.

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
| **SQLite trigger + temp table** | SQLite triggers cannot reference `temp.*`. Use a persistent table with a TTL column instead. | SQLite Error 1 |
| **Parameter shadows property** | A parameter named `vendors` shadows a property `Vendors` (VB.NET is case-insensitive). `Vendors.Clear()` clears the parameter, not the property — silent logic bug. Name parameters distinctly: `vendorList`, `inputItems`, etc. | silent |
| **EF Core 10 VB.NET ToListAsync empty** | `ToListAsync()` on a full entity query silently returns an empty list. `CountAsync()` and scalar projections work. Use a fresh `SqliteConnection` + synchronous `reader.Read()` loop writing to a class field. | silent |

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

## SQLite CLI

SQLite 3 is installed via winget. Use it to query `merchsys.db` directly from PowerShell:

```powershell
$sqlite3 = "C:\Users\Admin\AppData\Local\Microsoft\WinGet\Packages\SQLite.SQLite_Microsoft.Winget.Source_8wekyb3d8bbwe\sqlite3.exe"
$db = "$env:LOCALAPPDATA\MerchSys\merchsys.db"
& $sqlite3 "-header" "-column" $db "SELECT ... FROM ...;"
```

The `sqlite3` alias is also available in new shells after the PATH refresh (open a new terminal and run `sqlite3 $db "..."` directly).

---

**EF Core CLI is broken for VB.NET + EF Core 10.** Never use `dotnet ef migrations add` or `dotnet ef database update` in this project. Schema changes must be written as manual migration classes in `src/MerchSys.<Module>/Migrations/` and applied via `DatabaseInitializer` (raw `SqliteConnection` + `CREATE TABLE IF NOT EXISTS`) called from `Application_Startup`. See `agent_wiki/errors/efcore10-vbnet-migration-discovery-bug.md` for the exact pattern.

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

**Database:**
- One `DbContext` per module (`PurchasingDbContext`, `InventoryDbContext`, etc.)
- Single SQLite file (`merchsys.db`) — offline-first, always available
- Central **MariaDB 11.4.x** (via XAMPP) for sync — connected only when network + TCP probe succeeds
- Table prefixes: `Pur_`, `Inv_`, `Pos_`, `Acc_` — no cross-module foreign keys at DB level
- All tables in 3NF; audit columns (`CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt`) on every table; soft deletes (`IsDeleted`) on all financial/inventory records

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
| `Microsoft.EntityFrameworkCore.Sqlite` 10.x | All module libraries |
| `CommunityToolkit.Mvvm` (latest stable) | All module libraries |
| `Microsoft.Extensions.DependencyInjection` (latest) | MerchSys.App |
| `Microsoft.Extensions.Hosting` (latest) | MerchSys.App |
| `Notification.Wpf` (latest stable) | MerchSys.App |

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
- `LLM_Wiki/wiki/concepts/modular-monolith.md` — architecture pattern details
- `LLM_Wiki/wiki/concepts/client-server-wpf.md` — WPF stack and sync behavior
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
