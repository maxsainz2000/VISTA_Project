Imports MerchSys.SharedKernel.Interfaces

Namespace Entities

    ''' <summary>
    ''' Base class for entities that require both an audit trail and soft-delete support.
    ''' This is the most common base class — all financial and inventory records inherit from this.
    ''' Inherits <see cref="AuditableEntity"/> and implements <see cref="ISoftDeletable"/>.
    ''' </summary>
    Public MustInherit Class SoftDeletableEntity
        Inherits AuditableEntity
        Implements ISoftDeletable

        ''' <summary>True when this record has been soft-deleted and should be excluded from normal queries.</summary>
        Public Property IsDeleted As Boolean = False Implements ISoftDeletable.IsDeleted

        ''' <summary>Username of the user who deleted this record; Nothing if not deleted.</summary>
        Public Property DeletedBy As String Implements ISoftDeletable.DeletedBy

        ''' <summary>UTC timestamp when this record was soft-deleted; Nothing if not deleted.</summary>
        Public Property DeletedAt As DateTime? Implements ISoftDeletable.DeletedAt

    End Class

End Namespace
