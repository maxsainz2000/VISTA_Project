---
type: layer-manifest
module: MerchSys.App
layer: Services
last-updated: 2026-05-15
---

# MerchSys.App — Services

This page details the Service implementations specifically located within the **MerchSys.App** module (Composition Root). These typically include services with direct WPF or host dependencies.

## Infrastructure & UI Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.App/Configuration/ConnectionStringLoader.vb` | `ConnectionStringLoader` (Module) | Provides three-state overlay logic for production configuration. Loads `appsettings.Production.json` from `%LOCALAPPDATA%\VISTA\` to override connection strings without committing credentials. |
| `src/MerchSys.App/Data/DatabaseConfig.vb` | `DatabaseConfig` (Module) | Centralizes the SQLite database path (`%LOCALAPPDATA%\MerchSys\merchsys.db`) and provides the `AddModuleDbContexts` extension for DI registration of all four module contexts. |
| `src/MerchSys.App/Data/DatabaseInitializer.vb` | `DatabaseInitializer` (Module) | Applies all module migrations to the shared SQLite database on first run using ADO.NET. Workaround for EF Core 10's inability to discover VB.NET migration classes at runtime. |
| `src/MerchSys.App/Services/DefaultSessionService.vb` | `ISessionService`<br>`DefaultSessionService` | Stub singleton returning "Manager" session. To be replaced with a login-aware implementation in a future phase. |
| `src/MerchSys.App/Services/MediatREventBus.vb` | `IEventBus`<br>`MediatREventBus` | Thin adapter that delegates `IEventBus.PublishAsync` to MediatR's `IMediator.Publish`. Keeps module code depending on the narrower `IEventBus` interface. |
| `src/MerchSys.App/Services/WpfLowStockNotifier.vb` | `ILowStockNotifier`<br>`WpfLowStockNotifier` | WPF-specific implementation for low-stock alerts. Uses `Notification.Wpf`'s `NotificationManager` to display desktop toast notifications. Lives in the App layer to prevent WPF dependencies in the Inventory library. |
| `src/MerchSys.App/Startup/MediatRConfig.vb` | `MediatRConfig` (Module) | Extension module (`AddMediatRServices`) that registers MediatR and all cross-module handler assemblies. |
| `src/MerchSys.App/Startup/PosServiceRegistration.vb` | `PosServiceRegistration` (Module) | Extension module (`AddPosModule`) that registers POS services, view models, and the `ReceiptArchivalService`. |
| `src/MerchSys.App/Startup/SyncableRepositoryRegistration.vb` | `SyncableRepositoryRegistration` (Module) | Extension module (`AddSyncableRepositories`) that registers all module-specific `ISyncableRepository` implementations. |
| `src/MerchSys.App/Startup/SyncConfig.vb` | `SyncConfig` (Module) | Extension module (`AddSyncServices`) that configures DB sync, registering Contexts, Orchestrator, and Worker. |
| `src/MerchSys.App/Services/SyncOrchestrator.vb` | `SyncOrchestrator` | Iterates `ISyncableRepository` instances to synchronize local changes to MariaDB via the Conflict Resolver pipeline. |
| `src/MerchSys.App/Services/SyncWorker.vb` | `SyncWorker` | `BackgroundService` that executes the dual-condition probe and triggers the orchestrator. |
| `src/MerchSys.App/Services/DefaultNotificationService.vb` | `INotificationService`<br>`DefaultNotificationService` | Singleton that surfaces sync status changes and tracks `LastSuccessfulPushAt` for the UI shell. |

## Debug & Utilities
| File Path | Class | Description |
|---|---|---|
| `src/MerchSys.App/Debug/EventChainVerificationHarness.vb` | `EventChainVerificationHarness` | Orchestrates end-to-end event chain verification (GoodsReceived and SaleCompleted) against a scratch SQLite DB. |
| `src/MerchSys.App/Debug/EventChainReport.vb` | `EventChainReport` | Markdown report generator for the event chain verification results. |
