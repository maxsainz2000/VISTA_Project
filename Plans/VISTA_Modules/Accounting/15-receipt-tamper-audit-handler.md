---
module: MerchSys.Accounting
plan-id: ACC-15
title: "Receipt Tamper Audit Handler"
depends-on: [ACC-02, INFRA-04, INFRA-07, POS-13]
estimated-files: 4
---

# Receipt Tamper Audit Handler

## Context

INFRA-07 defined the cross-module `ReceiptTamperDetectedEvent` payload contract. POS-13 wired the publisher: when the receipt integrity check detects a hash mismatch, missing sequence row, or unexpected status transition, the event is raised on the MediatR bus. The 2026-05-11 Infrastructure audit flagged that **no handler exists** to consume this event on the Accounting side. The event currently fires into the void.

BIR tamper-evidence requires the detection itself **and** a durable, queryable audit trail. Without a handler, the publisher is theatre — auditors and investigators have no way to find tamper incidents months later. This plan adds:

1. A new Accounting audit ledger entity, `Acc_TamperAuditLog`, dedicated to receipt-tamper events. It is distinct from the existing generic audit columns on every Accounting table — those record who changed a row, this records security-relevant cross-module incidents.
2. A MediatR `INotificationHandler(Of ReceiptTamperDetectedEvent)` that persists the event with full context.
3. An EF Core migration adding the new table.

A view surface (a tamper-incident report) is **out of scope** for this plan — that belongs to a future Accounting reporting plan once the data is being captured.

## Prerequisites

- **ACC-02** (Accounting Data Access) — `AccountingDbContext`, migration pattern
- **INFRA-04** (MediatR Event Bus) — `INotificationHandler` registration
- **INFRA-07** (Cross-Module VAT Event Payload Contracts) — `ReceiptTamperDetectedEvent` definition
- **POS-13** (BIR Tamper-Proof Receipt Retention & Sequence) — publishes the event

## Wiki References

- `concepts/bir-compliance.md` — Tamper-evidence audit retention (10 years)
- `concepts/owasp-da-top10.md` — Structured audit logging on all security-relevant state changes
- `analysis/cross-module-data-flow.md` — `ReceiptTamperDetectedEvent` flow

## Deliverables

```
MerchSys.Accounting/Entities/
└── TamperAuditEntry.vb                                ' New entity

MerchSys.Accounting/Data/Migrations/
└── <timestamp>_AddTamperAuditLog.vb                   ' New migration

MerchSys.Accounting/Handlers/
└── ReceiptTamperDetectedHandler.vb                    ' New MediatR handler

MerchSys.Accounting/Services/
└── ITamperAuditQueryService.vb                        ' New read-side abstraction
```

The query service interface is included so a future reporting plan has a defined contract to consume; its implementation is a thin EF query and counts as part of the same file as the interface (single file, two declarations).

## Specification

### TamperAuditEntry entity

```
Public Class TamperAuditEntry
    Public Property Id As Long
    Public Property DetectedAt As DateTime              ' UTC
    Public Property ReceiptId As Long                   ' Foreign-keyless reference (modular monolith rule)
    Public Property ReceiptNumber As String             ' Denormalised snapshot
    Public Property TamperKind As String                ' "HashMismatch" | "MissingSequenceRow" | "StatusTransition" | "OutOfOrderNumber"
    Public Property DetectedByService As String         ' Class name of detector
    Public Property ExpectedValue As String             ' Nullable — e.g., expected hash
    Public Property ActualValue As String               ' Nullable — observed hash
    Public Property AdditionalContextJson As String     ' Nullable — free-form JSON payload from event
    Public Property MachineName As String
    Public Property OperatingUser As String             ' Windows identity at detection time
    Public Property CreatedAt As DateTime
    Public Property CreatedBy As String
End Class
```

Table name: `Acc_TamperAuditLog` (matches module prefix convention).

Constraints:
- All columns `NOT NULL` except the three explicitly noted as nullable.
- Composite index on `(DetectedAt, TamperKind)` to support time-windowed queries by incident type.
- **No** `IsDeleted` soft-delete column — tamper records are append-only by design. **No** `ModifiedBy` / `ModifiedAt` — they cannot be modified.

This deliberate deviation from the standard audit-column convention (CLAUDE.md) is documented in an inline XML doc comment and called out in the implementation summary.

### Migration

`AddTamperAuditLog`:
- `CREATE TABLE Acc_TamperAuditLog (...)`
- Index `IX_Acc_TamperAuditLog_DetectedAt_TamperKind`
- Trigger (SQLite): block UPDATE and DELETE on the table — mirrors the POS-13 tamper-table pattern.

The MariaDB equivalent triggers belong to **INFRA-08** scope (it covers central-side tamper-table immutability). Document the cross-reference in the migration's XML doc comment so a future MariaDB delta plan can pick it up; do not attempt to write MariaDB DDL here.

