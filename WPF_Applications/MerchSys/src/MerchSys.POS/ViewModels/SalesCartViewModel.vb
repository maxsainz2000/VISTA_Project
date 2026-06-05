Imports System.Collections.ObjectModel
Imports CommunityToolkit.Mvvm.ComponentModel
Imports CommunityToolkit.Mvvm.Input
Imports MediatR
Imports MerchSys.POS.Entities
Imports MerchSys.POS.Services
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Queries
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace ViewModels

    ''' <summary>
    ''' A single row in the cart DataGrid, wrapping the CartLineDto fields with ObservableObject
    ''' so that in-place edits to Quantity and DiscountAmount are reflected immediately in the UI.
    ''' </summary>
    Public Class CartLineItem
        Inherits ObservableObject

        Private _quantity As Integer
        Private _discountAmount As Decimal

        Public Property LineIndex As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property UnitPrice As Decimal
        Public Property AvailableStock As Integer

        Public Property Quantity As Integer
            Get
                Return _quantity
            End Get
            Set(value As Integer)
                SetProperty(_quantity, value)
                OnPropertyChanged(NameOf(LineTotal))
            End Set
        End Property

        Public Property DiscountAmount As Decimal
            Get
                Return _discountAmount
            End Get
            Set(value As Decimal)
                SetProperty(_discountAmount, value)
                OnPropertyChanged(NameOf(LineTotal))
            End Set
        End Property

        Public ReadOnly Property LineTotal As Decimal
            Get
                Return (Quantity * UnitPrice) - DiscountAmount
            End Get
        End Property

        Public ReadOnly Property IsOverStock As Boolean
            Get
                Return AvailableStock > 0 AndAlso Quantity > AvailableStock
            End Get
        End Property

    End Class

    ''' <summary>
    ''' A product result entry shown in the search panel.
    ''' Populated from <see cref="GetProductCatalogResult"/> via MediatR.
    ''' </summary>
    Public Class ProductSearchItem
        Public Property ProductId As Integer
        Public Property ProductName As String
        Public Property Sku As String
        Public Property UnitPrice As Decimal
        Public Property AvailableStock As Integer
        Public Property IsLowStock As Boolean
    End Class

    ''' <summary>
    ''' ViewModel for the Sales Cart screen — the primary POS transaction interface.
    ''' Coordinates cart building, payment method selection, credit customer validation,
    ''' payment processing, and receipt display.
    ''' </summary>
    Public Class SalesCartViewModel
        Inherits ObservableObject

        Private ReadOnly _cartService As ICartService
        Private ReadOnly _paymentService As IPaymentService
        Private ReadOnly _creditService As ICreditService
        Private ReadOnly _receiptService As IReceiptService
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _conflictPresenter As IConflictPresenter
        Private ReadOnly _notifications As INotificationService

        Private _currentCartId As Guid = Guid.Empty
        Private _heldCartId As Guid = Guid.Empty
        Private _selectedCartLine As CartLineItem

        Public Property HeldCartId As Guid
            Get
                Return _heldCartId
            End Get
            Set(value As Guid)
                SetProperty(_heldCartId, value)
                OnPropertyChanged(NameOf(HasHeldCart))
                HoldCartCommand.NotifyCanExecuteChanged()
                RecallCartCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property HasHeldCart As Boolean
            Get
                Return HeldCartId <> Guid.Empty
            End Get
        End Property

        Public Property SelectedCartLine As CartLineItem
            Get
                Return _selectedCartLine
            End Get
            Set(value As CartLineItem)
                SetProperty(_selectedCartLine, value)
            End Set
        End Property

        Public Event TransactionCompleted As EventHandler

        Public ReadOnly Property SearchAndAddProductCommand As AsyncRelayCommand
        Public ReadOnly Property ApplyDiscountCommand As AsyncRelayCommand(Of CartLineItem)
        Public ReadOnly Property HoldCartCommand As AsyncRelayCommand
        Public ReadOnly Property RecallCartCommand As AsyncRelayCommand

        Public Sub New(cartService As ICartService,
                       paymentService As IPaymentService,
                       creditService As ICreditService,
                       receiptService As IReceiptService,
                       mediator As IMediator,
                       conflictPresenter As IConflictPresenter,
                       notifications As INotificationService)

            _cartService = cartService
            _paymentService = paymentService
            _creditService = creditService
            _receiptService = receiptService
            _mediator = mediator
            _conflictPresenter = conflictPresenter
            _notifications = notifications

            CartLines = New ObservableCollection(Of CartLineItem)()
            ProductSearchResults = New ObservableCollection(Of ProductSearchItem)()
            CreditCustomers = New ObservableCollection(Of CreditAccount)()

            SearchProductCommand = New AsyncRelayCommand(AddressOf SearchProductsAsync)
            AddToCartCommand = New AsyncRelayCommand(Of ProductSearchItem)(AddressOf AddToCartAsync)
            RemoveLineCommand = New AsyncRelayCommand(Of CartLineItem)(AddressOf RemoveLineAsync)
            UpdateQuantityCommand = New AsyncRelayCommand(Of CartLineItem)(AddressOf UpdateQuantityAsync)
            SelectPaymentMethodCommand = New RelayCommand(Of String)(AddressOf SelectPaymentMethod)
            SearchCreditCustomerCommand = New AsyncRelayCommand(AddressOf SearchCreditCustomersAsync)
            SelectCreditCustomerCommand = New RelayCommand(Of CreditAccount)(AddressOf SelectCreditCustomer)
            PayCommand = New AsyncRelayCommand(AddressOf ProcessPaymentAsync, Function() CanPay)
            NewTransactionCommand = New AsyncRelayCommand(AddressOf StartNewTransactionAsync)

            SearchAndAddProductCommand = New AsyncRelayCommand(AddressOf SearchAndAddProductAsync)
            ApplyDiscountCommand = New AsyncRelayCommand(Of CartLineItem)(AddressOf ApplyDiscountAsync)
            HoldCartCommand = New AsyncRelayCommand(AddressOf HoldCartAsync, Function() CartLines.Count > 0)
            RecallCartCommand = New AsyncRelayCommand(AddressOf RecallCartAsync, Function() HasHeldCart)

            ' Fire-and-forget initialization: create the first cart and load credit customers.
            Dim initTask = InitializeAsync()
        End Sub

        ' ─── Product Search ───────────────────────────────────────────────────────

        Private _productSearchText As String = String.Empty
        Public Property ProductSearchText As String
            Get
                Return _productSearchText
            End Get
            Set(value As String)
                SetProperty(_productSearchText, value)
            End Set
        End Property

        Public Property ProductSearchResults As ObservableCollection(Of ProductSearchItem)

        Private _selectedProduct As ProductSearchItem
        Public Property SelectedProduct As ProductSearchItem
            Get
                Return _selectedProduct
            End Get
            Set(value As ProductSearchItem)
                SetProperty(_selectedProduct, value)
            End Set
        End Property

        ' ─── Cart ─────────────────────────────────────────────────────────────────

        Public Property CartLines As ObservableCollection(Of CartLineItem)

        Private _subTotal As Decimal
        Public Property SubTotal As Decimal
            Get
                Return _subTotal
            End Get
            Set(value As Decimal)
                SetProperty(_subTotal, value)
            End Set
        End Property

        Private _discountTotal As Decimal
        Public Property DiscountTotal As Decimal
            Get
                Return _discountTotal
            End Get
            Set(value As Decimal)
                SetProperty(_discountTotal, value)
            End Set
        End Property

        Private _vatAmount As Decimal
        Public Property VatAmount As Decimal
            Get
                Return _vatAmount
            End Get
            Set(value As Decimal)
                SetProperty(_vatAmount, value)
            End Set
        End Property

        Private _grandTotal As Decimal
        Public Property GrandTotal As Decimal
            Get
                Return _grandTotal
            End Get
            Set(value As Decimal)
                SetProperty(_grandTotal, value)
                OnPropertyChanged(NameOf(CanPay))
                OnPropertyChanged(NameOf(ChangeAmount))
                PayCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        ' ─── Payment Method ───────────────────────────────────────────────────────

        Private _selectedPaymentMethod As PaymentMethod = PaymentMethod.Cash
        Public Property SelectedPaymentMethod As PaymentMethod
            Get
                Return _selectedPaymentMethod
            End Get
            Set(value As PaymentMethod)
                SetProperty(_selectedPaymentMethod, value)
                OnPropertyChanged(NameOf(IsCash))
                OnPropertyChanged(NameOf(IsCredit))
                OnPropertyChanged(NameOf(IsCashSelected))
                OnPropertyChanged(NameOf(IsGCashSelected))
                OnPropertyChanged(NameOf(IsBankSelected))
                OnPropertyChanged(NameOf(IsCreditSelected))
                OnPropertyChanged(NameOf(CanPay))
                PayCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property IsCash As Boolean
            Get
                Return SelectedPaymentMethod = PaymentMethod.Cash
            End Get
        End Property

        Public ReadOnly Property IsCredit As Boolean
            Get
                Return SelectedPaymentMethod = PaymentMethod.Credit
            End Get
        End Property

        Public ReadOnly Property IsCashSelected As Boolean
            Get
                Return SelectedPaymentMethod = PaymentMethod.Cash
            End Get
        End Property

        Public ReadOnly Property IsGCashSelected As Boolean
            Get
                Return SelectedPaymentMethod = PaymentMethod.GCash
            End Get
        End Property

        Public ReadOnly Property IsBankSelected As Boolean
            Get
                Return SelectedPaymentMethod = PaymentMethod.BankTransfer
            End Get
        End Property

        Public ReadOnly Property IsCreditSelected As Boolean
            Get
                Return SelectedPaymentMethod = PaymentMethod.Credit
            End Get
        End Property

        ' ─── Cash Payment ─────────────────────────────────────────────────────────

        Private _amountTendered As Decimal
        Public Property AmountTendered As Decimal
            Get
                Return _amountTendered
            End Get
            Set(value As Decimal)
                SetProperty(_amountTendered, value)
                OnPropertyChanged(NameOf(ChangeAmount))
                OnPropertyChanged(NameOf(CanPay))
                PayCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property ChangeAmount As Decimal
            Get
                Return Math.Max(0D, AmountTendered - GrandTotal)
            End Get
        End Property

        ' ─── Credit Customer ──────────────────────────────────────────────────────

        Private _creditCustomerSearch As String = String.Empty
        Public Property CreditCustomerSearch As String
            Get
                Return _creditCustomerSearch
            End Get
            Set(value As String)
                SetProperty(_creditCustomerSearch, value)
            End Set
        End Property

        Public Property CreditCustomers As ObservableCollection(Of CreditAccount)

        Private _selectedCreditCustomer As CreditAccount
        Public Property SelectedCreditCustomer As CreditAccount
            Get
                Return _selectedCreditCustomer
            End Get
            Set(value As CreditAccount)
                SetProperty(_selectedCreditCustomer, value)
                OnPropertyChanged(NameOf(IsCustomerBlocked))
                OnPropertyChanged(NameOf(BlockedCustomerMessage))
                OnPropertyChanged(NameOf(CanPay))
                PayCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property IsCustomerBlocked As Boolean
            Get
                Return SelectedCreditCustomer IsNot Nothing AndAlso SelectedCreditCustomer.IsBlocked
            End Get
        End Property

        Public ReadOnly Property BlockedCustomerMessage As String
            Get
                If SelectedCreditCustomer IsNot Nothing AndAlso SelectedCreditCustomer.IsBlocked Then
                    Return $"⛔ Customer has outstanding balance of ₱{SelectedCreditCustomer.CurrentBalance:N2}"
                End If
                Return String.Empty
            End Get
        End Property

        ' ─── Pay Gate ─────────────────────────────────────────────────────────────

        Public ReadOnly Property CanPay As Boolean
            Get
                If IsBusy Then Return False
                If CartLines.Count = 0 Then Return False
                If GrandTotal <= 0D Then Return False
                If SelectedPaymentMethod = PaymentMethod.Cash AndAlso AmountTendered < GrandTotal Then Return False
                If SelectedPaymentMethod = PaymentMethod.Credit Then
                    If SelectedCreditCustomer Is Nothing Then Return False
                    If SelectedCreditCustomer.IsBlocked Then Return False
                End If
                Return True
            End Get
        End Property

        ' ─── Receipt ──────────────────────────────────────────────────────────────

        Private _currentReceipt As OfficialReceipt
        Public Property CurrentReceipt As OfficialReceipt
            Get
                Return _currentReceipt
            End Get
            Set(value As OfficialReceipt)
                SetProperty(_currentReceipt, value)
            End Set
        End Property

        Private _isReceiptVisible As Boolean
        Public Property IsReceiptVisible As Boolean
            Get
                Return _isReceiptVisible
            End Get
            Set(value As Boolean)
                SetProperty(_isReceiptVisible, value)
            End Set
        End Property

        ' ─── Status / Busy ────────────────────────────────────────────────────────

        Private _statusMessage As String = String.Empty
        Public Property StatusMessage As String
            Get
                Return _statusMessage
            End Get
            Set(value As String)
                SetProperty(_statusMessage, value)
            End Set
        End Property

        Private _isBusy As Boolean
        Public Property IsBusy As Boolean
            Get
                Return _isBusy
            End Get
            Set(value As Boolean)
                SetProperty(_isBusy, value)
                OnPropertyChanged(NameOf(CanPay))
                PayCommand.NotifyCanExecuteChanged()
            End Set
        End Property

        Private _isError As Boolean
        Public Property IsError As Boolean
            Get
                Return _isError
            End Get
            Set(value As Boolean)
                SetProperty(_isError, value)
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

        ' ─── Commands ─────────────────────────────────────────────────────────────

        Public ReadOnly Property SearchProductCommand As AsyncRelayCommand
        Public ReadOnly Property AddToCartCommand As AsyncRelayCommand(Of ProductSearchItem)
        Public ReadOnly Property RemoveLineCommand As AsyncRelayCommand(Of CartLineItem)
        Public ReadOnly Property UpdateQuantityCommand As AsyncRelayCommand(Of CartLineItem)
        Public ReadOnly Property SelectPaymentMethodCommand As RelayCommand(Of String)
        Public ReadOnly Property SearchCreditCustomerCommand As AsyncRelayCommand
        Public ReadOnly Property SelectCreditCustomerCommand As RelayCommand(Of CreditAccount)
        Public ReadOnly Property PayCommand As AsyncRelayCommand
        Public ReadOnly Property NewTransactionCommand As AsyncRelayCommand

        ' ─── Async Handlers ───────────────────────────────────────────────────────

        Private Async Function InitializeAsync() As Task
            Dim cart = Await _cartService.CreateCartAsync()
            _currentCartId = cart.CartId
            Await SearchCreditCustomersAsync()
        End Function

        Private Async Function SearchAndAddProductAsync() As Task
            If String.IsNullOrWhiteSpace(ProductSearchText) Then Return

            IsError = False
            IsBusy = True
            Try
                Dim query As New GetProductCatalogQuery() With {.SearchTerm = ProductSearchText}
                Dim result = Await _mediator.Send(query)

                ProductSearchResults.Clear()
                For Each item In result.Items
                    ProductSearchResults.Add(New ProductSearchItem() With {
                        .ProductId = item.ProductId,
                        .ProductName = item.ProductName,
                        .Sku = item.Sku,
                        .UnitPrice = item.UnitPrice,
                        .AvailableStock = item.AvailableStock,
                        .IsLowStock = item.IsLowStock
                    })
                Next

                If ProductSearchResults.Count = 1 Then
                    Dim matchedProduct = ProductSearchResults(0)
                    Await AddToCartAsync(matchedProduct)
                    ProductSearchText = String.Empty
                    ProductSearchResults.Clear()
                ElseIf ProductSearchResults.Count = 0 Then
                    StatusMessage = $"No product found matching '{ProductSearchText}'."
                End If
            Catch ex As Exception
                StatusMessage = $"Product search error: {ex.Message}"
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function HoldCartAsync() As Task
            If CartLines.Count = 0 Then Return

            HeldCartId = _currentCartId
            Await StartNewTransactionAsync()
            _notifications.ShowSuccess("Cart held successfully.")
        End Function

        Private Async Function RecallCartAsync() As Task
            If Not HasHeldCart Then Return

            Dim tempId = _currentCartId
            _currentCartId = HeldCartId
            HeldCartId = tempId

            Try
                Dim cart = Await _cartService.GetCartAsync(_currentCartId)
                SyncCartLines(cart)
                _notifications.ShowSuccess("Cart recalled successfully.")
            Catch ex As Exception
                StatusMessage = $"Could not recall cart: {ex.Message}"
                ' Revert swap
                Dim revertId = _currentCartId
                _currentCartId = HeldCartId
                HeldCartId = revertId
            End Try
        End Function

        Private Async Function SearchProductsAsync() As Task
            If String.IsNullOrWhiteSpace(ProductSearchText) Then
                ProductSearchResults.Clear()
                IsError = False
                Return
            End If

            IsError = False
            IsBusy = True
            Try
                Dim query As New GetProductCatalogQuery() With {.SearchTerm = ProductSearchText}
                Dim result = Await _mediator.Send(query)

                ProductSearchResults.Clear()
                For Each item In result.Items
                    ProductSearchResults.Add(New ProductSearchItem() With {
                        .ProductId = item.ProductId,
                        .ProductName = item.ProductName,
                        .Sku = item.Sku,
                        .UnitPrice = item.UnitPrice,
                        .AvailableStock = item.AvailableStock,
                        .IsLowStock = item.IsLowStock
                    })
                Next
            Catch ex As Exception
                StatusMessage = $"Product search error: {ex.Message}"
                ErrorMessage = ex.Message
                IsError = True
            Finally
                IsBusy = False
            End Try
        End Function

        Private Async Function AddToCartAsync(product As ProductSearchItem) As Task
            If product Is Nothing Then Return

            Try
                If product.AvailableStock = 0 Then
                    StatusMessage = $"'{product.ProductName}' is out of stock."
                    Return
                End If

                Dim cart = Await _cartService.AddLineAsync(
                    _currentCartId, product.ProductId, product.ProductName, 1, product.UnitPrice)
                SyncCartLines(cart, product)
                StatusMessage = String.Empty
            Catch ex As Exception
                StatusMessage = $"Could not add product: {ex.Message}"
            End Try
        End Function

        Private Async Function RemoveLineAsync(line As CartLineItem) As Task
            If line Is Nothing Then Return

            Try
                Dim cart = Await _cartService.RemoveLineAsync(_currentCartId, line.LineIndex)
                SyncCartLines(cart)
                StatusMessage = String.Empty
            Catch ex As Exception
                StatusMessage = $"Could not remove line: {ex.Message}"
            End Try
        End Function

        Private Async Function UpdateQuantityAsync(line As CartLineItem) As Task
            If line Is Nothing Then Return

            Try
                If line.Quantity <= 0 Then
                    Await RemoveLineAsync(line)
                    Return
                End If

                If line.AvailableStock > 0 AndAlso line.Quantity > line.AvailableStock Then
                    StatusMessage = $"Warning: '{line.ProductName}' qty exceeds available stock ({line.AvailableStock})."
                End If

                Dim cart = Await _cartService.UpdateLineQuantityAsync(_currentCartId, line.LineIndex, line.Quantity)
                SyncCartLines(cart)
            Catch ex As Exception
                StatusMessage = $"Could not update quantity: {ex.Message}"
            End Try
        End Function

        Private Async Function ApplyDiscountAsync(line As CartLineItem) As Task
            If line Is Nothing Then Return

            Try
                Dim cart = Await _cartService.ApplyLineDiscountAsync(_currentCartId, line.LineIndex, line.DiscountAmount)
                SyncCartLines(cart)
            Catch ex As Exception
                StatusMessage = $"Could not apply discount: {ex.Message}"
            End Try
        End Function

        Private Sub SelectPaymentMethod(methodName As String)
            Select Case methodName
                Case "Cash" : SelectedPaymentMethod = PaymentMethod.Cash
                Case "GCash" : SelectedPaymentMethod = PaymentMethod.GCash
                Case "Bank" : SelectedPaymentMethod = PaymentMethod.BankTransfer
                Case "Credit" : SelectedPaymentMethod = PaymentMethod.Credit
            End Select
        End Sub

        Private Async Function SearchCreditCustomersAsync() As Task
            Try
                Dim accounts = If(String.IsNullOrWhiteSpace(CreditCustomerSearch),
                                  Await _creditService.GetAllAccountsAsync(),
                                  Await _creditService.SearchAccountsAsync(CreditCustomerSearch))

                CreditCustomers.Clear()
                For Each a In accounts
                    CreditCustomers.Add(a)
                Next
            Catch ex As Exception
                StatusMessage = $"Customer search error: {ex.Message}"
            End Try
        End Function

        Private Sub SelectCreditCustomer(customer As CreditAccount)
            SelectedCreditCustomer = customer
        End Sub

        Private Async Function ProcessPaymentAsync() As Task
            If Not CanPay Then Return

            IsBusy = True
            StatusMessage = String.Empty
            Dim generalError As Boolean = False
            Dim generalErrorMessage As String = String.Empty

            Dim customerId As Integer? = Nothing
            If SelectedPaymentMethod = PaymentMethod.Credit AndAlso SelectedCreditCustomer IsNot Nothing Then
                customerId = SelectedCreditCustomer.Id
            End If
            Dim tenderedAmount = If(SelectedPaymentMethod = PaymentMethod.Cash, AmountTendered, GrandTotal)

            Try
                Await ConcurrencyHelper.ExecuteWithConflictPromptAsync(
                    Async Function()
                        Dim transaction = Await _cartService.FinalizeAsync(
                            _currentCartId, SelectedPaymentMethod, tenderedAmount, customerId)

                        Dim payResult = Await _paymentService.ProcessPaymentAsync(
                            transaction.Id, SelectedPaymentMethod, tenderedAmount, customerId)

                        If Not payResult.Success Then
                            StatusMessage = $"Payment failed: {payResult.ErrorMessage}"
                            Await _cartService.VoidTransactionAsync(transaction.Id, payResult.ErrorMessage)
                            Dim newCart = Await _cartService.CreateCartAsync()
                            _currentCartId = newCart.CartId
                            Return
                        End If

                        Dim receipt = Await _receiptService.GenerateReceiptAsync(transaction.Id)
                        CurrentReceipt = receipt
                        IsReceiptVisible = True
                        StatusMessage = $"Payment successful — {receipt.ReceiptNumber}"
                        _notifications.ShowSuccess($"Payment successful — {receipt.ReceiptNumber}")

                        Dim nextCart = Await _cartService.CreateCartAsync()
                        _currentCartId = nextCart.CartId
                        CartLines.Clear()
                        SubTotal = 0D
                        DiscountTotal = 0D
                        VatAmount = 0D
                        GrandTotal = 0D
                        AmountTendered = 0D
                        HoldCartCommand.NotifyCanExecuteChanged()
                        RaiseEvent TransactionCompleted(Me, EventArgs.Empty)
                    End Function,
                    AddressOf StartNewTransactionAsync,
                    _conflictPresenter)
            Catch ex As Exception
                generalError = True
                generalErrorMessage = ex.Message
            Finally
                IsBusy = False
            End Try

            If generalError Then
                StatusMessage = $"Payment error: {generalErrorMessage}"
            End If
        End Function

        Private Async Function StartNewTransactionAsync() As Task
            Dim cart = Await _cartService.CreateCartAsync()
            _currentCartId = cart.CartId
            CartLines.Clear()
            ProductSearchResults.Clear()
            ProductSearchText = String.Empty
            SubTotal = 0D
            DiscountTotal = 0D
            VatAmount = 0D
            GrandTotal = 0D
            AmountTendered = 0D
            SelectedCreditCustomer = Nothing
            CreditCustomerSearch = String.Empty
            IsReceiptVisible = False
            CurrentReceipt = Nothing
            StatusMessage = String.Empty
            SelectedPaymentMethod = PaymentMethod.Cash
            HoldCartCommand.NotifyCanExecuteChanged()
        End Function

        ' ─── Private Helpers ──────────────────────────────────────────────────────

        ''' <summary>
        ''' Rebuilds CartLines from the CartDto after any service call.
        ''' Preserves the AvailableStock from the original ProductSearchItem when adding.
        ''' </summary>
        Private Sub SyncCartLines(cart As CartDto, Optional addedProduct As ProductSearchItem = Nothing)
            ' Preserve available-stock values already set on existing lines.
            Dim stockMap As New Dictionary(Of Integer, Integer)()
            For Each existing In CartLines
                If Not stockMap.ContainsKey(existing.ProductId) Then
                    stockMap(existing.ProductId) = existing.AvailableStock
                End If
            Next
            If addedProduct IsNot Nothing Then
                stockMap(addedProduct.ProductId) = addedProduct.AvailableStock
            End If

            CartLines.Clear()
            For i = 0 To cart.Lines.Count - 1
                Dim src = cart.Lines(i)
                Dim stock As Integer = 0
                stockMap.TryGetValue(src.ProductId, stock)
                CartLines.Add(New CartLineItem() With {
                    .LineIndex = i,
                    .ProductId = src.ProductId,
                    .ProductName = src.ProductName,
                    .Quantity = src.Quantity,
                    .UnitPrice = src.UnitPrice,
                    .DiscountAmount = src.DiscountAmount,
                    .AvailableStock = stock
                })
            Next

            SubTotal = cart.SubTotal
            DiscountTotal = cart.DiscountTotal
            VatAmount = cart.VatAmount
            GrandTotal = cart.GrandTotal
            HoldCartCommand.NotifyCanExecuteChanged()
        End Sub

    End Class

End Namespace
