Namespace Presentation

    ''' <summary>
    ''' Data carrying object for a user confirmation dialog prompt.
    ''' </summary>
    Public Class ConfirmationRequest
        Public Property Title As String
        Public Property Message As String
        Public Property ConfirmButtonText As String
        Public Property IsDestructive As Boolean
        Public Property RequireTypedConfirmation As String

        Public Sub New(title As String, message As String, confirmButtonText As String, isDestructive As Boolean, Optional requireTypedConfirmation As String = Nothing)
            Me.Title = title
            Me.Message = message
            Me.ConfirmButtonText = confirmButtonText
            Me.IsDestructive = isDestructive
            Me.RequireTypedConfirmation = requireTypedConfirmation
        End Sub
    End Class

End Namespace
