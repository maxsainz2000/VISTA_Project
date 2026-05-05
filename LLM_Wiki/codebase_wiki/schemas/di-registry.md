---
type: schema-map
last-updated: 2026-05-05
---

# Dependency Injection Registry

This page documents the composition root in `MerchSys.App`.

## POS Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `ICartService` | `CartService` | Scoped |
| `IPaymentService` | `PaymentService` | Scoped |
| `ICreditService` | `CreditService` | Scoped |
| `IReceiptService` | `ReceiptService` | Scoped |
| `ISalesReturnService` | `SalesReturnService` | Scoped |
| `IDailySummaryService` | `DailySummaryService` | Scoped |

## Inventory Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IStockService` | `StockService` | Scoped |
| `IExpiryTrackingService` | `ExpiryTrackingService` | Scoped |
| `IStockDashboardService` | `StockDashboardService` | Scoped |
| `ILowStockAlertService` | `LowStockAlertService` | Scoped |
| `IInventoryAuditService` | `InventoryAuditService` | Scoped |
| `IShrinkageService` | `ShrinkageService` | Scoped |

## Purchasing Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IPurchaseOrderService` | `PurchaseOrderService` | Scoped |
| `IPriceChangeService` | `PriceChangeService` | Scoped |
| `IGoodsReceivingService` | `GoodsReceivingService` | Scoped |

> **Note:** `IVendorService`, `IAccountsPayableService`, and `IReorderService` exist in `src/MerchSys.Purchasing/Services/` but are **not** registered in `PurchasingServiceCollectionExtensions.vb`. They are instantiated directly where used (or will be registered in a future plan).

## Shared Services
| Interface | Implementation | Lifetime |
|---|---|---|
| `IEventBus` | `MediatR` | Transient (by MediatR default) |
