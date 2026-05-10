Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Entities
Imports MerchSys.Accounting.Enums
Imports MerchSys.Accounting.Exceptions
Imports MerchSys.SharedKernel.Queries

Namespace Services

    ''' <summary>
    ''' Computes the VAT Payable (or Percentage Tax) KPI for the Financial Overview dashboard.
    ''' For VAT-registered businesses it generates or reuses the current month's Form 2550M.
    ''' For non-VAT businesses it generates or reuses the current quarter's Form 2551Q.
    ''' Invokes <see cref="IVatReportingService"/> on every call; the service is idempotent for
    ''' <c>Generated</c>-state returns and reads the filed record on <see cref="VatReturnLockedException"/>.
    ''' </summary>
    Public Class VatPayableKpiProvider
        Implements IKpiProvider

        Private ReadOnly _vatReportingService As IVatReportingService
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _logger As ILogger(Of VatPayableKpiProvider)

        Public Sub New(vatReportingService As IVatReportingService,
                       mediator As IMediator,
                       logger As ILogger(Of VatPayableKpiProvider))
            _vatReportingService = vatReportingService
            _mediator = mediator
            _logger = logger
        End Sub

        Public ReadOnly Property KpiKey As String = "VatPayable" Implements IKpiProvider.KpiKey

        Public Async Function ProvideAsync(asOfDate As DateTime) As Task(Of KpiValue) Implements IKpiProvider.ProvideAsync
            Try
                Dim vatConfig = Await _mediator.Send(New GetVatConfigurationQuery())

                Dim vatReturn As VatReturn = Nothing
                Dim displayLabel As String = String.Empty
                Dim deadline As DateTime = DateTime.UtcNow
                Dim periodDescription As String = String.Empty

                If vatConfig.IsVatRegistered Then
                    Dim year = asOfDate.Year
                    Dim month = asOfDate.Month
                    deadline = FilingDeadline(VatReturnFormType.Form2550M, year, month)
                    periodDescription = New DateTime(year, month, 1).ToString("MMMM yyyy")
                    displayLabel = "VAT Payable"

                    Dim monthlyLocked As Boolean = False
                    Try
                        vatReturn = Await _vatReportingService.GenerateMonthlyVatReturnAsync(year, month)
                    Catch ex As VatReturnLockedException
                        monthlyLocked = True
                    End Try
                    If monthlyLocked Then
                        ' Period already filed — read the existing filed return
                        Dim all = Await _vatReportingService.ListReturnsAsync(year)
                        vatReturn = all.FirstOrDefault(
                            Function(r) r.Period = month AndAlso r.PeriodType = VatReturnPeriodType.Monthly)
                    End If
                Else
                    Dim year = asOfDate.Year
                    Dim quarter = GetCurrentQuarter(asOfDate.Month)
                    deadline = FilingDeadline(VatReturnFormType.Form2551Q, year, quarter)
                    periodDescription = $"Q{quarter} {year}"
                    displayLabel = "Percentage Tax"

                    Dim quarterlyLocked As Boolean = False
                    Try
                        vatReturn = Await _vatReportingService.GenerateNonVatPercentageTaxAsync(year, quarter)
                    Catch ex As VatReturnLockedException
                        quarterlyLocked = True
                    End Try
                    If quarterlyLocked Then
                        Dim all = Await _vatReportingService.ListReturnsAsync(year)
                        vatReturn = all.FirstOrDefault(
                            Function(r) r.Period = quarter AndAlso r.PeriodType = VatReturnPeriodType.Quarterly)
                    End If
                End If

                If vatReturn Is Nothing Then
                    Return New KpiValue With {
                        .Key = KpiKey,
                        .DisplayLabel = displayLabel,
                        .Amount = 0D,
                        .SecondaryText = Nothing,
                        .DueDate = Nothing,
                        .Severity = KpiSeverity.Info
                    }
                End If

                Dim daysUntilDeadline = (deadline.Date - asOfDate.Date).Days
                Dim severity As KpiSeverity
                If daysUntilDeadline <= 3 Then
                    severity = KpiSeverity.Critical
                ElseIf daysUntilDeadline <= 7 Then
                    severity = KpiSeverity.Warning
                Else
                    severity = KpiSeverity.Info
                End If

                Return New KpiValue With {
                    .Key = KpiKey,
                    .DisplayLabel = displayLabel,
                    .Amount = vatReturn.VatPayable,
                    .SecondaryText = "Due " & deadline.ToString("MMM d"),
                    .DueDate = deadline,
                    .Severity = severity
                }

            Catch ex As Exception
                _logger.LogWarning(ex, "VatPayableKpiProvider: could not compute VAT KPI.")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Returns the BIR eFPS filing deadline for the given form and period.
        ''' Form 2550M: 25th of the following month (BIR eFPS rule, concepts/bir-compliance.md).
        ''' Form 2550Q / 2551Q: 25th of the month following the quarter end.
        ''' </summary>
        Private Shared Function FilingDeadline(formType As VatReturnFormType,
                                               year As Integer,
                                               period As Integer) As DateTime
            If formType = VatReturnFormType.Form2550M Then
                ' period = month (1–12); eFPS filers: 25th of the following month
                Return New DateTime(year, period, 1).AddMonths(1).AddDays(24)
            Else
                ' period = quarter (1–4); deadline = 25th of the month after the quarter closes
                Dim quarterEndMonth = period * 3
                Dim followingMonth = New DateTime(year, quarterEndMonth, 1).AddMonths(1)
                Return New DateTime(followingMonth.Year, followingMonth.Month, 25)
            End If
        End Function

        Private Shared Function GetCurrentQuarter(month As Integer) As Integer
            Return (month - 1) \ 3 + 1
        End Function

    End Class

End Namespace
