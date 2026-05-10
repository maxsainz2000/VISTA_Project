Namespace Sync

    Public Class SyncProbeResult

        Public Property NetworkAvailable As Boolean
        Public Property ServerReachable As Boolean
        Public Property LatencyMs As Integer?
        Public Property ProbedAt As DateTime
        ''' <summary>Exception type name if the probe failed; Nothing on success.</summary>
        Public Property [Error] As String

        Public ReadOnly Property IsHealthy As Boolean
            Get
                Return NetworkAvailable AndAlso ServerReachable
            End Get
        End Property

    End Class

End Namespace
