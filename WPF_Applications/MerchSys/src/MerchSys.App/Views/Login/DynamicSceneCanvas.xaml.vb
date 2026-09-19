Imports System.Windows.Media.Animation
Imports System.Windows.Threading

Namespace Views.Login

    ''' <summary>
    ''' The login backdrop (UX-50): a single painterly raster hero (<c>HeroImage</c>) with tasteful
    ''' motion, sampled by the frosted-glass card through <see cref="SceneVisual"/>.
    '''
    ''' The UX-48/49 procedural vector scene + 60s mood clock + ~40 ambient clocks were retired when
    ''' the hero became a finished illustration. What remains is a small, purposeful motion set:
    ''' a slow Ken-Burns zoom/pan and cursor parallax on the hero layer (the two warm window glows
    ''' ride the same transform, so they stay pinned to the painting), a gentle bulb-glow flicker,
    ''' a one-shot fade-in entrance, a success "welcome in" pulse on the window glow, and the sleep
    ''' dim overlay. The public lifecycle surface (<see cref="Start"/> / <see cref="Pause"/> /
    ''' <see cref="[Resume]"/> / <see cref="StopAll"/> / <see cref="PlayEntrance"/> /
    ''' <see cref="PulseDoorGlow"/> / <see cref="SetDimmed"/>) is unchanged so LoginView is untouched.
    ''' The host MUST call StopAll when it becomes invisible — LoginView instances are transient and
    ''' hidden, not closed, so a survivor would tick forever.
    ''' </summary>
    Partial Public Class DynamicSceneCanvas
        Inherits UserControl

        Private NotInheritable Class ClockEntry
            Public Target As IAnimatable
            Public Prop As DependencyProperty
            Public Clock As AnimationClock
        End Class

        Private Const AmbientFrameRate As Integer = 30
        Private Const FlickerFrameRate As Integer = 24
        Private Const ParallaxThrottleMs As Integer = 33
        Private Const ScaleBase As Double = 1.07
        Private Const ScaleZoom As Double = 1.11
        Private Const GlowWindowBase As Double = 0.18
        Private Const GlowBulbsLow As Double = 0.26
        Private Const GlowBulbsHigh As Double = 0.48
        ' Time-of-day mood = a deep-indigo grade overlay (LoginHeroGradeBrush) darkening the hero;
        ' the warm window/bulb glows sit ABOVE it in HeroLayer, so the storefront still pops.
        Private Const GradeDusk As Double = 0.0
        Private Const GradeEvening As Double = 0.32
        Private Const GradeLateNight As Double = 0.52
        Private Const MoodCrossfadeSeconds As Double = 3.5

        Private Enum MoodPhase
            Dusk        ' 05:00-18:59 — hero mood (blue hour), grade off
            Evening     ' 19:00-22:59 — full night, scene darkened/cooled
            LateNight   ' 23:00-04:59 — closing time, darkest
        End Enum

        Private ReadOnly _ambientClocks As New List(Of ClockEntry)
        Private ReadOnly _rng As New Random()

        Private _started As Boolean
        Private _paused As Boolean
        Private _staticMode As Boolean
        Private _hostWindow As Window
        Private _lastParallaxTick As Integer
        Private ReadOnly _phaseTimer As DispatcherTimer
        Private _currentMood As MoodPhase
        Private _gradeOpacity As Double

        ''' <summary>The scene visual sampled by the frosted-glass card. Never hand the card an
        ''' ancestor of itself — VisualBrush self-reference is illegal.</summary>
        Public ReadOnly Property SceneVisual As Visual
            Get
                Return SceneRoot
            End Get
        End Property

        Public Sub New()
            InitializeComponent()
            _phaseTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(60)}
            AddHandler _phaseTimer.Tick, AddressOf OnPhaseTimerTick
        End Sub

        ' ── Public lifecycle API (surface preserved for LoginView) ─────────────

        Public Sub Start(staticMode As Boolean)
            If _started Then Return
            _started = True
            _staticMode = staticMode
            GlowWindow.Opacity = GlowWindowBase
            GlowBulbs.Opacity = (GlowBulbsLow + GlowBulbsHigh) / 2.0
            _currentMood = CurrentMood()
            ApplyMood(_currentMood, animate:=False)
            If _staticMode Then Return
            StartAmbientLoops()
            HookParallax()
            _phaseTimer.Start()
        End Sub

        Public Sub Pause()
            If Not _started OrElse _staticMode OrElse _paused Then Return
            _paused = True
            PauseClocks(_ambientClocks)
        End Sub

        Public Sub [Resume]()
            If Not _started OrElse _staticMode OrElse Not _paused Then Return
            _paused = False
            ResumeClocks(_ambientClocks)
        End Sub

        ''' <summary>
        ''' Hard teardown: every clock detached, one-shots cleared, transforms reset.
        ''' Idempotent — called on hide, on close, and after the success beat.
        ''' </summary>
        Public Sub StopAll()
            _phaseTimer.Stop()
            StopClocks(_ambientClocks)
            GradeOverlay.BeginAnimation(OpacityProperty, Nothing)
            GradeOverlay.Opacity = GradeDusk
            _gradeOpacity = GradeDusk
            SceneRoot.BeginAnimation(OpacityProperty, Nothing)
            SceneRoot.Opacity = 1.0
            GlowWindow.BeginAnimation(OpacityProperty, Nothing)
            GlowWindow.Opacity = GlowWindowBase
            DimOverlay.BeginAnimation(OpacityProperty, Nothing)
            DimOverlay.Opacity = 0
            HeroScale.ScaleX = ScaleBase : HeroScale.ScaleY = ScaleBase
            HeroDriftTr.X = 0 : HeroDriftTr.Y = 0
            HeroParallaxTr.X = 0 : HeroParallaxTr.Y = 0
            UnhookParallax()
            _paused = False
            _started = False
        End Sub

        ''' <summary>Entrance: the scene fades up once (~640ms). One-shot; never in static mode.</summary>
        Public Sub PlayEntrance()
            If Not _started OrElse _staticMode Then Return
            SceneRoot.Opacity = 0.55
            Dim fade As New DoubleAnimation(1.0, New Duration(TimeSpan.FromMilliseconds(640))) With {
                .FillBehavior = FillBehavior.Stop,
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            SceneRoot.BeginAnimation(OpacityProperty, fade)
        End Sub

        ''' <summary>Success beat: the storefront window glow blooms once — "welcome in."</summary>
        Public Sub PulseDoorGlow()
            If Not _started OrElse _staticMode Then Return
            Dim pulse As New DoubleAnimation(Math.Min(1.0, GlowWindowBase + 0.26),
                                             New Duration(TimeSpan.FromMilliseconds(340))) With {
                .AutoReverse = True,
                .FillBehavior = FillBehavior.Stop,
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            GlowWindow.BeginAnimation(OpacityProperty, pulse)
        End Sub

        ''' <summary>Dims the scene slightly while the avatar sleeps.</summary>
        Public Sub SetDimmed(dimmed As Boolean)
            If _staticMode Then Return
            Dim dimAnim As New DoubleAnimation(If(dimmed, 0.1, 0.0), New Duration(TimeSpan.FromMilliseconds(600))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            DimOverlay.BeginAnimation(OpacityProperty, dimAnim)
        End Sub

        ' ── Time-of-day mood (deep-indigo grade overlay; glows ride above it) ──

        Private Shared Function CurrentMood() As MoodPhase
            Dim hour As Integer = DateTime.Now.Hour
#If DEBUG Then
            ' Verification affordance: force a mood without waiting for the clock.
            Dim overrideValue As String = Environment.GetEnvironmentVariable("VISTA_LOGIN_SCENE_HOUR")
            Dim overrideHour As Integer
            If Integer.TryParse(overrideValue, overrideHour) AndAlso overrideHour >= 0 AndAlso overrideHour <= 23 Then
                hour = overrideHour
            End If
#End If
            If hour >= 23 OrElse hour <= 4 Then Return MoodPhase.LateNight
            If hour >= 19 Then Return MoodPhase.Evening
            Return MoodPhase.Dusk
        End Function

        Private Shared Function GradeOpacityFor(phase As MoodPhase) As Double
            Select Case phase
                Case MoodPhase.Evening
                    Return GradeEvening
                Case MoodPhase.LateNight
                    Return GradeLateNight
                Case Else
                    Return GradeDusk
            End Select
        End Function

        ''' <summary>Applies a time-of-day grade by animating the overlay opacity (3.5s crossfade,
        ''' or instant on first show / in static mode). The warm glows are above the grade.</summary>
        Private Sub ApplyMood(phase As MoodPhase, animate As Boolean)
            Dim target As Double = GradeOpacityFor(phase)
            GradeOverlay.BeginAnimation(OpacityProperty, Nothing)
            If animate AndAlso Not _staticMode Then
                Dim fade As New DoubleAnimation(_gradeOpacity, target, New Duration(TimeSpan.FromSeconds(MoodCrossfadeSeconds))) With {
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                }
                GradeOverlay.BeginAnimation(OpacityProperty, fade)
            End If
            GradeOverlay.Opacity = target
            _gradeOpacity = target
        End Sub

        Private Sub OnPhaseTimerTick(sender As Object, e As EventArgs)
            Dim mood As MoodPhase = CurrentMood()
            If mood <> _currentMood Then
                _currentMood = mood
                ApplyMood(mood, animate:=True)
            End If
        End Sub

        ' ── Ambient motion (Ken-Burns zoom/pan + bulb flicker) ─────────────────

        Private Sub StartAmbientLoops()
            ' Ken-Burns zoom: the frame breathes very slowly between ScaleBase and ScaleZoom.
            StartClockTracked(HeroScale, ScaleTransform.ScaleXProperty, ZoomAnim(55), _ambientClocks)
            StartClockTracked(HeroScale, ScaleTransform.ScaleYProperty, ZoomAnim(55), _ambientClocks)

            ' Ken-Burns pan: gentle drift, offset periods from the zoom so motion never feels linear.
            StartClockTracked(HeroDriftTr, TranslateTransform.XProperty, PanAnim(-22, 70), _ambientClocks)
            StartClockTracked(HeroDriftTr, TranslateTransform.YProperty, PanAnim(-13, 61), _ambientClocks)

            ' Bulb-string flicker: the warm strip under the awning breathes gently.
            Dim flicker As New DoubleAnimation(GlowBulbsLow, GlowBulbsHigh, New Duration(TimeSpan.FromSeconds(3.4))) With {
                .AutoReverse = True,
                .RepeatBehavior = RepeatBehavior.Forever,
                .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 2.0),
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Timeline.SetDesiredFrameRate(flicker, FlickerFrameRate)
            StartClockTracked(GlowBulbs, OpacityProperty, flicker, _ambientClocks)
        End Sub

        Private Shared Function ZoomAnim(seconds As Double) As DoubleAnimation
            Dim a As New DoubleAnimation(ScaleBase, ScaleZoom, New Duration(TimeSpan.FromSeconds(seconds))) With {
                .AutoReverse = True,
                .RepeatBehavior = RepeatBehavior.Forever,
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Timeline.SetDesiredFrameRate(a, AmbientFrameRate)
            Return a
        End Function

        Private Shared Function PanAnim(toValue As Double, seconds As Double) As DoubleAnimation
            Dim a As New DoubleAnimation(0, toValue, New Duration(TimeSpan.FromSeconds(seconds))) With {
                .AutoReverse = True,
                .RepeatBehavior = RepeatBehavior.Forever,
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Timeline.SetDesiredFrameRate(a, AmbientFrameRate)
            Return a
        End Function

        ' ── Clock bookkeeping ──────────────────────────────────────────────────

        Private Shared Sub StartClockTracked(target As IAnimatable, targetProp As DependencyProperty, animation As AnimationTimeline, clockList As List(Of ClockEntry))
            Dim clock As AnimationClock = animation.CreateClock()
            target.ApplyAnimationClock(targetProp, clock, HandoffBehavior.SnapshotAndReplace)
            clockList.Add(New ClockEntry With {.Target = target, .Prop = targetProp, .Clock = clock})
        End Sub

        Private Shared Sub StopClocks(clockList As List(Of ClockEntry))
            For Each ce As ClockEntry In clockList
                ce.Clock.Controller?.Stop()
                ce.Target.ApplyAnimationClock(ce.Prop, Nothing)
            Next
            clockList.Clear()
        End Sub

        Private Shared Sub PauseClocks(clockList As List(Of ClockEntry))
            For Each ce As ClockEntry In clockList
                ce.Clock.Controller?.Pause()
            Next
        End Sub

        Private Shared Sub ResumeClocks(clockList As List(Of ClockEntry))
            For Each ce As ClockEntry In clockList
                ce.Clock.Controller?.Resume()
            Next
        End Sub

        ' ── Cursor parallax (hero layer shifts subtly against the cursor) ──────

        Private Sub HookParallax()
            _hostWindow = Window.GetWindow(Me)
            If _hostWindow IsNot Nothing Then
                AddHandler _hostWindow.PreviewMouseMove, AddressOf OnHostMouseMove
            End If
        End Sub

        Private Sub UnhookParallax()
            If _hostWindow IsNot Nothing Then
                RemoveHandler _hostWindow.PreviewMouseMove, AddressOf OnHostMouseMove
                _hostWindow = Nothing
            End If
        End Sub

        Private Sub OnHostMouseMove(sender As Object, e As MouseEventArgs)
            Dim tick As Integer = Environment.TickCount
            If tick - _lastParallaxTick < ParallaxThrottleMs Then Return
            _lastParallaxTick = tick

            Dim hostWidth As Double = _hostWindow.ActualWidth
            Dim hostHeight As Double = _hostWindow.ActualHeight
            If hostWidth <= 0 OrElse hostHeight <= 0 Then Return

            Dim cursorPos As Point = e.GetPosition(_hostWindow)
            Dim nx As Double = Math.Clamp((cursorPos.X / hostWidth - 0.5) * 2.0, -1.0, 1.0)
            Dim ny As Double = Math.Clamp((cursorPos.Y / hostHeight - 0.5) * 2.0, -1.0, 1.0)
            HeroParallaxTr.X = -6.0 * nx
            HeroParallaxTr.Y = -4.0 * ny
        End Sub

    End Class

End Namespace
