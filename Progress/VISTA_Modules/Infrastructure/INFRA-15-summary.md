---
module: Infrastructure
agent: claude-code
date: 2026-05-22
plan-ref: Plans/VISTA_Modules/Infrastructure/15-login-form-authentication.md
status: completed
---

## Task Summary

Implemented full login form and user authentication (INFRA-15). Covers OWASP DA2 (authentication & session), DA4 (Argon2id cryptography), DA5 (role-based navigation), and DA6 (no default credentials — mandatory first-login password change).

**Plan:** `[[15-login-form-authentication]]`

## What Was Done

- Created `src/MerchSys.SharedKernel/Entities/UserAccount.vb` — new entity with `Id`, `Username`, `PasswordHash`, `Role`, `IsActive`, `FailedLoginAttempts`, `LockedUntil`, `LastPasswordChangeAt`, `CreatedAt`, `ModifiedAt`
- Created `src/MerchSys.App/Services/IAuthenticationService.vb` — interface, `AuthenticationResult`, `PasswordChangeResult`, `AuthenticationService` implementation (raw ADO.NET over `SqliteConnection` per EF Core VB.NET bug workaround), and `Friend Module PasswordHashHelper` (Argon2id hash/verify with 16-byte random salt, 19456 KiB memory, 2 iterations, 1 parallelism, 32-byte output)
- Created `src/MerchSys.App/Services/LoginSessionService.vb` — `ISessionService` backed by `UserAccount`; in-memory only (DA3); `ClearUser()` on logout
- Created `src/MerchSys.App/Helpers/PasswordBoxHelper.vb` — attached-property bridge (`IsMonitoring` + `BoundPassword`) that binds `PasswordBox.Password` to a ViewModel string; `[ThreadStatic]` guard prevents re-entrant updates
- Created `src/MerchSys.App/ViewModels/LoginViewModel.vb` — `LoginCommand` (AsyncRelayCommand), `ChangePasswordCommand`, eye-toggle commands; computed inverse properties (`HidePassword`, `HasError`, `IsNotLoggingIn`, `HidePasswordChange`, `HideNewPassword`) for XAML binding without custom converters; `Reset()` for post-logout re-display; `LoginSucceeded` event
- Created `src/MerchSys.App/Views/LoginView.xaml` + `LoginView.xaml.vb` — standalone `Window` with dark branding, username/password fields with eye-toggle reveal, error message area, LOG IN button, and a first-login password-change panel (revealed when `ShowPasswordChange = True`)
- Modified `src/MerchSys.App/Data/DatabaseInitializer.vb` — added migration `20260522100000_AddUserAccounts`: creates `Sys_UserAccounts` (NOCASE collation on `Username`, unique index), seeds `manager` (Role=1) and `owner` (Role=2) rows with Argon2id-hashed `Vista2026!`; `LastPasswordChangeAt = NULL` signals first-login state
- Modified `src/MerchSys.App/MerchSys.App.vbproj` — added `Konscious.Security.Cryptography.Argon2 v1.3.0`
- Modified `src/MerchSys.App/Services/DefaultSessionService.vb` — updated doc comment: DEBUG-bypass only, never ships to production
- Modified `src/MerchSys.App/Application.xaml` — added `ShutdownMode="OnExplicitShutdown"` so hiding `LoginView` (on successful login) does not trigger app shutdown
- Modified `src/MerchSys.App/Application.xaml.vb` — new login flow: `ShowLoginView()` → `HandleLoginSucceeded` (hides `LoginView`, resolves singleton `MainWindow`, calls `RefreshNavigation()`, shows `MainWindow`) → `HandleLogoutRequested` (hides `MainWindow`, calls `ShowLoginView()` again); `HandleLoginViewClosed` shuts down if login view is dismissed without completing auth; `LoginSessionService` + `IAuthenticationService` registered in DI; `DefaultSessionService` kept for `#If DEBUG` + `VISTA_BYPASS_LOGIN=1` env-var override
- Modified `src/MerchSys.App/ViewModels/MainWindowViewModel.vb` — constructor now injects `LoginSessionService` directly; `NavigationGroups` initialised as `ObservableCollection` from `BuildNavigationGroups()` list; `BuildNavigationGroups()` return type changed to `List(Of NavigationGroup)`; added `RefreshNavigation()` (clears and rebuilds nav items — enables role switch after logout/re-login); added `LogoutCommand` (RelayCommand) and `LogoutRequested` event
- Modified `src/MerchSys.App/MainWindow.xaml` — added "Log Out" button pinned to the bottom of the sidebar (above the status bar), bound to `LogoutCommand`, styled in red (`#E74C3C`) via the existing `NavItemButton` style

## Argon2id Parameters

| Parameter | Value | DA4 Spec |
|---|---|---|
| Memory | 19 456 KiB | 19 MiB ✅ |
| Iterations | 2 | 2 ✅ |
| Parallelism | 1 | 1 ✅ |
| Hash length | 32 bytes | 32 bytes ✅ |
| Salt | 16 bytes, `RandomNumberGenerator.Fill` | 16 bytes random ✅ |
| Format | `$argon2id$v=19$m=19456,t=2,p=1$<salt>$<hash>` | ✅ |

## PasswordBoxHelper Pattern

`PasswordBoxHelper` uses two attached properties:
- `IsMonitoring="True"` — subscribes to `PasswordBox.PasswordChanged` and writes to `BoundPassword`
- `BoundPassword="{Binding Password, Mode=TwoWay}"` — syncs with the ViewModel string

**SecureString → String trade-off:** WPF's `PasswordBox` holds the password in a `SecureString` internally. The helper converts to `String` at `PasswordChanged` so Argon2id can receive a `byte[]` (computed from `Encoding.UTF8.GetBytes`). `SecureString` cannot be preserved end-to-end with Argon2id — this is the standard WPF trade-off, documented inline.

## Startup Flow

```
Application_Startup
  → ShowLoginView()               (LoginView shown as standalone Window)
  → User enters credentials
  → HandleLoginSucceeded          (LoginView.Hide, MainWindow.Show, RefreshNavigation)
  → User clicks Log Out
  → HandleLogoutRequested         (MainWindow.Hide, ShowLoginView again)
  → User closes MainWindow (X)
  → HandleMainWindowClosed        → Shutdown()
```

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | ✅ Passed (Argon2id hashing, mandatory password change, role-based sidebar, and account lockout verified) |

## Issues Encountered

None — build succeeded on first attempt.

## What's Next

- [x] Operator verification: log in as `manager` / `Vista2026!` — should trigger mandatory password change prompt (DA6 criterion 6) *(completed/verified in Operator checklist)*
- [x] Operator verification: log in as `owner` / new password — should show Owner-restricted navigation (no VAT Settings, no VAT Return) (criterion 5) *(completed/verified in Operator checklist)*
- [x] Operator verification: 5 consecutive wrong passwords triggers lockout with remaining-minutes message (criterion 8) *(completed/verified in Operator checklist)*
- [x] Operator verification: Log Out → log in as Owner in same session → nav items change (criterion 11 / POS-13) *(completed/verified in Operator checklist)*
- [ ] **Deferred:** Session inactivity timeout (DA2 partial — 15–30 min idle detection + warning dialog). Non-trivial UI concern; follow-up plan required. Documented here per plan spec.

## Cross-References

- Domain Wiki: `[[owasp-da-top10]]`, `[[system_plan Section 7 & 8]]`
- Agent Wiki: `[[efcore10-vbnet-migration-discovery-bug]]`, `[[vbnet-await-catch-finally]]`
