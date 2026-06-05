Imports System.Windows.Input

Namespace Interfaces

    ''' <summary>
    ''' Represents an active filter facet displayed as a chip in the FilterSummaryBar.
    ''' </summary>
    Public Class FilterChipItem
        Public Property DisplayText As String
        Public Property FilterKey As String
        Public Property RemoveCommand As ICommand

        Public Sub New(displayText As String, filterKey As String, removeCommand As ICommand)
            Me.DisplayText = displayText
            Me.FilterKey = filterKey
            Me.RemoveCommand = removeCommand
        End Sub
    End Class

End Namespace
