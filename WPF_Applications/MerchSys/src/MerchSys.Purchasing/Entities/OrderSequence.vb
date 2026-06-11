Namespace Entities

    ''' <summary>
    ''' Per-key sequence table for Purchase Order and Goods Receipt numbers.
    ''' SeqKey can be e.g. "PO-2026" or "GR-2026"; <see cref="NextValue"/> holds the next number to assign.
    ''' Rows are created lazily on the first creation/receipt of a new year.
    ''' <para>
    ''' Concurrent access is serialized via optimistic concurrency on <see cref="RowVersion"/>;
    ''' the service retries on <c>DbUpdateConcurrencyException</c>.
    ''' </para>
    ''' </summary>
    Public Class OrderSequence
        Implements MerchSys.SharedKernel.Interfaces.IAuditable

        ''' <summary>Unique key for this sequence (e.g. "PO-2026", "GR-2026"). Primary Key.</summary>
        Public Property SeqKey As String

        ''' <summary>
        ''' The next sequence number to issue. Starts at 1 and is monotonic. Values may be skipped
        ''' if the PO/GR creation fails after the number is committed in its own transaction.
        ''' </summary>
        Public Property NextValue As Integer

        Public Property CreatedBy As String Implements MerchSys.SharedKernel.Interfaces.IAuditable.CreatedBy
        Public Property CreatedAt As DateTime Implements MerchSys.SharedKernel.Interfaces.IAuditable.CreatedAt
        Public Property ModifiedBy As String Implements MerchSys.SharedKernel.Interfaces.IAuditable.ModifiedBy
        Public Property ModifiedAt As DateTime? Implements MerchSys.SharedKernel.Interfaces.IAuditable.ModifiedAt

        Public Property RowVersion As DateTime

    End Class

End Namespace
