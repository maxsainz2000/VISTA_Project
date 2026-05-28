---
module: Infrastructure
agent: claude-code
date: 2026-05-16
plan-ref: Plans/VISTA_Modules/Infrastructure/13-syncable-repository-migration.md
status: completed
---

# INFRA-13: ISyncableRepository Write-Path Migration & Consumer Registration

## Task Summary

Migrated all four module write-path services from `_context.SaveChangesAsync()` to `_repository.SaveChangesWithJournalAsync()`, populating `Sync_Journal` on every local write. Also implemented the non-generic `ISyncableRepository` interface on all four module repositories and registered them in the DI container so `SyncOrchestrator` can resolve `IEnumerable(Of ISyncableRepository)`.

**Plan:** `13-syncable-repository-migration.md`

## What Was Done

### Module Repositories — Non-Generic Interface Implementation

Each module repository now implements both the generic producer interface and the non-generic consumer interface:

- Modified `src/MerchSys.Purchasing/Data/PurchasingSyncableRepository.vb` — added `Implements ISyncableRepository` (non-generic), `ModuleName = "Purchasing"`, `GetPendingChangesAsync()`, `MarkSyncedAsync()`
- Modified `src/MerchSys.Inventory/Data/InventorySyncableRepository.vb` — added `Implements ISyncableRepository` (non-generic), `ModuleName = "Inventory"`, `GetPendingChangesAsync()`, `MarkSyncedAsync()`
- Modified `src/MerchSys.POS/Data/PosSyncableRepository.vb` — added `Implements ISyncableRepository` (non-generic), `ModuleName = "POS"`, `GetPendingChangesAsync()`, `MarkSyncedAsync()`
- Modified `src/MerchSys.Accounting/Data/AccountingSyncableRepository.vb` — added `Implements ISyncableRepository` (non-generic), `ModuleName = "Accounting"`, `GetPendingChangesAsync()`, `MarkSyncedAsync()`

The `GetPendingChangesAsync()` implementation queries `_journalContext.SyncJournalEntries` filtered by `ModuleName` where `SyncedAt Is Nothing`, ordered by `CreatedAt`. `MarkSyncedAsync()` loads matching entries by ID and sets `SyncedAt = UtcNow`.

### DI Registration

- Modified `src/MerchSys.App/Startup/SyncableRepositoryRegistration.vb` — added four additional `AddScoped(Of ISyncableRepository, TImpl)()` registrations so `SyncOrchestrator` can resolve `IEnumerable(Of ISyncableRepository)` and get all four implementations.

### Service Write-Path Migration

#### Purchasing Module — 6 services, 18 total calls migrated

| Service | `SaveChangesAsync` Calls Replaced |
|---|---|
| `VendorService.vb` | 3 |
| `PurchaseOrderService.vb` | 7 |
| `AccountsPayableService.vb` | 2 |
| `GoodsReceivingService.vb` | 1 |
| `ReorderService.vb` | 5 |
| `PriceChangeService.vb` | 2 |

#### Inventory Module — 5 services, 7 total calls migrated

| Service | `SaveChangesAsync` Calls Replaced |
|---|---|
| `StockService.vb` | 2 |
| `ShrinkageService.vb` | 1 |
| `ExpiryTrackingService.vb` | 1 |
| `LowStockAlertService.vb` | 1 |
| `InventoryAuditService.vb` | 2 |

#### POS Module — 7 services, 9 total calls migrated

| Service | `SaveChangesAsync` Calls Replaced |
|---|---|
| `CartService.vb` | 2 |
| `PaymentService.vb` | 1 |
| `CreditService.vb` | 3 |
| `ReceiptService.vb` | 1 |
| `VatAwareReceiptService.vb` | 1 |
| `SalesReturnService.vb` | 1 |
| `VatConfigurationWriter` (in `IVatConfigurationWriter.vb`) | 1 |

#### Accounting Module — 2 services, 7 total calls migrated

| Service | `SaveChangesAsync` Calls Replaced |
|---|---|
| `FinancialOverviewService.vb` | 1 |
| `VatReportingService.vb` | 6 |

**Total: 20 service files modified, 41 `SaveChangesAsync` calls replaced.**

### Exclusions

