Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MerchSys.POS.Entities
Imports MerchSys.POS.Services
Imports MerchSys.SharedKernel.Enums

Namespace ViewModels

    ''' <summary>
    ''' A single row in the transaction history DataGrid.
    ''' Inherits ObservableObject so that HasReturns can be updated live after a return is processed.
    ''' </summary>
    Public Class TransactionSummaryItem
        Inherits ObservableObject

        Public Property TransactionId As Integer
        Public Property TransactionNumber As String
        Public Property TransactionDate As DateTime
        Public Property CustomerName As String
        Public Property PaymentMethod As String
        Public Property TotalAmount As Decimal
        Public Property ItemCount As Integer
        Public Property IsVoided As Boolean

        Private _hasReturns As Boolean
        Public Property HasReturns As Boolean
            Get
                Return _hasReturns
            End Get
            Set(value As Boolean)
                If SetProperty(_hasReturns, value) Then
                    OnPropertyChanged(NameOf(HasReturnsText))
                End If
            End Set
        End Property

        Public ReadOnly Property HasReturnsText As String
            Get
                Return If(HasReturns, "Yes", "—")
            End Get
        End Property

        Public ReadOnly Property StatusText As String
            Get
                Return If(IsVoided, "Voided", "Completed")
            End Get
        End Property

        Friend Property Lines As ICollection(Of SalesTransactionLine)
    End Class

    ''' <summary>A line item row shown in the detail panel.</summary>
    Public Class TransactionDetailLine
        Public Property ProductName As String
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property DiscountAmount As Decimal
        Public Property LineTotal As Decimal
    End Class

    ''' <summary>A return record shown in the returns tab of the detail panel.</summary>
    Public Class ReturnSummaryItem
        Public Property ReturnDate As DateTime
        Public Property ProductName As String
        Public Property QuantityReturned As Integer
        Public Property RefundAmount As Decimal
        Public Property Reason As String
        Public ReadOnly Property RestockedText As String
            Get
                Return If(IsRestocked, "Yes", "No")
            End Get
        End Property
        Public Property IsRestocked As Boolean
    End Class

    ''' <summary>Represents one returnable product line in the return dialog.</summary>
    Public Class ReturnLineSelection
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property OriginalQuantity As Integer
        Public Property AlreadyReturned As Integer

        Public ReadOnly Property MaxReturnable As Integer
            Get
                Return OriginalQuantity - AlreadyReturned
            End Get
        End Property

        Public ReadOnly Property DisplayText As String
            Get
                Return $"{ProductName}  (original: {OriginalQuantity}, already returned: {AlreadyReturned}, max: {MaxReturnable})"
            End Get
        End Property
    End Class

    ''' <summary>
    ''' ViewModel for the Transaction History screen.
    ''' Supports date/TX#/payment-method filtering, transaction detail display,
    ''' receipt print-preview, and return processing.
    ''' </summary>
    Public Class TransactionHistoryViewModel
        Inherits ObservableObject

        Private ReadOnly _cartService As ICartService
        Private ReadOnly _returnService As ISalesReturnService
        Private ReadOnly _receiptService As IReceiptService

        Private _selectedReceiptId As Integer

        ' ── Filter state ──────────────────────────────────────────────────────────

        Private _dateFrom As DateTime = New DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
        Private _dateTo As DateTime = DateTime.Today
        Private _txNumberFilter As String = ""
        Private _selectedPaymentMethodFilter As String = "All"

        Public ReadOnly Property PaymentMethodFilters As List(Of String) =
            New List(Of String) From {"All", "Cash", "GCash", "BankTransfer", "Credit"}

        ' ── Transaction list ──────────────────────────────────────────────────────

        Private _transactions As New ObservableCollection(Of TransactionSummaryItem)()
        Private _selectedTransaction As TransactionSummaryItem

        ' ── Detail panel ──────────────────────────────────────────────────────────

        Private _detailLines As New ObservableCollection(Of TransactionDetailLine)()
        Private _detailReturns As New ObservableCollection(Of ReturnSummaryItem)()
        Private _receiptNumber As String = ""
        Private _receiptDate As String = ""
        Private _receiptBusinessName As String = ""
        Private _receiptTin As String = ""
        Private _hasReceipt As Boolean
        Private _isDetailVisible As Boolean

        ' ── Return dialog ─────────────────────────────────────────────────────────

        Private _isReturnDialogVisible As Boolean
        Private _returnableLines As New ObservableCollection(Of ReturnLineSelection)()
        Private _selectedReturnLine As ReturnLineSelection
        Private _returnQuantity As Integer = 1
        Private _returnReason As String = ""
        Private _shouldRestock As Boolean = True
        Private _returnMaxQty As Integer

        ' ── Status ────────────────────────────────────────────────────────────────

        Private _statusMessage As String = ""
        Private _isStatusSuccess As Boolean
        Private _isBusy As Boolean

        ' ── Commands ──────────────────────────────────────────────────────────────

        Public ReadOnly Property SearchCommand As AsyncRelayCommand
        Public ReadOnly Property ClearFiltersCommand As RelayCommand
        Public ReadOnly Property SelectTransactionCommand As AsyncRelayCommand(Of TransactionSummaryItem)
        Public ReadOnly Property ViewReceiptCommand As AsyncRelayCommand
        Public ReadOnly Property OpenReturnDialogCommand As AsyncRelayCommand
        Public ReadOnly Property ProcessReturnCommand As AsyncRelayCommand
        Public ReadOnly Property CancelReturnCommand As RelayCommand

        ' ── Constructor ───────────────────────────────────────────────────────────

        Public Sub New(cartService As ICartService,
                       returnService As ISalesReturnService,
                       receiptService As IReceiptService)

            _cartService = cartService
            _returnService = returnService
            _receiptService = receiptService

            SearchCommand = New AsyncRelayCommand(AddressOf SearchAsync)
            ClearFiltersCommand = New RelayCommand(AddressOf ClearFilters)
            SelectTransactionCommand = New AsyncRelayCommand(Of TransactionSummaryItem)(AddressOf SelectTransactionAsync)
            ViewReceiptCommand = New AsyncRelayCommand(
                AddressOf ViewReceiptAsync,
                Function() SelectedTransaction IsNot Nothing AndAlso HasReceipt)
            OpenReturnDialogCommand = New AsyncRelayCommand(
                AddressOf OpenReturnDialogAsync,
                Function() SelectedTransaction IsNot Nothing AndAlso Not SelectedTransaction.IsVoided)
            ProcessReturnCommand = New AsyncRelayCommand(AddressOf ProcessReturnAsync, AddressOf CanProcessReturn)
            CancelReturnCommand = New RelayCommand(Sub() IsReturnDialogVisible = False)
        End Sub

        ' ── Filter properties ─────────────────────────────────────────────────────

        Public Property DateFrom As DateTime
            Get
                Return _dateFrom
            End Get
            Set(value As DateTime)
                SetProperty(_dateFrom, value)
            End Set
        End Property

        Public Property DateTo As DateTime
            Get
                Return _dateTo
            End Get
            Set(value As DateTime)
                SetProperty(_dateTo, value)
            End Set
        End Property

        Public Property TxNumberFilter As String
            Get
                Return _txNumberFilter
            End Get
            Set(value As String)
                SetProperty(_txNumberFilter, value)
            End Set
        End Property

        Public Property SelectedPaymentMethodFilter As String
            Get
                Return _selectedPaymentMethodFilter
            End Get
            Set(value As String)
                SetProperty(_selectedPaymentMethodFilter, value)
            End Set
        End Property

        ' ── Transaction list properties ───────────────────────────────────────────

        Public ReadOnly Property Transactions As ObservableCollection(Of TransactionSummaryItem)
            Get
                Return _transactions
            End Get
        End Property

        Public Property SelectedTransaction As TransactionSummaryItem
            Get
                Return _selectedTransaction
            End Get
            Set(value As TransactionSummaryItem)
                If SetProperty(_selectedTransaction, value) Then
                    OpenReturnDialogCommand.NotifyCanExecuteChanged()
                    ViewReceiptCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        ' ── Detail panel properties ────────────────────────────────────────────────

        Public ReadOnly Property DetailLines As ObservableCollection(Of TransactionDetailLine)
            Get
                Return _detailLines
            End Get
        End Property

        Public ReadOnly Property DetailReturns As ObservableCollection(Of ReturnSummaryItem)
            Get
                Return _detailReturns
            End Get
        End Property

        Public Property ReceiptNumber As String
            Get
                Return _receiptNumber
            End Get
            Set(value As String)
                SetProperty(_receiptNumber, value)
            End Set
        End Property

        Public Property ReceiptDate As String
            Get
                Return _receiptDate
            End Get
            Set(value As String)
                SetProperty(_receiptDate, value)
            End Set
        End Property

        Public Property ReceiptBusinessName As String
            Get
                Return _receiptBusinessName
            End Get
            Set(value As String)
                SetProperty(_receiptBusinessName, value)
            End Set
        End Property

        Public Property ReceiptTin As String
            Get
                Return _receiptTin
            End Get
            Set(value As String)
                SetProperty(_receiptTin, value)
            End Set
        End Property

        Public Property HasReceipt As Boolean
            Get
                Return _hasReceipt
            End Get
            Set(value As Boolean)
                If SetProperty(_hasReceipt, value) Then
                    ViewReceiptCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property IsDetailVisible As Boolean
            Get
                Return _isDetailVisible
            End Get
            Set(value As Boolean)
                SetProperty(_isDetailVisible, value)
            End Set
        End Property

        ' ── Return dialog properties ───────────────────────────────────────────────

        Public Property IsReturnDialogVisible As Boolean
            Get
                Return _isReturnDialogVisible
            End Get
            Set(value As Boolean)
                SetProperty(_isReturnDialogVisible, value)
            End Set
        End Property

        Public ReadOnly Property ReturnableLines As ObservableCollection(Of ReturnLineSelection)
            Get
                Return _returnableLines
            End Get
        End Property

        Public Property SelectedReturnLine As ReturnLineSelection
            Get
                Return _selectedReturnLine
            End Get
            Set(value As ReturnLineSelection)
                If SetProperty(_selectedReturnLine, value) Then
                    ReturnMaxQty = If(value IsNot Nothing, value.MaxReturnable, 0)
                    ReturnQuantity = If(value IsNot Nothing, 1, 0)
                    ProcessReturnCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property ReturnQuantity As Integer
            Get
                Return _returnQuantity
            End Get
            Set(value As Integer)
                If SetProperty(_returnQuantity, value) Then
                    ProcessReturnCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property ReturnReason As String
            Get
                Return _returnReason
            End Get
            Set(value As String)
                If SetProperty(_returnReason, value) Then
                    ProcessReturnCommand.NotifyCanExecuteChanged()
                End If
            End Set
        End Property

        Public Property ShouldRestock As Boolean
            Get
                Return _shouldRestock
            End Get
            Set(value As Boolean)
                SetProperty(_shouldRestock, value)
            End Set
        End Property

        Public Property ReturnMaxQty As Integer
            Get
                Return _returnMaxQty
            End Get
            Set(value As Integer)
                SetProperty(_returnMaxQty, value)
            End Set
        End Property

        ' ── Status properties ──────────────────────────────────────────────────────

        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Public Property IsStatusSuccess As Boolean
            Get
                Return _isStatusSuccess
            End Get
            Set(value As Boolean)
                SetProperty(_isStatusSuccess, value)
            End Set
        End Property

        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                SetProperty(_isBusy, value)
            End Set
        End Property

        ' ── Command implementations ────────────────────────────────────────────────

        Private Async Function SearchAsync() As Task
            IsBusy = True
            StatusMessage = ""
            IsDetailVisible = False
            Try
                Dim endOfDay = DateTo.Date.AddDays(1).AddTicks(-1)

                Dim results = Await _cartService.GetTransactionHistoryAsync(DateFrom.Date, endOfDay)

                ' Query returns by the actual loaded transaction IDs so returns for old transactions
                ' are found regardless of when the return was processed (Approach B, POS-11).
                Dim txIds = results.Select(Function(t) t.Id).ToList()
                Dim txIdsWithReturns = Await _returnService.GetTransactionIdsWithReturnsAsync(txIds)

                ' Apply in-memory filters
                Dim filtered = results.AsEnumerable()

                If Not String.IsNullOrWhiteSpace(TxNumberFilter) Then
                    Dim pattern = TxNumberFilter.Trim()
                    filtered = filtered.Where(
                        Function(t) t.TransactionNumber.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                End If

                If SelectedPaymentMethodFilter <> "All" Then
                    Dim pm = CType([Enum].Parse(GetType(PaymentMethod), SelectedPaymentMethodFilter), PaymentMethod)
                    filtered = filtered.Where(Function(t) t.PaymentMethod = pm)
                End If

                Transactions.Clear()
                For Each tx In filtered
                    Transactions.Add(New TransactionSummaryItem() With {
                        .TransactionId = tx.Id,
                        .TransactionNumber = tx.TransactionNumber,
                        .TransactionDate = tx.TransactionDate,
                        .CustomerName = If(String.IsNullOrWhiteSpace(tx.CustomerName), "Walk-in", tx.CustomerName),
                        .PaymentMethod = tx.PaymentMethod.ToString(),
                        .TotalAmount = tx.TotalAmount,
                        .ItemCount = If(tx.Lines IsNot Nothing, tx.Lines.Count, 0),
                        .HasReturns = txIdsWithReturns.Contains(tx.Id),
                        .IsVoided = tx.IsVoided,
                        .Lines = tx.Lines
                    })
                Next

                StatusMessage = $"{Transactions.Count} transaction(s) found."
                IsStatusSuccess = True
            Catch ex As Exception
                StatusMessage = $"Error loading transactions: {ex.Message}"
                IsStatusSuccess = False
            Finally
                IsBusy = False
            End Try
        End Function

        Private Sub ClearFilters()
            DateFrom = New DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            DateTo = DateTime.Today
            TxNumberFilter = ""
            SelectedPaymentMethodFilter = "All"
        End Sub

        Friend Async Function SelectTransactionAsync(item As TransactionSummaryItem) As Task
            If item Is Nothing Then Return

            SelectedTransaction = item
            IsBusy = True
            IsDetailVisible = False
            HasReceipt = False
            _selectedReceiptId = 0
            DetailLines.Clear()
            DetailReturns.Clear()
            ReceiptNumber = ""
            ReceiptDate = ""
            ReceiptBusinessName = ""
            ReceiptTin = ""

            Try
                ' Populate line items from the eagerly-loaded Lines collection
                If item.Lines IsNot Nothing Then
                    For Each line In item.Lines
                        DetailLines.Add(New TransactionDetailLine() With {
                            .ProductName = line.ProductName,
                            .Quantity = line.Quantity,
                            .UnitPrice = line.UnitPrice,
                            .DiscountAmount = line.DiscountAmount,
                            .LineTotal = (line.Quantity * line.UnitPrice) - line.DiscountAmount
                        })
                    Next
                End If

                ' Load receipt
                Dim receipt = Await _receiptService.GetReceiptByTransactionAsync(item.TransactionId)
                If receipt IsNot Nothing Then
                    _selectedReceiptId = receipt.Id
                    ReceiptNumber = receipt.ReceiptNumber
                    ReceiptDate = receipt.IssueDate.ToString("yyyy-MM-dd HH:mm")
                    ReceiptBusinessName = receipt.BusinessName
                    ReceiptTin = receipt.BusinessTIN
                    HasReceipt = True
                End If

                ' Load returns for this transaction
                Dim returns = Await _returnService.GetReturnsForTransactionAsync(item.TransactionId)
                For Each ret In returns
                    DetailReturns.Add(New ReturnSummaryItem() With {
                        .ReturnDate = ret.ReturnDate,
                        .ProductName = ret.ProductName,
                        .QuantityReturned = ret.QuantityReturned,
                        .RefundAmount = ret.RefundAmount,
                        .Reason = ret.Reason,
                        .IsRestocked = ret.IsRestocked
                    })
                Next

                IsDetailVisible = True
                OpenReturnDialogCommand.NotifyCanExecuteChanged()
            Catch ex As Exception
                StatusMessage = $"Error loading transaction details: {ex.Message}"
                IsStatusSuccess = False
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function ViewReceiptAsync() As Task
            If SelectedTransaction Is Nothing OrElse Not HasReceipt Then Return

            IsBusy = True
            Try
                Await _receiptService.PrintReceiptAsync(_selectedReceiptId)
            Catch ex As Exception
                StatusMessage = $"Error printing receipt: {ex.Message}"
                IsStatusSuccess = False
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function OpenReturnDialogAsync() As Task
            If SelectedTransaction Is Nothing Then Return

            IsBusy = True
            Try
                ReturnableLines.Clear()
                ReturnReason = ""
                ShouldRestock = True

                ' Load existing returns to compute per-product already-returned quantities
                Dim existingReturns = Await _returnService.GetReturnsForTransactionAsync(SelectedTransaction.TransactionId)
                Dim alreadyReturned = existingReturns.
                    GroupBy(Function(r) r.ProductId).
                    ToDictionary(Function(g) g.Key, Function(g) g.Sum(Function(r) r.QuantityReturned))

                If SelectedTransaction.Lines IsNot Nothing Then
                    For Each line In SelectedTransaction.Lines
                        Dim returned As Integer = 0
                        alreadyReturned.TryGetValue(line.ProductId, returned)
                        Dim maxReturnable = line.Quantity - returned
                        If maxReturnable > 0 Then
                            ReturnableLines.Add(New ReturnLineSelection() With {
                                .ProductId = line.ProductId,
                                .ProductName = line.ProductName,
                                .OriginalQuantity = line.Quantity,
                                .AlreadyReturned = returned
                            })
                        End If
                    Next
                End If

                If ReturnableLines.Count = 0 Then
                    StatusMessage = "All items in this transaction have already been fully returned."
                    IsStatusSuccess = False
                    Return
                End If

                SelectedReturnLine = ReturnableLines.First()
                ReturnQuantity = 1
                IsReturnDialogVisible = True
            Catch ex As Exception
                StatusMessage = $"Error preparing return: {ex.Message}"
                IsStatusSuccess = False
            Finally
                IsBusy = False
            End Try
        End Function

        Private Function CanProcessReturn() As Boolean
            If SelectedReturnLine Is Nothing Then Return False
            If String.IsNullOrWhiteSpace(ReturnReason) Then Return False
            If ReturnQuantity < 1 Then Return False
            If ReturnQuantity > SelectedReturnLine.MaxReturnable Then Return False
            Return True
        End Function

        Private Async Function ProcessReturnAsync() As Task
            If Not CanProcessReturn() Then Return

            IsBusy = True
            Try
                Await _returnService.ProcessReturnAsync(
                    SelectedTransaction.TransactionId,
                    SelectedReturnLine.ProductId,
                    ReturnQuantity,
                    ReturnReason.Trim(),
                    ShouldRestock)

                Dim productName = SelectedReturnLine.ProductName
                Dim qty = ReturnQuantity

                IsReturnDialogVisible = False
                StatusMessage = $"Return processed: {qty}× {productName}."
                IsStatusSuccess = True

                ' Mark the grid row and refresh the detail panel
                SelectedTransaction.HasReturns = True
                Await SelectTransactionAsync(SelectedTransaction)
            Catch ex As Exception
                StatusMessage = $"Return failed: {ex.Message}"
                IsStatusSuccess = False
            Finally
                IsBusy = False
            End Try
        End Function

    End Class

End Namespace
