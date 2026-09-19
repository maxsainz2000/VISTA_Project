---
module: MerchSys.Accounting
plan-id: ACC-07
title: "View — Financial Overview"
depends-on: [ACC-03, ACC-06]
estimated-files: 3
---

# View — Financial Overview Dashboard

## Context

The primary Accounting screen — a KPI dashboard showing revenue, margins, AR/AP, inventory value, trends, and top products. Both Manager and Owner use this screen. Includes the mandatory "What This Means" box.

## Prerequisites

- **ACC-03** (Financial Overview Service), **ACC-06** (What This Means Engine)

## Wiki References

- `entities/module-accounting.md` — "Financial Overview Dashboard — KPIs, 6-month trend, top products"
- `concepts/plain-language-reporting.md` — mandatory box, always visible

## Deliverables

```
MerchSys.App/Views/Accounting/
├── FinancialOverviewView.xaml
└── FinancialOverviewView.xaml.vb

MerchSys.Accounting/ViewModels/
└── FinancialOverviewViewModel.vb
```

## Specification

### Screen Layout

1. **KPI Cards (top row):**
   - Today Revenue | MTD Revenue | YTD Revenue | Gross Margin % | AR Outstanding | AP Outstanding | Inventory Value

2. **"What This Means" Box (prominent, cannot be hidden):**
   - Distinct background (light blue or light yellow panel)
   - 💡 icon + "What This Means" header
   - Plain-language interpretation from `WhatThisMeansService`

3. **6-Month Trend Chart:**
   - Bar/line chart: Revenue, COGS, Gross Profit per month
   - Labels on axes

4. **Top Products Table:**
   - Top 10 by revenue: ProductName, Units Sold, Revenue, COGS, Margin %

5. **Alerts Panel:**
   - Overdue AR count, Overdue AP count, Low stock warnings

### Auto-Refresh
Refresh data on navigation to this screen and every 5 minutes.

## Acceptance Criteria

1. `dotnet build` succeeds
2. All KPIs displayed correctly
3. **"What This Means" box always visible** — no collapse/hide option
4. Trend chart shows 6 months
5. Top products ranked by revenue
6. Both Manager and Owner can access this screen

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Accounting/ACC-07-summary.md`
