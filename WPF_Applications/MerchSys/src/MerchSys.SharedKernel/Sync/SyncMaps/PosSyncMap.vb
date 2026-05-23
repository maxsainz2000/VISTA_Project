Imports System.Text.Json

Namespace Sync.SyncMaps

    ' ── Remote entity POCOs ───────────────────────────────────────────────────────────
    ' Pos_OfficialReceipts and Pos_CreditPayments are AppendOnly (BIR-mandated).
    ' Navigation properties and EF Core shadow properties are excluded.

    Public Class RemoteSalesTransaction
        Public Property Id As Integer
        Public Property TransactionNumber As String
        Public Property TransactionDate As DateTime
        Public Property CustomerId As Integer?
        Public Property CustomerName As String
        Public Property PaymentMethod As Integer
        Public Property SubTotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property VatAmount As Decimal
        Public Property TotalAmount As Decimal
        Public Property AmountTendered As Decimal
        Public Property ChangeAmount As Decimal
        Public Property IsVoided As Boolean
        Public Property VoidReason As String
        Public Property IsDeleted As Boolean
        Public Property DeletedBy As String
        Public Property DeletedAt As DateTime?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteSalesTransactionLine
        Public Property Id As Integer
        Public Property TransactionId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Quantity As Decimal
        Public Property UnitPrice As Decimal
        Public Property LineTotal As Decimal
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteCreditAccount
        Public Property Id As Integer
        Public Property CustomerName As String
        Public Property CreditLimit As Decimal
        Public Property Balance As Decimal
        Public Property IsBlocked As Boolean
        Public Property IsDeleted As Boolean
        Public Property DeletedBy As String
        Public Property DeletedAt As DateTime?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteOfficialReceipt
        Public Property Id As Integer
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
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
        ''' <summary>SHA-256 hash from Pos_ReceiptIntegrity; Nothing for receipts created before POS-13.</summary>
        Public Property IntegrityHash As String
    End Class

    Public Class RemoteCreditPayment
        Public Property Id As Integer
        Public Property CreditAccountId As Integer
        Public Property PaymentAmount As Decimal
        Public Property PaymentDate As DateTime
        Public Property PaymentMethod As Integer
        Public Property Notes As String
        Public Property ReceivedBy As String
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteSalesReturn
        Public Property Id As Integer
        Public Property OriginalTransactionId As Integer
        Public Property ReturnDate As DateTime
        Public Property Reason As String
        Public Property RefundAmount As Decimal
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    ''' <summary>
    ''' Placeholder for the Pos_ReceiptIntegrity table defined in the MariaDB DDL.
    ''' No local EF Core entity exists yet; entries for this table will not be produced
    ''' until the corresponding POS-module implementation is added.
    ''' </summary>
    Public Class RemoteReceiptIntegrity
        Public Property Id As Integer
        Public Property TransactionId As Integer
        Public Property ReceiptId As Integer
        Public Property ChecksumHash As String
        Public Property ValidatedAt As DateTime
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    ' ── Sync map ──────────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Ownership: POS module sync contract.
    ''' Policy: <c>Pos_OfficialReceipts</c>, <c>Pos_ReceiptIntegrity</c>, and
    ''' <c>Pos_CreditPayments</c> are AppendOnly (BIR compliance — see POS-13).
    ''' All other Pos_* tables use LastWriteWins.
    ''' </summary>
    Public Class PosSyncMap

        Private Shared ReadOnly _options As New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True
        }

        Public Shared ReadOnly Property Tables As IReadOnlyList(Of String) = New String() {
            "Pos_SalesTransactions", "Pos_SalesTransactionLines", "Pos_CreditAccounts",
            "Pos_OfficialReceipts", "Pos_CreditPayments", "Pos_SalesReturns",
            "Pos_ReceiptIntegrity"
        }

        Public Shared Function ToRemote(entry As SyncJournal) As Object
            Select Case entry.TableName
                Case "Pos_SalesTransactions"
                    Return JsonSerializer.Deserialize(Of RemoteSalesTransaction)(entry.Payload, _options)
                Case "Pos_SalesTransactionLines"
                    Return JsonSerializer.Deserialize(Of RemoteSalesTransactionLine)(entry.Payload, _options)
                Case "Pos_CreditAccounts"
                    Return JsonSerializer.Deserialize(Of RemoteCreditAccount)(entry.Payload, _options)
                Case "Pos_OfficialReceipts"
                    Return JsonSerializer.Deserialize(Of RemoteOfficialReceipt)(entry.Payload, _options)
                Case "Pos_CreditPayments"
                    Return JsonSerializer.Deserialize(Of RemoteCreditPayment)(entry.Payload, _options)
                Case "Pos_SalesReturns"
                    Return JsonSerializer.Deserialize(Of RemoteSalesReturn)(entry.Payload, _options)
                Case "Pos_ReceiptIntegrity"
                    Return JsonSerializer.Deserialize(Of RemoteReceiptIntegrity)(entry.Payload, _options)
                Case Else
                    Return Nothing
            End Select
        End Function

        Public Shared Function GetRemoteKey(entry As SyncJournal) As Object
            Return entry.RowId
        End Function

    End Class

End Namespace
