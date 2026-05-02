---
module: MerchSys.Accounting
plan-id: ACC-08
title: "View — Income Statement"
depends-on: [ACC-04, ACC-06]
estimated-files: 3
---

# View — Income Statement

## Context

WPF View for the merchandising-format income statement (P&L). Supports monthly, quarterly, and annual views. Includes per-product margin analysis tab and mandatory "What This Means" box.

## Prerequisites

- **ACC-04** (Income Statement Service), **ACC-06** (What This Means Engine)

## Deliverables

```
MerchSys.App/Views/Accounting/
├── IncomeStatementView.xaml
└── IncomeStatementView.xaml.vb

MerchSys.Accounting/ViewModels/
└── IncomeStatementViewModel.vb
```

## Specification

### Screen Layout

1. **Period Selector:** Monthly / Quarterly / Annual toggle + date/period picker
2. **Income Statement Display:**
   ```
   Net Sales                    ₱ XX,XXX.XX
   Less: Cost of Goods Sold     (₱ XX,XXX.XX)
   ─────────────────────────────────────────
   Gross Profit                 ₱ XX,XXX.XX    (XX.X%)
   Less: Operating Expenses     (₱ XX,XXX.XX)
      Shrinkage Loss            (₱ XX,XXX.XX)
   ─────────────────────────────────────────
   Net Income                   ₱ XX,XXX.XX    (XX.X%)
   ```

3. **"What This Means" Box (mandatory):**
   - Plain-language interpretation of the P&L
   - Compares to previous period if available

4. **Per-Product Margins Tab:**
   - DataGrid: ProductName, Revenue, COGS, GrossProfit, MarginPercent, UnitsSold
   - Sort by Margin (lowest first to identify underperforming products)

### Formatting
- Philippine Peso (₱) currency format
- Negative amounts in parentheses: (₱1,234.56)
- Percentages to 1 decimal place

## Acceptance Criteria

1. `dotnet build` succeeds
2. Merchandising P&L format rendered correctly
3. Monthly/quarterly/annual switching works
4. **"What This Means" box present and always visible**
5. Per-product margins sortable
6. Currency formatting correct (₱, commas, parentheses for negatives)

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-08-summary.md`
