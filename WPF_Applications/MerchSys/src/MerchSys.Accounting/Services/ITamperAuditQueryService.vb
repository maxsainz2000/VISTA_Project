Imports System.Threading
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

            Dim rows = Await _db.TamperAuditEntries.
                Where(Function(e) e.DetectedAt >= fromUtc AndAlso e.DetectedAt <= toUtc).
                OrderByDescending(Function(e) e.DetectedAt).
                ToListAsync(CancellationToken.None)

            Return rows

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
