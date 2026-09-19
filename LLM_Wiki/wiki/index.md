---
type: index
title: "VISTA Domain Wiki — Master Index"
aliases: [wiki index, domain wiki, wiki home]
last-updated: 2026-06-01
---

# VISTA Domain Wiki — Master Index

> Central catalog for all domain knowledge pages in the VISTA wiki vault.
> Maintained by **Antigravity**. Other agents: read only.

---

## Source Summaries

Condensed versions of raw documents in `Sources/`. Never fabricated — every claim traceable.

| Page | Raw Source | Lines |
|---|---|---|
| [[wiki/sources/system-plan\|System Plan]] *(partially superseded)* | `Sources/system_plan.md` | 375 |
| [[wiki/sources/system-plan-amendment-2026-05-28\|System Plan Amendment 2026-05-28]] | `Sources/system_plan_amendment_2026-05-28.md` | — |
| [[wiki/sources/purchasing-module-paper\|Purchasing Paper]] | `Sources/Purchasing-Module_AcademicPaper.md` | 266 |
| [[wiki/sources/inventory-module-paper\|Inventory Paper]] | `Sources/Inventory-Module_AcademicPaper.md` | 218 |
| [[wiki/sources/pos-module-paper\|POS Paper]] | `Sources/POS-Module_AcademicPaper.md` | 214 |
| [[wiki/sources/accounting-module-paper\|Accounting Paper]] | `Sources/Accounting-Module_AcademicPaper.md` | 240 |
| [[wiki/sources/villon-interview-populated\|Villon Interview]] | `Sources/Villon_Interview_Populated.md` | 361 |

---

## Entities

Named things in the project — modules, organizations, locations.

| Page | Type | Description |
|---|---|---|
| [[wiki/entities/villon-farm-supply\|Villon Farm Supply]] | Business | Target business — agricultural retail, ~50 SKUs |
| [[wiki/entities/module-purchasing\|Purchasing Module]] | Module | PO lifecycle, vendor directory, AP, reorder engine |
| [[wiki/entities/module-inventory\|Inventory Module]] | Module | Central hub — stock dashboard, FIFO, expiry, alerts |
| [[wiki/entities/module-pos\|POS Module]] | Module | Cart-based TX, credit management, BIR receipts |
| [[wiki/entities/module-accounting\|Accounting Module]] | Module | Financial reports with "What This Means" boxes |
| [[wiki/entities/tacurong-city\|Tacurong City]] | Location | Sultan Kudarat — locale for POS/Purchasing papers |

---

## Concepts

Definitions, patterns, and standards used across the project.

### Architecture & Infrastructure

| Page | Description |
|---|---|
| [[wiki/concepts/modular-monolith\|Modular Monolith]] | Single .exe, 4 class libraries, enforced boundaries |
| [[wiki/concepts/mediatr-mediator\|MediatR Mediator]] | Event-driven cross-module communication |
| [[wiki/concepts/centralized-database-architecture\|Centralized Database Architecture]] | **Authoritative** — pure client-server against MariaDB, no SQLite |
| [[wiki/concepts/offline-first-sync\|Offline-First Sync]] *(superseded)* | Historical — SQLite → MariaDB sync (replaced 2026-05-28) |
| [[wiki/concepts/client-server-wpf\|Client-Server WPF]] | Desktop architecture, tech stack summary |

### Business Logic & Domain

| Page | Description |
|---|---|
| [[wiki/concepts/fifo-costing\|FIFO Costing]] | Batch-level first-in-first-out inventory valuation |
| [[wiki/concepts/expiry-date-tracking\|Expiry Date Tracking]] | Batch-level monitoring for perishable goods |
| [[wiki/concepts/utang-credit-system\|Utang Credit System]] | Philippine informal credit + hard blocking rule |
| [[wiki/concepts/bir-compliance\|BIR Compliance]] | Official receipt requirements (OR-YYYY-XXXX) |
| [[wiki/concepts/vat-ready\|VAT-Ready Structure]] | Configurable VAT (12%) and non-VAT support |
| [[wiki/concepts/plain-language-reporting\|Plain-Language Reporting]] | Mandatory "What This Means" interpretation boxes |
| [[wiki/concepts/reorder-suggestion-engine\|Reorder Suggestion Engine]] | Threshold-based reorder with seasonal flags |

### Research & Evaluation

| Page | Description |
|---|---|
| [[wiki/concepts/owasp-da-top10\|OWASP DA Top 10]] | Desktop security requirements (DA1–DA10) |
| [[wiki/concepts/iso-25010-2023\|ISO 25010:2023]] | 8-characteristic software quality standard |
| [[wiki/concepts/utaut2\|UTAUT2]] | Technology acceptance framework |
| [[wiki/concepts/descriptive-developmental-design\|Descriptive-Developmental Design]] | Research methodology (SOP 1–4) |

---

## Analysis Pages

Cross-cutting analysis and reference material synthesized from multiple sources.

| Page | Description |
|---|---|
| [[wiki/analysis/cross-module-data-flow\|Cross-Module Data Flow]] | MediatR event catalog + integration diagram |
| [[wiki/analysis/problem-feature-matrix\|Problem-Feature Matrix]] | All 25 problems → 25 features (traceability) |
| [[wiki/analysis/tech-stack-reference\|Tech Stack Reference]] | Full technology stack with versions |

---

## Quick Stats

| Metric | Count |
|---|---|
| Source Summaries | 7 |
| Entity Pages | 6 |
| Concept Pages | 15 (1 superseded) |
| Analysis Pages | 3 |
| **Total Wiki Pages** | **31** |
| Problems Documented | 25 |
| Modules | 4 |

---

## Activity Log

See [[wiki/log\|log.md]] for chronological wiki maintenance history.
