---
module: MerchSys.POS
plan-id: POS-14
title: "VAT Configuration, Schema Extension & Calculation"
depends-on: [POS-01, POS-02, POS-06, POS-13, INFRA-07]
estimated-files: 8
---

# VAT Configuration, Schema Extension & Calculation

## Context

The Feature Gap Audit (2026-05-10) flagged that VAT calculation logic, the VAT-registered/non-VAT toggle, and per-bucket VAT columns are absent. POS-06 references VAT mathematically but has no source of truth for the rate, no per-line treatment classification, and no event payload that downstream Accounting can consume. This plan introduces a singleton `VatConfiguration`, partial-class extensions that add the BIR three-bucket columns to `SalesTransaction` / `SalesTransactionLine` (without modifying POS-01 source files), an `IVatCalculator` for rate decomposition, and a `VatAwareReceiptService` that wraps POS-06 and publishes `SaleCompletedWithVatEvent` (INFRA-07). The legacy `SaleCompletedEvent` continues firing during the migration window.

## Prerequisites

- **POS-01** (POS Domain Models) — `SalesTransaction`, `SalesTransactionLine`, `OfficialReceipt`
- **POS-02** (POS Data Access) — `POSDbContext` exists
- **POS-06** (Receipt Generation) — `IReceiptService` and `ReceiptService` exist
- **POS-13** (BIR Retention) — `IReceiptIntegrityService` (used here for hash inputs that include VAT fields)
- **INFRA-07** (VAT Event Payload) — `SaleCompletedWithVatEvent`, `VatTreatment` enum

## Wiki References

- `concepts/vat-ready.md` — three-bucket model, rate, exemption rules
- `concepts/bir-compliance.md` — VAT receipt requirements
- `Sources/POS-Module_AcademicPaper.md` — VAT-ready structure requirement

## Deliverables

```
MerchSys.POS/Entities/
├── VatConfiguration.vb
└── Extensions/
    ├── SalesTransactionVatExtension.vb
    └── SalesTransactionLineVatExtension.vb

MerchSys.POS/Data/Configurations/
└── VatConfigurationMap.vb

MerchSys.POS/Services/
├── IVatCalculator.vb
├── VatCalculator.vb
└── VatAwareReceiptService.vb
```

(The two extension files are `Partial Class` declarations targeting `SalesTransaction` / `SalesTransactionLine` so POS-01 source remains untouched. The migration `AddVatThreeBucketColumns` is part of this plan but lives under `MerchSys.POS/Migrations/`.)

## Specification

### VatConfiguration (`Pos_VatConfiguration`)
Singleton table — exactly one row, enforced by check constraint `Id = 1`.
```
Inherits AuditableEntity

Property Id As Integer                       ' Always 1
Property IsVatRegistered As Boolean
Property VatRate As Decimal                  ' Default 0.12; precision(5,4)
Property EffectiveFrom As DateTime
Property BusinessTIN As String
Property BusinessName As String              ' "Villon Farm Supply"
Property BusinessAddress As String
Property NonVatPercentageTaxRate As Decimal  ' Default 0.03 (Form 2551Q)
```

### SalesTransaction partial-class extension
```
Partial Public Class SalesTransaction
    Property VatableSales As Decimal              ' Sum of line VatableAmount
    Property VatExemptSales As Decimal            ' Sum of line VatExemptAmount
    Property ZeroRatedSales As Decimal            ' Sum of line ZeroRatedAmount
    Property VatRateSnapshot As Decimal           ' Rate at time of sale
    Property IsVatRegisteredSnapshot As Boolean   ' Mode at time of sale
End Class
```

