---
module: MerchSys.Infrastructure
plan-id: INFRA-13
title: "ISyncableRepository Write-Path Migration & Consumer Registration"
depends-on: [INFRA-09]
estimated-files: 20
priority: critical
---

# ISyncableRepository Write-Path Migration & Consumer Registration

## Context

INFRA-09 delivered the `ISyncableRepository(Of TContext)` abstraction and per-module implementations, but explicitly scoped out the **call-site migration**: every module's service write paths still call `_context.SaveChangesAsync()` directly instead of `_repository.SaveChangesWithJournalAsync()`. The 2026-05-15 Infrastructure audit flagged this as **Priority 1** — without this migration, `Sync_Journal` is never populated at runtime and the entire sync pipeline (INFRA-05/06/12) pushes nothing.

Additionally, `SyncOrchestrator` resolves `IEnumerable(Of ISyncableRepository)` (non-generic) which has no registered implementations — the consumer-side registration is missing.

This plan is mechanical but broad: it touches every service file that writes data across all four modules.

## Prerequisites

- **INFRA-09** (ISyncableRepository) — `ISyncableRepository(Of TContext)`, `SaveChangesWithJournalAsync`, per-module implementations, DI registration

## Wiki References

- `concepts/offline-first-sync.md` — Every local write must journal for sync
- `concepts/modular-monolith.md` — Module-private DbContext boundaries

## Deliverables

```
' Modified files across all four modules — estimated ~20 service files

MerchSys.Purchasing/Services/
├── PurchaseOrderService.vb                     ' Modified
├── GoodsReceivingService.vb                    ' Modified
├── VendorService.vb                            ' Modified
├── ApTrackingService.vb                        ' Modified
└── ... (other write-path services)

MerchSys.Inventory/Services/
├── StockService.vb                             ' Modified
├── ShrinkageService.vb                         ' Modified
├── ExpiryTrackingService.vb                    ' Modified
└── ... (other write-path services)

MerchSys.POS/Services/
├── CartService.vb                              ' Modified
├── PaymentService.vb                           ' Modified
├── CreditService.vb                            ' Modified
├── DailySummaryService.vb                      ' Modified
└── ... (other write-path services)

MerchSys.Accounting/Services/
├── RevenueRecordingService.vb                  ' Modified
├── VatReportingService.vb                      ' Modified
└── ... (other write-path services)

MerchSys.SharedKernel/Persistence/
└── ISyncableRepository.vb                      ' Modified — add non-generic marker interface

MerchSys.App/Startup/
└── SyncableRepositoryRegistration.vb           ' Modified — add non-generic registrations
```

## Specification

### Migration Pattern

For each service that calls `_context.SaveChangesAsync()`:

**Before:**
```vb
Public Class PurchaseOrderService
    Private ReadOnly _context As PurchasingDbContext

    Public Sub New(context As PurchasingDbContext)
        _context = context
    End Sub

    Public Async Function CreateOrderAsync(...) As Task
        ' ... build entity ...
        Await _context.SaveChangesAsync()
    End Function
End Class
```

**After:**
```vb
Public Class PurchaseOrderService
    Private ReadOnly _context As PurchasingDbContext
    Private ReadOnly _repository As ISyncableRepository(Of PurchasingDbContext)

    Public Sub New(context As PurchasingDbContext,
                   repository As ISyncableRepository(Of PurchasingDbContext))
        _context = context
        _repository = repository
    End Sub

    Public Async Function CreateOrderAsync(...) As Task
        ' ... build entity ...
        Await _repository.SaveChangesWithJournalAsync(CancellationToken.None)
    End Function
End Class
```

### Exclusions

The following services/write paths are **excluded** from this migration:

- `ReceiptIntegrityService` — writes to `Pos_ReceiptIntegrity` which is `[NoSync]` by design
- `TamperAuditHandler` — writes to `Acc_TamperAuditLog` which is `[NoSync]` by design
- EF migration runners — schema changes are not journalled
- Any service that only reads data (no `SaveChangesAsync` calls)

### Non-Generic Consumer Interface

Add a non-generic marker interface to `SharedKernel` that `SyncOrchestrator` can resolve:

```vb
Public Interface ISyncableRepository
    ReadOnly Property ModuleName As String
    Function GetPendingJournalCountAsync() As Task(Of Integer)
End Interface
```

Each module's repository implements both the generic and non-generic interfaces. Register all four as `ISyncableRepository` (non-generic) in the DI container for `SyncOrchestrator` enumeration.

## Implementation Notes

- This is a **mechanical** migration: each file follows the same pattern. The risk is low per file but the volume is high.
- Services that use `_context` for reads and writes: keep `_context` for reads (queries), use `_repository` for writes. Do not replace `_context` entirely — the repository is a write-side concern only.
- Some services may have multiple `SaveChangesAsync` calls (e.g., multi-step transactions). Each call should be migrated. If the service uses explicit transactions (`BeginTransaction`), the repository must participate in the same transaction — verify INFRA-09's implementation handles this.
- Per the feedback memory: VB.NET `Await` is not allowed in `Catch`/`Finally`. Check for any `SaveChangesAsync` calls inside `Catch` blocks and refactor if found.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. `grep -r "SaveChangesAsync" --include="*.vb"` in the four module `Services/` folders returns **zero** hits (all migrated to `SaveChangesWithJournalAsync`), excluding the repository implementations themselves and the excluded services listed above.
3. Each migrated service constructor accepts `ISyncableRepository(Of TContext)`.
4. `SyncOrchestrator` can resolve `IEnumerable(Of ISyncableRepository)` (non-generic) and get 4 implementations.
5. A write operation through any migrated service produces a corresponding `Sync_Journal` entry.
6. Excluded services (`ReceiptIntegrityService`, `TamperAuditHandler`) continue to use `_context.SaveChangesAsync()` directly and produce no journal entries.
7. Services with explicit transactions still work correctly with the repository.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-13-summary.md` using `Progress/_template.md`. Include:
- Complete list of migrated files (service name, module, number of `SaveChangesAsync` calls replaced).
- List of excluded files and rationale.
- Note on transaction handling if any multi-step services were encountered.
- The non-generic `ISyncableRepository` interface definition.

### Documentation
- Inline comment on each migrated `SaveChangesWithJournalAsync` call: `' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population`
