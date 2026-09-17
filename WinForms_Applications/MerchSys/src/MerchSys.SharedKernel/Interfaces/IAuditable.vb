Namespace Interfaces

    ''' <summary>
    ''' Marks an entity as auditable, tracking who created and last modified it.
    ''' </summary>
    Public Interface IAuditable

        ''' <summary>Username of the user who created this record.</summary>
        Property CreatedBy As String

        ''' <summary>UTC timestamp when this record was created.</summary>
        Property CreatedAt As DateTime

        ''' <summary>Username of the user who last modified this record.</summary>
        Property ModifiedBy As String

        ''' <summary>UTC timestamp of the last modification; Nothing if never modified.</summary>
        Property ModifiedAt As DateTime?

    End Interface

End Namespace
