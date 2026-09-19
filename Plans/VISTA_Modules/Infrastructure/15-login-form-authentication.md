---
module: MerchSys.Infrastructure
plan-id: INFRA-15
title: "Login Form & User Authentication"
depends-on: [INFRA-01, INFRA-03]
estimated-files: 8
---

# Login Form & User Authentication

## Context

The system plan defines two user roles — **Manager** (full control) and **Owner** (read-only KPIs + notifications) — and specifies comprehensive OWASP DA Top 10 compliance, including DA2 (Authentication & Session), DA4 (Cryptography), and DA5 (Authorization). The system plan's Section 7 states:

> **System Users:** Manager (full control), Owner (read-only KPIs + notifications)

However, the current codebase has **no authentication mechanism**. `DefaultSessionService` (in `MerchSys.App/Services/`) always returns `UserRole.Manager` and username `"Manager"`, with a doc-comment that explicitly says: *"Replace with a login-aware implementation when a login screen is added in a future phase."*

This gap makes it impossible to:
- Switch between Manager and Owner roles at runtime
- Test Owner-restricted views (ACC checklist Test 8, POS checklist Test 13)
- Satisfy OWASP DA2 (Authentication & Session) or DA6 (no default credentials in release)

Multiple Operator verification checklists instruct the operator to "log in as Manager" or "log in as Owner" — these tests are currently **untestable**.

## Prerequisites

- **INFRA-01** (Solution Scaffold) — solution structure, `MerchSys.App` host project
- **INFRA-03** (Database Contexts) — `ApplicationDbContext` or shared database infrastructure, migration pipeline

## Wiki References

- `concepts/owasp-da-top10.md` — DA2 (Broken Authentication), DA4 (Improper Cryptography), DA5 (Improper Authorization), DA6 (Security Misconfiguration)
- `Sources/system_plan.md` — Section 7 (User Roles), Section 8 (Security Requirements)

## Deliverables

```
MerchSys.SharedKernel/Entities/
└── UserAccount.vb                                       ' New — user entity

MerchSys.App/Data/Migrations/
└── XXXXXXXX_AddUserAccountsTable.vb                     ' New — EF Core migration

MerchSys.App/Services/
├── IAuthenticationService.vb                            ' New — auth interface + implementation
├── LoginSessionService.vb                               ' New — ISessionService backed by authenticated user
└── DefaultSessionService.vb                             ' Modified — kept for #If DEBUG fallback

MerchSys.App/ViewModels/
└── LoginViewModel.vb                                    ' New

MerchSys.App/Views/
└── LoginView.xaml + LoginView.xaml.vb                   ' New

MerchSys.App/Application.xaml.vb                         ' Modified — show LoginView before MainWindow
```

## Specification

### UserAccount Entity

```
Namespace Entities

    Public Class UserAccount

        Public Property Id As Integer
        Public Property Username As String               ' Unique, case-insensitive
        Public Property PasswordHash As String           ' Argon2id hash
        Public Property Role As UserRole                 ' Manager = 1, Owner = 2
        Public Property IsActive As Boolean              ' Soft-disable without deleting
        Public Property FailedLoginAttempts As Integer   ' For lockout tracking
        Public Property LockedUntil As DateTime?         ' Null = not locked
        Public Property CreatedAt As DateTime
        Public Property ModifiedAt As DateTime?

    End Class

End Namespace
```

Table name: `Sys_UserAccounts`. Unique index on `Username`. The entity lives in `SharedKernel` so both `MerchSys.App` and any future module can reference the role/user type.

### Database Migration

The migration creates `Sys_UserAccounts` and seeds two default accounts:

| Username | Role | Default Password |
|----------|------|-----------------|
| `manager` | Manager | `Vista2026!` |
| `owner` | Owner | `Vista2026!` |

> **DA6 compliance note:** The system plan requires "No default credentials." To satisfy this, the login form must display a **mandatory password-change prompt** on first login when the account still uses the seed password. The `LastPasswordChangeAt` field (nullable) being `Nothing` signals first-login state. This is specified in the LoginViewModel section below.

