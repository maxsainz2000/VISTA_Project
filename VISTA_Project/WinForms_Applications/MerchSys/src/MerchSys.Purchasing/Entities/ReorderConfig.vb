Namespace Entities

    ''' <summary>
    ''' Per-product configuration that drives the reorder suggestion engine.
    ''' Stores the reorder threshold, safety buffer, preferred vendor, and seasonal multiplier
    ''' used when evaluating whether a stock top-up suggestion should be raised.
    ''' </summary>
    Public Class ReorderConfig
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Cross-module reference to the Inventory product.</summary>
        Public Property ProductId As Integer

        ''' <summary>Denormalised product name for display without a cross-module join.</summary>
        Public Property ProductName As String

        ''' <summary>Optional FK to the preferred <see cref="Vendor"/> for this product.</summary>
        Public Property PreferredVendorId As Integer?

        ''' <summary>
        ''' Reorder point — the stock level at or below which a suggestion is generated.
        ''' Conceptually: (Avg Daily Demand × LeadTimeDays) + SafetyStock, set manually or derived offline.
        ''' </summary>
        Public Property MinimumThreshold As Integer

        ''' <summary>Buffer quantity added to absorb demand variability.</summary>
        Public Property SafetyStock As Integer

        ''' <summary>Default quantity to suggest on the generated purchase order.</summary>
        Public Property DefaultOrderQuantity As Integer

        ''' <summary>Expected days between PO placement and goods receipt for this product.</summary>
        Public Property LeadTimeDays As Integer

        ''' <summary>True when this product experiences predictable demand peaks (e.g. planting season).</summary>
        Public Property IsSeasonalItem As Boolean

        ''' <summary>
        ''' Factor applied to MinimumThreshold when IsSeasonalItem is True.
        ''' 1.0 = no change; 1.5 = raise threshold by 50 %.
        ''' </summary>
        Public Property SeasonalMultiplier As Decimal

        ''' <summary>False disables monitoring without deleting the configuration.</summary>
        Public Property IsActive As Boolean

        ' --- Navigation ---

        ''' <summary>The vendor preferred for reorder purchases, if configured.</summary>
        Public Property PreferredVendor As Vendor

    End Class

End Namespace
