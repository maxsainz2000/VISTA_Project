Namespace Sync

    ''' <summary>
    ''' Checks whether the offline-first sync preconditions are satisfied.
    ''' Both the network adapter must be up AND the central MariaDB server must be reachable
    ''' on its configured TCP port before any data movement is attempted.
    ''' See concepts/offline-first-sync.md for the dual-condition rule.
    ''' </summary>
    Public Interface ISyncProbe

        Function ProbeAsync() As Task(Of SyncProbeResult)

    End Interface

End Namespace
