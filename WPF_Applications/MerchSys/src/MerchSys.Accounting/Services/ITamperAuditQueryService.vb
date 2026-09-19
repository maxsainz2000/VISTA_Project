Imports System.Threading
Imports MySqlConnector
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities

Namespace Services

    ''' <summary>
    ''' Read-side query contract for the tamper audit log.
    ''' Defined here so future reporting plans have a stable interface to consume.
    ''' Implementation: <see cref="TamperAuditQueryService"/> (same file).
    ''' </summary>
    Public Interface ITamperAuditQueryService
        Function GetIncidentsAsync(fromUtc As DateTime, toUtc As DateTime) As Task(Of IReadOnlyList(Of TamperAuditEntry))
        Function CountByKindAsync(fromUtc As DateTime, toUtc As DateTime) As Task(Of IReadOnlyDictionary(Of String, Integer))
    End Interface

    ''' <summary>
    ''' EF Core implementation of <see cref="ITamperAuditQueryService"/>.
    ''' Thin queries against <c>Acc_TamperAuditLog</c> — no business logic.
    ''' </summary>
    Public Class TamperAuditQueryService
        Implements ITamperAuditQueryService

        Private ReadOnly _db As AccountingDbContext

        Public Sub New(db As AccountingDbContext)
            _db = db
        End Sub

        Public Async Function GetIncidentsAsync(
            fromUtc As DateTime,
            toUtc As DateTime
        ) As Task(Of IReadOnlyList(Of TamperAuditEntry)) Implements ITamperAuditQueryService.GetIncidentsAsync

            Dim tamperAuditList As New List(Of TamperAuditEntry)()
            Dim giConnStr = _db.Database.GetConnectionString()
            Using giConn As New MySqlConnection(giConnStr)
                Await giConn.OpenAsync()
                Using giCmd = giConn.CreateCommand()
                    giCmd.CommandText = "SELECT Id, DetectedAt, ReceiptId, ReceiptNumber, TamperKind, DetectedByService, " &
                                        "ExpectedValue, ActualValue, AdditionalContextJson, MachineName, OperatingUser, " &
                                        "CreatedAt, CreatedBy " &
                                        "FROM Acc_TamperAuditLog " &
                                        "WHERE DetectedAt >= @fromUtc AND DetectedAt <= @toUtc " &
                                        "ORDER BY DetectedAt DESC"
                    giCmd.Parameters.Add(New MySqlParameter("@fromUtc", fromUtc))
                    giCmd.Parameters.Add(New MySqlParameter("@toUtc", toUtc))
                    Using giReader = giCmd.ExecuteReader()
                        While giReader.Read()
                            tamperAuditList.Add(New TamperAuditEntry With {
                                .Id = giReader.GetInt64(0),
                                .DetectedAt = giReader.GetDateTime(1),
                                .ReceiptId = giReader.GetInt64(2),
                                .ReceiptNumber = giReader.GetString(3),
                                .TamperKind = giReader.GetString(4),
                                .DetectedByService = giReader.GetString(5),
                                .ExpectedValue = If(giReader.IsDBNull(6), Nothing, giReader.GetString(6)),
                                .ActualValue = If(giReader.IsDBNull(7), Nothing, giReader.GetString(7)),
                                .AdditionalContextJson = If(giReader.IsDBNull(8), Nothing, giReader.GetString(8)),
                                .MachineName = giReader.GetString(9),
                                .OperatingUser = giReader.GetString(10),
                                .CreatedAt = giReader.GetDateTime(11),
                                .CreatedBy = giReader.GetString(12)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return tamperAuditList

        End Function

        Public Async Function CountByKindAsync(
            fromUtc As DateTime,
            toUtc As DateTime
        ) As Task(Of IReadOnlyDictionary(Of String, Integer)) Implements ITamperAuditQueryService.CountByKindAsync

            Dim kinds = Await _db.TamperAuditEntries.
                Where(Function(e) e.DetectedAt >= fromUtc AndAlso e.DetectedAt <= toUtc).
                Select(Function(e) e.TamperKind).
                ToListAsync(CancellationToken.None)

            Dim result As IReadOnlyDictionary(Of String, Integer) =
                kinds.GroupBy(Function(k) k).
                ToDictionary(Function(g) g.Key, Function(g) g.Count())

            Return result

        End Function

    End Class

End Namespace
