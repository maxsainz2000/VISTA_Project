Imports MediatR

Namespace Events

    ''' <summary>
    ''' Published by POS (POS-13) when the receipt integrity check detects a hash mismatch,
    ''' indicating potential tampering with an issued Official Receipt.
    ''' Consumed by Accounting (audit log handler) for mandatory incident recording.
    ''' BIR rationale: Official Receipts are immutable once issued (RR No. 18-2012);
    ''' any post-issuance alteration is a compliance violation requiring documentation.
    ''' </summary>
    Public Class ReceiptTamperDetectedEvent
        Implements INotification

        ''' <summary>Primary key of the tampered receipt row in Pos_ tables.</summary>
        Public Property ReceiptId As Integer

        ''' <summary>Human-readable OR number in format OR-YYYY-XXXX for audit reports.</summary>
        Public Property ReceiptNumber As String

        ''' <summary>SHA-256 hash computed at issuance and stored with the receipt.</summary>
        Public Property ExpectedHash As String

        ''' <summary>SHA-256 hash computed at verification time — differs from <see cref="ExpectedHash"/> when tampering occurred.</summary>
        Public Property ActualHash As String

        ''' <summary>UTC date/time the mismatch was detected.</summary>
        Public Property DetectedAt As DateTime

        ''' <summary>Name of the service or background job that performed the integrity check (e.g., "ReceiptIntegrityService").</summary>
        Public Property DetectedBy As String

    End Class

End Namespace
