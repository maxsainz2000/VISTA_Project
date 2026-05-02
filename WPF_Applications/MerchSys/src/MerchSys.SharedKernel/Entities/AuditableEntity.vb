Imports MerchSys.SharedKernel.Interfaces

Namespace Entities

    ''' <summary>
    ''' Base class for entities that require an audit trail but do not support soft deletion.
    ''' Inherits <see cref="BaseEntity"/> and implements <see cref="IAuditable"/>.
    ''' </summary>
    Public MustInherit Class AuditableEntity
        Inherits BaseEntity
        Implements IAuditable

        ''' <summary>Username of the user who created this record.</summary>
        Public Property CreatedBy As String Implements IAuditable.CreatedBy

        ''' <summary>UTC timestamp when this record was created.</summary>
        Public Property CreatedAt As DateTime Implements IAuditable.CreatedAt

        ''' <summary>Username of the user who last modified this record.</summary>
        Public Property ModifiedBy As String Implements IAuditable.ModifiedBy

        ''' <summary>UTC timestamp of the last modification; Nothing if never modified.</summary>
        Public Property ModifiedAt As DateTime? Implements IAuditable.ModifiedAt

    End Class

End Namespace
