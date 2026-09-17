Imports MerchSys.POS.Entities

Namespace Services

    ''' <summary>
    ''' Manages SHA-256 hash-chain integrity for Official Receipts.
    ''' Enforces NIRC §113 (issuance sequence controls) and §235 (10-year tamper-proof retention).
    ''' </summary>
    Public Interface IReceiptIntegrityService

        ''' <summary>
        ''' Computes the deterministic canonical hash for <paramref name="receipt"/>,
        ''' chains it to the previous receipt's hash, and persists a <see cref="ReceiptIntegrity"/> sidecar.
        ''' Must be called immediately after the <see cref="OfficialReceipt"/> row is saved.
        ''' </summary>
        Function ComputeAndPersistAsync(receipt As OfficialReceipt) As Task(Of ReceiptIntegrity)

        ''' <summary>
        ''' Validates the stored integrity hash for a single receipt by recomputing it from
        ''' current database state. Publishes <c>ReceiptTamperDetectedEvent</c> on mismatch.
        ''' </summary>
        Function ValidateAsync(receiptId As Integer) As Task(Of IntegrityValidationResult)

        ''' <summary>
        ''' Walks the full hash chain for all receipts issued in <paramref name="year"/>
        ''' and returns a chain validation result. Stops and publishes tamper event at first failure.
        ''' </summary>
        Function ValidateChainAsync(year As Integer) As Task(Of ChainValidationResult)

        ''' <summary>
        ''' Returns the next formatted receipt number (OR-YYYY-XXXX) using a row-locked,
        ''' serializable-isolation sequence row. Retries up to 10 times on concurrency conflict.
        ''' </summary>
        Function GetNextReceiptNumberAsync(year As Integer) As Task(Of String)

    End Interface

    ''' <summary>Result of a single-receipt integrity hash check.</summary>
    Public Class IntegrityValidationResult
        Public Property IsValid As Boolean
        Public Property ReceiptId As Integer
        Public Property ExpectedHash As String
        Public Property ActualHash As String
    End Class

    ''' <summary>Result of a full-year hash chain validation.</summary>
    Public Class ChainValidationResult
        Public Property IsValid As Boolean
        Public Property Year As Integer
        Public Property TotalChecked As Integer
        Public Property FirstFailedReceiptId As Integer?
    End Class

End Namespace