### ReceiptTamperDetectedHandler

```
Public Class ReceiptTamperDetectedHandler
    Implements INotificationHandler(Of ReceiptTamperDetectedEvent)

    Public Async Function HandleAsync(
        notification As ReceiptTamperDetectedEvent,
        cancellationToken As CancellationToken
    ) As Task Implements INotificationHandler(Of ReceiptTamperDetectedEvent).Handle

End Class
```

Behaviour:
1. Build a `TamperAuditEntry` from the event payload.
2. Capture `Environment.MachineName` and the current Windows identity.
3. Persist via a scoped `AccountingDbContext.Add(...) + SaveChangesAsync()`.
4. If the save itself fails (e.g., DB locked, migration not yet applied), log the failure via `ILogger` at `Critical` and rethrow — a silent swallow here would be a second-order tamper vulnerability.
5. Do **not** publish a follow-on event. The handler is a sink.

Per the feedback memory (`feedback_vbnet_await_catch.md`): if a `Try/Catch` is used around `SaveChangesAsync`, do not `Await` inside the `Catch` block. Capture the exception, log after the `Catch`, then rethrow.

### ITamperAuditQueryService

```
Public Interface ITamperAuditQueryService
    Function GetIncidentsAsync(fromUtc As DateTime, toUtc As DateTime) As Task(Of IReadOnlyList(Of TamperAuditEntry))
    Function CountByKindAsync(fromUtc As DateTime, toUtc As DateTime) As Task(Of IReadOnlyDictionary(Of String, Integer))
End Interface
```

Implementation in the same file: thin EF queries against `Acc_TamperAuditLog`.

### DI registration

Add to the existing Accounting module-local DI extension (whichever file owns `IFinancialOverviewService` registration):

```
services.AddScoped(Of ITamperAuditQueryService, TamperAuditQueryService)
services.AddScoped(Of INotificationHandler(Of ReceiptTamperDetectedEvent), ReceiptTamperDetectedHandler)
```

If the project already uses MediatR assembly scanning, the explicit notification handler line is unnecessary — verify by reading INFRA-04's registration block before adding. Do not register twice.

## Implementation Notes

- The handler is the audit sink, not the policy point. It does not decide what to do with a tamper — it only records it. Notification UI, manager email, or operational alerts are future scope.
- The denormalised `ReceiptNumber` snapshot is intentional: by the time someone investigates the audit log months later, the original POS row may be archived to `Pos_OfficialReceiptArchive` (POS-13). The snapshot survives the move.
- The migration adds `INSERT`-permissive but `UPDATE/DELETE`-blocking SQLite triggers. This mirrors POS-13's pattern on `Pos_ReceiptIntegrity` exactly; copy that trigger SQL with adjusted table and column names rather than reinventing the construction.
- The composite index `(DetectedAt, TamperKind)` is sized for the expected access pattern: investigators query a date window and filter by incident type. A single-column index on `DetectedAt` would force a full scan filtered by kind for the common report.
- No cross-module project reference. The handler consumes the event type from `MerchSys.SharedKernel` (where INFRA-07 placed the contract) and writes to `AccountingDbContext`. This is exactly the modular-monolith pattern.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `Acc_TamperAuditLog` table exists in `AccountingDbContext.OnModelCreating` and in the new migration.
3. The migration installs INSERT-permissive, UPDATE-blocking, DELETE-blocking SQLite triggers on `Acc_TamperAuditLog`.
4. Index `IX_Acc_TamperAuditLog_DetectedAt_TamperKind` is present.
5. `ReceiptTamperDetectedHandler` implements `INotificationHandler(Of ReceiptTamperDetectedEvent)`.
6. Publishing a `ReceiptTamperDetectedEvent` via the MediatR bus results in exactly one new row in `Acc_TamperAuditLog`.
7. The row carries the event payload, `Environment.MachineName`, and the current Windows identity.
8. Attempting `UPDATE Acc_TamperAuditLog SET TamperKind = '...' WHERE Id = 1` fails with a SQLite constraint error from the trigger.
9. Attempting `DELETE FROM Acc_TamperAuditLog WHERE Id = 1` fails likewise.
10. `ITamperAuditQueryService.GetIncidentsAsync` returns rows ordered by `DetectedAt DESC`.
11. The handler is registered exactly once in DI (no double registration if MediatR scanning is on).

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-15-summary.md` using `Progress/_template.md`. Include the migration SQL (table + triggers + index) inline, and a snippet showing the event-publication smoke test producing one row.

### Documentation
- XML doc comment on `TamperAuditEntry` describing why it deviates from the standard CLAUDE.md audit-column convention.
- XML doc comment on `ReceiptTamperDetectedHandler` describing the sink-only role (does not publish follow-on events).
- A short cross-reference note in the migration file pointing at INFRA-08 for the MariaDB-equivalent immutability triggers, so the central-side scope is visible from the local-side code.
