Imports MerchSys.Accounting.Enums

Namespace Entities

    ''' <summary>
    ''' Header record for one BIR VAT filing period (monthly Form 2550M, quarterly Form 2550Q,
    ''' or quarterly percentage-tax Form 2551Q).  Once <c>FilingStatus = Filed</c> this record
    ''' is immutable; deletion and amendment are enforced at the service layer in ACC-11.
    ''' See <c>concepts/vat-ready.md</c> and <c>concepts/bir-compliance.md</c>.
    ''' </summary>
    Public Class VatReturn
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Calendar year of the filing period (e.g., 2026).</summary>
        Public Property Year As Integer

        ''' <summary>Period number: 1–12 for monthly filings; 1–4 for quarterly filings.</summary>
        Public Property Period As Integer

        ''' <summary>Whether this return covers a monthly or quarterly period.</summary>
        Public Property PeriodType As VatReturnPeriodType

        ''' <summary>BIR form type — 2550M, 2550Q, or 2551Q.</summary>
        Public Property FormType As VatReturnFormType

        ''' <summary>Total sales amount subject to 12 % VAT for the period.</summary>
        Public Property TotalVatableSales As Decimal

        ''' <summary>Total sales amount that is BIR-exempt for the period.</summary>
        Public Property TotalVatExemptSales As Decimal

        ''' <summary>Total sales amount that is zero-rated for the period.</summary>
        Public Property TotalZeroRatedSales As Decimal

        ''' <summary>Total output VAT due on vatable sales.</summary>
        Public Property TotalOutputVat As Decimal

        ''' <summary>Total purchases subject to VAT (claimable input basis).</summary>
        Public Property TotalVatablePurchases As Decimal

        ''' <summary>Total input VAT claimable from vatable purchases.</summary>
        Public Property TotalInputVat As Decimal

        ''' <summary>
        ''' Net VAT payable: OutputVat minus InputVat for VAT-registered entities,
        ''' or 3 % of non-VAT sales for Form 2551Q filers.
        ''' </summary>
        Public Property VatPayable As Decimal

        ''' <summary>Current lifecycle status of this return.</summary>
        Public Property FilingStatus As VatFilingStatus

        ''' <summary>UTC timestamp when this return was filed with BIR; Nothing if not yet filed.</summary>
        Public Property FiledAt As DateTime?

        ''' <summary>Username of the staff member who filed this return; Nothing if not yet filed.</summary>
        Public Property FiledBy As String

        ''' <summary>UTC timestamp when this return was generated from ledger data.</summary>
        Public Property GeneratedAt As DateTime

        ''' <summary>
        ''' Snapshot of the merchant's VAT-registration status at generation time.
        ''' Determines whether 2550M/Q or 2551Q rules apply.
        ''' </summary>
        Public Property IsVatRegisteredSnapshot As Boolean

        ''' <summary>Line-level breakdown backing this return (cascade-deleted with the header).</summary>
        Public Property Lines As ICollection(Of VatReturnLine)

    End Class

End Namespace
