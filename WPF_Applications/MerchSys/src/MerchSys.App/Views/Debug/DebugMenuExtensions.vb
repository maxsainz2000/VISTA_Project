#If DEBUG Then
' Developer-only debug panel. Not reachable in Release builds. See ACC-13 for harness source.
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports MerchSys.Accounting.Debug

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

    End Class

End Namespace
#End If
