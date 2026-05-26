---
type: layer-manifest
module: MerchSys.App
layer: Services
last-updated: 2026-05-26
---

# MerchSys.App — Services

This page details the Service implementations specifically located within the **MerchSys.App** module (Composition Root). These typically include services with direct WPF or host dependencies.

## Infrastructure & UI Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.App/Configuration/ConnectionStringLoader.vb` | `ConnectionStringLoader` (Module) | Provides three-state overlay logic for production configuration. Loads `appsettings.Production.json` from `%LOCALAPPDATA%\VISTA\` to override connection strings without committing credentials. |
| `src/MerchSys.App/Data/DatabaseConfig.vb` | `DatabaseConfig` (Module) | Centralizes the SQLite database path (`%LOCALAPPDATA%\MerchSys\merchsys.db`) and provides the `AddModuleDbContexts` extension for DI registration of all four module contexts, injecting the `RoleGuardInterceptor` interceptor for OWASP DA5 database-level write rejection. |
| `src/MerchSys.App/Data/DatabaseInitializer.vb` | `DatabaseInitializer` (Module) | Applies all module migrations to the shared SQLite database on first run using ADO.NET. Workaround for EF Core 10's inability to discover VB.NET migration classes at runtime. |
| `src/MerchSys.App/Services/DefaultSessionService.vb` | `ISessionService`<br>`DefaultSessionService` | Stub singleton returning "Manager" session. Implements `IsAuthenticated = True`. |
| `src/MerchSys.App/Services/MediatREventBus.vb` | `IEventBus`<br>`MediatREventBus` | Thin adapter that delegates `IEventBus.PublishAsync` to MediatR's `IMediator.Publish`. Keeps module code depending on the narrower `IEventBus` interface. |
| `src/MerchSys.App/Services/WpfLowStockNotifier.vb` | `ILowStockNotifier`<br>`WpfLowStockNotifier` | WPF-specific implementation for low-stock alerts. Uses `Notification.Wpf`'s `NotificationManager` to display desktop toast notifications. Lives in the App layer to prevent WPF dependencies in the Inventory library. |
| `src/MerchSys.App/Startup/MediatRConfig.vb` | `MediatRConfig` (Module) | Extension module (`AddMediatRServices`) that registers MediatR and all cross-module handler assemblies. |
| `src/MerchSys.App/Startup/PosServiceRegistration.vb` | `PosServiceRegistration` (Module) | Extension module (`AddPosModule`) that registers POS services, view models, and the `ReceiptArchivalService`. |
| `src/MerchSys.App/Startup/SyncableRepositoryRegistration.vb` | `SyncableRepositoryRegistration` (Module) | Extension module (`AddSyncableRepositories`) that registers all module-specific `ISyncableRepository` implementations. |
| `src/MerchSys.App/Startup/SyncConfig.vb` | `SyncConfig` (Module) | Extension module (`AddSyncServices`) that configures DB sync, registering Contexts, Orchestrator, and Worker. |
| `src/MerchSys.App/Services/SyncOrchestrator.vb` | `SyncOrchestrator` | Iterates `ISyncableRepository` instances to synchronize local changes to MariaDB via the Conflict Resolver pipeline. Runs background database flushes wrapped in the `WriteContextKind.System` scope. |
| `src/MerchSys.App/Services/Sync/ISyncTransmitter.vb`<br>`src/MerchSys.App/Services/Sync/MariaDbSyncTransmitter.vb` | `ISyncTransmitter`<br>`MariaDbSyncTransmitter` | Pomelo-backed implementation for real data transmission. Replaces the placeholder MarkSyncedAsync stub in SyncOrchestrator. Handles batch semantics, financial reject-on-conflict, and non-financial upsert. |
| `src/MerchSys.App/Services/SyncWorker.vb` | `SyncWorker` | `BackgroundService` that executes the dual-condition probe and triggers the orchestrator. |
| `src/MerchSys.App/Services/DefaultNotificationService.vb` | `INotificationService`<br>`DefaultNotificationService` | Singleton that surfaces sync status changes and tracks `LastSuccessfulPushAt` for the UI shell. |
| `src/MerchSys.App/Services/IAuthenticationService.vb` | `IAuthenticationService`<br>`AuthenticationService` | Validates credentials and returns authenticated user. Implements Argon2id hashing and lockout policy. Lockout increments are executed in a `System` context; password changes run in an `AuthSelfService` context, protected by service-level checks preventing Owners from editing other users' credentials. |
| `src/MerchSys.App/Services/LoginSessionService.vb` | `ISessionService`<br>`LoginSessionService` | Session service backed by authenticated UserAccount. Replaces DefaultSessionService, implementing `IsAuthenticated` and `CurrentRole`. |
| `src/MerchSys.App/Services/IIdleMonitor.vb`<br>`src/MerchSys.App/Services/WpfIdleMonitor.vb` | `IIdleMonitor`<br>`WpfIdleMonitor` | OWASP DA2 session inactivity timeout. Hooks DispatcherTimer and InputManager.PreProcessInput to monitor user activity, prompting warning dialog or forced logout (includes NoOpIdleMonitor stub under DEBUG). |
| `src/MerchSys.App/Helpers/PasswordBoxHelper.vb` | `PasswordBoxHelper` | Attached-property bridge for PasswordBox.Password binding to ViewModel string. |

## Debug & Utilities
| File Path | Class | Description |
|---|---|---|
| `src/MerchSys.App/Debug/EventChainVerificationHarness.vb` | `EventChainVerificationHarness` | Orchestrates end-to-end event chain verification (GoodsReceived and SaleCompleted) against a scratch SQLite DB. |
| `src/MerchSys.App/Debug/EventChainReport.vb` | `EventChainReport` | Markdown report generator for the event chain verification results. |
| `src/MerchSys.App/Startup/DebugServiceRegistration.vb` | `DebugServiceRegistration` (Module) | Registers DebugMenuView as Transient in DI for developer diagnostics. |
