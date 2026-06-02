Imports System.IO
Imports System.Windows

Namespace Services.Theming

    ''' <summary>
    ''' Implementation of <see cref="IThemeService"/> that hot-swaps theme ResourceDictionaries 
    ''' at runtime and persists user preferences to LocalAppData.
    ''' </summary>
    Public Class ThemeService
        Implements IThemeService

        Private _current As AppTheme = AppTheme.Light

        Public ReadOnly Property Current As AppTheme Implements IThemeService.Current
            Get
                Return _current
            End Get
        End Property

        Public Sub Apply(theme As AppTheme) Implements IThemeService.Apply
            Dim appResources = Application.Current.Resources
            Dim mergedDicts = appResources.MergedDictionaries

            ' Find the existing palette resource dictionary (Light or Dark)
            Dim oldPalette As ResourceDictionary = Nothing
            For Each dict In mergedDicts
                If dict.Source IsNot Nothing Then
                    Dim path = dict.Source.OriginalString
                    If path.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase) OrElse
                       path.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase) Then
                        oldPalette = dict
                        Exit For
                    End If
                End If
            Next

            ' Build the new palette path
            Dim newSource = If(theme = AppTheme.Dark,
                               "pack://application:,,,/Themes/Dark.xaml",
                               "pack://application:,,,/Themes/Light.xaml")

            Dim newPalette As New ResourceDictionary() With {
                .Source = New Uri(newSource, UriKind.Absolute)
            }

            ' Hot-swap the ResourceDictionary
            If oldPalette IsNot Nothing Then
                Dim index = mergedDicts.IndexOf(oldPalette)
                mergedDicts(index) = newPalette
            Else
                mergedDicts.Add(newPalette)
            End If

            _current = theme
            SavePersisted(theme)
        End Sub

        Public Sub Toggle() Implements IThemeService.Toggle
            If _current = AppTheme.Light Then
                Apply(AppTheme.Dark)
            Else
                Apply(AppTheme.Light)
            End If
        End Sub

        Public Function LoadPersisted() As AppTheme Implements IThemeService.LoadPersisted
            Try
                Dim folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MerchSys")
                Dim filePath = Path.Combine(folderPath, "ui-settings.json")
                
                If File.Exists(filePath) Then
                    Dim json = File.ReadAllText(filePath)
                    If json.Contains("""Dark""", StringComparison.OrdinalIgnoreCase) Then
                        Return AppTheme.Dark
                    End If
                End If
            Catch ex As Exception
                ' Fail-safe: fallback to default if settings are corrupted or inaccessible
            End Try
            Return AppTheme.Light
        End Function

        ''' <summary>
        ''' Persists the selected theme synchronously to `%LOCALAPPDATA%\MerchSys\ui-settings.json`.
        ''' </summary>
        Private Sub SavePersisted(theme As AppTheme)
            Try
                Dim folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MerchSys")
                If Not Directory.Exists(folderPath) Then
                    Directory.CreateDirectory(folderPath)
                End If

                Dim filePath = Path.Combine(folderPath, "ui-settings.json")
                ' Write a simple, mini JSON file to avoid external library dependencies
                Dim json = $"{{""theme"": ""{theme}""}}"
                File.WriteAllText(filePath, json)
            Catch ex As Exception
                ' Fail-safe: ignore or log error to prevent application crashes during settings write
            End Try
        End Sub

    End Class

End Namespace
