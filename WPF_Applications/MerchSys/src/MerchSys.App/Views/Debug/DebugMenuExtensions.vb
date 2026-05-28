#If DEBUG Then
' Developer-only debug panel. Not reachable in Release builds.
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports MerchSys.POS.Services

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
    ''' Wired to the "Developer Tools" navigation group only in Debug configuration.
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
                .Text = "POS VatConfigurationLoader Cache Check",
                .FontSize = 15,
                .FontWeight = FontWeights.SemiBold,
                .Margin = New Thickness(0, 0, 0, 6)
            })
            root.Children.Add(New TextBlock() With {
                .Text = "Resolves the singleton VatConfigurationLoader from DI, calls GetAsync(), " &
                        "and displays the returned VatConfiguration fields. " &
                        "Run after saving changes in VAT Settings to confirm the cache returns updated values.",
                .Foreground = Brushes.DimGray,
                .FontSize = 12,
                .TextWrapping = TextWrapping.Wrap,
                .MaxWidth = 520,
                .Margin = New Thickness(0, 0, 0, 12)
            })

            Dim vatLoaderBtn As New Button() With {
                .Content = "Run VatConfigurationLoader Check",
                .Padding = New Thickness(18, 8, 18, 8),
                .FontSize = 13,
                .HorizontalAlignment = HorizontalAlignment.Left
            }
            AddHandler vatLoaderBtn.Click, AddressOf RunVatConfigLoaderCheck_Click
            root.Children.Add(vatLoaderBtn)

            Content = New ScrollViewer() With {
                .Content = root,
                .VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            }
        End Sub

        Private Async Sub RunVatConfigLoaderCheck_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = DirectCast(sender, Button)
            btn.IsEnabled = False
            btn.Content = "Running…"

            Dim config As MerchSys.POS.Entities.VatConfiguration = Nothing
            Dim runError As Exception = Nothing
            Try
                Dim loader = DebugHostHolder.CurrentHost.Services.GetRequiredService(Of VatConfigurationLoader)()
                config = Await loader.GetAsync()
            Catch ex As Exception
                runError = ex
            End Try

            btn.IsEnabled = True
            btn.Content = "Run VatConfigurationLoader Check"

            If runError IsNot Nothing Then
                MessageBox.Show(
                    $"Check failed: {runError.GetType().Name}: {runError.Message}" &
                    Environment.NewLine & Environment.NewLine & runError.ToString(),
                    "VatConfigurationLoader Check",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error)
                Return
            End If

            If config Is Nothing Then
                MessageBox.Show(
                    "GetAsync() returned Nothing — no VatConfiguration row found (Id = 1 missing).",
                    "VatConfigurationLoader Check",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning)
                Return
            End If

            Dim summary =
                $"IsVatRegistered         = {config.IsVatRegistered}" & Environment.NewLine &
                $"VatRate                 = {config.VatRate} ({config.VatRate * 100D:N0}%)" & Environment.NewLine &
                $"NonVatPercentageTaxRate = {config.NonVatPercentageTaxRate} ({config.NonVatPercentageTaxRate * 100D:N0}%)" & Environment.NewLine &
                $"BusinessTIN             = {If(String.IsNullOrEmpty(config.BusinessTIN), "(empty)", config.BusinessTIN)}" & Environment.NewLine &
                $"BusinessName            = {config.BusinessName}" & Environment.NewLine &
                $"BusinessAddress         = {If(String.IsNullOrEmpty(config.BusinessAddress), "(empty)", config.BusinessAddress)}" & Environment.NewLine &
                $"EffectiveFrom           = {config.EffectiveFrom:yyyy-MM-dd HH:mm:ss} UTC" & Environment.NewLine &
                $"ModifiedAt              = {config.ModifiedAt:yyyy-MM-dd HH:mm:ss}"

            MessageBox.Show(summary, "VatConfigurationLoader.GetAsync() Result", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

    End Class

End Namespace
#End If
