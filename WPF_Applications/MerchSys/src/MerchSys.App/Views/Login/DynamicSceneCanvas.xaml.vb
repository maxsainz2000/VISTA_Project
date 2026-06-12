Imports System.Windows.Media.Animation
Imports System.Windows.Threading

Namespace Views.Login

    ''' <summary>
    ''' The animated time-of-day farm panorama behind the login card (UX-48).
    ''' Owns the 60s phase clock, the 4s palette crossfade, all ambient animation clocks,
    ''' and the cursor parallax. Lifecycle: <see cref="Start"/> / <see cref="Pause"/> /
    ''' <see cref="[Resume]"/> / <see cref="StopAll"/> — the host view MUST call StopAll when
    ''' it becomes invisible (LoginView instances are transient and are hidden, not closed).
    '''
    ''' All animated fills are local unfrozen brushes built in code (never resource brushes —
    ''' resource Freezables may be shared/frozen and cannot be animated safely).
    ''' Every animated property is a transform, a color on a local brush, or an opacity.
    ''' </summary>
    Partial Public Class DynamicSceneCanvas
        Inherits UserControl

        Private NotInheritable Class ClockEntry
            Public Target As IAnimatable
            Public Prop As DependencyProperty
            Public Clock As AnimationClock
        End Class

        ' Phase-independent timing/extent constants
        Private Const CrossfadeSeconds As Double = 4.0
        Private Const AmbientFrameRate As Integer = 30
        Private Const ParallaxThrottleMs As Integer = 33

        Private ReadOnly _ambientClocks As New List(Of ClockEntry)
        Private ReadOnly _fireflyClocks As New List(Of ClockEntry)
        Private ReadOnly _birdClocks As New List(Of ClockEntry)
        Private ReadOnly _crossfadeClocks As New List(Of ClockEntry)

        Private ReadOnly _phaseTimer As DispatcherTimer
        Private ReadOnly _rng As New Random()

        Private _currentPhase As LoginScenePhase
        Private _started As Boolean
        Private _paused As Boolean
        Private _staticMode As Boolean
        Private _firefliesRunning As Boolean
        Private _birdsRunning As Boolean

        Private _hostWindow As Window
        Private _lastParallaxTick As Integer

        ' Animated brushes — created unfrozen in code, assigned to scene shapes once.
        Private _skyBrush As LinearGradientBrush
        Private _mountainBrush As SolidColorBrush
        Private _fieldFarBrush As SolidColorBrush
        Private _fieldNearBrush As SolidColorBrush
        Private _foliageBrush As SolidColorBrush
        Private _roadBrush As SolidColorBrush
        Private _storeBrush As SolidColorBrush
        Private _cloudBrush As SolidColorBrush
        Private _signPlateBrush As SolidColorBrush

        Private Structure ScenePalette
            Public SkyTop As Color
            Public SkyMid As Color
            Public SkyHorizon As Color
            Public Mountain As Color
            Public FieldFar As Color
            Public FieldNear As Color
            Public Foliage As Color
            Public Road As Color
            Public Store As Color
            Public Cloud As Color
            Public SignPlate As Color
            Public CloudOpacity As Double
            Public StarOpacity As Double
            Public MoonOpacity As Double
            Public SunOpacity As Double
            Public HaloOpacity As Double
            Public SunX As Double
            Public SunY As Double
            Public FirefliesOn As Boolean
            Public BirdsOn As Boolean
        End Structure

        Public Sub New()
            InitializeComponent()
            BuildAnimatedBrushes()
            _phaseTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(60)}
            AddHandler _phaseTimer.Tick, AddressOf OnPhaseTimerTick
        End Sub

        ' ── Public lifecycle API ───────────────────────────────────────────────

        Public Sub Start(staticMode As Boolean)
            If _started Then Return
            _started = True
            _staticMode = staticMode
            _currentPhase = LoginScenePhaseProvider.GetPhase(CurrentTimeOfDay())
            ApplyPhase(_currentPhase, animate:=False)
            If _staticMode Then Return

            StartAmbientLoops()
            HookParallax()
            _phaseTimer.Start()
        End Sub

        ''' <summary>Pauses ambient loops (window deactivated). Phase crossfades are short-lived and unaffected.</summary>
        Public Sub Pause()
            If Not _started OrElse _staticMode OrElse _paused Then Return
            _paused = True
            PauseClocks(_ambientClocks)
            PauseClocks(_fireflyClocks)
            PauseClocks(_birdClocks)
        End Sub

        Public Sub [Resume]()
            If Not _started OrElse _staticMode OrElse Not _paused Then Return
            _paused = False
            ResumeClocks(_ambientClocks)
            ResumeClocks(_fireflyClocks)
            ResumeClocks(_birdClocks)
        End Sub

        ''' <summary>
        ''' Hard teardown: every clock detached, the phase timer stopped, parallax unhooked.
        ''' Idempotent — called on hide, on close, and after the success beat.
        ''' </summary>
        Public Sub StopAll()
            _phaseTimer.Stop()
            StopClocks(_ambientClocks)
            StopClocks(_fireflyClocks)
            StopClocks(_birdClocks)
            StopClocks(_crossfadeClocks)
            _firefliesRunning = False
            _birdsRunning = False
            DimOverlay.BeginAnimation(OpacityProperty, Nothing)
            DimOverlay.Opacity = 0
            UnhookParallax()
            CloudsParallaxTr.X = 0 : CloudsParallaxTr.Y = 0
            MountainParallaxTr.X = 0 : MountainParallaxTr.Y = 0
            NearParallaxTr.X = 0 : NearParallaxTr.Y = 0
            _paused = False
            _started = False
        End Sub

        ''' <summary>Dims the scene slightly while the mascot sleeps.</summary>
        Public Sub SetDimmed(dimmed As Boolean)
            If _staticMode Then Return
            Dim dimAnim As New DoubleAnimation(If(dimmed, 0.1, 0.0), New Duration(TimeSpan.FromMilliseconds(600))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            DimOverlay.BeginAnimation(OpacityProperty, dimAnim)
        End Sub

        ' ── Brushes & palette ──────────────────────────────────────────────────

        Private Sub BuildAnimatedBrushes()
            Dim dayPalette As ScenePalette = GetPalette(LoginScenePhase.Day)

            _skyBrush = New LinearGradientBrush With {
                .StartPoint = New Point(0.5, 0),
                .EndPoint = New Point(0.5, 1)
            }
            _skyBrush.GradientStops.Add(New GradientStop(dayPalette.SkyTop, 0.0))
            _skyBrush.GradientStops.Add(New GradientStop(dayPalette.SkyMid, 0.55))
            _skyBrush.GradientStops.Add(New GradientStop(dayPalette.SkyHorizon, 1.0))
            SkyRect.Fill = _skyBrush

            _mountainBrush = New SolidColorBrush(dayPalette.Mountain)
            MountainPath.Fill = _mountainBrush

            _fieldFarBrush = New SolidColorBrush(dayPalette.FieldFar)
            FieldFarPath.Fill = _fieldFarBrush

            _fieldNearBrush = New SolidColorBrush(dayPalette.FieldNear)
            FieldNearPath.Fill = _fieldNearBrush

            _roadBrush = New SolidColorBrush(dayPalette.Road)
            RoadPath.Fill = _roadBrush

            _storeBrush = New SolidColorBrush(dayPalette.Store)
            StorePath.Fill = _storeBrush

            _cloudBrush = New SolidColorBrush(dayPalette.Cloud)
            Cloud1.Fill = _cloudBrush
            Cloud2.Fill = _cloudBrush
            Cloud3.Fill = _cloudBrush

            _signPlateBrush = New SolidColorBrush(dayPalette.SignPlate)
            SignPlate.Fill = _signPlateBrush
            WindowGlow1.Fill = _signPlateBrush
            WindowGlow2.Fill = _signPlateBrush
            WindowGlow3.Fill = _signPlateBrush

            _foliageBrush = New SolidColorBrush(dayPalette.Foliage)
            For Each nearChild As UIElement In NearGroup.Children
                Dim palayPath = TryCast(nearChild, Path)
                If palayPath IsNot Nothing AndAlso "Palay".Equals(TryCast(palayPath.Tag, String), StringComparison.Ordinal) Then
                    palayPath.Stroke = _foliageBrush
                End If
            Next
        End Sub

        Private Function GetPalette(phase As LoginScenePhase) As ScenePalette
            Dim prefix As String = "Scene" & phase.ToString()
            Dim p As New ScenePalette With {
                .SkyTop = Col(prefix & "SkyTopColor"),
                .SkyMid = Col(prefix & "SkyMidColor"),
                .SkyHorizon = Col(prefix & "SkyHorizonColor"),
                .Mountain = Col(prefix & "MountainColor"),
                .FieldFar = Col(prefix & "FieldFarColor"),
                .FieldNear = Col(prefix & "FieldNearColor"),
                .Foliage = Col(prefix & "FoliageColor"),
                .Road = Col(prefix & "RoadColor"),
                .Store = Col(prefix & "StoreColor"),
                .Cloud = Col(prefix & "CloudColor"),
                .SignPlate = Col(prefix & "SignPlateColor")
            }

            Select Case phase
                Case LoginScenePhase.Dawn
                    p.CloudOpacity = 0.7 : p.StarOpacity = 0 : p.MoonOpacity = 0
                    p.SunOpacity = 0.95 : p.HaloOpacity = 0
                    p.SunX = 300 : p.SunY = 610
                    p.FirefliesOn = False : p.BirdsOn = False
                Case LoginScenePhase.Day
                    p.CloudOpacity = 0.95 : p.StarOpacity = 0 : p.MoonOpacity = 0
                    p.SunOpacity = 1 : p.HaloOpacity = 0
                    p.SunX = 820 : p.SunY = 180
                    p.FirefliesOn = False : p.BirdsOn = True
                Case LoginScenePhase.Golden
                    p.CloudOpacity = 0.8 : p.StarOpacity = 0 : p.MoonOpacity = 0
                    p.SunOpacity = 0.95 : p.HaloOpacity = 0
                    p.SunX = 1270 : p.SunY = 540
                    p.FirefliesOn = False : p.BirdsOn = True
                Case LoginScenePhase.Dusk
                    p.CloudOpacity = 0.5 : p.StarOpacity = 0.5 : p.MoonOpacity = 0.35
                    p.SunOpacity = 0 : p.HaloOpacity = 0.6
                    p.SunX = 1270 : p.SunY = 560
                    p.FirefliesOn = True : p.BirdsOn = False
                Case Else ' Night
                    p.CloudOpacity = 0.3 : p.StarOpacity = 1 : p.MoonOpacity = 1
                    p.SunOpacity = 0 : p.HaloOpacity = 0.85
                    p.SunX = 1270 : p.SunY = 560
                    p.FirefliesOn = True : p.BirdsOn = False
            End Select

            Return p
        End Function

        Private Function Col(resourceKey As String) As Color
            Return CType(FindResource(resourceKey), Color)
        End Function

        ' ── Phase application & crossfade ──────────────────────────────────────

        Private Sub OnPhaseTimerTick(sender As Object, e As EventArgs)
            Dim phaseNow As LoginScenePhase = LoginScenePhaseProvider.GetPhase(CurrentTimeOfDay())
            If phaseNow <> _currentPhase Then
                _currentPhase = phaseNow
                ApplyPhase(phaseNow, animate:=True)
            End If
        End Sub

        Private Sub ApplyPhase(phase As LoginScenePhase, animate As Boolean)
            Dim p As ScenePalette = GetPalette(phase)

            ' Any in-flight crossfade is replaced wholesale.
            StopClocks(_crossfadeClocks)

            If animate AndAlso Not _staticMode Then
                CrossfadeColor(_skyBrush.GradientStops(0), p.SkyTop)
                CrossfadeColor(_skyBrush.GradientStops(1), p.SkyMid)
                CrossfadeColor(_skyBrush.GradientStops(2), p.SkyHorizon)
                CrossfadeColor(_mountainBrush, p.Mountain)
                CrossfadeColor(_fieldFarBrush, p.FieldFar)
                CrossfadeColor(_fieldNearBrush, p.FieldNear)
                CrossfadeColor(_foliageBrush, p.Foliage)
                CrossfadeColor(_roadBrush, p.Road)
                CrossfadeColor(_storeBrush, p.Store)
                CrossfadeColor(_cloudBrush, p.Cloud)
                CrossfadeColor(_signPlateBrush, p.SignPlate)

                CrossfadeDouble(CloudsLayer, OpacityProperty, p.CloudOpacity)
                CrossfadeDouble(StarsLayer, OpacityProperty, p.StarOpacity)
                CrossfadeDouble(MoonGroup, OpacityProperty, p.MoonOpacity)
                CrossfadeDouble(SunGroup, OpacityProperty, p.SunOpacity)
                CrossfadeDouble(SignHalo, OpacityProperty, p.HaloOpacity)
                CrossfadeDouble(FirefliesLayer, OpacityProperty, If(p.FirefliesOn, 1.0, 0.0))
                CrossfadeDouble(BirdsLayer, OpacityProperty, If(p.BirdsOn, 1.0, 0.0))
                CrossfadeDouble(SunTranslate, TranslateTransform.XProperty, p.SunX)
                CrossfadeDouble(SunTranslate, TranslateTransform.YProperty, p.SunY)
            Else
                _skyBrush.GradientStops(0).Color = p.SkyTop
                _skyBrush.GradientStops(1).Color = p.SkyMid
                _skyBrush.GradientStops(2).Color = p.SkyHorizon
                _mountainBrush.Color = p.Mountain
                _fieldFarBrush.Color = p.FieldFar
                _fieldNearBrush.Color = p.FieldNear
                _foliageBrush.Color = p.Foliage
                _roadBrush.Color = p.Road
                _storeBrush.Color = p.Store
                _cloudBrush.Color = p.Cloud
                _signPlateBrush.Color = p.SignPlate

                CloudsLayer.Opacity = p.CloudOpacity
                StarsLayer.Opacity = p.StarOpacity
                MoonGroup.Opacity = p.MoonOpacity
                SunGroup.Opacity = p.SunOpacity
                SignHalo.Opacity = p.HaloOpacity
                FirefliesLayer.Opacity = If(p.FirefliesOn, 1.0, 0.0)
                BirdsLayer.Opacity = If(p.BirdsOn, 1.0, 0.0)
                SunTranslate.X = p.SunX
                SunTranslate.Y = p.SunY
            End If

            ' Critter loops only run while their phase needs them (and never in static mode).
            If _staticMode Then Return
            If p.FirefliesOn AndAlso Not _firefliesRunning Then StartFireflies()
            If Not p.FirefliesOn AndAlso _firefliesRunning Then
                StopClocks(_fireflyClocks)
                _firefliesRunning = False
            End If
            If p.BirdsOn AndAlso Not _birdsRunning Then StartBirds()
            If Not p.BirdsOn AndAlso _birdsRunning Then
                StopClocks(_birdClocks)
                _birdsRunning = False
            End If
        End Sub

        Private Sub CrossfadeColor(target As IAnimatable, toColor As Color)
            Dim colorAnim As New ColorAnimation(toColor, New Duration(TimeSpan.FromSeconds(CrossfadeSeconds))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Dim targetProp As DependencyProperty
            If TypeOf target Is GradientStop Then
                targetProp = GradientStop.ColorProperty
            Else
                targetProp = SolidColorBrush.ColorProperty
            End If
            StartClockTracked(target, targetProp, colorAnim, _crossfadeClocks)
        End Sub

        Private Sub CrossfadeDouble(target As IAnimatable, targetProp As DependencyProperty, toValue As Double)
            Dim dblAnim As New DoubleAnimation(toValue, New Duration(TimeSpan.FromSeconds(CrossfadeSeconds))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            StartClockTracked(target, targetProp, dblAnim, _crossfadeClocks)
        End Sub

        ' ── Ambient loops ──────────────────────────────────────────────────────

        Private Sub StartAmbientLoops()
            ' Cloud drift: enter stage-left, exit stage-right, seamless wrap offscreen.
            Dim cloudDurations() As Double = {240, 180, 150}
            Dim cloudOffsets() As Double = {-60, -120, -30}
            Dim cloudPaths() As Path = {Cloud1, Cloud2, Cloud3}
            For i As Integer = 0 To 2
                Dim driftAnim As New DoubleAnimation(-400, 1950, New Duration(TimeSpan.FromSeconds(cloudDurations(i)))) With {
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(cloudOffsets(i))
                }
                Timeline.SetDesiredFrameRate(driftAnim, AmbientFrameRate)
                StartClockTracked(DriftTransformOf(cloudPaths(i)), TranslateTransform.XProperty, driftAnim, _ambientClocks)
            Next

            ' Palay sway: ±1.8°, staggered.
            Dim swayTransforms() As RotateTransform = {Sway1, Sway2, Sway3, Sway4, Sway5, Sway6}
            For Each swayTr As RotateTransform In swayTransforms
                Dim swayAnim As New DoubleAnimation(-1.8, 1.8, New Duration(TimeSpan.FromSeconds(4 + _rng.NextDouble() * 2))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 4),
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                }
                Timeline.SetDesiredFrameRate(swayAnim, AmbientFrameRate)
                StartClockTracked(swayTr, RotateTransform.AngleProperty, swayAnim, _ambientClocks)
            Next

            ' Star twinkle: every second star, capped at 8.
            Dim twinkleCount As Integer = 0
            For starIndex As Integer = 0 To StarsLayer.Children.Count - 1 Step 2
                If twinkleCount >= 8 Then Exit For
                Dim star As UIElement = StarsLayer.Children(starIndex)
                Dim twinkleAnim As New DoubleAnimation(0.35, 0.95, New Duration(TimeSpan.FromSeconds(2.2 + _rng.NextDouble() * 1.6))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 3)
                }
                Timeline.SetDesiredFrameRate(twinkleAnim, AmbientFrameRate)
                StartClockTracked(star, OpacityProperty, twinkleAnim, _ambientClocks)
                twinkleCount += 1
            Next
        End Sub

        Private Sub StartFireflies()
            _firefliesRunning = True
            For Each fireflyElement As UIElement In FirefliesLayer.Children
                Dim firefly = TryCast(fireflyElement, Ellipse)
                If firefly Is Nothing Then Continue For
                Dim floatTr = CType(firefly.RenderTransform, TranslateTransform)

                Dim pulseAnim As New DoubleAnimation(0.05, 0.9, New Duration(TimeSpan.FromSeconds(2.6 + _rng.NextDouble() * 1.9))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 4)
                }
                Timeline.SetDesiredFrameRate(pulseAnim, AmbientFrameRate)
                StartClockTracked(firefly, OpacityProperty, pulseAnim, _fireflyClocks)

                Dim driftXAnim As New DoubleAnimation(-14, 14, New Duration(TimeSpan.FromSeconds(7 + _rng.NextDouble() * 4))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 6),
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                }
                Timeline.SetDesiredFrameRate(driftXAnim, AmbientFrameRate)
                StartClockTracked(floatTr, TranslateTransform.XProperty, driftXAnim, _fireflyClocks)

                Dim driftYAnim As New DoubleAnimation(-8, 8, New Duration(TimeSpan.FromSeconds(5 + _rng.NextDouble() * 3))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 5),
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                }
                Timeline.SetDesiredFrameRate(driftYAnim, AmbientFrameRate)
                StartClockTracked(floatTr, TranslateTransform.YProperty, driftYAnim, _fireflyClocks)
            Next
        End Sub

        Private Sub StartBirds()
            _birdsRunning = True
            Dim flightDurations() As Double = {19, 24}
            Dim birdIndex As Integer = 0
            For Each birdElement As UIElement In BirdsLayer.Children
                Dim birdCanvas = TryCast(birdElement, Canvas)
                If birdCanvas Is Nothing Then Continue For
                Dim flightTr = CType(birdCanvas.RenderTransform, TranslateTransform)
                Dim wingPath = CType(birdCanvas.Children(0), Path)
                Dim wingScale = CType(wingPath.RenderTransform, ScaleTransform)

                Dim flightAnim As New DoubleAnimation(-160, 1760, New Duration(TimeSpan.FromSeconds(flightDurations(birdIndex Mod 2)))) With {
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(birdIndex * 9)
                }
                Timeline.SetDesiredFrameRate(flightAnim, AmbientFrameRate)
                StartClockTracked(flightTr, TranslateTransform.XProperty, flightAnim, _birdClocks)

                Dim bobAnim As New DoubleAnimation(-10, 10, New Duration(TimeSpan.FromSeconds(2.8))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 2),
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                }
                Timeline.SetDesiredFrameRate(bobAnim, AmbientFrameRate)
                StartClockTracked(flightTr, TranslateTransform.YProperty, bobAnim, _birdClocks)

                Dim flapAnim As New DoubleAnimation(1.0, 0.55, New Duration(TimeSpan.FromSeconds(0.32))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever
                }
                StartClockTracked(wingScale, ScaleTransform.ScaleYProperty, flapAnim, _birdClocks)

                birdIndex += 1
            Next
        End Sub

        Private Shared Function DriftTransformOf(cloudPath As Path) As TranslateTransform
            Dim group = CType(cloudPath.RenderTransform, TransformGroup)
            Return CType(group.Children(1), TranslateTransform)
        End Function

        ' ── Clock bookkeeping ──────────────────────────────────────────────────

        Private Shared Sub StartClockTracked(target As IAnimatable, targetProp As DependencyProperty, timeline As AnimationTimeline, clockList As List(Of ClockEntry))
            Dim clock As AnimationClock = timeline.CreateClock()
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

        ' ── Parallax ───────────────────────────────────────────────────────────

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

            ' Camera-pan illusion: nearer layers shift more, all against the cursor.
            CloudsParallaxTr.X = -4 * nx : CloudsParallaxTr.Y = -1.6 * ny
            MountainParallaxTr.X = -7 * nx : MountainParallaxTr.Y = -2.8 * ny
            NearParallaxTr.X = -12 * nx : NearParallaxTr.Y = -4.8 * ny
        End Sub

        ' ── Clock source ───────────────────────────────────────────────────────

        Private Shared Function CurrentTimeOfDay() As TimeSpan
#If DEBUG Then
            ' Verification affordance (mirrors VISTA_BYPASS_LOGIN): force a phase without waiting for sunset.
            Dim overrideValue As String = Environment.GetEnvironmentVariable("VISTA_LOGIN_SCENE_HOUR")
            Dim overrideHour As Integer
            If Integer.TryParse(overrideValue, overrideHour) AndAlso overrideHour >= 0 AndAlso overrideHour <= 23 Then
                Return New TimeSpan(overrideHour, 30, 0)
            End If
#End If
            Return DateTime.Now.TimeOfDay
        End Function

    End Class

End Namespace
