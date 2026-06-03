Imports MerchSys.SharedKernel.Interfaces
Imports Notification.Wpf

Namespace Services

    ''' <summary>
    ''' Concrete implementation of INotificationService that displays toast notifications via Notification.Wpf.
    ''' </summary>
    Public Class DefaultNotificationService
        Implements INotificationService

        Private ReadOnly _toastManager As NotificationManager

        Public Sub New()
            _toastManager = New NotificationManager()
        End Sub

        ''' <summary>Displays a success toast notification with the given message.</summary>
        Public Sub ShowSuccess(message As String) Implements INotificationService.ShowSuccess
            _toastManager.Show(New NotificationContent() With {
                .Title = "Success",
                .Message = message,
                .Type = NotificationType.Success
            })
        End Sub

        ''' <summary>Displays an error toast notification with the given message.</summary>
        Public Sub ShowError(message As String) Implements INotificationService.ShowError
            _toastManager.Show(New NotificationContent() With {
                .Title = "Error",
                .Message = message,
                .Type = NotificationType.Error
            })
        End Sub

        ''' <summary>Displays an informational toast notification with the given message.</summary>
        Public Sub ShowInfo(message As String) Implements INotificationService.ShowInfo
            _toastManager.Show(New NotificationContent() With {
                .Title = "Info",
                .Message = message,
                .Type = NotificationType.Information
            })
        End Sub

        ''' <summary>Displays a warning toast notification with the given message.</summary>
        Public Sub ShowWarning(message As String) Implements INotificationService.ShowWarning
            _toastManager.Show(New NotificationContent() With {
                .Title = "Warning",
                .Message = message,
                .Type = NotificationType.Warning
            })
        End Sub

    End Class

End Namespace
