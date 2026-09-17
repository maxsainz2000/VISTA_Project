Imports MediatR

Namespace Events

    ''' <summary>
    ''' Published by Inventory when stock loss is recorded (damage, spoilage, expiry, or admin correction).
    ''' Consumed by Accounting to record the inventory write-off as an expense.
    ''' Fires after the stock controller saves a shrinkage entry in the Inventory module.
    ''' </summary>
    Public Class ShrinkageRecordedEvent
        Implements INotification

        ''' <summary>Product that suffered the loss.</summary>
        Public Property ProductId As Integer

        ''' <summary>Human-readable product name, denormalized for Accounting consumer.</summary>
        Public Property ProductName As String

        ''' <summary>Number of units lost.</summary>
        Public Property QuantityLost As Integer

        ''' <summary>FIFO batch cost per unit of the lost stock.</summary>
        Public Property UnitCost As Decimal

        ''' <summary>Total write-off value: QuantityLost × UnitCost.</summary>
        Public Property TotalValue As Decimal

        ''' <summary>Category of loss. Expected values: "Damage", "Spoilage", "Expiry", "Admin Error".</summary>
        Public Property Reason As String

        ''' <summary>UTC date/time the shrinkage was recorded.</summary>
        Public Property RecordedDate As DateTime

    End Class

End Namespace
