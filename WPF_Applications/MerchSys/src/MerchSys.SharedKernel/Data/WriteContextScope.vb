Imports System
Imports System.Threading
Imports MerchSys.SharedKernel.Interfaces

Namespace Data

    ''' <summary>
    ''' Implementation of <see cref="IWriteContextScope"/> using <see cref="AsyncLocal(Of Frame)"/>
    ''' to track the current write context kind and asserted self-service username across async boundaries.
    ''' NOTE: AsyncLocal is required because ThreadLocal or ThreadStatic will break when Await continuations
    ''' resume on the WPF dispatcher or background threads.
    ''' </summary>
    Public Class WriteContextScope
        Implements IWriteContextScope

        Private Shared ReadOnly CurrentFrame As New AsyncLocal(Of Frame)()

        Private Class Frame
            Public ReadOnly Kind As WriteContextKind
            Public ReadOnly SelfServiceUsername As String

            Public Sub New(kind As WriteContextKind, selfServiceUsername As String)
                Me.Kind = kind
                Me.SelfServiceUsername = selfServiceUsername
            End Sub
        End Class

        Public ReadOnly Property Current As WriteContextKind Implements IWriteContextScope.Current
            Get
                Dim f = CurrentFrame.Value
                Return If(f?.Kind, WriteContextKind.User)
            End Get
        End Property

        Public ReadOnly Property SelfServiceUsername As String Implements IWriteContextScope.SelfServiceUsername
            Get
                Dim f = CurrentFrame.Value
                Return If(f?.SelfServiceUsername, String.Empty)
            End Get
        End Property

        Public Function Enter(kind As WriteContextKind, Optional selfServiceUsername As String = Nothing) As IDisposable Implements IWriteContextScope.Enter
            Dim previous = CurrentFrame.Value
            Dim nextFrame = New Frame(kind, selfServiceUsername)
            CurrentFrame.Value = nextFrame
            Return New ScopeDisposable(previous)
        End Function

        Private Class ScopeDisposable
            Implements IDisposable

            Private ReadOnly _previous As Frame
            Private _disposed As Boolean = False

            Public Sub New(previous As Frame)
                _previous = previous
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                If Not _disposed Then
                    CurrentFrame.Value = _previous
                    _disposed = True
                End If
            End Sub
        End Class

    End Class

End Namespace
