Imports System.Threading
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.Purchasing.Data
Imports MerchSys.Purchasing.Entities
Imports MerchSys.SharedKernel.Persistence

Namespace Services

    Public Class AccountsPayableService
        Implements IAccountsPayableService

        Private ReadOnly _db As PurchasingDbContext
        Private ReadOnly _repository As ISyncableRepository(Of PurchasingDbContext)

        Public Sub New(db As PurchasingDbContext,
                       repository As ISyncableRepository(Of PurchasingDbContext))
            _db = db
            _repository = repository
        End Sub

        Public Async Function CreateFromPurchaseOrderAsync(purchaseOrderId As Integer, invoiceNumber As String, invoiceDate As DateTime, dueDate As DateTime) As Task(Of AccountsPayableEntry) Implements IAccountsPayableService.CreateFromPurchaseOrderAsync
            Dim po As PurchaseOrder = Await _db.PurchaseOrders.
                FirstOrDefaultAsync(Function(p) p.Id = purchaseOrderId)

            If po Is Nothing Then
                Throw New InvalidOperationException($"Purchase order {purchaseOrderId} not found.")
            End If

            Dim alreadyExists As Boolean = Await _db.AccountsPayableEntries.
                AnyAsync(Function(ap) ap.PurchaseOrderId = purchaseOrderId)

            If alreadyExists Then
                Throw New InvalidOperationException($"An accounts payable entry already exists for purchase order {purchaseOrderId}.")
            End If

            Dim receipts As List(Of GoodsReceipt) = Await _db.GoodsReceipts.
                Include(Function(gr) gr.Lines).
                Where(Function(gr) gr.PurchaseOrderId = purchaseOrderId).
                ToListAsync()

            Dim totalAmount As Decimal = receipts.
                SelectMany(Function(gr) gr.Lines).
                Sum(Function(grl) CDec(grl.QuantityReceived) * grl.UnitCost)

            Dim entry As New AccountsPayableEntry With {
                .PurchaseOrderId = purchaseOrderId,
                .VendorId = po.VendorId,
                .InvoiceNumber = invoiceNumber,
                .InvoiceDate = invoiceDate,
                .DueDate = dueDate,
                .TotalAmount = totalAmount,
                .AmountPaid = 0D,
                .Balance = totalAmount,
                .IsPaid = (totalAmount = 0D)
            }

            _db.AccountsPayableEntries.Add(entry)
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return Await GetByIdWithNavigationAsync(entry.Id)
        End Function

        Public Async Function RecordPaymentAsync(apEntryId As Integer, amount As Decimal) As Task(Of AccountsPayableEntry) Implements IAccountsPayableService.RecordPaymentAsync
            If amount <= 0D Then
                Throw New InvalidOperationException("Payment amount must be greater than zero.")
            End If

            Dim entry As AccountsPayableEntry = Await _db.AccountsPayableEntries.
                FirstOrDefaultAsync(Function(ap) ap.Id = apEntryId)

            If entry Is Nothing Then
                Throw New InvalidOperationException($"Accounts payable entry {apEntryId} not found.")
            End If

            If entry.IsPaid Then
                Throw New InvalidOperationException($"Accounts payable entry {apEntryId} is already fully paid.")
            End If

            If amount > entry.Balance Then
                Throw New InvalidOperationException(
                    $"Payment amount {amount:F2} exceeds outstanding balance {entry.Balance:F2}.")
            End If

            entry.AmountPaid += amount
            entry.Balance = entry.TotalAmount - entry.AmountPaid
            entry.IsPaid = (entry.Balance = 0D)

            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _db.SaveChangesAsync() for sync journal population

            Return Await GetByIdWithNavigationAsync(apEntryId)
        End Function

        Public Async Function GetAllOutstandingAsync() As Task(Of List(Of AccountsPayableEntry)) Implements IAccountsPayableService.GetAllOutstandingAsync
            Return Await _db.AccountsPayableEntries.
                Include(Function(ap) ap.Vendor).
                Include(Function(ap) ap.PurchaseOrder).
                Where(Function(ap) Not ap.IsPaid).
                OrderBy(Function(ap) ap.DueDate).
                ToListAsync()
        End Function

        Public Async Function GetByVendorAsync(vendorId As Integer) As Task(Of List(Of AccountsPayableEntry)) Implements IAccountsPayableService.GetByVendorAsync
            Return Await _db.AccountsPayableEntries.
                Include(Function(ap) ap.Vendor).
                Include(Function(ap) ap.PurchaseOrder).
                Where(Function(ap) ap.VendorId = vendorId).
                OrderByDescending(Function(ap) ap.InvoiceDate).
                ToListAsync()
        End Function

        Public Async Function GetOverdueAsync() As Task(Of List(Of AccountsPayableEntry)) Implements IAccountsPayableService.GetOverdueAsync
            Dim today As DateTime = DateTime.UtcNow.Date
            Return Await _db.AccountsPayableEntries.
                Include(Function(ap) ap.Vendor).
                Include(Function(ap) ap.PurchaseOrder).
                Where(Function(ap) ap.DueDate < today AndAlso Not ap.IsPaid).
                OrderBy(Function(ap) ap.DueDate).
                ToListAsync()
        End Function

        Public Async Function GetTotalOutstandingAsync() As Task(Of Decimal) Implements IAccountsPayableService.GetTotalOutstandingAsync
            Dim hasOutstanding As Boolean = Await _db.AccountsPayableEntries.
                AnyAsync(Function(ap) Not ap.IsPaid)

            If Not hasOutstanding Then
                Return 0D
            End If

            Return Await _db.AccountsPayableEntries.
                Where(Function(ap) Not ap.IsPaid).
                SumAsync(Function(ap) ap.Balance)
        End Function

        Public Async Function GetAllAsync() As Task(Of List(Of AccountsPayableEntry)) Implements IAccountsPayableService.GetAllAsync
            Return Await _db.AccountsPayableEntries.
                Include(Function(apEnt) apEnt.Vendor).
                Include(Function(apEnt) apEnt.PurchaseOrder).
                OrderByDescending(Function(apEnt) apEnt.InvoiceDate).
                ToListAsync()
        End Function

        Private Async Function GetByIdWithNavigationAsync(id As Integer) As Task(Of AccountsPayableEntry)
            Return Await _db.AccountsPayableEntries.
                Include(Function(ap) ap.Vendor).
                Include(Function(ap) ap.PurchaseOrder).
                FirstOrDefaultAsync(Function(ap) ap.Id = id)
        End Function

    End Class

End Namespace
