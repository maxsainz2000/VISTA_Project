Imports MerchSys.SharedKernel.Enums

Namespace Entities

    ''' <summary>
    ''' One source-document line contributing to a <see cref="VatReturn"/>.  Kept for audit
    ''' traceability back to the originating POS receipt or Purchasing goods-received row.
    ''' See <c>concepts/vat-ready.md</c>.
    ''' </summary>
    Public Class VatReturnLine
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>FK to the parent <see cref="VatReturn"/> header.</summary>
        Public Property VatReturnId As Integer

        ''' <summary>Originating module — "POS" or "Purchasing".</summary>
        Public Property SourceModule As String

        ''' <summary>Originating table — e.g., "Pos_OfficialReceipts" or "Pur_GoodsReceived".</summary>
        Public Property SourceTable As String

        ''' <summary>
        ''' Primary key of the source row in its originating table.
        ''' Long to accommodate sync-journal IDs from MariaDB reconciliation.
        ''' </summary>
        Public Property SourceRowId As Long

        ''' <summary>Transaction date of the originating document.</summary>
        Public Property TransactionDate As DateTime

        ''' <summary>Vatable portion of this line.</summary>
        Public Property VatableAmount As Decimal

        ''' <summary>VAT-exempt portion of this line.</summary>
        Public Property VatExemptAmount As Decimal

        ''' <summary>Zero-rated portion of this line.</summary>
        Public Property ZeroRatedAmount As Decimal

        ''' <summary>Output VAT on this line (0 for input-VAT-only lines).</summary>
        Public Property OutputVat As Decimal

        ''' <summary>Input VAT on this line (0 for output-VAT-only lines).</summary>
        Public Property InputVat As Decimal

        ''' <summary>BIR VAT classification applied to this source document line.</summary>
        Public Property Treatment As VatTreatment

        ''' <summary>Navigation back to the parent VAT return header.</summary>
        Public Property VatReturn As VatReturn

    End Class

End Namespace
