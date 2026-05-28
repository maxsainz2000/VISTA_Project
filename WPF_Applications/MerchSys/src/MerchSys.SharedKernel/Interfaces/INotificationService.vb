Namespace Interfaces

    ''' <summary>
    ''' Cross-cutting service for surfacing user-facing toast notifications to the UI shell.
    ''' </summary>
    Public Interface INotificationService

        ''' <summary>Displays a success toast notification with the given message.</summary>
        Sub ShowSuccess(message As String)

        ''' <summary>Displays an error toast notification with the given message.</summary>
        Sub ShowError(message As String)

    End Interface

End Namespace
