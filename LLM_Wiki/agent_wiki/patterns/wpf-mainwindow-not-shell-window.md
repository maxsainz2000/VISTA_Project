---
name: wpf-mainwindow-not-shell-window
description: Application.Current.MainWindow is the first window shown (LoginView), not the shell — iterating Application.Current.Windows is the reliable way to find a specific window by ViewModel type
metadata:
  type: pattern
  module: MerchSys.App
  tags: [wpf, navigation, login, mainwindow, runtime-bug]
  agent: claude-code
  date: 2026-05-22
---

## Pattern: Finding the Shell Window After Login

### Problem

`Application.Current.MainWindow` is automatically set by WPF to the **first window shown**, not necessarily the shell/navigation window. In VISTA, `LoginView` is shown first (`ShowLoginView()` in `Application_Startup`), so `Application.Current.MainWindow` points to `LoginView` for the entire session — even after login succeeds and the shell `MainWindow` is visible.

Any code that does:

```vb
Dim mainWindow = TryCast(Application.Current.MainWindow?.DataContext, MainWindowViewModel)
If mainWindow Is Nothing Then Return   ' ← always fires, navigation silently suppressed
```

will silently fail because `LoginView.DataContext` is `LoginViewModel`, not `MainWindowViewModel`.

### Symptom

Navigation triggered from within a view (e.g. clicking the VAT Payable tile on `FinancialOverviewView`) does nothing — no error, no exception, just silence.

### Fix

Iterate `Application.Current.Windows` to find the window whose `DataContext` is the target ViewModel type:

```vb
Dim mainVm As MainWindowViewModel = Nothing
For Each win As Window In Application.Current.Windows
    Dim vm = TryCast(win.DataContext, MainWindowViewModel)
    If vm IsNot Nothing Then
        mainVm = vm
        Exit For
    End If
Next
If mainVm Is Nothing Then Return
```

### Where It Was Applied

- `MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb` — `OnNavigateToVatReturnRequested` handler (ACC-test-7, 2026-05-22)

### Prevention

Avoid `Application.Current.MainWindow` anywhere that assumes it's the shell. The reliable alternatives are:
1. Iterate `Application.Current.Windows` filtered by DataContext type (above).
2. Inject the target ViewModel directly into the view constructor via DI (`MainWindowViewModel` is a singleton).
3. Explicitly set `Application.Current.MainWindow = _mainWindow` in `HandleLoginSucceeded` after the shell is shown.

## Detector Contract

> Added 2026-05-24 after the agent-wiki audit (`Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md`)
> flagged the comment on line 33 of `FinancialOverviewView.xaml.vb` — a comment in code that
> deliberately *avoids* the antipattern by iterating `Application.Current.Windows`. See
> `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md`.

Any audit that detects this rule MUST strip comments before pattern-matching.

### Flag (positive patterns)

A `Application.Current.MainWindow` reference that:

- Appears in executable code (not inside a `'...` comment or a `"..."` string literal).
- Reads `.MainWindow` to dereference it (`Application.Current.MainWindow.DataContext`,
  `TryCast(Application.Current.MainWindow, ...)`, etc.).

### Do NOT flag (negative patterns)

- Text inside a single-line VB.NET comment (`'`).
- Text inside an XML doc-comment (`'''`).
- Text inside a string literal (`"..."`) or interpolated string (`$"..."`).
- An *assignment* `Application.Current.MainWindow = X` — this is the recommended remediation
  pattern (Prevention option 3 above), not the antipattern.

The corpus that exercises these patterns lives at `Operator/audit-tests/rule-19/` (see INFRA-18).
