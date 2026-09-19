Namespace Presentation

    ''' <summary>
    ''' Represents an optional action button attached to a toast notification.
    ''' </summary>
    Public Class NotificationAction

        ''' <summary>The text label shown on the action button.</summary>
        Public Property Label As String

        ''' <summary>The callback action triggered when the button is clicked.</summary>
        Public Property Callback As Action

        ''' <summary>
        ''' Initializes a new instance of the NotificationAction class.
        ''' </summary>
        ''' <param name="label">The button label.</param>
        ''' <param name="callback">The callback callback.</param>
        Public Sub New(label As String, callback As Action)
            Me.Label = label
            Me.Callback = callback
        End Sub

    End Class

End Namespace
