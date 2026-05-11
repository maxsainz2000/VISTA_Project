---
module: MerchSys.Infrastructure
plan-id: INFRA-09
title: "ISyncableRepository Per-Module Implementation"
depends-on: [INFRA-03, INFRA-05, INFRA-06]
estimated-files: 6
---

# ISyncableRepository Per-Module Implementation

## Context

INFRA-05 delivered the `SyncWorker`, `SyncOrchestrator`, and the `Sync_Journal` table that records pending row-level changes for the local → central push. INFRA-06 defined the MariaDB target schema and the `SyncOrchestrator.RunAsync` transmission path. Both plans recorded the same "What's Next" item: each module's `Data/` folder still needs an `ISyncableRepository` implementation that **appends to `Sync_Journal` on local writes**. Without that hook, `Sync_Journal` is empty forever and the entire INFRA-05/06 pipeline pushes nothing.

The 2026-05-11 Infrastructure audit confirmed this as **Priority 1** for the module: the sync infrastructure is feature-complete on both ends but has no producer in the middle. This plan closes the gap for all four module `DbContext`s.

The repository abstraction is intentionally thin — it wraps a `SaveChangesAsync` call so the journal append is in the same transaction as the data write, eliminating the gap where the write succeeds but the journal entry is lost. There is **no** new project, no new shared library beyond what `SharedKernel` already exposes.

## Prerequisites

- **INFRA-03** (Database Contexts) — all four module `DbContext`s
- **INFRA-05** (Sync Worker & Connectivity Probe) — `Sync_Journal` schema, `ISyncJournalAppender` (or the existing append-side abstraction)
- **INFRA-06** (MariaDB Central Schema & Reconciliation) — confirms the push consumer exists on the other end

## Wiki References

- `concepts/client-server-wpf.md` — Local-first sync model; central is a replica, not a master
- `concepts/modular-monolith.md` — Module-private `DbContext`; no cross-module repository sharing
- `analysis/cross-module-data-flow.md` — Sync is orthogonal to MediatR; both run independently

## Deliverables

```
MerchSys.SharedKernel/Persistence/
└── ISyncableRepository.vb                              ' New abstraction

MerchSys.Purchasing/Data/
└── PurchasingSyncableRepository.vb                     ' New

MerchSys.Inventory/Data/
└── InventorySyncableRepository.vb                      ' New

MerchSys.POS/Data/
└── PosSyncableRepository.vb                            ' New

MerchSys.Accounting/Data/
└── AccountingSyncableRepository.vb                     ' New

MerchSys.App/Startup/
└── SyncableRepositoryRegistration.vb                   ' New — DI registration extension
```

## Specification

### ISyncableRepository abstraction (SharedKernel)

```
Public Interface ISyncableRepository(Of TContext As DbContext)

    Function SaveChangesWithJournalAsync(
        cancellationToken As CancellationToken
    ) As Task(Of Integer)

    Function GetTrackedChangeDescriptors() As IReadOnlyList(Of SyncJournalDescriptor)

End Interface

Public Class SyncJournalDescriptor
    Public Property TableName As String
    Public Property PrimaryKeyJson As String
    Public Property OperationKind As String         ' "Insert" | "Update" | "Delete"
    Public Property RowSnapshotJson As String       ' Nullable for Delete
    Public Property OccurredAt As DateTime          ' UTC
End Class
```

`SaveChangesWithJournalAsync` is the only write API the module services should call. It:

1. Calls `_context.ChangeTracker.DetectChanges()`.
2. Captures the set of `Added`, `Modified`, and `Deleted` entries as `SyncJournalDescriptor` rows.
3. Calls `_context.SaveChangesAsync(cancellationToken)`.
4. If save succeeded, batch-inserts the captured descriptors into `Sync_Journal` via the existing `ISyncJournalAppender` from INFRA-05 — within the same transaction if the provider supports it (SQLite does, for a single connection).
5. Returns the row count from `SaveChangesAsync`.

`GetTrackedChangeDescriptors` is the read side, used by tests and the debug harness to inspect what *would* be journalled without committing. It does not mutate state.

### Per-module implementation pattern

Each module's repository is a thin wrapper:

```
Public Class PurchasingSyncableRepository
    Implements ISyncableRepository(Of PurchasingDbContext)

    Private ReadOnly _context As PurchasingDbContext
    Private ReadOnly _journal As ISyncJournalAppender

    Public Sub New(context As PurchasingDbContext, journal As ISyncJournalAppender)
        _context = context
        _journal = journal
    End Sub

    ' Implementation delegates to a shared base helper in SharedKernel
End Class
```

The construction logic (change capture, descriptor build, batched insert) lives in a `MustInherit` base class **or** a static helper in `SharedKernel/Persistence/SyncableRepositoryCore.vb`. Either is fine; **pick one and document the choice** in the implementation summary so future agents do not introduce the other variant in parallel.

