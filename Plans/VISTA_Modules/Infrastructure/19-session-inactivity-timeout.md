---
module: MerchSys.Infrastructure
plan-id: INFRA-19
title: "Session Inactivity Timeout (DA2 Completion)"
depends-on: [INFRA-15]
estimated-files: 6
priority: medium
---

# Session Inactivity Timeout (DA2 Completion)

## Context

OWASP DA2 (Authentication & Session) is partially satisfied by INFRA-15 (login, hashed credentials, logout, in-memory session). The remaining DA2 requirement — drawn from `concepts/owasp-da-top10.md` and `Sources/system_plan.md` Section 8 — is a **15–30 minute inactivity timeout** that invalidates the session and returns the user to the login screen.

INFRA-15 deliberately deferred this as a "non-trivial UI concern" (see `Progress/VISTA_Modules/Infrastructure/INFRA-15-summary.md` What's Next, and `Plans/Future/deferred-features-backlog.md` item #6). The login plumbing is already in place:

- `LoginSessionService` (singleton, `MerchSys.App/Services/LoginSessionService.vb`) — `SetUser` / `ClearUser` / `IsAuthenticated`
- `Application.xaml.vb` — `HandleLogoutRequested` already hides `MainWindow` and re-shows `LoginView`
- `MainWindowViewModel.LogoutRequested` event — the standard logout entrypoint

This plan adds **idle detection**, a **countdown warning dialog**, and **automatic logout** on timeout, reusing the existing logout flow rather than creating a parallel one.

## Prerequisites

- **INFRA-15** (Login Form & User Authentication) — completed; provides `LoginSessionService`, `MainWindowViewModel.LogoutRequested`, and the `LoginView` re-show flow

## Wiki References

- `concepts/owasp-da-top10.md` — DA2 (Broken Authentication & Session Management)
- `Sources/system_plan.md` — Section 8 (Security Requirements)
- `agent_wiki/errors/efcore10-vbnet-tolistasync-empty.md` — informational (not relevant here; this plan adds no DB reads)

## Deliverables

```
MerchSys.App/Services/
├── IIdleMonitor.vb                              ' New — interface + IdleMonitorOptions + events
└── WpfIdleMonitor.vb                            ' New — DispatcherTimer + InputManager hook

MerchSys.App/ViewModels/
└── SessionTimeoutWarningViewModel.vb            ' New — countdown VM with Extend / Sign Out commands

MerchSys.App/Views/
├── SessionTimeoutWarningView.xaml               ' New — modal dialog
└── SessionTimeoutWarningView.xaml.vb            ' New — code-behind

MerchSys.App/Application.xaml.vb                 ' Modified — wire monitor into login/logout flow + DI
MerchSys.App/appsettings.json                    ' Modified — add Session:IdleTimeoutMinutes
```

## Specification

### IIdleMonitor (and IdleMonitorOptions)

Lives in `MerchSys.App/Services/IIdleMonitor.vb`.

```vb
Public Class IdleMonitorOptions
    ''' <summary>Idle duration before the session is force-logged-out. DA2 spec: 15–30 min.</summary>
    Public Property IdleTimeoutMinutes As Integer = 20

    ''' <summary>Seconds of remaining idle before the warning dialog is shown. Default: 60.</summary>
    Public Property WarningLeadSeconds As Integer = 60
End Class

Public Class IdleMonitorWarningEventArgs
    Inherits EventArgs
    Public Property RemainingSeconds As Integer
End Class

Public Interface IIdleMonitor
    ''' <summary>True once Start has been called and Stop has not.</summary>
    ReadOnly Property IsRunning As Boolean

    ''' <summary>Whole seconds since the last recorded user activity.</summary>
    ReadOnly Property IdleSeconds As Integer

    ''' <summary>Whole seconds remaining before forced logout (0 if monitor is stopped).</summary>
    ReadOnly Property RemainingSeconds As Integer

    Sub Start()
    Sub [Stop]()

    ''' <summary>
    ''' Manually reset the idle clock. Called by the WPF input hook on any keyboard or
    ''' pointer event, and also by the warning dialog's "Stay signed in" button.
    ''' </summary>
    Sub RecordActivity()

    ''' <summary>
    ''' Raised once per warning window when <see cref="RemainingSeconds"/> first drops to
    ''' <c>WarningLeadSeconds</c>. Re-raised only after a subsequent <see cref="RecordActivity"/>.
    ''' </summary>
    Event WarningShown As EventHandler(Of IdleMonitorWarningEventArgs)

    ''' <summary>
    ''' Raised once when the idle threshold is reached. Subscriber must perform the logout.
    ''' </summary>
    Event SessionExpired As EventHandler
End Interface
```

### WpfIdleMonitor

Lives in `MerchSys.App/Services/WpfIdleMonitor.vb`. Singleton.

**Construction:**
- Inject `IdleMonitorOptions` (registered as a singleton from `IConfiguration` — see DI section)
- Clamp `IdleTimeoutMinutes` into the DA2-compliant range `[15, 30]` in the constructor; if the configured value is outside, log a warning and use the nearest boundary

**Activity tracking:**
- Hook `InputManager.Current.PreProcessInput` once when `Start()` is called
  - Filter for events whose `StagingItem.Input` is a `KeyEventArgs`, `MouseEventArgs`, or `TouchEventArgs` — ignore the synthetic ones that fire during routing
  - Calling `e.Cancel()` is **never** appropriate here; the hook is observation-only
- `_lastActivityUtc = DateTime.UtcNow` on every activity event and on every `RecordActivity()` call

**Tick loop:**
- `DispatcherTimer` on `Application.Current.Dispatcher`, interval `TimeSpan.FromSeconds(1)`
- On each tick:
  1. Compute `idleSeconds = (UtcNow - _lastActivityUtc).TotalSeconds`
  2. Compute `remainingSeconds = Math.Max(0, timeoutSeconds - idleSeconds)`
  3. If `remainingSeconds = 0` and `_expiredRaised = False`: set `_expiredRaised = True`, `Stop()`, raise `SessionExpired`
  4. Else if `remainingSeconds <= WarningLeadSeconds` and `_warningRaised = False`: set `_warningRaised = True`, raise `WarningShown(remainingSeconds)`
- `RecordActivity()` clears both `_warningRaised` and `_expiredRaised` so a recovery extends the cycle

**Start/Stop:**
- `Start()` is idempotent — calling twice does not double-hook `InputManager`
- `Stop()` unhooks `InputManager.PreProcessInput`, stops the `DispatcherTimer`, clears `_warningRaised`/`_expiredRaised`, and sets `IsRunning = False`
- Disposing the singleton (on app exit) must call `Stop()` — implement `IDisposable` for safety

### SessionTimeoutWarningViewModel

Lives in `MerchSys.App/ViewModels/SessionTimeoutWarningViewModel.vb`. `CommunityToolkit.Mvvm`-based.

- `[ObservableProperty] RemainingSeconds As Integer`
- `[ObservableProperty] CountdownDisplay As String` — formatted `"0:59"`, `"0:58"`, …, `"0:00"`. Setter on `RemainingSeconds` recomputes this.
- `[RelayCommand] StaySignedIn` — raises a `StayRequested` event consumed by `Application.xaml.vb` (which calls `_idleMonitor.RecordActivity()` and closes the dialog)
- `[RelayCommand] SignOut` — raises a `SignOutRequested` event (consumed by `Application.xaml.vb`, which closes the dialog and routes through the existing `HandleLogoutRequested` path)
- Public method `Tick(remaining As Integer)` — called by the owning Application each second while the dialog is open; updates `RemainingSeconds`

### SessionTimeoutWarningView

A modal `Window` (not a `UserControl`).

- `WindowStartupLocation="CenterOwner"`, `ResizeMode="NoResize"`, `ShowInTaskbar="False"`, `Topmost="True"`
- Owner set to `MainWindow` at construction time
- Layout: large countdown text (e.g. `0:42`), explanatory message ("You will be signed out for inactivity in 42 seconds."), two buttons: **Stay signed in** (primary) and **Sign out now** (secondary)
- On `Window.Closed` without an explicit decision (user clicked the close `X`): treat as **Sign out** — never silently extend the session
- Code-behind subscribes the buttons to the VM's commands; no business logic in code-behind

### Application.xaml.vb — wiring

**DI registration (inside `ConfigureServices`):**

```vb
' Infrastructure: Idle monitor (DA2)
services.AddSingleton(Of IdleMonitorOptions)(
    Function(sp)
        Dim cfg = sp.GetRequiredService(Of IConfiguration)()
        Return New IdleMonitorOptions With {
            .IdleTimeoutMinutes = cfg.GetValue(Of Integer)("Session:IdleTimeoutMinutes", 20),
            .WarningLeadSeconds = cfg.GetValue(Of Integer)("Session:WarningLeadSeconds", 60)
        }
    End Function)
services.AddSingleton(Of IIdleMonitor, WpfIdleMonitor)()
services.AddTransient(Of SessionTimeoutWarningViewModel)()
services.AddTransient(Of SessionTimeoutWarningView)()
```

**Lifecycle hooks (add to `Application` class):**

- Add private fields `_idleMonitor As IIdleMonitor` and `_warningView As SessionTimeoutWarningView`
- In `Application_Startup`, after `mainVm = ...`:
  ```vb
  _idleMonitor = _host.Services.GetRequiredService(Of IIdleMonitor)()
  AddHandler _idleMonitor.WarningShown, AddressOf HandleIdleWarning
  AddHandler _idleMonitor.SessionExpired, AddressOf HandleSessionExpired
  ```
- In `HandleLoginSucceeded`, after `_mainWindow.Show()`:
  ```vb
  _idleMonitor.Start()
  ```
- In `HandleLogoutRequested`, **before** `_mainWindow.Hide()`:
  ```vb
  _idleMonitor.Stop()
  CloseWarningDialogIfOpen()
  ```
- Add `HandleIdleWarning(sender, e As IdleMonitorWarningEventArgs)`:
  - Resolve `SessionTimeoutWarningView` from DI
  - Set `Owner = _mainWindow`, subscribe to its VM's `StayRequested` and `SignOutRequested` events
  - Call `Dispatcher.Invoke` if not on UI thread (the `DispatcherTimer` is already on UI thread, but the `InputManager` hook may surface on background threads in edge cases)
  - Start a per-second `DispatcherTimer` in `HandleIdleWarning` that calls `_warningView.ViewModel.Tick(_idleMonitor.RemainingSeconds)` while the dialog is open; stop the timer when the dialog closes
  - `ShowDialog()` is blocking — must use `Show()` (non-modal) so the underlying app remains visible behind the topmost dialog (per UX requirement: countdown visible without freezing background)
- Add `HandleSessionExpired(sender, e)`:
  - `CloseWarningDialogIfOpen()`
  - Resolve the singleton `LoginSessionService` directly (not via `ISessionService` since `ClearUser` is on the concrete class) and call `ClearUser()`
  - Invoke `mainVm.LogoutCommand.Execute(Nothing)` to fire the standard `LogoutRequested` event that `HandleLogoutRequested` already handles
- `CloseWarningDialogIfOpen()` — null-safe close + handler removal + null-out of `_warningView`

### appsettings.json (new section)

```json
{
  "Session": {
    "IdleTimeoutMinutes": 20,
    "WarningLeadSeconds": 60
  }
}
```

If a production overlay file is in use (per `AddProductionOverlay`), document the same keys there as overridable.

## Implementation Notes

- **DEBUG bypass:** mirror the `VISTA_BYPASS_LOGIN` pattern. Under `#If DEBUG Then`, check `Environment.GetEnvironmentVariable("VISTA_DISABLE_IDLE_TIMEOUT")` — when `= "1"`, register a no-op stub `IIdleMonitor` whose `Start()`/`Stop()` do nothing. This avoids surprising developers during long debugging sessions. **Must not ship to release builds**, hence the `#If DEBUG` guard.
- **`Await` in `Catch`/`Finally` (project trap):** the idle monitor is fully synchronous — no `Await` anywhere — so this trap doesn't apply here. Do not add `Async` to `Start`/`Stop`/`RecordActivity` even if tempted to await the dispatcher.
- **Reserved-name traps:** do **not** name a parameter `now`, `date`, `err`, or `stop`. Use `currentUtc`, `recordedAt`, `errMsg`, and the bracketed `[Stop]` only for the method name.
- **Parameter-shadows-property trap:** the `Tick(remaining As Integer)` method name `remaining` does not collide with the `RemainingSeconds` property, but reviewers should still check that `remaining` is not aliased to a same-named property in the VM (case-insensitive).
- **No new DB tables.** Configuration lives in `appsettings.json`. Session state remains in-memory only (DA3).
- **`SessionExpired` reuses the existing logout flow.** Do not duplicate `HandleLogoutRequested`; route through `mainVm.LogoutCommand` so future logout-side-effects (audit logging, sync flush, etc.) automatically apply.
- **Owner of the warning window** is `_mainWindow`. The dialog must not appear over `LoginView` — if `_mainWindow.IsVisible = False` when `HandleIdleWarning` fires, swallow the event and treat it as already-expired (call `HandleSessionExpired` directly). This handles the edge case of the user clicking Log Out at the exact moment the warning fires.
- **No raising on UI dispose:** when the app shuts down (`Application_Exit`), call `_idleMonitor.Stop()` before disposing the host so the timer cannot fire against a disposed DI container.

## Acceptance Criteria

1. `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` succeeds with 0 errors, 0 warnings.
2. With `Session:IdleTimeoutMinutes = 20`: after 19 minutes of no input, the warning dialog appears with a 60-second countdown.
3. Clicking **Stay signed in** dismisses the dialog and resets the idle clock — the next warning fires no sooner than 19 minutes later.
4. Clicking **Sign out now** (or closing the dialog with X) returns to `LoginView` via the existing logout flow.
5. If the user does nothing during the 60-second countdown, the session is force-terminated, `LoginSessionService.ClearUser()` is called, and `LoginView` is shown.
6. Any keyboard or pointer input during the warning countdown resets the idle clock and closes the dialog (verifies the `InputManager.PreProcessInput` hook).
7. After logout (either manual via the Log Out button or automatic via timeout), `_idleMonitor.IsRunning = False` and re-login restarts the monitor cleanly with a fresh clock.
8. Setting `VISTA_DISABLE_IDLE_TIMEOUT=1` in a DEBUG build disables the monitor (no warnings, no auto-logout); the env var has no effect in a Release build.
9. Configuring `IdleTimeoutMinutes` outside `[15, 30]` clamps to the nearest boundary and logs a warning at startup.
10. `Application_Exit` stops the monitor cleanly; no `DispatcherTimer` callbacks fire after host disposal.
11. The warning dialog is `Topmost`, centred over `MainWindow`, non-modal (the underlying app remains visible and responsive — though irrelevant since the user has been idle).

## Operator Checklist Update

Add a new test to `Operator/INFRA-verification-checklist.md` under the existing INFRA-15 deferred section, **replacing** the current `⏳ Deferred` placeholder:

> ## INFRA-19 — Session Inactivity Timeout
>
> **Setup:** Set `Session:IdleTimeoutMinutes = 1` in `appsettings.json` for testing convenience (revert before final acceptance).
>
> 1. Log in as `manager`. Do not touch the keyboard or mouse.
> 2. **Expected:** After ~0 minutes (with `WarningLeadSeconds = 60` clamped against the 1-minute timeout = warning fires immediately), the countdown dialog appears.
> 3. Click **Stay signed in** → dialog closes, you remain logged in.
> 4. Stop touching the input. Wait through the full countdown.
> 5. **Expected:** App returns to `LoginView`. Logging back in works normally.
> 6. Revert `Session:IdleTimeoutMinutes` to 20 before signing off.

(Adjust the prompt copy on review — the goal is a single test that exercises both the warning and the auto-logout paths.)

## Output Requirements

### Implementation Summary

Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-19-summary.md` using `Progress/_template.md`. Document:
- The exact `InputManager.PreProcessInput` filter chosen (which event types count as "activity")
- Whether any reserved-keyword traps were hit during implementation
- The clamp behaviour observed when `IdleTimeoutMinutes` is configured outside `[15, 30]`
- Whether `Application_Exit` cleanly stops the monitor without warning-callback exceptions

### Documentation

- XML doc comments on `IIdleMonitor` (interface), `IdleMonitorOptions`, and `WpfIdleMonitor` — citing DA2 and the 15–30 min range
- Inline comment in `Application.xaml.vb` near `_idleMonitor.Start()` describing why monitoring only starts after a successful login (no clock during the LoginView lifetime)

## Backlog Cleanup

On completion of INFRA-19, remove item **#6 Session Inactivity Timeout (DA2 Partial)** from `Plans/Future/deferred-features-backlog.md` and update the `last-synced` date. Also update `Operator/INFRA-verification-checklist.md` to replace the `⏳ Deferred — requires follow-up plan` placeholder with the new operator test described above.
