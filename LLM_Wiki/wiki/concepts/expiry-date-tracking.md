---
type: concept
title: "Expiry Date Tracking"
aliases: [expiry tracking, batch-level expiry, perishable monitoring]
sources: [Sources/Inventory-Module_AcademicPaper.md, Sources/Villon_Interview_Populated.md]
related: [villon-interview-populated, fifo-costing, module-inventory]
last-updated: 2026-06-01
---

# Expiry Date Tracking

## Definition

Systematic monitoring of expiration dates at the **batch level** for products with limited shelf life: pesticides, seeds, and animal feeds. The system alerts when products approach expiry and prevents sale of expired goods.

## Affected Products

| Category | Expiry Concern |
|---|---|
| Pesticides | Efficacy degrades past expiry |
| Seeds | Germination rates drop |
| Animal Feeds | Spoilage, health risk |
| Fertilizers | Generally shelf-stable — no expiry tracking required |

## Alert Logic

1. Each batch carries an `ExpiryDate` (nullable — not all products expire)
2. System calculates `DaysUntilExpiry = ExpiryDate − Today`
3. **Near-expiry alert** when `DaysUntilExpiry ≤ threshold` (manager-configurable)
4. **Expired status** when `ExpiryDate < Today` — flagged red, blocked from sale

## FIFO Synergy

[[fifo-costing|FIFO]] naturally prioritizes oldest batches for sale, which aligns with expiry management. The combination means the system both costs and tracks age correctly.

## Operational Validation

The populated manager interview (`Sources/Villon_Interview_Populated.md`) validates these expiry tracking details:
- **Perishable Categories (Q12):** The manager explicitly confirmed that expiry dates must be tracked for **Pesticides/Chemicals**, **Seeds**, and **Feeds**.
- **Fertilizer Exemption (Q12):** The manager did not select fertilizers as requiring expiry tracking, operationally validating the design decision that fertilizers are shelf-stable and do not require tracking.

## Source References

- [[wiki/sources/inventory-module-paper|Inventory Paper]] — Problem I4, batch-level expiry definition
- [[wiki/sources/villon-interview-populated|Villon Interview]] — operational validation of product expiry classifications

