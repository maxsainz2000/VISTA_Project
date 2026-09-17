Imports MediatR

Namespace Events

    ''' <summary>
    ''' Published by POS (<c>VatConfigurationWriter</c>) whenever the VAT configuration row is
    ''' successfully updated and the cache invalidated.
    ''' Consumers can react to registration-status transitions — for example, an Accounting handler
    ''' might reset KPI labels or adjust default report periods when the business crosses the BIR
    ''' VAT-registration threshold (₱3M annual gross sales).
    ''' Key state-change properties: <see cref="IsVatRegistered"/> (new value),
    ''' <see cref="PreviousIsVatRegistered"/> (value before the save).
    ''' </summary>
    Public Class VatConfigurationChangedEvent
        Implements INotification

        ''' <summary>UTC timestamp when the configuration was saved to the database.</summary>
        Public Property OccurredAt As DateTime

        ''' <summary>VAT-registered status after the update.</summary>
        Public Property IsVatRegistered As Boolean

        ''' <summary>
        ''' VAT-registered status before the update.
        ''' Consumers can detect a registration-mode transition by comparing this with
        ''' <see cref="IsVatRegistered"/> (e.g., False → True means the business just crossed
        ''' the threshold and must now collect 12% output VAT).
        ''' </summary>
        Public Property PreviousIsVatRegistered As Boolean

    End Class

End Namespace
