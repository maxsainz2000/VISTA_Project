---
module: Infrastructure
agent: claude-code
date: 2026-05-12
plan-ref: Plans/VISTA_Modules/Infrastructure/09-syncable-repository.md
status: completed
---

## Task Summary

Implemented INFRA-09: the producer-side `ISyncableRepository(Of TContext)` abstraction, four module implementations, the `<NoSync>` exclusion attribute, and DI registration. This closes the gap between the INFRA-05/06 sync pipeline (which was feature-complete on both ends) and the module data layer (which was not yet producing `Sync_Journal` rows).

**Plan:** `[[09-syncable-repository]]`

## What Was Done

### SharedKernel — new files

- Created `src/MerchSys.SharedKernel/Persistence/NoSyncAttribute.vb` — `<NoSync>` class attribute; applied to entity classes that must never be journalled
- Created `src/MerchSys.SharedKernel/Persistence/SyncJournalDescriptor.vb` — lightweight DTO capturing one EF change event; fields: `TableName`, `PrimaryKeyJson`, `OperationKind`, `RowSnapshotJson`, `OccurredAt`
- Created `src/MerchSys.SharedKernel/Persistence/ISyncableRepository.vb` — generic producer-side interface `ISyncableRepository(Of TContext As DbContext)` with XML-documented transactional guarantee; distinct from the consumer-side `MerchSys.SharedKernel.Sync.ISyncableRepository` (non-generic)
- Created `src/MerchSys.SharedKernel/Persistence/SyncableRepositoryCore.vb` — public `Module` (static helper) containing all change-capture, descriptor-building, and journal-mapping logic; also contains `Friend NotInheritable Class PreSaveInfo` (internal capture DTO)

### SharedKernel — modified files

- `src/MerchSys.SharedKernel/Sync/SyncJournal.vb` — added `Imports MerchSys.SharedKernel.Persistence` and applied `<NoSync>` attribute

### Module repositories — new files

- Created `src/MerchSys.Purchasing/Data/PurchasingSyncableRepository.vb`
- Created `src/MerchSys.Inventory/Data/InventorySyncableRepository.vb`
- Created `src/MerchSys.POS/Data/PosSyncableRepository.vb`
- Created `src/MerchSys.Accounting/Data/AccountingSyncableRepository.vb`

All four are thin wrappers that delegate to `SyncableRepositoryCore` with their respective `DbContext` type and module name string.

### Entity exclusions — modified files

- `src/MerchSys.POS/Entities/ReceiptIntegrity.vb` — added `Imports MerchSys.SharedKernel.Persistence` and applied `<NoSync>` attribute
- `src/MerchSys.Accounting/Entities/TamperAuditEntry.vb` — added `Imports MerchSys.SharedKernel.Persistence` and applied `<NoSync>` attribute

### App — new file

- Created `src/MerchSys.App/Startup/SyncableRepositoryRegistration.vb` — `Public Module SyncableRepositoryRegistration` with extension method `AddSyncableRepositories(IServiceCollection)` registering all four implementations as `Scoped`

### App — modified file

- `src/MerchSys.App/Application.xaml.vb` — added `services.AddSyncableRepositories()` call after `services.AddSyncServices(...)`

## Design Decisions

### Static helper Module, not MustInherit base class

`SyncableRepositoryCore` is a VB.NET `Module` (static class). A `MustInherit` base was rejected because:
- The four module DbContexts already use `BaseDbContext` inheritance; adding a second base class for the repository wrapper would require introducing a new wrapper type per module with its own inheritance chain.
- A `Module` is simpler: one file, no instantiation, no DI registration of the core itself.
- Future plans adding a fifth module can follow the same delegation pattern without touching SharedKernel.

### INFRA-05 journal-appender abstraction

INFRA-05 did **not** create an `ISyncJournalAppender` interface. The journal-append side is `SyncJournalDbContext` (a concrete `BaseDbContext` subclass). The four module repositories are injected with `SyncJournalDbContext` directly — no wrapper interface.

### Excluded tables and the mechanism used

| Table | Entity class | Exclusion mechanism |
|---|---|---|
| `Sync_Journal` | `SyncJournal` (SharedKernel) | By table name (built-in) **and** `<NoSync>` attribute |
| `__EFMigrationsHistory` | *(EF internal, no CLR type)* | By table name (built-in) |
| `Pos_ReceiptIntegrity` | `ReceiptIntegrity` (POS) | `<NoSync>` attribute |
| `Acc_TamperAuditLog` | `TamperAuditEntry` (Accounting) | `<NoSync>` attribute |

The built-in name check catches tables that are never mapped as entity types. The `<NoSync>` attribute handles domain entities that are mapped but deliberately excluded. Both mechanisms are active; `Sync_Journal` is covered by both for belt-and-suspenders.

### Producer-side call-site migration is out of scope

This plan delivers the abstraction and four implementations; it does **not** change any `_context.SaveChangesAsync()` call site in the existing service layer. A follow-up integration plan (suggested location: `Plans/VISTA_Modules/Integration/`) must migrate each module's write paths to `_repository.SaveChangesWithJournalAsync()`.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Issues Encountered

- **Issue:** `CancellationToken` not resolved in `SyncableRepositoryCore` — `System.Threading` was not imported.
  - **Resolution:** Added `Imports System.Threading`.

- **Issue:** `Friend Module SyncableRepositoryCore` is not accessible from the four module projects (separate assemblies). `Friend` in VB.NET is assembly-scoped.
  - **Resolution:** Changed to `Public Module SyncableRepositoryCore` with `Public` methods for the two cross-assembly entry points (`CaptureDescriptors`, `SaveWithJournalAsync`). Internal helpers (`CollectPreSaveInfo`, `BuildDescriptors`, `ToJournalEntry`, `PreSaveInfo`) remain `Private`/`Friend`.

## What's Next

- [ ] Integration plan: migrate each module's service write paths from `_context.SaveChangesAsync()` to `_repository.SaveChangesWithJournalAsync()`. Suggested plan ID: `INT-06` or a dedicated `INFRA-10`. High file count expected (mechanical but broad).
- [ ] Consumer-side `ISyncableRepository` (non-generic) implementations — needed so `SyncOrchestrator` can iterate pending `Sync_Journal` entries per module. Currently `SyncOrchestrator` resolves `IEnumerable(Of ISyncableRepository)` (non-generic) which is not yet backed by any registered implementation.

## Cross-References

- Domain Wiki pages consulted: `[[concepts/client-server-wpf.md]]`, `[[concepts/modular-monolith.md]]`
- Agent Wiki entries consulted: `[[vbnet-await-catch-restriction]]` (memory — Await not allowed in Catch/Finally)
- Plan dependencies: `[[INFRA-03]]`, `[[INFRA-05]]`, `[[INFRA-06]]`

## Codebase Wiki Discrepancies

- `codebase_wiki/shared-kernel/index.md` does not yet list the new `Persistence/` folder or its four files.
- `codebase_wiki/pos/index.md` and `codebase_wiki/accounting/index.md` do not reflect the `<NoSync>` attribute additions to `ReceiptIntegrity` and `TamperAuditEntry`.
