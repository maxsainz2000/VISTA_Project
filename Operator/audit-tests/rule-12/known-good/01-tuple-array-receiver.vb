' Rule 12 known-good: .Count(predicate) on a tuple array — not a List(Of T)
' Arrays expose Length, not Count, so .Count(pred) unambiguously resolves to Enumerable.Count.
' Source: VatLedgerSchemaHarnessRunner.vb:37 (verified false positive in 2026-05-24 audit)

Namespace Tests
    Public Class SchemaHarnessRunner
        Public Sub Run()
            Dim checks = {
                ("Pos_Receipts table exists", True),
                ("Pos_ReceiptItems table exists", True),
                ("Acc_VatLedger table exists", True),
                ("Acc_VatLedgerItems table exists", False)
            }
            Dim passCount = checks.Count(Function(t) t.Item2 = True)
            Dim failCount = checks.Count(Function(t) t.Item2 = False)
            System.Console.WriteLine($"Pass: {passCount}, Fail: {failCount}")
        End Sub
    End Class
End Namespace
