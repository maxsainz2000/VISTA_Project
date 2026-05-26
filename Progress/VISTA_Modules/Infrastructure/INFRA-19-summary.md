---
module: Infrastructure
agent: claude-code
date: 2026-05-26
plan-ref: Plans/VISTA_Modules/Infrastructure/19-session-inactivity-timeout.md
status: completed
---

## Task Summary

Implemented OWASP DA2 session inactivity timeout (INFRA-19). Adds idle detection via `InputManager.PreProcessInput`, a 60-second countdown warning dialog, and automatic forced logout on timeout — all routed through the existing `HandleLogoutRequested` flow from INFRA-15.

**Plan:** `[[19-session-inactivity-timeout]]`

## What Was Done

- Created `src/MerchSys.App/Services/IIdleMonitor.vb` — `IdleMonitorOptions`, `IdleMonitorWarningEventArgs`, and `IIdleMonitor` interface
- Created `src/MerchSys.App/Services/WpfIdleMonitor.vb` — singleton `WpfIdleMonitor` (DispatcherTimer + `InputManager.PreProcessInput` hook); `NoOpIdleMonitor` stub in `#If DEBUG` block for `VISTA_DISABLE_IDLE_TIMEOUT=1` bypass
- Created `src/MerchSys.App/ViewModels/SessionTimeoutWarningViewModel.vb` — countdown VM with `StaySignedInCommand`, `SignOutCommand`, `Tick(remaining)`, and `CountdownDisplay` formatted as `"M:SS"`
- Created `src/MerchSys.App/Views/SessionTimeoutWarningView.xaml` — modal `Window`, `Topmost="True"`, `WindowStartupLocation="CenterOwner"`, dark theme matching LoginView; large countdown text + message + two-button layout
- Created `src/MerchSys.App/Views/SessionTimeoutWarningView.xaml.vb` — code-behind with `MarkDecisionMade()` guard; `OnClosing` override treats X-close as Sign Out
- Modified `src/MerchSys.App/Application.xaml.vb` — DI registration of `IdleMonitorOptions`, `IIdleMonitor`, `SessionTimeoutWarningViewModel`, `SessionTimeoutWarningView`; wired `HandleIdleWarning`, `HandleSessionExpired`; idle monitor starts after login, stops before logout; `Application_Exit` stops monitor before host disposal
- Modified `src/MerchSys.App/appsettings.json` — added `Session:IdleTimeoutMinutes` (20) and `Session:WarningLeadSeconds` (60) section

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | N/A — separate testing session |

## Issues Encountered

None. No reserved-keyword traps hit during implementation.

**InputManager filter chosen:** `TypeOf inputArgs Is KeyEventArgs OrElse TypeOf inputArgs Is MouseEventArgs OrElse TypeOf inputArgs Is TouchEventArgs` — this catches all keyboard/pointer input while ignoring synthetic routing events. `MouseEventArgs` covers all mouse subclasses (`MouseButtonEventArgs`, `MouseWheelEventArgs`, etc.) via inheritance.

**Reserved-keyword traps:** The `[Stop]` bracket is required only in the method *definition* in VB.NET. Call sites use `.Stop()` — in a member access context the compiler correctly resolves it without brackets. No variable name collisions: `remaining` (parameter in `Tick`) vs `RemainingSeconds` (property) are distinct names. `currentUtc`/`recordedAt` naming convention used in implementation notes is satisfied by field name `_lastActivityUtc`.

**Clamp behaviour:** Constructor clamps `_options.IdleTimeoutMinutes` to `[15, 30]` via `Math.Max(15, Math.Min(30, value))` and logs a `LogWarning` via `ILogger(Of WpfIdleMonitor)` if the configured value was outside range.

**Application_Exit:** `_idleMonitor.Stop()` is called via explicit null guard (`If _idleMonitor IsNot Nothing Then`) before `_host.StopAsync()`. This ensures no DispatcherTimer callbacks fire against a disposed DI container.

## What's Next

- [ ] Operator verification: set `Session:IdleTimeoutMinutes = 1`, verify warning dialog, Stay, Sign out, auto-logout paths (see INFRA-verification-checklist.md INFRA-19 section)
- [ ] Revert `Session:IdleTimeoutMinutes` to 20 after operator testing

## Cross-References

- Domain Wiki pages consulted: `[[owasp-da-top10]]` (DA2 Broken Authentication & Session Management)
- Plan dependencies: `[[INFRA-15]]` (LoginSessionService, HandleLogoutRequested, MainWindowViewModel.LogoutRequested)
- Backlog item removed: `Plans/Future/deferred-features-backlog.md` item #6
