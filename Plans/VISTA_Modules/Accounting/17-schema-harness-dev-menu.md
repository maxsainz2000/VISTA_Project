---
module: MerchSys.Accounting
plan-id: ACC-17
title: "Schema Verification Harness Dev-Menu Integration"
depends-on: [ACC-13]
estimated-files: 2
priority: low
---

# Schema Verification Harness Dev-Menu Integration

## Context

ACC-13 delivered `VatLedgerSchemaHarnessRunner.RunAndReportAsync` — a diagnostic harness that validates four VAT schema integrity checks (column presence, index existence, trigger presence, cascade behaviour). The 2026-05-15 Accounting audit notes that the harness is **not yet wired to any UI entry point**, so it cannot be invoked without manual code changes. This plan adds a developer-only menu item in `MerchSys.App` to invoke the harness from the running application.

## Prerequisites

- **ACC-13** (VAT Ledger Schema Verification) — `VatLedgerSchemaHarnessRunner`, `CheckResult` output model

## Deliverables

```
MerchSys.App/Views/Debug/
└── DebugMenuExtensions.vb                      ' Modified or new — add harness menu item

MerchSys.App/Startup/
└── DebugServiceRegistration.vb                 ' Modified — register IDbContextFactory if needed
```

## Specification

### Debug Menu Item

Add a menu item labelled **"Run VAT Schema Harness"** to the existing developer/debug menu (if one exists from INT-12/INT-13 wiring). If no debug menu exists, create a minimal one gated by `#If DEBUG`:

```vb
#If DEBUG Then
Private Async Sub RunVatSchemaHarness_Click(sender As Object, e As RoutedEventArgs)
    Dim runner = _host.Services.GetRequiredService(Of VatLedgerSchemaHarnessRunner)()
    Dim results = Await runner.RunAndReportAsync()
    ' Display results in a MessageBox or toast notification
    Dim summary = String.Join(Environment.NewLine,
        results.Select(Function(r) $"{If(r.Passed, "✅", "❌")} {r.CheckName}: {r.Message}"))
    MessageBox.Show(summary, "VAT Schema Harness Results")
End Sub
#End If
```

### Optional: IDbContextFactory Registration

If `VatLedgerSchemaHarnessRunner` requires `IDbContextFactory(Of AccountingDbContext)` for multi-instance patterns, add:

```vb
services.AddDbContextFactory(Of AccountingDbContext)()
```

to `DatabaseConfig.AddModuleDbContexts`. Only add this if the harness constructor requires it — check ACC-13's implementation at build time.

## Implementation Notes

- The menu item is `#If DEBUG` gated — it will not appear in Release builds.
- This is a low-priority quality-of-life improvement; the harness can alternatively be invoked from the Immediate Window in a Debug session.
- Similar wiring was done for INT-12's `EventChainVerificationHarness` — follow the same pattern.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings in both Debug and Release configurations.
2. In Debug configuration, a "Run VAT Schema Harness" menu item is visible.
3. Clicking the menu item invokes `RunAndReportAsync` and displays results.
4. In Release configuration, the menu item is absent.
5. If `IDbContextFactory` is registered, it does not conflict with existing `AccountingDbContext` registrations.

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-17-summary.md` using `Progress/_template.md`.

### Documentation
- Inline comment on the menu item noting it is for development use only and citing ACC-13.
