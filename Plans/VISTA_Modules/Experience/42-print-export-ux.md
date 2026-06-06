---
module: MerchSys.App
plan-id: UX-42
title: "Print & Export UX — BIR Official-Receipt Template, PDF/CSV Export & Print Preview"
depends-on: [UX-21]
estimated-files: 9
---

# Print & Export UX — BIR Official-Receipt Template, PDF/CSV Export & Print Preview

> **Level 3 (Pro) — item P7** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). The most "feature-like"
> roadmap item — a compliant BIR Official-Receipt print template plus report export (PDF/CSV) with
> print preview. **Confirm scope before generating into code.** Presents existing data; adds no new
> business metric.

## Context

Printing/export is a real operational need (BIR compliance, owner record-keeping), and the current
presentation isn't print-shaped:

- **BIR Official Receipt** — POS already renders a receipt block (`Views/POS/SalesCartView.xaml` —
  `CurrentReceipt.BusinessTIN`/`IssueDate`/`TotalAmount`/`VatAmount`), but there is no print-formatted OR
  honouring the BIR rules in `LLM_Wiki/wiki/concepts/bir-compliance.md` (OR layout, VAT breakdown).
- **Report export** — the Accounting reports (`IncomeStatementView`, `VatReturnView`,
  `VatReliefReportView`) have no PDF/CSV export or print preview.

Built on **UX-21 (B5)** centralised currency/date formatting so printed numbers match on-screen. This is
a **presentation layer over existing data** — no new KPI/business rule, no data-access change. UX-08
listed a BIR OR template as a non-goal *for that sub-epic*; the roadmap deliberately reclaims it here.

## Prerequisites

- **UX-21** — centralised number/date/currency formatting; printed values must use it so they match the
  on-screen presentation.
- **Domain wiki (read-only)** — `LLM_Wiki/wiki/concepts/bir-compliance.md` (OR rules, VAT handling).
- Existing receipt/report data: `CurrentReceipt.*` (POS), and the Accounting report views/VMs.
- Existing deps only. **No new NuGet** unless this plan explicitly authorises one for PDF generation —
  decide during scope confirmation; default is WPF's built-in `FlowDocument`/`XpsDocument`/`PrintDialog`
  and a hand-rolled CSV writer.

## Scope

> **Scope-confirmation gate:** because this is the most feature-like item and may touch a NuGet
> decision, confirm the PDF approach (built-in XPS/FlowDocument→XPS vs. an authorised package) and the
> exact report set **before** implementing. Record the decision in the summary.

### A. BIR Official-Receipt print template
A print-formatted OR (`FlowDocument`/fixed template) populated from `CurrentReceipt.*`, laid out per
`bir-compliance.md` (business name/TIN, OR number, date, line items, VATable/VAT-exempt/zero-rated
breakdown, VAT amount, total), with a `PrintDialog` path. Numbers/dates via UX-21 formatting.

### B. Report export (PDF/CSV) + print preview
Export commands on the Accounting reports: **CSV** (hand-rolled, culture-safe) and **PDF/print** (via
the chosen approach), each with a **print preview** before output. Exports reflect exactly what's on
screen (same formatting), read-only.

> **Out of scope:**
> - Any new business metric, KPI, or VAT *computation* — printing/exporting presents values a service
>   already computes.
> - Data-access, `DbContext`, or contract changes; emailing/uploading documents.
> - A reporting-engine dependency beyond what this plan explicitly authorises.

## Specification

### 0. Watch-items

1. **Presentation over existing data.** No new computation; pull `CurrentReceipt.*` and existing report
   VM values. VAT/total are displayed, not recomputed here.
2. **BIR correctness.** The OR layout matches `bir-compliance.md` (required fields, VAT breakdown); do
   not invent fields or omit mandated ones.
3. **UX-21 formatting.** All printed/exported peso amounts, quantities, and dates go through the
   centralised formatters so print matches screen exactly.
4. **NuGet discipline.** No new package unless this plan authorises it in the confirmed scope; prefer
   built-in XPS/`FlowDocument` + a hand-rolled CSV writer. Pin any authorised package.
5. **Role model.** Owner can print/export read-only reports; OR printing is a Manager/POS action — Owner
   gets no write/issue affordance.
6. **VB traps** — no `Await` in `Catch`/`Finally` (BC36943) around print/export IO (capture error state,
   await after); reserved-keyword-safe names; `System.Console` if logging; full `clr-namespace` root
   prefix (MC3074) for any new view.

### 1. Constraints

- Build on UX-21 formatting and existing receipt/report data; add no data-access or contract change.
- No new NuGet unless explicitly authorised in the confirmed scope. No inline hex on any new template.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check (both themes where on-screen; print output is theme-independent):** an OR prints
  with all BIR-required fields and a correct VAT breakdown; each targeted report previews then exports
  to PDF and CSV with formatting matching the screen.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. A BIR-compliant Official Receipt prints from `CurrentReceipt.*`, laid out per `bir-compliance.md`,
   with numbers/dates via UX-21 formatting.
3. The targeted Accounting reports export to PDF and CSV with a print preview, matching on-screen values.
4. No new business metric/computation, data-access, or contract change; any new NuGet is explicitly
   authorised and pinned.
5. Owner export is read-only; OR issuance stays a Manager/POS action.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-42-summary.md` (template `Progress/_template.md`). Include: the
confirmed scope (PDF approach + report set + any authorised NuGet and why), the OR template's BIR-field
mapping, the export/preview wiring, and realization (OR print, each report's preview+PDF+CSV). Note
`codebase_wiki` discrepancies (OR template, export commands, any new package) for Antigravity.

### Documentation
Add `patterns/wpf-vista-print-export.md` (the OR `FlowDocument` template + BIR field map, the
preview→PDF/CSV export recipe, UX-21 formatting reuse, the NuGet decision) per
`workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
