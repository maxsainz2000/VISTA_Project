Imports System.Text.Json

Namespace Sync.SyncMaps

    ' ── Remote entity POCOs ───────────────────────────────────────────────────────────
    ' These mirror the Inv_* MariaDB tables.  Computed properties (CurrentStock,
    ' TotalValue) are excluded; they are derived on-demand from StockBatch rows.

    Public Class RemoteProductCategory
        Public Property Id As Integer
        Public Property Name As String
        Public Property Description As String
        Public Property IsDeleted As Boolean
        Public Property DeletedBy As String
        Public Property DeletedAt As DateTime?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteProduct
        Public Property Id As Integer
        Public Property Sku As String
        Public Property Name As String
        Public Property CategoryId As Integer
        Public Property RetailPrice As Decimal
        Public Property Unit As String
        Public Property HasExpiry As Boolean
        Public Property MinimumThreshold As Decimal
        Public Property IsDeleted As Boolean
        Public Property DeletedBy As String
        Public Property DeletedAt As DateTime?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteStockBatch
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property QuantityReceived As Decimal
        Public Property QuantityRemaining As Decimal
        Public Property UnitCost As Decimal
        Public Property ReceiptDate As DateTime
        Public Property ExpiryDate As DateTime?
        Public Property IsExpired As Boolean
        Public Property IsFullyConsumed As Boolean
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteShrinkageRecord
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property Reason As String
        Public Property QuantityLost As Decimal
        Public Property UnitCost As Decimal
        Public Property TotalValue As Decimal
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteStockAlertConfig
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property MinimumThreshold As Decimal
        Public Property ExpiryAlertDays As Integer
        Public Property IsAlertEnabled As Boolean
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteStockMovement
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property MovementType As Integer
        Public Property Quantity As Decimal
        Public Property OccurredAt As DateTime
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteStockAuditRecord
        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property ExpectedQuantity As Decimal
        Public Property PhysicalCount As Decimal
        Public Property Variance As Decimal
        Public Property Reason As String
        Public Property Notes As String
        Public Property PerformedBy As String
        Public Property AuditedAt As DateTime
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    ''' <summary>
    ''' Synced representation of a local Inv_SaleCogs record.
    ''' This entity represents an append-only per-batch FIFO COGS record.
    ''' Reference: ACC-21 (Local SQLite Schema) + INFRA-22 (Central Schema Sync).
    ''' </summary>
    Public Class RemoteSaleCogs
        Public Property Id As Integer
        Public Property TransactionId As Integer
        Public Property ProductId As Integer
        Public Property BatchId As Integer
        Public Property QuantityDeducted As Integer
        Public Property UnitCost As Decimal
        Public Property Cogs As Decimal
        Public Property DeductedAt As DateTime
    End Class

    ' ── Sync map ──────────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Ownership: Inventory module sync contract.
    ''' Policy: all Inv_* tables use LastWriteWins — stock counts and adjustments may be corrected.
    ''' Deserializes journal payloads to the canonical remote POCO for each Inventory table.
    ''' </summary>
    Public Class InventorySyncMap

        Private Shared ReadOnly _options As New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True
        }

        Public Shared ReadOnly Property Tables As IReadOnlyList(Of String) = New String() {
            "Inv_ProductCategories", "Inv_Products", "Inv_StockBatches",
            "Inv_ShrinkageRecords", "Inv_StockAlertConfigs", "Inv_StockMovements",
            "Inv_StockAuditRecords", "Inv_SaleCogs" ' INFRA-22: Inv_SaleCogs is append-only — LastWriteWins is moot in practice.
        }

        Public Shared Function ToRemote(entry As SyncJournal) As Object
            Select Case entry.TableName
                Case "Inv_ProductCategories"
                    Return JsonSerializer.Deserialize(Of RemoteProductCategory)(entry.Payload, _options)
                Case "Inv_Products"
                    Return JsonSerializer.Deserialize(Of RemoteProduct)(entry.Payload, _options)
                Case "Inv_StockBatches"
                    Return JsonSerializer.Deserialize(Of RemoteStockBatch)(entry.Payload, _options)
                Case "Inv_ShrinkageRecords"
                    Return JsonSerializer.Deserialize(Of RemoteShrinkageRecord)(entry.Payload, _options)
                Case "Inv_StockAlertConfigs"
                    Return JsonSerializer.Deserialize(Of RemoteStockAlertConfig)(entry.Payload, _options)
                Case "Inv_StockMovements"
                    Return JsonSerializer.Deserialize(Of RemoteStockMovement)(entry.Payload, _options)
                Case "Inv_StockAuditRecords"
                    Return JsonSerializer.Deserialize(Of RemoteStockAuditRecord)(entry.Payload, _options)
                Case "Inv_SaleCogs"
                    Return JsonSerializer.Deserialize(Of RemoteSaleCogs)(entry.Payload, _options)
                Case Else
                    Return Nothing
            End Select
        End Function

        Public Shared Function GetRemoteKey(entry As SyncJournal) As Object
            Return entry.RowId
        End Function

    End Class

End Namespace
