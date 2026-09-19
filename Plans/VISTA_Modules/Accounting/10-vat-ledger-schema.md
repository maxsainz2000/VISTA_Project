---
module: MerchSys.Accounting
plan-id: ACC-10
title: "Accounting VAT Ledger Schema Extension"
depends-on: [ACC-01, ACC-02, INFRA-07]
estimated-files: 5
---

# Accounting VAT Ledger Schema Extension

## Context

The Feature Gap Audit (2026-05-10) found that Accounting ledger tables carry no VAT columns and no `VatReturn` header to back BIR Form 2550M/Q or 2551Q reporting. ACC-11 will implement the reporting service and view; this plan provides the schema substrate it needs. All entity changes ship as **partial-class extensions** plus a single EF migration so the original ACC-01/ACC-02 source files are not modified.

## Prerequisites

- **ACC-01** (Accounting Domain Models) — `RevenueRecord`, `ExpenseRecord`, `InventoryShrinkageRecord`, etc. exist
- **ACC-02** (Accounting Data Access) — `AccountingDbContext` exists
- **INFRA-07** (VAT Event Payload) — `VatTreatment` enum referenced from new `VatReturnLine`

## Wiki References

- `concepts/vat-ready.md` — output/input VAT ledger requirements
- `concepts/bir-compliance.md` — Form 2550M/Q field set
- `analysis/cross-module-data-flow.md` — Accounting consumer position

## Deliverables

```
MerchSys.Accounting/Entities/
├── VatReturn.vb
├── VatReturnLine.vb
└── Extensions/
    └── LedgerVatExtensions.vb

MerchSys.Accounting/Data/Configurations/
└── VatReturnMap.vb
```

(EF migration `AddVatLedgerColumns` ships alongside but lives under `MerchSys.Accounting/Migrations/` and is counted separately from the deliverable file count.)

## Specification

### Ledger partial-class extensions (`LedgerVatExtensions.vb`)
One file containing partial declarations for every revenue/expense/inventory entity defined in ACC-01. For each:
```
Partial Public Class RevenueRecord
    Property VatableAmount As Decimal
    Property VatExemptAmount As Decimal
    Property ZeroRatedAmount As Decimal
    Property OutputVat As Decimal
    Property InputVat As Decimal                 ' 0 for revenue rows; populated for expense/COGS
    Property VatTreatment As VatTreatment        ' From SharedKernel
End Class
```

Apply the same five-column extension to: `RevenueRecord`, `ExpenseRecord`, `InventoryShrinkageRecord`, `CostOfGoodsRecord` (whichever names ACC-01 actually uses — match those exactly when implementing). For revenue rows `InputVat` is always `0`; for expense/COGS rows `OutputVat` is always `0`.

### VatReturn (`Acc_VatReturns`)
Header for one filing period (monthly Form 2550M, quarterly Form 2550Q, or quarterly Form 2551Q for non-VAT).
```
Inherits AuditableEntity

Property Year As Integer
Property Period As Integer                       ' 1–12 monthly; 1–4 quarterly
Property PeriodType As VatReturnPeriodType       ' Enum: Monthly, Quarterly
Property FormType As VatReturnFormType           ' Enum: Form2550M, Form2550Q, Form2551Q
Property TotalVatableSales As Decimal
Property TotalVatExemptSales As Decimal
Property TotalZeroRatedSales As Decimal
Property TotalOutputVat As Decimal
Property TotalVatablePurchases As Decimal
Property TotalInputVat As Decimal
Property VatPayable As Decimal                   ' OutputVat - InputVat (or 3% × non-VAT sales for 2551Q)
Property FilingStatus As VatFilingStatus         ' Enum: Draft, Generated, Filed, Amended
Property FiledAt As DateTime?
Property FiledBy As String                       ' Nullable
Property GeneratedAt As DateTime
Property IsVatRegisteredSnapshot As Boolean

' Navigation
Property Lines As ICollection(Of VatReturnLine)
```

