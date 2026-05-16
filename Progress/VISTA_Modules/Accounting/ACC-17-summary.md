---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-16
plan-ref: Plans/VISTA_Modules/Accounting/17-schema-harness-dev-menu.md
status: completed
---

## Task Summary

Implemented ACC-17: Schema Verification Harness Dev-Menu Integration. Wired the
`VatLedgerSchemaHarnessRunner` (ACC-13) to a developer-only sidebar navigation entry so the
harness can be invoked from the running application without manual code changes.

**Plan:** `[[17-schema-harness-dev-menu]]`

## What Was Done

- Created `src/MerchSys.App/Views/Debug/DebugMenuExtensions.vb` — code-only `UserControl`
  (`DebugMenuView`) that renders the developer debug panel. Contains the `#If DEBUG`-gated
  `RunVatSchemaHarness_Click` handler, which calls
  `VatLedgerSchemaHarnessRunner.RunAndReportAsync(Nothing)` and surfaces a `MessageBox` on
  completion (or on error). BC36943 compliant: `Await` is inside `Try`, not `Catch`.
- Created `src/MerchSys.App/Startup/DebugServiceRegistration.vb` — `#If DEBUG`-gated
  `DebugServiceRegistration` Module with `AddDebugServices` extension method that registers
  `DebugMenuView` as Transient in DI.
- Modified `src/MerchSys.App/Application.xaml.vb` — added `#If DEBUG` block calling
  `services.AddDebugServices()` after the Shell registrations.
- Modified `src/MerchSys.App/ViewModels/MainWindowViewModel.vb` — refactored
  `BuildNavigationGroups` to assign to a local variable before returning, then appends a
  "Developer Tools" `NavigationGroup` with a "Run VAT Schema Harness" `NavigationItem`
  (pointing to `DebugMenuView`) inside a `#If DEBUG` block.

## IDbContextFactory Registration

Not required. `VatLedgerSchemaHarnessRunner.RunAndReportAsync` accepts `IHost` as a parameter
but does not use it at runtime — the harness instantiates its own scratch `VatLedgerSchemaHarness`
with `Nothing`. The `host` parameter is present for signature symmetry only. No
`AddDbContextFactory` addition was needed.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds — Debug (`dotnet build --configuration Debug`) | ✅ 0 errors, 0 warnings* |
| Solution builds — Release (`dotnet build --configuration Release`) | ✅ 0 errors, 0 warnings* |
| Unit tests | N/A |
| Manual verification | ⬜ Pending operator run |

*Pre-existing `BC40000` warning in `MerchSys.POS\Data\Configurations\VatConfigurationMap.vb`
(obsolete `HasCheckConstraint` overload) is not related to this plan.

## Architecture Notes

### Code-only UserControl pattern

`DebugMenuView` has no XAML partner. WPF UserControls can be defined entirely in code by
inheriting `UserControl` and assigning `Content` in the constructor. This avoids any XAML that
would be compiled into Release artifacts.

### `#If DEBUG` guard consistency

All four touched surfaces are guarded consistently:
- `DebugMenuExtensions.vb` — entire file under `#If DEBUG Then ... #End If`
- `DebugServiceRegistration.vb` — entire file under `#If DEBUG Then ... #End If`
- `Application.xaml.vb` — only the `AddDebugServices()` call is inside `#If DEBUG`
- `MainWindowViewModel.vb` — only the `groups.Add(...)` call is inside `#If DEBUG`

In Release, `GetType(Views.Debug.DebugMenuView)` is never compiled because the `#If DEBUG`
block around it is stripped before the compiler sees it.

### IHost not in DI container

`IHost` is not registered in the DI container by `Host.CreateDefaultBuilder`. The runner's
`host` parameter is unused at runtime (per the source comment in
`VatLedgerSchemaHarnessRunner.vb`), so `Nothing` is passed from the click handler.
If a future runner variant does need the host, the pattern is to capture `_host` via lambda
closure in `Application.xaml.vb` (the field is set before any view is resolved).

## Issues Encountered

None. Both Debug and Release builds passed on the first attempt.

## What's Next

- [ ] Operator: run Debug build, navigate to "Developer Tools → Run VAT Schema Harness",
      click button, confirm `MessageBox` appears and a `.md` report appears in `%TEMP%`.

## Cross-References

- Domain Wiki consulted: `[[bir-compliance]]`, `[[modular-monolith]]`
- Codebase Wiki consulted: `[[modules/accounting/debug]]`, `[[modules/app/ui]]`,
  `[[modules/app/services]]`
- Prior harness: `[[ACC-13]]` (`VatLedgerSchemaHarnessRunner`, `VatLedgerSchemaHarness`)
- Navigation pattern: `[[INT-13]]` (type-based `NavigationItem`, `NavigationGroup`)
