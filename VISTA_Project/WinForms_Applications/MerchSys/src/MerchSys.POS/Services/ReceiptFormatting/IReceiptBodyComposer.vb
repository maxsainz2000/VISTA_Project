Imports MerchSys.POS.Entities

Namespace Services

    ''' <summary>
    ''' Builds a structured, printer-ready body for a BIR-compliant Official Receipt.
    ''' The body is divided into five named blocks (Header, Items, Totals, VatDisclosure, Footer)
    ''' so future rendering layers (ESC/POS bytes, PDF, graphical templates) can style each
    ''' block independently without touching compliance logic.
    ''' BIR RR 18-2012 §4 requires the VAT disclosure block on every Official Receipt issued by
    ''' a VAT-registered taxpayer; this interface enforces that separation at the content level.
    ''' </summary>
    Public Interface IReceiptBodyComposer

        ''' <summary>
        ''' Composes the full receipt body from <paramref name="receipt"/>, its
        ''' <paramref name="lineItems"/>, and the active <paramref name="vatConfig"/>.
        ''' </summary>
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when <c>vatConfig.BusinessTIN</c> is null or empty — a taxpayer without
        ''' a TIN is a configuration error, not a runtime fallback condition.
        ''' Use POS-17 VatSettingsView to correct the configuration before issuing receipts.
        ''' </exception>
        Function ComposeAsync(
            receipt As OfficialReceipt,
            lineItems As IReadOnlyList(Of SalesTransactionLine),
            vatConfig As VatConfiguration
        ) As Task(Of ReceiptBody)

    End Interface

    ''' <summary>
    ''' Structured receipt body composed of five named blocks.
    ''' Blocks are joined with one blank-line separator by <see cref="AllLines"/>.
    ''' Plain-text lines store peso amounts using the "₱" glyph. ASCII-only thermal printers
    ''' that cannot render "₱" must substitute "PHP " in their own printer-driver layer;
    ''' the stored content always uses "₱" so digital copies remain correct.
    ''' </summary>
    Public Class ReceiptBody

        Public Property HeaderLines As IReadOnlyList(Of String)
        Public Property ItemLines As IReadOnlyList(Of String)
        Public Property TotalsBlock As IReadOnlyList(Of String)
        Public Property VatDisclosureBlock As IReadOnlyList(Of String)
        Public Property FooterLines As IReadOnlyList(Of String)

        ''' <summary>
        ''' Concatenates all five blocks in order, inserting one empty line between each block.
        ''' </summary>
        Public ReadOnly Property AllLines As IEnumerable(Of String)
            Get
                Dim all As New List(Of String)()
                AddBlock(all, HeaderLines)
                all.Add(String.Empty)
                AddBlock(all, ItemLines)
                all.Add(String.Empty)
                AddBlock(all, TotalsBlock)
                all.Add(String.Empty)
                AddBlock(all, VatDisclosureBlock)
                all.Add(String.Empty)
                AddBlock(all, FooterLines)
                Return all.AsReadOnly()
            End Get
        End Property

        Private Shared Sub AddBlock(target As List(Of String), block As IReadOnlyList(Of String))
            If block IsNot Nothing Then target.AddRange(block)
        End Sub

    End Class

End Namespace
