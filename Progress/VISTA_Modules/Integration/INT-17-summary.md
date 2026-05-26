---
module: MerchSys.Integration
agent: claude-code
date: 2026-05-26
plan-ref: Plans/VISTA_Modules/Integration/17-parameter-shadow-renames.md
status: completed
---

# INT-17: Rename parameters that shadow properties (Rule 14 true positives)

## Task Summary

Mechanical rename of 12 constructor/method parameters that shadow instance properties
(Rule 14 true positives). All confirmed sites were already mitigated with `Me.` qualifiers,
so this eliminates the latent footgun without changing any behaviour.

**Plan:** `[[17-parameter-shadow-renames]]`

## What Was Done

- Modified `MerchSys.Accounting/Exceptions/VatReturnLockedException.vb:26`
  — renamed `returnId`→`lockedReturnId`, `year`→`lockedYear`, `period`→`lockedPeriod`,
    `formType`→`lockedFormType` in constructor signature and body (including `MyBase.New` message).
- Modified `MerchSys.App/Models/NavigationItem.vb:28` (in `NavigationGroup.New`)
  — renamed `groupName`→`name`, `items`→`navigationItems`.
- Modified `MerchSys.App/Views/LoginView.xaml.vb:16`
  — renamed `viewModel`→`vm` in constructor signature, `_viewModel = vm`, `DataContext = vm`.
- Modified `MerchSys.Inventory/Services/StockService.vb:21` (in `InsufficientStockException.New`)
  — renamed `productId`→`product`, `requestedQty`→`requested`, `availableQty`→`available`.
- Modified `MerchSys.POS/Debug/ReceiptArchivalHarness.vb:750` (in `EmptyConfigurationSection.New`)
  — renamed `key`→`sectionKey`.
- Modified `MerchSys.POS/Debug/ReceiptArchivalHarness.vb:782` (in `EmptyConfigurationSection.GetSection`)
  — renamed `key`→`sectionKey`.

### Conditional row — `VatReturnViewModel.PopulateFromReturn`

Grepped for `Public Property VatReturn` in `VatReturnViewModel.vb` — **no match**.
`VatReturnViewModel` has no `VatReturn` property of its own; the parameter
`vatReturn As VatReturn` in `PopulateFromReturn` does not shadow any instance state.
**Decision: confirmed false positive. No rename applied.**

### Throw-site and caller checks

- `Throw New VatReturnLockedException(...)` — one site in `VatReportingService.vb:390`,
  positional args only. No caller change needed.
- `Throw New InsufficientStockException(...)` — three sites (`StockService.vb:116`,
  `ShrinkageService.vb:64`, `ShrinkageService.vb:124`), all positional. No caller change needed.
- `New NavigationGroup(...)` — all call sites in `MainWindowViewModel.vb` use positional args.
  No caller change needed.
- `LoginView` construction — no named-argument call site found. DI wires it positionally.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A |
| Manual verification | N/A |

## Rule 14 hit counts (affected files)

| File | Before (shadowing params) | After |
|---|---|---|
| `VatReturnLockedException.vb` | 4 | 0 |
| `NavigationItem.vb` | 2 | 0 |
| `LoginView.xaml.vb` | 1 | 0 |
| `StockService.vb` (`InsufficientStockException`) | 3 | 0 |
| `ReceiptArchivalHarness.vb` (`EmptyConfigurationSection`) | 2 | 0 |
| `VatReturnViewModel.vb` | 0 (false positive, not renamed) | 0 |
| **Total** | **12** | **0** |

## Issues Encountered

- `EmptyConfigurationSection.GetSection` required extra context in the Edit call because
  `EmptyConfiguration` (the parent class) also has an identically-named method. Disambiguated
  by including the surrounding `Value` property and `End Class` / `End Namespace` block.

## Note for CLAUDE.md reviewers

This commit eliminates the seven confirmed true-positive shadowing sites found in the
2026-05-24 audit cycle. The antipattern itself (Rule 14: "Parameter shadows property") is
unchanged and remains active for future code. If INFRA-18's re-run surfaces additional
true positives in new code, open a follow-up rename task rather than expanding INT-17.

## What's Next

- [ ] Re-run INFRA-18 Rule 14 detector on affected files to confirm zero hits.
- [ ] If new true positives surface in code merged after 2026-05-24, open INT-17b.

## Cross-References

- Agent Wiki: `[[vbnet-parameter-shadows-property]]`
- Plan: `[[17-parameter-shadow-renames]]`
