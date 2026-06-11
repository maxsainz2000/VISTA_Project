Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Interface ICreditService

        Function CreateAccountAsync(customerName As String, phone As String, Optional address As String = Nothing) As Task(Of CreditAccount)
        Function GetAccountAsync(id As Integer) As Task(Of CreditAccount)
        Function GetAllAccountsAsync() As Task(Of List(Of CreditAccount))
        Function SearchAccountsAsync(searchTerm As String) As Task(Of List(Of CreditAccount))

        ''' <summary>
        ''' Returns True only when the customer's CurrentBalance is exactly zero.
        ''' Any outstanding balance blocks new credit — non-negotiable.
        ''' </summary>
        Function CanExtendCreditAsync(customerId As Integer) As Task(Of Boolean)

        Function ChargeCreditAsync(customerId As Integer, amount As Decimal, transactionId As Integer) As Task
        Function RecordPaymentAsync(customerId As Integer, amount As Decimal, paymentMethod As PaymentMethod, receivedBy As String) As Task(Of CreditPayment)
        Function GetPaymentHistoryAsync(customerId As Integer) As Task(Of List(Of CreditPayment))
        Function GetTotalOutstandingAsync() As Task(Of Decimal)
        Function GetOverdueAccountsAsync() As Task(Of List(Of CreditAccount))
        Function GetCreditTransactionsAsync(accountId As Integer) As Task(Of List(Of CreditTransactionItem))

    End Interface

    Public Class CreditTransactionItem
        Public Property TransactionDate As DateTime
        Public Property TransactionNumber As String
        Public Property Amount As Decimal
    End Class

End Namespace
