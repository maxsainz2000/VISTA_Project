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

        ''' <summary>ViewType.FullName of the last-navigated screen. Empty = no saved view.</summary>
        Public Property LastViewKey As String = String.Empty

        ''' <summary>Ordered list of pinned ViewType.FullName keys, in pin order.</summary>
        Public Property FavoriteKeys As List(Of String) = New List(Of String)()

        ''' <summary>Bounded list of recently-visited ViewType.FullName keys, most-recent-first.</summary>
        Public Property RecentKeys As List(Of String) = New List(Of String)()

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

                    If root.TryGetProperty("lastViewKey", prop) AndAlso prop.ValueKind = JsonValueKind.String Then
                        LastViewKey = If(prop.GetString(), String.Empty)
                    End If

                    If root.TryGetProperty("favoriteKeys", prop) AndAlso prop.ValueKind = JsonValueKind.Array Then
                        FavoriteKeys = New List(Of String)()
                        For Each el In prop.EnumerateArray()
                            If el.ValueKind = JsonValueKind.String Then
                                Dim sk = el.GetString()
                                If Not String.IsNullOrEmpty(sk) Then FavoriteKeys.Add(sk)
                            End If
                        Next
                    End If

                    If root.TryGetProperty("recentKeys", prop) AndAlso prop.ValueKind = JsonValueKind.Array Then
                        RecentKeys = New List(Of String)()
                        For Each el In prop.EnumerateArray()
                            If el.ValueKind = JsonValueKind.String Then
                                Dim sk = el.GetString()
                                If Not String.IsNullOrEmpty(sk) Then RecentKeys.Add(sk)
                            End If
                        Next
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
                Dim themeEsc = EscapeJsonString(Theme)
                Dim lastViewEsc = EscapeJsonString(LastViewKey)
                Dim favArr = SerializeStringArray(FavoriteKeys)
                Dim recArr = SerializeStringArray(RecentKeys)

                Dim json = $"{{""theme"":""{themeEsc}"",""windowLeft"":{leftStr},""windowTop"":{topStr},""windowWidth"":{widthStr},""windowHeight"":{heightStr},""windowMaximized"":{maxStr},""lastViewKey"":""{lastViewEsc}"",""favoriteKeys"":{favArr},""recentKeys"":{recArr}}}"
                File.WriteAllText(_filePath, json)
            Catch
                ' Fail-safe: ignore write errors to avoid crashing on close
            End Try
        End Sub

        ' Escapes only the two characters that can appear in our stored values
        ' (backslash and quote). All values written here are type FullNames and the
        ' theme name ("Light"/"Dark") — none contain control characters — so the
        ' minimal escaper is sufficient. Do not reuse for arbitrary user text.
        Private Shared Function EscapeJsonString(s As String) As String
            Return s.Replace("\", "\\").Replace("""", "\""")
        End Function

        Private Shared Function SerializeStringArray(items As List(Of String)) As String
            If items.Count = 0 Then Return "[]"
            Dim parts = items.Select(Function(s) $"""{EscapeJsonString(s)}""")
            Return "[" & String.Join(",", parts) & "]"
        End Function

    End Class

End Namespace
