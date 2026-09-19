Imports MediatR
Imports MerchSys.SharedKernel.Enums

Namespace Events

    ''' <summary>
    ''' Published by POS when a sales transaction is finalised and payment is confirmed.
    ''' Consumed by Inventory (to decrement stock via FIFO) and Accounting (to record revenue and COGS).
    ''' Fires after the cashier confirms the transaction and the receipt is generated.
    ''' </summary>
    Public Class SaleCompletedEvent
        Implements INotification

        ''' <summary>ID of the completed POS transaction.</summary>
        Public Property TransactionId As Integer

        ''' <summary>UTC date/time the transaction was finalised.</summary>
        Public Property TransactionDate As DateTime

        ''' <summary>Payment method used for this transaction.</summary>
        Public Property PaymentMethod As PaymentMethod

        ''' <summary>Total transaction amount including all line items after discounts.</summary>
        Public Property TotalAmount As Decimal

        ''' <summary>Optional customer ID. Nothing for anonymous cash sales.</summary>
        Public Property CustomerId As Integer?

        ''' <summary>Products sold in this transaction.</summary>
        Public Property Items As List(Of SaleItem)

        Public Sub New()
            Items = New List(Of SaleItem)()
        End Sub

        ''' <summary>A single product line within a completed sale.</summary>
        Public Class SaleItem

            ''' <summary>Product identifier (matches Inv_ tables).</summary>
            Public Property ProductId As Integer

            ''' <summary>Human-readable product name, denormalized for event consumers.</summary>
            Public Property ProductName As String

            ''' <summary>Number of units sold.</summary>
            Public Property Quantity As Integer

            ''' <summary>Selling price per unit before discount.</summary>
            Public Property UnitPrice As Decimal

            ''' <summary>Total discount applied to this line item.</summary>
            Public Property DiscountAmount As Decimal

        End Class

    End Class

End Namespace
