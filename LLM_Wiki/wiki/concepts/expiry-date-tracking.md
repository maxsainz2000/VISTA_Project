---
type: concept
title: "Expiry Date Tracking"
aliases: [expiry tracking, batch-level expiry, perishable monitoring]
sources: [Sources/Inventory-Module_AcademicPaper.md]
related: [fifo-costing, module-inventory]
last-updated: 2026-05-02
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

## Source References

- [[wiki/sources/inventory-module-paper|Inventory Paper]] — Problem I4, batch-level expiry definition
