Namespace Services

    ''' <summary>
    ''' Abstraction for a pluggable KPI contributor that can be injected into the Financial Overview
    ''' pipeline without touching ACC-03.  Each provider is identified by a unique <see cref="KpiKey"/>
    ''' and returns a single <see cref="KpiValue"/> snapshot for a given reference date.
    ''' Future providers (cash-on-hand, AR aging, etc.) register the same interface without changing
    ''' the core overview service.
    ''' </summary>
    Public Interface IKpiProvider
        ''' <summary>Stable identifier used by the decorator to map results to DTO properties.</summary>
        ReadOnly Property KpiKey As String
        ''' <summary>Computes the KPI value as of <paramref name="asOfDate"/> (UTC).</summary>
        Function ProvideAsync(asOfDate As DateTime) As Task(Of KpiValue)
    End Interface

    ''' <summary>KPI snapshot returned by an <see cref="IKpiProvider"/>.</summary>
    Public Class KpiValue
        Public Property Key As String
        Public Property DisplayLabel As String
        Public Property Amount As Decimal
        ''' <summary>Optional secondary line, e.g. "Due Jul 25". May be Nothing.</summary>
        Public Property SecondaryText As String
        ''' <summary>Typed due date used by the decorator; derived from the same deadline as <see cref="SecondaryText"/>.</summary>
        Public Property DueDate As DateTime?
        Public Property Severity As KpiSeverity
    End Class

    ''' <summary>Visual urgency level for a KPI tile.</summary>
    Public Enum KpiSeverity
        Info = 0
        ''' <summary>Approaching deadline or overdue by fewer than 7 days.</summary>
        Warning = 1
        ''' <summary>Overdue, within 3 days, or a large negative balance.</summary>
        Critical = 2
    End Enum

End Namespace
