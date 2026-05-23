#If DEBUG Then
Imports System.Collections.Concurrent
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging.Abstractions
Imports MediatR
Imports MerchSys.POS.Data
Imports MerchSys.POS.Services

Namespace Tests

    ''' <summary>
    ''' Debug-only concurrency harness that verifies <see cref="ReceiptIntegrityService.GetNextReceiptNumberAsync"/>
    ''' produces unique, sequential receipt numbers with no gaps under 1000 parallel callers.
    ''' <para>
    ''' This module is excluded from Release builds via the surrounding <c>#If DEBUG Then</c> block.
    ''' Invoke <see cref="RunAsync"/> from a console app or interactive session pointing at a scratch database.
    ''' </para>
    ''' </summary>
    Friend Module Pos_SequenceConcurrencyHarness

        ''' <summary>
        ''' Runs 1000 parallel <c>GetNextReceiptNumberAsync</c> calls against a temporary
        ''' scratch database and asserts the results are unique and form a contiguous sequence.
        ''' Invoke this overload from the Immediate Window: <c>? Await Pos_SequenceConcurrencyHarness.RunAsync()</c>
        ''' </summary>
        Public Async Function RunAsync() As Task
            Dim tempDb = IO.Path.Combine(IO.Path.GetTempPath(), $"harness_{Guid.NewGuid():N}.db")
            Try
                Await RunAsync($"Data Source={tempDb}")
            Finally
                If IO.File.Exists(tempDb) Then IO.File.Delete(tempDb)
            End Try
        End Function

        ''' <summary>
        ''' Runs 1000 parallel <c>GetNextReceiptNumberAsync</c> calls and asserts the results
        ''' are unique and form a contiguous sequence 1-1000.
        ''' </summary>
        ''' <param name="connectionString">SQLite connection string for the scratch database.</param>
        Public Async Function RunAsync(connectionString As String) As Task
            Const CallCount As Integer = 1000
            Dim year = DateTime.UtcNow.Year

            Console.WriteLine($"[Harness] Starting {CallCount} parallel GetNextReceiptNumberAsync calls for year {year}...")

            Dim results As New ConcurrentBag(Of String)()
            Dim tasks = Enumerable.Range(0, CallCount).
                Select(Function(i)
                           Return Task.Run(Async Function()
                                               Dim options = New DbContextOptionsBuilder(Of POSDbContext)().
                                                   UseSqlite(connectionString).
                                                   Options
                                               Using ctx = New POSDbContext(options)
                                                   Await ctx.Database.EnsureCreatedAsync()
                                                   Dim svc = New ReceiptIntegrityService(
                                                       ctx,
                                                       DirectCast(Nothing, IMediator),
                                                       NullLogger(Of ReceiptIntegrityService).Instance)
                                                   Dim number = Await svc.GetNextReceiptNumberAsync(year)
                                                   results.Add(number)
                                               End Using
                                           End Function)
                       End Function).
                ToArray()

            Await Task.WhenAll(tasks)

            Dim sorted = results.Order().ToArray()
            Dim distinct = results.Distinct().Count()

            Console.WriteLine($"[Harness] Generated {results.Count} numbers, {distinct} unique.")

            If distinct <> CallCount Then
                Console.WriteLine($"[FAIL] Expected {CallCount} unique numbers, got {distinct}.")
                Return
            End If

            Dim expected = Enumerable.Range(1, CallCount).
                Select(Function(n) $"OR-{year:D4}-{n:D4}").
                ToHashSet()
            Dim missing = expected.Except(results).ToList()

            If missing.Count > 0 Then
                Console.WriteLine($"[FAIL] {missing.Count} expected numbers missing from results.")
                For Each m In missing.Take(10)
                    Console.WriteLine($"  Missing: {m}")
                Next
            Else
                Console.WriteLine($"[PASS] All {CallCount} numbers are unique and form a contiguous sequence.")
            End If
        End Function

    End Module

End Namespace
#End If
