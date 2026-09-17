Namespace Entities

    ''' <summary>
    ''' Base class for entities that require optimistic concurrency tracking via a RowVersion TIMESTAMP(6) column.
    ''' </summary>
    Public MustInherit Class ConcurrencyAwareEntity
        Inherits BaseEntity

        ''' <summary>
        ''' The optimistic concurrency token mapped to a TIMESTAMP(6) column.
        ''' </summary>
        Public Property RowVersion As DateTime

    End Class

End Namespace
