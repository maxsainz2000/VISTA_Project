Imports System.Text.Json

Namespace Sync.SyncMaps

    ' ── Remote entity POCOs ───────────────────────────────────────────────────────────
    ' All Acc_* tables are AppendOnly — ledger rows are immutable once posted.
    ' The MariaDB triggers enforce this at the DB level; ConflictResolver enforces it
    ' on the client side (defence in depth).

    Public Class RemoteFinancialPeriod
        Public Property Id As Integer
        Public Property PeriodType As String
        Public Property StartDate As DateTime
        Public Property EndDate As DateTime
        Public Property TotalRevenue As Decimal
        Public Property TotalCOGS As Decimal
        Public Property GrossProfit As Decimal
        Public Property GrossMarginPercent As Decimal
        Public Property TotalExpenses As Decimal
        Public Property NetIncome As Decimal
        Public Property IsClosed As Boolean
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteRevenueRecord
        Public Property Id As Integer
        Public Property RecordDate As DateTime
        Public Property SourceTransactionId As Integer
        Public Property PaymentMethod As Integer
        Public Property GrossAmount As Decimal
        Public Property DiscountAmount As Decimal
        Public Property NetAmount As Decimal
        Public Property VatAmount As Decimal
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QuantitySold As Integer
        Public Property COGS As Decimal
        Public Property GrossProfit As Decimal
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteExpenseRecord
        Public Property Id As Integer
        Public Property RecordDate As DateTime
        Public Property Category As String
        Public Property Description As String
        Public Property Amount As Decimal
        Public Property SourceModule As String
        Public Property SourceReferenceId As Integer?
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    Public Class RemoteFinancialSnapshot
        Public Property Id As Integer
        Public Property SnapshotDate As DateTime
        Public Property TotalAR As Decimal
        Public Property TotalAP As Decimal
        Public Property InventoryValue As Decimal
        Public Property TodayRevenue As Decimal
        Public Property MonthToDateRevenue As Decimal
        Public Property YearToDateRevenue As Decimal
        Public Property CreatedBy As String
        Public Property CreatedAt As DateTime
        Public Property ModifiedBy As String
        Public Property ModifiedAt As DateTime?
    End Class

    ' ── Sync map ──────────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Ownership: Accounting module sync contract.
    ''' Policy: all Acc_* tables are AppendOnly — ledger rows are immutable once posted.
    ''' The MariaDB BEFORE UPDATE/DELETE triggers enforce this at the database level.
    ''' </summary>
    Public Class AccountingSyncMap

        Private Shared ReadOnly _options As New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True
        }

        Public Shared ReadOnly Property Tables As IReadOnlyList(Of String) = New String() {
            "Acc_FinancialPeriods", "Acc_RevenueRecords", "Acc_ExpenseRecords",
            "Acc_FinancialSnapshots"
        }

        Public Shared Function ToRemote(entry As SyncJournal) As Object
            Select Case entry.TableName
                Case "Acc_FinancialPeriods"
                    Return JsonSerializer.Deserialize(Of RemoteFinancialPeriod)(entry.Payload, _options)
                Case "Acc_RevenueRecords"
                    Return JsonSerializer.Deserialize(Of RemoteRevenueRecord)(entry.Payload, _options)
                Case "Acc_ExpenseRecords"
                    Return JsonSerializer.Deserialize(Of RemoteExpenseRecord)(entry.Payload, _options)
                Case "Acc_FinancialSnapshots"
                    Return JsonSerializer.Deserialize(Of RemoteFinancialSnapshot)(entry.Payload, _options)
                Case Else
                    Return Nothing
            End Select
        End Function

        Public Shared Function GetRemoteKey(entry As SyncJournal) As Object
            Return entry.RowId
        End Function

    End Class

End Namespace
