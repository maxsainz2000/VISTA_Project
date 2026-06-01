---
type: entity
title: "Villon Farm Supply"
aliases: [VFS, the store, the business]
sources: [Sources/system_plan.md, Sources/POS-Module_AcademicPaper.md, Sources/Inventory-Module_AcademicPaper.md, Sources/Purchasing-Module_AcademicPaper.md, Sources/Accounting-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, module-purchasing, module-inventory, module-pos, module-accounting, tacurong-city]
last-updated: 2026-06-01
---

# Villon Farm Supply

The target business for the VISTA system. A single-location agricultural retail store in the Philippines.

## Profile

| Field | Value |
|---|---|
| Type | Agricultural supply and trade (merchandising) |
| Location | Tacurong City, Sultan Kudarat (POS/Purchasing papers); Lanao del Norte (Accounting paper) |
| Products | Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds |
| SKU Count | ~50 (confirmed in manager interview) |
| Personnel | Manager (operational respondent), Owner (personally handles bookkeeping manually) |
| BIR Status | Registered — issues Official Receipts (ORs) |
| Peak Day | Sunday (local market day) |
| Peak Season | Palay planting and growing season |
| Credit Practice | [[utang-credit-system\|Utang]] — credit extended to select customers; zero-tolerance balance block; collections via store visits or personal home visits |

## Current State (Pre-VISTA)

- **Sales:** Manual receipt book, credit ledger (S1–S5)
- **Inventory:** Physical record book, weekly 1–3 hr physical count (I1–I7)
- **Purchasing:** Gut-feel reorders, manual record book, tracking of AP based on supplier visit schedules rather than formal ledgers (P1–P6)
- **Accounting:** Manual bookkeeping by the owner; daily/weekly summaries delivered to owner via text/chat messages (A1–A7)

## User Roles

| Role | Access Level | Primary Needs |
|---|---|---|
| Manager | Full CRUD on all operational features | Process sales, manage stock, create POs, manage collections |
| Owner | Read-only on dashboards/reports | Financial oversight (daily/weekly updates), inventory valuation |

## Source Notes

> [!NOTE]
> The Accounting paper lists the locale as "Lanao del Norte" while POS and Purchasing papers specify "Tacurong City, Sultan Kudarat." This may reflect different phases of drafting. See [[wiki/sources/pos-module-paper|POS Paper]] and [[wiki/sources/accounting-module-paper|Accounting Paper]] for details.

