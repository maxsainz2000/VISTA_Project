Imports MerchSys.Inventory.Entities
Imports MerchSys.SharedKernel.Paging

Namespace Services

    Public Interface IShrinkageService
        Function RecordShrinkageAsync(productId As Integer, quantity As Integer, reason As String, notes As String, Optional batchId As Integer? = Nothing) As Task(Of ShrinkageRecord)
        Function GetShrinkageHistoryAsync(Optional productId As Integer? = Nothing) As Task(Of List(Of ShrinkageRecord))

        ''' <summary>
        ''' Keyset-paged shrinkage history (INFRA-34), newest first, ordered by (RecordedDate, Id).
        ''' Pass the prior page's NextCursor back in <paramref name="request"/> to load more.
        ''' </summary>
        Function GetShrinkageHistoryPageAsync(request As PageRequest, Optional productId As Integer? = Nothing) As Task(Of PagedResult(Of ShrinkageRecord))
        Function GetTotalShrinkageValueAsync(startDate As DateTime, endDate As DateTime) As Task(Of Decimal)
    End Interface

End Namespace
