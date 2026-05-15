#If DEBUG Then

Imports System.IO
Imports Microsoft.Extensions.Hosting

Namespace Debug

    ''' <summary>
    ''' Thin static entry point for the VAT ledger schema verification harness (ACC-13).
    ''' Intended to be wired to a developer-only menu item in <c>MerchSys.App</c>; must not
    ''' be reachable from release builds.
    ''' </summary>
    Public Module VatLedgerSchemaHarnessRunner

        ''' <summary>
        ''' Runs all four schema checks and writes a Markdown report to
        ''' <c>%TEMP%\vat-ledger-schema-report-&lt;timestamp&gt;.md</c>.
        ''' Does not throw on check failure; the Markdown report is the deliverable.
        ''' Toast notification (pass/fail count) should be surfaced by the calling
        ''' <c>MerchSys.App</c> layer after this method returns, using
        ''' <c>Notification.Wpf</c> which is not available from <c>MerchSys.Accounting</c>.
        ''' </summary>
        ''' <param name="host">The application host; not used in this implementation but kept
        ''' in the signature for symmetry with other debug runners and future DI resolution.</param>
        Public Async Function RunAndReportAsync(host As IHost) As Task
            ' Harness creates its own scratch contexts; factory arg is not called at runtime.
            Dim harness As New VatLedgerSchemaHarness(Nothing)
            Dim report = Await harness.RunAllAsync()

            Dim checks = {
                ("Check 1 — Fresh Database Apply", report.FreshDatabaseApplyResult),
                ("Check 2 — Existing Database Apply (Idempotency)", report.ExistingDatabaseApplyResult),
                ("Check 3 — Duplicate Filing Blocked", report.DuplicateFilingBlockedResult),
                ("Check 4 — Cascade Delete", report.CascadeDeleteResult)
            }

            Dim passCount = checks.Count(Function(t) t.Item2 IsNot Nothing AndAlso t.Item2.Passed)

            Dim timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
            Dim reportPath = Path.Combine(Path.GetTempPath(), $"vat-ledger-schema-report-{timestamp}.md")

            Dim lines As New List(Of String) From {
                "# VAT Ledger Schema Verification Report (ACC-13)",
                "",
                $"**Run:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
                $"**Result:** {passCount}/4 checks passed",
                ""
            }

            For Each entry In checks
                Dim title = entry.Item1
                Dim result = entry.Item2
                Dim status = If(result IsNot Nothing AndAlso result.Passed, "PASS", "FAIL")
                lines.Add($"## {title}")
                lines.Add($"**Status:** {status}  ")
                lines.Add($"**Duration:** {result?.DurationMs} ms  ")
                lines.Add($"**Detail:** {result?.Detail}")
                lines.Add("")
            Next

            lines.AddRange({
                "## PRAGMA Queries Used",
                "",
                "| Query | Purpose |",
                "|---|---|",
                "| `PRAGMA table_info('Acc_VatReturns')` | Enumerate columns (cid, name, type, notnull, dflt_value, pk) |",
                "| `PRAGMA table_info('Acc_VatReturnLines')` | Enumerate line table columns |",
                "| `PRAGMA index_list('Acc_VatReturns')` | List indexes (seq, name, unique, origin, partial) |",
                "| `PRAGMA foreign_key_list('Acc_VatReturnLines')` | Confirm ON DELETE CASCADE is declared (id, seq, table, from, to, on_update, on_delete, match) |",
                "| `SELECT MigrationId FROM __EFMigrationsHistory` | Verify each migration applied exactly once (idempotency) |"
            })

            Await File.WriteAllLinesAsync(reportPath, lines)

            Console.WriteLine(
                $"[VatLedgerSchemaHarnessRunner] {passCount}/4 passed. Report: {reportPath}")
        End Function

    End Module

End Namespace

#End If
