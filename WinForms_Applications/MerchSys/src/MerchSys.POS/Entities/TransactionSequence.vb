Namespace Entities

    ''' <summary>
    ''' Per-year sequence table for Sales Transaction numbers.
    ''' One row per calendar year; <see cref="NextValue"/> holds the next TX number to assign.
    ''' Rows are created lazily on the first checkout of a new year.
    ''' <para>
    ''' Concurrent access is serialized via optimistic concurrency on <see cref="RowVersion"/>;
    ''' the service retries on <c>DbUpdateConcurrencyException</c>.
    ''' </para>
    ''' </summary>
    Public Class TransactionSequence
        Implements MerchSys.SharedKernel.Interfaces.IAuditable

        ''' <summary>Calendar year this sequence row governs. Primary Key.</summary>
        Public Property Year As Integer

        ''' <summary>
        ''' The next sequence number to issue. Starts at 1 and is monotonic. Values may be skipped
        ''' if a checkout fails after the number is committed in its own transaction — the BIR
        ''' gap-free guarantee applies to the OfficialReceipt number, not this transaction number.
        ''' </summary>
        Public Property NextValue As Integer

        Public Property CreatedBy As String Implements MerchSys.SharedKernel.Interfaces.IAuditable.CreatedBy
        Public Property CreatedAt As DateTime Implements MerchSys.SharedKernel.Interfaces.IAuditable.CreatedAt
        Public Property ModifiedBy As String Implements MerchSys.SharedKernel.Interfaces.IAuditable.ModifiedBy
        Public Property ModifiedAt As DateTime? Implements MerchSys.SharedKernel.Interfaces.IAuditable.ModifiedAt

        Public Property RowVersion As DateTime

    End Class

End Namespace
