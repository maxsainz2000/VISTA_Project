Imports MerchSys.SharedKernel.Enums

Namespace Entities

    ''' <summary>
    ''' Represents a single point-of-sale transaction. Each sale — whether paid in cash,
    ''' e-wallet, bank transfer, or credit — produces exactly one <see cref="SalesTransaction"/>.
    ''' Transaction numbers follow the format TX-YYYY-XXXX (auto-sequential, no gaps).
    ''' </summary>
    Public Class SalesTransaction
        Inherits MerchSys.SharedKernel.Entities.SoftDeletableEntity

        ''' <summary>Sequential transaction number in TX-YYYY-XXXX format.</summary>
        Public Property TransactionNumber As String

        ''' <summary>Date and time the transaction was recorded.</summary>
        Public Property TransactionDate As DateTime

        ''' <summary>Foreign key to the customer; Nothing for anonymous cash sales.</summary>
        Public Property CustomerId As Integer?

        ''' <summary>Denormalized customer name captured at the time of sale.</summary>
        Public Property CustomerName As String

        ''' <summary>Payment method used for this transaction.</summary>
        Public Property PaymentMethod As PaymentMethod

        ''' <summary>Sum of all line totals before any transaction-level discount.</summary>
        Public Property SubTotal As Decimal

        ''' <summary>Total discount applied at the transaction level.</summary>
        Public Property DiscountAmount As Decimal

        ''' <summary>VAT amount (12 % if VAT-registered; 0 otherwise).</summary>
        Public Property VatAmount As Decimal

        ''' <summary>Final amount due: SubTotal − DiscountAmount + VatAmount.</summary>
        Public Property TotalAmount As Decimal

        ''' <summary>Cash amount handed over by the customer.</summary>
        Public Property AmountTendered As Decimal

        ''' <summary>Change returned to the customer: AmountTendered − TotalAmount.</summary>
        Public Property ChangeAmount As Decimal

        ''' <summary>True when this transaction has been voided by a manager.</summary>
        Public Property IsVoided As Boolean

        ''' <summary>Required explanation when <see cref="IsVoided"/> is True.</summary>
        Public Property VoidReason As String

        ' --- Navigation ---

        ''' <summary>The individual product lines that make up this transaction.</summary>
        Public Property Lines As ICollection(Of SalesTransactionLine) = New List(Of SalesTransactionLine)()

        ''' <summary>The BIR-compliant official receipt issued for this transaction.</summary>
        Public Property Receipt As OfficialReceipt

        ''' <summary>The credit account charged when <see cref="PaymentMethod"/> is Credit.</summary>
        Public Property CreditAccount As CreditAccount

    End Class

End Namespace
