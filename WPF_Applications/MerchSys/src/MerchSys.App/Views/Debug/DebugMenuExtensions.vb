#If DEBUG Then
' Developer-only debug panel. Not reachable in Release builds. See ACC-13 for harness source.
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports Microsoft.Extensions.Hosting
Imports MerchSys.Accounting.Debug
Imports MerchSys.POS.Tests

''' <summary>
''' Holds the IHost reference so debug harnesses (running inside MerchSys.App) can resolve
''' production services without coupling harness code directly to the Application class.
''' Only populated in Debug builds; always Nothing in Release.
''' </summary>
Public Module DebugHostHolder
    Public Property CurrentHost As IHost
End Module

Namespace Views.Debug

    ''' Code-only UserControl that hosts the developer debug panel.
    ''' Wired to the "Developer Tools" navigation group only in Debug configuration (ACC-17).
    Public Class DebugMenuView
        Inherits UserControl

        Public Sub New()
            Dim root As New StackPanel() With {
                .Margin = New Thickness(32)
            }

            root.Children.Add(New TextBlock() With {
                .Text = "Developer Tools",
                .FontSize = 22,
                .FontWeight = FontWeights.SemiBold,
                .Margin = New Thickness(0, 0, 0, 4)
            })
            root.Children.Add(New TextBlock() With {
                .Text = "These options are only available in Debug builds and will not appear in production.",
                .Foreground = Brushes.Gray,
                .FontSize = 12,
                .Margin = New Thickness(0, 0, 0, 28)
            })

            Dim separator As New Separator() With {
                .Margin = New Thickness(0, 0, 0, 24)
            }
            root.Children.Add(separator)

            root.Children.Add(New TextBlock() With {
                .Text = "VAT Ledger Schema Verification",
                .FontSize = 15,
                .FontWeight = FontWeights.SemiBold,
                .Margin = New Thickness(0, 0, 0, 6)
            })
            root.Children.Add(New TextBlock() With {
                .Text = "Runs the four ACC-13 schema checks against an isolated SQLite database " &
                        "and writes a Markdown report to %TEMP%.",
                .Foreground = Brushes.DimGray,
                .FontSize = 12,
                .TextWrapping = TextWrapping.Wrap,
                .MaxWidth = 520,
                .Margin = New Thickness(0, 0, 0, 12)
            })

            Dim vatBtn As New Button() With {
                .Content = "Run VAT Schema Harness",
                .Padding = New Thickness(18, 8, 18, 8),
                .FontSize = 13,
                .HorizontalAlignment = HorizontalAlignment.Left,
                .Tag = "idle"
            }
            ' Development use only — invokes the VAT ledger schema verification harness (ACC-13).
            AddHandler vatBtn.Click, AddressOf RunVatSchemaHarness_Click
            root.Children.Add(vatBtn)

            root.Children.Add(New Separator() With {.Margin = New Thickness(0, 24, 0, 24)})

            root.Children.Add(New TextBlock() With {
                .Text = "VAT Tile Smoke Test",
                .FontSize = 15,
                .FontWeight = FontWeights.SemiBold,
                .Margin = New Thickness(0, 0, 0, 6)
            })
            root.Children.Add(New TextBlock() With {
                .Text = "Seeds synthetic VAT ledger data (OutputVat ₱12,000 / InputVat ₱3,000 ⇒ payable ₱9,000) " &
                        "into an isolated scratch database, calls the decorated IFinancialOverviewService, " &
                        "and confirms VatReturnView is registered in DI (ACC-14 / ACC-16).",
                .Foreground = Brushes.DimGray,
                .FontSize = 12,
                .TextWrapping = TextWrapping.Wrap,
                .MaxWidth = 520,
                .Margin = New Thickness(0, 0, 0, 12)
            })

            Dim smokeBtn As New Button() With {
                .Content = "Run VAT Tile Smoke Harness",
                .Padding = New Thickness(18, 8, 18, 8),
                .FontSize = 13,
                .HorizontalAlignment = HorizontalAlignment.Left
            }
            AddHandler smokeBtn.Click, AddressOf RunVatTileSmokeHarness_Click
            root.Children.Add(smokeBtn)

            root.Children.Add(New Separator() With {.Margin = New Thickness(0, 24, 0, 24)})

            root.Children.Add(New TextBlock() With {
                .Text = "Event Chain Verification",
                .FontSize = 15,
                .FontWeight = FontWeights.SemiBold,
                .Margin = New Thickness(0, 0, 0, 6)
            })
            root.Children.Add(New TextBlock() With {
                .Text = "Runs both GoodsReceived and SaleCompleted event chains against an isolated " &
                        "scratch SQLite database and writes a Markdown report to %TEMP% (INT-12).",
                .Foreground = Brushes.DimGray,
                .FontSize = 12,
                .TextWrapping = TextWrapping.Wrap,
                .MaxWidth = 520,
                .Margin = New Thickness(0, 0, 0, 12)
            })

            Dim chainBtn As New Button() With {
                .Content = "Run Event Chain Harness",
                .Padding = New Thickness(18, 8, 18, 8),
                .FontSize = 13,
                .HorizontalAlignment = HorizontalAlignment.Left
            }
            AddHandler chainBtn.Click, AddressOf RunEventChainHarness_Click
            root.Children.Add(chainBtn)

            root.Children.Add(New Separator() With {.Margin = New Thickness(0, 24, 0, 24)})

            root.Children.Add(New TextBlock() With {
                .Text = "POS Receipt Sequence Concurrency (POS-13)",
                .FontSize = 15,
                .FontWeight = FontWeights.SemiBold,
                .Margin = New Thickness(0, 0, 0, 6)
            })
            root.Children.Add(New TextBlock() With {
                .Text = "Fires 1000 parallel GetNextReceiptNumberAsync calls against a temp scratch database " &
                        "and asserts no duplicate or missing sequence numbers.",
                .Foreground = Brushes.DimGray,
                .FontSize = 12,
                .TextWrapping = TextWrapping.Wrap,
                .MaxWidth = 520,
                .Margin = New Thickness(0, 0, 0, 12)
            })

            Dim posSeqBtn As New Button() With {
                .Content = "Run POS Sequence Concurrency Harness",
                .Padding = New Thickness(18, 8, 18, 8),
                .FontSize = 13,
                .HorizontalAlignment = HorizontalAlignment.Left
            }
            AddHandler posSeqBtn.Click, AddressOf RunPosSequenceHarness_Click
            root.Children.Add(posSeqBtn)

            Content = root
        End Sub

        Private Async Sub RunVatSchemaHarness_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = DirectCast(sender, Button)
            btn.IsEnabled = False
            btn.Content = "Running…"

            Dim runError As Exception = Nothing
            Try
                ' IHost parameter is not used at runtime; runner instantiates its own scratch context.
                Await VatLedgerSchemaHarnessRunner.RunAndReportAsync(Nothing)
            Catch ex As Exception
                runError = ex
            End Try

            btn.IsEnabled = True
            btn.Content = "Run VAT Schema Harness"

            If runError Is Nothing Then
                MessageBox.Show(
                    "VAT Schema Harness completed. Markdown report written to %TEMP%." &
                    Environment.NewLine & Environment.NewLine &
                    "Check the Output / Console window for the exact file path.",
                    "VAT Schema Harness Results",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information)
            Else
                MessageBox.Show(
                    $"Harness run failed: {runError.GetType().Name}: {runError.Message}",
                    "VAT Schema Harness Results",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error)
            End If
        End Sub

        Private Async Sub RunVatTileSmokeHarness_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = DirectCast(sender, Button)
            btn.IsEnabled = False
            btn.Content = "Running…"

            Dim report As VatTileSmokeReport = Nothing
            Dim runError As Exception = Nothing
            Try
                report = Await VatTileSmokeHarness.RunAsync(DebugHostHolder.CurrentHost)
            Catch ex As Exception
                runError = ex
            End Try

            btn.IsEnabled = True
            btn.Content = "Run VAT Tile Smoke Harness"

            If runError IsNot Nothing Then
                MessageBox.Show(
                    $"Harness run failed: {runError.GetType().Name}: {runError.Message}" &
                    Environment.NewLine & Environment.NewLine & runError.ToString(),
                    "VAT Tile Smoke Harness",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error)
            Else
                Dim vatOk = report.ComputedVatPayable = 9000D
                Dim navOk = report.NavigationRouteFound
                Dim summary = $"ComputedVatPayable = {report.ComputedVatPayable:N2}  {If(vatOk, "✅", "❌")}" &
                              Environment.NewLine &
                              $"NavigationRouteFound = {report.NavigationRouteFound}  {If(navOk, "✅", "❌")}" &
                              Environment.NewLine & Environment.NewLine &
                              "Markdown report written to %TEMP%."
                Dim icon = If(vatOk AndAlso navOk, MessageBoxImage.Information, MessageBoxImage.Warning)
                MessageBox.Show(summary, "VAT Tile Smoke Harness", MessageBoxButton.OK, icon)
            End If
        End Sub

        Private Async Sub RunEventChainHarness_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = DirectCast(sender, Button)
            btn.IsEnabled = False
            btn.Content = "Running…"

            Dim reportPath As String = Nothing
            Dim overallPassed As Boolean = False
            Dim runError As Exception = Nothing
            Try
                Dim harness As New MerchSys.App.Debug.EventChainVerificationHarness(Nothing)
                Dim goodsResult = Await harness.VerifyGoodsReceivedChainAsync()
                Dim saleResult = Await harness.VerifySaleCompletedChainAsync()
                reportPath = Await MerchSys.App.Debug.EventChainReport.WriteAsync(goodsResult, saleResult)
                overallPassed = goodsResult.Passed AndAlso saleResult.Passed
                MerchSys.App.Debug.EventChainReport.NotifyCompletion(reportPath, overallPassed)
                Await harness.CleanupAsync()
            Catch ex As Exception
                runError = ex
            End Try

            btn.IsEnabled = True
            btn.Content = "Run Event Chain Harness"

            If runError IsNot Nothing Then
                MessageBox.Show(
                    $"Harness run failed: {runError.GetType().Name}: {runError.Message}",
                    "Event Chain Harness",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error)
            End If
        End Sub

        Private Async Sub RunPosSequenceHarness_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = DirectCast(sender, Button)
            btn.IsEnabled = False
            btn.Content = "Running… (1000 calls)"

            Dim output As String = String.Empty
            Dim runError As Exception = Nothing
            Dim sw As New IO.StringWriter()
            Dim prevOut = System.Console.Out
            System.Console.SetOut(sw)
            Try
                Await Pos_SequenceConcurrencyHarness.RunAsync()
            Catch ex As Exception
                runError = ex
            Finally
                System.Console.SetOut(prevOut)
                output = sw.ToString()
            End Try

            btn.IsEnabled = True
            btn.Content = "Run POS Sequence Concurrency Harness"

            If runError IsNot Nothing Then
                MessageBox.Show(
                    $"Harness run failed: {runError.GetType().Name}: {runError.Message}" &
                    Environment.NewLine & Environment.NewLine & runError.ToString(),
                    "POS Sequence Concurrency Harness",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error)
            Else
                Dim passed = output.Contains("[PASS]")
                Dim icon = If(passed, MessageBoxImage.Information, MessageBoxImage.Warning)
                MessageBox.Show(output,
                    "POS Sequence Concurrency Harness",
                    MessageBoxButton.OK,
                    icon)
            End If
        End Sub

    End Class

End Namespace
#End If
