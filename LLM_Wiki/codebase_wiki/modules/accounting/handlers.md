---
type: layer-manifest
module: MerchSys.Accounting
layer: Handlers
last-updated: 2026-05-27
---

# MerchSys.Accounting — Handlers

This page details the MediatR event handlers for the **MerchSys.Accounting** module.

## Event Handlers

| File Path | Class | Listens To | Responsibility |
|---|---|---|---|
| `src/MerchSys.Accounting/Handlers/SaleCompletedAccountingHandler.vb` | `SaleCompletedAccountingHandler` | `SaleCompletedEvent` | Records per-item revenue and COGS expenses. Resolves FIFO cost by sending `GetProductCostQuery` to Inventory via MediatR. |
| `src/MerchSys.Accounting/Handlers/GoodsReceivedAccountingHandler.vb` | `GoodsReceivedAccountingHandler` | `GoodsReceivedEvent` | Records accounts payable "Purchase" expenses when goods enter the system. |
| `src/MerchSys.Accounting/Handlers/CreditPaymentAccountingHandler.vb` | `CreditPaymentAccountingHandler` | `CreditPaymentEvent` | Records "AR Reduction" expenses (offsets) when customers pay down credit balances. |
| `src/MerchSys.Accounting/Handlers/SaleCompletedWithVatHandler.vb` | `SaleCompletedWithVatHandler` | `SaleCompletedWithVatEvent` | Records VAT details from sales transactions into the `Acc_VatReturnLines` table. Supports idempotency via transaction/product ID pairs. |
| `src/MerchSys.Accounting/Handlers/GoodsReceivedWithVatHandler.vb` | `GoodsReceivedWithVatHandler` | `GoodsReceivedWithVatEvent` | Records VAT details from purchasing/expenses into the `Acc_VatReturnLines` table, categorized by Goods/Services/Capital Goods. |
| `src/MerchSys.Accounting/Handlers/ShrinkageAccountingHandler.vb` | `ShrinkageAccountingHandler` | `ShrinkageRecordedEvent` | Records "Shrinkage" expenses for inventory write-offs or theft loss. |
| `src/MerchSys.Accounting/Handlers/ReceiptTamperDetectedHandler.vb` | `ReceiptTamperDetectedHandler` | `ReceiptTamperDetectedEvent` | Consumes tamper events from POS; persists to `Acc_TamperAuditLog` with machine/user context. Rethrows on save failure to ensure bus-level retry or DLQ logic. |

> [!NOTE]
> As of **INFRA-21**, all six transaction/event-consuming handlers (excluding `ReceiptTamperDetectedHandler`) write records using `ISyncableRepository(Of AccountingDbContext)`. This ensures all derived financial records (revenue, expenses, VAT) are captured in `Sync_Journal` for offline-first replication. `ReceiptTamperDetectedHandler` continues to use direct DbContext saves for local-only audit entries.
