Namespace Services

    ''' <summary>
    ''' Provides static access to the app-level IConnectionHealthMonitor singleton.
    ''' Set once in Application_Startup after the host is built.
    ''' Used by DisableOnOfflineBehavior which cannot use constructor injection.
    ''' </summary>
    Friend Module ConnectionHealthMonitorLocator
        Friend Property Current As IConnectionHealthMonitor
    End Module

End Namespace
