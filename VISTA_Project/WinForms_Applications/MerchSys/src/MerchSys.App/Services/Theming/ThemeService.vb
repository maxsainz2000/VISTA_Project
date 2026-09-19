Imports System.Windows
Imports MerchSys.App.Services

Namespace Services.Theming

    ''' <summary>
    ''' Implementation of <see cref="IThemeService"/> that hot-swaps theme ResourceDictionaries
    ''' at runtime and persists user preferences to LocalAppData via UiSettingsStore.
    ''' </summary>
    Public Class ThemeService
        Implements IThemeService

        Private ReadOnly _store As UiSettingsStore
        Private _current As AppTheme = AppTheme.Light

        Public Sub New(store As UiSettingsStore)
            _store = store
        End Sub

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
                If _store.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase) Then
                    Return AppTheme.Dark
                End If
            Catch
                ' Fail-safe: fallback to default if store is in an unexpected state
            End Try
            Return AppTheme.Light
        End Function

        Private Sub SavePersisted(theme As AppTheme)
            Try
                _store.Theme = theme.ToString()
                _store.Save()
            Catch
                ' Fail-safe: ignore errors to prevent crashes during settings write
            End Try
        End Sub

    End Class

End Namespace
