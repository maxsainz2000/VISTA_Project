Imports MediatR
Imports MerchSys.SharedKernel.Enums

Namespace Events

    ''' <summary>
    ''' Published by POS when a customer makes a payment against their outstanding credit (utang) balance.
    ''' Consumed by Accounting to reduce the accounts-receivable balance for the customer.
    ''' Fires after the cashier records a manual credit payment — not triggered by cash sales.
    ''' </summary>
    Public Class CreditPaymentEvent
        Implements INotification

        ''' <summary>Customer making the credit repayment.</summary>
        Public Property CustomerId As Integer

        ''' <summary>Amount paid against the outstanding balance.</summary>
        Public Property PaymentAmount As Decimal

        ''' <summary>UTC date/time the payment was recorded.</summary>
        Public Property PaymentDate As DateTime

        ''' <summary>
        ''' Payment method used. Only Cash, GCash, or BankTransfer are valid for credit repayments
        ''' (Credit itself is not a valid repayment method).
        ''' </summary>
        Public Property PaymentMethod As PaymentMethod

    End Class

End Namespace
