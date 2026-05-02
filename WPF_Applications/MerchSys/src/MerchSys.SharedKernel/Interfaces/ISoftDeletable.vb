Namespace Interfaces

    ''' <summary>
    ''' Marks an entity as soft-deletable, preserving the record in storage while hiding it from active queries.
    ''' </summary>
    Public Interface ISoftDeletable

        ''' <summary>True when this record has been soft-deleted and should be excluded from normal queries.</summary>
        Property IsDeleted As Boolean

        ''' <summary>Username of the user who deleted this record; Nothing if not deleted.</summary>
        Property DeletedBy As String

        ''' <summary>UTC timestamp when this record was soft-deleted; Nothing if not deleted.</summary>
        Property DeletedAt As DateTime?

    End Interface

End Namespace
