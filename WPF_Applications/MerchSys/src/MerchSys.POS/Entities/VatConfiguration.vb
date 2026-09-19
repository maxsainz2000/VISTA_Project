Imports MerchSys.SharedKernel.Entities

Namespace Entities

    ''' <summary>
    ''' Singleton VAT configuration row for the business. Exactly one record is permitted; the
    ''' database enforces this with a check constraint on Id = 1 plus the primary key uniqueness.
    ''' Stores whether the business is VAT-registered, the active rate, and BIR business identifiers
    ''' required to appear on every Official Receipt.
    ''' See: LLM_Wiki/wiki/concepts/vat-ready.md
    ''' </summary>
    Public Class VatConfiguration
        Inherits AuditableEntity

        ''' <summary>
        ''' True when the business is VAT-registered with BIR and must collect 12% output VAT.
        ''' When False, every line is exempt and <see cref="NonVatPercentageTaxRate"/> governs
        ''' aggregate percentage-tax reporting via ACC-11.
        ''' </summary>
        Public Property IsVatRegistered As Boolean

        ''' <summary>
        ''' Active VAT rate; default 0.12 (12%). Stored with precision(5,4).
        ''' Captured as <c>VatRateSnapshot</c> on each <c>SalesTransaction</c> so historical
        ''' receipts remain accurate after a rate change.
        ''' </summary>
        Public Property VatRate As Decimal

        ''' <summary>Date from which the current rate and registration status become effective.</summary>
        Public Property EffectiveFrom As DateTime

        ''' <summary>BIR-issued Tax Identification Number for the business.</summary>
        Public Property BusinessTIN As String

        ''' <summary>Legal name of the business as registered with BIR (e.g., "Villon Farm Supply").</summary>
        Public Property BusinessName As String

        ''' <summary>Registered address of the business, printed on Official Receipts.</summary>
        Public Property BusinessAddress As String

        ''' <summary>
        ''' Percentage tax rate applied under BIR Form 2551Q when <see cref="IsVatRegistered"/> is False;
        ''' default 0.03 (3%). This is an aggregate-level tax reported by ACC-11 — it is not computed
        ''' per line within this module.
        ''' </summary>
        Public Property NonVatPercentageTaxRate As Decimal

    End Class

End Namespace
