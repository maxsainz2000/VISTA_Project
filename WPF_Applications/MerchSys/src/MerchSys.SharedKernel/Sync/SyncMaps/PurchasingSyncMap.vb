Imports System.Text.Json

Namespace Sync.SyncMaps

    ' ── Remote entity POCOs ───────────────────────────────────────────────────────────
    ' These mirror the Pur_* MariaDB tables.  Navigation properties are intentionally
    ' absent; SQLite-specific computed columns (CurrentStock etc.) are also excluded.

    Public Class RemoteVendor
        Public Property Id As Integer
        Public Property Name As String
        Public Property ContactPerson As String
        Public Property Phone As String
        Public Property Email As String
        Public Property Address As String
        Public Property DefaultLeadTimeDays As Integer
        Public Property Notes As String
        Public Property IsDeleted As Boolean
        Public Property DeletedBy As String
        Public Property DeletedAt As DateTime?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemotePurchaseOrder
        Public Property Id As Integer
        Public Property OrderNumber As String
        Public Property VendorId As Integer
        Public Property Status As Integer
        Public Property OrderDate As DateTime
        Public Property ExpectedDeliveryDate As DateTime?
        Public Property TotalAmount As Decimal
        Public Property Notes As String
        Public Property IsDeleted As Boolean
        Public Property DeletedBy As String
        Public Property DeletedAt As DateTime?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemotePurchaseOrderLine
        Public Property Id As Integer
        Public Property PurchaseOrderId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QuantityOrdered As Decimal
        Public Property UnitCost As Decimal
        Public Property LineTotal As Decimal
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteGoodsReceipt
        Public Property Id As Integer
        Public Property PurchaseOrderId As Integer
        Public Property ReceiptNumber As String
        Public Property ReceivedDate As DateTime
        Public Property ReceivedBy As String
        Public Property Notes As String
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteGoodsReceiptLine
        Public Property Id As Integer
        Public Property GoodsReceiptId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QuantityOrdered As Decimal
        Public Property QuantityReceived As Decimal
        Public Property UnitCost As Decimal
        Public Property ExpiryDate As DateTime?
        Public Property HasDiscrepancy As Boolean
        Public Property DiscrepancyNotes As String
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteAccountsPayableEntry
        Public Property Id As Integer
        Public Property PurchaseOrderId As Integer
        Public Property VendorId As Integer
        Public Property InvoiceNumber As String
        Public Property InvoiceDate As DateTime
        Public Property DueDate As DateTime
        Public Property TotalAmount As Decimal
        Public Property AmountPaid As Decimal
        Public Property Balance As Decimal
        Public Property IsPaid As Boolean
        Public Property Notes As String
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteReorderConfig
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property PreferredVendorId As Integer?
        Public Property MinimumThreshold As Decimal
        Public Property SafetyStock As Decimal
        Public Property DefaultOrderQuantity As Decimal
        Public Property LeadTimeDays As Integer
        Public Property IsSeasonalItem As Boolean
        Public Property SeasonalMultiplier As Decimal
        Public Property IsActive As Boolean
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteReorderSuggestion
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property CurrentStock As Decimal
        Public Property ReorderPoint As Decimal
        Public Property SuggestedQuantity As Decimal
        Public Property PreferredVendorId As Integer?
        Public Property PreferredVendorName As String
        Public Property EstimatedLeadTimeDays As Integer
        Public Property IsSeasonalAdjusted As Boolean
        Public Property Status As String
        Public Property ConvertedToPOId As Integer?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemotePriceChangeAlert
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property VendorId As Integer
        Public Property VendorName As String
        Public Property PreviousUnitCost As Decimal
        Public Property NewUnitCost As Decimal
        Public Property ChangePercent As Decimal
        Public Property ChangeDirection As String
        Public Property GoodsReceiptId As Integer
        Public Property IsAcknowledged As Boolean
        Public Property AcknowledgedAt As DateTime?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    ' ── Sync map ──────────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Ownership: Purchasing module sync contract.
    ''' Policy: all Pur_* tables use LastWriteWins — PO edits are acceptable until goods are received.
    ''' Deserializes journal payloads to the canonical remote POCO for each Purchasing table.
    ''' </summary>
    Public Class PurchasingSyncMap

        Private Shared ReadOnly _options As New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True
        }

        Public Shared ReadOnly Property Tables As IReadOnlyList(Of String) = New String() {
            "Pur_Vendors", "Pur_PurchaseOrders", "Pur_PurchaseOrderLines",
            "Pur_GoodsReceipts", "Pur_GoodsReceiptLines", "Pur_AccountsPayable",
            "Pur_ReorderConfigs", "Pur_ReorderSuggestions", "Pur_PriceChangeAlerts"
        }

        Public Shared Function ToRemote(entry As SyncJournal) As Object
            Select Case entry.TableName
                Case "Pur_Vendors"
                    Return JsonSerializer.Deserialize(Of RemoteVendor)(entry.Payload, _options)
                Case "Pur_PurchaseOrders"
                    Return JsonSerializer.Deserialize(Of RemotePurchaseOrder)(entry.Payload, _options)
                Case "Pur_PurchaseOrderLines"
                    Return JsonSerializer.Deserialize(Of RemotePurchaseOrderLine)(entry.Payload, _options)
                Case "Pur_GoodsReceipts"
                    Return JsonSerializer.Deserialize(Of RemoteGoodsReceipt)(entry.Payload, _options)
                Case "Pur_GoodsReceiptLines"
                    Return JsonSerializer.Deserialize(Of RemoteGoodsReceiptLine)(entry.Payload, _options)
                Case "Pur_AccountsPayable"
                    Return JsonSerializer.Deserialize(Of RemoteAccountsPayableEntry)(entry.Payload, _options)
                Case "Pur_ReorderConfigs"
                    Return JsonSerializer.Deserialize(Of RemoteReorderConfig)(entry.Payload, _options)
                Case "Pur_ReorderSuggestions"
                    Return JsonSerializer.Deserialize(Of RemoteReorderSuggestion)(entry.Payload, _options)
                Case "Pur_PriceChangeAlerts"
                    Return JsonSerializer.Deserialize(Of RemotePriceChangeAlert)(entry.Payload, _options)
                Case Else
                    Return Nothing
            End Select
        End Function

        Public Shared Function GetRemoteKey(entry As SyncJournal) As Object
            Return entry.RowId
        End Function

    End Class

End Namespace
