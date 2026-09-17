Namespace Entities

    ''' <summary>
    ''' Root base class for all domain entities. Provides the integer primary key used by EF Core.
    ''' Use this directly only for simple lookup/reference entities that require no audit trail.
    ''' </summary>
    Public MustInherit Class BaseEntity

        ''' <summary>Auto-increment integer primary key managed by EF Core.</summary>
        Public Property Id As Integer

    End Class

End Namespace
