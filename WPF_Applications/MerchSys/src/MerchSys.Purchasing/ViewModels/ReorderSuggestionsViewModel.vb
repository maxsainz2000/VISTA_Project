Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services

Namespace ViewModels

    ''' <summary>Flat display row for the reorder suggestions grid.</summary>
    Public Class SuggestionRow
        Public Property Id As Integer
        Public Property ProductName As String
        Public Property CurrentStock As Integer
        Public Property ReorderPoint As Integer
        Public Property SuggestedQty As Integer
        Public Property PreferredVendor As String
        Public Property EstLeadTimeDays As Integer
        Public Property Status As String
        Public Property IsSeasonalAdjusted As Boolean
    End Class

    ''' <summary>
    ''' ViewModel for the Reorder Suggestions screen.
    ''' Two-tab layout: Suggestions (generate / accept / dismiss) and Configuration (per-product thresholds).
    ''' </summary>
    Public Class ReorderSuggestionsViewModel
        Inherits ObservableObject

        Private ReadOnly _reorderService As IReorderService
        Private _allSuggestions As List(Of SuggestionRow) = New List(Of SuggestionRow)()
        Private _editingConfig As ReorderConfig

        Public Sub New(reorderService As IReorderService)
            _reorderService = reorderService

            Suggestions = New ObservableCollection(Of SuggestionRow)()
            Configs = New ObservableCollection(Of ReorderConfig)()

            GenerateCommand = New AsyncRelayCommand(AddressOf GenerateAsync, Function() Not IsBusy)
            RefreshCommand = New AsyncRelayCommand(AddressOf LoadDataAsync, Function() Not IsBusy)
            SetFilterCommand = New RelayCommand(Of String)(Sub(f) ActiveFilter = f)
            SwitchTabCommand = New RelayCommand(Of String)(Sub(t) ActiveTab = t)
            AcceptCommand = New AsyncRelayCommand(Of SuggestionRow)(AddressOf AcceptAsync, Function(r) r IsNot Nothing AndAlso Not IsBusy)
            DismissCommand = New AsyncRelayCommand(Of SuggestionRow)(AddressOf DismissAsync, Function(r) r IsNot Nothing AndAlso Not IsBusy)
            OpenEditConfigCommand = New RelayCommand(Of ReorderConfig)(AddressOf OpenEditConfig, Function(cfg) cfg IsNot Nothing)
            SaveConfigCommand = New AsyncRelayCommand(AddressOf SaveConfigAsync, Function() IsEditDialogOpen AndAlso Not IsBusy)
            CancelEditCommand = New RelayCommand(AddressOf CloseEditDialog)

            Dim initTask = LoadDataAsync()
        End Sub

        ' ─── Tab State ────────────────────────────────────────────────────────────

        Private _activeTab As String = "Suggestions"
        Public Property ActiveTab As String
            Get
                Return _activeTab
            End Get
            Set(value As String)
                If SetProperty(_activeTab, value) Then
                    OnPropertyChanged(NameOf(IsSuggestionsTabActive))
                    OnPropertyChanged(NameOf(IsConfigTabActive))
                End If
            End Set
        End Property

        Public ReadOnly Property IsSuggestionsTabActive As Boolean
            Get
                Return _activeTab = "Suggestions"
            End Get
        End Property

        Public ReadOnly Property IsConfigTabActive As Boolean
            Get
                Return _activeTab = "Configuration"
            End Get
        End Property

        ' ─── Filter ───────────────────────────────────────────────────────────────

        Private _activeFilter As String = "Pending"
        Public Property ActiveFilter As String
            Get
                Return _activeFilter
            End Get
            Set(value As String)
                If SetProperty(_activeFilter, value) Then
                    OnPropertyChanged(NameOf(IsPendingFilterActive))
                    OnPropertyChanged(NameOf(IsAcceptedFilterActive))
                    OnPropertyChanged(NameOf(IsDismissedFilterActive))
                    ApplyFilter()
                End If
            End Set
        End Property

        Public ReadOnly Property IsPendingFilterActive As Boolean
            Get
                Return _activeFilter = "Pending"
            End Get
        End Property

        Public ReadOnly Property IsAcceptedFilterActive As Boolean
            Get
                Return _activeFilter = "Accepted"
            End Get
        End Property

        Public ReadOnly Property IsDismissedFilterActive As Boolean
            Get
                Return _activeFilter = "Dismissed"
            End Get
        End Property

        ' ─── Grid Data ────────────────────────────────────────────────────────────

        Public Property Suggestions As ObservableCollection(Of SuggestionRow)
        Public Property Configs As ObservableCollection(Of ReorderConfig)

        ' ─── Status ───────────────────────────────────────────────────────────────

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                If SetProperty(_isBusy, value) Then
                    GenerateCommand.NotifyCanExecuteChanged()
                    RefreshCommand.NotifyCanExecuteChanged()
                    AcceptCommand.NotifyCanExecuteChanged()
                    DismissCommand.NotifyCanExecuteChanged()
                    SaveConfigCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _statusMessage As String = String.Empty
        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public Property GenerateCommand As AsyncRelayCommand
        Public Property RefreshCommand As AsyncRelayCommand
        Public Property SetFilterCommand As RelayCommand(Of String)
        Public Property SwitchTabCommand As RelayCommand(Of String)
        Public Property AcceptCommand As AsyncRelayCommand(Of SuggestionRow)
        Public Property DismissCommand As AsyncRelayCommand(Of SuggestionRow)
        Public Property OpenEditConfigCommand As RelayCommand(Of ReorderConfig)
        Public Property SaveConfigCommand As AsyncRelayCommand
        Public Property CancelEditCommand As RelayCommand

        ' ─── Config Edit Dialog ────────────────────────────────────────────────────

        Private _isEditDialogOpen As Boolean
        Public Property IsEditDialogOpen As Boolean
            Get
                Return _isEditDialogOpen
            End Get
            Set(value As Boolean)
                If SetProperty(_isEditDialogOpen, value) Then
                    SaveConfigCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _editProductName As String = String.Empty
        Public Property EditProductName As String
            Get
                Return _editProductName
            End Get
            Set(value As String)
                SetProperty(_editProductName, value)
            End Set
        End Property

        Private _editMinThreshold As String = String.Empty
        Public Property EditMinThreshold As String
            Get
                Return _editMinThreshold
            End Get
            Set(value As String)
                SetProperty(_editMinThreshold, value)
            End Set
        End Property

        Private _editSafetyStock As String = String.Empty
        Public Property EditSafetyStock As String
            Get
                Return _editSafetyStock
            End Get
            Set(value As String)
                SetProperty(_editSafetyStock, value)
            End Set
        End Property

        Private _editDefaultOrderQty As String = String.Empty
        Public Property EditDefaultOrderQty As String
            Get
                Return _editDefaultOrderQty
            End Get
            Set(value As String)
                SetProperty(_editDefaultOrderQty, value)
            End Set
        End Property

        Private _editLeadTimeDays As String = String.Empty
        Public Property EditLeadTimeDays As String
            Get
                Return _editLeadTimeDays
            End Get
            Set(value As String)
                SetProperty(_editLeadTimeDays, value)
            End Set
        End Property

        Private _editIsSeasonalItem As Boolean
        Public Property EditIsSeasonalItem As Boolean
            Get
                Return _editIsSeasonalItem
            End Get
            Set(value As Boolean)
                SetProperty(_editIsSeasonalItem, value)
            End Set
        End Property

        Private _editSeasonalMultiplier As String = String.Empty
        Public Property EditSeasonalMultiplier As String
            Get
                Return _editSeasonalMultiplier
            End Get
            Set(value As String)
                SetProperty(_editSeasonalMultiplier, value)
            End Set
        End Property

        Private _editIsActive As Boolean
        Public Property EditIsActive As Boolean
            Get
                Return _editIsActive
            End Get
            Set(value As Boolean)
                SetProperty(_editIsActive, value)
            End Set
        End Property

        Private _editError As String = String.Empty
        Public Property EditError As String
            Get
                Return _editError
            End Get
            Set(value As String)
                If SetProperty(_editError, value) Then
                    OnPropertyChanged(NameOf(HasEditError))
                End If
            End Set
        End Property

        Public ReadOnly Property HasEditError As Boolean
            Get
                Return Not String.IsNullOrEmpty(_editError)
            End Get
        End Property

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsBusy = True
            Try
                Dim allSugg = Await _reorderService.GetAllSuggestionsAsync()
                _allSuggestions = allSugg.Select(Function(sg) New SuggestionRow With {
                    .Id = sg.Id,
                    .ProductName = sg.ProductName,
                    .CurrentStock = sg.CurrentStock,
                    .ReorderPoint = sg.ReorderPoint,
                    .SuggestedQty = sg.SuggestedQuantity,
                    .PreferredVendor = If(String.IsNullOrEmpty(sg.PreferredVendorName), "—", sg.PreferredVendorName),
                    .EstLeadTimeDays = sg.EstimatedLeadTimeDays,
                    .Status = sg.Status,
                    .IsSeasonalAdjusted = sg.IsSeasonalAdjusted
                }).ToList()

                ApplyFilter()

                Dim cfgs = Await _reorderService.GetAllConfigsAsync()
                Configs.Clear()
                For Each cfg In cfgs
                    Configs.Add(cfg)
                Next

                StatusMessage = $"Loaded {_allSuggestions.Count} suggestion(s), {Configs.Count} config(s)."
            Catch ex As Exception
                StatusMessage = $"Load failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub ApplyFilter()
            Dim filtered = _allSuggestions.Where(Function(sg) sg.Status = _activeFilter).ToList()
            Suggestions.Clear()
            For Each row In filtered
                Suggestions.Add(row)
            Next
        End Sub

        Private Async Function GenerateAsync() As Task
            IsBusy = True
            Try
                Dim newSugg = Await _reorderService.GenerateSuggestionsAsync()
                StatusMessage = $"Generated {newSugg.Count} new suggestion(s)."
                Await LoadDataAsync()
            Catch ex As Exception
                StatusMessage = $"Generate failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function AcceptAsync(row As SuggestionRow) As Task
            If row Is Nothing Then Return
            IsBusy = True
            Try
                Dim po = Await _reorderService.AcceptSuggestionAsync(row.Id)
                StatusMessage = $"Draft PO {po.OrderNumber} created."
                Await LoadDataAsync()
            Catch ex As InvalidOperationException
                StatusMessage = $"Cannot accept: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function DismissAsync(row As SuggestionRow) As Task
            If row Is Nothing Then Return
            IsBusy = True
            Try
                Await _reorderService.DismissSuggestionAsync(row.Id)
                StatusMessage = "Suggestion dismissed."
                Await LoadDataAsync()
            Catch ex As InvalidOperationException
                StatusMessage = $"Cannot dismiss: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

        ' ─── Config Editing ────────────────────────────────────────────────────────

        Private Sub OpenEditConfig(cfg As ReorderConfig)
            _editingConfig = cfg
            EditProductName = cfg.ProductName
            EditMinThreshold = cfg.MinimumThreshold.ToString()
            EditSafetyStock = cfg.SafetyStock.ToString()
            EditDefaultOrderQty = cfg.DefaultOrderQuantity.ToString()
            EditLeadTimeDays = cfg.LeadTimeDays.ToString()
            EditIsSeasonalItem = cfg.IsSeasonalItem
            EditSeasonalMultiplier = cfg.SeasonalMultiplier.ToString()
            EditIsActive = cfg.IsActive
            EditError = String.Empty
            IsEditDialogOpen = True
        End Sub

        Private Async Function SaveConfigAsync() As Task
            Dim minThreshold As Integer
            Dim safetyStock As Integer
            Dim defaultOrderQty As Integer
            Dim leadTimeDays As Integer
            Dim seasonalMult As Decimal

            If Not Integer.TryParse(EditMinThreshold, minThreshold) OrElse minThreshold < 0 Then
                EditError = "Minimum threshold must be a non-negative integer."
                Return
            End If
            If Not Integer.TryParse(EditSafetyStock, safetyStock) OrElse safetyStock < 0 Then
                EditError = "Safety stock must be a non-negative integer."
                Return
            End If
            If Not Integer.TryParse(EditDefaultOrderQty, defaultOrderQty) OrElse defaultOrderQty <= 0 Then
                EditError = "Default order quantity must be a positive integer."
                Return
            End If
            If Not Integer.TryParse(EditLeadTimeDays, leadTimeDays) OrElse leadTimeDays < 0 Then
                EditError = "Lead time days must be a non-negative integer."
                Return
            End If
            If Not Decimal.TryParse(EditSeasonalMultiplier, seasonalMult) OrElse seasonalMult < 1D Then
                EditError = "Seasonal multiplier must be a decimal value ≥ 1.0."
                Return
            End If

            IsBusy = True
            Try
                _editingConfig.MinimumThreshold = minThreshold
                _editingConfig.SafetyStock = safetyStock
                _editingConfig.DefaultOrderQuantity = defaultOrderQty
                _editingConfig.LeadTimeDays = leadTimeDays
                _editingConfig.IsSeasonalItem = EditIsSeasonalItem
                _editingConfig.SeasonalMultiplier = seasonalMult
                _editingConfig.IsActive = EditIsActive

                Await _reorderService.UpdateConfigAsync(_editingConfig)
                Dim savedName = _editingConfig.ProductName
                CloseEditDialog()
                Await LoadDataAsync()
                StatusMessage = $"Config for {savedName} updated."
            Catch ex As Exception
                EditError = $"Save failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub CloseEditDialog()
            IsEditDialogOpen = False
            _editingConfig = Nothing
            EditError = String.Empty
        End Sub

    End Class

End Namespace
