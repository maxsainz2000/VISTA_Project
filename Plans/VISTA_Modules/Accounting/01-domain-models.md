---
module: MerchSys.Accounting
plan-id: ACC-01
title: "Accounting Domain Models"
depends-on: [INFRA-02]
estimated-files: 4
---

# Accounting Domain Models

## Context

Defines the core entities for the Accounting module — the aggregator that reads data from Purchasing (AP), Inventory (COGS/valuations), and POS (revenue/AR) to auto-generate financial reports. Every report carries a mandatory "What This Means" interpretation.

## Prerequisites

- **INFRA-02** (Shared Kernel) — `SoftDeletableEntity`, `AuditableEntity`

## Wiki References

- `entities/module-accounting.md` — A1–A7, V1 scope, V2 scope, data flow
- `concepts/plain-language-reporting.md` — "What This Means" boxes, mandatory, non-optional
- `concepts/fifo-costing.md` — FIFO-based COGS calculation
- `sources/accounting-module-paper.md` — V1 features, merchandising P&L format

## Deliverables

```
MerchSys.Accounting/Entities/
├── FinancialPeriod.vb
├── RevenueRecord.vb
├── ExpenseRecord.vb
└── FinancialSnapshot.vb
```

## Specification

### FinancialPeriod
```
Inherits AuditableEntity

Property PeriodType As String               ' "Daily", "Weekly", "Monthly", "Quarterly", "Annual"
Property StartDate As DateTime
Property EndDate As DateTime
Property TotalRevenue As Decimal
Property TotalCOGS As Decimal
Property GrossProfit As Decimal             ' Revenue - COGS
Property GrossMarginPercent As Decimal      ' (GrossProfit / Revenue) × 100
Property TotalExpenses As Decimal           ' Operating expenses
Property NetIncome As Decimal               ' GrossProfit - Expenses
Property IsClosed As Boolean                ' Period finalized
```

### RevenueRecord
```
Inherits AuditableEntity

Property RecordDate As DateTime
Property SourceTransactionId As Integer     ' Cross-module: POS TX ID
Property PaymentMethod As PaymentMethod
Property GrossAmount As Decimal
Property DiscountAmount As Decimal
Property NetAmount As Decimal               ' Gross - Discount
Property VatAmount As Decimal
Property ProductId As Integer               ' For per-product margin tracking
Property ProductName As String
Property QuantitySold As Integer
Property COGS As Decimal                    ' From FIFO batch costs
Property GrossProfit As Decimal             ' NetAmount - COGS
```

### ExpenseRecord
```
Inherits AuditableEntity

Property RecordDate As DateTime
Property Category As String                 ' "COGS", "Shrinkage", "Operating"
Property Description As String
Property Amount As Decimal
Property SourceModule As String             ' "Purchasing", "Inventory", "POS"
Property SourceReferenceId As Integer?      ' Optional FK to source record
```

### FinancialSnapshot
```
Inherits AuditableEntity

Property SnapshotDate As DateTime
Property TotalAR As Decimal                 ' Accounts Receivable (from POS credits)
Property TotalAP As Decimal                 ' Accounts Payable (from Purchasing)
Property InventoryValue As Decimal          ' From Inventory FIFO valuation
Property TodayRevenue As Decimal
Property MonthToDateRevenue As Decimal
Property YearToDateRevenue As Decimal
```

## Implementation Notes

- `RevenueRecord` is created when Accounting handles `SaleCompletedEvent` — one record per product per transaction
- `ExpenseRecord` captures COGS (from sale events), shrinkage costs, and AP/purchase expenses
- `FinancialSnapshot` is a point-in-time cache of key financial indicators, refreshed periodically
- All monetary values use `Decimal` with precision(18,2)
- No cross-module entity navigation — only integer IDs

## Acceptance Criteria

1. `dotnet build` succeeds
2. All 4 entities exist with correct namespaces
3. Revenue record captures per-product COGS for margin analysis
4. No cross-module entity references
5. XML doc comments on all public members

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-01-summary.md`
