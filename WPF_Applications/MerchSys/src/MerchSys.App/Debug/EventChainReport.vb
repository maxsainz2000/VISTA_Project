#If DEBUG Then
Imports System.IO
Imports System.Text
Imports Notification.Wpf

Namespace Debug

    ''' <summary>
    ''' Builds and writes the Markdown event-chain verification report.
    ''' Output path: <c>%TEMP%\event-chain-report-&lt;timestamp&gt;.md</c>.
    ''' Surfaces a <see cref="NotificationManager"/> toast with the report path on completion
    ''' so the operator can open the file directly from the desktop notification.
    ''' </summary>
    Public Class EventChainReport

        ''' <summary>
        ''' Assembles the Markdown report body from the two chain results.
        ''' </summary>
        Public Shared Function BuildMarkdown(goodsResult As ChainVerificationResult,
                                             saleResult As ChainVerificationResult) As String
            Dim sb As New StringBuilder()
            sb.AppendLine("# Event Chain Verification Report")
            sb.AppendLine()
            sb.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            Dim overallPassed = goodsResult.Passed AndAlso saleResult.Passed
            sb.AppendLine($"**Overall:** {If(overallPassed, "PASS ✅", "FAIL ❌")}")
            sb.AppendLine()
            AppendChainSection(sb, goodsResult)
            AppendChainSection(sb, saleResult)
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Writes the report to <c>%TEMP%\event-chain-report-&lt;timestamp&gt;.md</c> and
        ''' returns the full file path.
        ''' </summary>
        Public Shared Async Function WriteAsync(goodsResult As ChainVerificationResult,
                                                saleResult As ChainVerificationResult) As Task(Of String)
            Dim timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss")
            Dim reportPath = Path.Combine(Path.GetTempPath(),
                $"event-chain-report-{timestamp}.md")
            Await File.WriteAllTextAsync(reportPath, BuildMarkdown(goodsResult, saleResult))
            Return reportPath
        End Function

        ''' <summary>
        ''' Shows a <see cref="NotificationManager"/> desktop toast with the report path.
        ''' Must be called from the WPF UI thread (typically the continuation after
        ''' <c>Await harness.CleanupAsync()</c> in a button-click handler).
        ''' </summary>
        Public Shared Sub NotifyCompletion(reportPath As String, passed As Boolean)
            Dim manager As New NotificationManager()
            manager.Show(New NotificationContent() With {
                .Title = If(passed, "Event Chain: PASS ✅", "Event Chain: FAIL ❌"),
                .Message = $"Report: {reportPath}",
                .Type = If(passed, NotificationType.Success, NotificationType.Error)
            })
        End Sub

        ' ── Private helpers ──────────────────────────────────────────────────────────────

        Private Shared Sub AppendChainSection(sb As StringBuilder,
                                              result As ChainVerificationResult)
            sb.AppendLine($"## {result.ChainName} chain")
            sb.AppendLine($"- Publisher fired: {Icon(result.PublisherFired)}")
            sb.AppendLine($"- Handler executed: {Icon(result.HandlerExecuted)}")
            sb.AppendLine($"- Stock movement row: {Icon(result.StockMovementRowFound)} " &
                          $"(Type={result.ActualMovementType})")
            sb.AppendLine($"- Duration: {result.DurationMs} ms")
            If Not String.IsNullOrEmpty(result.Detail) Then
                sb.AppendLine($"- Detail: {result.Detail}")
            End If
            sb.AppendLine()
        End Sub

        Private Shared Function Icon(value As Boolean) As String
            Return If(value, "✅", "❌")
        End Function

    End Class

End Namespace
#End If
