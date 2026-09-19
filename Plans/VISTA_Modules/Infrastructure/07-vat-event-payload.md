---
module: Infrastructure
plan-id: INFRA-07
title: "Cross-Module VAT Event Payload Contracts"
depends-on: [INFRA-04]
estimated-files: 4
---

# Cross-Module VAT Event Payload Contracts

## Context

The Feature Gap Audit (2026-05-10) flagged VAT-readiness as missing across POS and Accounting. Adding VAT to the existing `SaleCompletedEvent` and `GoodsReceivedEvent` (defined in INFRA-04) would silently break any consumer that doesn't yet understand the new fields. Instead, this plan introduces sibling event contracts that carry the BIR three-bucket structure (vatable, exempt, zero-rated) plus output/input VAT. POS-14 publishes the new sale event; Purchasing publishes the new receipt event in a follow-up; ACC-10/ACC-11 consume both. The original INFRA-04 events remain unchanged for non-VAT-aware consumers.

A third event — `ReceiptTamperDetectedEvent` — is also defined here so it lives in `SharedKernel` alongside the other inter-module contracts (POS-13 publishes it; Accounting consumes for audit logging).

## Prerequisites

- **INFRA-04** (MediatR Event Bus) — `INotification` registration and assembly scanning are already in place

## Wiki References

- `concepts/vat-ready.md` — three-bucket model, output/input VAT
- `concepts/bir-compliance.md` — receipt immutability
- `analysis/cross-module-data-flow.md` — current event catalog

## Deliverables

```
MerchSys.SharedKernel/Events/
├── SaleCompletedWithVatEvent.vb
├── GoodsReceivedWithVatEvent.vb
└── ReceiptTamperDetectedEvent.vb

MerchSys.SharedKernel/Enums/
└── VatTreatment.vb
```

## Specification

### VatTreatment (enum)
```
Public Enum VatTreatment
    Vatable = 0      ' Subject to 12% (or configured rate)
    Exempt = 1       ' BIR-exempt (e.g., agricultural inputs in scope)
    ZeroRated = 2    ' 0%-rated transactions (e.g., export sales)
End Enum
```

### SaleCompletedWithVatEvent
```
Public Class SaleCompletedWithVatEvent
    Implements INotification

    ' Mirrors SaleCompletedEvent core fields
    Public Property TransactionId As Integer
    Public Property TransactionDate As DateTime
    Public Property PaymentMethod As PaymentMethod
    Public Property TotalAmount As Decimal
    Public Property CustomerId As Integer?
    Public Property Items As List(Of SaleItemWithVat)

    ' VAT three-bucket totals (transaction-level)
    Public Property VatableSales As Decimal
    Public Property VatExemptSales As Decimal
    Public Property ZeroRatedSales As Decimal
    Public Property OutputVat As Decimal           ' Sum of line OutputVat
    Public Property IsVatRegistered As Boolean     ' Snapshot of config at time of sale

    Public Class SaleItemWithVat
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property DiscountAmount As Decimal
        Public Property Treatment As VatTreatment
        Public Property VatableAmount As Decimal
        Public Property VatExemptAmount As Decimal
        Public Property ZeroRatedAmount As Decimal
        Public Property OutputVat As Decimal
    End Class
End Class
```

### GoodsReceivedWithVatEvent
```
Public Class GoodsReceivedWithVatEvent
    Implements INotification

    ' Mirrors GoodsReceivedEvent core fields
    Public Property PurchaseOrderId As Integer
    Public Property ReceivedDate As DateTime
    Public Property Items As List(Of GoodsReceivedItemWithVat)

    ' VAT totals (header-level)
    Public Property VatableInput As Decimal
    Public Property VatExemptInput As Decimal
    Public Property ZeroRatedInput As Decimal
    Public Property InputVat As Decimal

    Public Class GoodsReceivedItemWithVat
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QuantityReceived As Integer
        Public Property UnitCost As Decimal
        Public Property ExpiryDate As DateTime?
        Public Property Treatment As VatTreatment
        Public Property InputVat As Decimal
    End Class
End Class
```

### ReceiptTamperDetectedEvent
```
Public Class ReceiptTamperDetectedEvent
    Implements INotification

    Public Property ReceiptId As Integer
    Public Property ReceiptNumber As String         ' OR-YYYY-XXXX
    Public Property ExpectedHash As String
    Public Property ActualHash As String
    Public Property DetectedAt As DateTime
    Public Property DetectedBy As String            ' Service name that detected
End Class
```

### Coexistence rule
- POS-14 publishes **both** `SaleCompletedEvent` (legacy) and `SaleCompletedWithVatEvent` for the same transaction during the migration window
- Once ACC-10/ACC-11 are deployed, follow-up plans can deprecate the legacy publish; that decision is out of scope here
- Handlers must be idempotent against double-publish if both events update the same ledger row — use `TransactionId` as a natural key

## Implementation Notes

- All three events implement `INotification` (one-to-many)
- All `Decimal` monetary properties are precision(18,2)
- `IsVatRegistered` snapshot on the sale event is mandatory — historical receipts must reflect the VAT mode at issuance, not the current config
- No DI changes needed; the existing assembly scan in `MediatRConfig` (INFRA-04) picks up the new events automatically

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings
2. All three events implement `INotification`
3. Existing INFRA-04 events are not modified
4. `MediatRConfig.AddMediatRServices` resolves the new events without code changes (assembly scan)
5. XML doc comments on every event/property: source, consumer(s), payload meaning, BIR rationale
6. `VatTreatment` enum referenced from at least the two new event classes

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/Infrastructure/INFRA-07-summary.md` using `Progress/_template.md`.

### Documentation
- XML doc comments specifying publishers (POS-14 for sales, Purchasing for receipts, POS-13 for tamper) and consumers (ACC-10, ACC-11, Accounting audit log handler)
