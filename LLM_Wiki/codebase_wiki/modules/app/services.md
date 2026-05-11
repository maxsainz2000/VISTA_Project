---
type: layer-manifest
module: MerchSys.App
layer: Services
last-updated: 2026-05-11
---

# MerchSys.App — Services

This page details the Service implementations specifically located within the **MerchSys.App** module (Composition Root). These typically include services with direct WPF or host dependencies.

## Infrastructure & UI Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.App/Data/DatabaseConfig.vb` | `DatabaseConfig` (Module) | Centralizes the SQLite database path (`%LOCALAPPDATA%\MerchSys\merchsys.db`) and provides the `AddModuleDbContexts` extension for DI registration of all four module contexts. |
| `src/MerchSys.App/Data/DatabaseInitializer.vb` | `DatabaseInitializer` (Module) | Applies all module migrations to the shared SQLite database on first run using ADO.NET. Workaround for EF Core 10's inability to discover VB.NET migration classes at runtime. |
| `src/MerchSys.App/Services/DefaultSessionService.vb` | `ISessionService`<br>`DefaultSessionService` | Stub singleton returning "Manager" session. To be replaced with a login-aware implementation in a future phase. |
| `src/MerchSys.App/Services/MediatREventBus.vb` | `IEventBus`<br>`MediatREventBus` | Thin adapter that delegates `IEventBus.PublishAsync` to MediatR's `IMediator.Publish`. Keeps module code depending on the narrower `IEventBus` interface. |
| `src/MerchSys.App/Services/WpfLowStockNotifier.vb` | `ILowStockNotifier`<br>`WpfLowStockNotifier` | WPF-specific implementation for low-stock alerts. Uses `Notification.Wpf`'s `NotificationManager` to display desktop toast notifications. Lives in the App layer to prevent WPF dependencies in the Inventory library. |
| `src/MerchSys.App/Startup/MediatRConfig.vb` | `MediatRConfig` (Module) | Extension module (`AddMediatRServices`) that registers MediatR and all cross-module handler assemblies with the DI container. |
| `src/MerchSys.App/Services/SyncOrchestrator.vb` | `SyncOrchestrator` | Iterates `ISyncableRepository` instances to synchronize local changes to MariaDB via the Conflict Resolver pipeline. |
| `src/MerchSys.App/Services/SyncWorker.vb` | `SyncWorker` | `BackgroundService` that executes the dual-condition probe and triggers the orchestrator. |
| `src/MerchSys.App/Services/DefaultNotificationService.vb` | `INotificationService`<br>`DefaultNotificationService` | Singleton that surfaces sync status changes to the UI shell. |
