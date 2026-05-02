---
type: concept
title: "BIR Compliance"
aliases: [BIR, Official Receipt, OR-YYYY-XXXX, Bureau of Internal Revenue]
sources: [Sources/POS-Module_AcademicPaper.md]
related: [module-pos, vat-ready]
last-updated: 2026-05-02
---

# BIR Compliance

## Requirements

The Bureau of Internal Revenue requires all registered Philippine businesses to:

1. **Issue official receipts** for every sales transaction (NIRC §113)
2. Include: business name, address, TIN, date, itemized goods, amount, unique receipt number
3. Use **sequential numbering** — no gaps, no duplicates
4. **Retain records** for minimum 10 years

## VISTA Implementation

| Feature | Detail |
|---|---|
| Receipt Format | `OR-YYYY-XXXX` (year + sequential number) |
| Generation | Automatic on every completed sale — no manual step |
| Content | All BIR-mandated fields included |
| Storage | Full digital record, searchable by date and TX number |
| Tamper-proof | Receipt numbers are system-generated, non-editable |

## VAT Considerations

- Businesses with annual gross sales > PHP 3M must register for VAT
- VISTA implements a [[vat-ready|VAT-ready]] structure supporting both VAT (12%) and non-VAT
- Configuration based on Villon Farm Supply's current tax registration status

## Penalties for Non-Compliance

- PHP 1,000–50,000 per violation (NIRC §264)
- Potential criminal prosecution for repeat offenders

## Source References

- [[wiki/sources/pos-module-paper|POS Paper]] — RR 18-2012, RR 11-2018, receipt requirements
