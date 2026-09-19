---
type: layer-manifest
module: MerchSys.POS
layer: Handlers
last-updated: 2026-05-07
---

# MerchSys.POS — Handlers

This page details the MediatR Handlers for the **MerchSys.POS** module.

## Request Handlers

| File Path | Class | Handles | Responsibilities |
|---|---|---|---|
| `src/MerchSys.POS/Handlers/GetTotalARQueryHandler.vb` | `GetTotalARQueryHandler` | `GetTotalARQuery` | Sums `CreditAccount.CurrentBalance` across all non-deleted accounts to provide total AR for Accounting. |
| `src/MerchSys.POS/Handlers/GetVatConfigurationQueryHandler.vb` | `GetVatConfigurationQueryHandler` | `GetVatConfigurationQuery` | Provides VAT registration status and tax rates for reporting logic. |
| `src/MerchSys.POS/Handlers/GetOverdueARCountQueryHandler.vb` | `GetOverdueARCountQueryHandler` | `GetOverdueARCountQuery` | Returns the number of active customer credit accounts with outstanding balances (> 0) for Accounting. |

