Imports MerchSys.SharedKernel.Enums

Namespace Entities

    ''' <summary>
    ''' Partial-class extensions adding BIR three-bucket VAT columns to every Accounting
    ''' ledger entity.  Entities that carry only output VAT (revenue) leave <c>InputVat = 0</c>;
    ''' entities that carry only input VAT (expenses) leave <c>OutputVat = 0</c>.
    ''' See <c>concepts/vat-ready.md</c> for ledger requirements.
    ''' </summary>
    Partial Public Class RevenueRecord
        ''' <summary>Portion of the net sale amount subject to 12 % output VAT.</summary>
        Public Property VatableAmount As Decimal

        ''' <summary>Portion of the net sale amount that is BIR-exempt (Sec. 109 NIRC).</summary>
        Public Property VatExemptAmount As Decimal

        ''' <summary>Portion of the net sale amount that is zero-rated.</summary>
        Public Property ZeroRatedAmount As Decimal

        ''' <summary>Output VAT collected on this revenue line (VatableAmount × configured rate).</summary>
        Public Property OutputVat As Decimal

        ''' <summary>Always 0 for revenue rows; input VAT is not applicable to sales lines.</summary>
        Public Property InputVat As Decimal

        ''' <summary>BIR VAT classification applied to this revenue line.</summary>
        Public Property VatTreatment As VatTreatment
    End Class

    Partial Public Class ExpenseRecord
        ''' <summary>Portion of the expense amount subject to VAT (claimable input).</summary>
        Public Property VatableAmount As Decimal

        ''' <summary>Portion of the expense amount that is VAT-exempt.</summary>
        Public Property VatExemptAmount As Decimal

        ''' <summary>Portion of the expense amount that is zero-rated.</summary>
        Public Property ZeroRatedAmount As Decimal

        ''' <summary>Always 0 for expense rows; output VAT is not applicable to cost lines.</summary>
        Public Property OutputVat As Decimal

        ''' <summary>Input VAT paid on this expense line (claimable against output VAT).</summary>
        Public Property InputVat As Decimal

        ''' <summary>BIR VAT classification applied to this expense line.</summary>
        Public Property VatTreatment As VatTreatment
    End Class

End Namespace
