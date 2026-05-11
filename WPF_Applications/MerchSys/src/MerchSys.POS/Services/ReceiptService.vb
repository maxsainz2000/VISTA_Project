Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Class ReceiptService
        Implements IReceiptService

        Private ReadOnly _context As POSDbContext
        Private ReadOnly _receiptIntegrity As IReceiptIntegrityService
        Private ReadOnly _bodyComposer As IReceiptBodyComposer
        Private ReadOnly _businessName As String
        Private ReadOnly _businessAddress As String
        Private ReadOnly _businessTIN As String
        Private ReadOnly _isVatRegistered As Boolean

        Public Sub New(context As POSDbContext,
                       configuration As IConfiguration,
                       receiptIntegrity As IReceiptIntegrityService,
                       bodyComposer As IReceiptBodyComposer)
            _context = context
            _receiptIntegrity = receiptIntegrity
            _bodyComposer = bodyComposer
            _businessName = If(configuration("POS:BusinessName"), "Villon Farm Supply")
            _businessAddress = If(configuration("POS:BusinessAddress"), "")
            _businessTIN = If(configuration("POS:BusinessTIN"), "")
            _isVatRegistered = String.Equals(configuration("POS:IsVatRegistered"), "true", StringComparison.OrdinalIgnoreCase)
        End Sub

        ''' <summary>
        ''' Generates a BIR-compliant Official Receipt for <paramref name="transactionId"/>.
        ''' Receipt numbering is delegated to <see cref="IReceiptIntegrityService.GetNextReceiptNumberAsync"/>
        ''' (POS-15) which uses a row-locked serializable-isolation sequence to guarantee gap-free,
        ''' monotonic, concurrency-safe receipt numbers.
        ''' Receipt body composition (including the BIR VAT disclosure block) is handled by
        ''' <see cref="IReceiptBodyComposer"/> when the receipt is printed via
        ''' <see cref="PrintReceiptAsync"/> (POS-18).
        ''' </summary>
        Public Async Function GenerateReceiptAsync(transactionId As Integer) As Task(Of OfficialReceipt) Implements IReceiptService.GenerateReceiptAsync
            Dim existing = Await _context.OfficialReceipts.
                FirstOrDefaultAsync(Function(r) r.TransactionId = transactionId)
            If existing IsNot Nothing Then
                Return existing
            End If

            Dim transaction = Await _context.SalesTransactions.
                Include(Function(t) t.Lines).
                FirstOrDefaultAsync(Function(t) t.Id = transactionId AndAlso Not t.IsDeleted)
            If transaction Is Nothing Then
                Throw New InvalidOperationException($"Transaction {transactionId} not found.")
            End If

            Dim receiptNumber = Await _receiptIntegrity.GetNextReceiptNumberAsync(DateTime.Now.Year)
            Dim now = DateTime.UtcNow

            Dim vatableAmount As Decimal = 0D
            Dim vatAmount As Decimal = 0D
            If _isVatRegistered Then
                vatableAmount = Math.Round(transaction.TotalAmount / 1.12D, 2)
                vatAmount = transaction.TotalAmount - vatableAmount
            End If

            Dim receipt As New OfficialReceipt() With {
                .TransactionId = transactionId,
                .ReceiptNumber = receiptNumber,
                .BusinessName = _businessName,
                .BusinessAddress = _businessAddress,
                .BusinessTIN = _businessTIN,
                .IssueDate = now,
                .TotalAmount = transaction.TotalAmount,
                .VatAmount = vatAmount,
                .IsVatRegistered = _isVatRegistered
            }

            _context.OfficialReceipts.Add(receipt)
            Await _context.SaveChangesAsync()

            Return receipt
        End Function

        Public Async Function GetReceiptAsync(receiptNumber As String) As Task(Of OfficialReceipt) Implements IReceiptService.GetReceiptAsync
            Return Await _context.OfficialReceipts.
                Include(Function(r) r.Transaction).
                FirstOrDefaultAsync(Function(r) r.ReceiptNumber = receiptNumber)
        End Function

        Public Async Function GetReceiptByTransactionAsync(transactionId As Integer) As Task(Of OfficialReceipt) Implements IReceiptService.GetReceiptByTransactionAsync
            Return Await _context.OfficialReceipts.
                Include(Function(r) r.Transaction).
                FirstOrDefaultAsync(Function(r) r.TransactionId = transactionId)
        End Function

        ''' <summary>
        ''' Prints a receipt by composing the BIR-compliant body via <see cref="IReceiptBodyComposer"/>
        ''' and writing it to the output channel. The VAT disclosure block (POS-18) is included
        ''' in the composed body for all VAT-registered receipts.
        ''' </summary>
        Public Async Function PrintReceiptAsync(receiptId As Integer) As Task Implements IReceiptService.PrintReceiptAsync
            Dim receipt = Await _context.OfficialReceipts.
                Include(Function(r) r.Transaction).
                    ThenInclude(Function(t) t.Lines).
                FirstOrDefaultAsync(Function(r) r.Id = receiptId)
            If receipt Is Nothing Then
                Throw New InvalidOperationException($"Receipt {receiptId} not found.")
            End If

            Dim vatConfig = Await _context.VatConfigurations.FirstOrDefaultAsync(Function(v) v.Id = 1)
            If vatConfig Is Nothing Then
                Throw New InvalidOperationException(
                    "VatConfiguration row (Id=1) is missing. Run the AddVatThreeBucketColumns migration.")
            End If

            Dim lineItems As IReadOnlyList(Of SalesTransactionLine) =
                If(receipt.Transaction?.Lines IsNot Nothing,
                   receipt.Transaction.Lines.ToList().AsReadOnly(),
                   New List(Of SalesTransactionLine)().AsReadOnly())

            Dim body = Await _bodyComposer.ComposeAsync(receipt, lineItems, vatConfig)
            Dim rendered = String.Join(Environment.NewLine, body.AllLines)
            Console.WriteLine(rendered)
        End Function

    End Class

End Namespace
