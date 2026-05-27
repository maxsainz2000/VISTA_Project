Imports System.Text.Json.Nodes
Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Events
Imports MerchSys.SharedKernel.Persistence
Imports MerchSys.SharedKernel.Sync

Namespace Services

    ''' <summary>
    ''' Decorator over <see cref="ReceiptService"/> that stampes the BIR three-bucket VAT totals
    ''' onto the <see cref="SalesTransaction"/> and each <see cref="SalesTransactionLine"/> before
    ''' delegating to the inner service to produce the <see cref="OfficialReceipt"/>.
    ''' After the receipt is generated it computes the POS-13 hash chain and publishes
    ''' <see cref="SaleCompletedWithVatEvent"/>. The base <see cref="SaleCompletedEvent"/> is
    ''' published by <see cref="PaymentService"/> and must not be re-published here.
    ''' </summary>
    Public Class VatAwareReceiptService
        Implements IReceiptService

        Private ReadOnly _inner As ReceiptService
        Private ReadOnly _vatCalculator As IVatCalculator
        Private ReadOnly _context As POSDbContext
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _integrityService As IReceiptIntegrityService
        Private ReadOnly _repository As ISyncableRepository(Of POSDbContext)
        Private ReadOnly _journalDb As SyncJournalDbContext

        Public Sub New(inner As ReceiptService,
                       vatCalculator As IVatCalculator,
                       context As POSDbContext,
                       mediator As IMediator,
                       integrityService As IReceiptIntegrityService,
                       repository As ISyncableRepository(Of POSDbContext),
                       journalDb As SyncJournalDbContext)
            _inner = inner
            _vatCalculator = vatCalculator
            _context = context
            _mediator = mediator
            _integrityService = integrityService
            _repository = repository
            _journalDb = journalDb
        End Sub

        ''' <summary>
        ''' Stamps VAT buckets on the transaction and its lines, delegates to the inner service
        ''' to create the <see cref="OfficialReceipt"/>, then secures the hash chain and publishes
        ''' both the VAT-aware and legacy sale-completed events.
        ''' </summary>
        Public Async Function GenerateReceiptAsync(transactionId As Integer) As Task(Of OfficialReceipt) Implements IReceiptService.GenerateReceiptAsync
            ' Idempotency guard: return immediately if the receipt was already generated.
            Dim existingReceipt = Await _context.OfficialReceipts.
                FirstOrDefaultAsync(Function(r) r.TransactionId = transactionId)
            If existingReceipt IsNot Nothing Then
                Return existingReceipt
            End If

            ' Step 1 — load transaction with lines and the VAT config singleton.
            Dim transaction = Await _context.SalesTransactions.
                Include(Function(t) t.Lines).
                FirstOrDefaultAsync(Function(t) t.Id = transactionId AndAlso Not t.IsDeleted)
            If transaction Is Nothing Then
                Throw New InvalidOperationException($"Transaction {transactionId} not found.")
            End If

            Dim config = Await _context.VatConfigurations.
                FirstOrDefaultAsync(Function(v) v.Id = 1)
            If config Is Nothing Then
                Throw New InvalidOperationException("VatConfiguration row (Id=1) is missing. Run the AddVatThreeBucketColumns migration.")
            End If

            ' Step 2 — decompose each line and write the VAT fields.
            For Each line In transaction.Lines
                Dim breakdown = _vatCalculator.CalculateLine(line, config)
                line.VatableAmount = breakdown.VatableAmount
                line.VatExemptAmount = breakdown.VatExemptAmount
                line.ZeroRatedAmount = breakdown.ZeroRatedAmount
                line.OutputVat = breakdown.OutputVat

                ' Honour the override rule: when not registered, every line is effectively Exempt.
                If Not config.IsVatRegistered Then
                    line.Treatment = SharedKernel.Enums.VatTreatment.Exempt
                End If
            Next

            ' Step 3 — aggregate to transaction level and capture config snapshots.
            Dim totals = _vatCalculator.AggregateTransaction(transaction, config)
            transaction.VatableSales = totals.VatableSales
            transaction.VatExemptSales = totals.VatExemptSales
            transaction.ZeroRatedSales = totals.ZeroRatedSales
            transaction.VatRateSnapshot = config.VatRate
            transaction.IsVatRegisteredSnapshot = config.IsVatRegistered

            ' Step 4 — persist VAT fields before the receipt is issued.
            Await _repository.SaveChangesWithJournalAsync(CancellationToken.None) ' INFRA-13: Migrated from _context.SaveChangesAsync() for sync journal population

            ' Step 5 — delegate to the inner ReceiptService to produce the OfficialReceipt.
            Dim receipt = Await _inner.GenerateReceiptAsync(transactionId)

            ' Step 6 — secure the hash chain (now includes VAT totals in the canonical payload).
            Dim integrity = Await _integrityService.ComputeAndPersistAsync(receipt)

            ' Patch the Sync_Journal payload for this receipt to include IntegrityHash.
            ' The payload was captured in Step 5 before the hash existed, so it must be
            ' back-filled here or IntegrityHash will always be NULL in MariaDB.
            If integrity IsNot Nothing AndAlso Not String.IsNullOrEmpty(integrity.IntegrityHash) Then
                Dim journalEntry = Await _journalDb.SyncJournalEntries.
                    FirstOrDefaultAsync(Function(j) j.TableName = "Pos_OfficialReceipts" AndAlso
                                                    j.RowId = CLng(receipt.Id) AndAlso
                                                    j.SyncedAt Is Nothing)
                If journalEntry IsNot Nothing Then
                    Dim node = JsonNode.Parse(journalEntry.Payload)
                    node("IntegrityHash") = JsonValue.Create(integrity.IntegrityHash)
                    journalEntry.Payload = node.ToJsonString()
                    Await _journalDb.SaveChangesAsync()
                    _journalDb.ChangeTracker.Clear()
                End If
            End If

            ' Step 7 — publish both events so legacy and VAT-aware consumers each receive their contract.
            ' Handlers must be idempotent on TransactionId during the migration window.
            Dim vatItems = transaction.Lines.Select(Function(l)
                                                        Return New SaleCompletedWithVatEvent.SaleItemWithVat With {
                                                            .ProductId = l.ProductId,
                                                            .ProductName = l.ProductName,
                                                            .Quantity = l.Quantity,
                                                            .UnitPrice = l.UnitPrice,
                                                            .DiscountAmount = l.DiscountAmount,
                                                            .Treatment = l.Treatment,
                                                            .VatableAmount = l.VatableAmount,
                                                            .VatExemptAmount = l.VatExemptAmount,
                                                            .ZeroRatedAmount = l.ZeroRatedAmount,
                                                            .OutputVat = l.OutputVat
                                                        }
                                                    End Function).ToList()

            Await _mediator.Publish(New SaleCompletedWithVatEvent With {
                .TransactionId = transaction.Id,
                .TransactionDate = transaction.TransactionDate,
                .PaymentMethod = transaction.PaymentMethod,
                .TotalAmount = transaction.TotalAmount,
                .CustomerId = transaction.CustomerId,
                .VatableSales = totals.VatableSales,
                .VatExemptSales = totals.VatExemptSales,
                .ZeroRatedSales = totals.ZeroRatedSales,
                .OutputVat = totals.OutputVat,
                .IsVatRegistered = config.IsVatRegistered,
                .Items = vatItems
            })

            Return receipt
        End Function

        Public Function GetReceiptAsync(receiptNumber As String) As Task(Of OfficialReceipt) Implements IReceiptService.GetReceiptAsync
            Return _inner.GetReceiptAsync(receiptNumber)
        End Function

        Public Function GetReceiptByTransactionAsync(transactionId As Integer) As Task(Of OfficialReceipt) Implements IReceiptService.GetReceiptByTransactionAsync
            Return _inner.GetReceiptByTransactionAsync(transactionId)
        End Function

        Public Function PrintReceiptAsync(receiptId As Integer) As Task Implements IReceiptService.PrintReceiptAsync
            Return _inner.PrintReceiptAsync(receiptId)
        End Function

    End Class

End Namespace
