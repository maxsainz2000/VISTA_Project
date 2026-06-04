Imports System.IO
Imports System.Text.Json

Namespace Services

    ''' <summary>
    ''' Singleton store for all per-laptop UI preferences persisted to
    ''' %LOCALAPPDATA%\MerchSys\ui-settings.json.  Both ThemeService and
    ''' WindowPlacementService read and write through this class so neither
    ''' save ever clobbers the other's keys.
    ''' </summary>
    Public Class UiSettingsStore

        Private Shared ReadOnly _folderPath As String =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MerchSys")

        Private Shared ReadOnly _filePath As String =
            Path.Combine(_folderPath, "ui-settings.json")

        ' ── In-memory state (defaults = first-run values) ────────────────────

        ''' <summary>"Light" or "Dark".</summary>
        Public Property Theme As String = "Light"

        ''' <summary>Double.NaN signals no saved placement → caller uses first-run defaults.</summary>
        Public Property WindowLeft As Double = Double.NaN
        Public Property WindowTop As Double = Double.NaN
        Public Property WindowWidth As Double = 1280.0
        Public Property WindowHeight As Double = 720.0
        Public Property WindowMaximized As Boolean = True

        Public Sub New()
            LoadFromDisk()
        End Sub

        ' ── Disk I/O ─────────────────────────────────────────────────────────

        Private Sub LoadFromDisk()
            Try
                If Not File.Exists(_filePath) Then Return

                Dim json = File.ReadAllText(_filePath)
                Using doc = JsonDocument.Parse(json)
                    Dim root = doc.RootElement
                    Dim prop As JsonElement

                    If root.TryGetProperty("theme", prop) AndAlso prop.ValueKind = JsonValueKind.String Then
                        Theme = If(prop.GetString(), "Light")
                    End If

                    If root.TryGetProperty("windowLeft", prop) AndAlso prop.ValueKind = JsonValueKind.Number Then
                        WindowLeft = prop.GetDouble()
                    End If

                    If root.TryGetProperty("windowTop", prop) AndAlso prop.ValueKind = JsonValueKind.Number Then
                        WindowTop = prop.GetDouble()
                    End If

                    If root.TryGetProperty("windowWidth", prop) AndAlso prop.ValueKind = JsonValueKind.Number Then
                        WindowWidth = prop.GetDouble()
                    End If

                    If root.TryGetProperty("windowHeight", prop) AndAlso prop.ValueKind = JsonValueKind.Number Then
                        WindowHeight = prop.GetDouble()
                    End If

                    If root.TryGetProperty("windowMaximized", prop) AndAlso
                       (prop.ValueKind = JsonValueKind.True OrElse prop.ValueKind = JsonValueKind.False) Then
                        WindowMaximized = prop.GetBoolean()
                    End If
                End Using
            Catch
                ' Fail-safe: corrupt or inaccessible file → keep defaults
            End Try
        End Sub

        ''' <summary>
        ''' Writes all current property values to disk. Called by ThemeService and
        ''' WindowPlacementService after updating their respective properties.
        ''' </summary>
        Public Sub Save()
            Try
                If Not Directory.Exists(_folderPath) Then
                    Directory.CreateDirectory(_folderPath)
                End If

                Dim inv = System.Globalization.CultureInfo.InvariantCulture
                Dim leftStr = If(Double.IsNaN(WindowLeft), "null", WindowLeft.ToString("G", inv))
                Dim topStr = If(Double.IsNaN(WindowTop), "null", WindowTop.ToString("G", inv))
                Dim widthStr = WindowWidth.ToString("G", inv)
                Dim heightStr = WindowHeight.ToString("G", inv)
                Dim maxStr = If(WindowMaximized, "true", "false")
                Dim themeEsc = Theme.Replace("\", "\\").Replace("""", "\""")

                Dim json = $"{{""theme"":""{themeEsc}"",""windowLeft"":{leftStr},""windowTop"":{topStr},""windowWidth"":{widthStr},""windowHeight"":{heightStr},""windowMaximized"":{maxStr}}}"
                File.WriteAllText(_filePath, json)
            Catch
                ' Fail-safe: ignore write errors to avoid crashing on close
            End Try
        End Sub

    End Class

End Namespace
