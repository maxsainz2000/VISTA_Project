Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Class DailySummaryService
        Implements IDailySummaryService

        Private ReadOnly _context As POSDbContext
        Private _buildDailyTxList As List(Of SalesTransaction)
        Private _buildDailyRetList As List(Of SalesReturn)
        Private _buildPeriodTxList As List(Of SalesTransaction)
        Private _buildPeriodRetList As List(Of SalesReturn)

        Public Sub New(context As POSDbContext)
            _context = context
        End Sub

        Public Async Function GetDailySummaryAsync(targetDate As DateTime) As Task(Of DailySummaryDto) Implements IDailySummaryService.GetDailySummaryAsync
            Dim dayStart = targetDate.Date
            Dim dayEnd = dayStart.AddDays(1).AddTicks(-1)
            Return Await BuildDailySummaryAsync(dayStart, dayEnd)
        End Function

        Public Async Function GetWeeklySummaryAsync(weekStartDate As DateTime) As Task(Of PeriodSummaryDto) Implements IDailySummaryService.GetWeeklySummaryAsync
            Dim periodStart = weekStartDate.Date
            Dim periodEnd = periodStart.AddDays(7).AddTicks(-1)
            Return Await BuildPeriodSummaryAsync(periodStart, periodEnd)
        End Function

        Public Async Function GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of PeriodSummaryDto) Implements IDailySummaryService.GetMonthlySummaryAsync
            Dim periodStart = New DateTime(year, month, 1)
            Dim periodEnd = periodStart.AddMonths(1).AddTicks(-1)
            Return Await BuildPeriodSummaryAsync(periodStart, periodEnd)
        End Function

        Private Async Function BuildDailySummaryAsync(dayStart As DateTime, dayEnd As DateTime) As Task(Of DailySummaryDto)
            _buildDailyTxList = New List(Of SalesTransaction)()
            _buildDailyRetList = New List(Of SalesReturn)()
            Dim dsConnStr = _context.Database.GetConnectionString()
            Using dsConn As New SqliteConnection(dsConnStr)
                Await dsConn.OpenAsync()
                Using dsCmd = dsConn.CreateCommand()
                    dsCmd.CommandText = "SELECT Id, TransactionDate, PaymentMethod, TotalAmount " &
                                         "FROM Pos_SalesTransactions " &
                                         "WHERE TransactionDate >= @start AND TransactionDate <= @end " &
                                         "AND IsVoided = 0 AND IsDeleted = 0"
                    dsCmd.Parameters.Add(New SqliteParameter("@start", dayStart.ToString("o")))
                    dsCmd.Parameters.Add(New SqliteParameter("@end", dayEnd.ToString("o")))
                    Using dsReader = dsCmd.ExecuteReader()
                        While dsReader.Read()
                            _buildDailyTxList.Add(New SalesTransaction With {
                                .Id = dsReader.GetInt32(0),
                                .TransactionDate = dsReader.GetDateTime(1),
                                .PaymentMethod = CType(dsReader.GetInt32(2), PaymentMethod),
                                .TotalAmount = dsReader.GetDecimal(3)
                            })
                        End While
                    End Using
                End Using
                If _buildDailyTxList.Count > 0 Then
                    Dim txIds = String.Join(",", _buildDailyTxList.Select(Function(t) t.Id))
                    Dim lineMap As New Dictionary(Of Integer, List(Of SalesTransactionLine))()
                    Using lCmd = dsConn.CreateCommand()
                        lCmd.CommandText = "SELECT TransactionId, ProductId, ProductName, Quantity, LineTotal " &
                                            $"FROM Pos_SalesTransactionLines WHERE TransactionId IN ({txIds})"
                        Using lReader = lCmd.ExecuteReader()
                            While lReader.Read()
                                Dim line As New SalesTransactionLine With {
                                    .TransactionId = lReader.GetInt32(0),
                                    .ProductId = lReader.GetInt32(1),
                                    .ProductName = lReader.GetString(2),
                                    .Quantity = lReader.GetInt32(3),
                                    .LineTotal = lReader.GetDecimal(4)
                                }
                                If Not lineMap.ContainsKey(line.TransactionId) Then lineMap(line.TransactionId) = New List(Of SalesTransactionLine)()
                                lineMap(line.TransactionId).Add(line)
                            End While
                        End Using
                    End Using
                    For Each tx In _buildDailyTxList
                        Dim txLines As List(Of SalesTransactionLine) = Nothing
                        If lineMap.TryGetValue(tx.Id, txLines) Then
                            For Each ln In txLines : tx.Lines.Add(ln) : Next
                        End If
                    Next
                End If
                Using retCmd = dsConn.CreateCommand()
                    retCmd.CommandText = "SELECT ReturnDate, RefundAmount FROM Pos_SalesReturns " &
                                          "WHERE ReturnDate >= @start AND ReturnDate <= @end"
                    retCmd.Parameters.Add(New SqliteParameter("@start", dayStart.ToString("o")))
                    retCmd.Parameters.Add(New SqliteParameter("@end", dayEnd.ToString("o")))
                    Using retReader = retCmd.ExecuteReader()
                        While retReader.Read()
                            _buildDailyRetList.Add(New SalesReturn With {
                                .ReturnDate = retReader.GetDateTime(0),
                                .RefundAmount = retReader.GetDecimal(1)
                            })
                        End While
                    End Using
                End Using
            End Using
            Dim transactions As List(Of SalesTransaction) = _buildDailyTxList
            Dim returns As List(Of SalesReturn) = _buildDailyRetList

            Dim totalSales = transactions.Sum(Function(t) t.TotalAmount)
            Dim txCount = transactions.Count
            Dim avgValue = If(txCount > 0, totalSales / txCount, 0D)

            Dim paymentBreakdown = transactions _
                .GroupBy(Function(t) t.PaymentMethod) _
                .Select(Function(g)
                            Dim groupTotal = g.Sum(Function(t) t.TotalAmount)
                            Return New PaymentMethodBreakdownDto() With {
                                .PaymentMethod = g.Key,
                                .Count = g.Count(),
                                .Total = groupTotal,
                                .Percentage = If(totalSales > 0D, Math.Round(groupTotal / totalSales * 100D, 2), 0D)
                            }
                        End Function) _
                .OrderByDescending(Function(b) b.Total) _
                .ToList()

            Dim topProducts = transactions _
                .SelectMany(Function(t) t.Lines) _
                .GroupBy(Function(l) New With {Key .Id = l.ProductId, Key .Name = l.ProductName}) _
                .Select(Function(g) New TopProductDto() With {
                    .ProductId = g.Key.Id,
                    .ProductName = g.Key.Name,
                    .QuantitySold = g.Sum(Function(l) l.Quantity),
                    .TotalRevenue = g.Sum(Function(l) l.LineTotal)
                }) _
                .OrderByDescending(Function(p) p.QuantitySold) _
                .Take(5) _
                .ToList()

            Return New DailySummaryDto() With {
                .[Date] = dayStart,
                .TotalSales = totalSales,
                .TransactionCount = txCount,
                .AverageTransactionValue = avgValue,
                .PaymentBreakdown = paymentBreakdown,
                .TopSellingProducts = topProducts,
                .ReturnCount = returns.Count,
                .ReturnValue = returns.Sum(Function(r) r.RefundAmount)
            }
        End Function

        Private Async Function BuildPeriodSummaryAsync(periodStart As DateTime, periodEnd As DateTime) As Task(Of PeriodSummaryDto)
            _buildPeriodTxList = New List(Of SalesTransaction)()
            _buildPeriodRetList = New List(Of SalesReturn)()
            Dim psConnStr = _context.Database.GetConnectionString()
            Using psConn As New SqliteConnection(psConnStr)
                Await psConn.OpenAsync()
                Using psCmd = psConn.CreateCommand()
                    psCmd.CommandText = "SELECT Id, TransactionDate, PaymentMethod, TotalAmount " &
                                         "FROM Pos_SalesTransactions " &
                                         "WHERE TransactionDate >= @start AND TransactionDate <= @end " &
                                         "AND IsVoided = 0 AND IsDeleted = 0"
                    psCmd.Parameters.Add(New SqliteParameter("@start", periodStart.ToString("o")))
                    psCmd.Parameters.Add(New SqliteParameter("@end", periodEnd.ToString("o")))
                    Using psReader = psCmd.ExecuteReader()
                        While psReader.Read()
                            _buildPeriodTxList.Add(New SalesTransaction With {
                                .Id = psReader.GetInt32(0),
                                .TransactionDate = psReader.GetDateTime(1),
                                .PaymentMethod = CType(psReader.GetInt32(2), PaymentMethod),
                                .TotalAmount = psReader.GetDecimal(3)
                            })
                        End While
                    End Using
                End Using
                If _buildPeriodTxList.Count > 0 Then
                    Dim txIds = String.Join(",", _buildPeriodTxList.Select(Function(t) t.Id))
                    Dim lineMap As New Dictionary(Of Integer, List(Of SalesTransactionLine))()
                    Using lCmd = psConn.CreateCommand()
                        lCmd.CommandText = "SELECT TransactionId, ProductId, ProductName, Quantity, LineTotal " &
                                            $"FROM Pos_SalesTransactionLines WHERE TransactionId IN ({txIds})"
                        Using lReader = lCmd.ExecuteReader()
                            While lReader.Read()
                                Dim line As New SalesTransactionLine With {
                                    .TransactionId = lReader.GetInt32(0),
                                    .ProductId = lReader.GetInt32(1),
                                    .ProductName = lReader.GetString(2),
                                    .Quantity = lReader.GetInt32(3),
                                    .LineTotal = lReader.GetDecimal(4)
                                }
                                If Not lineMap.ContainsKey(line.TransactionId) Then lineMap(line.TransactionId) = New List(Of SalesTransactionLine)()
                                lineMap(line.TransactionId).Add(line)
                            End While
                        End Using
                    End Using
                    For Each tx In _buildPeriodTxList
                        Dim txLines As List(Of SalesTransactionLine) = Nothing
                        If lineMap.TryGetValue(tx.Id, txLines) Then
                            For Each ln In txLines : tx.Lines.Add(ln) : Next
                        End If
                    Next
                End If
                Using retCmd = psConn.CreateCommand()
                    retCmd.CommandText = "SELECT ReturnDate, RefundAmount FROM Pos_SalesReturns " &
                                          "WHERE ReturnDate >= @start AND ReturnDate <= @end"
                    retCmd.Parameters.Add(New SqliteParameter("@start", periodStart.ToString("o")))
                    retCmd.Parameters.Add(New SqliteParameter("@end", periodEnd.ToString("o")))
                    Using retReader = retCmd.ExecuteReader()
                        While retReader.Read()
                            _buildPeriodRetList.Add(New SalesReturn With {
                                .ReturnDate = retReader.GetDateTime(0),
                                .RefundAmount = retReader.GetDecimal(1)
                            })
                        End While
                    End Using
                End Using
            End Using
            Dim transactions As List(Of SalesTransaction) = _buildPeriodTxList
            Dim returns As List(Of SalesReturn) = _buildPeriodRetList

            Dim totalSales = transactions.Sum(Function(t) t.TotalAmount)
            Dim txCount = transactions.Count
            Dim avgValue = If(txCount > 0, totalSales / txCount, 0D)

            Dim paymentBreakdown = transactions _
                .GroupBy(Function(t) t.PaymentMethod) _
                .Select(Function(g)
                            Dim groupTotal = g.Sum(Function(t) t.TotalAmount)
                            Return New PaymentMethodBreakdownDto() With {
                                .PaymentMethod = g.Key,
                                .Count = g.Count(),
                                .Total = groupTotal,
                                .Percentage = If(totalSales > 0D, Math.Round(groupTotal / totalSales * 100D, 2), 0D)
                            }
                        End Function) _
                .OrderByDescending(Function(b) b.Total) _
                .ToList()

            Dim topProducts = transactions _
                .SelectMany(Function(t) t.Lines) _
                .GroupBy(Function(l) New With {Key .Id = l.ProductId, Key .Name = l.ProductName}) _
                .Select(Function(g) New TopProductDto() With {
                    .ProductId = g.Key.Id,
                    .ProductName = g.Key.Name,
                    .QuantitySold = g.Sum(Function(l) l.Quantity),
                    .TotalRevenue = g.Sum(Function(l) l.LineTotal)
                }) _
                .OrderByDescending(Function(p) p.QuantitySold) _
                .Take(5) _
                .ToList()

            ' Build per-day breakdown for trend analysis
            Dim txByDate = transactions.ToLookup(Function(t) t.TransactionDate.Date)
            Dim retByDate = returns.ToLookup(Function(r) r.ReturnDate.Date)

            Dim dailyBreakdown As New List(Of DailySummaryDto)()
            Dim cursor = periodStart.Date
            Do While cursor <= periodEnd.Date
                Dim dayTx = txByDate(cursor).ToList()
                Dim dayRet = retByDate(cursor).ToList()
                Dim dayTotal = dayTx.Sum(Function(t) t.TotalAmount)
                Dim dayCount = dayTx.Count

                Dim dayPayBreakdown = dayTx _
                    .GroupBy(Function(t) t.PaymentMethod) _
                    .Select(Function(g)
                                Dim gTotal = g.Sum(Function(t) t.TotalAmount)
                                Return New PaymentMethodBreakdownDto() With {
                                    .PaymentMethod = g.Key,
                                    .Count = g.Count(),
                                    .Total = gTotal,
                                    .Percentage = If(dayTotal > 0D, Math.Round(gTotal / dayTotal * 100D, 2), 0D)
                                }
                            End Function) _
                    .OrderByDescending(Function(b) b.Total) _
                    .ToList()

                Dim dayTopProducts = dayTx _
                    .SelectMany(Function(t) t.Lines) _
                    .GroupBy(Function(l) New With {Key .Id = l.ProductId, Key .Name = l.ProductName}) _
                    .Select(Function(g) New TopProductDto() With {
                        .ProductId = g.Key.Id,
                        .ProductName = g.Key.Name,
                        .QuantitySold = g.Sum(Function(l) l.Quantity),
                        .TotalRevenue = g.Sum(Function(l) l.LineTotal)
                    }) _
                    .OrderByDescending(Function(p) p.QuantitySold) _
                    .Take(5) _
                    .ToList()

                dailyBreakdown.Add(New DailySummaryDto() With {
                    .[Date] = cursor,
                    .TotalSales = dayTotal,
                    .TransactionCount = dayCount,
                    .AverageTransactionValue = If(dayCount > 0, dayTotal / dayCount, 0D),
                    .PaymentBreakdown = dayPayBreakdown,
                    .TopSellingProducts = dayTopProducts,
                    .ReturnCount = dayRet.Count,
                    .ReturnValue = dayRet.Sum(Function(r) r.RefundAmount)
                })

                cursor = cursor.AddDays(1)
            Loop

            Return New PeriodSummaryDto() With {
                .PeriodStart = periodStart,
                .PeriodEnd = periodEnd,
                .TotalSales = totalSales,
                .TransactionCount = txCount,
                .AverageTransactionValue = avgValue,
                .PaymentBreakdown = paymentBreakdown,
                .TopSellingProducts = topProducts,
                .ReturnCount = returns.Count,
                .ReturnValue = returns.Sum(Function(r) r.RefundAmount),
                .DailyBreakdown = dailyBreakdown
            }
        End Function

    End Class

End Namespace
