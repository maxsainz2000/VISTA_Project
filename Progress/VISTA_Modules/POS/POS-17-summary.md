---
module: MerchSys.POS
agent: claude-code
date: 2026-05-11
plan-ref: Plans/VISTA_Modules/POS/17-vat-settings-ui.md
status: completed
---

## Task Summary

Implements the VAT Settings UI (POS-17): a dedicated settings view, view-model, write-side service, and shell navigation entry that allows a Manager to edit `Pos_VatConfiguration` via the UI rather than via ad-hoc SQL. After every successful save the `VatConfigurationLoader` singleton cache is invalidated so `VatAwareReceiptService` and the Accounting KPI providers pick up the new values on the next operation without an app restart.

**Plan:** `[[17-vat-settings-ui]]`

## What Was Done

- Created `src/MerchSys.SharedKernel/Events/VatConfigurationChangedEvent.vb` — new MediatR event with `OccurredAt`, `IsVatRegistered`, `PreviousIsVatRegistered`
- Created `src/MerchSys.POS/Services/IVatConfigurationWriter.vb` — interface `IVatConfigurationWriter`, DTOs `VatConfigurationUpdateRequest`/`VatConfigurationUpdateResult`, and implementation `VatConfigurationWriter` (Scoped)
- Created `src/MerchSys.POS/ViewModels/VatSettingsViewModel.vb` — ViewModel with `SaveCommand`, `ReloadCommand`, percent ↔ fraction conversion, `IsNotVatRegistered` inverse property for XAML visibility
- Created `src/MerchSys.App/Views/POS/VatSettingsView.xaml` — two-column settings form: toggle, conditional TIN/rate fields, business info, validation error panel
- Created `src/MerchSys.App/Views/POS/VatSettingsView.xaml.vb` — code-behind; triggers `ReloadAsync` on `UserControl.Loaded`
- Modified `src/MerchSys.SharedKernel/Interfaces/INotificationService.vb` — added `ShowSuccess(message)` and `ShowError(message)` methods so module-library ViewModels can surface toast notifications without a direct WPF reference
- Modified `src/MerchSys.App/Services/DefaultNotificationService.vb` — implemented `ShowSuccess`/`ShowError` using `Notification.Wpf.NotificationManager` (same pattern as `WpfLowStockNotifier`)
- Modified `src/MerchSys.App/Startup/PosServiceRegistration.vb` — registered `IVatConfigurationWriter → VatConfigurationWriter` (Scoped) and `VatSettingsViewModel` (Transient)
- Modified `src/MerchSys.App/Application.xaml.vb` — registered `Views.POS.VatSettingsView` (Transient)
- Modified `src/MerchSys.App/ViewModels/MainWindowViewModel.vb` — extracted `BuildPosNavItems()` helper; appends `VatSettings → VatSettingsView` entry only when `CurrentRole = Manager`

## TIN Regex

```
^\d{3}-\d{3}-\d{3}(-\d{3}|-\d{5})?$
```

**Origin:** BIR Form 2303 (Certificate of Registration). Supports three accepted TIN formats:
- 9-digit (`999-999-999`) — individual taxpayer, no branch code
- 12-digit (`999-999-999-000`) — registered business, 3-digit branch code
- 14-digit (`999-999-999-00000`) — legacy / VAT-registered business, 5-digit branch code

Defined as the constant `TinPattern` in `VatConfigurationWriter` with a BIR-form citation comment.

## VatConfigurationChangedEvent

`VatConfigurationChangedEvent` was **newly added** to `MerchSys.SharedKernel/Events/` (POS-14 did not define it). It is published by `VatConfigurationWriter.UpdateAsync` after `_loader.Invalidate()` is called. No handler is registered by this plan; an Accounting handler can be added later.

## Shell Navigation Key

```vb
New NavigationItem With {.DisplayName = "VAT Settings", .ViewType = GetType(Views.POS.VatSettingsView)}
```

Added inside `BuildPosNavItems()` behind a `_session.CurrentRole = UserRole.Manager` guard — consistent with the INT-02 convention used by `BuildAccountingNavItems()` for `VatReturnView`.

## View ASCII Rendering