Add `LastPasswordChangeAt As DateTime?` to the entity for this purpose.

### IAuthenticationService

```
Public Interface IAuthenticationService

    ''' <summary>
    ''' Validates credentials and returns the authenticated user, or Nothing on failure.
    ''' Increments FailedLoginAttempts on failure; resets on success.
    ''' Locks the account for 15 minutes after 5 consecutive failures (DA2).
    ''' </summary>
    Function AuthenticateAsync(username As String, password As String) As Task(Of AuthenticationResult)

    ''' <summary>
    ''' Changes the password for the specified user. Validates the old password first.
    ''' </summary>
    Function ChangePasswordAsync(userId As Integer, currentPassword As String, newPassword As String) As Task(Of PasswordChangeResult)

End Interface

Public Class AuthenticationResult
    Public Property Success As Boolean
    Public Property User As UserAccount                  ' Null on failure
    Public Property FailureReason As String              ' "Invalid credentials", "Account locked", "Account disabled"
    Public Property RemainingLockoutMinutes As Integer?   ' Only set when locked
End Class

Public Class PasswordChangeResult
    Public Property Success As Boolean
    Public Property ValidationErrors As IReadOnlyList(Of String)
End Class
```

**Implementation (`AuthenticationService`):**

1. Look up user by `Username` (case-insensitive) from `Sys_UserAccounts`.
2. If not found or `IsActive = False`, return failure — *"Invalid credentials"* (do not reveal whether username exists per OWASP).
3. If `LockedUntil > DateTime.UtcNow`, return failure — *"Account locked. Try again in {N} minutes."*
4. Verify password against `PasswordHash` using **Argon2id** via the `Konscious.Security.Cryptography.Argon2` NuGet package (the most maintained .NET Argon2id library).
5. If password incorrect: increment `FailedLoginAttempts`, set `LockedUntil` if `FailedLoginAttempts >= 5`, save, return failure.
6. If password correct: reset `FailedLoginAttempts` to 0, clear `LockedUntil`, update `ModifiedAt`, save, return success with the `UserAccount`.

**Argon2id parameters** (per system plan DA4):
- Memory: 19 MiB (19456 KiB)
- Iterations: 2
- Parallelism: 1
- Hash length: 32 bytes
- Salt: 16 bytes, `RandomNumberGenerator.Fill()`

Password stored as: `$argon2id$v=19$m=19456,t=2,p=1${base64Salt}${base64Hash}`

**`ChangePasswordAsync` validation:**
- New password must be ≥ 8 characters.
- New password must differ from current password.
- Old password must verify correctly before allowing change.

### LoginSessionService

```
Public Class LoginSessionService
    Implements ISessionService

    Private _currentUser As UserAccount

    Public Sub SetUser(user As UserAccount)
        _currentUser = user
    End Sub

    Public Sub ClearUser()
        _currentUser = Nothing
    End Sub

    Public ReadOnly Property CurrentUsername As String Implements ISessionService.CurrentUsername
        Get
            Return If(_currentUser?.Username, String.Empty)
        End Get
    End Property

    Public ReadOnly Property CurrentRole As UserRole Implements ISessionService.CurrentRole
        Get
            Return If(_currentUser?.Role, UserRole.Manager)
        End Get
    End Property

    Public ReadOnly Property IsAuthenticated As Boolean
        Get
            Return _currentUser IsNot Nothing
        End Get
    End Property

End Class
```

Registered as **singleton** in DI, replacing `DefaultSessionService`:

```
services.AddSingleton(Of LoginSessionService)()
services.AddSingleton(Of ISessionService)(Function(sp) sp.GetRequiredService(Of LoginSessionService)())
```

`DefaultSessionService` is retained but only registered under a `#If DEBUG` compile conditional with a config flag, allowing developers to bypass login during rapid debugging. The release build always requires authentication.

### LoginViewModel

