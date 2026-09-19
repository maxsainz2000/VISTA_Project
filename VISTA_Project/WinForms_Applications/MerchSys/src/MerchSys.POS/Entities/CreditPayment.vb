Imports MerchSys.SharedKernel.Enums

Namespace Entities

    ''' <summary>
    ''' Records a payment applied against a customer's <see cref="CreditAccount"/>.
    ''' The <see cref="PaymentMethod"/> on a credit payment must not be <see cref="PaymentMethod.Credit"/>
    ''' — credit debts cannot be settled with more credit.
    ''' </summary>
    Public Class CreditPayment
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Foreign key to the <see cref="CreditAccount"/> being paid down.</summary>
        Public Property CreditAccountId As Integer

        ''' <summary>Amount paid in this single payment.</summary>
        Public Property PaymentAmount As Decimal

        ''' <summary>Date and time the payment was received.</summary>
        Public Property PaymentDate As DateTime

        ''' <summary>
        ''' Method used to make this payment.
        ''' Must be Cash, GCash, or BankTransfer — never Credit.
        ''' </summary>
        Public Property PaymentMethod As PaymentMethod

        ''' <summary>Optional notes about this payment (e.g. reference numbers).</summary>
        Public Property Notes As String

        ''' <summary>Username of the staff member who recorded this payment.</summary>
        Public Property ReceivedBy As String

        ' --- Navigation ---

        ''' <summary>The credit account against which this payment was applied.</summary>
        Public Property CreditAccount As CreditAccount

    End Class

End Namespace
