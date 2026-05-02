---
module: Infrastructure
plan-id: INFRA-04
title: "MediatR Event Bus"
depends-on: [INFRA-01, INFRA-02]
estimated-files: 10
---

# MediatR Event Bus

## Context

All inter-module communication in VISTA flows through MediatR. No module directly references another module's classes or DbContext. This plan defines the shared event and query contracts in SharedKernel, registers MediatR in DI, and establishes the patterns that all modules will follow for publishing and handling events.

## Prerequisites

- **INFRA-01** (Solution Scaffold) — MediatR NuGet package installed
- **INFRA-02** (Shared Kernel) — base entity types exist

## Wiki References

- `concepts/mediatr-mediator.md` — event catalog, rules, usage patterns
- `analysis/cross-module-data-flow.md` — event list, source/consumer mappings
- `concepts/modular-monolith.md` — "no direct class references across module boundaries"

## Deliverables

```
MerchSys.SharedKernel/
├── Events/
│   ├── GoodsReceivedEvent.vb           ← Purchasing → Inventory, Accounting
│   ├── SaleCompletedEvent.vb           ← POS → Inventory, Accounting
│   ├── CreditPaymentEvent.vb          ← POS → Accounting
│   └── ShrinkageRecordedEvent.vb      ← Inventory → Accounting
├── Queries/
│   ├── GetInventoryValuationQuery.vb   ← Accounting → Inventory
│   ├── GetInventoryValuationResult.vb  ← Response type
│   ├── GetCurrentStockQuery.vb         ← Purchasing (reorder) → Inventory
│   └── GetCurrentStockResult.vb        ← Response type
├── Interfaces/
│   └── IEventBus.vb                    ← Optional abstraction over IMediator

MerchSys.App/
└── Startup/
    └── MediatRConfig.vb                ← DI registration for MediatR
```

## Specification

### Event Contracts

All events implement `INotification` from MediatR (fire-and-forget, multiple consumers).

**GoodsReceivedEvent:**
```
Public Class GoodsReceivedEvent
    Implements INotification

    Public Property PurchaseOrderId As Integer
    Public Property ReceivedDate As DateTime
    Public Property Items As List(Of GoodsReceivedItem)

    Public Class GoodsReceivedItem
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QuantityReceived As Integer
        Public Property UnitCost As Decimal
        Public Property ExpiryDate As DateTime?       ' Nullable — not all products expire
    End Class
End Class
```

**SaleCompletedEvent:**
```
Public Class SaleCompletedEvent
    Implements INotification

    Public Property TransactionId As Integer
    Public Property TransactionDate As DateTime
    Public Property PaymentMethod As PaymentMethod     ' Enum from SharedKernel
    Public Property TotalAmount As Decimal
    Public Property CustomerId As Integer?             ' Nullable — cash sales have no customer
    Public Property Items As List(Of SaleItem)

    Public Class SaleItem
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property DiscountAmount As Decimal
    End Class
End Class
```

**CreditPaymentEvent:**
```
Public Class CreditPaymentEvent
    Implements INotification

    Public Property CustomerId As Integer
    Public Property PaymentAmount As Decimal
    Public Property PaymentDate As DateTime
    Public Property PaymentMethod As PaymentMethod    ' Cash, GCash, or BankTransfer
End Class
```

**ShrinkageRecordedEvent:**
```
Public Class ShrinkageRecordedEvent
    Implements INotification

    Public Property ProductId As Integer
    Public Property ProductName As String
    Public Property QuantityLost As Integer
    Public Property UnitCost As Decimal               ' FIFO batch cost of lost units
    Public Property TotalValue As Decimal              ' Qty × Unit Cost
    Public Property Reason As String                   ' "Damage", "Spoilage", "Expiry", "Admin Error"
    Public Property RecordedDate As DateTime
End Class
```

### Query Contracts

Queries use `IRequest(Of TResponse)` from MediatR (request/response, single handler).

**GetInventoryValuationQuery / Result:**
```
Public Class GetInventoryValuationQuery
    Implements IRequest(Of GetInventoryValuationResult)

    Public Property AsOfDate As DateTime?   ' Nullable — defaults to Now
End Class

Public Class GetInventoryValuationResult
    Public Property TotalValue As Decimal
    Public Property Items As List(Of ProductValuation)

    Public Class ProductValuation
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property TotalQuantity As Integer
        Public Property TotalValue As Decimal
    End Class
End Class
```

**GetCurrentStockQuery / Result:**
```
Public Class GetCurrentStockQuery
    Implements IRequest(Of GetCurrentStockResult)

    Public Property ProductId As Integer?    ' Nullable — null = all products
End Class

Public Class GetCurrentStockResult
    Public Property Items As List(Of StockLevel)

    Public Class StockLevel
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property CurrentQuantity As Integer
        Public Property MinimumThreshold As Integer
        Public Property IsBelowThreshold As Boolean
    End Class
End Class
```

### MediatR DI Registration (`MediatRConfig.vb`)

```
Public Module MediatRConfig
    <Extension>
    Public Sub AddMediatRServices(services As IServiceCollection)
        services.AddMediatR(Sub(cfg)
            cfg.RegisterServicesFromAssembly(GetType(GoodsReceivedEvent).Assembly)  ' SharedKernel
            cfg.RegisterServicesFromAssembly(GetType(PurchasingDbContext).Assembly)  ' Purchasing handlers
            cfg.RegisterServicesFromAssembly(GetType(InventoryDbContext).Assembly)   ' Inventory handlers
            cfg.RegisterServicesFromAssembly(GetType(POSDbContext).Assembly)         ' POS handlers
            cfg.RegisterServicesFromAssembly(GetType(AccountingDbContext).Assembly)  ' Accounting handlers
        End Sub)
    End Sub
End Module
```

## Implementation Notes

- **Events are notifications** (one-to-many) — `INotification`
- **Queries are requests** (one-to-one) — `IRequest(Of T)`
- All event/query contracts live in `SharedKernel` so both publisher and consumer can reference them without cross-module dependencies
- **Handlers** live in the consuming module's `Handlers/` folder (created in later plans)
- Event properties should be immutable in practice — use `ReadOnly` properties where VB.NET allows, or at minimum do not expose setters that consumers would call
- All `DateTime` values are UTC

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors
2. All 4 event classes implement `INotification`
3. All 2 query classes implement `IRequest(Of TResult)`
4. `MediatRConfig` registers all assemblies
5. No module project directly references another module project
6. XML doc comments on every event/query explaining source, consumer(s), and payload purpose

## Output Requirements

### Implementation Summary
After completing all code, create a progress report at:
```
Progress/VISTA_Modules/Infrastructure/INFRA-04-summary.md
```
Using the template structure from `Progress/_template.md`.

### Documentation
- XML doc comments on every event class: who publishes it, who consumes it, when it fires
- XML doc comments on every query class: who sends it, who handles it, what it returns
