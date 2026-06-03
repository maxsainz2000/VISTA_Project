Imports System.Collections.ObjectModel
Imports System.ComponentModel.DataAnnotations
Imports System.Linq
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Inventory.Services
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace ViewModels

    Public Class ShrinkageRowItem
        Inherits ObservableObject

        Public Property Id As Integer
        Public Property ProductId As Integer
        Public Property RecordedDate As DateTime
        Public Property ProductName As String
        Public Property QuantityLost As Integer
        Public Property UnitCost As Decimal
        Public Property TotalValue As Decimal
        Public Property Reason As String
        Public Property Notes As String
        Public Property RecordedBy As String

    End Class

    Public Class ShrinkageProductItem
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property CurrentStock As Integer
        Public Property DisplayText As String
        Public Overrides Function ToString() As String
            Return DisplayText
        End Function
    End Class

    Public Class ShrinkageBatchItem
        Public Property BatchId As Integer
        Public Property DisplayText As String
        Public Overrides Function ToString() As String
            Return DisplayText
        End Function
    End Class

    Public Class ShrinkageViewModel
        Inherits ObservableValidator

        Private ReadOnly _shrinkageService As IShrinkageService
        Private ReadOnly _stockService As IStockService
        Private ReadOnly _conflictPresenter As IConflictPresenter
        Private ReadOnly _reasonOptions As String() = {"Damage", "Spoilage", "Expiry", "Admin Error"}
        Private _allHistory As New List(Of ShrinkageRowItem)()

        Public Sub New(shrinkageService As IShrinkageService, stockService As IStockService, conflictPresenter As IConflictPresenter)
            _shrinkageService = shrinkageService
            _stockService = stockService
            _conflictPresenter = conflictPresenter

            HistoryItems = New ObservableCollection(Of ShrinkageRowItem)()
            FilterProducts = New ObservableCollection(Of ShrinkageProductItem)()
            DialogProducts = New ObservableCollection(Of ShrinkageProductItem)()
            DialogBatches = New ObservableCollection(Of ShrinkageBatchItem)()

            _filterStartDate = New DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
            _filterEndDate = DateTime.Now.Date

            LoadDataCommand = New AsyncRelayCommand(AddressOf LoadDataAsync)
            OpenDialogCommand = New RelayCommand(AddressOf OpenDialog)
            CancelDialogCommand = New RelayCommand(AddressOf CloseDialog)
            ExecuteRecordCommand = New AsyncRelayCommand(AddressOf ExecuteRecordAsync, Function() Not HasErrors)

            Dim t = LoadDataAsync()
        End Sub

        ' ─── Collections ─────────────────────────────────────────────────────────

        Public Property HistoryItems As ObservableCollection(Of ShrinkageRowItem)
        Public Property FilterProducts As ObservableCollection(Of ShrinkageProductItem)
        Public Property DialogProducts As ObservableCollection(Of ShrinkageProductItem)
        Public Property DialogBatches As ObservableCollection(Of ShrinkageBatchItem)

        Public ReadOnly Property ReasonOptions As String()
            Get
                Return _reasonOptions
            End Get
        End Property

        ' ─── Filters ─────────────────────────────────────────────────────────────

        Private _filterStartDate As DateTime
        Public Property FilterStartDate As DateTime
            Get
                Return _filterStartDate
            End Get
            Set(value As DateTime)
                If SetProperty(_filterStartDate, value) Then ApplyFilters()
            End Set
        End Property

        Private _filterEndDate As DateTime
        Public Property FilterEndDate As DateTime
            Get
                Return _filterEndDate
            End Get
            Set(value As DateTime)
                If SetProperty(_filterEndDate, value) Then ApplyFilters()
            End Set
        End Property

        Private _filterSelectedProduct As ShrinkageProductItem
        Public Property FilterSelectedProduct As ShrinkageProductItem
            Get
                Return _filterSelectedProduct
            End Get
            Set(value As ShrinkageProductItem)
                If SetProperty(_filterSelectedProduct, value) Then ApplyFilters()
            End Set
        End Property

        Private _filterReason As String = String.Empty
        Public Property FilterReason As String
            Get
                Return _filterReason
            End Get
            Set(value As String)
                If SetProperty(_filterReason, value) Then ApplyFilters()
            End Set
        End Property

        ' ─── Summary ─────────────────────────────────────────────────────────────

        Private _periodTotalValue As Decimal
        Public Property PeriodTotalValue As Decimal
            Get
                Return _periodTotalValue
            End Get
            Set(value As Decimal)
                SetProperty(_periodTotalValue, value)
            End Set
        End Property

        Private _filteredCount As Integer
        Public Property FilteredCount As Integer
            Get
                Return _filteredCount
            End Get
            Set(value As Integer)
                SetProperty(_filteredCount, value)
            End Set
        End Property

        ' ─── Dialog ──────────────────────────────────────────────────────────────

        Private _isDialogOpen As Boolean
        Public Property IsDialogOpen As Boolean
            Get
                Return _isDialogOpen
            End Get
            Set(value As Boolean)
                SetProperty(_isDialogOpen, value)
            End Set
        End Property

        Private _dialogSelectedProduct As ShrinkageProductItem
        Public Property DialogSelectedProduct As ShrinkageProductItem
            Get
                Return _dialogSelectedProduct
            End Get
            Set(value As ShrinkageProductItem)
                If SetProperty(_dialogSelectedProduct, value) Then
                    Dim t = LoadDialogBatchesAsync()
                End If
            End Set
        End Property

        Private _dialogQuantity As Integer = 1
        <Range(1, Integer.MaxValue, ErrorMessage:="Quantity must be greater than zero.")>
        Public Property DialogQuantity As Integer
            Get
                Return _dialogQuantity
            End Get
            Set(value As Integer)
                If SetProperty(_dialogQuantity, value, True) Then
                    If ExecuteRecordCommand IsNot Nothing Then ExecuteRecordCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _dialogReason As String = "Damage"
        <Required(ErrorMessage:="Reason is required.")>
        Public Property DialogReason As String
            Get
                Return _dialogReason
            End Get
            Set(value As String)
                If SetProperty(_dialogReason, value, True) Then
                    If ExecuteRecordCommand IsNot Nothing Then ExecuteRecordCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Private _dialogNotes As String = String.Empty
        Public Property DialogNotes As String
            Get
                Return _dialogNotes
            End Get
            Set(value As String)
                SetProperty(_dialogNotes, value)
            End Set
        End Property

        Private _dialogSelectedBatch As ShrinkageBatchItem
        Public Property DialogSelectedBatch As ShrinkageBatchItem
            Get
                Return _dialogSelectedBatch
            End Get
            Set(value As ShrinkageBatchItem)
                SetProperty(_dialogSelectedBatch, value)
            End Set
        End Property

        Private _dialogAvailableStock As Integer
        Public Property DialogAvailableStock As Integer
            Get
                Return _dialogAvailableStock
            End Get
            Set(value As Integer)
                SetProperty(_dialogAvailableStock, value)
            End Set
        End Property

        ' ─── Status ──────────────────────────────────────────────────────────────

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                SetProperty(_isBusy, value)
                OnPropertyChanged(NameOf(IsEmpty))
            End Set
        End Property

        Private _isError As Boolean
        Public Property IsError As Boolean
            Get
                Return _isError
            End Get
            Set(value As Boolean)
                SetProperty(_isError, value)
                OnPropertyChanged(NameOf(IsEmpty))
            End Set
        End Property

        Private _errorMessage As String = String.Empty
        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Set(value As String)
                SetProperty(_errorMessage, value)
            End Set
        End Property

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return HistoryItems.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
        End Property

        Private _lastRefreshed As String = String.Empty
        Public Property LastRefreshed As String
            Get
                Return _lastRefreshed
            End Get
            Set(value As String)
                SetProperty(_lastRefreshed, value)
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

        Private _isStatusError As Boolean
        Public Property IsStatusError As Boolean
            Get
                Return _isStatusError
            End Get
            Set(value As Boolean)
                SetProperty(_isStatusError, value)
            End Set
        End Property

        ' ─── Commands ────────────────────────────────────────────────────────────

        Public Property LoadDataCommand As AsyncRelayCommand
        Public Property OpenDialogCommand As RelayCommand
        Public Property CancelDialogCommand As RelayCommand
        Public Property ExecuteRecordCommand As AsyncRelayCommand

        ' ─── Validation ──────────────────────────────────────────────────────────

        Public Function ValidateDialog() As String
            If DialogSelectedProduct Is Nothing Then Return "Select a product."
            If DialogAvailableStock = 0 Then Return $"No available stock for {DialogSelectedProduct.ProductName}."
            If DialogQuantity <= 0 Then Return "Quantity must be greater than zero."
            If DialogQuantity > DialogAvailableStock Then Return $"Quantity exceeds available stock ({DialogAvailableStock} units)."
            If String.IsNullOrWhiteSpace(DialogReason) Then Return "Select a reason."
            Return String.Empty
        End Function

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadDataAsync() As Task
            IsError = False
            IsBusy = True
            Try
                Dim histTask = _shrinkageService.GetShrinkageHistoryAsync()
                Dim stockTask = _stockService.GetCurrentStockAsync(Nothing)
                Await Task.WhenAll(histTask, stockTask)

                _allHistory.Clear()
                For Each rec In histTask.Result
                    _allHistory.Add(New ShrinkageRowItem With {
                        .Id = rec.Id,
                        .ProductId = rec.ProductId,
                        .RecordedDate = rec.RecordedDate,
                        .ProductName = If(rec.Product IsNot Nothing, rec.Product.Name, $"Product #{rec.ProductId}"),
                        .QuantityLost = rec.QuantityLost,
                        .UnitCost = rec.UnitCost,
                        .TotalValue = rec.TotalValue,
                        .Reason = rec.Reason,
                        .Notes = If(rec.Notes, String.Empty),
                        .RecordedBy = If(rec.CreatedBy, String.Empty)
                    })
                Next

                Dim productList = stockTask.Result

                FilterProducts.Clear()
                FilterProducts.Add(New ShrinkageProductItem With {
                    .ProductId = 0,
                    .ProductName = "(All Products)",
                    .DisplayText = "(All Products)",
                    .CurrentStock = 0
                })
                For Each s In productList
                    FilterProducts.Add(New ShrinkageProductItem With {
                        .ProductId = s.ProductId,
                        .ProductName = s.ProductName,
                        .DisplayText = s.ProductName,
                        .CurrentStock = s.CurrentQuantity
                    })
                Next
                If _filterSelectedProduct Is Nothing Then
                    _filterSelectedProduct = FilterProducts.FirstOrDefault()
                    OnPropertyChanged(NameOf(FilterSelectedProduct))
                End If

                DialogProducts.Clear()
                For Each s In productList
                    DialogProducts.Add(New ShrinkageProductItem With {
                        .ProductId = s.ProductId,
                        .ProductName = s.ProductName,
                        .DisplayText = $"{s.ProductName}  (Stock: {s.CurrentQuantity})",
                        .CurrentStock = s.CurrentQuantity
                    })
                Next

                ApplyFilters()
                LastRefreshed = $"Refreshed {DateTime.Now:HH:mm:ss}"
                IsError = False
            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function LoadDialogBatchesAsync() As Task
            DialogBatches.Clear()
            DialogBatches.Add(New ShrinkageBatchItem With {
                .BatchId = 0,
                .DisplayText = "(Auto — FIFO)"
            })
            DialogSelectedBatch = DialogBatches.First()
            DialogAvailableStock = 0

            If DialogSelectedProduct Is Nothing Then Return

            Dim batches = Await _stockService.GetStockBatchesAsync(DialogSelectedProduct.ProductId)
            Dim available = batches.Where(Function(b) b.QuantityRemaining > 0) _
                                   .OrderBy(Function(b) b.ReceiptDate) _
                                   .ToList()

            For Each b In available
                Dim label As String = $"Batch #{b.Id} — {b.QuantityRemaining} units @ ₱{b.UnitCost:N2}"
                If b.ExpiryDate.HasValue Then
                    label &= $" (exp {b.ExpiryDate.Value:MM/dd/yyyy})"
                End If
                DialogBatches.Add(New ShrinkageBatchItem With {
                    .BatchId = b.Id,
                    .DisplayText = label
                })
            Next

            Dim total As Integer = 0
            For Each b In available
                total += b.QuantityRemaining
            Next
            DialogAvailableStock = total
        End Function

        Private Sub ApplyFilters()
            Dim startDay As DateTime = FilterStartDate.Date
            Dim endDay As DateTime = FilterEndDate.Date.AddDays(1).AddTicks(-1)

            Dim filtered As New List(Of ShrinkageRowItem)()
            For Each r In _allHistory
                Dim localDate As DateTime = r.RecordedDate.ToLocalTime()
                If localDate < startDay OrElse localDate > endDay Then Continue For
                If FilterSelectedProduct IsNot Nothing AndAlso FilterSelectedProduct.ProductId <> 0 Then
                    If r.ProductId <> FilterSelectedProduct.ProductId Then Continue For
                End If
                If Not String.IsNullOrEmpty(FilterReason) Then
                    If r.Reason <> FilterReason Then Continue For
                End If
                filtered.Add(r)
            Next

            HistoryItems.Clear()
            For Each r In filtered
                HistoryItems.Add(r)
            Next

            Dim total As Decimal = 0D
            For Each r In filtered
                total += r.TotalValue
            Next
            PeriodTotalValue = total
            FilteredCount = filtered.Count
            OnPropertyChanged(NameOf(IsEmpty))
        End Sub

        ' ─── Dialog Logic ─────────────────────────────────────────────────────────

        Private Sub ClearDialogErrors()
            ClearErrors(NameOf(DialogQuantity))
            ClearErrors(NameOf(DialogReason))
        End Sub

        Private Sub OpenDialog()
            DialogSelectedProduct = Nothing
            _dialogQuantity = 1
            _dialogReason = "Damage"
            DialogNotes = String.Empty
            DialogSelectedBatch = Nothing
            DialogAvailableStock = 0
            DialogBatches.Clear()
            DialogBatches.Add(New ShrinkageBatchItem With {
                .BatchId = 0,
                .DisplayText = "(Auto — FIFO)"
            })
            ClearDialogErrors()
            OnPropertyChanged(NameOf(DialogQuantity))
            OnPropertyChanged(NameOf(DialogReason))
            IsDialogOpen = True
            If ExecuteRecordCommand IsNot Nothing Then ExecuteRecordCommand.NotifyCanExecuteChanged()
        End Sub

        Private Sub CloseDialog()
            IsDialogOpen = False
            ClearDialogErrors()
        End Sub

        Public Async Function ExecuteRecordAsync() As Task
            ValidateAllProperties()
            If HasErrors Then Return

            IsBusy = True
            StatusMessage = String.Empty
            IsStatusError = False
            IsDialogOpen = False
            Dim generalError As Boolean = False
            Dim generalErrorMessage As String = String.Empty
            Dim recordedQty As Integer = 0
            Dim recordedValue As Decimal = 0D
            Dim recordedProductName As String = If(DialogSelectedProduct IsNot Nothing, DialogSelectedProduct.ProductName, String.Empty)

            Try
                Dim batchId As Integer? = Nothing
                If DialogSelectedBatch IsNot Nothing AndAlso DialogSelectedBatch.BatchId <> 0 Then
                    batchId = DialogSelectedBatch.BatchId
                End If

                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        Dim rec = Await _shrinkageService.RecordShrinkageAsync(
                            DialogSelectedProduct.ProductId,
                            DialogQuantity,
                            DialogReason,
                            DialogNotes,
                            batchId)
                        recordedQty = rec.QuantityLost
                        recordedValue = rec.TotalValue
                    End Function,
                    AddressOf LoadDataAsync,
                    _conflictPresenter)

                If saved Then
                    StatusMessage = $"Recorded: {recordedProductName} × {recordedQty} unit(s) — ₱{recordedValue:N2}."
                    IsStatusError = False
                    Await LoadDataAsync()
                End If
            Catch ex As Exception
                generalError = True
                generalErrorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If generalError Then
                StatusMessage = $"Failed: {generalErrorMessage}"
                IsStatusError = True
                IsDialogOpen = True
            End If
        End Function

    End Class

End Namespace
