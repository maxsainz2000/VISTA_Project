---
type: concept
title: "VAT-Ready Structure"
aliases: [VAT-ready, VAT configuration]
sources: [Sources/POS-Module_AcademicPaper.md]
related: [bir-compliance, module-pos]
last-updated: 2026-05-02
---

# VAT-Ready Structure

## Definition

The POS Module's transaction structure supports both VAT-registered (12% VAT) and non-VAT registered scenarios through configuration, without code changes.

## Rules

- Businesses with annual gross sales > PHP 3M must register for VAT
- Many small businesses fluctuate around this threshold
- System must support both modes via configuration setting

## Implementation

| Mode | Behavior |
|---|---|
| VAT-Registered | VAT amount calculated and shown separately on receipt |
| Non-VAT | No VAT line; simplified receipt |

## Source References

- [[wiki/sources/pos-module-paper|POS Paper]] — VAT-ready definition, Reyes & Tan (2023)
