---
module: MerchSys.POS
plan-id: POS-02
title: "POS Data Access"
depends-on: [INFRA-03, POS-01]
estimated-files: 8
---

# POS Data Access

## Context

Configures the `POSDbContext` with EF Core entity configurations for all POS entities and seed data for development.

## Prerequisites

- **INFRA-03** (Database Contexts) — `POSDbContext` shell exists
- **POS-01** (Domain Models) — all POS entities exist

## Deliverables

```
MerchSys.POS/Data/
├── POSDbContext.vb                        ← Update: add DbSets
├── Configurations/
│   ├── SalesTransactionConfiguration.vb
│   ├── SalesTransactionLineConfiguration.vb
│   ├── OfficialReceiptConfiguration.vb
│   ├── CreditAccountConfiguration.vb
│   ├── CreditPaymentConfiguration.vb
│   └── SalesReturnConfiguration.vb
└── SeedData/
    └── POSSeedData.vb
```

## Specification

### Table Names (Pos_ prefix)
`Pos_SalesTransactions`, `Pos_SalesTransactionLines`, `Pos_OfficialReceipts`, `Pos_CreditAccounts`, `Pos_CreditPayments`, `Pos_SalesReturns`

### Key Configurations
- **SalesTransaction:** TransactionNumber required max 20 unique, TotalAmount precision(18,2), Index on TransactionDate
- **OfficialReceipt:** ReceiptNumber required max 20 unique, one-to-one with SalesTransaction
- **CreditAccount:** CustomerName required max 200, CurrentBalance precision(18,2), Index on IsBlocked
- **CreditPayment:** PaymentAmount precision(18,2), CreditAccountId FK required
- **SalesReturn:** OriginalTransactionId FK required, Reason required max 500

### Seed Data
3 sample credit accounts for development testing:
- "Juan Dela Cruz" — farmer, balance ₱0 (not blocked)
- "Maria Santos" — farmer, balance ₱500 (blocked)
- "Pedro Reyes" — farmer, balance ₱0 (not blocked)

## Acceptance Criteria

1. `dotnet build` succeeds
2. All tables use `Pos_` prefix
3. Receipt-Transaction is one-to-one relationship
4. Seed data creates sample credit accounts

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-02-summary.md`
