Namespace Enums

    ''' <summary>
    ''' BIR three-bucket VAT classification applied at the product line level.
    ''' Determines how a sale or purchase line is taxed under the Philippine VAT regime.
    ''' Referenced by <see cref="Events.SaleCompletedWithVatEvent.SaleItemWithVat"/> and
    ''' <see cref="Events.GoodsReceivedWithVatEvent.GoodsReceivedItemWithVat"/>.
    ''' BIR rationale: Revenue Regulations No. 16-2005 mandates separate disclosure of
    ''' vatable, exempt, and zero-rated sales on every OR.
    ''' </summary>
    Public Enum VatTreatment

        ''' <summary>Subject to 12% output/input VAT (or the configured rate). Default for most agricultural retail transactions.</summary>
        Vatable = 0

        ''' <summary>BIR-exempt — e.g., agricultural inputs under Sec. 109(B) NIRC. No VAT collected or claimable.</summary>
        Exempt = 1

        ''' <summary>0%-rated — e.g., export sales. VAT is charged at zero rate; input VAT on related purchases is creditable.</summary>
        ZeroRated = 2

    End Enum

End Namespace
