Imports System.Collections.Generic
Imports MerchSys.SharedKernel.Enums

Namespace Exceptions

    ''' <summary>
    ''' Exception thrown when an unauthorized write is attempted (e.g. by a user in the Owner role)
    ''' at the data layer, violating OWASP DA5.
    ''' </summary>
    Public Class UnauthorizedWriteException
        Inherits UnauthorizedAccessException

        Public Sub New(userRole As UserRole, targetTables As IReadOnlyList(Of String))
            MyBase.New($"Role '{userRole}' is not permitted to write to: {String.Join(", ", targetTables)}.")
            Me.Role = userRole
            Me.TableNames = targetTables
        End Sub

        Public ReadOnly Property Role As UserRole
        Public ReadOnly Property TableNames As IReadOnlyList(Of String)

    End Class

End Namespace
