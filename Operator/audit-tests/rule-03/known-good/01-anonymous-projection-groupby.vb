' Rule 3 known-good: GroupBy + Select anonymous projection
' Chain contains .Select(Function(g) New With {...}) before .ToListAsync() — excluded by rule.
' Source: FinancialOverviewService.vb:90 (verified false positive in 2026-05-24 audit)

Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class ExampleService
        Private _db As ExampleDbContext

        Public Async Function GetMonthlyTrendAsync() As Task(Of List(Of Object))
            Dim result = Await _db.Records _
                .Where(Function(r) r.RecordDate >= DateTime.UtcNow.AddDays(-90)) _
                .GroupBy(Function(r) New With {Key .Year = r.Year, Key .Month = r.Month}) _
                .Select(Function(g) New With {
                    Key .Year = g.Key.Year,
                    Key .Month = g.Key.Month,
                    Key .Revenue = g.Sum(Function(r) r.NetAmount)
                }) _
                .OrderBy(Function(g) g.Year).ThenBy(Function(g) g.Month) _
                .ToListAsync()
            Return result.Cast(Of Object)().ToList()
        End Function
    End Class
End Namespace
