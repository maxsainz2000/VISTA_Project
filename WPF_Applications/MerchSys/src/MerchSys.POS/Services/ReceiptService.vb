Imports System.Text
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Configuration
Imports MerchSys.POS.Data
Imports MerchSys.POS.Entities
Imports MerchSys.SharedKernel.Enums

Namespace Services

    Public Class ReceiptService
        Implements IReceiptService

        Private Const LineWidth As Integer = 40
        Private Const Separator As String = "----------------------------------------"

        Private ReadOnly _context As POSDbContext
        Private ReadOnly _receiptIntegrity As IReceiptIntegrityService
        Private ReadOnly _businessName As String
        Private ReadOnly _businessAddress As String
        Private ReadOnly _businessTIN As String
        Private ReadOnly _isVatRegistered As Boolean

        Public Sub New(context As POSDbContext, configuration As IConfiguration, receiptIntegrity As IReceiptIntegrityService)
            _context = context
            _receiptIntegrity = receiptIntegrity
            _businessName = If(configuration("POS:BusinessName"), "Villon Farm Supply")
            _businessAddress = If(configuration("POS:BusinessAddress"), "")
            _businessTIN = If(configuration("POS:BusinessTIN"), "")
            _isVatRegistered = String.Equals(configuration("POS:IsVatRegistered"), "true", StringComparison.OrdinalIgnoreCase)
        End Sub

        ''' <summary>
        ''' Generates a BIR-compliant Official Receipt for <paramref name="transactionId"/>.
        ''' Receipt numbering is delegated to <see cref="IReceiptIntegrityService.GetNextReceiptNumberAsync"/>
        ''' (POS-13) which uses a row-locked serializable-isolation sequence to guarantee gap-free,
        ''' monotonic, concurrency-safe receipt numbers.
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

            Dim itemsSnapshot = BuildItemsSnapshot(transaction)

            Dim receipt As New OfficialReceipt() With {
                .TransactionId = transactionId,
                .ReceiptNumber = receiptNumber,
                .BusinessName = _businessName,
                .BusinessAddress = _businessAddress,
                .BusinessTIN = _businessTIN,
                .IssueDate = now,
                .Items = itemsSnapshot,
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

        Public Async Function PrintReceiptAsync(receiptId As Integer) As Task Implements IReceiptService.PrintReceiptAsync
            Dim receipt = Await _context.OfficialReceipts.
                Include(Function(r) r.Transaction).
                    ThenInclude(Function(t) t.Lines).
                FirstOrDefaultAsync(Function(r) r.Id = receiptId)
            If receipt Is Nothing Then
                Throw New InvalidOperationException($"Receipt {receiptId} not found.")
            End If

            Dim formatted = FormatReceipt(receipt)
            Console.WriteLine(formatted)
        End Function

        ' --- Private Helpers ---

        Private Shared Function BuildItemsSnapshot(transaction As SalesTransaction) As String
            Dim sb As New StringBuilder()
            For Each line In transaction.Lines
                sb.AppendLine($"{line.ProductName}|{line.Quantity}|{line.UnitPrice:F2}|{line.LineTotal:F2}")
            Next
            Return sb.ToString().TrimEnd()
        End Function

        Private Function FormatReceipt(receipt As OfficialReceipt) As String
            Dim sb As New StringBuilder()

            sb.AppendLine(CenterText(receipt.BusinessName))
            If Not String.IsNullOrWhiteSpace(receipt.BusinessAddress) Then
                sb.AppendLine(CenterText(receipt.BusinessAddress))
            End If
            If Not String.IsNullOrWhiteSpace(receipt.BusinessTIN) Then
                sb.AppendLine(CenterText($"TIN: {receipt.BusinessTIN}"))
            End If
            sb.AppendLine(CenterText("OFFICIAL RECEIPT"))
            sb.AppendLine(Separator)

            sb.AppendLine(FormatLabelValue("Receipt No:", receipt.ReceiptNumber))
            sb.AppendLine(FormatLabelValue("Date:", receipt.IssueDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm")))

            If receipt.Transaction IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(receipt.Transaction.CustomerName) Then
                sb.AppendLine(FormatLabelValue("Customer:", receipt.Transaction.CustomerName))
            End If

            sb.AppendLine(Separator)
            sb.AppendLine(FormatItemHeader())
            sb.AppendLine(Separator)

            If receipt.Transaction?.Lines IsNot Nothing Then
                For Each line In receipt.Transaction.Lines
                    sb.AppendLine(FormatItemLine(line.ProductName, line.Quantity, line.UnitPrice, line.LineTotal))
                    If line.DiscountAmount > 0D Then
                        sb.AppendLine(FormatLabelValue("  Discount:", $"-{line.DiscountAmount:F2}"))
                    End If
                Next
            Else
                For Each rawLine In ParseItemsSnapshot(receipt.Items)
                    sb.AppendLine(rawLine)
                Next
            End If

            sb.AppendLine(Separator)
            If receipt.Transaction IsNot Nothing Then
                sb.AppendLine(FormatLabelValue("Subtotal:", receipt.Transaction.SubTotal.ToString("F2")))
                If receipt.Transaction.DiscountAmount > 0D Then
                    sb.AppendLine(FormatLabelValue("Discount:", $"-{receipt.Transaction.DiscountAmount:F2}"))
                End If
            End If

            If receipt.IsVatRegistered Then
                Dim vatableAmount = Math.Round(receipt.TotalAmount / 1.12D, 2)
                sb.AppendLine(FormatLabelValue("VAT (12%):", receipt.VatAmount.ToString("F2")))
                sb.AppendLine(FormatLabelValue("Vatable Amt:", vatableAmount.ToString("F2")))
            End If

            sb.AppendLine(FormatLabelValue("TOTAL:", receipt.TotalAmount.ToString("F2")))

            If receipt.Transaction IsNot Nothing Then
                Dim paymentLabel = GetPaymentLabel(receipt.Transaction.PaymentMethod)
                sb.AppendLine(FormatLabelValue("Payment:", paymentLabel))
                If receipt.Transaction.PaymentMethod = PaymentMethod.Cash Then
                    sb.AppendLine(FormatLabelValue("Tendered:", receipt.Transaction.AmountTendered.ToString("F2")))
                    sb.AppendLine(FormatLabelValue("Change:", receipt.Transaction.ChangeAmount.ToString("F2")))
                End If
            End If

            sb.AppendLine(Separator)
            sb.AppendLine(CenterText("Thank you for your purchase!"))
            sb.AppendLine(CenterText("This serves as your Official Receipt."))

            Return sb.ToString()
        End Function

        Private Shared Function CenterText(text As String) As String
            If text.Length >= LineWidth Then Return text.Substring(0, LineWidth)
            Dim padding = (LineWidth - text.Length) \ 2
            Return text.PadLeft(padding + text.Length).PadRight(LineWidth)
        End Function

        Private Shared Function FormatLabelValue(label As String, value As String) As String
            Dim available = LineWidth - label.Length - 1
            If available < 1 Then Return (label & " " & value).Substring(0, LineWidth)
            Return label & value.PadLeft(available + value.Length).Substring(Math.Max(0, value.Length - available))
        End Function

        Private Shared Function FormatItemHeader() As String
            Dim item = "Item"
            Dim qty = "Qty"
            Dim price = "Price"
            Dim total = "Total"
            Return $"{item,-18}{qty,4}{price,8}{total,10}"
        End Function

        Private Shared Function FormatItemLine(name As String, qty As Integer, unitPrice As Decimal, lineTotal As Decimal) As String
            Dim truncatedName = If(name.Length > 18, name.Substring(0, 18), name)
            Return $"{truncatedName,-18}{qty,4}{unitPrice,8:F2}{lineTotal,10:F2}"
        End Function

        Private Shared Function ParseItemsSnapshot(items As String) As IEnumerable(Of String)
            If String.IsNullOrWhiteSpace(items) Then Return Enumerable.Empty(Of String)()
            Dim result As New List(Of String)()
            For Each rawLine In items.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                Dim parts = rawLine.Split("|"c)
                If parts.Length = 4 Then
                    Dim name = parts(0)
                    Dim qty As Integer
                    Dim unitPrice As Decimal
                    Dim lineTotal As Decimal
                    If Integer.TryParse(parts(1), qty) AndAlso
                       Decimal.TryParse(parts(2), unitPrice) AndAlso
                       Decimal.TryParse(parts(3), lineTotal) Then
                        result.Add(FormatItemLine(name, qty, unitPrice, lineTotal))
                    End If
                End If
            Next
            Return result
        End Function

        Private Shared Function GetPaymentLabel(method As PaymentMethod) As String
            Select Case method
                Case PaymentMethod.Cash : Return "Cash"
                Case PaymentMethod.GCash : Return "GCash"
                Case PaymentMethod.BankTransfer : Return "Bank Transfer"
                Case PaymentMethod.Credit : Return "Credit (Utang)"
                Case Else : Return method.ToString()
            End Select
        End Function

    End Class

End Namespace
