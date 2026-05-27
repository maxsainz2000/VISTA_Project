---
module: MerchSys.Infrastructure
plan-id: INFRA-21
title: "ISyncableRepository Write-Path Migration — Accounting Handlers"
depends-on: [INFRA-13]
estimated-files: 6
priority: high
---

# ISyncableRepository Write-Path Migration — Accounting Handlers

## Context

INFRA-13 migrated all module **service** write paths from `_context.SaveChangesAsync()` to
`_repository.SaveChangesWithJournalAsync()`, populating `Sync_Journal` for central replication.
That plan explicitly scoped out the Accounting **event handlers** — the MediatR
`INotificationHandler` classes that react to cross-module events
(`SaleCompletedEvent`, `GoodsReceivedEvent`, `CreditPaymentEvent`,
`ShrinkageRecordedEvent`, `SaleCompletedWithVatEvent`, `GoodsReceivedWithVatEvent`)
and write derived accounting rows.

The 2026-05-26 deferred-features audit logged this gap as item #9 in
`Plans/Future/deferred-features-backlog.md`. The 2026-05-27 scope decision resolved
it in favor of migration on these grounds:

1. **Half-mirrored data is worse than none.** Sibling services in the same module
   (`VatReportingService`, `FinancialOverviewService`) already journal. Without the
   handlers, central MariaDB contains the source events (sales, receipts, credit
   payments) but is missing the derived accounting rows (`RevenueRecord`,
   `ExpenseRecord`) used by every financial report. That asymmetry silently misleads
   any audit that queries central as a source of truth.
2. **BIR audit posture.** ACC-15 (tamper-audit immutability) and ACC-20 (tamper-report
   export) treat accounting records as legal-grade. Central replication is the
   durability layer for that legal record.
3. **Owner read-only access.** Owner views financial reports. If the Owner ever
   queries from a recovery machine, the central DB must contain the derived
   accounting rows.
4. **Near-zero conflict risk.** Five of the six in-scope handlers insert only; the
   two `WithVat` handlers UPDATE rows they themselves created earlier in the same
   transaction window, so no multi-writer conflict surface is introduced.
5. **Low implementation cost.** The pattern is already proven across ~20 services.
   Each handler needs one DI parameter and one method-call swap.

## Prerequisites

- **INFRA-13** (ISyncableRepository write-path migration & consumer registration) —
  established the pattern this plan applies; `AccountingSyncableRepository` is
  already registered as both `ISyncableRepository(Of AccountingDbContext)` and
  the non-generic `ISyncableRepository`.

## Wiki References

- `concepts/offline-first-sync.md` — every local write must journal for sync
- `concepts/modular-monolith.md` — module-private DbContext boundaries
- `Plans/Future/deferred-features-backlog.md` — item #9 (this plan closes it)

## Deliverables

```
WPF_Applications/MerchSys/src/MerchSys.Accounting/Handlers/
├── SaleCompletedAccountingHandler.vb        ' Modified
├── GoodsReceivedAccountingHandler.vb        ' Modified
├── CreditPaymentAccountingHandler.vb        ' Modified
├── ShrinkageAccountingHandler.vb            ' Modified
├── SaleCompletedWithVatHandler.vb           ' Modified
└── GoodsReceivedWithVatHandler.vb           ' Modified
```

## Specification

### Migration Pattern

Each handler currently looks like:

```vb
Public Class SaleCompletedAccountingHandler
    Implements INotificationHandler(Of SaleCompletedEvent)

    Private ReadOnly _db As AccountingDbContext
    Private ReadOnly _mediator As IMediator
    Private ReadOnly _writeContext As IWriteContextScope
    Private ReadOnly _logger As ILogger(Of SaleCompletedAccountingHandler)

    Public Sub New(db As AccountingDbContext,
                   mediator As IMediator,
                   writeContext As IWriteContextScope,
                   logger As ILogger(Of SaleCompletedAccountingHandler))
        _db = db
        _mediator = mediator
        _writeContext = writeContext
        _logger = logger
    End Sub

    Public Async Function Handle(notification As SaleCompletedEvent,
                                 cancellationToken As CancellationToken) As Task _
        Implements INotificationHandler(Of SaleCompletedEvent).Handle

        Using _writeContext.Enter(WriteContextKind.System)
            ' ... build entities, _db.RevenueRecords.Add(...), etc. ...
            Await _db.SaveChangesAsync(cancellationToken)
        End Using
    End Function
End Class
```

After migration:

