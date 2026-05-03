# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**VISTA** — Villon Integrated Supply and Trade Application. A WPF desktop system for Villon Farm Supply (agricultural retail, ~50 SKUs, Philippines). Built in **Visual Basic .NET 10** targeting .NET 10, following a **modular monolith** architecture.

The repository is in **active implementation phase**. All source code lives under `WPF_Applications/MerchSys/`. The `Plans/` directory contains the detailed implementation plans, `Progress/` contains implementation summaries, and `LLM_Wiki/` is the authoritative two-tier knowledge base.

## Language

All source code is **Visual Basic .NET (VB.NET)**. Every `dotnet new` command must include `--language VB`. All files use `.vb` extension. Do not generate C# syntax.

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

# EF Core migrations (run from within the module's Data/ project context)
dotnet ef migrations add <MigrationName> --project src/MerchSys.<Module>
dotnet ef database update --project src/MerchSys.<Module>
```

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

The project is driven by 51 independent batch plans located in `Plans/VISTA_Modules/`. 
When tasked with "implement this plan" and given a plan file, you must focus **only on writing the code and documentation** specified in that plan. **Testing and deep troubleshooting will be conducted in separate future sessions.** Do not create test projects.

Each plan specifies:
- `depends-on` — plans that must be completed first (ensuring a clean DAG execution)
- `estimated-files` — expected file count
- Deliverables, acceptance criteria, and output requirements

After completing a plan, you must **generate an implementation summary** at `Progress/VISTA_Modules/<Module>/<PLAN-ID>-summary.md` using the template at `Progress/_template.md`.

**Implementation order:** INFRA-01 → INFRA-02 → INFRA-03 → INFRA-04 → then module plans in dependency order.

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
- **MANDATORY:** After resolving a significant bug or establishing a new code pattern, you **MUST** log it in the Agent Wiki. Use `agent_wiki/_templates/error-fix.md` or `agent_wiki/_templates/pattern.md` as your starting point. Update `agent_wiki/index.md` and `agent_wiki/log.md` when you add a new entry.

### 3. Codebase Wiki (`LLM_Wiki/codebase_wiki/`) — **READ ONLY**
This is the pre-digested codebase intelligence layer mapped by Antigravity. It contains the live architecture, file indices, and class signatures.
- **MANDATORY PRE-TASK READING:** Before writing any code for a plan, you **MUST** read `LLM_Wiki/codebase_wiki/index.md` and the specific module index page for your target module. Use this instead of scanning the entire codebase. **Do not write to this folder.**

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
