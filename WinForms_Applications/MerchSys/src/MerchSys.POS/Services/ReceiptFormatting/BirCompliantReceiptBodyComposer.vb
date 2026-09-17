Imports MerchSys.POS.Entities

Namespace Services

    ''' <summary>
    ''' BIR-compliant receipt body composer.
    ''' Produces a five-block <see cref="ReceiptBody"/> that satisfies the Bureau of Internal
    ''' Revenue Official Receipt requirements for both VAT-registered and non-VAT taxpayers.
    ''' Reads VAT bucket totals (VatableSales, VatExemptSales, ZeroRatedSales) from the
    ''' associated <see cref="SalesTransaction"/> populated by POS-14's VatAwareReceiptService.
    ''' </summary>
    Public Class BirCompliantReceiptBodyComposer
        Implements IReceiptBodyComposer

        ' Receipt paper assumed to be 40-character-wide standard thermal.
        ' No hard-wrap is encoded here — the printer driver is responsible for physical width.
        ' Label column for the VAT disclosure block.
        Private Const DisclosureLabelWidth As Integer = 22

        Public Function ComposeAsync(
            receipt As OfficialReceipt,
            lineItems As IReadOnlyList(Of SalesTransactionLine),
            vatConfig As VatConfiguration
        ) As Task(Of ReceiptBody) Implements IReceiptBodyComposer.ComposeAsync

            If String.IsNullOrWhiteSpace(vatConfig.BusinessTIN) Then
                Throw New InvalidOperationException(
                    "VatConfiguration.BusinessTIN is required on every Official Receipt. " &
                    "Update the VAT settings via VatSettingsView before issuing receipts.")
            End If

            Dim body As New ReceiptBody() With {
                .HeaderLines = BuildHeader(receipt, vatConfig).AsReadOnly(),
                .ItemLines = BuildItemLines(lineItems).AsReadOnly(),
                .TotalsBlock = BuildTotals(receipt, lineItems).AsReadOnly(),
                .VatDisclosureBlock = BuildVatDisclosure(receipt, vatConfig).AsReadOnly(),
                .FooterLines = BuildFooter().AsReadOnly()
            }

            Return Task.FromResult(body)
        End Function

        ' --- Block builders ---

        Private Shared Function BuildHeader(receipt As OfficialReceipt, vatConfig As VatConfiguration) As List(Of String)
            Dim lines As New List(Of String)()

            lines.Add(vatConfig.BusinessName)

            If Not String.IsNullOrWhiteSpace(vatConfig.BusinessAddress) Then
                For Each addressLine In vatConfig.BusinessAddress.Split(
                        New String() {Environment.NewLine, vbCrLf, vbLf},
                        StringSplitOptions.RemoveEmptyEntries)
                    lines.Add(addressLine.Trim())
                Next
            End If

            If vatConfig.IsVatRegistered Then
                lines.Add("VAT-Registered Taxpayer")
            Else
                lines.Add("Non-VAT Taxpayer")
            End If
            lines.Add("TIN: " & vatConfig.BusinessTIN)

            ' OR No. printed using the receipt number produced by IReceiptIntegrityService,
            ' which follows the BIR Form 2541 nine-digit zero-padded sequence convention.
            lines.Add("OR No.: " & receipt.ReceiptNumber)
            lines.Add("Date: " & receipt.IssueDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))

            Return lines
        End Function

        Private Shared Function BuildItemLines(lineItems As IReadOnlyList(Of SalesTransactionLine)) As List(Of String)
            Dim lines As New List(Of String)()
            If lineItems Is Nothing OrElse lineItems.Count = 0 Then Return lines

            lines.Add(FormatItemHeader())
            lines.Add(New String("-"c, 40))

            For Each item In lineItems
                lines.Add(FormatItemLine(item.ProductName, item.Quantity, item.UnitPrice, item.LineTotal))
                If item.DiscountAmount > 0D Then
                    lines.Add("  Discount: -" & item.DiscountAmount.ToString("N2"))
                End If
            Next

            Return lines
        End Function

        Private Shared Function BuildTotals(receipt As OfficialReceipt, lineItems As IReadOnlyList(Of SalesTransactionLine)) As List(Of String)
            Dim lines As New List(Of String)()

            Dim subtotal = If(lineItems IsNot Nothing AndAlso lineItems.Count > 0,
                              lineItems.Sum(Function(l) l.LineTotal),
                              receipt.TotalAmount)

            lines.Add("Subtotal: " & Money(subtotal))
            lines.Add("Total:    " & Money(receipt.TotalAmount))

            Return lines
        End Function

        ''' <summary>
        ''' Builds the BIR-mandated VAT disclosure block.
        ''' For VAT-registered taxpayers: four lines (VATable Sales, VAT-Exempt Sales,
        ''' Zero-Rated Sales, Output VAT) with a rule separator before Output VAT.
        ''' For non-VAT taxpayers: two lines (Gross Sales, Percentage Tax).
        ''' Zero values are always printed — BIR requires unambiguous disclosure, not condensed output.
        ''' </summary>
        Private Shared Function BuildVatDisclosure(receipt As OfficialReceipt, vatConfig As VatConfiguration) As List(Of String)
            Dim lines As New List(Of String)()

            If vatConfig.IsVatRegistered Then
                Dim txn = receipt.Transaction
                Dim vatableSales As Decimal = If(txn IsNot Nothing, txn.VatableSales, 0D)
                Dim vatExemptSales As Decimal = If(txn IsNot Nothing, txn.VatExemptSales, 0D)
                Dim zeroRatedSales As Decimal = If(txn IsNot Nothing, txn.ZeroRatedSales, 0D)
                Dim outputVat As Decimal = receipt.VatAmount

                lines.Add(DisclosureLine("VATable Sales:", Money(vatableSales)))
                lines.Add(DisclosureLine("VAT-Exempt Sales:", Money(vatExemptSales)))
                lines.Add(DisclosureLine("Zero-Rated Sales:", Money(zeroRatedSales)))
                lines.Add(New String(" "c, DisclosureLabelWidth) & "─────────")
                lines.Add(DisclosureLine("Output VAT (12%):", Money(outputVat)))
            Else
                Dim grossSales = receipt.TotalAmount
                Dim taxRate = vatConfig.NonVatPercentageTaxRate
                Dim percentageTax = Math.Round(grossSales * taxRate, 2)
                Dim rateDisplay = (taxRate * 100D).ToString("0") & "%"

                lines.Add(DisclosureLine("Gross Sales:", Money(grossSales)))
                lines.Add(DisclosureLine($"Percentage Tax ({rateDisplay}):", Money(percentageTax)))
            End If

            Return lines
        End Function

        Private Shared Function BuildFooter() As List(Of String)
            Return New List(Of String)() From {
                "Thank you for your business.",
                "This serves as your Official Receipt."
            }
        End Function

        ' --- Formatting helpers ---

        Private Shared Function Money(amount As Decimal) As String
            Return "₱" & amount.ToString("N2")
        End Function

        Private Shared Function DisclosureLine(label As String, value As String) As String
            Return label.PadRight(DisclosureLabelWidth) & value
        End Function

        Private Shared Function FormatItemHeader() As String
            Return $"{"Item",-18}{"Qty",4}{"Price",8}{"Total",10}"
        End Function

        Private Shared Function FormatItemLine(name As String, qty As Integer, unitPrice As Decimal, lineTotal As Decimal) As String
            Dim truncated = If(name.Length > 18, name.Substring(0, 18), name)
            Return $"{truncated,-18}{qty,4}{unitPrice,8:F2}{lineTotal,10:F2}"
        End Function

    End Class

End Namespace
