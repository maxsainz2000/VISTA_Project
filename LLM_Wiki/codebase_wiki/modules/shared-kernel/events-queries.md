---
type: layer-manifest
module: MerchSys.SharedKernel
layer: Events & Queries
last-updated: 2026-05-03
---

# MerchSys.SharedKernel — Events & Queries

## MediatR Events
| File Path | Event Class | Inherits | Key Properties | Published By |
|---|---|---|---|---|
| `Events/CreditPaymentEvent.vb` | `CreditPaymentEvent` | `INotification` | `PaymentId`, `CustomerId`, `Amount` | POS |
| `Events/GoodsReceivedEvent.vb` | `GoodsReceivedEvent` | `INotification` | `PurchaseOrderId`, `ReceivedDate` | Purchasing |
| `Events/SaleCompletedEvent.vb` | `SaleCompletedEvent` | `INotification` | `TransactionId`, `TotalAmount` | POS |
| `Events/ShrinkageRecordedEvent.vb`| `ShrinkageRecordedEvent`| `INotification` | `InventoryItemId`, `QuantityLoss` | Inventory |
| `Events/StockReturnedEvent.vb` | `StockReturnedEvent` | `INotification` | `TransactionId`, `ReturnedItems` | POS |

## MediatR Queries
| File Path | Query Class | Result Type | Properties | Handled By |
|---|---|---|---|---|
| `Queries/GetCurrentStockQuery.vb` | `GetCurrentStockQuery` | `GetCurrentStockResult` | `ProductSku` | Inventory |
| `Queries/GetInventoryValuationQuery.vb`| `GetInventoryValuationQuery` | `GetInventoryValuationResult`| N/A | Inventory |
| `Queries/GetProductCatalogQuery.vb`| `GetProductCatalogQuery`| `GetProductCatalogResult`| N/A | Inventory |
