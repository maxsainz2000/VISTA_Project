Namespace Entities

    ''' <summary>
    ''' A single reorder suggestion produced by the <see cref="MerchSys.Purchasing.Services.ReorderService"/>.
    ''' Represents a product whose current stock has fallen to or below its computed reorder point.
    ''' The manager reviews each suggestion and either accepts it (creating a draft PO) or dismisses it.
    ''' </summary>
    Public Class ReorderSuggestion
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Cross-module reference to the Inventory product.</summary>
        Public Property ProductId As Integer

        ''' <summary>Denormalised product name captured at suggestion generation time.</summary>
        Public Property ProductName As String

        ''' <summary>Stock on hand at the moment the suggestion was generated.</summary>
        Public Property CurrentStock As Integer

        ''' <summary>Effective reorder point at the time of generation (seasonal multiplier already applied if relevant).</summary>
        Public Property ReorderPoint As Integer

        ''' <summary>Recommended order quantity derived from <see cref="ReorderConfig.DefaultOrderQuantity"/>.</summary>
        Public Property SuggestedQuantity As Integer

        ''' <summary>Preferred vendor FK copied from the matching <see cref="ReorderConfig"/>; Nothing if none configured.</summary>
        Public Property PreferredVendorId As Integer?

        ''' <summary>Preferred vendor name denormalised at suggestion generation time.</summary>
        Public Property PreferredVendorName As String

        ''' <summary>Lead time copied from the matching <see cref="ReorderConfig"/> at generation time.</summary>
        Public Property EstimatedLeadTimeDays As Integer

        ''' <summary>True when the reorder point was multiplied by the seasonal factor.</summary>
        Public Property IsSeasonalAdjusted As Boolean

        ''' <summary>Workflow state: "Pending", "Accepted", or "Dismissed".</summary>
        Public Property Status As String

        ''' <summary>FK to the <see cref="PurchaseOrder"/> created when this suggestion was accepted; Nothing until accepted.</summary>
        Public Property ConvertedToPOId As Integer?

    End Class

End Namespace
