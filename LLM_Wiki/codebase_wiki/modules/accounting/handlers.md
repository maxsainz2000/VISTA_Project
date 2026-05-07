---
type: layer-manifest
module: MerchSys.Accounting
layer: Handlers
last-updated: 2026-05-07
---

# MerchSys.Accounting — Handlers

This page details the MediatR event handlers for the **MerchSys.Accounting** module.

## Event Handlers

| File Path | Class | Listens To | Responsibility |
|---|---|---|---|
| `src/MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` | `SaleCompletedAccountingHandler` | `SaleCompletedEvent` | Records per-item revenue and COGS expenses. Resolves FIFO cost by sending `GetProductCostQuery` to Inventory via MediatR. |
| `src/MerchSys.Accounting/Handlers/GoodsReceivedAccountingHandler.vb` | `GoodsReceivedAccountingHandler` | `GoodsReceivedEvent` | Records accounts payable "Purchase" expenses when goods enter the system. |
| `src/MerchSys.Accounting/Handlers/CreditPaymentAccountingHandler.vb` | `CreditPaymentAccountingHandler` | `CreditPaymentEvent` | Records "AR Reduction" expenses (offsets) when customers pay down credit balances. |
| `src/MerchSys.Accounting/Handlers/ShrinkageAccountingHandler.vb` | `ShrinkageAccountingHandler` | `ShrinkageRecordedEvent` | Records "Shrinkage" expenses for inventory write-offs or theft loss. |
