Namespace Exceptions

    ''' <summary>
    ''' Thrown when an attempt is made to modify or delete an entity that is immutable
    ''' under BIR regulations (NIRC §235 — 10-year tamper-proof receipt preservation).
    ''' Message includes entity type, primary key, and the BIR rule reference.
    ''' </summary>
    Public Class ImmutableEntityException
        Inherits InvalidOperationException

        ''' <summary>
        ''' Initializes the exception with the entity type and primary key.
        ''' </summary>
        Public Sub New(entityType As Type, primaryKey As Object)
            MyBase.New($"Entity '{entityType.Name}' (PK={primaryKey}) is immutable under NIRC §235 (BIR 10-year receipt retention). Modifications and deletions are prohibited.")
        End Sub

    End Class

End Namespace
