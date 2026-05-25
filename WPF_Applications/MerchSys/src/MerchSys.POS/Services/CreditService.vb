Imports System.Threading
Imports Microsoft.Data.Sqlite
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Interfaces
Imports MerchSys.SharedKernel.Persistence

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
        Private ReadOnly _repository As ISyncableRepository(Of POSDbContext)
        Private _creditAccountList As List(Of CreditAccount)
        Private _creditPaymentList As List(Of CreditPayment)

        Public Sub New(context As POSDbContext,
                       eventBus As IEventBus,
                       repository As ISyncableRepository(Of POSDbContext))
            _context = context
            _eventBus = eventBus
            _repository = repository
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
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population
            Return account
        End Function

        Public Async Function GetAccountAsync(id As Integer) As Task(Of CreditAccount) Implements ICreditService.GetAccountAsync
            Dim account = Await _context.CreditAccounts _
                .Include(Function(a) a.Payments) _
                .FirstOrDefaultAsync(Function(a) a.Id = id AndAlso Not a.IsDeleted)
            Return account
        End Function

        Public Async Function GetAllAccountsAsync() As Task(Of List(Of CreditAccount)) Implements ICreditService.GetAllAccountsAsync
            _creditAccountList = New List(Of CreditAccount)()
            Dim gaConnStr = _context.Database.GetConnectionString()
            Using gaConn As New SqliteConnection(gaConnStr)
                Await gaConn.OpenAsync()
                Using gaCmd = gaConn.CreateCommand()
                    gaCmd.CommandText = "SELECT Id, CustomerName, Phone, Address, CurrentBalance, TotalCreditExtended, " &
                                        "TotalPaymentsReceived, IsBlocked, LastTransactionDate, Notes, " &
                                        "IsDeleted, DeletedBy, DeletedAt, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pos_CreditAccounts WHERE IsDeleted = 0 ORDER BY CustomerName"
                    Using gaReader = gaCmd.ExecuteReader()
                        While gaReader.Read()
                            _creditAccountList.Add(ReadCreditAccount(gaReader))
                        End While
                    End Using
                End Using
            End Using
            Return _creditAccountList
        End Function

        Public Async Function SearchAccountsAsync(searchTerm As String) As Task(Of List(Of CreditAccount)) Implements ICreditService.SearchAccountsAsync
            _creditAccountList = New List(Of CreditAccount)()
            Dim saConnStr = _context.Database.GetConnectionString()
            Using saConn As New SqliteConnection(saConnStr)
                Await saConn.OpenAsync()
                Using saCmd = saConn.CreateCommand()
                    saCmd.CommandText = "SELECT Id, CustomerName, Phone, Address, CurrentBalance, TotalCreditExtended, " &
                                        "TotalPaymentsReceived, IsBlocked, LastTransactionDate, Notes, " &
                                        "IsDeleted, DeletedBy, DeletedAt, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pos_CreditAccounts " &
                                        "WHERE IsDeleted = 0 AND (lower(CustomerName) LIKE @term OR lower(Phone) LIKE @term) " &
                                        "ORDER BY CustomerName"
                    saCmd.Parameters.Add(New SqliteParameter("@term", "%" & searchTerm.ToLower() & "%"))
                    Using saReader = saCmd.ExecuteReader()
                        While saReader.Read()
                            _creditAccountList.Add(ReadCreditAccount(saReader))
                        End While
                    End Using
                End Using
            End Using
            Return _creditAccountList
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

            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population
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
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population

            Await _eventBus.PublishAsync(New CreditPaymentEvent() With {
                .CustomerId = customerId,
                .PaymentAmount = amount,
                .PaymentDate = payment.PaymentDate,
                .PaymentMethod = paymentMethod
            })

            Return payment
        End Function

        Public Async Function GetPaymentHistoryAsync(customerId As Integer) As Task(Of List(Of CreditPayment)) Implements ICreditService.GetPaymentHistoryAsync
            _creditPaymentList = New List(Of CreditPayment)()
            Dim phConnStr = _context.Database.GetConnectionString()
            Using phConn As New SqliteConnection(phConnStr)
                Await phConn.OpenAsync()
                Using phCmd = phConn.CreateCommand()
                    phCmd.CommandText = "SELECT Id, CreditAccountId, PaymentAmount, PaymentDate, PaymentMethod, Notes, ReceivedBy, " &
                                        "CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pos_CreditPayments WHERE CreditAccountId = @customerId " &
                                        "ORDER BY PaymentDate DESC"
                    phCmd.Parameters.Add(New SqliteParameter("@customerId", customerId))
                    Using phReader = phCmd.ExecuteReader()
                        While phReader.Read()
                            _creditPaymentList.Add(New CreditPayment With {
                                .Id = phReader.GetInt32(0),
                                .CreditAccountId = phReader.GetInt32(1),
                                .PaymentAmount = phReader.GetDecimal(2),
                                .PaymentDate = phReader.GetDateTime(3),
                                .PaymentMethod = CType(phReader.GetInt32(4), PaymentMethod),
                                .Notes = If(phReader.IsDBNull(5), Nothing, phReader.GetString(5)),
                                .ReceivedBy = phReader.GetString(6),
                                .CreatedBy = phReader.GetString(7),
                                .CreatedAt = phReader.GetDateTime(8),
                                .ModifiedBy = If(phReader.IsDBNull(9), Nothing, phReader.GetString(9)),
                                .ModifiedAt = If(phReader.IsDBNull(10), Nothing, CType(phReader.GetDateTime(10), DateTime?))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return _creditPaymentList
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
            _creditAccountList = New List(Of CreditAccount)()
            Dim odConnStr = _context.Database.GetConnectionString()
            Using odConn As New SqliteConnection(odConnStr)
                Await odConn.OpenAsync()
                Using odCmd = odConn.CreateCommand()
                    odCmd.CommandText = "SELECT Id, CustomerName, Phone, Address, CurrentBalance, TotalCreditExtended, " &
                                        "TotalPaymentsReceived, IsBlocked, LastTransactionDate, Notes, " &
                                        "IsDeleted, DeletedBy, DeletedAt, CreatedBy, CreatedAt, ModifiedBy, ModifiedAt " &
                                        "FROM Pos_CreditAccounts " &
                                        "WHERE IsDeleted = 0 AND CurrentBalance > 0 " &
                                        "AND (LastTransactionDate IS NULL OR LastTransactionDate < @cutoff) " &
                                        "ORDER BY CurrentBalance DESC"
                    odCmd.Parameters.Add(New SqliteParameter("@cutoff", cutoff.ToString("o")))
                    Using odReader = odCmd.ExecuteReader()
                        While odReader.Read()
                            _creditAccountList.Add(ReadCreditAccount(odReader))
                        End While
                    End Using
                End Using
            End Using
            Return _creditAccountList
        End Function

        Private Shared Function ReadCreditAccount(r As Microsoft.Data.Sqlite.SqliteDataReader) As CreditAccount
            Return New CreditAccount With {
                .Id = r.GetInt32(0),
                .CustomerName = r.GetString(1),
                .Phone = r.GetString(2),
                .Address = If(r.IsDBNull(3), Nothing, r.GetString(3)),
                .CurrentBalance = r.GetDecimal(4),
                .TotalCreditExtended = r.GetDecimal(5),
                .TotalPaymentsReceived = r.GetDecimal(6),
                .IsBlocked = r.GetBoolean(7),
                .LastTransactionDate = If(r.IsDBNull(8), CType(Nothing, DateTime?), CType(r.GetDateTime(8), DateTime?)),
                .Notes = If(r.IsDBNull(9), Nothing, r.GetString(9)),
                .IsDeleted = r.GetBoolean(10),
                .DeletedBy = If(r.IsDBNull(11), Nothing, r.GetString(11)),
                .DeletedAt = If(r.IsDBNull(12), Nothing, CType(r.GetDateTime(12), DateTime?)),
                .CreatedBy = r.GetString(13),
                .CreatedAt = r.GetDateTime(14),
                .ModifiedBy = If(r.IsDBNull(15), Nothing, r.GetString(15)),
                .ModifiedAt = If(r.IsDBNull(16), Nothing, CType(r.GetDateTime(16), DateTime?))
            }
        End Function

    End Class

End Namespace