```
Public Class LoginViewModel
    Inherits ObservableObject

    Public Sub New(auth As IAuthenticationService, session As LoginSessionService)
    End Sub

    <ObservableProperty>
    Private _username As String

    <ObservableProperty>
    Private _password As String                           ' Bound via SecurePassword helper

    <ObservableProperty>
    Private _errorMessage As String

    <ObservableProperty>
    Private _isLoggingIn As Boolean

    <ObservableProperty>
    Private _showPasswordChange As Boolean                ' True when first-login detected

    <ObservableProperty>
    Private _newPassword As String

    <ObservableProperty>
    Private _confirmNewPassword As String

    <RelayCommand>
    Private Async Function LoginAsync() As Task

    <RelayCommand>
    Private Async Function ChangePasswordAndLoginAsync() As Task

    ''' <summary>
    ''' Raised when authentication succeeds (and password change is complete if required).
    ''' The Application host subscribes to this to show MainWindow.
    ''' </summary>
    Public Event LoginSucceeded As EventHandler

End Class
```

**`LoginAsync` flow:**

1. Call `IAuthenticationService.AuthenticateAsync(Username, Password)`.
2. If failure: display `ErrorMessage` with the failure reason. Clear password field.
3. If success and `User.LastPasswordChangeAt Is Nothing`: set `ShowPasswordChange = True` (first-login password change required per DA6).
4. If success and password already changed: call `session.SetUser(result.User)`, raise `LoginSucceeded`.

**`ChangePasswordAndLoginAsync` flow:**

1. Validate `NewPassword = ConfirmNewPassword`. If not, show error.
2. Call `IAuthenticationService.ChangePasswordAsync(userId, currentPassword, NewPassword)`.
3. If success: update `LastPasswordChangeAt`, call `session.SetUser(...)`, raise `LoginSucceeded`.
4. If failure: display validation errors.

### LoginView.xaml

A centered login card with the application branding:

```
┌──────────────────────────────────────┐
│                                      │
│            [VISTA Logo]              │
│   Villon Integrated Supply & Trade   │
│            Application               │
│                                      │
│   ┌──────────────────────────────┐   │
│   │  Username                    │   │
│   └──────────────────────────────┘   │
│   ┌──────────────────────────────┐   │
│   │  Password            [👁]   │   │
│   └──────────────────────────────┘   │
│                                      │
│   [ Error message area ]             │
│                                      │
│   ┌──────────────────────────────┐   │
│   │          LOG IN              │   │
│   └──────────────────────────────┘   │
│                                      │
└──────────────────────────────────────┘
```

When `ShowPasswordChange = True`, a secondary panel slides in below the login fields:

```
│   New Password:      [________]      │
│   Confirm Password:  [________]      │
│   [  Set Password & Continue  ]      │
```

**WPF implementation notes:**
- Use `PasswordBox` for password fields (WPF security best practice — `SecureString` in memory).
- The `PasswordBox.Password` property is not bindable in WPF by design. Use the attached-property pattern (a `PasswordBoxHelper` that bridges the `PasswordChanged` event to a bindable property). This is a standard WPF pattern; do not introduce a third-party MVVM password-binding library.
- The eye icon toggles a `TextBox` overlay to reveal the password (common UX pattern).
- Enter key in the password field triggers `LoginCommand`.
- Style the card to match the existing app's dark/neutral theme (`MainWindow.xaml` styles).

### Application Startup Flow

Modify `Application.xaml.vb`:

1. On `OnStartup`, resolve `LoginView` and show it as a **standalone Window** (not as a navigation item inside the shell).
2. Subscribe to `LoginViewModel.LoginSucceeded`.
3. When `LoginSucceeded` fires: hide `LoginView`, resolve and show `MainWindow`, call `MainWindowViewModel.NavigateToDefault()`.
4. When `MainWindow` closes: shutdown the application.

### Logout

Add a "Log out" menu item or button to the `MainWindow` shell (e.g., in the top-right corner or in the status bar next to the sync indicator). On click:

1. Call `LoginSessionService.ClearUser()`.
2. Hide `MainWindow`.
3. Show `LoginView` with cleared fields.

This allows testing the Owner ↔ Manager role switch without restarting the app, directly enabling the checklist test POS-13 ("Log out and log in as Owner").

### DI Registration Summary

