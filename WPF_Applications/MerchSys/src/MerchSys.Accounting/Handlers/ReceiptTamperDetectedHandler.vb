Imports System.Security.Principal
Imports System.Threading
Imports MediatR
Imports Microsoft.Extensions.Logging
Imports MerchSys.Accounting.Data
Imports MerchSys.Accounting.Entities
Imports MerchSys.SharedKernel.Events

Namespace Handlers

    ''' <summary>
    ''' Consumes <see cref="ReceiptTamperDetectedEvent"/> and persists a durable row in
    ''' <c>Acc_TamperAuditLog</c> (entity: <see cref="TamperAuditEntry"/>).
    '''
    ''' Sink-only role: this handler records the incident and does nothing else.
    ''' It does NOT publish any follow-on events, send notifications, or take remediation
    ''' action. Notification UI, manager email, and operational alerts are future scope.
    '''
    ''' If the database write fails, the failure is logged at Critical and the exception
    ''' is rethrown — a silent swallow would be a second-order tamper vulnerability.
    ''' </summary>
    Public Class ReceiptTamperDetectedHandler
        Implements INotificationHandler(Of ReceiptTamperDetectedEvent)

        Private ReadOnly _db As AccountingDbContext
        Private ReadOnly _logger As ILogger(Of ReceiptTamperDetectedHandler)

        Public Sub New(db As AccountingDbContext, logger As ILogger(Of ReceiptTamperDetectedHandler))
            _db = db
            _logger = logger
        End Sub

        Public Async Function Handle(
            notification As ReceiptTamperDetectedEvent,
            cancellationToken As CancellationToken
        ) As Task Implements INotificationHandler(Of ReceiptTamperDetectedEvent).Handle

    #Disable Warning CA1416
        Dim windowsUser = WindowsIdentity.GetCurrent()?.Name
#Enable Warning CA1416
            If String.IsNullOrEmpty(windowsUser) Then
                windowsUser = Environment.UserName
            End If

            Dim entry As New TamperAuditEntry With {
                .DetectedAt = notification.DetectedAt,
                .ReceiptId = notification.ReceiptId,
                .ReceiptNumber = notification.ReceiptNumber,
                .TamperKind = "HashMismatch",
                .DetectedByService = notification.DetectedBy,
                .ExpectedValue = notification.ExpectedHash,
                .ActualValue = notification.ActualHash,
                .AdditionalContextJson = Nothing,
                .MachineName = Environment.MachineName,
                .OperatingUser = windowsUser,
                .CreatedAt = DateTime.UtcNow,
                .CreatedBy = NameOf(ReceiptTamperDetectedHandler)
            }

            _logger.LogInformation(
                "Persisting tamper audit entry for receipt {ReceiptId} ({ReceiptNumber}), kind={TamperKind}.",
                entry.ReceiptId, entry.ReceiptNumber, entry.TamperKind)

            Dim saveEx As Exception = Nothing
            Try
                _db.TamperAuditEntries.Add(entry)
                Await _db.SaveChangesAsync(cancellationToken)
            Catch ex As Exception
                saveEx = ex
            End Try

            If saveEx IsNot Nothing Then
                _logger.LogCritical(
                    saveEx,
                    "CRITICAL: Failed to persist TamperAuditEntry for receipt {ReceiptId}. " &
                    "The tamper event was raised but the audit record was NOT saved.",
                    notification.ReceiptId)
                Throw saveEx
            End If

            _logger.LogInformation(
                "Tamper audit entry {AuditId} saved for receipt {ReceiptId}.",
                entry.Id, entry.ReceiptId)

        End Function

    End Class

End Namespace
