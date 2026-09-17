Imports System.Threading.Tasks

Namespace Interfaces

    ''' <summary>
    ''' Cross-cutting service for prompting the user when optimistic concurrency conflicts occur.
    ''' </summary>
    Public Interface IConflictPresenter

        ''' <summary>
        ''' Displays a concurrency conflict prompt and returns True if the user selected Refresh, or False if Cancel.
        ''' </summary>
        Function PromptAsync() As Task(Of Boolean)

    End Interface

End Namespace
