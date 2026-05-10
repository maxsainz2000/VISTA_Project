Namespace Entities

    ''' <summary>
    ''' Per-year sequence table for BIR Official Receipt numbers.
    ''' One row per calendar year; <see cref="NextValue"/> holds the next OR number to assign.
    ''' Rows are created lazily on the first issuance of a new year.
    ''' <para>
    ''' Concurrent access is serialized via optimistic concurrency on <see cref="RowVersion"/>;
    ''' the service retries up to 10 times on <c>DbUpdateConcurrencyException</c>.
    ''' No in-memory counter or <c>MAX(ReceiptNumber)+1</c> pattern is used (NIRC §113).
    ''' </para>
    ''' </summary>
    Public Class ReceiptSequence
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Calendar year this sequence row governs. Must be unique.</summary>
        Public Property Year As Integer

        ''' <summary>The next OR sequence number to issue. Starts at 1; never gaps.</summary>
        Public Property NextValue As Integer

        ''' <summary>
        ''' EF Core optimistic concurrency token. Updated to a new <c>Guid.ToByteArray()</c>
        ''' on every write to detect concurrent modifications across connections.
        ''' </summary>
        Public Property RowVersion As Byte()

    End Class

End Namespace
