---
type: concept
title: "Plain-Language Reporting"
aliases: [What This Means, plain-language interpretation, WTM boxes]
sources: [Sources/Accounting-Module_AcademicPaper.md]
related: [module-accounting]
last-updated: 2026-05-02
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

## Source References

- [[wiki/sources/accounting-module-paper|Accounting Paper]] — A5, "What This Means" as architectural requirement
