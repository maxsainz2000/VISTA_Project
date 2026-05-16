Imports System.Collections.Concurrent
Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class CartService
        Implements ICartService

        ''' <summary>
        ''' In-memory cart store shared across DI scope lifetimes for the lifetime of the process.
        ''' Carts are created here and removed when finalized or abandoned.
        ''' </summary>
        Private Shared ReadOnly _carts As New ConcurrentDictionary(Of Guid, CartDto)()

        Private ReadOnly _context As POSDbContext
        Private ReadOnly _receiptService As IReceiptService
        Private ReadOnly _isVatRegistered As Boolean
        Private ReadOnly _repository As ISyncableRepository(Of POSDbContext)

        Public Sub New(context As POSDbContext,
                       receiptService As IReceiptService,
                       configuration As IConfiguration,
                       repository As ISyncableRepository(Of POSDbContext))
            _context = context
            _receiptService = receiptService
            _isVatRegistered = String.Equals(configuration("POS:IsVatRegistered"), "true", StringComparison.OrdinalIgnoreCase)
            _repository = repository
        End Sub

        Public Function CreateCartAsync() As Task(Of CartDto) Implements ICartService.CreateCartAsync
            Dim cart As New CartDto() With {.CartId = Guid.NewGuid()}
            RecalculateTotals(cart)
            _carts(cart.CartId) = cart
            Return Task.FromResult(cart)
        End Function

        Public Function AddLineAsync(cartId As Guid, productId As Integer, productName As String, quantity As Integer, unitPrice As Decimal) As Task(Of CartDto) Implements ICartService.AddLineAsync
            Dim cart = GetCart(cartId)
            cart.Lines.Add(New CartLineDto() With {
                .ProductId = productId,
                .ProductName = productName,
                .Quantity = quantity,
                .UnitPrice = unitPrice,
                .DiscountAmount = 0D
            })
            RecalculateTotals(cart)
            Return Task.FromResult(cart)
        End Function

        Public Function UpdateLineQuantityAsync(cartId As Guid, lineIndex As Integer, newQuantity As Integer) As Task(Of CartDto) Implements ICartService.UpdateLineQuantityAsync
            Dim cart = GetCart(cartId)
            ValidateLineIndex(cart, lineIndex)
            cart.Lines(lineIndex).Quantity = newQuantity
            RecalculateTotals(cart)
            Return Task.FromResult(cart)
        End Function

        Public Function RemoveLineAsync(cartId As Guid, lineIndex As Integer) As Task(Of CartDto) Implements ICartService.RemoveLineAsync
            Dim cart = GetCart(cartId)
            ValidateLineIndex(cart, lineIndex)
            cart.Lines.RemoveAt(lineIndex)
            RecalculateTotals(cart)
            Return Task.FromResult(cart)
        End Function

        Public Function ApplyLineDiscountAsync(cartId As Guid, lineIndex As Integer, discountAmount As Decimal) As Task(Of CartDto) Implements ICartService.ApplyLineDiscountAsync
            Dim cart = GetCart(cartId)
            ValidateLineIndex(cart, lineIndex)
            cart.Lines(lineIndex).DiscountAmount = discountAmount
            RecalculateTotals(cart)
            Return Task.FromResult(cart)
        End Function

        Public Async Function FinalizeAsync(cartId As Guid, paymentMethod As PaymentMethod, amountTendered As Decimal, Optional customerId As Integer? = Nothing) As Task(Of SalesTransaction) Implements ICartService.FinalizeAsync
            Dim cart = GetCart(cartId)

            If cart.Lines.Count = 0 Then
                Throw New InvalidOperationException("Cannot finalize an empty cart.")
            End If

            If paymentMethod = PaymentMethod.Cash AndAlso amountTendered < cart.GrandTotal Then
                Throw New InvalidOperationException($"Insufficient cash tendered. Required: {cart.GrandTotal:F2}, tendered: {amountTendered:F2}.")
            End If

            Dim creditAccount As CreditAccount = Nothing
            If paymentMethod = PaymentMethod.Credit Then
                creditAccount = Await ValidateCreditCustomerAsync(customerId)
            End If

            Dim txNumber = Await GenerateTransactionNumberAsync()
            Dim now = DateTime.UtcNow
            Dim changeAmount = If(paymentMethod = PaymentMethod.Cash, amountTendered - cart.GrandTotal, 0D)

            Dim transaction As New SalesTransaction() With {
                .TransactionNumber = txNumber,
                .TransactionDate = now,
                .CustomerId = customerId,
                .CustomerName = If(creditAccount IsNot Nothing, creditAccount.CustomerName, Nothing),
                .PaymentMethod = paymentMethod,
                .SubTotal = cart.SubTotal,
                .DiscountAmount = cart.DiscountTotal,
                .VatAmount = cart.VatAmount,
                .TotalAmount = cart.GrandTotal,
                .AmountTendered = amountTendered,
                .ChangeAmount = changeAmount,
                .IsVoided = False,
                .CreditAccount = creditAccount
            }

            For Each cartLine In cart.Lines
                transaction.Lines.Add(New SalesTransactionLine() With {
                    .ProductId = cartLine.ProductId,
                    .ProductName = cartLine.ProductName,
                    .Quantity = cartLine.Quantity,
                    .UnitPrice = cartLine.UnitPrice,
                    .DiscountAmount = cartLine.DiscountAmount,
                    .LineTotal = cartLine.LineTotal
                })
            Next

            _context.SalesTransactions.Add(transaction)
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population

            Await _receiptService.GenerateReceiptAsync(transaction.Id)

            Dim removed As CartDto = Nothing
            _carts.TryRemove(cartId, removed)

            Return transaction
        End Function

        Public Async Function VoidTransactionAsync(transactionId As Integer, reason As String) As Task Implements ICartService.VoidTransactionAsync
            Dim transaction = Await _context.SalesTransactions.FindAsync(transactionId)
            If transaction Is Nothing Then
                Throw New InvalidOperationException($"Transaction {transactionId} not found.")
            End If
            If transaction.IsVoided Then
                Throw New InvalidOperationException($"Transaction {transactionId} is already voided.")
            End If
            transaction.IsVoided = True
            transaction.VoidReason = reason
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population
        End Function

        Public Async Function GetTransactionHistoryAsync(Optional startDate As DateTime? = Nothing, Optional endDate As DateTime? = Nothing) As Task(Of List(Of SalesTransaction)) Implements ICartService.GetTransactionHistoryAsync
            Dim query As IQueryable(Of SalesTransaction) = _context.SalesTransactions.
                Include(Function(t) t.Lines).
                Where(Function(t) Not t.IsDeleted)

            If startDate.HasValue Then
                query = query.Where(Function(t) t.TransactionDate >= startDate.Value)
            End If
            If endDate.HasValue Then
                query = query.Where(Function(t) t.TransactionDate <= endDate.Value)
            End If

            Return Await query.OrderByDescending(Function(t) t.TransactionDate).ToListAsync()
        End Function

        ' --- Private Helpers ---

        Private Function GetCart(cartId As Guid) As CartDto
            Dim cart As CartDto = Nothing
            If Not _carts.TryGetValue(cartId, cart) Then
                Throw New InvalidOperationException($"Cart {cartId} not found.")
            End If
            Return cart
        End Function

        Private Shared Sub ValidateLineIndex(cart As CartDto, lineIndex As Integer)
            If lineIndex < 0 OrElse lineIndex >= cart.Lines.Count Then
                Throw New ArgumentOutOfRangeException(NameOf(lineIndex), "Line index is out of range.")
            End If
        End Sub

        Private Sub RecalculateTotals(cart As CartDto)
            For Each cartLine In cart.Lines
                cartLine.LineTotal = (cartLine.Quantity * cartLine.UnitPrice) - cartLine.DiscountAmount
            Next
            cart.SubTotal = cart.Lines.Sum(Function(l) l.Quantity * l.UnitPrice)
            cart.DiscountTotal = cart.Lines.Sum(Function(l) l.DiscountAmount)
            cart.VatAmount = If(_isVatRegistered, Math.Round(cart.SubTotal * 0.12D, 2), 0D)
            cart.GrandTotal = cart.SubTotal - cart.DiscountTotal + cart.VatAmount
        End Sub

        Private Async Function ValidateCreditCustomerAsync(customerId As Integer?) As Task(Of CreditAccount)
            If Not customerId.HasValue Then
                Throw New InvalidOperationException("A customer ID is required for credit transactions.")
            End If
            Dim account = Await _context.CreditAccounts.FindAsync(customerId.Value)
            If account Is Nothing Then
                Throw New InvalidOperationException($"Credit account {customerId.Value} not found.")
            End If
            If account.IsBlocked Then
                Throw New InvalidOperationException($"Customer '{account.CustomerName}' has an outstanding balance and is blocked from new credit purchases.")
            End If
            Return account
        End Function

        Private Async Function GenerateTransactionNumberAsync() As Task(Of String)
            Dim year = DateTime.Now.Year
            Dim count = Await _context.SalesTransactions.
                CountAsync(Function(t) t.TransactionDate.Year = year) + 1
            Return $"TX-{year}-{count:D4}"
        End Function

    End Class

End Namespace
