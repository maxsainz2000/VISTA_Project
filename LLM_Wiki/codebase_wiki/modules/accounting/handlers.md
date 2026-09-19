---
type: layer-manifest
module: MerchSys.Accounting
layer: Handlers
last-updated: 2026-05-28
---

# MerchSys.Accounting — Handlers

This page details the MediatR event handlers for the **MerchSys.Accounting** module.

## Event Handlers

| File Path | Class | Listens To | Responsibility |
|---|---|---|---|
| `src/MerchSys.Accounting/Handlers/SaleRevenueHandler.vb` | `SaleRevenueHandler` | `SaleCompletedWithVatEvent` | Consolidated authoritative writer for sales transactions (ACC-22). Records revenue (`Acc_RevenueRecords`), VAT details, and COGS expenses (`Acc_ExpenseRecords`). Queries exact per-batch FIFO COGS breakdown via `GetSaleCogsBreakdownQuery` (ACC-21), falling back defensively to oldest FIFO batch cost when not yet committed. |
| `src/MerchSys.Accounting/Handlers/GoodsReceivedAccountingHandler.vb` | `GoodsReceivedAccountingHandler` | `GoodsReceivedEvent` | Records accounts payable "Purchase" expenses when goods enter the system. |
| `src/MerchSys.Accounting/Handlers/CreditPaymentAccountingHandler.vb` | `CreditPaymentAccountingHandler` | `CreditPaymentEvent` | Records "AR Reduction" expenses (offsets) when customers pay down credit balances. |
| `src/MerchSys.Accounting/Handlers/GoodsReceivedWithVatHandler.vb` | `GoodsReceivedWithVatHandler` | `GoodsReceivedWithVatEvent` | Records VAT details from purchasing/expenses into the `Acc_VatReturnLines` table, categorized by Goods/Services/Capital Goods. |
| `src/MerchSys.Accounting/Handlers/ShrinkageAccountingHandler.vb` | `ShrinkageAccountingHandler` | `ShrinkageRecordedEvent` | Records "Shrinkage" expenses for inventory write-offs or theft loss. |
| `src/MerchSys.Accounting/Handlers/ReceiptTamperDetectedHandler.vb` | `ReceiptTamperDetectedHandler` | `ReceiptTamperDetectedEvent` | Consumes tamper events from POS; persists to `Acc_TamperAuditLog` with machine/user context. Rethrows on save failure to ensure bus-level retry or DLQ logic. |

> [!NOTE]
> As of **INFRA-27**, the offline-first SQLite sync layer was decommissioned. Handlers now perform direct transactional writes to the central MariaDB using `AccountingDbContext.SaveChangesAsync()`, ensuring ACID compliance and eliminating the `Sync_Journal` overhead.
