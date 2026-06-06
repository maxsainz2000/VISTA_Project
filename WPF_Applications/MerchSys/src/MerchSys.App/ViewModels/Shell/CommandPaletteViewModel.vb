Imports System.Collections.ObjectModel
Imports System.Threading
Imports System.Windows.Media
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MediatR
Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.App.Models
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Queries
Imports MerchSys.Inventory.ViewModels

Namespace ViewModels.Shell

    Public Class CommandPaletteViewModel
        Inherits ObservableObject

        Private ReadOnly _services As IServiceProvider
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _prefs As Services.IUserPreferencesService
        Private _searchCts As CancellationTokenSource

        Private _isOpen As Boolean
        Public Property IsOpen As Boolean
            Get
                Return _isOpen
            End Get
            Set(value As Boolean)
                If SetProperty(_isOpen, value) Then
                    If value Then
                        Query = String.Empty
                        Results.Clear()
                        Dim t = RunSearchAsync(String.Empty, CancellationToken.None)
                    Else
                        ' Cleanup on close
                        If _searchCts IsNot Nothing Then
                            _searchCts.Cancel()
                            _searchCts.Dispose()
                            _searchCts = Nothing
                        End If
                    End If
                End If
            End Set
        End Property

        Private _query As String = String.Empty
        Public Property Query As String
            Get
                Return _query
            End Get
            Set(value As String)
                If SetProperty(_query, value) Then
                    OnQueryChanged(value)
                End If
            End Set
        End Property

        Public Property Results As ObservableCollection(Of CommandPaletteItem)

        Private _selectedIndex As Integer = -1
        Public Property SelectedIndex As Integer
            Get
                Return _selectedIndex
            End Get
            Set(value As Integer)
                SetProperty(_selectedIndex, value)
            End Set
        End Property

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                If SetProperty(_isBusy, value) Then
                    OnPropertyChanged(NameOf(IsEmpty))
                End If
            End Set
        End Property

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return Results.Count = 0 AndAlso Not IsBusy
            End Get
        End Property

        Public ReadOnly Property OpenCommand As RelayCommand
        Public ReadOnly Property CloseCommand As RelayCommand
        Public ReadOnly Property ExecuteSelectedCommand As RelayCommand

        Public Sub New(services As IServiceProvider, mediator As IMediator,
                       prefs As Services.IUserPreferencesService)
            _services = services
            _mediator = mediator
            _prefs = prefs
            Results = New ObservableCollection(Of CommandPaletteItem)()

            OpenCommand = New RelayCommand(Sub() IsOpen = True)
            CloseCommand = New RelayCommand(Sub() IsOpen = False)
            ExecuteSelectedCommand = New RelayCommand(AddressOf ExecuteSelected)
        End Sub

        Public Sub MoveSelectionUp()
            If Results.Count = 0 Then Return
            If SelectedIndex <= 0 Then
                SelectedIndex = Results.Count - 1
            Else
                SelectedIndex -= 1
            End If
        End Sub

        Public Sub MoveSelectionDown()
            If Results.Count = 0 Then Return
            If SelectedIndex >= Results.Count - 1 Then
                SelectedIndex = 0
            Else
                SelectedIndex += 1
            End If
        End Sub

        Private Async Sub OnQueryChanged(newQuery As String)
            ' Cancel any pending search
            If _searchCts IsNot Nothing Then
                _searchCts.Cancel()
                _searchCts.Dispose()
            End If

            _searchCts = New CancellationTokenSource()
            Dim token = _searchCts.Token

            Try
                ' Debounce delay of 250ms
                Await Task.Delay(250, token)
                Await RunSearchAsync(newQuery, token)
            Catch ex As TaskCanceledException
                ' Ignored
            Catch ex As Exception
                ' Suppressed
            End Try
        End Sub

        Private Async Function RunSearchAsync(searchTerm As String, token As CancellationToken) As Task
            IsBusy = True
            Try
                Dim matchedItems As New List(Of CommandPaletteItem)()
                Dim mainVm = _services.GetRequiredService(Of MainWindowViewModel)()
                Dim term = searchTerm.Trim().ToLower()

                ' Resolve static geometries from Application.Current.Resources
                Dim boxIcon = TryCast(Application.Current.TryFindResource("IconBoxGeometry"), Geometry)
                Dim chartBarIcon = TryCast(Application.Current.TryFindResource("IconChartBarGeometry"), Geometry)
                Dim cashIcon = TryCast(Application.Current.TryFindResource("IconCashGeometry"), Geometry)
                Dim chartLineIcon = TryCast(Application.Current.TryFindResource("IconChartLineGeometry"), Geometry)
                Dim boltIcon = TryCast(Application.Current.TryFindResource("IconBoltGeometry"), Geometry)
                Dim starIcon = TryCast(Application.Current.TryFindResource("IconStarGeometry"), Geometry)

                ' 0. Zero-state accelerators: Favorites and Recents (empty query only).
                ' Keys surfaced here are skipped in the full "Screens" list below so the same
                ' screen is never shown twice in the zero-state palette.
                Dim acceleratorKeys As New HashSet(Of String)()
                If String.IsNullOrWhiteSpace(term) Then
                    Dim lastKey = _prefs.GetLastViewKey()

                    Dim favPairs = _prefs.GetFavorites(mainVm.AllNavigableItems)
                    For Each fp In favPairs
                        matchedItems.Add(New CommandPaletteItem With {
                            .DisplayName = fp.Item.DisplayName,
                            .Subtitle = "Favorite · " & GetModuleName(fp.[Module]),
                            .Section = "Favorites",
                            .IconData = starIcon,
                            .TargetItem = fp.Item
                        })
                        acceleratorKeys.Add(fp.Item.ViewType.FullName)
                    Next

                    Dim recentPairs = _prefs.GetRecents(mainVm.AllNavigableItems)
                    For Each rp In recentPairs
                        ' Skip the screen the user is already on (always recents[0]) and anything
                        ' already pinned as a favorite — no point echoing it back as "Recent".
                        Dim recKey = rp.Item.ViewType.FullName
                        If recKey = lastKey OrElse acceleratorKeys.Contains(recKey) Then Continue For
                        matchedItems.Add(New CommandPaletteItem With {
                            .DisplayName = rp.Item.DisplayName,
                            .Subtitle = "Recent · " & GetModuleName(rp.[Module]),
                            .Section = "Recent",
                            .IconData = GetModuleIcon(rp.[Module]),
                            .TargetItem = rp.Item
                        })
                        acceleratorKeys.Add(recKey)
                    Next
                End If

                ' 1. Search screens from AllNavigableItems
                For Each navigable In mainVm.AllNavigableItems
                    Dim matches = False
                    If String.IsNullOrWhiteSpace(term) Then
                        ' Zero-state: list every screen except those already shown as a Favorite/Recent.
                        matches = Not acceleratorKeys.Contains(navigable.Item.ViewType.FullName)
                    Else
                        Dim dispName = navigable.Item.DisplayName.ToLower()
                        Dim moduleName = GetModuleName(navigable.[Module]).ToLower()
                        If dispName.Contains(term) OrElse moduleName.Contains(term) Then
                            matches = True
                        End If
                    End If

                    If matches Then
                        Dim icon = boxIcon
                        Select Case navigable.[Module]
                            Case AppModule.Purchasing : icon = boxIcon
                            Case AppModule.Inventory : icon = chartBarIcon
                            Case AppModule.POS : icon = cashIcon
                            Case AppModule.Accounting : icon = chartLineIcon
                            Case AppModule.DeveloperTools : icon = boltIcon
                        End Select

                        matchedItems.Add(New CommandPaletteItem With {
                            .DisplayName = navigable.Item.DisplayName,
                            .Subtitle = "Screen in " & GetModuleName(navigable.[Module]),
                            .Section = "Screens",
                            .IconData = icon,
                            .TargetItem = navigable.Item
                        })
                    End If
                Next

                ' 2. Search products asynchronously via MediatR (only if term is not empty)
                If Not String.IsNullOrWhiteSpace(term) Then
                    Dim query As New GetProductsForCatalogQuery With {.SearchTerm = searchTerm}
                    Dim products = Await _mediator.Send(query, token)
                    If products IsNot Nothing Then
                        Dim count = 0
                        For Each p In products
                            If count >= 5 Then Exit For
                            matchedItems.Add(New CommandPaletteItem With {
                                .DisplayName = p.Name,
                                .Subtitle = "Product — SKU: " & p.Sku,
                                .Section = "Products",
                                .IconData = boxIcon,
                                .TargetProductSku = p.Sku
                            })
                            count += 1
                        Next
                    End If
                End If

                token.ThrowIfCancellationRequested()

                Results.Clear()
                For Each item In matchedItems
                    Results.Add(item)
                Next

                OnPropertyChanged(NameOf(IsEmpty))

                ' Update SelectedIndex
                If Results.Count > 0 Then
                    SelectedIndex = 0
                Else
                    SelectedIndex = -1
                End If

            Catch ex As TaskCanceledException
                ' Ignored
            Catch ex As Exception
                ' Suppressed
            Finally
                IsBusy = False
            End Try
        End Function

        Private Function GetModuleName(m As AppModule) As String
            Select Case m
                Case AppModule.Purchasing : Return "Purchasing"
                Case AppModule.Inventory : Return "Inventory"
                Case AppModule.POS : Return "Point of Sale"
                Case AppModule.Accounting : Return "Accounting"
                Case AppModule.DeveloperTools : Return "Developer Tools"
                Case Else : Return String.Empty
            End Select
        End Function

        Private Function GetModuleIcon(m As AppModule) As Geometry
            Dim resourceKey As String
            Select Case m
                Case AppModule.Purchasing : resourceKey = "IconBoxGeometry"
                Case AppModule.Inventory : resourceKey = "IconChartBarGeometry"
                Case AppModule.POS : resourceKey = "IconCashGeometry"
                Case AppModule.Accounting : resourceKey = "IconChartLineGeometry"
                Case AppModule.DeveloperTools : resourceKey = "IconBoltGeometry"
                Case Else : resourceKey = "IconBoxGeometry"
            End Select
            Return TryCast(Application.Current.TryFindResource(resourceKey), Geometry)
        End Function

        Private Sub ExecuteSelected()
            If SelectedIndex < 0 OrElse SelectedIndex >= Results.Count Then Return
            Dim selected = Results(SelectedIndex)

            ' Close command palette first
            IsOpen = False

            Dim mainVm = _services.GetRequiredService(Of MainWindowViewModel)()

            If selected.TargetItem IsNot Nothing Then
                ' Navigate to screen. Set active module first to prevent desync (watch-item #2).
                Dim pair = mainVm.AllNavigableItems.FirstOrDefault(Function(n) n.Item.ViewType = selected.TargetItem.ViewType)
                If pair.Item IsNot Nothing Then
                    mainVm.ActiveModule = pair.[Module]
                    If mainVm.NavigateCommand.CanExecute(pair.Item) Then
                        mainVm.NavigateCommand.Execute(pair.Item)
                    End If
                End If
            ElseIf Not String.IsNullOrEmpty(selected.TargetProductSku) Then
                ' Navigate to Product Management View and filter by Sku
                Dim pair = mainVm.AllNavigableItems.FirstOrDefault(Function(n) n.Item.ViewType = GetType(Views.Inventory.ProductManagementView))
                If pair.Item IsNot Nothing Then
                    mainVm.ActiveModule = pair.[Module]
                    
                    If mainVm.NavigateCommand.CanExecute(pair.Item) Then
                        mainVm.NavigateCommand.Execute(pair.Item)
                    End If

                    ' Resolve ProductManagementView from the current view of MainWindowViewModel to set search filter
                    Dim userControl = TryCast(mainVm.CurrentView, UserControl)
                    If userControl IsNot Nothing Then
                        Dim pmVm = TryCast(userControl.DataContext, ProductManagementViewModel)
                        If pmVm IsNot Nothing Then
                            pmVm.SearchText = selected.TargetProductSku
                        End If
                    End If
                End If
            End If
        End Sub

    End Class

End Namespace
