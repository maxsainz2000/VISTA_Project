Imports MerchSys.App.Models

Namespace Services

    ''' <summary>
    ''' Persists per-laptop user preferences (last-view, favorites, recents) through the shared
    ''' UiSettingsStore, which writes a single ui-settings.json alongside theme and window placement.
    ''' Stable key: NavigationItem.ViewType.FullName (e.g. "MerchSys.App.Views.Inventory.StockDashboardView").
    ''' A stale key that no longer resolves in AllNavigableItems is silently skipped.
    ''' </summary>
    Public Class UserPreferencesService
        Implements IUserPreferencesService

        Private Const MaxRecents As Integer = 10

        Private ReadOnly _store As UiSettingsStore

        Public Sub New(store As UiSettingsStore)
            _store = store
        End Sub

        ' ── Last-view ─────────────────────────────────────────────────────────

        Public Function GetLastViewKey() As String Implements IUserPreferencesService.GetLastViewKey
            Return _store.LastViewKey
        End Function

        Public Sub RecordNavigation(item As NavigationItem) Implements IUserPreferencesService.RecordNavigation
            If item Is Nothing OrElse item.ViewType Is Nothing Then Return
            Dim viewKey = item.ViewType.FullName

            ' Update last-view key
            _store.LastViewKey = viewKey

            ' Update recents: de-dup then prepend
            _store.RecentKeys.Remove(viewKey)
            _store.RecentKeys.Insert(0, viewKey)
            If _store.RecentKeys.Count > MaxRecents Then
                _store.RecentKeys.RemoveAt(_store.RecentKeys.Count - 1)
            End If

            _store.Save()
        End Sub

        ' ── Favorites ─────────────────────────────────────────────────────────

        Public Function IsFavorite(item As NavigationItem) As Boolean Implements IUserPreferencesService.IsFavorite
            If item Is Nothing OrElse item.ViewType Is Nothing Then Return False
            Return _store.FavoriteKeys.Contains(item.ViewType.FullName)
        End Function

        Public Sub ToggleFavorite(item As NavigationItem) Implements IUserPreferencesService.ToggleFavorite
            If item Is Nothing OrElse item.ViewType Is Nothing Then Return
            Dim viewKey = item.ViewType.FullName
            If _store.FavoriteKeys.Contains(viewKey) Then
                _store.FavoriteKeys.Remove(viewKey)
                item.IsFavorite = False
            Else
                _store.FavoriteKeys.Add(viewKey)
                item.IsFavorite = True
            End If
            _store.Save()
        End Sub

        Public Function GetFavorites(allItems As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))) As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem)) Implements IUserPreferencesService.GetFavorites
            Dim result As New List(Of ([Module] As AppModule, Item As NavigationItem))()
            For Each favKey In _store.FavoriteKeys
                Dim matched = allItems.FirstOrDefault(
                    Function(n) n.Item IsNot Nothing AndAlso
                                n.Item.ViewType IsNot Nothing AndAlso
                                n.Item.ViewType.FullName = favKey)
                If matched.Item IsNot Nothing Then result.Add(matched)
            Next
            Return result
        End Function

        ' ── Recents ───────────────────────────────────────────────────────────

        Public Function GetRecents(allItems As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))) As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem)) Implements IUserPreferencesService.GetRecents
            Dim result As New List(Of ([Module] As AppModule, Item As NavigationItem))()
            For Each recKey In _store.RecentKeys
                Dim matched = allItems.FirstOrDefault(
                    Function(n) n.Item IsNot Nothing AndAlso
                                n.Item.ViewType IsNot Nothing AndAlso
                                n.Item.ViewType.FullName = recKey)
                If matched.Item IsNot Nothing Then result.Add(matched)
            Next
            Return result
        End Function

        ' ── Flag sync ─────────────────────────────────────────────────────────

        Public Sub SyncFavoriteFlagsToItems(items As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))) Implements IUserPreferencesService.SyncFavoriteFlagsToItems
            For Each pair In items
                If pair.Item IsNot Nothing AndAlso pair.Item.ViewType IsNot Nothing Then
                    pair.Item.IsFavorite = _store.FavoriteKeys.Contains(pair.Item.ViewType.FullName)
                End If
            Next
        End Sub

    End Class

End Namespace
