Imports System.Collections.ObjectModel
Imports System.ComponentModel.DataAnnotations
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Dtos
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace ViewModels

    ''' <summary>
    ''' Displays a submitted PO in a dropdown; used to identify which PO is being received.
    ''' </summary>
    Public Class POSelectorItem
        Public Property Id As Integer
        Public Property DisplayText As String
    End Class

    ''' <summary>
    ''' One line in the receiving grid. Pre-populated from the PO line; manager edits
    ''' QtyReceived, UnitCost, ExpiryDate, VatClassification, and DiscrepancyNotes before confirming.
    ''' HasDiscrepancy and VatAmount auto-recalculate on relevant property changes.
    ''' </summary>
    Public Class GRLineItem
        Inherits ObservableValidator

        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QtyOrdered As Integer

        Private _qtyReceived As Integer
        <Range(0, Integer.MaxValue, ErrorMessage:="Quantity must be non-negative.")>
        Public Property QtyReceived As Integer
            Get
                Return _qtyReceived
            End Get
            Set(value As Integer)
                If SetProperty(_qtyReceived, value, True) Then
                    OnPropertyChanged(NameOf(HasDiscrepancy))
                    OnPropertyChanged(NameOf(VatAmount))
                End If
            End Set
        End Property

        Private _unitCost As Decimal
        <Range(0.01, Double.MaxValue, ErrorMessage:="Unit cost must be greater than zero.")>
        Public Property UnitCost As Decimal
            Get
                Return _unitCost
            End Get
            Set(value As Decimal)
                If SetProperty(_unitCost, value, True) Then
                    OnPropertyChanged(NameOf(VatAmount))
                End If
            End Set
        End Property

        Private _expiryDate As DateTime?
        Public Property ExpiryDate As DateTime?
            Get
                Return _expiryDate
            End Get
            Set(value As DateTime?)
                SetProperty(_expiryDate, value)
            End Set
        End Property

        Private _discrepancyNotes As String = String.Empty
        Public Property DiscrepancyNotes As String
            Get
                Return _discrepancyNotes
            End Get
            Set(value As String)
                SetProperty(_discrepancyNotes, value)
            End Set
        End Property

        Private _vatClassification As VatTreatment = VatTreatment.Vatable
        Public Property VatClassification As VatTreatment
            Get
                Return _vatClassification
            End Get
            Set(value As VatTreatment)
                If SetProperty(_vatClassification, value) Then
                    OnPropertyChanged(NameOf(VatAmount))
                End If
            End Set
        End Property

        ''' <summary>Auto-computed input VAT for this line based on VatClassification and line total.</summary>
        Public ReadOnly Property VatAmount As Decimal
            Get
                Dim lineTotal As Decimal = CDec(QtyReceived) * UnitCost
                If _vatClassification = VatTreatment.Vatable Then
                    Return Math.Round(lineTotal - Math.Round(lineTotal / 1.12D, 2), 2)
                End If
                Return 0D
            End Get
        End Property

        Public ReadOnly Property HasDiscrepancy As Boolean
            Get
                Return QtyReceived <> QtyOrdered
            End Get
        End Property

        Public Sub Validate()
            ValidateAllProperties()
        End Sub

    End Class

    ''' <summary>
    ''' ViewModel for the Goods Receiving screen.
    ''' Loads Submitted POs, pre-fills receiving lines from the selected PO, and calls
    ''' IGoodsReceivingService.ReceiveGoodsAsync on confirmation.
    ''' </summary>
    Public Class GoodsReceivingViewModel
        Inherits ObservableValidator

        Private ReadOnly _poService As IPurchaseOrderService
        Private ReadOnly _grService As IGoodsReceivingService
        Private ReadOnly _conflictPresenter As IConflictPresenter
        Private ReadOnly _notifications As INotificationService

        ' Backing field for SelectedPO so ConfirmReceiptAsync can reset without
        ' re-triggering the setter's LoadPOLinesAsync call.
        Private _selectedPO As POSelectorItem

        Public Sub New(poService As IPurchaseOrderService,
                       grService As IGoodsReceivingService,
                       conflictPresenter As IConflictPresenter,
                       notifications As INotificationService)
            _poService = poService
            _grService = grService
            _conflictPresenter = conflictPresenter
            _notifications = notifications

            SubmittedPOs = New ObservableCollection(Of POSelectorItem)()
            ReceivingLines = New ObservableCollection(Of GRLineItem)()

            LoadPOsCommand = New AsyncRelayCommand(AddressOf LoadSubmittedPOsAsync)
            ConfirmReceiptCommand = New AsyncRelayCommand(AddressOf ConfirmReceiptAsync, Function() CanConfirm())

            Dim initTask = LoadSubmittedPOsAsync()
        End Sub

        ' ─── PO Selector ─────────────────────────────────────────────────────────

        Public Property SubmittedPOs As ObservableCollection(Of POSelectorItem)

        Public Property SelectedPO As POSelectorItem
            Get
                Return _selectedPO
            End Get
            Set(value As POSelectorItem)
                If SetProperty(_selectedPO, value) Then
                    Dim t = LoadPOLinesAsync()
                    ConfirmReceiptCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        ' ─── Receiving Grid ──────────────────────────────────────────────────────

        Public Property ReceivingLines As ObservableCollection(Of GRLineItem)

        ' ─── State ────────────────────────────────────────────────────────────────

        Private _isPOSelected As Boolean
        Public Property IsPOSelected As Boolean
            Get
                Return _isPOSelected
            End Get
            Set(value As Boolean)
                SetProperty(_isPOSelected, value)
            End Set
        End Property

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                If SetProperty(_isBusy, value) Then
                    ConfirmReceiptCommand.NotifyCanExecuteChanged()
                    OnPropertyChanged(NameOf(IsEmpty))
                End If
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
                Return SubmittedPOs.Count = 0 AndAlso Not IsBusy AndAlso Not IsError
            End Get
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

        Public Property LoadPOsCommand As AsyncRelayCommand
        Public Property ConfirmReceiptCommand As AsyncRelayCommand

        ' ─── Enum Source ──────────────────────────────────────────────────────────

        ''' <summary>Provides ComboBox ItemsSource for VatClassification column in the receiving grid.</summary>
        Public ReadOnly Property VatTreatmentValues As List(Of VatTreatment) =
            New List(Of VatTreatment) From {VatTreatment.Vatable, VatTreatment.Exempt, VatTreatment.ZeroRated}

        ' ─── CanExecute ───────────────────────────────────────────────────────────

        Private Function CanConfirm() As Boolean
            Return _selectedPO IsNot Nothing AndAlso ReceivingLines.Any() AndAlso Not IsBusy AndAlso ReceivingLines.All(Function(rl) Not rl.HasErrors)
        End Function

        Private Sub OnLineItemPropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
            If e.PropertyName = NameOf(GRLineItem.QtyReceived) OrElse e.PropertyName = NameOf(GRLineItem.UnitCost) OrElse e.PropertyName = "HasErrors" Then
                ConfirmReceiptCommand.NotifyCanExecuteChanged()
            End If
        End Sub

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadSubmittedPOsAsync() As Task
            IsError = False
            IsBusy = True
            Try
                Dim pos = Await _poService.GetAllAsync(PurchaseOrderStatus.Submitted)
                SubmittedPOs.Clear()
                For Each p In pos
                    SubmittedPOs.Add(New POSelectorItem With {
                        .Id = p.Id,
                        .DisplayText = $"{p.OrderNumber} — {If(p.Vendor IsNot Nothing, p.Vendor.Name, $"Vendor #{p.VendorId}")}"
                    })
                Next
                StatusMessage = If(SubmittedPOs.Any(),
                    $"{SubmittedPOs.Count} submitted PO(s) awaiting receipt.",
                    "No submitted POs found.")
                IsError = False
            Catch ex As Exception
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function LoadPOLinesAsync() As Task
            If _selectedPO Is Nothing Then
                For Each rl In ReceivingLines
                    RemoveHandler rl.PropertyChanged, AddressOf OnLineItemPropertyChanged
                Next
                ReceivingLines.Clear()
                IsPOSelected = False
                ConfirmReceiptCommand.NotifyCanExecuteChanged()
                Return
            End If
            IsBusy = True
            Try
                Dim po = Await _poService.GetByIdAsync(_selectedPO.Id)
                For Each rl In ReceivingLines
                    RemoveHandler rl.PropertyChanged, AddressOf OnLineItemPropertyChanged
                Next
                ReceivingLines.Clear()
                If po IsNot Nothing Then
                    For Each line In po.Lines
                        Dim rl = New GRLineItem With {
                            .ProductId = line.ProductId,
                            .ProductName = line.ProductName,
                            .QtyOrdered = line.QuantityOrdered,
                            .QtyReceived = line.QuantityOrdered,
                            .UnitCost = line.UnitCost,
                            .VatClassification = VatTreatment.Vatable
                        }
                        rl.Validate()
                        AddHandler rl.PropertyChanged, AddressOf OnLineItemPropertyChanged
                        ReceivingLines.Add(rl)
                    Next
                End If
                IsPOSelected = True
                ConfirmReceiptCommand.NotifyCanExecuteChanged()
            Finally
                IsBusy = False
            End Try
        End Function

        ' ─── Confirm Receipt ──────────────────────────────────────────────────────

        Private Async Function ConfirmReceiptAsync() As Task
            If _selectedPO Is Nothing Then Return

            If Not ReceivingLines.Any() Then
                StatusMessage = "No lines to receive."
                Return
            End If
            If ReceivingLines.All(Function(rl) rl.QtyReceived = 0) Then
                StatusMessage = "At least one line must have a received quantity greater than zero."
                Return
            End If

            Dim missingNotes = ReceivingLines.
                Where(Function(rl) rl.HasDiscrepancy AndAlso String.IsNullOrWhiteSpace(rl.DiscrepancyNotes)).
                Select(Function(rl) rl.ProductName).
                ToList()
            If missingNotes.Any() Then
                StatusMessage = $"Discrepancy notes required for: {String.Join(", ", missingNotes)}"
                Return
            End If

            IsBusy = True
            Dim invalidOperationError As Boolean = False
            Dim errorMessage As String = String.Empty
            Dim poId = _selectedPO.Id
            Dim grReceipt As GoodsReceipt = Nothing

            Try
                Dim dtos = ReceivingLines.Select(Function(rl) New ReceiveGoodsLineDto With {
                    .ProductId = rl.ProductId,
                    .ProductName = rl.ProductName,
                    .QuantityOrdered = rl.QtyOrdered,
                    .QuantityReceived = rl.QtyReceived,
                    .UnitCost = rl.UnitCost,
                    .ExpiryDate = rl.ExpiryDate,
                    .DiscrepancyNotes = If(rl.HasDiscrepancy, rl.DiscrepancyNotes, Nothing),
                    .VatClassification = rl.VatClassification
                }).ToList()

                Dim saved = Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        Dim rcpt = Await _grService.ReceiveGoodsAsync(poId, dtos)
                        grReceipt = rcpt
                    End Function,
                    Async Function()
                        For Each rl In ReceivingLines
                            RemoveHandler rl.PropertyChanged, AddressOf OnLineItemPropertyChanged
                        Next
                        _selectedPO = Nothing
                        OnPropertyChanged(NameOf(SelectedPO))
                        ReceivingLines.Clear()
                        IsPOSelected = False
                        ConfirmReceiptCommand.NotifyCanExecuteChanged()
                        Await LoadSubmittedPOsAsync()
                    End Function,
                    _conflictPresenter)

                If saved Then
                    StatusMessage = $"Receipt {grReceipt.ReceiptNumber} confirmed — PO marked Received."
                    _notifications.ShowSuccess($"Receipt {grReceipt.ReceiptNumber} confirmed.")

                    For Each rl In ReceivingLines
                        RemoveHandler rl.PropertyChanged, AddressOf OnLineItemPropertyChanged
                    Next
                    _selectedPO = Nothing
                    OnPropertyChanged(NameOf(SelectedPO))
                    ReceivingLines.Clear()
                    IsPOSelected = False
                    ConfirmReceiptCommand.NotifyCanExecuteChanged()

                    Await LoadSubmittedPOsAsync()
                End If
            Catch ex As InvalidOperationException
                invalidOperationError = True
                errorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If invalidOperationError Then
                StatusMessage = $"Receipt failed: {errorMessage}"
            End If
        End Function

    End Class

End Namespace
