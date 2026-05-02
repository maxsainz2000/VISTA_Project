---
module: MerchSys.Accounting
plan-id: ACC-06
title: "What This Means Engine"
depends-on: [ACC-03, ACC-04, ACC-05]
estimated-files: 2
---

# "What This Means" Engine

## Context

Implements the **mandatory, non-negotiable** plain-language interpretation system. Every report tab in the Accounting module must include a "What This Means" box that translates financial figures into non-technical, actionable sentences for the store manager. This is the defining feature of the Accounting module. Addresses Problem A5.

## Prerequisites

- **ACC-03, ACC-04, ACC-05** — report data services exist to provide the numbers

## Wiki References

- `concepts/plain-language-reporting.md` — design rules, example text, mandatory/non-optional requirement
- `sources/accounting-module-paper.md` — "A5: Manager has partial financial literacy", "<30% of Mindanao micro-business owners can interpret financial statements"

## Deliverables

```
MerchSys.Accounting/Services/
├── IWhatThisMeansService.vb
└── WhatThisMeansService.vb
```

## Specification

### IWhatThisMeansService
```
GenerateOverviewInterpretation(data As FinancialOverviewDto) As String
GenerateIncomeStatementInterpretation(data As IncomeStatementDto) As String
GenerateSalesSummaryInterpretation(data As AccountingSalesSummaryDto) As String
GenerateMarginAlert(currentMargin As Decimal, previousMargin As Decimal) As String
```

### Interpretation Rules

1. **Non-optional** — cannot be hidden, collapsed, or disabled in the UI
2. **Simple language** — no accounting jargon
3. **Business-focused** — explains what numbers mean for the business
4. **Actionable** — includes guidance: "This means...", "Consider..."
5. **Contextual** — compares to previous period where data is available

### Template Examples

**Financial Overview:**
```
"Your store earned ₱{revenue} today from {txCount} transactions. 
Month-to-date revenue is ₱{mtd}, which is {changePercent}% {higher/lower} than last month at this point. 
You have ₱{ar} in outstanding credit (utang) from customers and ₱{ap} in unpaid supplier bills."
```

**Income Statement:**
```
"This month, your gross profit margin is {margin}%. For every ₱100 of sales, your business keeps ₱{marginPesos} after paying for the cost of goods. 
{If margin dropped}: This is lower than last month ({prevMargin}%), which may be caused by supplier price increases. Consider reviewing your retail prices.
{If margin improved}: This is an improvement from last month ({prevMargin}%). Your pricing or cost management is working well."
```

**Sales Summary:**
```
"Today's total sales were ₱{total} from {count} transactions. 
{If credit > 30%}: {creditPercent}% of sales were on credit (utang). This is higher than normal — monitor your accounts receivable closely.
Your busiest payment method was {topMethod} with {topCount} transactions."
```

### Margin Alert
```
"⚠ Your gross margin dropped from {prev}% to {current}% — a {diff}% decline. 
Top contributors: {productList with biggest margin drops}. 
Consider checking recent supplier price changes."
```

### Implementation Pattern

Use a template-based approach:
1. Accept the DTO with raw numbers
2. Calculate comparisons (vs previous period if available)
3. Select appropriate templates based on thresholds (good/warning/critical)
4. Fill in values and return the formatted string
5. All text in English (Filipino translation is out of scope for V1)

## Implementation Notes

- This service returns strings — the UI simply displays them in a styled text block
- **Every** report ViewModel must call the appropriate interpretation method
- The text block should be visually prominent: use a distinct background color, an icon (💡 or 📊), and a label "What This Means"
- No conditional visibility — the box is ALWAYS shown

## Acceptance Criteria

1. `dotnet build` succeeds
2. Interpretation generated for all 3 report types
3. Text uses simple, non-technical language
4. Comparisons with previous period included
5. Actionable guidance included
6. No way to hide or disable the output

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-06-summary.md`
