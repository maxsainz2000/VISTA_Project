Namespace Services.Insights

    ''' <summary>
    ''' Pluggable insight contributor for the "What This Means" engine (ACC-06).
    ''' Each provider inspects the enriched <see cref="FinancialOverviewDto"/> and returns
    ''' a single actionable sentence (or Nothing if the provider has nothing to say for the current state).
    ''' Registered implementations are injected into <see cref="WhatThisMeansService"/> and appended
    ''' to the overview interpretation text without modifying ACC-06 consumers.
    ''' </summary>
    Public Interface IFinancialInsightProvider
        ''' <summary>
        ''' Produces an insight sentence given the supplied overview snapshot, or <c>Nothing</c> if
        ''' this provider has no relevant observation for the current data.
        ''' </summary>
        Function GenerateInsight(dto As FinancialOverviewDto) As String
    End Interface

End Namespace
