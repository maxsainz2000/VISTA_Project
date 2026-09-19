---
test-id: ACC-Test-7
checklist: ACC-verification-checklist.md
branch: debug/ACC-test-7
started: 2026-05-22T00:00
status: resolved
---

# Debug Session — ACC Test 7

## Problem Statement

Clicking the VAT Payable tile on the Financial Overview screen does nothing for the Manager role.
Expected: navigates to VatReturnView.

Test from ACC-verification-checklist.md, ACC-14 + ACC-16 combined, Test 7:
> Click the VAT Payable tile → should navigate to VAT Return View screen.

## Starting State
- **Commit:** `22723d0`
- **Build status:** clean (assumed — prior session left at 0 errors)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/Components/VatPayableTile.xaml`
  - `WPF_Applications/MerchSys/src/MerchSys.App/ViewModels/MainWindowViewModel.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb`
  - `WPF_Applications/MerchSys/src/MerchSys.Accounting/ViewModels/Extensions/FinancialOverviewVatExtension.vb`

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.App/Views/Accounting/FinancialOverviewView.xaml.vb` — navigation handler lives here; within Accounting view scope

### Off-limits (do NOT touch)
- `SharedKernel/Events/*` — shared contracts
- Other modules' services/handlers
- `Application.xaml.vb` — app infrastructure, outside Accounting scope

---

## Attempt Log

### Attempt 1
- **Hypothesis:** `Application.Current.MainWindow` is the `LoginView` (the first window shown in `Application_Startup` → `ShowLoginView()`), not the shell `MainWindow`. So `TryCast(Application.Current.MainWindow?.DataContext, MainWindowViewModel)` returns `Nothing` and the handler exits early — navigation never fires. Fix: iterate `Application.Current.Windows` to find the window whose DataContext is `MainWindowViewModel`.
- **Changed:** `FinancialOverviewView.xaml.vb` — replaced `Application.Current.MainWindow` lookup with a `For Each` over `Application.Current.Windows`
- **Build result:** clean — 0 errors, 0 warnings
- **Runtime result:** clicking VAT tile navigates to VatReturnView ✅
- **Verdict:** ✅ fixed
- **Action:** committed as `34bbf93`

---

## Resolution

- **Status:** resolved
- **Root cause:** `Application.Current.MainWindow` is set to `LoginView` (first window shown in `Application_Startup`), not the shell `MainWindow`. `TryCast` to `MainWindowViewModel` returned `Nothing` and the handler returned early — no navigation fired.
- **Fix description:** Replaced `Application.Current.MainWindow` lookup with a `For Each` over `Application.Current.Windows`, finding the window whose `DataContext` is `MainWindowViewModel`.
- **Final commit:** `34bbf93`
- **Agent wiki entry needed?** yes — `wpf-mainwindow-not-shell-window`
