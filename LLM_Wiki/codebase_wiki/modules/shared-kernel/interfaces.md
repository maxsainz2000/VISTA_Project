---
type: layer-manifest
module: MerchSys.SharedKernel
layer: Interfaces & Enums
last-updated: 2026-05-26
---

# MerchSys.SharedKernel — Interfaces & Enums

## Shared Interfaces
| File Path | Interface | Key Members |
|---|---|---|
| `Interfaces/IAuditable.vb` | `IAuditable` | `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` |
| `Interfaces/ISoftDeletable.vb` | `ISoftDeletable` | `IsDeleted`, `DeletedBy`, `DeletedAt` |
| `Interfaces/IEventBus.vb` | `IEventBus` | `PublishAsync(Of T As INotification)(event As T)` |
| `Interfaces/INotificationService.vb` | `INotificationService` | `ShowSuccess(message)`, `ShowError(message)` |
| `Interfaces/ISessionService.vb` | `ISessionService` | `CurrentUsername`, `CurrentRole`, `IsAuthenticated` |
| `Interfaces/IWriteContextScope.vb` | `IWriteContextScope` | `Current` (`WriteContextKind`), `SelfServiceUsername` (`String`), `Enter(kind, username) As IDisposable` |

## Enums
| File Path | Enum | Values |
|---|---|---|
| `Enums/PaymentMethod.vb` | `PaymentMethod` | `Cash`, `CreditCard`, `DebitCard`, `EWallet`, `Utang` |
| `Enums/PurchaseOrderStatus.vb` | `PurchaseOrderStatus` | `Draft`, `Submitted`, `Approved`, `PartiallyReceived`, `Completed`, `Cancelled` |
| `Enums/UserRole.vb` | `UserRole` | `Manager`, `Owner`, `Cashier`, `StockClerk` |
| `Interfaces/IWriteContextScope.vb` | `WriteContextKind` | `User` (0), `System` (1), `AuthSelfService` (2) |
| `Enums/VatTreatment.vb` | `VatTreatment` | `Vatable`, `Exempt`, `ZeroRated` |

