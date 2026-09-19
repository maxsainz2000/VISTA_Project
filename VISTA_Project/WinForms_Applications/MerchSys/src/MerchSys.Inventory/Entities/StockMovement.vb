Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Logs an individual stock change event (sale, receipt, shrinkage, or return) with a timestamp.
    ''' Enables time-windowed velocity queries as an improvement over lifetime-batch approximations.
    ''' </summary>
    Public Class StockMovement
        Inherits AuditableEntity

        Public Property ProductId As Integer
        Public Property MovementType As MovementType
        ''' <summary>Units changed. Positive for inbound (Receipt, Return), negative for outbound (Sale, Shrinkage).</summary>
        Public Property Quantity As Integer
        Public Property OccurredAt As DateTime

        Public Overridable Property Product As Product

    End Class

End Namespace
