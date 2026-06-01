Imports Microsoft.Extensions.DependencyInjection
Imports System.Runtime.CompilerServices

Namespace Startup

    ''' Registers developer-only debug views. The views are gated at the UI layer to the
    ''' Developer role; this registration is build-configuration independent.
    Public Module DebugServiceRegistration

        <Extension>
        Public Sub AddDebugServices(services As IServiceCollection)
            services.AddTransient(Of Views.Debug.DebugMenuView)()
        End Sub

    End Module

End Namespace
