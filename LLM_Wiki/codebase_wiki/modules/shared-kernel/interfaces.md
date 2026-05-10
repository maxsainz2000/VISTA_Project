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
| `Interfaces/INotificationService.vb` | `INotificationService` | `NotifySyncStatusChanged(status)`, `CurrentStatus` |
| `Interfaces/ISessionService.vb` | `ISessionService` | `CurrentUsername`, `CurrentRole` |
| `Sync/ISyncProbe.vb` | `ISyncProbe` | `ProbeAsync() As Task(Of SyncProbeResult)` |
| `Sync/ISyncableRepository.vb` | `ISyncableRepository` | `GetPendingChangesAsync()`, `MarkSyncedAsync(ids)` |

## Enums
| File Path | Enum | Values |
|---|---|---|
| `Enums/PaymentMethod.vb` | `PaymentMethod` | `Cash`, `CreditCard`, `DebitCard`, `EWallet`, `Utang` |
| `Enums/PurchaseOrderStatus.vb` | `PurchaseOrderStatus` | `Draft`, `Submitted`, `Approved`, `PartiallyReceived`, `Completed`, `Cancelled` |
| `Enums/UserRole.vb` | `UserRole` | `Manager`, `Owner`, `Cashier`, `StockClerk` |
| `Sync/SyncStatus.vb` | `SyncStatus` | `Offline`, `Probing`, `Online`, `Syncing`, `Error` |

