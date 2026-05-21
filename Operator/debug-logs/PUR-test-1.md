---
test-id: PUR-Test-1
checklist: PUR-verification-checklist.md
branch: debug/PUR-test-1
started: 2026-05-20T00:00
status: resolved
---

# Debug Session — PUR Test 1

## Problem Statement

Clicking the **Purchase Orders** tab crashes the app immediately with:

```
System.InvalidOperationException
Message=Default value '0' of type 'int' cannot be set on property 'VatClassification'
        of type 'MerchSys.SharedKernel.Enums.VatTreatment' in entity type 'GoodsReceiptLine'.
Source=Microsoft.EntityFrameworkCore.Relational
at MerchSys.Purchasing.Data.Configurations.GoodsReceiptLineConfiguration.Configure(...)
   in GoodsReceiptLineConfiguration.vb:line 18
```

Test being run: PUR-14 + PUR-15 Test 1 — Single-classification receipt creates VAT entries.

## Starting State
- **Commit:** `b885cb8`
- **Build status:** clean (0 errors, 0 warnings)
- **Relevant files:**
  - `WPF_Applications/MerchSys/src/MerchSys.Purchasing/Data/Configurations/GoodsReceiptLineConfiguration.vb`

## Allowed Files
- `WPF_Applications/MerchSys/src/MerchSys.Purchasing/Data/Configurations/GoodsReceiptLineConfiguration.vb` — the configuration file named in the stack trace

### Off-limits (do NOT touch)
- `MerchSys.SharedKernel/` — shared contracts, not the bug source
- Other modules' services/handlers

---

## Attempt Log

### Attempt 1
- **Hypothesis:** `builder.Property(...VatClassification).HasDefaultValue(0)` passes a literal `int` (0)
  but EF Core's `ConvertDefaultValue` requires the value to match the CLR type of the property
  (`VatTreatment`). Fix: replace `0` with `VatTreatment.Vatable` and add the missing `Imports`.
- **Changed:** `GoodsReceiptLineConfiguration.vb` line 18 — `HasDefaultValue(0)` → `HasDefaultValue(VatTreatment.Vatable)`; added `Imports MerchSys.SharedKernel.Enums`
- **Build result:** ✅ clean — 0 errors, 0 warnings
- **Runtime result:** app launches; Purchase Orders tab opens without crash
- **Verdict:** ✅ fixed
- **Action:** committed as `ea34244`

---

## Resolution

- **Status:** resolved
- **Root cause:** `GoodsReceiptLineConfiguration` called `HasDefaultValue(0)` (an `int` literal) on a
  property typed as `VatTreatment` (an enum). EF Core's `ConvertDefaultValue` validates that the
  default value's CLR type matches the property type and throws `InvalidOperationException` when they
  differ. This check runs when the `DbContext` model is being built — which happens on first access
  of the Purchasing DbContext, i.e. when the Purchase Orders tab loads.
- **Fix description:** Added `Imports MerchSys.SharedKernel.Enums` to the configuration file and
  changed `HasDefaultValue(0)` to `HasDefaultValue(VatTreatment.Vatable)`.
- **Final commit:** `ea34244`
- **Agent wiki entry needed?** yes — `efcore-hasdefaultvalue-enum-type-mismatch`
