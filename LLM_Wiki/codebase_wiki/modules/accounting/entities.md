---
type: layer-manifest
module: MerchSys.Accounting
layer: Entities
last-updated: 2026-05-16
---

# MerchSys.Accounting — Entities

This page details the Entities for the **MerchSys.Accounting** module.

## Files and Classes

| File Path | Class / Interface | Base / Implements | Key Members / Responsibilities |
|---|---|---|---|
| `src/MerchSys.Accounting/Entities/FinancialPeriod.vb` | `FinancialPeriod` | `AuditableEntity` | Summarized P&L per period type (Daily/Weekly/Monthly/Quarterly/Annual). |
| `src/MerchSys.Accounting/Entities/RevenueRecord.vb` | `RevenueRecord` | `AuditableEntity` | Captured from POS events; tracks FIFO COGS and per-product gross profit. |
| `src/MerchSys.Accounting/Entities/ExpenseRecord.vb` | `ExpenseRecord` | `AuditableEntity` | Captures postings from all source modules (COGS, shrinkage, operating expenses). |
| `src/MerchSys.Accounting/Entities/FinancialSnapshot.vb` | `FinancialSnapshot` | `AuditableEntity` | Point-in-time KPI cache for AR, AP, and inventory valuation. |
| `src/MerchSys.Accounting/Entities/VatReturn.vb` | `VatReturn` | `AuditableEntity` | Header for monthly/quarterly VAT or Percentage Tax filing periods. |
| `src/MerchSys.Accounting/Entities/VatReturnLine.vb` | `VatReturnLine` | `AuditableEntity` | Audit traceability linking VAT return buckets to source ledger rows. |
| `src/MerchSys.Accounting/Entities/TamperAuditEntry.vb` | `TamperAuditEntry` | (None) | Append-only audit ledger for POS tamper incidents. Marked `<NoSync>`. |
| `src/MerchSys.Accounting/Enums/VatFilingStatus.vb` | `VatFilingStatus` | `Enum` | Lifecycle status of a VAT return (Draft, Generated, Filed, Amended). |
| `src/MerchSys.Accounting/Enums/VatReturnFormType.vb` | `VatReturnFormType` | `Enum` | BIR form type for a VAT return filing (2550M, 2550Q, 2551Q). |
| `src/MerchSys.Accounting/Enums/VatReturnPeriodType.vb` | `VatReturnPeriodType` | `Enum` | VAT return filing frequency (Monthly, Quarterly). |
| `src/MerchSys.Accounting/Exceptions/VatReturnLockedException.vb` | `VatReturnLockedException` | `InvalidOperationException` | Exception thrown when attempting to generate or modify a VAT return for a locked/filed period. |

> [!NOTE]
> **Ledger VAT Extensions:** `RevenueRecord` and `ExpenseRecord` are extended via partial classes in `Entities/Extensions/LedgerVatExtensions.vb` to include BIR-compliant VAT columns.
> 
> **Dashboard VAT Extensions:** `FinancialOverviewDto` (Services) is extended via partial classes in `ViewModels/Extensions/FinancialOverviewVatExtension.vb` to include VAT-specific KPI fields (`VatPayable`, `VatFilingDueDate`, etc.) for the dashboard.

