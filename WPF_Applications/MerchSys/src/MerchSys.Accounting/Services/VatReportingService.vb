' Line-number mapping used by VatReturnExporter:
'   Form 2550M : lines 1–23 (TotalVatableSales→L1, ZeroRated→L2, Exempt→L3,
'                             TotalGross→L4, OutputVat→L5, InputVat→L9, VatPayable→L11)
'   Form 2550Q : lines 1–28 (same buckets across Q1/Q2/Q3 months; net→L14, due→L28)
'   Form 2551Q : lines 1–14 (gross receipts→L4, rate→L5, tax due→L6)

Imports System.Threading
Imports MediatR
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.Accounting.Enums
Imports MerchSys.Accounting.Exceptions
Imports MerchSys.SharedKernel.Enums
Imports MerchSys.SharedKernel.Queries

Namespace Services

    Public Class VatReportingService
        Implements IVatReportingService

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _mediator As IMediator
        Private ReadOnly _logger As ILogger(Of VatReportingService)

        Public Sub New(db As AccountingDbContext, mediator As IMediator, logger As ILogger(Of VatReportingService))
            _db = db
            _mediator = mediator
            _logger = logger
        End Sub

        ' ─── Public Interface ────────────────────────────────────────────────────────

        ''' <summary>
        ''' Generates Form 2550M (monthly VAT) for the given year/month.
        ''' Rejects if <c>IsVatRegistered = False</c>.  Regenerates if <c>Generated</c>;
        ''' throws <see cref="VatReturnLockedException"/> if <c>Filed</c>.
        ''' </summary>
        Public Async Function GenerateMonthlyVatReturnAsync(year As Integer, month As Integer) As Task(Of VatReturn) _
            Implements IVatReportingService.GenerateMonthlyVatReturnAsync

            Dim vatConfig = Await _mediator.Send(New GetVatConfigurationQuery())

            If Not vatConfig.IsVatRegistered Then
                Throw New InvalidOperationException(
                    "This business is not VAT-registered. Use GenerateNonVatPercentageTaxAsync (Form 2551Q) instead.")
            End If

            Dim windowStart = New DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc)
            Dim windowEnd = windowStart.AddMonths(1)

            Dim data = Await CollectLedgerDataAsync(windowStart, windowEnd)
            Dim vatPayable = Math.Round(data.TotalOutputVat - data.TotalInputVat, 2, MidpointRounding.ToEven)

            Await GuardAndClearExistingAsync(year, month, VatReturnPeriodType.Monthly, VatReturnFormType.Form2550M)

            Dim vatReturn = BuildVatReturn(
                year, month, VatReturnPeriodType.Monthly, VatReturnFormType.Form2550M,
                data, vatPayable, vatConfig.IsVatRegistered)

            _db.VatReturns.Add(vatReturn)
            Await _db.SaveChangesAsync()

            _logger.LogInformation("Generated Form 2550M: Year={Year} Month={Month} VatPayable={VatPayable}.", year, month, vatPayable)
            Return vatReturn
        End Function

        ''' <summary>
        ''' Generates Form 2550Q (quarterly VAT) for the given year/quarter (1–4).
        ''' Quarter windows: Q1=Jan–Mar, Q2=Apr–Jun, Q3=Jul–Sep, Q4=Oct–Dec.
        ''' </summary>
        Public Async Function GenerateQuarterlyVatReturnAsync(year As Integer, quarter As Integer) As Task(Of VatReturn) _
            Implements IVatReportingService.GenerateQuarterlyVatReturnAsync

            Dim vatConfig = Await _mediator.Send(New GetVatConfigurationQuery())

            If Not vatConfig.IsVatRegistered Then
                Throw New InvalidOperationException(
                    "This business is not VAT-registered. Use GenerateNonVatPercentageTaxAsync (Form 2551Q) instead.")
            End If

            Dim window = QuarterWindow(year, quarter)
            Dim data = Await CollectLedgerDataAsync(window.Start, window.End_)
            Dim vatPayable = Math.Round(data.TotalOutputVat - data.TotalInputVat, 2, MidpointRounding.ToEven)

            Await GuardAndClearExistingAsync(year, quarter, VatReturnPeriodType.Quarterly, VatReturnFormType.Form2550Q)

            Dim vatReturn = BuildVatReturn(
                year, quarter, VatReturnPeriodType.Quarterly, VatReturnFormType.Form2550Q,
                data, vatPayable, vatConfig.IsVatRegistered)

            _db.VatReturns.Add(vatReturn)
            Await _db.SaveChangesAsync()

            _logger.LogInformation("Generated Form 2550Q: Year={Year} Q={Quarter} VatPayable={VatPayable}.", year, quarter, vatPayable)
            Return vatReturn
        End Function

        ''' <summary>
        ''' Generates Form 2551Q (quarterly percentage tax) for non-VAT businesses.
        ''' TaxDue = TotalGrossReceipts × <c>NonVatPercentageTaxRate</c> (default 3%).
        ''' </summary>
        Public Async Function GenerateNonVatPercentageTaxAsync(year As Integer, quarter As Integer) As Task(Of VatReturn) _
            Implements IVatReportingService.GenerateNonVatPercentageTaxAsync

            Dim vatConfig = Await _mediator.Send(New GetVatConfigurationQuery())

            If vatConfig.IsVatRegistered Then
                Throw New InvalidOperationException(
                    "This business is VAT-registered. Use GenerateMonthlyVatReturnAsync or GenerateQuarterlyVatReturnAsync instead.")
            End If

            Dim window = QuarterWindow(year, quarter)
            Dim data = Await CollectLedgerDataAsync(window.Start, window.End_)

            ' For Form 2551Q: TotalVatableSales repurposed as gross taxable receipts
            Dim grossReceipts = data.TotalVatableSales + data.TotalVatExemptSales + data.TotalZeroRatedSales
            Dim taxDue = Math.Round(grossReceipts * vatConfig.NonVatPercentageTaxRate, 2, MidpointRounding.ToEven)

            Dim nonVatData = New LedgerData With {
                .TotalVatableSales = grossReceipts,
                .TotalVatExemptSales = 0D,
                .TotalZeroRatedSales = 0D,
                .TotalOutputVat = 0D,
                .TotalVatablePurchases = 0D,
                .TotalInputVat = 0D,
                .RevenueRecords = data.RevenueRecords,
                .ExpenseRecords = data.ExpenseRecords
            }

            Await GuardAndClearExistingAsync(year, quarter, VatReturnPeriodType.Quarterly, VatReturnFormType.Form2551Q)

            Dim vatReturn = BuildVatReturn(
                year, quarter, VatReturnPeriodType.Quarterly, VatReturnFormType.Form2551Q,
                nonVatData, taxDue, vatConfig.IsVatRegistered)

            _db.VatReturns.Add(vatReturn)
            Await _db.SaveChangesAsync()

            _logger.LogInformation("Generated Form 2551Q: Year={Year} Q={Quarter} TaxDue={TaxDue}.", year, quarter, taxDue)
            Return vatReturn
        End Function

        ''' <summary>Returns a <see cref="VatReturn"/> by PK including its line detail.</summary>
        Public Async Function GetReturnAsync(returnId As Integer) As Task(Of VatReturn) _
            Implements IVatReportingService.GetReturnAsync

            Return Await _db.VatReturns.
                Include(Function(r) r.Lines).
                FirstOrDefaultAsync(Function(r) r.Id = returnId)
        End Function

        ''' <summary>Lists return headers for a given year, or all years if Nothing.</summary>
        Public Async Function ListReturnsAsync(year As Integer?) As Task(Of IReadOnlyList(Of VatReturn)) _
            Implements IVatReportingService.ListReturnsAsync

            Dim query = _db.VatReturns.AsQueryable()
            If year.HasValue Then
                query = query.Where(Function(r) r.Year = year.Value)
            End If
            Dim result = Await query.OrderByDescending(Function(r) r.Year).
                ThenByDescending(Function(r) r.Period).
                ToListAsync()
            Return result.AsReadOnly()
        End Function

        ''' <summary>
        ''' Transitions the return from <c>Generated</c> to <c>Filed</c>.
        ''' Sets <c>FiledAt</c> and <c>FiledBy</c>.
        ''' </summary>
        Public Async Function FileReturnAsync(returnId As Integer, filedBy As String) As Task _
            Implements IVatReportingService.FileReturnAsync

            Dim vatReturn = Await _db.VatReturns.FindAsync(returnId)
            If vatReturn Is Nothing Then
                Throw New InvalidOperationException($"VAT return #{returnId} not found.")
            End If

            If vatReturn.FilingStatus <> VatFilingStatus.Generated Then
                Throw New InvalidOperationException(
                    $"Only Generated returns may be filed. Current status: {vatReturn.FilingStatus}.")
            End If

            vatReturn.FilingStatus = VatFilingStatus.Filed
            vatReturn.FiledAt = DateTime.UtcNow
            vatReturn.FiledBy = filedBy

            Await _db.SaveChangesAsync()
            _logger.LogInformation("Filed VAT return #{ReturnId} by {FiledBy}.", returnId, filedBy)
        End Function

        ''' <summary>
        ''' Creates a new <see cref="VatReturn"/> with <c>FilingStatus = Amended</c> recomputed
        ''' from the current ledger state.  The original <c>Filed</c> row is untouched.
        ''' </summary>
        Public Async Function AmendReturnAsync(returnId As Integer) As Task(Of VatReturn) _
            Implements IVatReportingService.AmendReturnAsync

            Dim original = Await _db.VatReturns.FindAsync(returnId)
            If original Is Nothing Then
                Throw New InvalidOperationException($"VAT return #{returnId} not found.")
            End If

            If original.FilingStatus <> VatFilingStatus.Filed Then
                Throw New InvalidOperationException(
                    $"Only Filed returns may be amended. Current status: {original.FilingStatus}.")
            End If

            Dim vatConfig = Await _mediator.Send(New GetVatConfigurationQuery())
            Dim amended As VatReturn

            Select Case original.FormType
                Case VatReturnFormType.Form2550M
                    Dim windowStart = New DateTime(original.Year, original.Period, 1, 0, 0, 0, DateTimeKind.Utc)
                    Dim data = Await CollectLedgerDataAsync(windowStart, windowStart.AddMonths(1))
                    Dim vatPayable = Math.Round(data.TotalOutputVat - data.TotalInputVat, 2, MidpointRounding.ToEven)
                    amended = BuildVatReturn(original.Year, original.Period,
                                            VatReturnPeriodType.Monthly, VatReturnFormType.Form2550M,
                                            data, vatPayable, original.IsVatRegisteredSnapshot)

                Case VatReturnFormType.Form2550Q
                    Dim window = QuarterWindow(original.Year, original.Period)
                    Dim data = Await CollectLedgerDataAsync(window.Start, window.End_)
                    Dim vatPayable = Math.Round(data.TotalOutputVat - data.TotalInputVat, 2, MidpointRounding.ToEven)
                    amended = BuildVatReturn(original.Year, original.Period,
                                            VatReturnPeriodType.Quarterly, VatReturnFormType.Form2550Q,
                                            data, vatPayable, original.IsVatRegisteredSnapshot)

                Case Else ' Form2551Q
                    Dim window = QuarterWindow(original.Year, original.Period)
                    Dim data = Await CollectLedgerDataAsync(window.Start, window.End_)
                    Dim grossReceipts = data.TotalVatableSales + data.TotalVatExemptSales + data.TotalZeroRatedSales
                    Dim taxDue = Math.Round(grossReceipts * vatConfig.NonVatPercentageTaxRate, 2, MidpointRounding.ToEven)
                    Dim nonVatData = New LedgerData With {
                        .TotalVatableSales = grossReceipts,
                        .TotalVatExemptSales = 0D,
                        .TotalZeroRatedSales = 0D,
                        .TotalOutputVat = 0D,
                        .TotalVatablePurchases = 0D,
                        .TotalInputVat = 0D,
                        .RevenueRecords = data.RevenueRecords,
                        .ExpenseRecords = data.ExpenseRecords
                    }
                    amended = BuildVatReturn(original.Year, original.Period,
                                            VatReturnPeriodType.Quarterly, VatReturnFormType.Form2551Q,
                                            nonVatData, taxDue, original.IsVatRegisteredSnapshot)
            End Select

            amended.FilingStatus = VatFilingStatus.Amended

            _db.VatReturns.Add(amended)
            Await _db.SaveChangesAsync()

            _logger.LogInformation("Amended VAT return #{OriginalId} → new Amended return #{NewId}.", returnId, amended.Id)
            Return amended
        End Function

        ' ─── Private Helpers ────────────────────────────────────────────────────────

        Private Async Function CollectLedgerDataAsync(windowStart As DateTime, windowEnd As DateTime) As Task(Of LedgerData)
            Dim revenues = Await _db.RevenueRecords.
                Where(Function(r) r.RecordDate >= windowStart AndAlso r.RecordDate < windowEnd).
                ToListAsync()

            Dim expenses = Await _db.ExpenseRecords.
                Where(Function(e) e.RecordDate >= windowStart AndAlso e.RecordDate < windowEnd AndAlso
                                  e.SourceModule = "Purchasing").
                ToListAsync()

            Return New LedgerData With {
                .TotalVatableSales = revenues.Sum(Function(r) r.VatableAmount),
                .TotalVatExemptSales = revenues.Sum(Function(r) r.VatExemptAmount),
                .TotalZeroRatedSales = revenues.Sum(Function(r) r.ZeroRatedAmount),
                .TotalOutputVat = revenues.Sum(Function(r) r.OutputVat),
                .TotalVatablePurchases = expenses.Sum(Function(e) e.VatableAmount),
                .TotalInputVat = expenses.Sum(Function(e) e.InputVat),
                .RevenueRecords = revenues,
                .ExpenseRecords = expenses
            }
        End Function

        Private Async Function GuardAndClearExistingAsync(year As Integer, period As Integer,
                                                           periodType As VatReturnPeriodType,
                                                           formType As VatReturnFormType) As Task
            Dim existing = Await _db.VatReturns.
                Include(Function(r) r.Lines).
                FirstOrDefaultAsync(
                    Function(r) r.Year = year AndAlso r.Period = period AndAlso
                                r.PeriodType = periodType AndAlso r.FormType = formType AndAlso
                                r.FilingStatus <> VatFilingStatus.Amended)

            If existing IsNot Nothing Then
                If existing.FilingStatus = VatFilingStatus.Filed Then
                    Throw New VatReturnLockedException(existing.Id, year, period, formType)
                End If
                ' Status is Generated (or Draft) — delete and recreate
                _db.VatReturns.Remove(existing)
                Await _db.SaveChangesAsync()
            End If
        End Function

        Private Shared Function BuildVatReturn(year As Integer, period As Integer,
                                               periodType As VatReturnPeriodType,
                                               formType As VatReturnFormType,
                                               data As LedgerData,
                                               vatPayable As Decimal,
                                               isVatRegistered As Boolean) As VatReturn
            Dim lines As New List(Of VatReturnLine)()

            For Each rev In data.RevenueRecords
                lines.Add(New VatReturnLine With {
                    .SourceModule = "POS",
                    .SourceTable = "Acc_RevenueRecords",
                    .SourceRowId = rev.Id,
                    .TransactionDate = rev.RecordDate,
                    .VatableAmount = rev.VatableAmount,
                    .VatExemptAmount = rev.VatExemptAmount,
                    .ZeroRatedAmount = rev.ZeroRatedAmount,
                    .OutputVat = rev.OutputVat,
                    .InputVat = 0D,
                    .Treatment = rev.VatTreatment
                })
            Next

            For Each exp In data.ExpenseRecords
                lines.Add(New VatReturnLine With {
                    .SourceModule = "Purchasing",
                    .SourceTable = "Acc_ExpenseRecords",
                    .SourceRowId = exp.Id,
                    .TransactionDate = exp.RecordDate,
                    .VatableAmount = exp.VatableAmount,
                    .VatExemptAmount = exp.VatExemptAmount,
                    .ZeroRatedAmount = exp.ZeroRatedAmount,
                    .OutputVat = 0D,
                    .InputVat = exp.InputVat,
                    .Treatment = exp.VatTreatment
                })
            Next

            Return New VatReturn With {
                .Year = year,
                .Period = period,
                .PeriodType = periodType,
                .FormType = formType,
                .TotalVatableSales = data.TotalVatableSales,
                .TotalVatExemptSales = data.TotalVatExemptSales,
                .TotalZeroRatedSales = data.TotalZeroRatedSales,
                .TotalOutputVat = data.TotalOutputVat,
                .TotalVatablePurchases = data.TotalVatablePurchases,
                .TotalInputVat = data.TotalInputVat,
                .VatPayable = vatPayable,
                .FilingStatus = VatFilingStatus.Generated,
                .GeneratedAt = DateTime.UtcNow,
                .IsVatRegisteredSnapshot = isVatRegistered,
                .Lines = lines
            }
        End Function

        ''' <summary>Returns the UTC start and exclusive-end of the requested BIR fiscal quarter.</summary>
        Friend Shared Function QuarterWindow(year As Integer, quarter As Integer) As (Start As DateTime, End_ As DateTime)
            Dim startMonth = (quarter - 1) * 3 + 1
            Dim windowStart = New DateTime(year, startMonth, 1, 0, 0, 0, DateTimeKind.Utc)
            Return (windowStart, windowStart.AddMonths(3))
        End Function

        ''' <summary>Aggregated ledger sums for a single reporting window.</summary>
        Private Class LedgerData
            Public Property TotalVatableSales As Decimal
            Public Property TotalVatExemptSales As Decimal
            Public Property TotalZeroRatedSales As Decimal
            Public Property TotalOutputVat As Decimal
            Public Property TotalVatablePurchases As Decimal
            Public Property TotalInputVat As Decimal
            Public Property RevenueRecords As List(Of RevenueRecord)
            Public Property ExpenseRecords As List(Of ExpenseRecord)
        End Class

    End Class

End Namespace
