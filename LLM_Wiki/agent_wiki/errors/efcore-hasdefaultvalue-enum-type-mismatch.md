---
type: error-fix
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-20
tags: [ef-core, sqlite, vb-net, enum, configuration, runtime-error]
error-code: InvalidOperationException
severity: runtime-error
---

# EF Core HasDefaultValue — enum property rejects int literal

## Problem

App crashes on first access of the `PurchasingDbContext` (e.g. loading the Purchase Orders tab):

```
System.InvalidOperationException
Message=Default value '0' of type 'int' cannot be set on property 'VatClassification'
        of type 'MerchSys.SharedKernel.Enums.VatTreatment' in entity type 'GoodsReceiptLine'.
Source=Microsoft.EntityFrameworkCore.Relational
at GoodsReceiptLineConfiguration.Configure(...) in GoodsReceiptLineConfiguration.vb:line 18
```

## Root Cause

`HasDefaultValue()` calls `ConvertDefaultValue()` internally, which validates that the supplied
value's CLR type **exactly matches** the property's CLR type. Passing `0` (an `Integer`) for a
property typed as a VB.NET enum (`VatTreatment`) fails this check even though enums are backed by
`int` at the IL level. The validation runs at `DbContext` model-build time, which happens on first
use — not at startup — so the build is clean but the crash appears at runtime.

## Fix

Pass the typed enum value instead of the integer literal, and import the enum's namespace.

```vb
' Before (broken)
Imports MerchSys.Purchasing.Entities

builder.Property(Function(l) l.VatClassification).HasDefaultValue(0)

' After (fixed)
Imports MerchSys.Purchasing.Entities
Imports MerchSys.SharedKernel.Enums

builder.Property(Function(l) l.VatClassification).HasDefaultValue(VatTreatment.Vatable)
```

## Prevention

- **Never pass a bare integer literal to `HasDefaultValue` for an enum property.** Always use the
  enum member (e.g. `VatTreatment.Vatable`, `PurchaseOrderStatus.Draft`).
- When adding VAT or status columns to an EF configuration, add the corresponding `Imports` for the
  SharedKernel enum namespace at the top of the configuration file.

## Related

- `[[vbnet-reserved-keyword-enum-member]]` — related enum pitfall (escaping reserved member names)
