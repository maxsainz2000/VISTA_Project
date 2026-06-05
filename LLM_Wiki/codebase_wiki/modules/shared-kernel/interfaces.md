---
type: layer-manifest
module: MerchSys.SharedKernel
layer: Interfaces & Enums
last-updated: 2026-06-05
---

# MerchSys.SharedKernel — Interfaces & Enums

## Shared Interfaces
| File Path | Interface | Key Members |
|---|---|---|
| `Interfaces/IAuditable.vb` | `IAuditable` | `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` |
| `Interfaces/ISoftDeletable.vb` | `ISoftDeletable` | `IsDeleted`, `DeletedBy`, `DeletedAt` |
| `Interfaces/IEventBus.vb` | `IEventBus` | `PublishAsync(Of T As INotification)(event As T)` |
| `Interfaces/INotificationService.vb` | `INotificationService` | `ShowSuccess(message, Optional action)`, `ShowError(message, Optional action)`, `ShowInfo(message, Optional action)`, `ShowWarning(message, Optional action)` — extended to support optional action button |
| `Presentation/NotificationAction.vb` | `NotificationAction` | Model class representing an optional action button attached to a toast notification. Exposes `Label` (button text) and `Callback` (`Action` delegate) (UX-29) |
| `Interfaces/IConflictPresenter.vb` | `IConflictPresenter` | `PromptAsync() As Task(Of Boolean)` |
| `Presentation/IConfirmationPresenter.vb` | `IConfirmationPresenter` | `PromptAsync(request) As Task(Of Boolean)` |
| `Presentation/ConfirmationRequest.vb` | `ConfirmationRequest` | Model representing a request to show a confirmation dialog, containing title, message, and confirmation buttons. |
| `Interfaces/ISessionService.vb` | `ISessionService` | `CurrentUsername`, `CurrentRole`, `IsAuthenticated` |
| `Interfaces/IWriteContextScope.vb` | `IWriteContextScope` | `Current` (`WriteContextKind`), `SelfServiceUsername` (`String`), `Enter(kind, username) As IDisposable` |
| `Presentation/IFreshnessAware.vb` | `IFreshnessAware` | `Property LastLoadedAt As DateTime?` — marker interface implemented by data ViewModels that expose a freshness timestamp; allows `FreshnessChip` bindings to be declared uniformly. Additive; does not force a base class (UX-24). |
| `Presentation/FilterChipItem.vb` | `FilterChipItem` | Model class (not an interface) representing an active filter chip in the `FilterSummaryBar`. Exposes `DisplayText` (the human-readable label), `FilterKey` (the filter facet identifier), and `RemoveCommand` (`ICommand` — a `RelayCommand` wired to reset the specific filter property in the ViewModel). Instantiated by `RefreshFilterChips()` in each ViewModel when `ApplyFilters()` runs; stored in `ObservableCollection(Of FilterChipItem)` bound to the bar's chip `ItemsControl` (UX-28). |

## Enums
| File Path | Enum | Values |
|---|---|---|
| `Enums/PaymentMethod.vb` | `PaymentMethod` | `Cash`, `CreditCard`, `DebitCard`, `EWallet`, `Utang` |
| `Enums/PurchaseOrderStatus.vb` | `PurchaseOrderStatus` | `Draft`, `Submitted`, `Approved`, `PartiallyReceived`, `Completed`, `Cancelled` |
| `Enums/UserRole.vb` | `UserRole` | `Manager`, `Owner`, `Cashier`, `StockClerk` |
| `Interfaces/IWriteContextScope.vb` | `WriteContextKind` | `User` (0), `System` (1), `AuthSelfService` (2) |
| `Enums/VatTreatment.vb` | `VatTreatment` | `Vatable`, `Exempt`, `ZeroRated` |

