Imports System.IO
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.Logging

Namespace Configuration

    ''' <summary>
    ''' Locates and validates the user-level production configuration overlay for MariaDB.
    ''' Three-state outcome:
    '''   Absent      — overlay file not found at %LOCALAPPDATA%\VISTA\appsettings.Production.json;
    '''                 SyncWorker runs in local-only mode.
    '''   Placeholder — file found but MariaDb.Password is still the sentinel value;
    '''                 treated as absent, actionable warning logged.
    '''   Valid       — file found with real credentials; connection string returned.
    ''' </summary>
    Public Module ConnectionStringLoader

        Private Const PlaceholderPassword As String = "<replace-with-real-password>"

        ''' <summary>Returns the OS-aware path where the operator must place the production config.</summary>
        Public Function GetProductionConfigPath() As String
            Return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VISTA",
                "appsettings.Production.json")
        End Function

        ''' <summary>
        ''' Adds the production overlay JSON file to the configuration builder when it exists.
        ''' Call from ConfigureAppConfiguration so the overlay is visible to all services.
        ''' </summary>
        <System.Runtime.CompilerServices.Extension>
        Public Sub AddProductionOverlay(cfg As IConfigurationBuilder)
            cfg.AddJsonFile(GetProductionConfigPath(), optional:=True, reloadOnChange:=False)
        End Sub

        ''' <summary>
        ''' Reads the MariaDb section from the merged IConfiguration and returns a Pomelo-compatible
        ''' connection string, or Nothing when the overlay is absent or still contains the placeholder.
        ''' </summary>
        Public Function GetMariaDbConnectionString(cfg As IConfiguration, logger As ILogger) As String
            Dim password = cfg("MariaDb:Password")

            If String.IsNullOrWhiteSpace(password) Then
                logger.LogWarning(
                    "MariaDb production config absent. Place the filled-in template at {Path}. " &
                    "SyncWorker is running in local-only mode.",
                    GetProductionConfigPath())
                Return Nothing
            End If

            If password = PlaceholderPassword Then
                logger.LogWarning(
                    "MariaDb production config contains the placeholder password. " &
                    "Open {Path}, replace the Password value with the real credential, and restart. " &
                    "SyncWorker is running in local-only mode.",
                    GetProductionConfigPath())
                Return Nothing
            End If

            Dim host = cfg("MariaDb:Host")
            Dim port = cfg("MariaDb:Port")
            Dim database = cfg("MariaDb:Database")
            Dim user = cfg("MariaDb:User")
            Dim sslMode = cfg("MariaDb:SslMode")

            Return $"Server={host};Port={port};Database={database};Uid={user};Pwd={password};SslMode={sslMode};"
        End Function

    End Module

End Namespace
