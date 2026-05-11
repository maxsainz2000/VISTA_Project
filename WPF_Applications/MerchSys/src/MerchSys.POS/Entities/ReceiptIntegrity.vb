Imports MerchSys.SharedKernel.Persistence

Namespace Entities

    ''' <summary>
    ''' Sidecar integrity record for an <see cref="OfficialReceipt"/>, one-to-one relationship.
    ''' Stores a SHA-256 hash chain that detects any post-issuance tampering with receipt data
    ''' as required by NIRC §113 (issuance controls) and §235 (10-year tamper-proof preservation).
    ''' Once persisted, this record is itself immutable (enforced by <c>ImmutableReceiptInterceptor</c>).
    ''' </summary>
    ''' <remarks>
    ''' Excluded from <c>Sync_Journal</c> appending — the integrity chain is local-only until
    ''' a future plan defines central retention semantics.
    ''' </remarks>
    <NoSync>
    Public Class ReceiptIntegrity
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>FK and unique key pointing to <see cref="OfficialReceipt.Id"/>.</summary>
        Public Property ReceiptId As Integer

        ''' <summary>
        ''' SHA-256 hex digest (64-char lowercase) of the canonical receipt JSON payload
        ''' concatenated with <see cref="PreviousHash"/>. NIRC §113.
        ''' </summary>
        Public Property IntegrityHash As String

        ''' <summary>
        ''' Hash of the immediately preceding receipt in the same year's chain.
        ''' Empty string for the first receipt issued in a given year.
        ''' </summary>
        Public Property PreviousHash As String

        ''' <summary>
        ''' UTC date/time after which this receipt has satisfied the NIRC §235
        ''' 10-year retention obligation (IssueDate + 10 years).
        ''' </summary>
        Public Property RetentionExpiresAt As DateTime

        ''' <summary>
        ''' Always <c>True</c>. Defense-in-depth immutability flag; serves as a detection
        ''' breadcrumb in audit journals if the value is ever found to be <c>False</c>.
        ''' </summary>
        Public Property IsImmutable As Boolean

        ''' <summary>Algorithm identifier for future upgrade paths. Current value: "SHA-256-v1".</summary>
        Public Property HashAlgorithm As String

        ''' <summary>
        ''' The deterministic JSON snapshot that was hashed at issuance time.
        ''' Stored to allow independent re-verification without reloading transaction lines.
        ''' </summary>
        Public Property CanonicalPayload As String

        ' --- Navigation ---

        ''' <summary>The official receipt to which this integrity sidecar belongs.</summary>
        Public Property Receipt As OfficialReceipt

    End Class

End Namespace
