Namespace Entities

    ''' <summary>
    ''' Records a single expense posting originating from Purchasing, Inventory, or POS.
    ''' Categories include "COGS" (auto-posted from sales), "Shrinkage", and "Operating".
    ''' </summary>
    Public Class ExpenseRecord
        Inherits MerchSys.SharedKernel.Entities.AuditableEntity

        ''' <summary>Calendar date on which the expense was incurred.</summary>
        Public Property RecordDate As DateTime

        ''' <summary>Expense category: "COGS", "Shrinkage", or "Operating".</summary>
        Public Property Category As String

        ''' <summary>Human-readable description of the expense.</summary>
        Public Property Description As String

        ''' <summary>Monetary amount of the expense.</summary>
        Public Property Amount As Decimal

        ''' <summary>Module that originated this expense: "Purchasing", "Inventory", or "POS".</summary>
        Public Property SourceModule As String

        ''' <summary>Optional integer FK to the source record within the originating module; Nothing if not applicable.</summary>
        Public Property SourceReferenceId As Integer?

    End Class

End Namespace
