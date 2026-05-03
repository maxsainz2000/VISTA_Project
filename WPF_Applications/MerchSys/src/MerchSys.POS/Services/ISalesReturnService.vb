Imports MerchSys.POS.Entities

Namespace Services

    Public Interface ISalesReturnService

        ''' <summary>
        ''' Validates and records a product return against an existing transaction.
        ''' Reason is required. Quantity must not exceed original quantity sold less any prior returns.
        ''' If <paramref name="shouldRestock"/> is True, a <c>StockReturnedEvent</c> is published.
        ''' If the original payment was Credit, the customer's outstanding balance is reduced.
        ''' </summary>
        Function ProcessReturnAsync(originalTransactionId As Integer, productId As Integer, quantity As Integer, reason As String, shouldRestock As Boolean) As Task(Of SalesReturn)

        ''' <summary>Returns all return records linked to a specific transaction.</summary>
        Function GetReturnsForTransactionAsync(transactionId As Integer) As Task(Of List(Of SalesReturn))

        ''' <summary>Returns all return records processed within the given date range (inclusive).</summary>
        Function GetReturnHistoryAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of SalesReturn))

    End Interface

End Namespace
