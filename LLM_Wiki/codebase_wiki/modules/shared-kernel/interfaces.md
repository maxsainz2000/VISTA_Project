---
type: layer-manifest
module: MerchSys.SharedKernel
layer: Interfaces & Enums
last-updated: 2026-05-10
---

# MerchSys.SharedKernel — Interfaces & Enums

## Shared Interfaces
| File Path | Interface | Key Members |
|---|---|---|
| `Interfaces/IAuditable.vb` | `IAuditable` | `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` |
| `Interfaces/ISoftDeletable.vb` | `ISoftDeletable` | `IsDeleted`, `DeletedBy`, `DeletedAt` |
| `Interfaces/IEventBus.vb` | `IEventBus` | `PublishAsync(Of T As INotification)(event As T)` |
| `Interfaces/INotificationService.vb` | `INotificationService` | `SyncStatusChanged` (Event), `CurrentSyncStatus`, `LastSuccessfulPushAt` |
| `Interfaces/ISessionService.vb` | `ISessionService` | `CurrentUsername`, `CurrentRole` |
| `Sync/ISyncProbe.vb` | `ISyncProbe` | `ProbeAsync() As Task(Of SyncProbeResult)` |
| `Sync/ISyncableRepository.vb` | `ISyncableRepository` | `GetPendingChangesAsync()`, `MarkSyncedAsync(ids)`. (Consumer-side). |
| `Persistence/ISyncableRepository.vb` | `ISyncableRepository(Of TContext)` | `SaveChangesWithJournalAsync(token)`, `GetTrackedChangeDescriptors()`. (Producer-side). |
| `Persistence/SyncableRepositoryCore.vb` | `SyncableRepositoryCore` | Static helper (Module) for change-capture and journal mapping. |
| `Sync/ConflictResolver.vb` | `IConflictResolver` | `ResolveAsync(remoteSnapshot, localEntry) As Task(Of ResolutionDecision)` |

## Enums
| File Path | Enum | Values |
|---|---|---|
| `Enums/PaymentMethod.vb` | `PaymentMethod` | `Cash`, `CreditCard`, `DebitCard`, `EWallet`, `Utang` |
| `Enums/PurchaseOrderStatus.vb` | `PurchaseOrderStatus` | `Draft`, `Submitted`, `Approved`, `PartiallyReceived`, `Completed`, `Cancelled` |
| `Enums/UserRole.vb` | `UserRole` | `Manager`, `Owner`, `Cashier`, `StockClerk` |
| `Sync/SyncStatus.vb` | `SyncStatus` | `Offline`, `Probing`, `Online`, `Syncing`, `Error` |
| `Sync/ConflictResolution.vb` | `ConflictResolution` | `LastWriteWins`, `AppendOnly`, `Reject` |
| `Sync/ConflictResolution.vb` | `SyncAction` | `Push`, `Skip`, `Reject` |
| `Enums/VatTreatment.vb` | `VatTreatment` | `Vatable`, `Exempt`, `ZeroRated` |

