Imports System.Security.Principal
Imports MerchSys.SharedKernel.Persistence

Namespace Entities

    ''' <summary>
    ''' Append-only security audit record for receipt tamper incidents raised by POS-13.
    ''' Excluded from <c>Sync_Journal</c> appending — tamper evidence is local-only until
    ''' a future plan defines central retention semantics.
    '''
    ''' Deliberate deviations from the CLAUDE.md standard audit-column convention:
    ''' • No <c>IsDeleted</c> / soft-delete — tamper records must be physically retained
    '''   (BIR §235 requires 10-year tamper-proof preservation; deletion would defeat the purpose).
    ''' • No <c>ModifiedBy</c> / <c>ModifiedAt</c> — the row is immutable after creation;
    '''   SQLite-level UPDATE/DELETE triggers (see migration AddTamperAuditLog) enforce this at
    '''   the database layer as well.
    ''' • No cross-module foreign key on <c>ReceiptId</c> — modular monolith rule; the
    '''   denormalised <c>ReceiptNumber</c> snapshot survives archival of the POS row.
    ''' </summary>
    <NoSync>
    Public Class TamperAuditEntry

        ''' <summary>Surrogate primary key (auto-increment).</summary>
        Public Property Id As Long

        ''' <summary>UTC date/time the integrity violation was detected.</summary>
        Public Property DetectedAt As DateTime

        ''' <summary>
        ''' Primary key of the affected receipt in Pos_ tables.
        ''' Foreign-keyless reference — cross-module FK would violate modular-monolith rules.
        ''' </summary>
        Public Property ReceiptId As Long

        ''' <summary>Denormalised OR number (e.g., OR-2026-0042). Survives POS row archival.</summary>
        Public Property ReceiptNumber As String

        ''' <summary>
        ''' Category of tamper evidence.
        ''' Expected values: "HashMismatch" | "MissingSequenceRow" | "StatusTransition" | "OutOfOrderNumber".
        ''' </summary>
        Public Property TamperKind As String

        ''' <summary>Class name of the service that performed the integrity check.</summary>
        Public Property DetectedByService As String

        ''' <summary>Expected value at time of issuance (e.g., original SHA-256 hash). Nullable.</summary>
        Public Property ExpectedValue As String

        ''' <summary>Value observed at verification time (e.g., recomputed SHA-256 hash). Nullable.</summary>
        Public Property ActualValue As String

        ''' <summary>Free-form JSON payload from the originating event for extended context. Nullable.</summary>
        Public Property AdditionalContextJson As String

        ''' <summary>NetBIOS or DNS name of the machine that ran the integrity check.</summary>
        Public Property MachineName As String

        ''' <summary>Windows identity (DOMAIN\User) of the process at detection time.</summary>
        Public Property OperatingUser As String

        ''' <summary>UTC timestamp when this audit row was inserted.</summary>
        Public Property CreatedAt As DateTime

        ''' <summary>Identity that triggered the creation of this audit row (typically the handler class name).</summary>
        Public Property CreatedBy As String

    End Class

End Namespace