(`VatAmount` already exists on POS-01's `SalesTransaction` — reuse it as the OutputVAT total.)

### SalesTransactionLine partial-class extension
```
Partial Public Class SalesTransactionLine
    Property Treatment As VatTreatment            ' From SharedKernel
    Property VatableAmount As Decimal
    Property VatExemptAmount As Decimal
    Property ZeroRatedAmount As Decimal
    Property OutputVat As Decimal                 ' VATable × Rate
End Class
```

### EF migration `AddVatThreeBucketColumns`
- Adds the seven new columns to `Pos_SalesTransactions`
- Adds the five new columns to `Pos_SalesTransactionLines`
- Creates `Pos_VatConfiguration` with seed row (`Id=1, IsVatRegistered=False, VatRate=0.12, NonVatPercentageTaxRate=0.03, BusinessTIN='', EffectiveFrom=now`)
- Backfill: existing rows get `Treatment = Vatable`, `VatableAmount = LineTotal`, all other VAT fields `0`, `IsVatRegisteredSnapshot = False`. Non-destructive.

### IVatCalculator
```
Public Interface IVatCalculator
    Function Decompose(grossAmount As Decimal, treatment As VatTreatment, config As VatConfiguration) As VatBreakdown
    Function CalculateLine(line As SalesTransactionLine, config As VatConfiguration) As VatBreakdown
    Function AggregateTransaction(transaction As SalesTransaction, config As VatConfiguration) As TransactionVatTotals
End Interface

Public Class VatBreakdown
    Public Property VatableAmount As Decimal
    Public Property VatExemptAmount As Decimal
    Public Property ZeroRatedAmount As Decimal
    Public Property OutputVat As Decimal
End Class

Public Class TransactionVatTotals
    Public Property VatableSales As Decimal
    Public Property VatExemptSales As Decimal
    Public Property ZeroRatedSales As Decimal
    Public Property OutputVat As Decimal
End Class
```

#### Decomposition rules
- Rate `r = config.VatRate` when `IsVatRegistered = True`, else `0`
- Prices on file are **VAT-inclusive** (Philippines retail convention)
- For `Vatable`:
  - `VatableAmount = Round(Gross / (1 + r), 2, MidpointRounding.ToEven)` (banker's rounding)
  - `OutputVat = Gross - VatableAmount`
  - `VatExemptAmount = 0`, `ZeroRatedAmount = 0`
- For `Exempt`:
  - `VatExemptAmount = Gross`; others `0`
- For `ZeroRated`:
  - `ZeroRatedAmount = Gross`; `OutputVat = 0`; others `0`
- When `IsVatRegistered = False`: every line is treated as `Exempt` regardless of `Treatment` field; OutputVat is always `0`. The `NonVatPercentageTaxRate` is **not** computed at line level — that is reported aggregately by ACC-11.

#### Rounding
- Decompose at **line level**, then sum to transaction totals. Do not decompose the transaction gross.
- Banker's rounding (`MidpointRounding.ToEven`) on every `Round` call.

### VatAwareReceiptService
Decorator over `IReceiptService` from POS-06. Constructor takes `IReceiptService innerService`, `IVatCalculator`, `IDbContextFactory(Of POSDbContext)`, `IMediator`, `IReceiptIntegrityService`.

`GenerateReceiptAsync(transactionId)`:
1. Load `SalesTransaction` with lines and `VatConfiguration`
2. For each line: `CalculateLine` → write `Treatment`, `VatableAmount`, `VatExemptAmount`, `ZeroRatedAmount`, `OutputVat`
3. `AggregateTransaction` → write transaction VAT totals + `VatRateSnapshot` + `IsVatRegisteredSnapshot`
4. Persist
5. Delegate to `innerService.GenerateReceiptAsync` to produce the `OfficialReceipt`
6. Compute `IReceiptIntegrityService.ComputeAndPersistAsync(receipt)` — receipt hash now includes the populated VAT totals
7. Publish `SaleCompletedWithVatEvent` (INFRA-07) **and** `SaleCompletedEvent` (legacy) — handlers must remain idempotent on `TransactionId`

`PrintReceiptAsync` is delegated unchanged; the VAT line on the formatted output already exists in POS-06.

### DI registration (in `MerchSys.App/Startup/`)
```
services.AddScoped(Of IReceiptService, ReceiptService)
services.Decorate(Of IReceiptService, VatAwareReceiptService)   ' Or manual factory if Decorate not available
services.AddScoped(Of IVatCalculator, VatCalculator)
services.AddSingleton(Of VatConfigurationLoader)                ' Caches the singleton row
```

If the project does not already use `Scrutor` for `Decorate`, register `VatAwareReceiptService` as `IReceiptService` directly and inject the inner `ReceiptService` by concrete type — the Scrutor dependency is preferred but not required.

## Implementation Notes

- The `VatConfiguration.Id = 1` constraint is enforced both at the EF layer (`HasCheckConstraint`) and via a unique index on `Id`
- `VatRateSnapshot` and `IsVatRegisteredSnapshot` exist so historical receipts retain the mode they were issued under, even after a configuration change
- Banker's rounding chosen over half-away-from-zero because BIR examples in `concepts/vat-ready.md` use it
- The legacy `SaleCompletedEvent` publish is retained until ACC-11 ships and consumers fully migrate; deletion of the legacy publish is out of scope here
- POS-13's hash input includes the new VAT fields automatically because the canonical payload references the persisted entity

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings
2. Migration `AddVatThreeBucketColumns` applies cleanly to a fresh and existing SQLite database; backfill produces non-null values for every row
3. With `IsVatRegistered = True, VatRate = 0.12`: a ₱112.00 vatable line decomposes to `VatableAmount = 100.00`, `OutputVat = 12.00`
4. With `IsVatRegistered = False`: every line yields `OutputVat = 0` and `VatExemptAmount = LineTotal` regardless of `Treatment` field
5. Transaction totals equal sum of line totals to the centavo (no rounding drift)
6. `VatAwareReceiptService.GenerateReceiptAsync` publishes both `SaleCompletedEvent` and `SaleCompletedWithVatEvent` with matching `TransactionId`
7. `VatConfiguration` table contains exactly one row after migration; attempts to insert a second row fail at the database
8. Hash chain in POS-13 still validates after the schema extension

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-14-summary.md` using `Progress/_template.md`.

### Documentation
- XML doc comments on `IVatCalculator`, `VatBreakdown`, `VatConfiguration` citing `concepts/vat-ready.md`
- Inline comment on the `Decorate` registration explaining publish-both-events migration window
