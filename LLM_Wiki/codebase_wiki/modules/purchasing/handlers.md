---
type: layer-manifest
module: MerchSys.Purchasing
layer: Handlers
last-updated: 2026-05-07
---

# MerchSys.Purchasing — Handlers

This page details the MediatR Handlers for the **MerchSys.Purchasing** module.

## Request Handlers

| File Path | Class | Handles | Responsibilities |
|---|---|---|---|
| `src/MerchSys.Purchasing/Handlers/GetTotalAPQueryHandler.vb` | `GetTotalAPQueryHandler` | `GetTotalAPQuery` | Sums `AccountsPayableEntry.Balance` for all unpaid entries to provide total AP for Accounting. |
| `src/MerchSys.Purchasing/Handlers/GetOverdueAPCountQueryHandler.vb` | `GetOverdueAPCountQueryHandler` | `GetOverdueAPCountQuery` | Returns the number of unpaid supplier bills past their due dates for Accounting. |

