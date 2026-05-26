Namespace Queries

    ''' <summary>
    ''' Result of <see cref="GetVatConfigurationQuery"/>.
    ''' Carries the VAT registration flag and applicable rates for the reporting period.
    ''' </summary>
    Public Class GetVatConfigurationResult

        ''' <summary>True when the business is VAT-registered and must file Form 2550M/Q.</summary>
        Public Property IsVatRegistered As Boolean

        ''' <summary>Active VAT rate (default 0.12). Used for display only — actual output VAT is pre-calculated on ledger rows.</summary>
        Public Property VatRate As Decimal

        ''' <summary>Percentage tax rate for non-VAT filers (default 0.03). Multiplied against gross receipts for Form 2551Q.</summary>
        Public Property NonVatPercentageTaxRate As Decimal

        ''' <summary>BIR-issued Tax Identification Number for the business.</summary>
        Public Property BusinessTIN As String

        ''' <summary>Legal name of the business as registered with BIR.</summary>
        Public Property BusinessName As String

        ''' <summary>Registered address of the business.</summary>
        Public Property BusinessAddress As String

    End Class

End Namespace
