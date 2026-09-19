---
type: layer-manifest
module: MerchSys.SharedKernel
layer: Events & Queries
last-updated: 2026-05-27
---

# MerchSys.SharedKernel — Events & Queries

## MediatR Events
| File Path | Event Class | Inherits | Key Properties | Published By |
|---|---|---|---|---|
| `Events/CreditPaymentEvent.vb` | `CreditPaymentEvent` | `INotification` | `CustomerId`, `PaymentAmount`, `PaymentDate`, `PaymentMethod` | POS |
| `Events/GoodsReceivedEvent.vb` | `GoodsReceivedEvent` | `INotification` | `PurchaseOrderId`, `ReceivedDate`, `Items` | Purchasing |
| `Events/SaleCompletedEvent.vb` | `SaleCompletedEvent` | `INotification` | `TransactionId`, `TransactionDate`, `PaymentMethod`, `TotalAmount`, `CustomerId`, `Items` | POS |
| `Events/ShrinkageRecordedEvent.vb`| `ShrinkageRecordedEvent`| `INotification` | `ProductId`, `ProductName`, `QuantityLost`, `UnitCost`, `TotalValue`, `Reason`, `RecordedDate` | Inventory |
| `Events/StockReturnedEvent.vb` | `StockReturnedEvent` | `INotification` | `ReturnId`, `OriginalTransactionId`, `ReturnDate`, `ProductId`, `ProductName`, `QuantityReturned`, `UnitPrice` | POS |
| `Events/SaleCompletedWithVatEvent.vb` | `SaleCompletedWithVatEvent` | `INotification` | `TransactionId`, `VatableSales`, `VatExemptSales`, `ZeroRatedSales`, `OutputVat`, `Items` | POS-14 |
| `Events/GoodsReceivedWithVatEvent.vb` | `GoodsReceivedWithVatEvent` | `INotification` | `PurchaseOrderId`, `VatableInput`, `VatExemptInput`, `ZeroRatedInput`, `InputVat`, `Items` | Purchasing |
| `Events/ReceiptTamperDetectedEvent.vb` | `ReceiptTamperDetectedEvent` | `INotification` | `ReceiptId`, `ReceiptNumber`, `ExpectedHash`, `ActualHash`, `DetectedAt` | POS-13 |
| `Events/VatConfigurationChangedEvent.vb` | `VatConfigurationChangedEvent` | `INotification` | `OccurredAt`, `IsVatRegistered`, `PreviousIsVatRegistered` | POS-17 |

## MediatR Queries
| File Path | Query Class | Result Type | Properties | Handled By |
|---|---|---|---|---|
| `Queries/GetCurrentStockQuery.vb` | `GetCurrentStockQuery` | `GetCurrentStockResult` | `ProductId` | Inventory |
| `Queries/GetInventoryValuationQuery.vb`| `GetInventoryValuationQuery` | `GetInventoryValuationResult`| `AsOfDate` | Inventory |
| `Queries/GetProductCatalogQuery.vb`| `GetProductCatalogQuery`| `GetProductCatalogResult`| `SearchTerm`, `ProductId` | Inventory |
| `Queries/GetProductCostQuery.vb` | `GetProductCostQuery` | `GetProductCostResult` | `ProductId` | Inventory |
| `Queries/GetSaleCogsBreakdownQuery.vb` | `GetSaleCogsBreakdownQuery` | `GetSaleCogsBreakdownResult` | `TransactionId`, `ProductId` | Inventory |
| `Queries/GetTotalARQuery.vb` | `GetTotalARQuery` | `Decimal` | N/A | POS |
| `Queries/GetTotalAPQuery.vb` | `GetTotalAPQuery` | `Decimal` | N/A | Purchasing |
| `Queries/GetLowStockAlertCountQuery.vb` | `GetLowStockAlertCountQuery` | `Integer` | N/A | Inventory |
| `Queries/GetVatConfigurationQuery.vb` | `GetVatConfigurationQuery` | `GetVatConfigurationResult` | N/A | POS |
| `Queries/GetProductsForCatalogQuery.vb` | `GetProductsForCatalogQuery` | `IReadOnlyList(Of ProductLookupDto)` | `SearchTerm` | Inventory |
| `Queries/GetOverdueAPCountQuery.vb` | `GetOverdueAPCountQuery` | `Integer` | N/A | Purchasing |
| `Queries/GetOverdueARCountQuery.vb` | `GetOverdueARCountQuery` | `Integer` | N/A | POS |


## Query Results
| File Path | Result Class |
|---|---|
| `Queries/GetCurrentStockResult.vb` | `GetCurrentStockResult` |
| `Queries/GetInventoryValuationResult.vb` | `GetInventoryValuationResult` |
| `Queries/GetProductCatalogResult.vb` | `GetProductCatalogResult` |
| `Queries/GetProductCostResult.vb` | `GetProductCostResult` |
| `Queries/GetSaleCogsBreakdownQuery.vb` | `GetSaleCogsBreakdownResult`<br>`SaleCogsLine` |
| `Queries/GetVatConfigurationResult.vb` | `GetVatConfigurationResult` |
| `Queries/GetProductsForCatalogQuery.vb` | `ProductLookupDto` |
