Imports CommunityToolkit.Mvvm.ComponentModel

Namespace Models

    ''' <summary>Top-level module identifiers used by the Activity Rail and Module Detail Panel.</summary>
    Public Enum AppModule
        Purchasing
        Inventory
        POS
        Accounting
        DeveloperTools
    End Enum

    ''' <summary>Display model for a single icon slot in the Activity Rail.</summary>
    Public Class RailItem
        Inherits ObservableObject

        Public Property ModuleId As AppModule
        Public Property Abbreviation As String   ' e.g. "PUR", "INV", "POS", "ACC"
        Public Property ToolTipText As String    ' e.g. "Purchasing (Ctrl+1)"

        Private _isActive As Boolean
        Public Property IsActive As Boolean
            Get
                Return _isActive
            End Get
            Set(value As Boolean)
                SetProperty(_isActive, value)
            End Set
        End Property

    End Class

End Namespace
