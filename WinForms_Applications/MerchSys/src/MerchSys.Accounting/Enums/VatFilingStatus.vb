Namespace Enums

    ''' <summary>Lifecycle status of a VAT return. Once <c>Filed</c>, deletion is blocked at the service layer (ACC-11).</summary>
    Public Enum VatFilingStatus
        Draft = 0
        Generated = 1
        Filed = 2
        Amended = 3
    End Enum

End Namespace
