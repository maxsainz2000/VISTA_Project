---
type: concept
title: "Plain-Language Reporting"
aliases: [What This Means, plain-language interpretation, WTM boxes]
sources: [Sources/Accounting-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, module-accounting]
last-updated: 2026-06-01
---

# Plain-Language Reporting ("What This Means")

## Definition

A **mandatory**, non-negotiable UI feature where every report tab in the Accounting Module includes a "What This Means" interpretation box that translates financial figures into non-technical sentences.

## Rationale

- Problem A5: Manager has partial financial literacy
- <30% of Mindanao micro-business owners can independently interpret financial statements (Dela Cruz & Santos, 2022)
- Plain-language summaries → significantly better financial decisions (Chen & Wei, 2021)

## Design Rules

1. Appears on **every** report tab — Financial Overview, Income Statement, Sales Summary
2. **Non-optional** — cannot be hidden, collapsed, or disabled
3. Uses simple, non-technical language
4. Explains what the numbers mean **for the business**, not just what they are
5. Includes actionable guidance where appropriate ("This means…", "Consider…")

## Example

> **What This Means:** Your gross profit margin this month is 28%. For every ₱100 of sales, your business keeps ₱28 after paying for the cost of goods. This is slightly lower than last month (31%), which may be caused by supplier price increases. Consider reviewing your retail prices.

## Operational Validation

The populated manager interview (`Sources/Villon_Interview_Populated.md`) validates these plain-language reporting needs:
- **Financial Literacy (Q33):** The manager confirmed they can only *partially* interpret standard income statements, which validates the problem statement (A5) and the necessity of simple text boxes.
- **Monthly Summary Needs (Q34):** The manager identified the exact three metrics they want highlighted in plain language:
  1. Actual monthly profit or loss.
  2. Cash flow status (liquidity).
  3. Inventory movement (what sold, what remained, what to buy).

## Source References

- [[wiki/sources/accounting-module-paper|Accounting Paper]] — A5, "What This Means" as architectural requirement
- [[wiki/sources/villon-interview-populated|Villon Interview]] — operational validation of financial literacy levels and reporting needs

