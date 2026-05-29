---
type: layer-manifest
module: MerchSys.App
layer: Services
last-updated: 2026-05-29
---

# MerchSys.App — Services

This page details the Service implementations specifically located within the **MerchSys.App** module (Composition Root). These typically include services with direct WPF or host dependencies.

## Infrastructure & UI Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.App/Configuration/ConnectionStringLoader.vb` | `ConnectionStringLoader` (Module) | Provides three-state overlay logic for production configuration. Loads `appsettings.Production.json` from `%LOCALAPPDATA%\VISTA\` to override connection strings without committing credentials. |
| `src/MerchSys.App/Data/DatabaseConfig.vb` | `DatabaseConfig` (Module) | Provides the `AddModuleDbContexts` extension for DI registration of all four module contexts, injecting the `RoleGuardInterceptor` interceptor for OWASP DA5 database-level write rejection. |
| `src/MerchSys.App/Data/MariaDbSchemaInitializer.vb` | `MariaDbSchemaInitializer` (Module) | Bootstraps the central MariaDB database schema (40 tables), seeds reference data, and seeds default user accounts transactionally. Enforces SHA-256 drift detection to cleanly abort app startup on script tampering. |
| `src/MerchSys.App/Services/DefaultSessionService.vb` | `ISessionService`<br>`DefaultSessionService` | Stub singleton returning "Manager" session. Implements `IsAuthenticated = True`. Used for development bypass. |
| `src/MerchSys.App/Services/MediatREventBus.vb` | `IEventBus`<br>`MediatREventBus` | Thin adapter that delegates `IEventBus.PublishAsync` to MediatR's `IMediator.Publish` (INT-11). Resolves missing DI registration crash for POS view navigation. |
| `src/MerchSys.App/Services/WpfLowStockNotifier.vb` | `ILowStockNotifier`<br>`WpfLowStockNotifier` | WPF-specific implementation for low-stock alerts. Uses `Notification.Wpf`'s `NotificationManager` to display desktop toast notifications. Lives in the App layer to prevent WPF dependencies in the Inventory library. |
| `src/MerchSys.App/Startup/MediatRConfig.vb` | `MediatRConfig` (Module) | Extension module (`AddMediatRServices`) that registers MediatR and all cross-module handler assemblies. |
| `src/MerchSys.App/Startup/PosServiceRegistration.vb` | `PosServiceRegistration` (Module) | Extension module (`AddPosModule`) that registers POS services, view models, and the `ReceiptArchivalService`. |
| `src/MerchSys.App/Services/DefaultNotificationService.vb` | `INotificationService`<br>`DefaultNotificationService` | Singleton that surfaces user-facing toast notifications (success, error) to the UI shell. |
| `src/MerchSys.App/Services/IAuthenticationService.vb` | `IAuthenticationService`<br>`AuthenticationService` | Validates credentials and returns authenticated user. Implements Argon2id hashing and lockout policy. |
| `src/MerchSys.App/Services/LoginSessionService.vb` | `ISessionService`<br>`LoginSessionService` | Session service backed by authenticated UserAccount. Replaces DefaultSessionService, implementing `IsAuthenticated` and `CurrentRole`. |
| `src/MerchSys.App/Services/IIdleMonitor.vb`<br>`src/MerchSys.App/Services/WpfIdleMonitor.vb` | `IIdleMonitor`<br>`WpfIdleMonitor` | OWASP DA2 session inactivity timeout. Hooks DispatcherTimer and InputManager.PreProcessInput to monitor user activity, prompting warning dialog or forced logout. |
| `src/MerchSys.App/Helpers/PasswordBoxHelper.vb` | `PasswordBoxHelper` | Attached-property bridge for PasswordBox.Password binding to ViewModel string. |
| `src/MerchSys.App/Services/IConnectionHealthMonitor.vb`<br>`src/MerchSys.App/Services/ConnectionHealthMonitor.vb` | `IConnectionHealthMonitor`<br>`ConnectionHealthMonitor` | Periodic MariaDB SELECT 1 health probe with a three-state machine (Online, Reconnecting, Offline), exponential backoff, and UI thread event dispatching. |
| `src/MerchSys.App/Services/ConnectionHealthMonitorLocator.vb` | `ConnectionHealthMonitorLocator` (Module) | Static accessor used by UI attached behaviors to check current connection health. |
| `src/MerchSys.App/Startup/ConnectionConfig.vb` | `ConnectionConfig` (Module) | Extension module (`AddConnectionHealthMonitor`) that registers connection health monitor services. |

## Debug & Utilities
| File Path | Class | Description |
|---|---|---|
| `src/MerchSys.App/Startup/DebugServiceRegistration.vb` | `DebugServiceRegistration` (Module) | Registers DebugMenuView as Transient in DI for developer diagnostics. |
