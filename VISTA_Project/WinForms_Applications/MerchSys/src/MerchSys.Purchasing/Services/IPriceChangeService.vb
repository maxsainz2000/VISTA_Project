Imports MerchSys.Purchasing.Entities

Namespace Services

    Public Interface IPriceChangeService

        ''' <summary>
        ''' Compares each goods receipt line against its originating PO line.
        ''' Creates a <see cref="PriceChangeAlert"/> for every line where the costs differ.
        ''' Returns the list of alerts that were created (empty list when all costs match).
        ''' </summary>
        Function DetectChangesAsync(goodsReceiptId As Integer) As Task(Of List(Of PriceChangeAlert))

        ''' <summary>Returns all alerts where IsAcknowledged is False, ordered by CreatedAt descending.</summary>
        Function GetUnacknowledgedAsync() As Task(Of List(Of PriceChangeAlert))

        ''' <summary>
        ''' Marks the alert as acknowledged and records the UTC timestamp.
        ''' Throws <see cref="InvalidOperationException"/> if the alert is not found.
        ''' </summary>
        Function AcknowledgeAsync(alertId As Integer) As Task

        ''' <summary>Returns all price change alerts for the given product, ordered by CreatedAt descending.</summary>
        Function GetHistoryForProductAsync(productId As Integer) As Task(Of List(Of PriceChangeAlert))

    End Interface

End Namespace