Unique composite index on `(Year, Period, PeriodType, FormType)` to prevent duplicate filings.

### VatReturnLine (`Acc_VatReturnLines`)
Line-level breakdown by source ledger row, kept for audit traceability back to the originating receipt or PO.
```
Inherits AuditableEntity

Property VatReturnId As Integer                  ' FK
Property SourceModule As String                  ' "POS" or "Purchasing"
Property SourceTable As String                   ' "Pos_OfficialReceipts" / "Pur_GoodsReceived"
Property SourceRowId As Long
Property TransactionDate As DateTime
Property VatableAmount As Decimal
Property VatExemptAmount As Decimal
Property ZeroRatedAmount As Decimal
Property OutputVat As Decimal
Property InputVat As Decimal
Property Treatment As VatTreatment

' Navigation
Property VatReturn As VatReturn
```

Index on `(VatReturnId)` and `(SourceModule, SourceTable, SourceRowId)`.

### Enums (in `MerchSys.Accounting/Enums/`)
```
Public Enum VatReturnPeriodType
    Monthly = 0
    Quarterly = 1
End Enum

Public Enum VatReturnFormType
    Form2550M = 0    ' Monthly VAT return (BIR)
    Form2550Q = 1    ' Quarterly VAT return
    Form2551Q = 2    ' Quarterly Percentage Tax (non-VAT, 3%)
End Enum

Public Enum VatFilingStatus
    Draft = 0
    Generated = 1
    Filed = 2
    Amended = 3
End Enum
```

### VatReturnMap (`Acc_VatReturns` configuration)
- Composite unique index `(Year, Period, PeriodType, FormType)`
- Cascade delete from `VatReturn` to `VatReturnLines` (a draft return can be regenerated; once `FilingStatus = Filed`, deletion is blocked at the service layer in ACC-11)
- All decimal columns precision(18,2)

### EF migration `AddVatLedgerColumns`
- Adds the six new columns to every revenue/expense/inventory ledger table
- Creates `Acc_VatReturns` and `Acc_VatReturnLines`
- Backfill: existing rows receive `VatableAmount = OriginalAmount`, `VatExemptAmount = 0`, `ZeroRatedAmount = 0`, `OutputVat = 0`, `InputVat = 0`, `VatTreatment = Vatable`. Non-destructive.

## Implementation Notes

- The five-column extension is identical across every ledger table for consistency. Even tables that only carry input or output VAT keep all five columns to simplify the reporting query in ACC-11.
- `VatReturn` is **not** marked `SoftDeletableEntity` even when `FilingStatus = Draft` — once filed, the row is locked at the service layer (ACC-11 is responsible for that gate).
- The schema does not enforce `OutputVat = 0 for InputVat-bearing rows` or vice versa. ACC-11 service-layer code maintains that invariant; enforcing it in DB triggers would conflict with future correction entries that touch both sides.
- `VatReturnLine.SourceRowId` is `Long` (not `Integer`) to accommodate sync-journal IDs from MariaDB after eventual reconciliation back into Accounting summaries.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings
2. EF migration `AddVatLedgerColumns` applies cleanly to a fresh and existing SQLite database
3. Backfill leaves no NULL values in the new columns on pre-existing ledger rows
4. Composite unique index on `Acc_VatReturns(Year, Period, PeriodType, FormType)` prevents duplicate filings (insertion of a duplicate fails at the database)
5. Cascade delete from `VatReturn` to `VatReturnLines` works for `Draft` returns
6. ACC-01 and ACC-02 source files are unchanged (verifiable via `git diff`)
7. Every new column is precision(18,2)
8. All three new enums are referenced from at least one entity property

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-10-summary.md` using `Progress/_template.md`.

### Documentation
- XML doc comments on `VatReturn`, `VatReturnLine`, and `LedgerVatExtensions` citing `concepts/vat-ready.md`
- Header comment in the migration file explaining the backfill strategy and listing every affected table
