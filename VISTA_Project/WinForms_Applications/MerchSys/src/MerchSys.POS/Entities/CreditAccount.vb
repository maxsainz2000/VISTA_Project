Namespace Entities

    ''' <summary>
    ''' Represents an informal customer credit (utang) account.
    ''' A customer with any outstanding balance is hard-blocked from making new credit
    ''' purchases — this rule is non-negotiable and enforced at the service layer.
    ''' <see cref="IsBlocked"/> reflects CurrentBalance &gt; 0 and must be maintained
    ''' consistently whenever the balance changes.
    ''' </summary>
    Public Class CreditAccount
        Inherits MerchSys.SharedKernel.Entities.SoftDeletableEntity

        ''' <summary>Full name of the credit customer.</summary>
        Public Property CustomerName As String

        ''' <summary>Contact phone number for the customer.</summary>
        Public Property Phone As String

        ''' <summary>Customer's address; optional.</summary>
        Public Property Address As String

        ''' <summary>
        ''' Outstanding balance currently owed by this customer.
        ''' When this value is greater than zero, <see cref="IsBlocked"/> must be True.
        ''' </summary>
        Public Property CurrentBalance As Decimal

        ''' <summary>Cumulative credit extended to this customer across all transactions.</summary>
        Public Property TotalCreditExtended As Decimal

        ''' <summary>Cumulative payments received from this customer.</summary>
        Public Property TotalPaymentsReceived As Decimal

        ''' <summary>
        ''' Hard-blocking flag. True whenever CurrentBalance &gt; 0.
        ''' A blocked customer cannot make new credit purchases — this is non-negotiable.
        ''' </summary>
        Public Property IsBlocked As Boolean

        ''' <summary>Date of the most recent transaction or payment on this account.</summary>
        Public Property LastTransactionDate As DateTime?

        ''' <summary>Free-text notes about this customer or account.</summary>
        Public Property Notes As String

        ' --- Navigation ---

        ''' <summary>All sales transactions charged to this credit account.</summary>
        Public Property Transactions As ICollection(Of SalesTransaction) = New List(Of SalesTransaction)()

        ''' <summary>All payments applied against this credit account.</summary>
        Public Property Payments As ICollection(Of CreditPayment) = New List(Of CreditPayment)()

    End Class

End Namespace
