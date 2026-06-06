---
type: layer-manifest
module: MerchSys.App
layer: Services
last-updated: 2026-06-06
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
| `src/MerchSys.App/Services/DefaultNotificationService.vb` | `INotificationService`<br>`DefaultNotificationService` | Singleton that surfaces user-facing toast notifications (success, error, information, warning) to the UI shell. Extended to support optional action buttons (UX-29). |
| `src/MerchSys.App/Services/DefaultConflictPresenter.vb` | `IConflictPresenter`<br>`DefaultConflictPresenter` | UI presenter that displays the `ConcurrencyConflictPrompt` dialog to handle optimistic concurrency write conflicts. |
| `src/MerchSys.App/Services/DefaultConfirmationPresenter.vb` | `IConfirmationPresenter`<br>`DefaultConfirmationPresenter` | UI presenter that displays the `ConfirmationDialog` to gate destructive, irreversible, or financial actions. |
| `src/MerchSys.App/Services/IAuthenticationService.vb` | `IAuthenticationService`<br>`AuthenticationService` | Validates credentials and returns authenticated user. Implements Argon2id hashing and lockout policy. |
| `src/MerchSys.App/Services/LoginSessionService.vb` | `ISessionService`<br>`LoginSessionService` | Session service backed by authenticated UserAccount. Replaces DefaultSessionService, implementing `IsAuthenticated` and `CurrentRole`. |
| `src/MerchSys.App/Services/IIdleMonitor.vb`<br>`src/MerchSys.App/Services/WpfIdleMonitor.vb` | `IIdleMonitor`<br>`WpfIdleMonitor` | OWASP DA2 session inactivity timeout. Hooks DispatcherTimer and InputManager.PreProcessInput to monitor user activity, prompting warning dialog or forced logout. |
| `src/MerchSys.App/Helpers/PasswordBoxHelper.vb` | `PasswordBoxHelper` | Attached-property bridge for PasswordBox.Password binding to ViewModel string. |
| `src/MerchSys.App/Helpers/FormHelper.vb` | `FormHelper` | Attached-property module providing `IsRequired` (shows/hides red required-field asterisk) and `InputMode` (`PositiveInteger`, `PositiveDecimal`) enforcing character filtering via `PreviewTextInput` regex checks, pasting interception, right-alignment, and lost-focus decimal formatting (`F2`) (UX-18). |
| `src/MerchSys.App/Services/IConnectionHealthMonitor.vb`<br>`src/MerchSys.App/Services/ConnectionHealthMonitor.vb` | `IConnectionHealthMonitor`<br>`ConnectionHealthMonitor` | Periodic MariaDB SELECT 1 health probe with a three-state machine (Online, Reconnecting, Offline), exponential backoff, and UI thread event dispatching. |
| `src/MerchSys.App/Services/ConnectionHealthMonitorLocator.vb` | `ConnectionHealthMonitorLocator` (Module) | Static accessor used by UI attached behaviors to check current connection health. |
| `src/MerchSys.App/Startup/ConnectionConfig.vb` | `ConnectionConfig` (Module) | Extension module (`AddConnectionHealthMonitor`) that registers connection health monitor services. |
| `src/MerchSys.App/Services/Theming/IThemeService.vb`<br>`src/MerchSys.App/Services/Theming/ThemeService.vb`<br>`src/MerchSys.App/Services/Theming/AppTheme.vb` | `IThemeService`<br>`ThemeService`<br>`AppTheme` (Enum) | Manages application-wide theme states (Light/Dark). Hot-swaps MergedDictionaries at runtime. Reads/writes theme preference via `UiSettingsStore` (which owns `%LOCALAPPDATA%\MerchSys\ui-settings.json`); no longer reads the file directly (UX-23). |
| `src/MerchSys.App/Services/UiSettingsStore.vb` | `UiSettingsStore` | Singleton JSON store for all per-laptop UI preferences. Reads/writes `%LOCALAPPDATA%\MerchSys\ui-settings.json` with all known keys (`theme`, `windowLeft`, `windowTop`, `windowWidth`, `windowHeight`, `windowMaximized`, `lastViewKey`, `favoriteKeys`, `recentKeys`). Loads eagerly on construction; `Save()` is called by both `ThemeService` and `WindowPlacementService` so neither save clobbers the other's keys (UX-23, UX-38). |
| `src/MerchSys.App/Services/WindowPlacementService.vb` | `WindowPlacement` (data class)<br>`WindowPlacementService` | Saves and restores per-laptop window size, position, and maximized state via `UiSettingsStore`. `LoadPlacement()` validates saved bounds against `SystemParameters.VirtualScreen*`, requires ≥ 100×30 px on-screen, and returns `Nothing` (triggering `CenterScreen + Maximized` fallback) on off-screen saves. `SavePlacement(window)` captures `RestoreBounds` when maximized so un-maximize restores a sensible size (UX-23). |
| `src/MerchSys.App/Services/IUserPreferencesService.vb`<br>`src/MerchSys.App/Services/UserPreferencesService.vb` | `IUserPreferencesService`<br>`UserPreferencesService` | Persists per-laptop user preferences (last-view screen, favorites list, and bounded recents list) through `UiSettingsStore`. Provides de-duplicated recents tracking and favorite synchronization for navigation items (UX-38). |

## Debug & Utilities
| File Path | Class | Description |
|---|---|---|
| `src/MerchSys.App/Startup/DebugServiceRegistration.vb` | `DebugServiceRegistration` (Module) | Registers DebugMenuView as Transient in DI for developer diagnostics. |
