Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data

Namespace Services

    Public Class DailySummaryService
        Implements IDailySummaryService

        Private ReadOnly _context As POSDbContext

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
            Dim transactions = Await _context.SalesTransactions _
                .Include(Function(t) t.Lines) _
                .Where(Function(t) t.TransactionDate >= dayStart _
                               AndAlso t.TransactionDate <= dayEnd _
                               AndAlso Not t.IsVoided _
                               AndAlso Not t.IsDeleted) _
                .ToListAsync()

            Dim returns = Await _context.SalesReturns _
                .Where(Function(r) r.ReturnDate >= dayStart AndAlso r.ReturnDate <= dayEnd) _
                .ToListAsync()

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
            Dim transactions = Await _context.SalesTransactions _
                .Include(Function(t) t.Lines) _
                .Where(Function(t) t.TransactionDate >= periodStart _
                               AndAlso t.TransactionDate <= periodEnd _
                               AndAlso Not t.IsVoided _
                               AndAlso Not t.IsDeleted) _
                .ToListAsync()

            Dim returns = Await _context.SalesReturns _
                .Where(Function(r) r.ReturnDate >= periodStart AndAlso r.ReturnDate <= periodEnd) _
                .ToListAsync()

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
