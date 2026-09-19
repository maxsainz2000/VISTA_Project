---
module: MerchSys.POS
agent: claude-code
date: 2026-05-11
plan-ref: Plans/VISTA_Modules/POS/15-receipt-numbering-integration.md
status: completed
---

## Task Summary

Closes three causally linked gaps identified in the 2026-05-11 POS audit:

1. `ReceiptService.GenerateReceiptAsync` was using a naïve `CountAsync` pattern to produce receipt numbers — not safe under concurrent transactions and superseded by POS-13.
2. `IReceiptIntegrityService` was registered in the composition root (`Application.xaml.vb`) rather than a module-local extension, inconsistent with the `AddPurchasingServices()` pattern.
3. `Pos_SequenceConcurrencyHarness` existed but had no runner that produces a verifiable Markdown report.

**Plan:** `[[15-receipt-numbering-integration]]`

## What Was Done

- Modified `WPF_Applications/MerchSys/src/MerchSys.POS/Services/ReceiptService.vb` — injected `IReceiptIntegrityService`, replaced `CountAsync`-based `GenerateReceiptNumberAsync` helper with `_receiptIntegrity.GetNextReceiptNumberAsync(DateTime.Now.Year)`, deleted the helper method entirely, added XML doc comment.
- Confirmed `WPF_Applications/MerchSys/src/MerchSys.POS/Services/VatAwareReceiptService.vb` — decorator pattern intact; delegates to `_inner.GenerateReceiptAsync()` with no numbering logic of its own. No changes required.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Startup/PosServiceRegistration.vb` — new `AddPosModule()` extension method consolidating all POS service and ViewModel registrations.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb` — replaced 14 individual POS `AddScoped`/`AddSingleton`/`AddTransient` lines with a single `services.AddPosModule()` call; removed now-unused `Imports MerchSys.POS.Services` and `Imports MerchSys.POS.ViewModels`.
- Created `WPF_Applications/MerchSys/src/MerchSys.POS/Debug/ReceiptSequenceHarnessReport.vb` — debug-only (`#If DEBUG`) harness runner; uses per-worker `POSDbContext` against a scratch SQLite file, writes Markdown report to `%TEMP%`, cleans up scratch DB.

## `ReceiptService.GenerateReceiptAsync` — exact diff

```diff
-        Public Sub New(context As POSDbContext, configuration As IConfiguration)
+        Public Sub New(context As POSDbContext, configuration As IConfiguration, receiptIntegrity As IReceiptIntegrityService)
             _context = context
+            _receiptIntegrity = receiptIntegrity
             ...

+        ''' <summary>
+        ''' Receipt numbering is delegated to IReceiptIntegrityService.GetNextReceiptNumberAsync
+        ''' (POS-13) — row-locked, serializable-isolation, gap-free, concurrency-safe.
+        ''' </summary>
         Public Async Function GenerateReceiptAsync(...) ...
-            Dim receiptNumber = Await GenerateReceiptNumberAsync()
+            Dim receiptNumber = Await _receiptIntegrity.GetNextReceiptNumberAsync(DateTime.Now.Year)

-        Private Async Function GenerateReceiptNumberAsync() As Task(Of String)
-            Dim year = DateTime.Now.Year
-            Dim count = Await _context.OfficialReceipts.
-                CountAsync(Function(r) r.IssueDate.Year = year) + 1
-            Return $"OR-{year}-{count:D4}"
-        End Function
```

## `IReceiptIntegrityService` registration — pre/post state

**Before (Application.xaml.vb composition root):**
```vb
services.AddScoped(Of IReceiptIntegrityService, ReceiptIntegrityService)()
services.AddScoped(Of ReceiptService)()
services.AddScoped(Of IReceiptService, VatAwareReceiptService)()
' ... 11 more POS lines ...
```

**After (Application.xaml.vb composition root):**
```vb
services.AddPosModule()
```

**After (PosServiceRegistration.vb — module-local):**
```vb
services.AddScoped(Of IReceiptIntegrityService, ReceiptIntegrityService)()
services.AddScoped(Of ReceiptService)()
services.AddScoped(Of IReceiptService, VatAwareReceiptService)()
services.AddScoped(Of IVatCalculator, VatCalculator)()
services.AddSingleton(Of VatConfigurationLoader)()
' ... remaining POS services + ViewModels
```

## Sample Harness Report (expected output under default parameters)

```markdown
# Receipt Sequence Concurrency Harness Report

**Date:** 2026-05-11 HH:mm:ss
**Result:** PASS ✓

## Parameters

| Parameter | Value |
|---|---|
| Concurrency | 16 |
| IterationsPerWorker | 50 |
| Year | 2026 |

## Results

| Metric | Value | Expected |
|---|---|---|
| TotalReservations | 800 | 800 |
| Duplicates | 0 | 0 |
| Gaps | 0 | 0 |
| ElapsedMs | ~<elapsed> | — |
```

*Harness not executed against live scratch DB in this session (see Implementation Notes below).*

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings (1 pre-existing BC40000 on VatConfigurationMap.vb — not introduced by this plan) |
| `git grep -n "CountAsync" ReceiptService.vb` | ✅ 0 matches |
| `git grep -n "MaxAsync" ReceiptService.vb` | ✅ 0 matches |
| `ReceiptService` constructor accepts `IReceiptIntegrityService` | ✅ |
| `VatAwareReceiptService` delegates to inner | ✅ confirmed — no changes required |
| `IReceiptIntegrityService` in `PosServiceRegistration.vb` | ✅ |
| `Application.xaml.vb` calls `AddPosModule()` | ✅ |
| `ReceiptSequenceHarnessReport` gated by `#If DEBUG` | ✅ |
| No edits to POS-13 source files | ✅ |

## Issues Encountered

- **Issue:** `MerchSys.POS` classlib does not reference `Microsoft.Extensions.Hosting`, so `IHost` in the harness signature caused `BC30002`.
  - **Resolution:** Removed the `host As IHost` parameter from `RunAndReportAsync`. The harness builds its own scratch `DbContextOptionsBuilder` and does not need the production host. The plan's spec included `IHost` for potential future factory resolution; since it was unused and caused a build error, removing it is strictly correct. Signature is now `RunAndReportAsync(Optional concurrency, Optional iterationsPerWorker)`.

- **Audit discrepancy — `MaxAsync` vs `CountAsync`:** The 2026-05-11 audit described the vulnerability as a `Max(ReceiptNumber) + 1` pattern. Actual code used `CountAsync(...) + 1` — same concurrency vulnerability (not gap-safe under concurrent transactions), different EF query. The fix is identical in both cases. Recorded here for future audit accuracy.

## What's Next

- [x] Execute `ReceiptSequenceHarnessReport.RunAndReportAsync()` against a scratch DB in a debug session to confirm the four metrics (Duplicates=0, Gaps=0, TotalReservations=800, elapsed) in a live environment. *(completed/verified in Operator checklist)*
- [x] POS-16: Receipt Archival Service *(completed in POS-16)*
- [x] POS-17: VAT Settings UI *(completed in POS-17)*
- [x] POS-18: Receipt Body VAT Buckets *(completed in POS-18)*

## Cross-References

- Domain Wiki: `[[bir-compliance]]`, `[[utang-credit-system]]`
- Codebase Wiki: `[[pos/services]]`, `[[schemas/di-registry]]`
- Plan dependencies confirmed: POS-06, POS-13, POS-14
