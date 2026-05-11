Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports Microsoft.Extensions.DependencyInjection
Imports MerchSys.App.Models
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces

Namespace ViewModels

    Public Class MainWindowViewModel
        Inherits ObservableObject

        Private ReadOnly _services As IServiceProvider
        Private ReadOnly _session As ISessionService
        Private _activeItem As NavigationItem

        Private _currentView As Object
        Public Property CurrentView As Object
            Get
                Return _currentView
            End Get
            Private Set(value As Object)
                SetProperty(_currentView, value)
            End Set
        End Property

        Public ReadOnly Property NavigationGroups As ObservableCollection(Of NavigationGroup)
        Public ReadOnly Property NavigateCommand As RelayCommand(Of NavigationItem)

        Public Sub New(services As IServiceProvider, session As ISessionService)
            _services = services
            _session = session
            NavigationGroups = BuildNavigationGroups()
            NavigateCommand = New RelayCommand(Of NavigationItem)(AddressOf Navigate)
        End Sub

        Private Sub Navigate(item As NavigationItem)
            If item Is Nothing Then Return
            If _activeItem IsNot Nothing Then _activeItem.IsActive = False
            _activeItem = item
            _activeItem.IsActive = True
            CurrentView = _services.GetRequiredService(item.ViewType)
        End Sub

        Public Sub NavigateToDefault()
            Dim defaultItem = NavigationGroups _
                .SelectMany(Function(g) g.Items) _
                .FirstOrDefault(Function(i) i.ViewType = GetType(Views.Inventory.StockDashboardView))
            If defaultItem IsNot Nothing Then Navigate(defaultItem)
        End Sub

        Private Function BuildNavigationGroups() As ObservableCollection(Of NavigationGroup)
            Return New ObservableCollection(Of NavigationGroup) From {
                New NavigationGroup("Point of Sale", BuildPosNavItems()),
                New NavigationGroup("Purchasing", New List(Of NavigationItem) From {
                    New NavigationItem With {.DisplayName = "Purchase Orders", .ViewType = GetType(Views.Purchasing.PurchaseOrderListView)},
                    New NavigationItem With {.DisplayName = "Goods Receiving", .ViewType = GetType(Views.Purchasing.GoodsReceivingView)},
                    New NavigationItem With {.DisplayName = "Vendor Directory", .ViewType = GetType(Views.Purchasing.VendorDirectoryView)},
                    New NavigationItem With {.DisplayName = "Accounts Payable", .ViewType = GetType(Views.Purchasing.APLedgerView)},
                    New NavigationItem With {.DisplayName = "Reorder Suggestions", .ViewType = GetType(Views.Purchasing.ReorderSuggestionsView)}
                }),
                New NavigationGroup("Inventory", New List(Of NavigationItem) From {
                    New NavigationItem With {.DisplayName = "Stock Dashboard", .ViewType = GetType(Views.Inventory.StockDashboardView)},
                    New NavigationItem With {.DisplayName = "Product Management", .ViewType = GetType(Views.Inventory.ProductManagementView)},
                    New NavigationItem With {.DisplayName = "Expiry Monitor", .ViewType = GetType(Views.Inventory.ExpiryMonitorView)},
                    New NavigationItem With {.DisplayName = "Shrinkage", .ViewType = GetType(Views.Inventory.ShrinkageView)}
                }),
                New NavigationGroup("Accounting", BuildAccountingNavItems())
            }
        End Function

        Private Function BuildPosNavItems() As List(Of NavigationItem)
            Dim items As New List(Of NavigationItem) From {
                New NavigationItem With {.DisplayName = "Sales Cart", .ViewType = GetType(Views.POS.SalesCartView)},
                New NavigationItem With {.DisplayName = "Credit Management", .ViewType = GetType(Views.POS.CreditManagementView)},
                New NavigationItem With {.DisplayName = "Transaction History", .ViewType = GetType(Views.POS.TransactionHistoryView)},
                New NavigationItem With {.DisplayName = "Daily Summary", .ViewType = GetType(Views.POS.DailySummaryView)}
            }
            ' INT-02 convention (type-based NavigationItem); Manager-only per OWASP DA financial config access rules
            If _session.CurrentRole = UserRole.Manager Then
                items.Add(New NavigationItem With {.DisplayName = "VAT Settings", .ViewType = GetType(Views.POS.VatSettingsView)})
            End If
            Return items
        End Function

        Private Function BuildAccountingNavItems() As List(Of NavigationItem)
            Dim items As New List(Of NavigationItem) From {
                New NavigationItem With {.DisplayName = "Financial Overview", .ViewType = GetType(Views.Accounting.FinancialOverviewView)},
                New NavigationItem With {.DisplayName = "Income Statement", .ViewType = GetType(Views.Accounting.IncomeStatementView)},
                New NavigationItem With {.DisplayName = "Sales Summary", .ViewType = GetType(Views.Accounting.SalesSummaryView)}
            }
            ' INT-02 convention (type-based NavigationItem); view source ACC-11; Manager-only per BIR access rules
            If _session.CurrentRole = UserRole.Manager Then
                items.Add(New NavigationItem With {.DisplayName = "VAT Return (BIR)", .ViewType = GetType(Views.Accounting.VatReturnView)})
            End If
            Return items
        End Function

    End Class

End Namespace
