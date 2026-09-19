---
module: MerchSys.POS
plan-id: POS-16
title: "Receipt Archival Background Service"
depends-on: [POS-06, POS-13]
estimated-files: 4
---

# Receipt Archival Background Service

## Context

POS-13 introduced `Pos_OfficialReceiptArchive` — a destination table for receipts past their `RetentionExpiresAt + GraceDays` window. The audit recorded the schema and the intent but no archival job was ever implemented. The 2026-05-11 POS audit flags this as **Priority 2** for BIR compliance: BIR requires a 10-year retention window. Without an archival job:

- The live `Pos_OfficialReceipts` table grows unbounded; query performance on dashboards (POS-08, POS-12) degrades as retention pile-up exceeds the operational window.
- The archive table is empty in production; an auditor asking "where are receipts from 2026" three years from now finds them in the live table mixed with 2029 transactions rather than cleanly separated.

This plan delivers a background service that runs on an internal schedule, moves eligible rows in batches from `Pos_OfficialReceipts` to `Pos_OfficialReceiptArchive` and from `Pos_ReceiptIntegrity` to a corresponding `Pos_ReceiptIntegrityArchive`, and emits structured logs of each archival batch. The service is conservative: it only moves rows that have passed the configured retention plus grace window, never moves anything within the active fiscal year, and writes an audit record of each move.

## Prerequisites

- **POS-06** (Receipt Generation) — `Pos_OfficialReceipts`, `IReceiptService`
- **POS-13** (BIR Tamper-Proof Receipt Retention & Sequence) — `Pos_OfficialReceiptArchive`, `Pos_ReceiptIntegrity`, `RetentionExpiresAt`, `GraceDays`

## Wiki References

- `concepts/bir-compliance.md` — 10-year retention; archival is permitted but must be auditable
- `concepts/owasp-da-top10.md` — Structured audit logging on data movement

## Deliverables

```
MerchSys.POS/Services/Archival/
├── IReceiptArchivalService.vb
├── ReceiptArchivalService.vb
└── ReceiptArchivalOptions.vb                          ' Bound from IConfiguration

MerchSys.POS/Data/Migrations/
└── <timestamp>_AddReceiptIntegrityArchive.vb          ' New table mirroring Pos_ReceiptIntegrity
```

The `IHostedService` registration occurs in the existing POS module DI extension; the hosted service implementation is `ReceiptArchivalService` itself (it implements both `IReceiptArchivalService` and `IHostedService` — see Specification).

## Specification

### Pos_ReceiptIntegrityArchive table

Schema-identical mirror of `Pos_ReceiptIntegrity` plus:

- `ArchivedAt As DateTime` (UTC, NOT NULL)
- `ArchivedByService As String` (NOT NULL, defaults to `"ReceiptArchivalService"`)

SQLite triggers blocking UPDATE and DELETE on this table — INSERT-only. Migration mirrors the POS-13 pattern.

The MariaDB equivalent triggers are scoped to INFRA-08 (and noted as out of scope here). Add a cross-reference comment in the migration.

### IReceiptArchivalService

```
Public Interface IReceiptArchivalService

    Function ArchiveEligibleAsync(
        asOfUtc As DateTime,
        batchSize As Integer,
        cancellationToken As CancellationToken
    ) As Task(Of ReceiptArchivalBatchResult)

End Interface

Public Class ReceiptArchivalBatchResult
    Public Property ReceiptsMoved As Integer
    Public Property IntegrityRowsMoved As Integer
    Public Property EarliestArchivedReceiptDate As DateTime?
    Public Property LatestArchivedReceiptDate As DateTime?
    Public Property DurationMs As Long
    Public Property HadMoreEligible As Boolean             ' True if batch size was the limiting factor
End Class
```

`ArchiveEligibleAsync` behaviour:

1. Open a single transaction on `PosDbContext`.
2. Query `Pos_OfficialReceipts` for rows where:
   - `Status = 'Issued'`
   - `RetentionExpiresAt IS NOT NULL`
   - `RetentionExpiresAt + GraceDays days < asOfUtc`
   - `IssuedAt.Year < <current fiscal year>` (never archive same-year data even if retention has technically expired)
3. Limit the result to `batchSize` rows ordered by `IssuedAt` ascending (oldest first).
4. For each eligible receipt:
   - Insert a copy into `Pos_OfficialReceiptArchive` with `ArchivedAt = asOfUtc` and `ArchivedByService` set.
   - Look up the matching `Pos_ReceiptIntegrity` row by `ReceiptId`.
   - Insert a copy into `Pos_ReceiptIntegrityArchive` with the same archival metadata.
5. After all archival inserts succeed, **then** delete the source rows from `Pos_OfficialReceipts` and `Pos_ReceiptIntegrity`. The append-only triggers on the source tables must permit DELETE for this specific path — see Implementation Notes for the trigger contract.
6. Commit the transaction. Any exception rolls back and leaves the live tables untouched.
7. Populate and return `ReceiptArchivalBatchResult`. `HadMoreEligible = True` if the eligibility query returned more rows than the batch limit.

### ReceiptArchivalService as IHostedService

