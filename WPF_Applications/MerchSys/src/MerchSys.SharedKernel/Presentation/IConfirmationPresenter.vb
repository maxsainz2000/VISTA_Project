Imports System.Threading.Tasks

Namespace Presentation

    ''' <summary>
    ''' Cross-cutting service for prompting the user with confirmation dialogs.
    ''' </summary>
    Public Interface IConfirmationPresenter

        ''' <summary>
        ''' Displays a confirmation prompt and returns True if the user selected Confirm, or False if Cancel.
        ''' </summary>
        Function PromptAsync(request As ConfirmationRequest) As Task(Of Boolean)

    End Interface

End Namespace
