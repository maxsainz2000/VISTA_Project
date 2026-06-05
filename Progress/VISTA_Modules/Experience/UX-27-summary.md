# Implementation Progress Report - UX-27

---
module: MerchSys.App
agent: antigravity
date: 2026-06-05
plan-ref: Plans/VISTA_Modules/Experience/27-shortcut-discoverability-overlay.md
status: completed
---

## Task Summary

Implemented the Keyboard-Shortcut Discoverability Overlay (`UX-27`) cheat sheet for `MerchSys.App`. The component lists all active global shortcut keys, dialog/form hotkeys, and cheatsheet commands. It is opened by pressing `?` or `Ctrl+/` and closed by pressing `Esc` or clicking the overlay backdrop. It honors user roles by conditionally displaying developer-only shortcuts, coordinates with the command palette overlay to ensure mutual exclusion, and guards the bare `?` gesture to prevent text entry hijacking in input controls.

**Plan:** `[[27-shortcut-discoverability-overlay.md]]`

## What Was Done

- **ShortcutsOverlay View & VM**:
  - Created `ShortcutsOverlay.xaml` and `ShortcutsOverlay.xaml.vb` under `Views/Shell/`. It maps a modal-style layout reusing the command palette design (dimmed backdrop, centered panel with large corner radius, and drop shadow).
  - Created `ShortcutsOverlayViewModel.vb` under `ViewModels/Shell/`. It manages `IsOpen` and exposes `IsDeveloper` checking if the active role is `Developer`.
  - Registered both components as Singletons in the DI container in `Application.xaml.vb`.
- **Keyboard Hooking & Guards**:
  - Extended `MainWindow_PreviewKeyDown` in `MainWindow.xaml.vb` to catch `Escape` to close the shortcuts overlay if visible.
  - Implemented the `?` guard: if the user types a bare `?` (Shift+OemQuestion) while focus is inside a `TextBoxBase` or `PasswordBox`, the event is bypassed to allow text input. If pressed outside a text field, it toggles the shortcuts overlay.
  - Wired `Ctrl+/` (Ctrl+OemQuestion) to toggle the shortcuts overlay unconditionally.
- **Mutual Exclusion Overlay Sync**:
  - Wired property-change listeners in `MainWindowViewModel.vb` such that opening the shortcuts overlay automatically closes the command palette, and opening the command palette automatically closes the shortcuts overlay.
  - Updated `CanNavigate` and `CanSelectModule` inside `MainWindowViewModel.vb` to block commands whenever either overlay is open.
- **Cheatsheet Content Layout**:
  - Grouped shortcut descriptions into Global Navigation (Ctrl+K, Ctrl+1..4, and developer-only Ctrl+D0) and Dialogs/Forms (Enter to confirm, Esc to cancel/close, Alt+Letter mnemonics, and overlay controls).
  - Styled hotkey labels as round theme-resilient chips using dynamic border and text secondary color tokens.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Completed successfully with 0 errors and 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified focus stashing/restoration, ? focus guard, role-based gating of Ctrl+D0, and mutual exclusion with command palette) |

## Cross-References

- Domain Wiki pages consulted: `[[wpf-vista-state-feedback]]`, `[[wpf-vista-keyboard-focus]]`
- Agent Wiki entries: `[[wpf-vista-overlay-pattern]]`
