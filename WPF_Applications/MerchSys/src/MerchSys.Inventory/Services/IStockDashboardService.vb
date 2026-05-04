Namespace Services

    Public Interface IStockDashboardService
        Function GetDashboardDataAsync() As Task(Of StockDashboardDto)
        Function GetProductDetailAsync(productId As Integer) As Task(Of ProductDetailDto)
    End Interface

    Public Class StockDashboardDto
        Public Property TotalProducts As Integer
        ''' <summary>Total inventory value using FIFO batch costing (Σ QtyRemaining × UnitCost).</summary>
        Public Property TotalStockValue As Decimal
        ''' <summary>Number of active products at or below their minimum threshold (includes Out of Stock).</summary>
        Public Property LowStockCount As Integer
        ''' <summary>Number of batches whose expiry date falls within the alert window.</summary>
        Public Property NearExpiryCount As Integer
        ''' <summary>Number of batches that have passed their expiry date and still have remaining qty.</summary>
        Public Property ExpiredCount As Integer
        Public Property Products As List(Of ProductSummaryDto) = New List(Of ProductSummaryDto)()
    End Class

    Public Class ProductSummaryDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Category As String
        Public Property CurrentStock As Integer
        Public Property RetailPrice As Decimal
        ''' <summary>FIFO valuation: Σ QtyRemaining × UnitCost across all active batches.</summary>
        Public Property StockValue As Decimal
        Public Property Unit As String
        Public Property MinThreshold As Integer
        ''' <summary>"Out" if CurrentStock = 0, "Low" if at or below threshold, "Normal" otherwise.</summary>
        Public Property StockStatus As String
        ''' <summary>"HasExpired" if any live batch is past expiry, "NearExpiry" if within alert window, "OK" otherwise.</summary>
        Public Property ExpiryStatus As String
        Public Property HasExpiry As Boolean
    End Class

    Public Class ProductDetailDto
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Category As String
        Public Property CurrentStock As Integer
        Public Property RetailPrice As Decimal
        Public Property StockValue As Decimal
        Public Property Unit As String
        Public Property HasExpiry As Boolean
        ''' <summary>All batches ordered by ReceiptDate ascending (oldest first, matching FIFO deduction order).</summary>
        Public Property StockBatches As List(Of StockBatchSummaryDto) = New List(Of StockBatchSummaryDto)()
        Public Property ShrinkageHistory As List(Of ShrinkageSummaryDto) = New List(Of ShrinkageSummaryDto)()
        ''' <summary>Inflow (receipt) and shrinkage events from the last 30 days, merged and sorted by date descending.</summary>
        Public Property RecentMovements As List(Of StockMovementDto) = New List(Of StockMovementDto)()
    End Class

    Public Class StockBatchSummaryDto
        Public Property BatchId As Integer
        Public Property ReceiptDate As DateTime
        Public Property QuantityReceived As Integer
        Public Property QuantityRemaining As Integer
        Public Property UnitCost As Decimal
        Public Property ExpiryDate As DateTime?
        Public Property IsExpired As Boolean
        Public Property SourcePurchaseOrderId As Integer?
    End Class

    Public Class ShrinkageSummaryDto
        Public Property RecordId As Integer
        Public Property RecordedDate As DateTime
        Public Property QuantityLost As Integer
        Public Property UnitCost As Decimal
        Public Property TotalValue As Decimal
        Public Property Reason As String
        Public Property Notes As String
    End Class

    Public Class StockMovementDto
        ''' <summary>"Inflow" for stock receipts, "Shrinkage" for loss events.</summary>
        Public Property MovementType As String
        Public Property MovementDate As DateTime
        Public Property Quantity As Integer
        Public Property UnitCost As Decimal
        Public Property Notes As String
    End Class

End Namespace
