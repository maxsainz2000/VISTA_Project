Imports MerchSys.Purchasing.Entities

Namespace Services

    Public Interface IAccountsPayableService

        ''' <summary>
        ''' Creates an AP entry for a purchase order using the actual costs from all goods receipt lines.
        ''' TotalAmount is derived from the sum of QuantityReceived × UnitCost across all receipts for the PO.
        ''' </summary>
        Function CreateFromPurchaseOrderAsync(purchaseOrderId As Integer, invoiceNumber As String, invoiceDate As DateTime, dueDate As DateTime) As Task(Of AccountsPayableEntry)

        ''' <summary>
        ''' Records a (partial or full) payment against an AP entry.
        ''' Accumulates AmountPaid, recalculates Balance, and marks IsPaid when Balance reaches zero.
        ''' Throws <see cref="InvalidOperationException"/> if amount exceeds the outstanding balance.
        ''' </summary>
        Function RecordPaymentAsync(apEntryId As Integer, amount As Decimal) As Task(Of AccountsPayableEntry)

        ''' <summary>Returns all AP entries where IsPaid is False, ordered by DueDate ascending.</summary>
        Function GetAllOutstandingAsync() As Task(Of List(Of AccountsPayableEntry))

        ''' <summary>Returns all AP entries for the specified vendor, ordered by InvoiceDate descending.</summary>
        Function GetByVendorAsync(vendorId As Integer) As Task(Of List(Of AccountsPayableEntry))

        ''' <summary>Returns all unpaid AP entries whose DueDate is earlier than today's UTC date.</summary>
        Function GetOverdueAsync() As Task(Of List(Of AccountsPayableEntry))

        ''' <summary>Returns the sum of Balance across all outstanding (unpaid) AP entries.</summary>
        Function GetTotalOutstandingAsync() As Task(Of Decimal)

        ''' <summary>Returns all AP entries ordered by InvoiceDate descending.</summary>
        Function GetAllAsync() As Task(Of List(Of AccountsPayableEntry))

    End Interface

End Namespace
