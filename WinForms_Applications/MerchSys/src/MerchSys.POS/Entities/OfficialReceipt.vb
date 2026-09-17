Namespace Entities

    ''' <summary>
    ''' BIR-compliant official receipt issued for a <see cref="SalesTransaction"/>.
    ''' Receipt numbers follow the format OR-YYYY-XXXX — auto-sequential with no gaps,
    ''' as required by Bureau of Internal Revenue regulations.
    ''' One receipt is produced per transaction; receipts are never voided independently
    ''' of their parent transaction.
    ''' </summary>
    Public Class OfficialReceipt
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Foreign key to the associated <see cref="SalesTransaction"/>.</summary>
        Public Property TransactionId As Integer

        ''' <summary>Sequential receipt number in OR-YYYY-XXXX format (BIR-mandated).</summary>
        Public Property ReceiptNumber As String

        ''' <summary>Legal business name printed on the receipt (e.g. "Villon Farm Supply").</summary>
        Public Property BusinessName As String

        ''' <summary>Business address printed on the receipt.</summary>
        Public Property BusinessAddress As String

        ''' <summary>BIR-issued Tax Identification Number printed on the receipt.</summary>
        Public Property BusinessTIN As String

        ''' <summary>Date and time the receipt was issued.</summary>
        Public Property IssueDate As DateTime

        ''' <summary>
        ''' Serialized snapshot of the line items printed on the receipt.
        ''' Stored as formatted text or JSON so the receipt can be reprinted at any time.
        ''' </summary>
        Public Property Items As String

        ''' <summary>Total amount shown on the receipt.</summary>
        Public Property TotalAmount As Decimal

        ''' <summary>VAT amount shown on the receipt (0 when not VAT-registered).</summary>
        Public Property VatAmount As Decimal

        ''' <summary>True when the business is VAT-registered; driven by system configuration.</summary>
        Public Property IsVatRegistered As Boolean

        ' --- Navigation ---

        ''' <summary>The transaction for which this receipt was issued.</summary>
        Public Property Transaction As SalesTransaction

    End Class

End Namespace
