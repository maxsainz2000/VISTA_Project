Imports MerchSys.App.Models

Namespace Services

    Public Interface IUserPreferencesService

        ''' <summary>Returns the ViewType.FullName key of the last navigated screen, or String.Empty.</summary>
        Function GetLastViewKey() As String

        ''' <summary>
        ''' Persists the last-viewed screen key and pushes the item to the front of the recents list.
        ''' Combines both writes into a single disk save.
        ''' </summary>
        Sub RecordNavigation(item As NavigationItem)

        ''' <summary>Returns favorited pairs resolved against the current navigable item list, in pin order.</summary>
        Function GetFavorites(allItems As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))) As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))

        ''' <summary>Returns recently-visited pairs resolved against the current navigable item list, most-recent-first.</summary>
        Function GetRecents(allItems As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))) As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))

        ''' <summary>Pins or unpins the item and persists the change.</summary>
        Sub ToggleFavorite(item As NavigationItem)

        ''' <summary>Returns True if the item's ViewType key is in the favorites list.</summary>
        Function IsFavorite(item As NavigationItem) As Boolean

        ''' <summary>
        ''' Stamps IsFavorite on every NavigationItem in the collection from the persisted favorites list.
        ''' Call after rebuilding AllNavigableItems so the sidebar star icons reflect saved state.
        ''' </summary>
        Sub SyncFavoriteFlagsToItems(items As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem)))

    End Interface

End Namespace
