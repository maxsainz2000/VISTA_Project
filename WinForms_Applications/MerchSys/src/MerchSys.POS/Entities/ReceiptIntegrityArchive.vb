Namespace Entities

    ''' <summary>
    ''' Cold-storage archive of <see cref="ReceiptIntegrity"/> sidecar rows whose parent receipt
    ''' has been moved to <see cref="OfficialReceiptArchive"/>. Schema mirrors
    ''' <see cref="ReceiptIntegrity"/> exactly, plus <see cref="ArchivedAt"/> and
    ''' <see cref="ArchivedByService"/> provenance columns.
    ''' <para>
    ''' This table is INSERT-only at the database level (SQLite triggers block UPDATE and DELETE).
    ''' NIRC §235 — 10-year tamper-proof preservation.
    ''' </para>
    ''' </summary>
    Public Class ReceiptIntegrityArchive
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Id of the source <see cref="ReceiptIntegrity"/> row.</summary>
        Public Property OriginalIntegrityId As Integer

        ''' <summary>Id of the source <see cref="OfficialReceipt"/> row.</summary>
        Public Property ReceiptId As Integer

        ''' <summary>SHA-256 hex digest copied from the source integrity record.</summary>
        Public Property IntegrityHash As String

        ''' <summary>Hash of the preceding receipt in the chain, copied from source.</summary>
        Public Property PreviousHash As String

        ''' <summary>UTC date/time after which the NIRC §235 retention obligation is satisfied.</summary>
        Public Property RetentionExpiresAt As DateTime

        ''' <summary>Always <c>True</c>. Copied from source as an immutability breadcrumb.</summary>
        Public Property IsImmutable As Boolean

        ''' <summary>Algorithm identifier copied from source. Current value: "SHA-256-v1".</summary>
        Public Property HashAlgorithm As String

        ''' <summary>The deterministic JSON payload that was hashed at issuance time.</summary>
        Public Property CanonicalPayload As String

        ''' <summary>UTC timestamp when this row was written to the integrity archive.</summary>
        Public Property ArchivedAt As DateTime

        ''' <summary>
        ''' Identifies the service that performed the archival.
        ''' Fixed value: <c>"ReceiptArchivalService"</c>.
        ''' </summary>
        Public Property ArchivedByService As String

    End Class

End Namespace
