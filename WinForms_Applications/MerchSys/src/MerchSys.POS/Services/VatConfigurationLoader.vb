Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities

Namespace Services

    ''' <summary>
    ''' Singleton cache for the <see cref="VatConfiguration"/> singleton row (Id = 1).
    ''' Uses <see cref="IServiceScopeFactory"/> to open a short-lived scope each time a fresh
    ''' load is needed, avoiding the scoped-into-singleton anti-pattern.
    ''' Call <see cref="InvalidateAsync"/> after any configuration update so the next request
    ''' picks up the new values.
    ''' </summary>
    Public Class VatConfigurationLoader

        Private ReadOnly _scopeFactory As IServiceScopeFactory
        Private _cached As VatConfiguration
        Private ReadOnly _lock As New SemaphoreSlim(1, 1)

        Public Sub New(scopeFactory As IServiceScopeFactory)
            _scopeFactory = scopeFactory
        End Sub

        ''' <summary>
        ''' Returns the cached <see cref="VatConfiguration"/> row, loading from the database
        ''' on the first call or after <see cref="InvalidateAsync"/> has been called.
        ''' </summary>
        Public Async Function GetAsync() As Task(Of VatConfiguration)
            If _cached IsNot Nothing Then Return _cached

            Await _lock.WaitAsync()
            Try
                If _cached IsNot Nothing Then Return _cached

                Using scope = _scopeFactory.CreateScope()
                    Dim ctx = scope.ServiceProvider.GetRequiredService(Of POSDbContext)()
                    _cached = Await ctx.VatConfigurations.
                        AsNoTracking().
                        FirstOrDefaultAsync(Function(v) v.Id = 1)
                End Using

                Return _cached
            Finally
                _lock.Release()
            End Try
        End Function

        ''' <summary>Clears the in-memory cache so the next call to <see cref="GetAsync"/> re-reads from the database.</summary>
        Public Sub Invalidate()
            _lock.Wait()
            Try
                _cached = Nothing
            Finally
                _lock.Release()
            End Try
        End Sub

    End Class

End Namespace
