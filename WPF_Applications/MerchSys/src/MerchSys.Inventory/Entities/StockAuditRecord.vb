Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Records a stock count event: the expected system quantity versus the physical count,
    ''' and the variance (positive = surplus, negative = shortage) applied as an adjustment.
    ''' </summary>
    Public Class StockAuditRecord
        Inherits AuditableEntity

        ''' <summary>Foreign key to the audited <see cref="Product"/>.</summary>
        Public Property ProductId As Integer

        ''' <summary>System-calculated stock level (sum of batch QuantityRemaining) at audit time.</summary>
        Public Property ExpectedQuantity As Integer

        ''' <summary>Physical count recorded by staff.</summary>
        Public Property PhysicalCount As Integer

        ''' <summary>PhysicalCount − ExpectedQuantity. Positive = surplus; negative = shortage.</summary>
        Public Property Variance As Integer

        ''' <summary>Human-readable reason or context for the audit or adjustment.</summary>
        Public Property Reason As String

        ''' <summary>Optional additional notes recorded by staff.</summary>
        Public Property Notes As String

        ''' <summary>Username of the person who performed the count.</summary>
        Public Property PerformedBy As String

        ''' <summary>UTC timestamp when the audit was performed.</summary>
        Public Property AuditedAt As DateTime

        ''' <summary>Navigation property to the audited product.</summary>
        Public Overridable Property Product As Product

    End Class

End Namespace
