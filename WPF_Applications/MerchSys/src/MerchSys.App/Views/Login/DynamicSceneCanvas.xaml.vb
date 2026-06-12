Imports System.Windows.Media.Animation
Imports System.Windows.Threading

Namespace Views.Login

    ''' <summary>
    ''' The dusk storefront hero shot behind the login card (UX-49; machinery inherited from UX-48).
    ''' Owns the 60s mood clock, the 4s palette crossfade, all ambient animation clocks, the
    ''' entrance choreography, and the cursor parallax. Lifecycle: <see cref="Start"/> /
    ''' <see cref="Pause"/> / <see cref="[Resume]"/> / <see cref="StopAll"/> — the host view MUST
    ''' call StopAll when it becomes invisible (LoginView instances are transient and are hidden,
    ''' not closed).
    '''
    ''' All animated fills are local unfrozen brushes built in code (never resource brushes —
    ''' resource Freezables may be shared/frozen and cannot be animated safely). Every animated
    ''' property is a transform, a color on a local brush, or an opacity. After each crossfade is
    ''' started, the same values are also written as local BASE values so one-shot effects
    ''' (door-glow pulse, entrance) can use FillBehavior.Stop without snapping to a stale base.
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
        Private Const TwinkleFrameRate As Integer = 24
        Private Const ParallaxThrottleMs As Integer = 33

        Private ReadOnly _ambientClocks As New List(Of ClockEntry)
        Private ReadOnly _crossfadeClocks As New List(Of ClockEntry)
        Private ReadOnly _entranceClocks As New List(Of ClockEntry)

        Private ReadOnly _phaseTimer As DispatcherTimer
        Private ReadOnly _catTimer As DispatcherTimer
        Private ReadOnly _rng As New Random()

        Private _currentPhase As LoginScenePhase
        Private _started As Boolean
        Private _paused As Boolean
        Private _staticMode As Boolean

        Private _hostWindow As Window
        Private _lastParallaxTick As Integer

        ' Animated brushes — created unfrozen in code, assigned to scene shapes once.
        Private _skyBrush As LinearGradientBrush
        Private _cloudBrush As SolidColorBrush
        Private _townBrush As SolidColorBrush
        Private _wallBrush As LinearGradientBrush
        Private _facadeBaseBrush As SolidColorBrush
        Private _awningGreenBrush As SolidColorBrush
        Private _awningCreamBrush As SolidColorBrush
        Private _signPlateBrush As SolidColorBrush
        Private _glowBrush As LinearGradientBrush
        Private _poolBrush As RadialGradientBrush
        Private _reflectionBrush As LinearGradientBrush
        Private _spillBrush As RadialGradientBrush
        Private _signHaloBrush As RadialGradientBrush
        Private _pavementBrush As LinearGradientBrush

        Private Structure ScenePalette
            Public SkyTop As Color
            Public SkyUpper As Color
            Public SkyLower As Color
            Public SkyHorizon As Color
            Public CloudStreak As Color
            Public Town As Color
            Public FacadeWall As Color
            Public FacadeBase As Color
            Public AwningGreen As Color
            Public AwningCream As Color
            Public SignPlate As Color
            Public GlowCore As Color
            Public GlowWarm As Color
            Public Pavement As Color
            Public PavementFar As Color
            Public Reflection As Color
            Public StarOpacity As Double
            Public MoonOpacity As Double
            Public CloudOpacity As Double
            Public SignHaloOpacity As Double
            Public DoorLightOpacity As Double
            Public WindowLightOpacity As Double
            Public BulbLayerOpacity As Double
            Public BulbEvenOpacity As Double
            Public BulbOddOpacity As Double
            Public FireflyOpacity As Double
            Public CatSittingOpacity As Double
        End Structure

        Private _currentScalars As ScenePalette

        ''' <summary>The scene visual sampled by the frosted-glass card. Never hand the card an
        ''' ancestor of itself — VisualBrush self-reference is illegal.</summary>
        Public ReadOnly Property SceneVisual As Visual
            Get
                Return SceneRoot
            End Get
        End Property

        Public Sub New()
            InitializeComponent()
            BuildAnimatedBrushes()
            _phaseTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(60)}
            AddHandler _phaseTimer.Tick, AddressOf OnPhaseTimerTick
            _catTimer = New DispatcherTimer()
            AddHandler _catTimer.Tick, AddressOf OnCatTick
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
            ScheduleNextCatFlick()
            _catTimer.Start()
        End Sub

        ''' <summary>Pauses ambient loops (window deactivated). Phase crossfades are short-lived and unaffected.</summary>
        Public Sub Pause()
            If Not _started OrElse _staticMode OrElse _paused Then Return
            _paused = True
            PauseClocks(_ambientClocks)
            PauseClocks(_entranceClocks)
            _catTimer.Stop()
        End Sub

        Public Sub [Resume]()
            If Not _started OrElse _staticMode OrElse Not _paused Then Return
            _paused = False
            ResumeClocks(_ambientClocks)
            ResumeClocks(_entranceClocks)
            _catTimer.Start()
        End Sub

        ''' <summary>
        ''' Hard teardown: every clock detached, timers stopped, parallax unhooked.
        ''' Idempotent — called on hide, on close, and after the success beat.
        ''' </summary>
        Public Sub StopAll()
            _phaseTimer.Stop()
            _catTimer.Stop()
            StopClocks(_ambientClocks)
            StopClocks(_crossfadeClocks)
            StopClocks(_entranceClocks)
            CatTailRotate.BeginAnimation(RotateTransform.AngleProperty, Nothing)
            DoorGlowRect.BeginAnimation(OpacityProperty, Nothing)
            DoorSpill.BeginAnimation(OpacityProperty, Nothing)
            DimOverlay.BeginAnimation(OpacityProperty, Nothing)
            DimOverlay.Opacity = 0
            UnhookParallax()
            CelestialParallaxTr.X = 0 : CelestialParallaxTr.Y = 0
            CloudsParallaxTr.X = 0 : CloudsParallaxTr.Y = 0
            TownParallaxTr.X = 0 : TownParallaxTr.Y = 0
            FacadeParallaxTr.X = 0 : FacadeParallaxTr.Y = 0
            PropsParallaxTr.X = 0 : PropsParallaxTr.Y = 0
            _paused = False
            _started = False
        End Sub

        ''' <summary>
        ''' Entrance choreography (UX-49): the scene brightens and the bulb string lights up
        ''' left-to-right, sign halo last — "the store turns on for you." One-shot, ~1.1s,
        ''' never in static mode. Call after <see cref="Start"/>.
        ''' </summary>
        Public Sub PlayEntrance()
            If Not _started OrElse _staticMode Then Return
            StopClocks(_entranceClocks)

            SceneRoot.Opacity = 0.55
            Dim sceneFade As New DoubleAnimation(1.0, New Duration(TimeSpan.FromMilliseconds(600))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            StartClockTracked(SceneRoot, OpacityProperty, sceneFade, _entranceClocks)

            Dim bulbGroupArr() As Canvas = BulbGroups()
            For i As Integer = 0 To bulbGroupArr.Length - 1
                Dim bulbTarget As Double = If(i Mod 2 = 0, _currentScalars.BulbEvenOpacity, _currentScalars.BulbOddOpacity)
                bulbGroupArr(i).Opacity = 0
                Dim popAnim As New DoubleAnimation(bulbTarget, New Duration(TimeSpan.FromMilliseconds(180))) With {
                    .BeginTime = TimeSpan.FromMilliseconds(120 + 70 * i),
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
                }
                StartClockTracked(bulbGroupArr(i), OpacityProperty, popAnim, _entranceClocks)
            Next

            SignHalo.Opacity = 0
            Dim haloAnim As New DoubleAnimation(_currentScalars.SignHaloOpacity, New Duration(TimeSpan.FromMilliseconds(300))) With {
                .BeginTime = TimeSpan.FromMilliseconds(800),
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            StartClockTracked(SignHalo, OpacityProperty, haloAnim, _entranceClocks)
        End Sub

        ''' <summary>Success beat: the doorway glow pulses once — "welcome in."</summary>
        Public Sub PulseDoorGlow()
            If Not _started OrElse _staticMode Then Return
            Dim glowPulse As New DoubleAnimation(Math.Min(1.0, _currentScalars.DoorLightOpacity + 0.22),
                                                 New Duration(TimeSpan.FromMilliseconds(320))) With {
                .AutoReverse = True,
                .FillBehavior = FillBehavior.Stop,
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            DoorGlowRect.BeginAnimation(OpacityProperty, glowPulse)
            DoorSpill.BeginAnimation(OpacityProperty, glowPulse.Clone())
        End Sub

        ''' <summary>Dims the scene slightly while the avatar sleeps.</summary>
        Public Sub SetDimmed(dimmed As Boolean)
            If _staticMode Then Return
            Dim dimAnim As New DoubleAnimation(If(dimmed, 0.1, 0.0), New Duration(TimeSpan.FromMilliseconds(600))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            DimOverlay.BeginAnimation(OpacityProperty, dimAnim)
        End Sub

        ' ── Brushes & palette ──────────────────────────────────────────────────

        Private Sub BuildAnimatedBrushes()
            Dim p As ScenePalette = GetPalette(LoginScenePhase.Dusk)

            _skyBrush = New LinearGradientBrush With {
                .StartPoint = New Point(0.5, 0),
                .EndPoint = New Point(0.5, 1)
            }
            _skyBrush.GradientStops.Add(New GradientStop(p.SkyTop, 0.0))
            _skyBrush.GradientStops.Add(New GradientStop(p.SkyUpper, 0.18))
            _skyBrush.GradientStops.Add(New GradientStop(p.SkyLower, 0.33))
            _skyBrush.GradientStops.Add(New GradientStop(p.SkyHorizon, 0.46))
            SkyRect.Fill = _skyBrush

            _cloudBrush = New SolidColorBrush(p.CloudStreak)
            CloudA.Fill = _cloudBrush
            CloudB.Fill = _cloudBrush
            CloudC.Fill = _cloudBrush

            _townBrush = New SolidColorBrush(p.Town)
            TownPath.Fill = _townBrush
            PalmLeft.Fill = _townBrush
            PalmRight.Fill = _townBrush

            _wallBrush = New LinearGradientBrush With {
                .StartPoint = New Point(0.5, 0),
                .EndPoint = New Point(0.5, 1)
            }
            _wallBrush.GradientStops.Add(New GradientStop(p.FacadeWall, 0.0))
            _wallBrush.GradientStops.Add(New GradientStop(p.FacadeBase, 1.0))
            WallRect.Fill = _wallBrush

            _facadeBaseBrush = New SolidColorBrush(p.FacadeBase)
            FasciaRect.Fill = _facadeBaseBrush
            PlankSeam1.Fill = _facadeBaseBrush
            PlankSeam2.Fill = _facadeBaseBrush
            PlankSeam3.Fill = _facadeBaseBrush
            BaseStrip.Fill = _facadeBaseBrush
            DoorSurroundRect.Fill = _facadeBaseBrush
            WindowFrameRect.Fill = _facadeBaseBrush
            WindowSillRect.Fill = _facadeBaseBrush
            AwningBarRect.Fill = _facadeBaseBrush

            _awningGreenBrush = New SolidColorBrush(p.AwningGreen)
            AwningGreenPath.Fill = _awningGreenBrush
            _awningCreamBrush = New SolidColorBrush(p.AwningCream)
            AwningCreamPath.Fill = _awningCreamBrush

            _signPlateBrush = New SolidColorBrush(p.SignPlate)
            SignPlateRect.Fill = _signPlateBrush

            ' Door + display window share one interior-glow gradient (brighter toward the floor).
            _glowBrush = New LinearGradientBrush With {
                .StartPoint = New Point(0.5, 0),
                .EndPoint = New Point(0.5, 1)
            }
            _glowBrush.GradientStops.Add(New GradientStop(p.GlowWarm, 0.0))
            _glowBrush.GradientStops.Add(New GradientStop(p.GlowCore, 0.62))
            DoorGlowRect.Fill = _glowBrush
            WindowGlowRect.Fill = _glowBrush

            _poolBrush = New RadialGradientBrush()
            _poolBrush.GradientStops.Add(New GradientStop(WithAlpha(p.GlowCore, &H55), 0.0))
            _poolBrush.GradientStops.Add(New GradientStop(WithAlpha(p.GlowCore, 0), 1.0))
            DoorPool.Fill = _poolBrush
            WindowPool.Fill = _poolBrush

            _reflectionBrush = New LinearGradientBrush With {
                .StartPoint = New Point(0.5, 0),
                .EndPoint = New Point(0.5, 1)
            }
            _reflectionBrush.GradientStops.Add(New GradientStop(WithAlpha(p.Reflection, &H4A), 0.0))
            _reflectionBrush.GradientStops.Add(New GradientStop(WithAlpha(p.Reflection, 0), 1.0))
            SignReflection.Fill = _reflectionBrush
            WindowReflection.Fill = _reflectionBrush
            DoorReflection.Fill = _reflectionBrush

            _spillBrush = New RadialGradientBrush()
            _spillBrush.GradientStops.Add(New GradientStop(WithAlpha(p.GlowWarm, &H30), 0.0))
            _spillBrush.GradientStops.Add(New GradientStop(WithAlpha(p.GlowWarm, 0), 1.0))
            DoorSpill.Fill = _spillBrush

            _signHaloBrush = New RadialGradientBrush()
            _signHaloBrush.GradientStops.Add(New GradientStop(WithAlpha(p.GlowWarm, &H52), 0.0))
            _signHaloBrush.GradientStops.Add(New GradientStop(WithAlpha(p.GlowWarm, 0), 1.0))
            SignHalo.Fill = _signHaloBrush

            _pavementBrush = New LinearGradientBrush With {
                .StartPoint = New Point(0.5, 0),
                .EndPoint = New Point(0.5, 1)
            }
            _pavementBrush.GradientStops.Add(New GradientStop(p.PavementFar, 0.0))
            _pavementBrush.GradientStops.Add(New GradientStop(p.Pavement, 1.0))
            PavementRect.Fill = _pavementBrush

            _currentScalars = p
        End Sub

        Private Function GetPalette(phase As LoginScenePhase) As ScenePalette
            Dim prefix As String = "Scene" & phase.ToString()
            Dim p As New ScenePalette With {
                .SkyTop = Col(prefix & "SkyTopColor"),
                .SkyUpper = Col(prefix & "SkyUpperColor"),
                .SkyLower = Col(prefix & "SkyLowerColor"),
                .SkyHorizon = Col(prefix & "SkyHorizonColor"),
                .CloudStreak = Col(prefix & "CloudStreakColor"),
                .Town = Col(prefix & "TownColor"),
                .FacadeWall = Col(prefix & "FacadeWallColor"),
                .FacadeBase = Col(prefix & "FacadeBaseColor"),
                .AwningGreen = Col(prefix & "AwningGreenColor"),
                .AwningCream = Col(prefix & "AwningCreamColor"),
                .SignPlate = Col(prefix & "SignPlateColor"),
                .GlowCore = Col(prefix & "GlowCoreColor"),
                .GlowWarm = Col(prefix & "GlowWarmColor"),
                .Pavement = Col(prefix & "PavementColor"),
                .PavementFar = Col(prefix & "PavementFarColor"),
                .Reflection = Col(prefix & "ReflectionColor")
            }

            Select Case phase
                Case LoginScenePhase.Dusk
                    p.StarOpacity = 0.35 : p.MoonOpacity = 0.55 : p.CloudOpacity = 0.8
                    p.SignHaloOpacity = 0.6 : p.DoorLightOpacity = 1.0 : p.WindowLightOpacity = 0.85
                    p.BulbLayerOpacity = 0.8 : p.BulbEvenOpacity = 1.0 : p.BulbOddOpacity = 1.0
                    p.FireflyOpacity = 0.75 : p.CatSittingOpacity = 1.0
                Case LoginScenePhase.Evening
                    p.StarOpacity = 1.0 : p.MoonOpacity = 1.0 : p.CloudOpacity = 0.45
                    p.SignHaloOpacity = 0.85 : p.DoorLightOpacity = 1.0 : p.WindowLightOpacity = 1.0
                    p.BulbLayerOpacity = 0.95 : p.BulbEvenOpacity = 1.0 : p.BulbOddOpacity = 1.0
                    p.FireflyOpacity = 1.0 : p.CatSittingOpacity = 1.0
                Case Else ' LateNight — closing time: interior dimmed, every other bulb off, cat asleep
                    p.StarOpacity = 1.0 : p.MoonOpacity = 1.0 : p.CloudOpacity = 0.3
                    p.SignHaloOpacity = 0.5 : p.DoorLightOpacity = 0.45 : p.WindowLightOpacity = 0.3
                    p.BulbLayerOpacity = 0.8 : p.BulbEvenOpacity = 0.0 : p.BulbOddOpacity = 0.55
                    p.FireflyOpacity = 0.6 : p.CatSittingOpacity = 0.0
            End Select

            Return p
        End Function

        Private Function Col(resourceKey As String) As Color
            Return CType(FindResource(resourceKey), Color)
        End Function

        Private Shared Function WithAlpha(c As Color, a As Byte) As Color
            Return Color.FromArgb(a, c.R, c.G, c.B)
        End Function

        Private Function BulbGroups() As Canvas()
            Return New Canvas() {Bulb0, Bulb1, Bulb2, Bulb3, Bulb4, Bulb5, Bulb6, Bulb7, Bulb8}
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
                CrossfadeColor(_skyBrush.GradientStops(1), p.SkyUpper)
                CrossfadeColor(_skyBrush.GradientStops(2), p.SkyLower)
                CrossfadeColor(_skyBrush.GradientStops(3), p.SkyHorizon)
                CrossfadeColor(_cloudBrush, p.CloudStreak)
                CrossfadeColor(_townBrush, p.Town)
                CrossfadeColor(_wallBrush.GradientStops(0), p.FacadeWall)
                CrossfadeColor(_wallBrush.GradientStops(1), p.FacadeBase)
                CrossfadeColor(_facadeBaseBrush, p.FacadeBase)
                CrossfadeColor(_awningGreenBrush, p.AwningGreen)
                CrossfadeColor(_awningCreamBrush, p.AwningCream)
                CrossfadeColor(_signPlateBrush, p.SignPlate)
                CrossfadeColor(_glowBrush.GradientStops(0), p.GlowWarm)
                CrossfadeColor(_glowBrush.GradientStops(1), p.GlowCore)
                CrossfadeColor(_poolBrush.GradientStops(0), WithAlpha(p.GlowCore, &H55))
                CrossfadeColor(_poolBrush.GradientStops(1), WithAlpha(p.GlowCore, 0))
                CrossfadeColor(_reflectionBrush.GradientStops(0), WithAlpha(p.Reflection, &H4A))
                CrossfadeColor(_reflectionBrush.GradientStops(1), WithAlpha(p.Reflection, 0))
                CrossfadeColor(_spillBrush.GradientStops(0), WithAlpha(p.GlowWarm, &H30))
                CrossfadeColor(_spillBrush.GradientStops(1), WithAlpha(p.GlowWarm, 0))
                CrossfadeColor(_signHaloBrush.GradientStops(0), WithAlpha(p.GlowWarm, &H52))
                CrossfadeColor(_signHaloBrush.GradientStops(1), WithAlpha(p.GlowWarm, 0))
                CrossfadeColor(_pavementBrush.GradientStops(0), p.PavementFar)
                CrossfadeColor(_pavementBrush.GradientStops(1), p.Pavement)

                CrossfadeDouble(StarsLayer, OpacityProperty, p.StarOpacity)
                CrossfadeDouble(MoonGroup, OpacityProperty, p.MoonOpacity)
                CrossfadeDouble(CloudsLayer, OpacityProperty, p.CloudOpacity)
                CrossfadeDouble(SignHalo, OpacityProperty, p.SignHaloOpacity)
                CrossfadeDouble(DoorGlowRect, OpacityProperty, p.DoorLightOpacity)
                CrossfadeDouble(DoorSpill, OpacityProperty, p.DoorLightOpacity)
                CrossfadeDouble(DoorPool, OpacityProperty, p.DoorLightOpacity)
                CrossfadeDouble(DoorReflection, OpacityProperty, p.DoorLightOpacity)
                CrossfadeDouble(WindowGlowRect, OpacityProperty, p.WindowLightOpacity)
                CrossfadeDouble(WindowPool, OpacityProperty, p.WindowLightOpacity)
                CrossfadeDouble(WindowReflection, OpacityProperty, p.WindowLightOpacity)
                CrossfadeDouble(SignReflection, OpacityProperty, p.SignHaloOpacity * 0.5)
                CrossfadeDouble(BulbsLayer, OpacityProperty, p.BulbLayerOpacity)
                Dim bulbGroupArr() As Canvas = BulbGroups()
                For i As Integer = 0 To bulbGroupArr.Length - 1
                    CrossfadeDouble(bulbGroupArr(i), OpacityProperty,
                                    If(i Mod 2 = 0, p.BulbEvenOpacity, p.BulbOddOpacity))
                Next
                CrossfadeDouble(FirefliesLayer, OpacityProperty, p.FireflyOpacity)
                CrossfadeDouble(CatSitting, OpacityProperty, p.CatSittingOpacity)
                CrossfadeDouble(CatAsleep, OpacityProperty, 1.0 - p.CatSittingOpacity)
            End If

            ' Base values always end up correct (under any clock) so FillBehavior.Stop
            ' one-shots (entrance, door pulse) never snap to a stale base.
            SetBaseValues(p)
            _currentScalars = p
        End Sub

        Private Sub SetBaseValues(p As ScenePalette)
            _skyBrush.GradientStops(0).Color = p.SkyTop
            _skyBrush.GradientStops(1).Color = p.SkyUpper
            _skyBrush.GradientStops(2).Color = p.SkyLower
            _skyBrush.GradientStops(3).Color = p.SkyHorizon
            _cloudBrush.Color = p.CloudStreak
            _townBrush.Color = p.Town
            _wallBrush.GradientStops(0).Color = p.FacadeWall
            _wallBrush.GradientStops(1).Color = p.FacadeBase
            _facadeBaseBrush.Color = p.FacadeBase
            _awningGreenBrush.Color = p.AwningGreen
            _awningCreamBrush.Color = p.AwningCream
            _signPlateBrush.Color = p.SignPlate
            _glowBrush.GradientStops(0).Color = p.GlowWarm
            _glowBrush.GradientStops(1).Color = p.GlowCore
            _poolBrush.GradientStops(0).Color = WithAlpha(p.GlowCore, &H55)
            _poolBrush.GradientStops(1).Color = WithAlpha(p.GlowCore, 0)
            _reflectionBrush.GradientStops(0).Color = WithAlpha(p.Reflection, &H4A)
            _reflectionBrush.GradientStops(1).Color = WithAlpha(p.Reflection, 0)
            _spillBrush.GradientStops(0).Color = WithAlpha(p.GlowWarm, &H30)
            _spillBrush.GradientStops(1).Color = WithAlpha(p.GlowWarm, 0)
            _signHaloBrush.GradientStops(0).Color = WithAlpha(p.GlowWarm, &H52)
            _signHaloBrush.GradientStops(1).Color = WithAlpha(p.GlowWarm, 0)
            _pavementBrush.GradientStops(0).Color = p.PavementFar
            _pavementBrush.GradientStops(1).Color = p.Pavement

            StarsLayer.Opacity = p.StarOpacity
            MoonGroup.Opacity = p.MoonOpacity
            CloudsLayer.Opacity = p.CloudOpacity
            SignHalo.Opacity = p.SignHaloOpacity
            DoorGlowRect.Opacity = p.DoorLightOpacity
            DoorSpill.Opacity = p.DoorLightOpacity
            DoorPool.Opacity = p.DoorLightOpacity
            DoorReflection.Opacity = p.DoorLightOpacity
            WindowGlowRect.Opacity = p.WindowLightOpacity
            WindowPool.Opacity = p.WindowLightOpacity
            WindowReflection.Opacity = p.WindowLightOpacity
            SignReflection.Opacity = p.SignHaloOpacity * 0.5
            BulbsLayer.Opacity = p.BulbLayerOpacity
            Dim bulbGroupArr() As Canvas = BulbGroups()
            For i As Integer = 0 To bulbGroupArr.Length - 1
                bulbGroupArr(i).Opacity = If(i Mod 2 = 0, p.BulbEvenOpacity, p.BulbOddOpacity)
            Next
            FirefliesLayer.Opacity = p.FireflyOpacity
            CatSitting.Opacity = p.CatSittingOpacity
            CatAsleep.Opacity = 1.0 - p.CatSittingOpacity
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
            ' Dusk cloud streaks: gentle ±28px oscillation — no wrap traversal needed.
            Dim cloudTransforms() As TranslateTransform = {CloudATr, CloudBTr, CloudCTr}
            Dim cloudDurations() As Double = {110, 90, 135}
            For i As Integer = 0 To 2
                Dim driftAnim As New DoubleAnimation(-28, 28, New Duration(TimeSpan.FromSeconds(cloudDurations(i)))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 40),
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                }
                Timeline.SetDesiredFrameRate(driftAnim, AmbientFrameRate)
                StartClockTracked(cloudTransforms(i), TranslateTransform.XProperty, driftAnim, _ambientClocks)
            Next

            ' Hanging sign sway: ±1.2° around its hang point.
            Dim swayAnim As New DoubleAnimation(-1.2, 1.2, New Duration(TimeSpan.FromSeconds(5.5))) With {
                .AutoReverse = True,
                .RepeatBehavior = RepeatBehavior.Forever,
                .BeginTime = TimeSpan.FromSeconds(-2),
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Timeline.SetDesiredFrameRate(swayAnim, AmbientFrameRate)
            StartClockTracked(SignSway, RotateTransform.AngleProperty, swayAnim, _ambientClocks)

            ' Bulb twinkle: six of the nine halos breathe gently (cap 6).
            Dim bulbGroupArr() As Canvas = BulbGroups()
            Dim twinkleIndices() As Integer = {0, 2, 4, 5, 7, 8}
            For Each bulbIndex As Integer In twinkleIndices
                Dim halo = CType(bulbGroupArr(bulbIndex).Children(1), Ellipse)
                Dim twinkleAnim As New DoubleAnimation(0.7, 1.0, New Duration(TimeSpan.FromSeconds(2.8 + _rng.NextDouble() * 1.7))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 3)
                }
                Timeline.SetDesiredFrameRate(twinkleAnim, TwinkleFrameRate)
                StartClockTracked(halo, OpacityProperty, twinkleAnim, _ambientClocks)
            Next

            ' Star twinkle: every third star, capped at 6.
            Dim twinkleCount As Integer = 0
            For starIndex As Integer = 0 To StarsLayer.Children.Count - 1 Step 3
                If twinkleCount >= 6 Then Exit For
                Dim star As UIElement = StarsLayer.Children(starIndex)
                Dim starAnim As New DoubleAnimation(0.35, 0.95, New Duration(TimeSpan.FromSeconds(2.2 + _rng.NextDouble() * 1.6))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 3)
                }
                Timeline.SetDesiredFrameRate(starAnim, TwinkleFrameRate)
                StartClockTracked(star, OpacityProperty, starAnim, _ambientClocks)
                twinkleCount += 1
            Next

            ' Wet-pavement sheen: two slow overlapping shimmer bands.
            Dim sheen1Anim As New DoubleAnimation(0.22, 0.42, New Duration(TimeSpan.FromSeconds(9))) With {
                .AutoReverse = True,
                .RepeatBehavior = RepeatBehavior.Forever,
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Timeline.SetDesiredFrameRate(sheen1Anim, TwinkleFrameRate)
            StartClockTracked(Sheen1, OpacityProperty, sheen1Anim, _ambientClocks)
            Dim sheen2Anim As New DoubleAnimation(0.2, 0.38, New Duration(TimeSpan.FromSeconds(13))) With {
                .AutoReverse = True,
                .RepeatBehavior = RepeatBehavior.Forever,
                .BeginTime = TimeSpan.FromSeconds(-5),
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Timeline.SetDesiredFrameRate(sheen2Anim, TwinkleFrameRate)
            StartClockTracked(Sheen2, OpacityProperty, sheen2Anim, _ambientClocks)

            ' Fireflies: pulse + drift (all moods here are dusk/night, so they always run).
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
                StartClockTracked(firefly, OpacityProperty, pulseAnim, _ambientClocks)

                Dim driftXAnim As New DoubleAnimation(-14, 14, New Duration(TimeSpan.FromSeconds(7 + _rng.NextDouble() * 4))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 6),
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                }
                Timeline.SetDesiredFrameRate(driftXAnim, AmbientFrameRate)
                StartClockTracked(floatTr, TranslateTransform.XProperty, driftXAnim, _ambientClocks)

                Dim driftYAnim As New DoubleAnimation(-8, 8, New Duration(TimeSpan.FromSeconds(5 + _rng.NextDouble() * 3))) With {
                    .AutoReverse = True,
                    .RepeatBehavior = RepeatBehavior.Forever,
                    .BeginTime = TimeSpan.FromSeconds(-_rng.NextDouble() * 5),
                    .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                }
                Timeline.SetDesiredFrameRate(driftYAnim, AmbientFrameRate)
                StartClockTracked(floatTr, TranslateTransform.YProperty, driftYAnim, _ambientClocks)
            Next
        End Sub

        ' ── Cat tail flick (one-shot every 12-21s; asleep cats don't flick) ────

        Private Sub ScheduleNextCatFlick()
            _catTimer.Interval = TimeSpan.FromSeconds(12 + _rng.NextDouble() * 9)
        End Sub

        Private Sub OnCatTick(sender As Object, e As EventArgs)
            ScheduleNextCatFlick()
            If _staticMode OrElse _paused OrElse _currentPhase = LoginScenePhase.LateNight Then Return
            Dim flick As New DoubleAnimationUsingKeyFrames With {.FillBehavior = FillBehavior.Stop}
            flick.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)))
            flick.KeyFrames.Add(New LinearDoubleKeyFrame(-16, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))))
            flick.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(460))))
            CatTailRotate.BeginAnimation(RotateTransform.AngleProperty, flick)
        End Sub

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
            CelestialParallaxTr.X = -2 * nx : CelestialParallaxTr.Y = -0.8 * ny
            CloudsParallaxTr.X = -2 * nx : CloudsParallaxTr.Y = -0.8 * ny
            TownParallaxTr.X = -4 * nx : TownParallaxTr.Y = -1.6 * ny
            FacadeParallaxTr.X = -7 * nx : FacadeParallaxTr.Y = -2.8 * ny
            PropsParallaxTr.X = -13 * nx : PropsParallaxTr.Y = -5.2 * ny
        End Sub

        ' ── Clock source ───────────────────────────────────────────────────────

        Private Shared Function CurrentTimeOfDay() As TimeSpan
#If DEBUG Then
            ' Verification affordance (mirrors VISTA_BYPASS_LOGIN): force a mood without waiting for sunset.
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
