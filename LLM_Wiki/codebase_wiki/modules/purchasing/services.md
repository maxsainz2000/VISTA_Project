---
type: layer-manifest
module: MerchSys.Purchasing
layer: Services
last-updated: 2026-05-04
---

# MerchSys.Purchasing — Services

This page details the Service implementations for the **MerchSys.Purchasing** module.

## Core Services

| File Path | Interface & Implementation | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Purchasing/Services/IPurchaseOrderService.vb`<br>`src/MerchSys.Purchasing/Services/PurchaseOrderService.vb` | `IPurchaseOrderService`<br>`PurchaseOrderService` | PO Lifecycle Service. Enforces the Draft → Submitted → Received → Verified → Closed state machine. `CloseAsync` creates an `AccountsPayableEntry`; `RecalculateTotal` updates `TotalAmount` on line changes. Invalid transitions throw `InvalidOperationException`. Also defines `CreatePOLineDto`. |

## Helpers

| File Path | Class | Key Responsibilities |
|---|---|---|
| `src/MerchSys.Purchasing/Helpers/SequentialNumberGenerator.vb` | `SequentialNumberGenerator` | Static helper with `Generate(prefix, year, existingNumbers)` to produce sequence strings like `PO-YYYY-XXXX` or `GR-YYYY-XXXX`. |
