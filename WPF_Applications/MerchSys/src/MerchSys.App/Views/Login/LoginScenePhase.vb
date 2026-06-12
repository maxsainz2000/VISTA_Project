Namespace Views.Login

    ''' <summary>
    ''' Time-of-day phases for the animated login panorama (UX-48).
    ''' The Philippines is near-equatorial, so fixed local-time bands are honest —
    ''' no solar-position math by design.
    ''' </summary>
    Public Enum LoginScenePhase
        Dawn = 0
        Day = 1
        Golden = 2
        Dusk = 3
        Night = 4
    End Enum

    ''' <summary>
    ''' Pure phase lookup: local time of day → <see cref="LoginScenePhase"/>.
    ''' Bands (local): Dawn 05:00–06:29 · Day 06:30–16:29 · Golden 16:30–17:59 ·
    ''' Dusk 18:00–19:29 · Night 19:30–04:59.
    ''' </summary>
    Public NotInheritable Class LoginScenePhaseProvider

        Private Sub New()
        End Sub

        Public Shared Function GetPhase(timeOfDay As TimeSpan) As LoginScenePhase
            Dim totalMinutes As Double = timeOfDay.TotalMinutes

            If totalMinutes >= 5 * 60 AndAlso totalMinutes < 6 * 60 + 30 Then
                Return LoginScenePhase.Dawn
            ElseIf totalMinutes >= 6 * 60 + 30 AndAlso totalMinutes < 16 * 60 + 30 Then
                Return LoginScenePhase.Day
            ElseIf totalMinutes >= 16 * 60 + 30 AndAlso totalMinutes < 18 * 60 Then
                Return LoginScenePhase.Golden
            ElseIf totalMinutes >= 18 * 60 AndAlso totalMinutes < 19 * 60 + 30 Then
                Return LoginScenePhase.Dusk
            Else
                Return LoginScenePhase.Night
            End If
        End Function

    End Class

End Namespace
