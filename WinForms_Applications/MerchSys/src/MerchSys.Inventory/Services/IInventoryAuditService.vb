Imports MerchSys.Inventory.Entities

Namespace Services

    Public Interface IInventoryAuditService
        ''' <summary>
        ''' Compares expected stock (sum of batch quantities) against a supplied physical count
        ''' for a product and records any variance as an adjustment.
        ''' </summary>
        Function PerformStockCountAsync(productId As Integer, physicalCount As Integer, performedBy As String, notes As String) As Task(Of StockAuditRecord)

        ''' <summary>Records a manual stock count adjustment without a prior comparison.</summary>
        Function RecordAdjustmentAsync(productId As Integer, adjustedQuantity As Integer, reason As String, performedBy As String) As Task(Of StockAuditRecord)

        ''' <summary>Returns audit records for a product or over a date range. Both filters are optional.</summary>
        Function GetAuditHistoryAsync(Optional productId As Integer? = Nothing, Optional startDate As DateTime? = Nothing, Optional endDate As DateTime? = Nothing) As Task(Of List(Of StockAuditRecord))

        ''' <summary>Returns the most recent audit record for each product.</summary>
        Function GetLatestAuditPerProductAsync() As Task(Of List(Of StockAuditRecord))
    End Interface

End Namespace
