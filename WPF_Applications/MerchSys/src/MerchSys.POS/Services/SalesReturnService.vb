Imports System.Threading
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class SalesReturnService
        Implements ISalesReturnService

        Private ReadOnly _context As POSDbContext
        Private ReadOnly _eventBus As IEventBus
        Private ReadOnly _repository As ISyncableRepository(Of POSDbContext)
        Private _returnsForTxList As List(Of SalesReturn)
        Private _returnHistoryList As List(Of SalesReturn)

        Public Sub New(context As POSDbContext,
                       eventBus As IEventBus,
                       repository As ISyncableRepository(Of POSDbContext))
            _context = context
            _eventBus = eventBus
            _repository = repository
        End Sub

        Public Async Function ProcessReturnAsync(
            originalTransactionId As Integer,
            productId As Integer,
            quantity As Integer,
            reason As String,
            shouldRestock As Boolean) As Task(Of SalesReturn) Implements ISalesReturnService.ProcessReturnAsync

            If String.IsNullOrWhiteSpace(reason) Then
                Throw New ArgumentException("A return reason is required.")
            End If

            If quantity <= 0 Then
                Throw New ArgumentException("Return quantity must be greater than zero.")
            End If

            Dim transaction = Await _context.SalesTransactions _
                .Include(Function(t) t.Lines) _
                .Include(Function(t) t.CreditAccount) _
                .FirstOrDefaultAsync(Function(t) t.Id = originalTransactionId AndAlso Not t.IsDeleted)

            If transaction Is Nothing Then
                Throw New InvalidOperationException($"Transaction {originalTransactionId} not found.")
            End If

            If transaction.IsVoided Then
                Throw New InvalidOperationException($"Cannot process return against voided transaction {originalTransactionId}.")
            End If

            Dim line = transaction.Lines.FirstOrDefault(Function(l) l.ProductId = productId)
            If line Is Nothing Then
                Throw New InvalidOperationException($"Product {productId} was not sold in transaction {originalTransactionId}.")
            End If

            Dim alreadyReturned = Await _context.SalesReturns _
                .Where(Function(r) r.OriginalTransactionId = originalTransactionId AndAlso r.ProductId = productId) _
                .SumAsync(Function(r) r.QuantityReturned)

            Dim remainingReturnable = line.Quantity - alreadyReturned
            If quantity > remainingReturnable Then
                Throw New InvalidOperationException(
                    $"Cannot return {quantity} unit(s) of '{line.ProductName}'. " &
                    $"Original quantity: {line.Quantity}, already returned: {alreadyReturned}, remaining: {remainingReturnable}.")
            End If

            Dim refundAmount = quantity * line.UnitPrice

            Dim salesReturn As New SalesReturn() With {
                .OriginalTransactionId = originalTransactionId,
                .ReturnDate = DateTime.UtcNow,
                .ProductId = productId,
                .ProductName = line.ProductName,
                .QuantityReturned = quantity,
                .UnitPrice = line.UnitPrice,
                .RefundAmount = refundAmount,
                .Reason = reason,
                .IsRestocked = shouldRestock
            }
            _context.SalesReturns.Add(salesReturn)

            If transaction.PaymentMethod = PaymentMethod.Credit AndAlso transaction.CreditAccount IsNot Nothing Then
                Dim account = transaction.CreditAccount
                account.CurrentBalance -= refundAmount
                If account.CurrentBalance < 0D Then account.CurrentBalance = 0D
                If account.CurrentBalance = 0D Then account.IsBlocked = False
                account.LastTransactionDate = DateTime.UtcNow
            End If

            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population

            If shouldRestock Then
                Await _eventBus.PublishAsync(New StockReturnedEvent() With {
                    .ReturnId = salesReturn.Id,
                    .OriginalTransactionId = originalTransactionId,
                    .ReturnDate = salesReturn.ReturnDate,
                    .ProductId = productId,
                    .ProductName = line.ProductName,
                    .QuantityReturned = quantity,
                    .UnitPrice = line.UnitPrice
                })
            End If

            Return salesReturn
        End Function

        Public Async Function GetReturnsForTransactionAsync(transactionId As Integer) As Task(Of List(Of SalesReturn)) Implements ISalesReturnService.GetReturnsForTransactionAsync
            _returnsForTxList = New List(Of SalesReturn)()
            Dim rftConnStr = _context.Database.GetConnectionString()
            Using rftConn As New SqliteConnection(rftConnStr)
                Await rftConn.OpenAsync()
                Using rftCmd = rftConn.CreateCommand()
                    rftCmd.CommandText = "SELECT Id, OriginalTransactionId, ReturnDate, ProductId, ProductName, " &
                                         "QuantityReturned, UnitPrice, RefundAmount, Reason, IsRestocked, " &
                                         "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                         "FROM Pos_SalesReturns WHERE OriginalTransactionId = @txId " &
                                         "ORDER BY ReturnDate"
                    rftCmd.Parameters.Add(New SqliteParameter("@txId", transactionId))
                    Using rftReader = rftCmd.ExecuteReader()
                        While rftReader.Read()
                            _returnsForTxList.Add(ReadSalesReturn(rftReader))
                        End While
                    End Using
                End Using
            End Using
            Return _returnsForTxList
        End Function

        Public Async Function GetReturnHistoryAsync(startDate As DateTime, endDate As DateTime) As Task(Of List(Of SalesReturn)) Implements ISalesReturnService.GetReturnHistoryAsync
            Dim endOfDay = endDate.Date.AddDays(1).AddTicks(-1)
            _returnHistoryList = New List(Of SalesReturn)()
            Dim rhConnStr = _context.Database.GetConnectionString()
            Using rhConn As New SqliteConnection(rhConnStr)
                Await rhConn.OpenAsync()
                Using rhCmd = rhConn.CreateCommand()
                    rhCmd.CommandText = "SELECT Id, OriginalTransactionId, ReturnDate, ProductId, ProductName, " &
                                         "QuantityReturned, UnitPrice, RefundAmount, Reason, IsRestocked, " &
                                         "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                         "FROM Pos_SalesReturns " &
                                         "WHERE ReturnDate >= @startDate AND ReturnDate <= @endOfDay " &
                                         "ORDER BY ReturnDate DESC"
                    rhCmd.Parameters.Add(New SqliteParameter("@startDate", startDate.ToString("o")))
                    rhCmd.Parameters.Add(New SqliteParameter("@endOfDay", endOfDay.ToString("o")))
                    Using rhReader = rhCmd.ExecuteReader()
                        While rhReader.Read()
                            _returnHistoryList.Add(ReadSalesReturn(rhReader))
                        End While
                    End Using
                End Using
            End Using
            Return _returnHistoryList
        End Function

        Public Async Function GetTransactionIdsWithReturnsAsync(transactionIds As List(Of Integer)) As Task(Of HashSet(Of Integer)) Implements ISalesReturnService.GetTransactionIdsWithReturnsAsync
            If transactionIds Is Nothing OrElse transactionIds.Count = 0 Then
                Return New HashSet(Of Integer)()
            End If
            Dim ids = Await _context.SalesReturns _
                .Where(Function(r) transactionIds.Contains(r.OriginalTransactionId)) _
                .Select(Function(r) r.OriginalTransactionId) _
                .Distinct() _
                .ToListAsync()
            Return New HashSet(Of Integer)(ids)
        End Function

        Private Shared Function ReadSalesReturn(r As Microsoft.Data.Sqlite.SqliteDataReader) As SalesReturn
            Return New SalesReturn With {
                .Id = r.GetInt32(0),
                .OriginalTransactionId = r.GetInt32(1),
                .ReturnDate = r.GetDateTime(2),
                .ProductId = r.GetInt32(3),
                .ProductName = r.GetString(4),
                .QuantityReturned = r.GetInt32(5),
                .UnitPrice = r.GetDecimal(6),
                .RefundAmount = r.GetDecimal(7),
                .Reason = r.GetString(8),
                .IsRestocked = r.GetBoolean(9),
                .CreatedBy = r.GetString(10),
                .CreatedAt = r.GetDateTime(11),
                .ModifiedBy = If(r.IsDBNull(12), Nothing, r.GetString(12)),
                .ModifiedAt = If(r.IsDBNull(13), Nothing, CType(r.GetDateTime(13), DateTime?))
            }
        End Function

    End Class

End Namespace