```vb
Public Class SaleCompletedAccountingHandler
    Implements INotificationHandler(Of SaleCompletedEvent)

    Private ReadOnly _db As AccountingDbContext
    Private ReadOnly _repository As ISyncableRepository(Of AccountingDbContext)
    Private ReadOnly _mediator As IMediator
    Private ReadOnly _writeContext As IWriteContextScope
    Private ReadOnly _logger As ILogger(Of SaleCompletedAccountingHandler)

    Public Sub New(db As AccountingDbContext,
                   repository As ISyncableRepository(Of AccountingDbContext),
                   mediator As IMediator,
                   writeContext As IWriteContextScope,
                   logger As ILogger(Of SaleCompletedAccountingHandler))
        _db = db
        _repository = repository
        _mediator = mediator
        _writeContext = writeContext
        _logger = logger
    End Sub

    Public Async Function Handle(notification As SaleCompletedEvent,
                                 cancellationToken As CancellationToken) As Task _
        Implements INotificationHandler(Of SaleCompletedEvent).Handle

        Using _writeContext.Enter(WriteContextKind.System)
            ' ... build entities, _db.RevenueRecords.Add(...), etc. ...
            Await _repository.SaveChangesWithJournalAsync(cancellationToken)
        End Using
    End Function
End Class
```

Apply the same shape to all six handlers. `_db` stays for entity attachment and
for query lookups in the `WithVat` handlers (`FirstOrDefaultAsync`); `_repository`
is used for the single `SaveChangesWithJournalAsync` call per Handle invocation.

### In-Scope Handlers

| Handler | Writes | Operation mix |
|---|---|---|
| `SaleCompletedAccountingHandler` | `RevenueRecord`, `ExpenseRecord` (COGS) | INSERT only |
| `GoodsReceivedAccountingHandler` | `ExpenseRecord` (Purchase) | INSERT only |
| `CreditPaymentAccountingHandler` | `ExpenseRecord` (AR Reduction) | INSERT only |
| `ShrinkageAccountingHandler` | `ExpenseRecord` (Shrinkage) | INSERT only |
| `SaleCompletedWithVatHandler` | `RevenueRecord` VAT columns | INSERT or UPDATE (idempotent) |
| `GoodsReceivedWithVatHandler` | `ExpenseRecord` VAT columns | INSERT or UPDATE (idempotent) |

### Exclusions

- **`ReceiptTamperDetectedHandler`** — writes to `Acc_TamperAuditLog`
  (`TamperAuditEntry`), which is decorated `<NoSync>` and is intentionally
  branch-local per ACC-15. Even if migrated, no journal rows would be produced,
  so leaving the direct `_db.SaveChangesAsync` call makes the intent explicit and
  matches the exclusion policy established in INFRA-13.

### DI Verification

`AccountingSyncableRepository` is already registered as both
`ISyncableRepository(Of AccountingDbContext)` (generic, INFRA-13) and the
non-generic `ISyncableRepository` (consumer side). No new registrations
are required — the constructor parameter resolves through the existing
generic binding.

## Implementation Notes

- Do **not** remove `_db` from any handler. The `WithVat` handlers need it for
  their idempotency lookups (`_db.RevenueRecords.FirstOrDefaultAsync(...)`,
  `_db.ExpenseRecords.FirstOrDefaultAsync(...)`).
- Do **not** wrap the new call site in `Try`/`Catch`. The existing flow lets
  exceptions propagate to MediatR, which is correct.
- `ReceiptTamperDetectedHandler` uses the `saveEx As Exception = Nothing` pattern
  to satisfy the VB.NET "no Await in Catch" restriction (BC36943). Leave it as-is.
- One in-scope handler — `SaleCompletedAccountingHandler` — runs `_mediator.Send`
  *before* `SaveChanges`. Order is unchanged; only the final save call swaps.
- `SyncableRepositoryCore.SaveWithJournalAsync` enforces transactional
  consistency between the data write and the journal append (see INFRA-13). The
  `Using _writeContext.Enter(...)` audit scope wraps both, so audit
  `CreatedBy`/`ModifiedBy` are still stamped as `System` for handler-driven writes.

## Acceptance Criteria

1. `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` succeeds with **0 errors, 0 warnings**.
2. All six in-scope handlers accept `ISyncableRepository(Of AccountingDbContext)`
   as a constructor dependency and use `_repository.SaveChangesWithJournalAsync`
   in place of `_db.SaveChangesAsync`.
3. `ReceiptTamperDetectedHandler` is unchanged.
4. A `grep` for `_db\.SaveChangesAsync` under
   `WPF_Applications/MerchSys/src/MerchSys.Accounting/Handlers/` matches
   **only** `ReceiptTamperDetectedHandler.vb`.
5. `Plans/Future/deferred-features-backlog.md` item #9 is marked **closed by INFRA-21**
   (frontmatter `item-9-completed: <date>`).

## Output Requirements

### Implementation Summary

Create at `Progress/VISTA_Modules/Infrastructure/INFRA-21-summary.md` using
`Progress/_template.md`. Include:

- Per-handler change record (file path, constructor parameter added, save-call
  line number replaced).
- Confirmation that `ReceiptTamperDetectedHandler` was correctly excluded.
- Build output (0/0 expected).
- Note any handlers discovered to have additional `SaveChangesAsync` calls
  beyond the single per-Handle save documented in this plan (none expected).

### Backlog Update

Update `Plans/Future/deferred-features-backlog.md`:

- Add `item-9-completed: <YYYY-MM-DD>` to the frontmatter.
- Mark section "## 9. ISyncableRepository Write-Path Migration — Accounting Handlers"
  as **CLOSED — completed by INFRA-21**.
