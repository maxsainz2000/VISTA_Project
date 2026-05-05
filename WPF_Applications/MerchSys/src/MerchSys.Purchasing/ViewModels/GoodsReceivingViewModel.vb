Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.Purchasing.Dtos
Imports MerchSys.Purchasing.Entities
Imports MerchSys.Purchasing.Services
Imports MerchSys.SharedKernel.Enums

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
    ''' QtyReceived, UnitCost, ExpiryDate, and DiscrepancyNotes before confirming.
    ''' HasDiscrepancy auto-recalculates when QtyReceived changes.
    ''' </summary>
    Public Class GRLineItem
        Inherits ObservableObject

        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property QtyOrdered As Integer

        Private _qtyReceived As Integer
        Public Property QtyReceived As Integer
            Get
                Return _qtyReceived
            End Get
            Set(value As Integer)
                If SetProperty(_qtyReceived, value) Then
                    OnPropertyChanged(NameOf(HasDiscrepancy))
                End If
            End Set
        End Property

        Private _unitCost As Decimal
        Public Property UnitCost As Decimal
            Get
                Return _unitCost
            End Get
            Set(value As Decimal)
                SetProperty(_unitCost, value)
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

        Public ReadOnly Property HasDiscrepancy As Boolean
            Get
                Return QtyReceived <> QtyOrdered
            End Get
        End Property

    End Class

    ''' <summary>
    ''' ViewModel for the Goods Receiving screen.
    ''' Loads Submitted POs, pre-fills receiving lines from the selected PO, and calls
    ''' IGoodsReceivingService.ReceiveGoodsAsync on confirmation.
    ''' </summary>
    Public Class GoodsReceivingViewModel
        Inherits ObservableObject

        Private ReadOnly _poService As IPurchaseOrderService
        Private ReadOnly _grService As IGoodsReceivingService

        ' Backing field for SelectedPO so ConfirmReceiptAsync can reset without
        ' re-triggering the setter's LoadPOLinesAsync call.
        Private _selectedPO As POSelectorItem

        Public Sub New(poService As IPurchaseOrderService, grService As IGoodsReceivingService)
            _poService = poService
            _grService = grService

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

        Public Property LoadPOsCommand As AsyncRelayCommand
        Public Property ConfirmReceiptCommand As AsyncRelayCommand

        ' ─── CanExecute ───────────────────────────────────────────────────────────

        Private Function CanConfirm() As Boolean
            Return _selectedPO IsNot Nothing AndAlso ReceivingLines.Any() AndAlso Not IsBusy
        End Function

        ' ─── Data Loading ─────────────────────────────────────────────────────────

        Private Async Function LoadSubmittedPOsAsync() As Task
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
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function LoadPOLinesAsync() As Task
            If _selectedPO Is Nothing Then
                ReceivingLines.Clear()
                IsPOSelected = False
                ConfirmReceiptCommand.NotifyCanExecuteChanged()
                Return
            End If
            IsBusy = True
            Try
                Dim po = Await _poService.GetByIdAsync(_selectedPO.Id)
                ReceivingLines.Clear()
                If po IsNot Nothing Then
                    For Each line In po.Lines
                        ReceivingLines.Add(New GRLineItem With {
                            .ProductId = line.ProductId,
                            .ProductName = line.ProductName,
                            .QtyOrdered = line.QuantityOrdered,
                            .QtyReceived = line.QuantityOrdered,
                            .UnitCost = line.UnitCost
                        })
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
            Try
                Dim dtos = ReceivingLines.Select(Function(rl) New ReceiveGoodsLineDto With {
                    .ProductId = rl.ProductId,
                    .ProductName = rl.ProductName,
                    .QuantityOrdered = rl.QtyOrdered,
                    .QuantityReceived = rl.QtyReceived,
                    .UnitCost = rl.UnitCost,
                    .ExpiryDate = rl.ExpiryDate,
                    .DiscrepancyNotes = If(rl.HasDiscrepancy, rl.DiscrepancyNotes, Nothing)
                }).ToList()

                Dim receipt = Await _grService.ReceiveGoodsAsync(_selectedPO.Id, dtos)
                StatusMessage = $"Receipt {receipt.ReceiptNumber} confirmed — PO marked Received."

                ' Reset without re-triggering LoadPOLinesAsync
                _selectedPO = Nothing
                OnPropertyChanged(NameOf(SelectedPO))
                ReceivingLines.Clear()
                IsPOSelected = False
                ConfirmReceiptCommand.NotifyCanExecuteChanged()

                Await LoadSubmittedPOsAsync()
            Catch ex As InvalidOperationException
                StatusMessage = $"Receipt failed: {ex.Message}"
            Finally
                IsBusy = False
            End Try
        End Function

    End Class

End Namespace