```
' Authentication
services.AddTransient(Of IAuthenticationService, AuthenticationService)()

' Session (replaces DefaultSessionService in Release)
services.AddSingleton(Of LoginSessionService)()
services.AddSingleton(Of ISessionService)(Function(sp) sp.GetRequiredService(Of LoginSessionService)())

' Login UI
services.AddTransient(Of LoginViewModel)()
services.AddTransient(Of LoginView)()
```

## Implementation Notes

- **Argon2id NuGet:** Use `Konscious.Security.Cryptography.Argon2` (latest stable). This is the most widely used .NET Argon2 implementation and satisfies DA4. Do **not** use `BCrypt.Net` — the system plan explicitly specifies Argon2id.
- **Session in-memory only (DA3):** The session state lives only in `LoginSessionService`'s `_currentUser` field. No session token is written to SQLite, disk, or config file. Clearing the reference on logout is sufficient.
- **No session timeout in V1:** The system plan mentions 15–30 minute inactivity timeout (DA2). This is a non-trivial UI concern (idle detection, warning dialog). Defer to a follow-up plan. Document this deferral in the implementation summary's "What's Next" section.
- **Password in PasswordBox:** WPF `PasswordBox` uses `SecureString` internally. The attached-property bridge converts to a regular `String` for the Argon2 hash computation. This is the standard WPF trade-off — `SecureString` cannot be passed to most crypto APIs without conversion. Document this in an inline comment.
- **Entity placement:** `UserAccount` is placed in `SharedKernel/Entities/` because `ISessionService` already lives in `SharedKernel/Interfaces/` and the role enum is in `SharedKernel/Enums/`. The entity needs to be accessible from `MerchSys.App` (for EF Core mapping) and potentially from module libraries that need to reference user IDs in audit columns.
- **EF Core mapping:** The `UserAccount` entity is mapped in the `MerchSys.App` database context (alongside other app-level tables like `Sync_Journal`). The table prefix is `Sys_` to distinguish system tables from module tables.
- Per the feedback memory: VB.NET `Await` is forbidden in `Catch`/`Finally`. All async error paths must capture the error and handle it after the `Try` block.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings.
2. Fresh database migration creates `Sys_UserAccounts` with two seeded users (manager, owner).
3. Launching the app shows a login form before the main shell.
4. Entering `manager` / `Vista2026!` authenticates successfully and shows the main shell with Manager navigation (including VAT Settings, VAT Return).
5. Entering `owner` / `Vista2026!` authenticates successfully and shows the main shell with Owner-restricted navigation (no VAT Settings, no VAT Return — per existing role gates in `MainWindowViewModel`).
6. First login with either default account triggers a mandatory password-change prompt (DA6 — no default credentials in production use).
7. After changing the password, subsequent logins require the new password.
8. 5 consecutive wrong passwords lock the account for 15 minutes. The login form displays the lockout message with remaining minutes.
9. A disabled account (`IsActive = False`) cannot log in.
10. Log out button in the shell clears the session, hides the main window, and returns to the login form.
11. Logging out as Manager and logging in as Owner in the same session changes the sidebar navigation items (enables POS checklist Test 13).
12. Password hash stored in `Sys_UserAccounts` uses Argon2id with the parameters specified in DA4.
13. No password or session token is written to SQLite, disk, or any log output (DA3).

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-15-summary.md` using `Progress/_template.md`. Include:

- Confirmation of Argon2id parameters matching the system plan DA4 specification.
- The `PasswordBoxHelper` attached-property pattern used and a note about the `SecureString` → `String` trade-off.
- The startup flow change in `Application.xaml.vb` (LoginView → LoginSucceeded → MainWindow).
- What was deferred: session inactivity timeout (DA2 partial).

### Documentation
- XML doc on `IAuthenticationService` describing the lockout policy and Argon2id parameter choices.
- XML doc on `LoginSessionService` explaining that it replaces `DefaultSessionService` and clarifying the in-memory-only session contract.
- Inline comment on the `PasswordBoxHelper` explaining why `SecureString` cannot be preserved end-to-end.
- Inline comment on the seed migration noting the DA6 first-login password-change requirement.
