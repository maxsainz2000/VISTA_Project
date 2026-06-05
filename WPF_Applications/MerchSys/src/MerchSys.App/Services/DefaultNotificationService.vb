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
        Public Sub ShowSuccess(message As String, Optional action As NotificationAction = Nothing) Implements INotificationService.ShowSuccess
            Dim content = New NotificationContent() With {
                .Title = "Success",
                .Message = message,
                .Type = NotificationType.Success
            }
            If action IsNot Nothing Then
                content.LeftButtonContent = action.Label
                content.LeftButtonAction = action.Callback
            End If
            _toastManager.Show(content)
        End Sub

        ''' <summary>Displays an error toast notification with the given message.</summary>
        Public Sub ShowError(message As String, Optional action As NotificationAction = Nothing) Implements INotificationService.ShowError
            Dim content = New NotificationContent() With {
                .Title = "Error",
                .Message = message,
                .Type = NotificationType.Error
            }
            If action IsNot Nothing Then
                content.LeftButtonContent = action.Label
                content.LeftButtonAction = action.Callback
            End If
            _toastManager.Show(content)
        End Sub

        ''' <summary>Displays an informational toast notification with the given message.</summary>
        Public Sub ShowInfo(message As String, Optional action As NotificationAction = Nothing) Implements INotificationService.ShowInfo
            Dim content = New NotificationContent() With {
                .Title = "Info",
                .Message = message,
                .Type = NotificationType.Information
            }
            If action IsNot Nothing Then
                content.LeftButtonContent = action.Label
                content.LeftButtonAction = action.Callback
            End If
            _toastManager.Show(content)
        End Sub

        ''' <summary>Displays a warning toast notification with the given message.</summary>
        Public Sub ShowWarning(message As String, Optional action As NotificationAction = Nothing) Implements INotificationService.ShowWarning
            Dim content = New NotificationContent() With {
                .Title = "Warning",
                .Message = message,
                .Type = NotificationType.Warning
            }
            If action IsNot Nothing Then
                content.LeftButtonContent = action.Label
                content.LeftButtonAction = action.Callback
            End If
            _toastManager.Show(content)
        End Sub

    End Class

End Namespace
