---
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
plan-ref: Plans/VISTA_Modules/Inventory/12-view-expiry-monitor.md
status: completed
---

## Task Summary

Implemented the Expiry Monitor screen (INV-12) — a dedicated view for monitoring batch expiry dates, with near-expiry alerts, expired batch listing, and one-click write-off to shrinkage.

**Plan:** `[[12-view-expiry-monitor]]`

## What Was Done

- Created `MerchSys.Inventory/ViewModels/ExpiryMonitorViewModel.vb` — ViewModel with two ObservableCollections (`NearExpiryBatches`, `ExpiredBatches`), three summary properties (`NearExpiryCount`, `ExpiredCount`, `TotalValueAtRisk`), a configurable `DaysThreshold` (default 30, clamped 1–365), `WriteOffCommand` (AsyncRelayCommand(Of ExpiryRowItem)), `RefreshCommand`, status feedback properties (`StatusMessage`, `IsStatusError`), and 60-second auto-refresh timer using the `classlib-viewmodel-auto-refresh-timer` pattern.
- Created `MerchSys.Inventory/ViewModels/ExpiryRowItem.vb` (nested in the ViewModel file) — flat bindable row with `UrgencyLevel` ("Red" ≤7 days, "Orange" 8–14 days, "Yellow" 15–30 days) driving DataGrid row coloring.
- Created `MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml` — three summary cards (NearExpiryCount, ExpiredCount, TotalValueAtRisk), threshold spinner (TextBox + RepeatButtons), TabControl with Near-Expiry and Expired tabs; row styles keyed to `UrgencyLevel`; Expired tab includes a per-row "Write Off" button in a DataGridTemplateColumn.
- Created `MerchSys.App/Views/Inventory/ExpiryMonitorView.xaml.vb` — constructor-injected ViewModel, `WriteOffButton_Click` handler (shows MessageBox confirmation before invoking `WriteOffCommand`), threshold spinner handlers, and numeric-only input filter for the threshold TextBox.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A — deferred to testing phase |

## Issues Encountered

None. The established `classlib-viewmodel-auto-refresh-timer` pattern and avoiding `.Count()` LINQ call (using a `For Each` accumulator loop instead for `TotalValueAtRisk`) prevented known build pitfalls.

## What's Next

- Wire `ExpiryMonitorView` and `ExpiryMonitorViewModel` into the DI container and navigation shell (deferred to integration/shell plan).
- INV-13 or subsequent Inventory views.

## Cross-References

- Domain Wiki pages consulted: none required beyond plan spec
- Agent Wiki entries consulted: `[[classlib-viewmodel-auto-refresh-timer]]`, `[[vbnet-list-count-property-shadows-linq-extension]]`