| File | Rationale |
|---|---|
| `POS/Services/ReceiptIntegrityService.vb` | Writes to `Pos_ReceiptIntegrity` which is `[NoSync]` by design |
| `Accounting/Handlers/ReceiptTamperDetectedHandler.vb` | Writes to `Acc_TamperAuditLog` which is `[NoSync]` by design |
| `App/Services/SyncOrchestrator.vb` | Writes to `_mariaDb` and `_journalDb`, not module DbContexts |
| `Accounting/Handlers/*.vb` (non-tamper) | Handlers are out of scope for this plan (plan targets `Services/` only) |
| `Inventory/ViewModels/ProductManagementViewModel.vb` | ViewModels are out of scope |

### Migration Pattern Applied

Every migrated service received:
1. `Imports System.Threading` (if not already present)
2. `Imports MerchSys.SharedKernel.Persistence`
3. A new `_repository As ISyncableRepository(Of TContext)` field
4. An updated constructor accepting `repository As ISyncableRepository(Of TContext)`
5. All `_context.SaveChangesAsync()` / `_db.SaveChangesAsync()` replaced with:
   ```vb
   Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population
   ```

### VatConfigurationWriter Transaction Pattern

`VatConfigurationWriter.UpdateAsync` wraps its save in a `Try/Catch` block to capture errors without re-throwing inline. The `SaveChangesAsync` call was inside the `Try` block (not in `Catch`/`Finally`), so migration was straightforward — no VB.NET Await-in-Catch restriction was triggered.

### Non-Generic ISyncableRepository Interface

The non-generic `ISyncableRepository` (already defined in `MerchSys.SharedKernel.Sync.ISyncableRepository.vb`) provides:
```vb
Public Interface ISyncableRepository
    ReadOnly Property ModuleName As String
    Function GetPendingChangesAsync() As Task(Of IReadOnlyList(Of SyncJournal))
    Function MarkSyncedAsync(journalIds As IEnumerable(Of Long)) As Task
End Interface
```

All four module repositories now implement both this interface and `ISyncableRepository(Of TContext)`. DI registers each concrete class under both interface keys, so they can be resolved for either write-path injection or orchestrator enumeration.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | ✅ Passed (Verified that writing through migrated services creates Sync_Journal rows) |

Build result: **0 errors, 1 warning** (pre-existing `BC40000` in `VatConfigurationMap.vb`, unrelated to INFRA-13).

## Issues Encountered

- **Mixed indentation in `ReorderService.vb`:** The `SaveChangesAsync` call inside the `If newSuggestions.Any() Then` block had 16-space indentation while the others had 12 spaces, requiring two separate replace passes.
  - **Resolution:** Applied `replace_all` twice with different indentation patterns.

## What's Next

- [x] Migration of `Accounting/Handlers` write paths ~~(if determined to be in scope for a follow-up plan)~~ — **Voided by INFRA-25/27 (2026-05-28).** INFRA-25 already migrated all handler write paths from `SaveChangesWithJournalAsync` → `SaveChangesAsync`. INFRA-27 then deleted the entire sync layer (`ISyncableRepository`, `SyncOrchestrator`, `Sync_Journal`). There is nothing left to migrate; the handlers write directly to MariaDB.
- [x] Migration of `Inventory/ViewModels/ProductManagementViewModel.vb` write paths ~~(if ViewModels are brought into sync scope)~~ — **Voided by INFRA-27 (2026-05-28).** The sync scope concept no longer exists. ViewModels calling `SaveChangesAsync` on the MariaDB DbContext directly is the correct architecture.
- [x] Runtime verification: execute a write through each migrated service and confirm a corresponding `Sync_Journal` row is created *(completed/verified in Operator checklist)*

## Codebase Wiki Discrepancies

None observed. The `codebase_wiki` accurately reflects the pre-existing `ISyncableRepository` (non-generic) in `Sync/ISyncableRepository.vb`.

## Cross-References

- Domain Wiki pages consulted: `concepts/offline-first-sync.md`, `concepts/modular-monolith.md`
- Agent Wiki entries consulted: `feedback_vbnet_await_catch.md` (memory) — confirmed `SaveChangesAsync` was not in any `Catch`/`Finally` blocks before migrating