```
Public Class ReceiptArchivalService
    Implements IReceiptArchivalService, IHostedService

    Public Sub New(
        scopeFactory As IServiceScopeFactory,
        options As IOptions(Of ReceiptArchivalOptions),
        logger As ILogger(Of ReceiptArchivalService)
    )

End Class
```

Hosted lifecycle:

- `StartAsync`: schedule a `PeriodicTimer` driven by `Options.IntervalHours`. Default `24` hours.
- Each tick: create a scope, resolve `PosDbContext`, call `ArchiveEligibleAsync(DateTime.UtcNow, Options.BatchSize, cancellationToken)`.
- Log each batch result at `Information` (counts + window). On exception, log at `Error` and continue — a single bad night must not crash the host process.
- `StopAsync`: dispose the timer; abandon any in-flight batch via the cancellation token (the transactional model means an aborted batch leaves no partial state).

### ReceiptArchivalOptions

```
Public Class ReceiptArchivalOptions
    Public Property IntervalHours As Integer = 24
    Public Property BatchSize As Integer = 500
    Public Property Enabled As Boolean = True
End Class
```

Bound from `Receipts:Archival` in `appsettings.json`. `Enabled = False` short-circuits both the scheduled run and any manual invocation.

### Append-only trigger contract

The POS-13 triggers on `Pos_OfficialReceipts` and `Pos_ReceiptIntegrity` block DELETE unconditionally. The archival service needs DELETE permission on the rows it has just successfully copied. Two options:

1. **Trigger-aware path (preferred)**: amend the POS-13 triggers to permit DELETE when a session variable `archival_in_progress = 1` is set. The service sets and unsets the variable around the batch. Simple, auditable, but requires modifying POS-13 deliverables.

2. **Trigger-bypass path**: drop the DELETE-blocking trigger temporarily, perform the deletes, recreate the trigger. Faster to implement but creates a window where the trigger is absent and is harder to reason about.

This plan picks **option 1**. Modify the POS-13 trigger definitions to consult the session variable. The implementation summary documents the exact trigger text before/after and confirms that absent the variable, DELETE remains blocked.

If the operator's deployment policy disallows option 1, defer the trigger amendment to a separate plan and ship the archival service with `Enabled = False`; document the deferral.

## Implementation Notes

- The fiscal-year guard prevents same-year archival even if retention windows are misconfigured shorter than expected. Without it, a `RetentionExpiresAt = IssuedAt + 30 days` typo could archive last week's transactions.
- The order is **insert into archive first, then delete from source** — the inverse is silently lossy if the archive insert fails mid-batch.
- The hosted-service registration goes in `Program.cs` / `Application.xaml.vb` via:
  ```
  services.AddHostedService(Of ReceiptArchivalService)
  ```
  And the service-as-interface registration:
  ```
  services.AddScoped(Of IReceiptArchivalService, ReceiptArchivalService)
  ```
  Resolving both refers to the same hosted lifetime instance via the scope factory pattern.
- Per the feedback memory: VB.NET `Await` is not allowed in `Catch`/`Finally`. The transactional rollback path captures state and awaits any compensating work outside the `Try` block.
- This plan does **not** ship a UI surface for triggering manual archival. The hosted service is sufficient. If an operator needs to force an archival run, they can call `IReceiptArchivalService.ArchiveEligibleAsync` directly from a debug menu — out of scope here.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. Migration `AddReceiptIntegrityArchive` creates `Pos_ReceiptIntegrityArchive` with the documented schema and INSERT-only triggers.
3. `ReceiptArchivalService` implements both `IReceiptArchivalService` and `IHostedService`.
4. Registered as a singleton hosted service in DI.
5. `ArchiveEligibleAsync` against a seeded scratch DB containing 100 expired receipts and 100 in-window receipts moves exactly the 100 expired ones when `batchSize = 200`.
6. Same scenario with `batchSize = 50` moves 50 rows and reports `HadMoreEligible = True`.
7. Receipts from the current fiscal year are never moved, even if `RetentionExpiresAt + GraceDays` is in the past.
8. A forced insert-failure on the archive table (e.g., simulated by violating a constraint) leaves the live tables fully intact (transactional rollback).
9. After the trigger amendment, attempting `DELETE FROM Pos_OfficialReceipts` from a normal session (without the archival session variable set) still fails with the POS-13 trigger error.
10. `Pos_OfficialReceiptArchive` and `Pos_ReceiptIntegrityArchive` reject UPDATE and DELETE unconditionally.
11. `Enabled = False` in options prevents the hosted timer from firing and prevents `ArchiveEligibleAsync` from doing any work (short-circuit at the entry point).

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-16-summary.md` using `Progress/_template.md`. Include:

- The exact before/after SQL of the POS-13 triggers showing the session-variable awareness.
- A sample `ReceiptArchivalBatchResult` from a seeded run (100 expired, 100 in-window, batchSize 200).
- The `appsettings.json` snippet for the `Receipts:Archival` section.
- A note recording whether option 1 (trigger amendment) was used; if option 2 was used instead, document why.

### Documentation
- XML doc on `IReceiptArchivalService` describing the fiscal-year guard and the insert-then-delete ordering.
- XML doc on `ReceiptArchivalOptions` listing the three knobs and their effective defaults.
- Inline comment on the trigger session-variable usage explaining why the archival path is allowed to DELETE.
