---
type: entity
title: "Villon Farm Supply"
aliases: [VFS, the store, the business]
sources: [Sources/system_plan.md, Sources/POS-Module_AcademicPaper.md, Sources/Inventory-Module_AcademicPaper.md, Sources/Purchasing-Module_AcademicPaper.md, Sources/Accounting-Module_AcademicPaper.md]
related: [module-purchasing, module-inventory, module-pos, module-accounting, tacurong-city]
last-updated: 2026-05-02
---

# Villon Farm Supply

The target business for the VISTA system. A single-location agricultural retail store in the Philippines.

## Profile

| Field | Value |
|---|---|
| Type | Agricultural supply and trade (merchandising) |
| Location | Tacurong City, Sultan Kudarat (POS/Purchasing papers); Lanao del Norte (Accounting paper) |
| Products | Fertilizers, Pesticides/Chemicals, Seeds, Animal Feeds |
| SKU Count | ~50 |
| Personnel | Manager (operational), Owner (oversight) |
| BIR Status | Registered — must issue Official Receipts |
| Peak Day | Sunday (local market day) |
| Peak Season | Palay planting and growing season |
| Credit Practice | [[utang-credit-system\|Utang]] — farmers buy on credit, pay after harvest |

## Current State (Pre-VISTA)

- **Sales:** Manual receipt book, no digital record (S1–S5)
- **Inventory:** Physical record book, weekly 1–3 hr count (I1–I7)
- **Purchasing:** Gut-feel reorders, manual supplier ledger (P1–P6)
- **Accounting:** Handwritten journals, reports via text message (A1–A7)

## User Roles

| Role | Access Level | Primary Needs |
|---|---|---|
| Manager | Full CRUD on all operational features | Process sales, manage stock, create POs |
| Owner | Read-only on dashboards/reports | Financial oversight, inventory valuation |

## Source Notes

> [!NOTE]
> The Accounting paper lists the locale as "Lanao del Norte" while POS and Purchasing papers specify "Tacurong City, Sultan Kudarat." This may reflect different phases of drafting. See [[wiki/sources/pos-module-paper|POS Paper]] and [[wiki/sources/accounting-module-paper|Accounting Paper]] for details.
