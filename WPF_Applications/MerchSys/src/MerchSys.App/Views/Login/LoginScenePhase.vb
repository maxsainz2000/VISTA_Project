Namespace Views.Login

    ''' <summary>
    ''' Lighting moods for the login storefront hero shot (UX-49).
    ''' The scene is deliberately art-directed at its most flattering hour (blue-hour dusk)
    ''' like a game title screen — daytime logins still see the hero look by design, so no
    ''' clock hour can render a weak palette. Real time of day survives as three moods of
    ''' the same composition.
    ''' </summary>
    Public Enum LoginScenePhase
        Dusk = 0
        Evening = 1
        LateNight = 2
    End Enum

    ''' <summary>
    ''' Pure mood lookup: local time of day → <see cref="LoginScenePhase"/>.
    ''' Bands (local): Dusk 05:00–18:59 (default hero) · Evening 19:00–22:59 ·
    ''' LateNight 23:00–04:59.
    ''' </summary>
    Public NotInheritable Class LoginScenePhaseProvider

        Private Sub New()
        End Sub

        Public Shared Function GetPhase(timeOfDay As TimeSpan) As LoginScenePhase
            Dim totalMinutes As Double = timeOfDay.TotalMinutes

            If totalMinutes >= 5 * 60 AndAlso totalMinutes < 19 * 60 Then
                Return LoginScenePhase.Dusk
            ElseIf totalMinutes >= 19 * 60 AndAlso totalMinutes < 23 * 60 Then
                Return LoginScenePhase.Evening
            Else
                Return LoginScenePhase.LateNight
            End If
        End Function

    End Class

End Namespace
