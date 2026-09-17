Imports MediatR

Namespace Queries

    ''' <summary>
    ''' Returns the singleton <c>Pos_VatConfiguration</c> row (Id = 1).
    ''' Handled by <c>GetVatConfigurationQueryHandler</c> in MerchSys.POS.
    ''' Consumed by ACC-11 (VatReportingService) to gate Form 2550M/Q vs Form 2551Q generation.
    ''' </summary>
    Public Class GetVatConfigurationQuery
        Implements IRequest(Of GetVatConfigurationResult)
    End Class

End Namespace
