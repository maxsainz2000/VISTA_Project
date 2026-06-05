Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.App.Models
Imports MerchSys.App.Services
Imports MerchSys.App.Services.Theming
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.App.ViewModels.Shell

Namespace ViewModels

    Public Class MainWindowViewModel
        Inherits ObservableObject

        Private ReadOnly _services As IServiceProvider
        Private ReadOnly _session As ISessionService
        Private ReadOnly _loginSession As LoginSessionService
        Private ReadOnly _themeService As IThemeService

        Private _activeNavItem As NavigationItem
        Private _currentView As Object
        Private _activeModule As AppModule = AppModule.Inventory

        ' ── Current view ─────────────────────────────────────────────────────────

        Public Property CurrentView As Object
            Get
                Return _currentView
            End Get
            Private Set(value As Object)
                SetProperty(_currentView, value)
            End Set
        End Property

        ' ── Session display ──────────────────────────────────────────────────────

        ''' <summary>Display name of the currently authenticated user.</summary>
        Public ReadOnly Property CurrentUsername As String
            Get
                Return _session.CurrentUsername
            End Get
        End Property

        ''' <summary>Role string for the sidebar (e.g. "Manager", "Owner").</summary>
        Public ReadOnly Property CurrentRoleDisplay As String
            Get
                Return _session.CurrentRole.ToString()
            End Get
        End Property

        ' ── Active module ────────────────────────────────────────────────────────

        ''' <summary>
        ''' The module currently selected in the Activity Rail.
        ''' Changing this updates ActiveModuleName and ActiveModuleItems.
        ''' </summary>
        Public Property ActiveModule As AppModule
            Get
                Return _activeModule
            End Get
            Set(value As AppModule)
                If SetProperty(_activeModule, value) Then
                    OnPropertyChanged(NameOf(ActiveModuleName))
                    RebuildActiveModuleItems()
                End If
            End Set
        End Property

        ''' <summary>Display name for the active module header in the Module Detail Panel.</summary>
        Public ReadOnly Property ActiveModuleName As String
            Get
                Select Case _activeModule
                    Case AppModule.Purchasing : Return "Purchasing"
                    Case AppModule.Inventory : Return "Inventory"
                    Case AppModule.POS : Return "Point of Sale"
                    Case AppModule.Accounting : Return "Accounting"
                    Case AppModule.DeveloperTools : Return "Developer Tools"
                    Case Else : Return String.Empty
                End Select
            End Get
        End Property

        ' ── Per-module navigation items ──────────────────────────────────────────

        Public ReadOnly Property PurchasingItems As ObservableCollection(Of NavigationItem)
        Public ReadOnly Property InventoryItems As ObservableCollection(Of NavigationItem)
        Public ReadOnly Property PosItems As ObservableCollection(Of NavigationItem)
        Public ReadOnly Property AccountingItems As ObservableCollection(Of NavigationItem)
        Public ReadOnly Property DeveloperToolsItems As ObservableCollection(Of NavigationItem)

        ' Legacy flat-group collection (kept for reference; new UI uses per-module collections above)
        Public ReadOnly Property NavigationGroups As ObservableCollection(Of NavigationGroup)

        Private _allNavigableItems As List(Of ([Module] As AppModule, Item As NavigationItem)) = New List(Of ([Module] As AppModule, Item As NavigationItem))()

        Public ReadOnly Property AllNavigableItems As IReadOnlyList(Of ([Module] As AppModule, Item As NavigationItem))
            Get
                Return _allNavigableItems
            End Get
        End Property

        ' ── Command Palette Integration ──────────────────────────────────────────

        Private _commandPalette As CommandPaletteViewModel

        Public ReadOnly Property CommandPalette As CommandPaletteViewModel
            Get
                If _commandPalette Is Nothing Then
                    _commandPalette = _services.GetRequiredService(Of CommandPaletteViewModel)()
                    AddHandler _commandPalette.PropertyChanged, AddressOf OnCommandPalettePropertyChanged
                End If
                Return _commandPalette
            End Get
        End Property

        Private Sub OnCommandPalettePropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
            If e.PropertyName = NameOf(CommandPaletteViewModel.IsOpen) Then
                OnPropertyChanged(NameOf(IsCommandPaletteOpen))
                SelectModuleCommand.NotifyCanExecuteChanged()
                NavigateCommand.NotifyCanExecuteChanged()
                If CommandPalette.IsOpen AndAlso ShortcutsOverlay.IsOpen Then
                    ShortcutsOverlay.IsOpen = False
                End If
            End If
        End Sub

        Public ReadOnly Property IsCommandPaletteOpen As Boolean
            Get
                Return CommandPalette.IsOpen
            End Get
        End Property

        ' ── Shortcuts Overlay Integration ────────────────────────────────────────

        Private _shortcutsOverlay As ShortcutsOverlayViewModel

        Public ReadOnly Property ShortcutsOverlay As ShortcutsOverlayViewModel
            Get
                If _shortcutsOverlay Is Nothing Then
                    _shortcutsOverlay = _services.GetRequiredService(Of ShortcutsOverlayViewModel)()
                    AddHandler _shortcutsOverlay.PropertyChanged, AddressOf OnShortcutsOverlayPropertyChanged
                End If
                Return _shortcutsOverlay
            End Get
        End Property

        Private Sub OnShortcutsOverlayPropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
            If e.PropertyName = NameOf(ShortcutsOverlayViewModel.IsOpen) Then
                OnPropertyChanged(NameOf(IsShortcutsOverlayOpen))
                SelectModuleCommand.NotifyCanExecuteChanged()
                NavigateCommand.NotifyCanExecuteChanged()
                If ShortcutsOverlay.IsOpen AndAlso CommandPalette.IsOpen Then
                    CommandPalette.IsOpen = False
                End If
            End If
        End Sub

        Public ReadOnly Property IsShortcutsOverlayOpen As Boolean
            Get
                Return ShortcutsOverlay.IsOpen
            End Get
        End Property

        ' ── Commands ─────────────────────────────────────────────────────────────

        Public ReadOnly Property NavigateCommand As RelayCommand(Of NavigationItem)
        Public ReadOnly Property SelectModuleCommand As RelayCommand(Of AppModule)
        Public ReadOnly Property LogoutCommand As RelayCommand
        Public ReadOnly Property ToggleThemeCommand As RelayCommand

        ''' <summary>Raised when the user clicks Log Out.</summary>
        Public Event LogoutRequested As EventHandler

        ' ── Constructor ──────────────────────────────────────────────────────────

        Public Sub New(services As IServiceProvider, session As ISessionService,
                       loginSession As LoginSessionService, themeService As IThemeService)
            _services = services
            _session = session
            _loginSession = loginSession
            _themeService = themeService

            PurchasingItems = New ObservableCollection(Of NavigationItem)()
            InventoryItems = New ObservableCollection(Of NavigationItem)()
            PosItems = New ObservableCollection(Of NavigationItem)()
            AccountingItems = New ObservableCollection(Of NavigationItem)()
            DeveloperToolsItems = New ObservableCollection(Of NavigationItem)()
            NavigationGroups = New ObservableCollection(Of NavigationGroup)(BuildNavigationGroups())

            NavigateCommand = New RelayCommand(Of NavigationItem)(AddressOf Navigate, AddressOf CanNavigate)
            SelectModuleCommand = New RelayCommand(Of AppModule)(AddressOf DoSelectModule, AddressOf CanSelectModule)
            LogoutCommand = New RelayCommand(AddressOf DoLogout)
            ToggleThemeCommand = New RelayCommand(AddressOf DoToggleTheme)

            RebuildModuleCollections()
        End Sub

        ' ── Navigation ───────────────────────────────────────────────────────────

        Private Function CanNavigate(item As NavigationItem) As Boolean
            Return Not IsCommandPaletteOpen AndAlso Not IsShortcutsOverlayOpen
        End Function

        Private Function CanSelectModule(m As AppModule) As Boolean
            Return Not IsCommandPaletteOpen AndAlso Not IsShortcutsOverlayOpen
        End Function

        Private Sub Navigate(item As NavigationItem)
            If item Is Nothing Then Return
            If _activeNavItem IsNot Nothing Then _activeNavItem.IsActive = False
            _activeNavItem = item
            _activeNavItem.IsActive = True
            CurrentView = _services.GetRequiredService(item.ViewType)
        End Sub

        ''' <summary>Navigate to the role-appropriate default landing page.</summary>
        Public Sub NavigateToDefault()
            If _session.CurrentRole = UserRole.Owner Then
                ActiveModule = AppModule.Accounting
                Dim item = AccountingItems.FirstOrDefault(
                    Function(i) i.ViewType = GetType(Views.OwnerDashboardView))
                If item IsNot Nothing Then Navigate(item)
            Else
                ActiveModule = AppModule.Inventory
                Dim item = InventoryItems.FirstOrDefault(
                    Function(i) i.ViewType = GetType(Views.Inventory.StockDashboardView))
                If item IsNot Nothing Then Navigate(item)
            End If
        End Sub

        ''' <summary>
        ''' Rebuilds all navigation for the current role and resets state.
        ''' Call after login to reflect the newly authenticated user.
        ''' </summary>
        Public Sub RefreshNavigation()
            NavigationGroups.Clear()
            For Each grp In BuildNavigationGroups()
                NavigationGroups.Add(grp)
            Next
            RebuildModuleCollections()
            OnPropertyChanged(NameOf(CurrentUsername))
            OnPropertyChanged(NameOf(CurrentRoleDisplay))
            _activeNavItem = Nothing
            CurrentView = Nothing
        End Sub

        Private Sub DoSelectModule(m As AppModule)
            ActiveModule = m
            ' Navigate to first item in the newly selected module
            Dim items = GetItemsForModule(m)
            If items IsNot Nothing AndAlso items.Count > 0 Then
                Navigate(items(0))
            End If
        End Sub

        Private Sub DoLogout()
            _loginSession.ClearUser()
            RaiseEvent LogoutRequested(Me, EventArgs.Empty)
        End Sub

        ' ── Module collection builders ────────────────────────────────────────────

        Private Sub RebuildModuleCollections()
            RebuildCollection(PurchasingItems, BuildRoleAwarePurchasingItems())
            RebuildCollection(InventoryItems, BuildRoleAwareInventoryItems())
            RebuildCollection(PosItems, BuildRoleAwarePosItems())
            RebuildCollection(AccountingItems, BuildRoleAwareAccountingItems())
            RebuildCollection(DeveloperToolsItems, BuildDeveloperToolsItems())
            RebuildAllNavigableItems()
        End Sub

        Private Sub RebuildAllNavigableItems()
            Dim list As New List(Of ([Module] As AppModule, Item As NavigationItem))()
            For Each itm In PurchasingItems
                list.Add((AppModule.Purchasing, itm))
            Next
            For Each itm In InventoryItems
                list.Add((AppModule.Inventory, itm))
            Next
            For Each itm In PosItems
                list.Add((AppModule.POS, itm))
            Next
            For Each itm In AccountingItems
                list.Add((AppModule.Accounting, itm))
            Next
            For Each itm In DeveloperToolsItems
                list.Add((AppModule.DeveloperTools, itm))
            Next
            _allNavigableItems = list
            OnPropertyChanged(NameOf(AllNavigableItems))
        End Sub

        Private Sub RebuildActiveModuleItems()
            ' No-op — the per-module collections don't change when the active module changes.
            ' Module panels bind directly to PurchasingItems / InventoryItems etc.
        End Sub

        Private Shared Sub RebuildCollection(target As ObservableCollection(Of NavigationItem),
                                              sourceList As List(Of NavigationItem))
            target.Clear()
            For Each itm In sourceList
                target.Add(itm)
            Next
        End Sub

        Private Function GetItemsForModule(m As AppModule) As ObservableCollection(Of NavigationItem)
            Select Case m
                Case AppModule.Purchasing : Return PurchasingItems
                Case AppModule.Inventory : Return InventoryItems
                Case AppModule.POS : Return PosItems
                Case AppModule.Accounting : Return AccountingItems
                Case AppModule.DeveloperTools : Return DeveloperToolsItems
                Case Else : Return Nothing
            End Select
        End Function

        ' ── Role-aware nav item builders ─────────────────────────────────────────

        Private Function BuildRoleAwarePurchasingItems() As List(Of NavigationItem)
            If _session.CurrentRole = UserRole.Owner Then
                Return New List(Of NavigationItem) From {
                    New NavigationItem With {.DisplayName = "Purchasing Dashboard", .ViewType = GetType(Views.Purchasing.PurchasingDashboardView)},
                    New NavigationItem With {.DisplayName = "Purchase Orders", .ViewType = GetType(Views.Purchasing.PurchaseOrderListView)},
                    New NavigationItem With {.DisplayName = "Accounts Payable", .ViewType = GetType(Views.Purchasing.APLedgerView)}
                }
            End If
            Return New List(Of NavigationItem) From {
                New NavigationItem With {.DisplayName = "Purchasing Dashboard", .ViewType = GetType(Views.Purchasing.PurchasingDashboardView)},
                New NavigationItem With {.DisplayName = "Purchase Orders", .ViewType = GetType(Views.Purchasing.PurchaseOrderListView)},
                New NavigationItem With {.DisplayName = "Goods Receiving", .ViewType = GetType(Views.Purchasing.GoodsReceivingView)},
                New NavigationItem With {.DisplayName = "Vendor Directory", .ViewType = GetType(Views.Purchasing.VendorDirectoryView)},
                New NavigationItem With {.DisplayName = "Vendor Product Catalog", .ViewType = GetType(Views.Purchasing.VendorCatalogView)},
                New NavigationItem With {.DisplayName = "Accounts Payable", .ViewType = GetType(Views.Purchasing.APLedgerView)},
                New NavigationItem With {.DisplayName = "Reorder Suggestions", .ViewType = GetType(Views.Purchasing.ReorderSuggestionsView)}
            }
        End Function

        Private Function BuildRoleAwareInventoryItems() As List(Of NavigationItem)
            If _session.CurrentRole = UserRole.Owner Then
                Return New List(Of NavigationItem) From {
                    New NavigationItem With {.DisplayName = "Stock Dashboard", .ViewType = GetType(Views.Inventory.StockDashboardView)}
                }
            End If
            Return New List(Of NavigationItem) From {
                New NavigationItem With {.DisplayName = "Stock Dashboard", .ViewType = GetType(Views.Inventory.StockDashboardView)},
                New NavigationItem With {.DisplayName = "Product Management", .ViewType = GetType(Views.Inventory.ProductManagementView)},
                New NavigationItem With {.DisplayName = "Expiry Monitor", .ViewType = GetType(Views.Inventory.ExpiryMonitorView)},
                New NavigationItem With {.DisplayName = "Shrinkage", .ViewType = GetType(Views.Inventory.ShrinkageView)}
            }
        End Function

        Private Function BuildRoleAwarePosItems() As List(Of NavigationItem)
            Dim items As New List(Of NavigationItem) From {
                New NavigationItem With {.DisplayName = "Sales Cart", .ViewType = GetType(Views.POS.SalesCartView)},
                New NavigationItem With {.DisplayName = "Credit Management", .ViewType = GetType(Views.POS.CreditManagementView)},
                New NavigationItem With {.DisplayName = "Transaction History", .ViewType = GetType(Views.POS.TransactionHistoryView)},
                New NavigationItem With {.DisplayName = "Daily Summary", .ViewType = GetType(Views.POS.DailySummaryView)}
            }
            If _session.CurrentRole <> UserRole.Owner Then
                items.Add(New NavigationItem With {.DisplayName = "VAT Settings", .ViewType = GetType(Views.POS.VatSettingsView)})
            End If
            If _session.CurrentRole = UserRole.Owner Then
                Return New List(Of NavigationItem) From {
                    New NavigationItem With {.DisplayName = "Transaction History", .ViewType = GetType(Views.POS.TransactionHistoryView)}
                }
            End If
            Return items
        End Function

        Private Function BuildRoleAwareAccountingItems() As List(Of NavigationItem)
            Dim items As New List(Of NavigationItem)()
            If _session.CurrentRole = UserRole.Owner Then
                items.Add(New NavigationItem With {.DisplayName = "KPI Overview", .ViewType = GetType(Views.OwnerDashboardView)})
            End If
            items.Add(New NavigationItem With {.DisplayName = "Financial Overview", .ViewType = GetType(Views.Accounting.FinancialOverviewView)})
            items.Add(New NavigationItem With {.DisplayName = "Income Statement", .ViewType = GetType(Views.Accounting.IncomeStatementView)})
            items.Add(New NavigationItem With {.DisplayName = "Sales Summary", .ViewType = GetType(Views.Accounting.SalesSummaryView)})
            items.Add(New NavigationItem With {.DisplayName = "VAT Relief Report", .ViewType = GetType(Views.Accounting.VatReliefReportView)})
            If _session.CurrentRole <> UserRole.Owner Then
                items.Add(New NavigationItem With {.DisplayName = "Tamper Audit Report", .ViewType = GetType(Views.Accounting.TamperAuditReportView)})
                items.Add(New NavigationItem With {.DisplayName = "VAT Return (BIR)", .ViewType = GetType(Views.Accounting.VatReturnView)})
            End If
            Return items
        End Function

        Private Function BuildDeveloperToolsItems() As List(Of NavigationItem)
            ' Developer Tools are exclusive to the Developer role — not Manager or Owner.
            ' Gating the items here (not just the rail icon) also closes the Ctrl+0 shortcut path.
            If _session.CurrentRole <> UserRole.Developer Then
                Return New List(Of NavigationItem)()
            End If
            Return New List(Of NavigationItem) From {
                New NavigationItem With {.DisplayName = "Run VAT Schema Harness", .ViewType = GetType(Views.Debug.DebugMenuView)}
            }
        End Function

        ' ── Legacy group builder (kept for compatibility) ─────────────────────────

        Private Function BuildNavigationGroups() As List(Of NavigationGroup)
            Dim groups = New List(Of NavigationGroup) From {
                New NavigationGroup("Purchasing", BuildRoleAwarePurchasingItems()),
                New NavigationGroup("Inventory", BuildRoleAwareInventoryItems()),
                New NavigationGroup("Point of Sale", BuildRoleAwarePosItems()),
                New NavigationGroup("Accounting", BuildRoleAwareAccountingItems())
            }
            If _session.CurrentRole = UserRole.Developer Then
                groups.Add(New NavigationGroup("Developer Tools", BuildDeveloperToolsItems()))
            End If
            Return groups
        End Function

        ' ── Theming ──────────────────────────────────────────────────────────────

        ''' <summary>
        ''' Gets whether the application is currently using the Dark theme.
        ''' </summary>
        Public ReadOnly Property IsDarkTheme As Boolean
            Get
                Return _themeService.Current = AppTheme.Dark
            End Get
        End Property

        ''' <summary>
        ''' Command that toggles the application theme and notifies the UI.
        ''' </summary>
        Private Sub DoToggleTheme()
            _themeService.Toggle()
            OnPropertyChanged(NameOf(IsDarkTheme))
        End Sub

    End Class

End Namespace
