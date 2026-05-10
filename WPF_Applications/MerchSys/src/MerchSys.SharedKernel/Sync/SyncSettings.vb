Namespace Sync

    Public Class SyncSettings

        Public Property CentralServerHost As String = "192.168.1.10"
        Public Property CentralServerPort As Integer = 3306
        Public Property ProbeIntervalSeconds As Integer = 30
        Public Property ProbeTimeoutMs As Integer = 2000

        ''' <summary>Pomelo connection string for the central MariaDB instance.</summary>
        Public Property MariaDbConnection As String = ""

        ''' <summary>Maximum sync attempts before a journal entry is permanently skipped.</summary>
        Public Property MaxAttempts As Integer = 5

        ''' <summary>Maximum journal entries processed per module per sync cycle.</summary>
        Public Property BatchSize As Integer = 100

    End Class

End Namespace
