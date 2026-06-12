Imports System.Windows.Media.Animation
Imports System.Windows.Threading

Namespace Views.Login

    ''' <summary>Mutually exclusive mascot poses; names match the VSM states in CarabaoAvatar.xaml.</summary>
    Public Enum AvatarPose
        Idle = 0
        Watching = 1
        Shy = 2
        Peeking = 3
        Thinking = 4
        Happy = 5
        Rejected = 6
        Sleeping = 7
    End Enum

    ''' <summary>
    ''' Tanod the carabao (UX-48). Pose changes go through VSM (GoToElementState — GoToState
    ''' silently no-ops on a UserControl); idle loops are code-managed clocks so static mode
    ''' runs zero animations/timers. Gaze tracks the USERNAME caret only — never password
    ''' input; the Shy pose is fixed regardless of what is typed.
    ''' </summary>
    Partial Public Class CarabaoAvatar
        Inherits UserControl

        Private ReadOnly _blinkTimer As DispatcherTimer
        Private ReadOnly _rejectRecoverTimer As DispatcherTimer
        Private ReadOnly _rng As New Random()

        Private _staticMode As Boolean
        Private _initialized As Boolean
        Private _pose As AvatarPose = AvatarPose.Idle
        Private _rejectThenPose As AvatarPose = AvatarPose.Idle
        Private _blinkCounter As Integer

        Private _breathClock As AnimationClock
        Private _chewClock As AnimationClock
        Private _zzzClock As AnimationClock

        Public Sub New()
            InitializeComponent()
            _blinkTimer = New DispatcherTimer()
            AddHandler _blinkTimer.Tick, AddressOf OnBlinkTick
            _rejectRecoverTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(1.2)}
            AddHandler _rejectRecoverTimer.Tick, AddressOf OnRejectRecoverTick
        End Sub

        ' ── Lifecycle ──────────────────────────────────────────────────────────

        Public Sub Initialize(staticMode As Boolean)
            If _initialized Then Return
            _initialized = True
            _staticMode = staticMode
            GoToPose(AvatarPose.Idle, animate:=False)
            If _staticMode Then Return

            StartBreathLoop()
            ScheduleNextBlink()
            _blinkTimer.Start()
        End Sub

        ''' <summary>Stops every timer and clock. Idempotent; must run before the window is hidden.</summary>
        Public Sub Shutdown()
            _blinkTimer.Stop()
            _rejectRecoverTimer.Stop()
            StopLoop(_breathClock, BreathScale, ScaleTransform.ScaleYProperty)
            StopLoop(_chewClock, MuzzleTr, TranslateTransform.YProperty)
            StopLoop(_zzzClock, ZzzFloatTr, TranslateTransform.YProperty)
            GazePupilL.BeginAnimation(TranslateTransform.XProperty, Nothing)
            GazePupilR.BeginAnimation(TranslateTransform.XProperty, Nothing)
            GazeHeadRotate.BeginAnimation(RotateTransform.AngleProperty, Nothing)
            BrowAlertTr.BeginAnimation(TranslateTransform.YProperty, Nothing)
            BounceTr.BeginAnimation(TranslateTransform.YProperty, Nothing)
            _initialized = False
        End Sub

        Public Sub Pause()
            If _staticMode OrElse Not _initialized Then Return
            _blinkTimer.Stop()
            _breathClock?.Controller?.Pause()
            _chewClock?.Controller?.Pause()
            _zzzClock?.Controller?.Pause()
        End Sub

        Public Sub [Resume]()
            If _staticMode OrElse Not _initialized Then Return
            _blinkTimer.Start()
            _breathClock?.Controller?.Resume()
            _chewClock?.Controller?.Resume()
            _zzzClock?.Controller?.Resume()
        End Sub

        ' ── Pose API ───────────────────────────────────────────────────────────

        Public ReadOnly Property CurrentPose As AvatarPose
            Get
                Return _pose
            End Get
        End Property

        Public Sub GoToPose(pose As AvatarPose, Optional animate As Boolean = True)
            _pose = pose
            VisualStateManager.GoToElementState(AvatarRoot, pose.ToString(), animate AndAlso Not _staticMode)
            UpdateLoopClocks()
        End Sub

        ''' <summary>Pupils/head follow the username caret. progress 0..1 across a nominal 24-char window.</summary>
        Public Sub SetGaze(progress As Double)
            If _staticMode Then Return
            Dim clamped As Double = Math.Clamp(progress, 0.0, 1.0)
            AnimateGaze(-3 + 6 * clamped, -2 + 4 * clamped)
        End Sub

        Public Sub ResetGaze()
            If _staticMode Then Return
            AnimateGaze(0, 0)
        End Sub

        ''' <summary>Subtle brow raise while CapsLock is on (the functional warning is the badge in the card).</summary>
        Public Sub SetCapsAlert(isOn As Boolean)
            If _staticMode Then Return
            Dim alertAnim As New DoubleAnimation(If(isOn, -3.0, 0.0), New Duration(TimeSpan.FromMilliseconds(150))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            BrowAlertTr.BeginAnimation(TranslateTransform.YProperty, alertAnim)
        End Sub

        ''' <summary>
        ''' Sad beat on a rejected attempt; auto-recovers to <paramref name="thenPose"/> after 1.2s.
        ''' Identical shape and duration for every failure reason (no enumeration tell).
        ''' </summary>
        Public Sub PlayRejected(thenPose As AvatarPose)
            If _staticMode Then Return
            _rejectThenPose = thenPose
            GoToPose(AvatarPose.Rejected)
            _rejectRecoverTimer.Stop()
            _rejectRecoverTimer.Start()
        End Sub

        ''' <summary>
        ''' Happy pose + double bounce (~450ms). The returned task completes when the bounce ends;
        ''' static mode returns a completed task (instant swap, exactly as pre-UX-48).
        ''' </summary>
        Public Function PlaySuccessBeatAsync() As Task
            If _staticMode Then
                GoToPose(AvatarPose.Happy, animate:=False)
                Return Task.CompletedTask
            End If

            _rejectRecoverTimer.Stop()
            GoToPose(AvatarPose.Happy)

            Dim tcs As New TaskCompletionSource(Of Boolean)(TaskCreationOptions.RunContinuationsAsynchronously)
            Dim bounce As New DoubleAnimationUsingKeyFrames With {.FillBehavior = FillBehavior.Stop}
            bounce.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)))
            bounce.KeyFrames.Add(New LinearDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(120))))
            bounce.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(230))))
            bounce.KeyFrames.Add(New LinearDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320))))
            bounce.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(450))))
            AddHandler bounce.Completed, Sub(completedSender, completedArgs) tcs.TrySetResult(True)
            BounceTr.BeginAnimation(TranslateTransform.YProperty, bounce)
            Return tcs.Task
        End Function

        ' ── Idle loops (code-managed so static mode runs none) ────────────────

        Private Sub StartBreathLoop()
            Dim breathAnim As New DoubleAnimation(1.0, 1.015, New Duration(TimeSpan.FromSeconds(3))) With {
                .AutoReverse = True,
                .RepeatBehavior = RepeatBehavior.Forever,
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
            }
            Timeline.SetDesiredFrameRate(breathAnim, 30)
            _breathClock = breathAnim.CreateClock()
            BreathScale.ApplyAnimationClock(ScaleTransform.ScaleYProperty, _breathClock)
        End Sub

        Private Sub UpdateLoopClocks()
            ' Cud-chewing only while Thinking.
            If _pose = AvatarPose.Thinking AndAlso Not _staticMode Then
                If _chewClock Is Nothing Then
                    Dim chewAnim As New DoubleAnimation(0, 1.6, New Duration(TimeSpan.FromSeconds(0.55))) With {
                        .AutoReverse = True,
                        .RepeatBehavior = RepeatBehavior.Forever,
                        .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                    }
                    Timeline.SetDesiredFrameRate(chewAnim, 30)
                    _chewClock = chewAnim.CreateClock()
                    MuzzleTr.ApplyAnimationClock(TranslateTransform.YProperty, _chewClock)
                End If
            Else
                StopLoop(_chewClock, MuzzleTr, TranslateTransform.YProperty)
            End If

            ' Zzz float only while Sleeping.
            If _pose = AvatarPose.Sleeping AndAlso Not _staticMode Then
                If _zzzClock Is Nothing Then
                    Dim floatAnim As New DoubleAnimation(0, -5, New Duration(TimeSpan.FromSeconds(1.6))) With {
                        .AutoReverse = True,
                        .RepeatBehavior = RepeatBehavior.Forever,
                        .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseInOut}
                    }
                    Timeline.SetDesiredFrameRate(floatAnim, 30)
                    _zzzClock = floatAnim.CreateClock()
                    ZzzFloatTr.ApplyAnimationClock(TranslateTransform.YProperty, _zzzClock)
                End If
            Else
                StopLoop(_zzzClock, ZzzFloatTr, TranslateTransform.YProperty)
            End If
        End Sub

        Private Sub StopLoop(ByRef loopClock As AnimationClock, target As IAnimatable, targetProp As DependencyProperty)
            If loopClock Is Nothing Then Return
            loopClock.Controller?.Stop()
            target.ApplyAnimationClock(targetProp, Nothing)
            loopClock = Nothing
        End Sub

        ' ── Blink / ear flick ──────────────────────────────────────────────────

        Private Sub ScheduleNextBlink()
            _blinkTimer.Interval = TimeSpan.FromSeconds(4 + _rng.NextDouble() * 3)
        End Sub

        Private Sub OnBlinkTick(sender As Object, e As EventArgs)
            ScheduleNextBlink()
            ' Blink only when the eyes are visibly open; FillBehavior.Stop reverts to the VSM-held value.
            If _pose <> AvatarPose.Idle AndAlso _pose <> AvatarPose.Watching AndAlso _pose <> AvatarPose.Thinking Then Return

            RunBlinkAnimation(LidLScale)
            RunBlinkAnimation(LidRScale)

            _blinkCounter += 1
            If _blinkCounter Mod 3 = 0 Then
                RunEarFlick(EarFlickL, -6)
                RunEarFlick(EarFlickR, 6)
            End If
        End Sub

        Private Shared Sub RunBlinkAnimation(lidScale As ScaleTransform)
            Dim blink As New DoubleAnimationUsingKeyFrames With {.FillBehavior = FillBehavior.Stop}
            blink.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)))
            blink.KeyFrames.Add(New LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(70))))
            blink.KeyFrames.Add(New LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110))))
            blink.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))))
            lidScale.BeginAnimation(ScaleTransform.ScaleYProperty, blink)
        End Sub

        Private Shared Sub RunEarFlick(flickTransform As RotateTransform, flickAngle As Double)
            Dim flick As New DoubleAnimationUsingKeyFrames With {.FillBehavior = FillBehavior.Stop}
            flick.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)))
            flick.KeyFrames.Add(New LinearDoubleKeyFrame(flickAngle, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))))
            flick.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))))
            flickTransform.BeginAnimation(RotateTransform.AngleProperty, flick)
        End Sub

        ' ── Helpers ────────────────────────────────────────────────────────────

        Private Sub AnimateGaze(pupilOffsetX As Double, headAngle As Double)
            Dim pupilAnim As New DoubleAnimation(pupilOffsetX, New Duration(TimeSpan.FromMilliseconds(120))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            GazePupilL.BeginAnimation(TranslateTransform.XProperty, pupilAnim)
            GazePupilR.BeginAnimation(TranslateTransform.XProperty, pupilAnim.Clone())

            Dim headAnim As New DoubleAnimation(headAngle, New Duration(TimeSpan.FromMilliseconds(160))) With {
                .EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}
            }
            GazeHeadRotate.BeginAnimation(RotateTransform.AngleProperty, headAnim)
        End Sub

        Private Sub OnRejectRecoverTick(sender As Object, e As EventArgs)
            _rejectRecoverTimer.Stop()
            If _pose = AvatarPose.Rejected Then
                GoToPose(_rejectThenPose)
            End If
        End Sub

    End Class

End Namespace
