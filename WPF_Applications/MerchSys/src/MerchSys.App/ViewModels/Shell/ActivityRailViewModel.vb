Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports MerchSys.App.Models
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces

Namespace ViewModels.Shell

    ''' <summary>
    ''' ViewModel for the 60px Activity Rail icon strip.
    ''' Wraps MainWindowViewModel to expose rail-specific display data (abbreviation, tooltip, IsActive).
    ''' </summary>
    Public Class ActivityRailViewModel
        Inherits ObservableObject

        Private ReadOnly _mainVm As MainWindowViewModel
        Private ReadOnly _session As ISessionService

        Public ReadOnly Property RailItems As ObservableCollection(Of RailItem)
        Public ReadOnly Property SelectModuleCommand As CommunityToolkit.Mvvm.Input.RelayCommand(Of AppModule)

        Public Sub New(mainVm As MainWindowViewModel, session As ISessionService)
            _mainVm = mainVm
            _session = session
            RailItems = New ObservableCollection(Of RailItem)(BuildRailItems())
            SelectModuleCommand = mainVm.SelectModuleCommand
            AddHandler _mainVm.PropertyChanged, AddressOf OnMainVmPropertyChanged
            SyncActiveStates(_mainVm.ActiveModule)
        End Sub

        Private Sub OnMainVmPropertyChanged(sender As Object,
                                            e As System.ComponentModel.PropertyChangedEventArgs)
            If e.PropertyName = NameOf(MainWindowViewModel.ActiveModule) Then
                SyncActiveStates(_mainVm.ActiveModule)
            End If
        End Sub

        Private Sub SyncActiveStates(activeModule As AppModule)
            For Each item In RailItems
                item.IsActive = (item.ModuleId = activeModule)
            Next
        End Sub

        Private Function BuildRailItems() As List(Of RailItem)
            Dim items As New List(Of RailItem) From {
                New RailItem With {.ModuleId = AppModule.Purchasing, .Abbreviation = "PUR", .ToolTipText = "Purchasing  (Ctrl+1)"},
                New RailItem With {.ModuleId = AppModule.Inventory, .Abbreviation = "INV", .ToolTipText = "Inventory  (Ctrl+2)"},
                New RailItem With {.ModuleId = AppModule.POS, .Abbreviation = "POS", .ToolTipText = "Point of Sale  (Ctrl+3)"},
                New RailItem With {.ModuleId = AppModule.Accounting, .Abbreviation = "ACC", .ToolTipText = "Accounting  (Ctrl+4)"}
            }
            If _session.CurrentRole = UserRole.Developer Then
                items.Add(New RailItem With {
                    .ModuleId = AppModule.DeveloperTools,
                    .Abbreviation = "DEV",
                    .ToolTipText = "Developer Tools  (Ctrl+0)"
                })
            End If
            Return items
        End Function

    End Class

End Namespace
