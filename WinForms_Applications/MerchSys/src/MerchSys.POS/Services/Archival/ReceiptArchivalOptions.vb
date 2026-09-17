Namespace Services.Archival

    ''' <summary>
    ''' Configuration knobs for the receipt archival background service.
    ''' Bound from the <c>Receipts:Archival</c> section in <c>appsettings.json</c>.
    ''' <para>
    ''' <b>IntervalHours</b> — how often the archival timer fires (default: 24 h).<br/>
    ''' <b>BatchSize</b> — maximum receipts moved per run; prevents long-running transactions
    ''' (default: 500).<br/>
    ''' <b>Enabled</b> — master switch; <c>False</c> prevents the hosted timer from starting
    ''' and short-circuits any manual invocation of
    ''' <see cref="IReceiptArchivalService.ArchiveEligibleAsync"/> (default: <c>True</c>).
    ''' </para>
    ''' </summary>
    Public Class ReceiptArchivalOptions

        ''' <summary>Hours between archival timer ticks. Default: 24.</summary>
        Public Property IntervalHours As Integer = 24

        ''' <summary>Maximum rows moved per batch. Default: 500.</summary>
        Public Property BatchSize As Integer = 500

        ''' <summary>
        ''' Master enable switch. When <c>False</c> the hosted timer never starts and
        ''' <see cref="IReceiptArchivalService.ArchiveEligibleAsync"/> returns immediately
        ''' with zero counts.
        ''' </summary>
        Public Property Enabled As Boolean = True

    End Class

End Namespace
