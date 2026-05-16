#If DEBUG Then
Imports Microsoft.Extensions.DependencyInjection
Imports System.Runtime.CompilerServices

Namespace Startup

    ''' Registers developer-only debug views. Must only be called in Debug configuration (ACC-17).
    Public Module DebugServiceRegistration

        <Extension>
        Public Sub AddDebugServices(services As IServiceCollection)
            services.AddTransient(Of Views.Debug.DebugMenuView)()
        End Sub

    End Module

End Namespace
#End If
