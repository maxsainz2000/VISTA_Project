Imports System.Collections.Generic
Imports System.Threading
Imports System.Threading.Tasks
Imports MerchSys.Accounting.Entities

Namespace Services.Reporting

    ''' <summary>
    ''' Contract for exporting tamper audit log records to external formats (CSV / PDF)
    ''' for BIR auditor submission.
    ''' </summary>
    Public Interface ITamperReportExporter

        ''' <summary>
        ''' Writes the supplied incidents to <paramref name="targetPath"/> in the requested
        ''' format. Caller is responsible for choosing the path (typically via SaveFileDialog).
        ''' Throws <see cref="InvalidOperationException"/> if the target file already exists.
        ''' </summary>
        ''' <param name="incidents">The list of tamper incident audit entries to export.</param>
        ''' <param name="dateFrom">The start date of the filter range for reporting.</param>
        ''' <param name="dateTo">The end date of the filter range for reporting.</param>
        ''' <param name="format">The export format (CSV or PDF).</param>
        ''' <param name="targetPath">The full path where the export file should be saved.</param>
        ''' <param name="cancellationToken">The cancellation token.</param>
        ''' <exception cref="InvalidOperationException">Thrown if the target file already exists.</exception>
        Function ExportAsync(
            incidents As IReadOnlyList(Of TamperAuditEntry),
            dateFrom As DateTime,
            dateTo As DateTime,
            format As TamperReportFormat,
            targetPath As String,
            cancellationToken As CancellationToken
        ) As Task

    End Interface

End Namespace
