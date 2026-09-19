Namespace Entities

    ''' <summary>
    ''' Cold-storage archive of Official Receipts whose <c>RetentionExpiresAt + ArchivePolicy:GraceDays</c>
    ''' threshold has elapsed. Schema mirrors <see cref="OfficialReceipt"/> exactly, plus
    ''' <see cref="ArchivedAt"/> and <see cref="ArchivedHash"/> provenance columns.
    ''' <para>
    ''' Receipts are <em>copied</em> into this table — never removed from the main
    ''' <c>Pos_OfficialReceipts</c> table. Archival is for long-term partitioning, not deletion.
    ''' NIRC §235 — 10-year preservation requirement.
    ''' </para>
    ''' </summary>
    Public Class OfficialReceiptArchive
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Primary key of the source <see cref="OfficialReceipt"/> row.</summary>
        Public Property OriginalReceiptId As Integer

        Public Property TransactionId As Integer
        Public Property ReceiptNumber As String
        Public Property BusinessName As String
        Public Property BusinessAddress As String
        Public Property BusinessTIN As String
        Public Property IssueDate As DateTime
        Public Property Items As String
        Public Property TotalAmount As Decimal
        Public Property VatAmount As Decimal
        Public Property IsVatRegistered As Boolean

        ''' <summary>UTC timestamp when this receipt copy was written to the archive.</summary>
        Public Property ArchivedAt As DateTime

        ''' <summary>
        ''' The <see cref="ReceiptIntegrity.IntegrityHash"/> value at the time of archival.
        ''' Proves that integrity was confirmed before the receipt was moved to cold storage.
        ''' </summary>
        Public Property ArchivedHash As String

    End Class

End Namespace
