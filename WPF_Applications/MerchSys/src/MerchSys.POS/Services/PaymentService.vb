Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class PaymentService
        Implements IPaymentService

        Private ReadOnly _context As POSDbContext
        Private ReadOnly _eventBus As IEventBus
        Private ReadOnly _repository As ISyncableRepository(Of POSDbContext)

        Public Sub New(context As POSDbContext,
                       eventBus As IEventBus,
                       repository As ISyncableRepository(Of POSDbContext))
            _context = context
            _eventBus = eventBus
            _repository = repository
        End Sub

        Public Async Function ProcessPaymentAsync(transactionId As Integer, paymentMethod As PaymentMethod, amountTendered As Decimal, Optional customerId As Integer? = Nothing) As Task(Of PaymentResultDto) Implements IPaymentService.ProcessPaymentAsync

            Dim transaction = Await _context.SalesTransactions _
                .Include(Function(t) t.Lines) _
                .Include(Function(t) t.CreditAccount) _
                .FirstOrDefaultAsync(Function(t) t.Id = transactionId AndAlso Not t.IsDeleted)

            If transaction Is Nothing Then
                Return Failure($"Transaction {transactionId} not found.")
            End If

            If transaction.IsVoided Then
                Return Failure($"Transaction {transactionId} has been voided.")
            End If

            Dim changeAmount As Decimal = 0D

            Select Case paymentMethod
                Case PaymentMethod.Cash
                    If amountTendered < transaction.TotalAmount Then
                        Return Failure($"Insufficient cash tendered. Required: {transaction.TotalAmount:F2}, tendered: {amountTendered:F2}.")
                    End If
                    changeAmount = amountTendered - transaction.TotalAmount

                Case PaymentMethod.GCash, PaymentMethod.BankTransfer
                    If amountTendered <> transaction.TotalAmount Then
                        Return Failure($"Amount tendered must equal the total for {paymentMethod} payments. Expected: {transaction.TotalAmount:F2}.")
                    End If

                Case PaymentMethod.Credit
                    If Not customerId.HasValue Then
                        Return Failure("A customer ID is required for credit transactions.")
                    End If
                    Dim creditAccount = Await _context.CreditAccounts.FindAsync(customerId.Value)
                    If creditAccount Is Nothing Then
                        Return Failure($"Credit account for customer {customerId.Value} not found.")
                    End If
                    If creditAccount.IsBlocked Then
                        Return Failure($"Customer '{creditAccount.CustomerName}' has an outstanding balance and is blocked from new credit purchases.")
                    End If
                    creditAccount.CurrentBalance += transaction.TotalAmount
                    creditAccount.TotalCreditExtended += transaction.TotalAmount
                    creditAccount.LastTransactionDate = DateTime.UtcNow
                    creditAccount.IsBlocked = True
                    Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population

            End Select

            Dim saleEvent As New SaleCompletedEvent() With {
                .TransactionId = transaction.Id,
                .TransactionDate = transaction.TransactionDate,
                .PaymentMethod = paymentMethod,
                .TotalAmount = transaction.TotalAmount,
                .CustomerId = customerId
            }
            For Each line In transaction.Lines
                saleEvent.Items.Add(New SaleCompletedEvent.SaleItem() With {
                    .ProductId = line.ProductId,
                    .ProductName = line.ProductName,
                    .Quantity = line.Quantity,
                    .UnitPrice = line.UnitPrice,
                    .DiscountAmount = line.DiscountAmount
                })
            Next
            Await _eventBus.PublishAsync(saleEvent)

            Return New PaymentResultDto() With {
                .Success = True,
                .TransactionId = transactionId,
                .ChangeAmount = changeAmount,
                .ReceiptNumber = Nothing
            }
        End Function

        Private Shared Function Failure(message As String) As PaymentResultDto
            Return New PaymentResultDto() With {.Success = False, .ErrorMessage = message}
        End Function

    End Class

End Namespace
