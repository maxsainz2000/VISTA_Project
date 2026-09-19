Namespace Enums

    ''' <summary>
    ''' Payment methods accepted at the point of sale. E-wallet and bank transfer entries are
    ''' recorded for audit purposes only — no actual electronic transfer is initiated by the system.
    ''' </summary>
    Public Enum PaymentMethod

        ''' <summary>Physical cash payment received at the counter.</summary>
        Cash = 1

        ''' <summary>GCash e-wallet payment — recorded only, no system-initiated transfer.</summary>
        GCash = 2

        ''' <summary>Bank transfer payment — recorded only, no system-initiated transfer.</summary>
        BankTransfer = 3

        ''' <summary>Credit (utang) — charged to the customer's informal credit account.</summary>
        Credit = 4

    End Enum

End Namespace