### Excluded tables

Not every table should be journalled. Exclude:

- `Sync_Journal` itself (infinite recursion).
- The `__EFMigrationsHistory` table.
- Tamper-audit tables (`Pos_ReceiptIntegrity`, `Acc_TamperAuditLog` — INFRA-08 / ACC-15) — these stay local-only by design until a future plan defines their sync semantics. The local SQLite triggers already block UPDATE/DELETE; journalling INSERTs without a central retention story would create a misleading expectation.
- Any table whose `EntityType.Annotations` carries the marker `[NoSync]` (a custom attribute defined here).

Define `<NoSync>` as a class-level attribute in `SharedKernel`:

```
<AttributeUsage(AttributeTargets.Class, AllowMultiple:=False)>
Public Class NoSyncAttribute
    Inherits Attribute
End Class
```

Apply it to the three excluded entity classes listed above as part of this plan's deliverable (those edits count within the per-module file budget — adjust the `estimated-files` if scope grows).

### DI registration

`SyncableRepositoryRegistration.vb` exposes:

```
Public Module SyncableRepositoryRegistration
    <Extension>
    Public Function AddSyncableRepositories(services As IServiceCollection) As IServiceCollection
        services.AddScoped(Of ISyncableRepository(Of PurchasingDbContext), PurchasingSyncableRepository)
        services.AddScoped(Of ISyncableRepository(Of InventoryDbContext), InventorySyncableRepository)
        services.AddScoped(Of ISyncableRepository(Of PosDbContext), PosSyncableRepository)
        services.AddScoped(Of ISyncableRepository(Of AccountingDbContext), AccountingSyncableRepository)
        Return services
    End Function
End Module
```

Call from the composition root: `services.AddSyncableRepositories()`.

## Implementation Notes

- **The producer side does not yet flip every service to use `ISyncableRepository`.** This plan ships the abstraction and the four implementations plus DI. A follow-up *integration* plan (or a series) is needed to migrate each module's write paths from `_context.SaveChangesAsync()` to `_repository.SaveChangesWithJournalAsync()`. That migration is mechanical but high-volume; mixing it into this plan would push file count well past the budget and the audit precedent (each plan stays focused).
- The audit explicitly says **"affects Purchasing, Inventory, POS, and Accounting `data/` folders"** — that scope means *delivering the implementations*, not flipping every call site. This plan honours that scope.
- The `ISyncJournalAppender` symbol is referenced as defined in INFRA-05. Confirm the exact name when reading INFRA-05's code; if the abstraction is named differently, use the actual name verbatim — do not introduce a parallel abstraction.
- Per the feedback memory: VB.NET `Await` is not allowed in `Catch`/`Finally` (BC36943). The transactional rollback path in `SaveChangesWithJournalAsync` must capture exception state and await any compensating work after the `Try` block, not inside `Catch`.
- The `<NoSync>` attribute is the only new SharedKernel public type; it is used by reflection in the repository core. Keep its surface tiny.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `ISyncableRepository(Of TContext)` is a public abstraction in `MerchSys.SharedKernel/Persistence/`.
3. All four module implementations exist and compile.
4. Calling `SaveChangesWithJournalAsync` on any module's repository for an `Added` row results in: (a) the entity row persisted, (b) a corresponding `Sync_Journal` row with `OperationKind = 'Insert'`, `TableName` matching the entity's table, `PrimaryKeyJson` containing the new PK.
5. The same call for a `Modified` row produces an `Update` journal entry; for a `Deleted` row, a `Delete` journal entry with `RowSnapshotJson = NULL`.
6. Entities marked `<NoSync>` produce **no** journal entry on any operation kind.
7. `Sync_Journal`, `__EFMigrationsHistory`, `Pos_ReceiptIntegrity`, and `Acc_TamperAuditLog` are not journalled even if not explicitly marked.
8. A rollback (`SaveChangesAsync` throws) leaves both the data table and `Sync_Journal` unchanged (transactional integrity).
9. The DI extension `AddSyncableRepositories()` registers all four implementations as scoped.
10. No call site outside this plan is modified — `_context.SaveChangesAsync` invocations across the existing service layer remain as-is, awaiting the follow-up migration plan.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-09-summary.md` using `Progress/_template.md`. Include:

- The exact name of the INFRA-05 journal-appender abstraction (so future agents do not search for it).
- Whether the shared core lives as a `MustInherit` base or a static helper, and why.
- The list of tables excluded from sync and the mechanism (built-in vs `<NoSync>` attribute).
- A note explicitly stating that producer-side call-site migration is **out of scope** and pointing at where that follow-up plan should live.

### Documentation
- XML doc on `ISyncableRepository` listing the transactional guarantee.
- XML doc on `<NoSync>` listing the four built-in exclusions and the rationale.
- Inline comment in the repository core explaining why `DetectChanges` is called explicitly before snapshotting (auto-detect-changes timing).
