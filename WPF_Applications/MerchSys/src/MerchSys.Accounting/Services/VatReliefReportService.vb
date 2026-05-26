' Raw SqliteConnection is used instead of EF Core ToListAsync() because EF Core 10 + VB.NET
' silently returns an empty list for full entity queries.
' See: agent_wiki/errors/efcore10-vbnet-tolistasync-empty.md

Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Accounting.Data

Namespace Services

    Public Class VatReliefReportService
        Implements IVatReliefReportService

        Private ReadOnly _db As AccountingDbContext

        Public Sub New(db As AccountingDbContext)
            _db = db
        End Sub

        Public Async Function GetMonthlySummaryAsync(year As Integer, month As Integer) As Task(Of VatReliefSummary) _
            Implements IVatReliefReportService.GetMonthlySummaryAsync

            Dim windowStart = New DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc)
            Dim windowEnd = windowStart.AddMonths(1)
            Return Await BuildSummaryAsync(year, month, windowStart, windowEnd)
        End Function

        Public Async Function GetTrailingMonthsAsync(endYear As Integer, endMonth As Integer, months As Integer) As Task(Of IReadOnlyList(Of VatReliefSummary)) _
            Implements IVatReliefReportService.GetTrailingMonthsAsync

            months = Math.Max(1, Math.Min(24, months))

            Dim result As New List(Of VatReliefSummary)(months)
            Dim cursor = New DateTime(endYear, endMonth, 1, 0, 0, 0, DateTimeKind.Utc)
            Dim startCursor = cursor.AddMonths(-(months - 1))

            Dim current = startCursor
            While current <= cursor
                Dim summary = Await GetMonthlySummaryAsync(current.Year, current.Month)
                result.Add(summary)
                current = current.AddMonths(1)
            End While

            Return result.AsReadOnly()
        End Function

        Private Async Function BuildSummaryAsync(year As Integer, month As Integer,
                                                  windowStart As DateTime,
                                                  windowEnd As DateTime) As Task(Of VatReliefSummary)
            Dim wsStr = windowStart.ToString("o")
            Dim weStr = windowEnd.ToString("o")
            Dim connStr = _db.Database.GetConnectionString()

            Dim vatableSales As Decimal = 0D
            Dim vatExemptSales As Decimal = 0D
            Dim zeroRatedSales As Decimal = 0D
            Dim outputVat As Decimal = 0D
            Dim salesCount As Integer = 0

            Dim vatablePurchases As Decimal = 0D
            Dim vatExemptPurchases As Decimal = 0D
            Dim zeroRatedPurchases As Decimal = 0D
            Dim inputVat As Decimal = 0D
            Dim purchaseCount As Integer = 0

            Using conn As New SqliteConnection(connStr)
                Await conn.OpenAsync()

                Using revCmd = conn.CreateCommand()
                    revCmd.CommandText =
                        "SELECT SUM(VatableAmount), SUM(VatExemptAmount), SUM(ZeroRatedAmount), " &
                        "SUM(OutputVat), COUNT(*) " &
                        "FROM Acc_RevenueRecords " &
                        "WHERE RecordDate >= @ws AND RecordDate < @we"
                    revCmd.Parameters.Add(New SqliteParameter("@ws", wsStr))
                    revCmd.Parameters.Add(New SqliteParameter("@we", weStr))
                    Using revReader = revCmd.ExecuteReader()
                        If revReader.Read() Then
                            vatableSales = If(revReader.IsDBNull(0), 0D, revReader.GetDecimal(0))
                            vatExemptSales = If(revReader.IsDBNull(1), 0D, revReader.GetDecimal(1))
                            zeroRatedSales = If(revReader.IsDBNull(2), 0D, revReader.GetDecimal(2))
                            outputVat = If(revReader.IsDBNull(3), 0D, revReader.GetDecimal(3))
                            salesCount = If(revReader.IsDBNull(4), 0, revReader.GetInt32(4))
                        End If
                    End Using
                End Using

                Using expCmd = conn.CreateCommand()
                    expCmd.CommandText =
                        "SELECT SUM(VatableAmount), SUM(VatExemptAmount), SUM(ZeroRatedAmount), " &
                        "SUM(InputVat), COUNT(*) " &
                        "FROM Acc_ExpenseRecords " &
                        "WHERE RecordDate >= @ws AND RecordDate < @we"
                    expCmd.Parameters.Add(New SqliteParameter("@ws", wsStr))
                    expCmd.Parameters.Add(New SqliteParameter("@we", weStr))
                    Using expReader = expCmd.ExecuteReader()
                        If expReader.Read() Then
                            vatablePurchases = If(expReader.IsDBNull(0), 0D, expReader.GetDecimal(0))
                            vatExemptPurchases = If(expReader.IsDBNull(1), 0D, expReader.GetDecimal(1))
                            zeroRatedPurchases = If(expReader.IsDBNull(2), 0D, expReader.GetDecimal(2))
                            inputVat = If(expReader.IsDBNull(3), 0D, expReader.GetDecimal(3))
                            purchaseCount = If(expReader.IsDBNull(4), 0, expReader.GetInt32(4))
                        End If
                    End Using
                End Using
            End Using

            ' Banker's rounding applied at the DTO boundary, not in SQL
            vatableSales = Math.Round(vatableSales, 2, MidpointRounding.ToEven)
            vatExemptSales = Math.Round(vatExemptSales, 2, MidpointRounding.ToEven)
            zeroRatedSales = Math.Round(zeroRatedSales, 2, MidpointRounding.ToEven)
            outputVat = Math.Round(outputVat, 2, MidpointRounding.ToEven)
            vatablePurchases = Math.Round(vatablePurchases, 2, MidpointRounding.ToEven)
            vatExemptPurchases = Math.Round(vatExemptPurchases, 2, MidpointRounding.ToEven)
            zeroRatedPurchases = Math.Round(zeroRatedPurchases, 2, MidpointRounding.ToEven)
            inputVat = Math.Round(inputVat, 2, MidpointRounding.ToEven)

            Dim totalGrossSales = vatableSales + vatExemptSales + zeroRatedSales + outputVat
            Dim totalGrossPurchases = vatablePurchases + vatExemptPurchases + zeroRatedPurchases + inputVat
            Dim netVatPayable = Math.Round(outputVat - inputVat, 2, MidpointRounding.ToEven)

            Return New VatReliefSummary With {
                .Year = year,
                .Month = month,
                .VatableSales = vatableSales,
                .VatExemptSales = vatExemptSales,
                .ZeroRatedSales = zeroRatedSales,
                .OutputVat = outputVat,
                .TotalGrossSales = totalGrossSales,
                .VatablePurchases = vatablePurchases,
                .VatExemptPurchases = vatExemptPurchases,
                .ZeroRatedPurchases = zeroRatedPurchases,
                .InputVat = inputVat,
                .TotalGrossPurchases = totalGrossPurchases,
                .NetVatPayable = netVatPayable,
                .SalesRecordCount = salesCount,
                .PurchaseRecordCount = purchaseCount
            }
        End Function

    End Class

End Namespace
