Namespace Presentation

    ''' <summary>
    ''' Marks a ViewModel or component as trackable for data freshness.
    ''' </summary>
    Public Interface IFreshnessAware

        ''' <summary>The timestamp when the data was last successfully loaded.</summary>
        Property LastLoadedAt As DateTime?

    End Interface

End Namespace
