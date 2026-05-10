Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.POS.Data
Imports MerchSys.SharedKernel.Queries

Namespace Handlers

    ''' <summary>
    ''' Returns the singleton <see cref="MerchSys.POS.Entities.VatConfiguration"/> row (Id = 1)
    ''' for cross-module consumption by ACC-11 <c>VatReportingService</c>.
    ''' Reads AsNoTracking for read-only query performance.
    ''' </summary>
    Public Class GetVatConfigurationQueryHandler
        Implements IRequestHandler(Of GetVatConfigurationQuery, GetVatConfigurationResult)

        Private ReadOnly _db As POSDbContext
        Private ReadOnly _logger As ILogger(Of GetVatConfigurationQueryHandler)

        Public Sub New(db As POSDbContext, logger As ILogger(Of GetVatConfigurationQueryHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(request As GetVatConfigurationQuery, cancellationToken As CancellationToken) As Task(Of GetVatConfigurationResult) _
            Implements IRequestHandler(Of GetVatConfigurationQuery, GetVatConfigurationResult).Handle

            Dim config = Await _db.VatConfigurations.
                AsNoTracking().
                FirstOrDefaultAsync(Function(v) v.Id = 1, cancellationToken)

            If config Is Nothing Then
                _logger.LogWarning("VatConfiguration row (Id=1) not found — returning non-VAT defaults.")
                Return New GetVatConfigurationResult With {
                    .IsVatRegistered = False,
                    .VatRate = 0.12D,
                    .NonVatPercentageTaxRate = 0.03D
                }
            End If

            Return New GetVatConfigurationResult With {
                .IsVatRegistered = config.IsVatRegistered,
                .VatRate = config.VatRate,
                .NonVatPercentageTaxRate = config.NonVatPercentageTaxRate
            }
        End Function

    End Class

End Namespace