### VAT-Registered = True
```
┌──────────────────────────────────────────────────────────────┐
│  VAT Settings — BIR Registration & Tax Rates    [Reload][Save]│
├──────────────────────────────────────────────────────────────┤
│  REGISTRATION STATUS                                          │
│  ☑ Business is VAT-registered with BIR                       │
│    (Applies when annual gross sales exceed ₱3M)              │
├──────────────────────────────────────────────────────────────┤
│  TAX RATES                                                    │
│  TIN (BIR)             [ 123-456-789-000          ]          │
│  VAT Rate (%)          [ 12                       ]          │
├──────────────────────────────────────────────────────────────┤
│  BUSINESS INFORMATION                                         │
│  Registered Business   [ Villon Farm Supply       ]          │
│  Name                                                         │
│  Registered Address    [ Barangay Poblacion       ]          │
│                        [ General Trias, Cavite    ]          │
└──────────────────────────────────────────────────────────────┘
```

### VAT-Registered = False
```
┌──────────────────────────────────────────────────────────────┐
│  VAT Settings — BIR Registration & Tax Rates    [Reload][Save]│
├──────────────────────────────────────────────────────────────┤
│  REGISTRATION STATUS                                          │
│  ☐ Business is VAT-registered with BIR                       │
│    (Applies when annual gross sales exceed ₱3M)              │
├──────────────────────────────────────────────────────────────┤
│  TAX RATES                                                    │
│  Percentage Tax Rate   [ 3                        ]          │
│  (%)                                                          │
├──────────────────────────────────────────────────────────────┤
│  BUSINESS INFORMATION                                         │
│  Registered Business   [ Villon Farm Supply       ]          │
│  Name                                                         │
│  Registered Address    [ Barangay Poblacion       ]          │
│                        [ General Trias, Cavite    ]          │
└──────────────────────────────────────────────────────────────┘
```

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| 0 errors | ✅ |
| Pre-existing warnings | 1 (BC40000 in VatConfigurationMap.vb — pre-existing from POS-14, not introduced by this plan) |
| Unit tests | N/A |
| Manual verification | ✅ Passed (Load, save, reload round-trip, event published and cache invalidated) |

## Issues Encountered

- **Issue:** `For Each err In result.ValidationErrors` caused BC30068 + BC30311 — VB.NET's built-in `Err` global (`ErrObject`) shadows the loop variable `err` (case-insensitive).
  - **Resolution:** Renamed loop variable to `errMsg`.
  - **Agent Wiki entry:** `[[vbnet-err-builtin-shadows-loop-variable]]`

## What's Next

- [x] Manual testing: verify load, save, and reload round-trip via the UI *(completed/verified in Operator checklist)*
- [x] Verify `VatConfigurationLoader.GetAsync()` returns new values after save (acceptance criterion 3) *(completed/verified in Operator checklist)*
- [x] Verify `VatConfigurationChangedEvent` is published with the correct `PreviousIsVatRegistered` (acceptance criterion 11) *(completed/verified in Operator checklist)*
- [x] Verify Manager sees "VAT Settings" in sidebar; Owner does not (acceptance criteria 9–10) *(completed/verified in Operator checklist)*

## Cross-References

- Domain Wiki pages consulted: `[[concepts/bir-compliance.md]]`, `[[concepts/owasp-da-top10.md]]`
- Agent Wiki entries consulted: `[[feedback_vbnet_await_catch.md]]` (Await-in-Catch restriction)
- Agent Wiki entry added: `[[vbnet-err-builtin-shadows-loop-variable]]`

## Codebase Wiki Discrepancies

- `codebase_wiki/modules/app/ui.md` does not yet list `VatSettingsView` (expected — wiki is synced post-implementation per the hybrid commit workflow).
- `codebase_wiki/modules/pos/ui.md` does not yet list `VatSettingsViewModel`.
- `codebase_wiki/modules/pos/services.md` does not yet list `IVatConfigurationWriter` / `VatConfigurationWriter`.
- `codebase_wiki/modules/shared-kernel/events-queries.md` does not yet list `VatConfigurationChangedEvent`.
- `codebase_wiki/schemas/di-registry.md` does not yet list the new registrations.
