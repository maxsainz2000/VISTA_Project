Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces

Namespace Services

    Public Class CreditBlockedException
        Inherits Exception

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

    End Class

    Public Class CreditService
        Implements ICreditService

        Private ReadOnly _context As POSDbContext
        Private ReadOnly _eventBus As IEventBus

        Public Sub New(context As POSDbContext, eventBus As IEventBus)
            _context = context
            _eventBus = eventBus
        End Sub

        Public Async Function CreateAccountAsync(customerName As String, phone As String, Optional address As String = Nothing) As Task(Of CreditAccount) Implements ICreditService.CreateAccountAsync
            Dim account As New CreditAccount() With {
                .CustomerName = customerName,
                .Phone = phone,
                .Address = address,
                .CurrentBalance = 0D,
                .TotalCreditExtended = 0D,
                .TotalPaymentsReceived = 0D,
                .IsBlocked = False
            }
            _context.CreditAccounts.Add(account)
            Await _context.SaveChangesAsync()
            Return account
        End Function

        Public Async Function GetAccountAsync(id As Integer) As Task(Of CreditAccount) Implements ICreditService.GetAccountAsync
            Dim account = Await _context.CreditAccounts _
                .Include(Function(a) a.Payments) _
                .FirstOrDefaultAsync(Function(a) a.Id = id AndAlso Not a.IsDeleted)
            Return account
        End Function

        Public Async Function GetAllAccountsAsync() As Task(Of List(Of CreditAccount)) Implements ICreditService.GetAllAccountsAsync
            Dim accounts = Await _context.CreditAccounts _
                .Where(Function(a) Not a.IsDeleted) _
                .OrderBy(Function(a) a.CustomerName) _
                .ToListAsync()
            Return accounts
        End Function

        Public Async Function SearchAccountsAsync(searchTerm As String) As Task(Of List(Of CreditAccount)) Implements ICreditService.SearchAccountsAsync
            Dim term = searchTerm.ToLower()
            Dim accounts = Await _context.CreditAccounts _
                .Where(Function(a) Not a.IsDeleted AndAlso
                                   (a.CustomerName.ToLower().Contains(term) OrElse
                                    a.Phone.ToLower().Contains(term))) _
                .OrderBy(Function(a) a.CustomerName) _
                .ToListAsync()
            Return accounts
        End Function

        ''' <summary>
        ''' Zero-tolerance hard block: any outstanding balance (> 0) prevents new credit.
        ''' This rule cannot be overridden.
        ''' </summary>
        Public Async Function CanExtendCreditAsync(customerId As Integer) As Task(Of Boolean) Implements ICreditService.CanExtendCreditAsync
            Dim account = Await GetAccountAsync(customerId)
            If account Is Nothing Then Return False
            Return account.CurrentBalance = 0D
        End Function

        Public Async Function ChargeCreditAsync(customerId As Integer, amount As Decimal, transactionId As Integer) As Task Implements ICreditService.ChargeCreditAsync
            Dim account = Await GetAccountAsync(customerId)
            If account Is Nothing Then
                Throw New InvalidOperationException($"Credit account {customerId} not found.")
            End If

            If Not Await CanExtendCreditAsync(customerId) Then
                Throw New CreditBlockedException($"Customer '{account.CustomerName}' has an outstanding balance of {account.CurrentBalance:F2} and is blocked from new credit purchases.")
            End If

            account.CurrentBalance += amount
            account.TotalCreditExtended += amount
            account.IsBlocked = True
            account.LastTransactionDate = DateTime.UtcNow

            Await _context.SaveChangesAsync()
        End Function

        Public Async Function RecordPaymentAsync(customerId As Integer, amount As Decimal, paymentMethod As PaymentMethod, receivedBy As String) As Task(Of CreditPayment) Implements ICreditService.RecordPaymentAsync
            If amount <= 0D Then
                Throw New ArgumentException("Payment amount must be greater than zero.")
            End If

            Dim account = Await GetAccountAsync(customerId)
            If account Is Nothing Then
                Throw New InvalidOperationException($"Credit account {customerId} not found.")
            End If

            If amount > account.CurrentBalance Then
                Throw New InvalidOperationException($"Overpayment rejected. Payment amount {amount:F2} exceeds outstanding balance {account.CurrentBalance:F2}.")
            End If

            account.CurrentBalance -= amount
            account.TotalPaymentsReceived += amount
            account.LastTransactionDate = DateTime.UtcNow

            If account.CurrentBalance = 0D Then
                account.IsBlocked = False
            End If

            Dim payment As New CreditPayment() With {
                .CreditAccountId = customerId,
                .PaymentAmount = amount,
                .PaymentDate = DateTime.UtcNow,
                .PaymentMethod = paymentMethod,
                .ReceivedBy = receivedBy
            }
            _context.CreditPayments.Add(payment)
            Await _context.SaveChangesAsync()

            Await _eventBus.PublishAsync(New CreditPaymentEvent() With {
                .CustomerId = customerId,
                .PaymentAmount = amount,
                .PaymentDate = payment.PaymentDate,
                .PaymentMethod = paymentMethod
            })

            Return payment
        End Function

        Public Async Function GetPaymentHistoryAsync(customerId As Integer) As Task(Of List(Of CreditPayment)) Implements ICreditService.GetPaymentHistoryAsync
            Dim payments = Await _context.CreditPayments _
                .Where(Function(p) p.CreditAccountId = customerId) _
                .OrderByDescending(Function(p) p.PaymentDate) _
                .ToListAsync()
            Return payments
        End Function

        Public Async Function GetTotalOutstandingAsync() As Task(Of Decimal) Implements ICreditService.GetTotalOutstandingAsync
            Dim total = Await _context.CreditAccounts _
                .Where(Function(a) Not a.IsDeleted) _
                .SumAsync(Function(a) a.CurrentBalance)
            Return total
        End Function

        ''' <summary>
        ''' Returns accounts with a non-zero balance and no payment activity in the last 30 days.
        ''' </summary>
        Public Async Function GetOverdueAccountsAsync() As Task(Of List(Of CreditAccount)) Implements ICreditService.GetOverdueAccountsAsync
            Dim cutoff = DateTime.UtcNow.AddDays(-30)
            Dim accounts = Await _context.CreditAccounts _
                .Where(Function(a) Not a.IsDeleted AndAlso
                                   a.CurrentBalance > 0D AndAlso
                                   (a.LastTransactionDate Is Nothing OrElse a.LastTransactionDate.GetValueOrDefault() < cutoff)) _
                .OrderByDescending(Function(a) a.CurrentBalance) _
                .ToListAsync()
            Return accounts
        End Function

    End Class

End Namespace
