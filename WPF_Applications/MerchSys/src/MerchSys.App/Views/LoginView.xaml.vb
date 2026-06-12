Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports MerchSys.App.ViewModels
Imports MerchSys.App.Views.Login

Namespace Views

    ''' <summary>
    ''' Login window + UX-48 presentation wiring (scene, mascot, CapsLock badge, reject shake).
    ''' All animation is view-side; the only VM surface consumed beyond existing bindings is the
    ''' additive LoginAttemptFailed event. Teardown discipline matters: LoginView instances are
    ''' transient and are HIDDEN (not closed) after success, so everything must stop on
    ''' IsVisibleChanged=False or hidden windows would keep timers ticking forever.
    ''' </summary>
    Partial Class LoginView
        Inherits Window

        Private Enum PasswordFieldKind
            None = 0
            Main = 1
            NewPassword = 2
            Confirm = 3
        End Enum

        Private ReadOnly _viewModel As LoginViewModel
        Private ReadOnly _sleepTimer As DispatcherTimer

        Private _staticMode As Boolean
        Private _sceneStarted As Boolean
        Private _isAsleep As Boolean
        Private _capsBadgeShown As Boolean
        Private _usernameFocused As Boolean
        Private _focusedPasswordKind As PasswordFieldKind = PasswordFieldKind.None
        Private _lastActivityTick As Integer

        Public ReadOnly Property ViewModel As LoginViewModel
            Get
                Return _viewModel
            End Get
        End Property

        Public Sub New(vm As LoginViewModel)
            InitializeComponent()
            _viewModel = vm
            DataContext = vm

            _sleepTimer = New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(45)}
            AddHandler _sleepTimer.Tick, AddressOf OnSleepTimerTick

            AddHandler vm.LoginAttemptFailed, AddressOf OnLoginAttemptFailed
            AddHandler vm.PropertyChanged, AddressOf OnViewModelPropertyChanged

            AddHandler Me.IsVisibleChanged, AddressOf OnVisibilityChanged
            AddHandler Me.Closed, AddressOf OnClosedTeardown
            AddHandler Me.Activated, AddressOf OnWindowActivated
            AddHandler Me.Deactivated, AddressOf OnWindowDeactivated
            AddHandler Me.PreviewKeyDown, AddressOf OnWindowPreviewKey
            AddHandler Me.PreviewKeyUp, AddressOf OnWindowPreviewKey
            AddHandler Me.PreviewMouseDown, AddressOf OnWindowPreviewPointer
            AddHandler Me.PreviewMouseMove, AddressOf OnWindowPreviewPointer

            ' Username gaze tracking (username ONLY — never password input).
            AddHandler UsernameTextBox.GotFocus, AddressOf OnUsernameGotFocus
            AddHandler UsernameTextBox.LostFocus, AddressOf OnUsernameLostFocus
            AddHandler UsernameTextBox.TextChanged, AddressOf OnUsernameTextChanged
            AddHandler UsernameTextBox.SelectionChanged, AddressOf OnUsernameSelectionChanged

            ' Password-type fields → Shy/Peeking + CapsLock badge.
            AddHandler PasswordBoxHidden.GotFocus, Sub() OnPasswordFieldGotFocus(PasswordFieldKind.Main)
            AddHandler PasswordBoxHidden.LostFocus, Sub() OnPasswordFieldLostFocus(PasswordFieldKind.Main)
            AddHandler PasswordRevealBox.GotFocus, Sub() OnPasswordFieldGotFocus(PasswordFieldKind.Main)
            AddHandler PasswordRevealBox.LostFocus, Sub() OnPasswordFieldLostFocus(PasswordFieldKind.Main)
            AddHandler NewPasswordBox.GotFocus, Sub() OnPasswordFieldGotFocus(PasswordFieldKind.NewPassword)
            AddHandler NewPasswordBox.LostFocus, Sub() OnPasswordFieldLostFocus(PasswordFieldKind.NewPassword)
            AddHandler NewPasswordRevealBox.GotFocus, Sub() OnPasswordFieldGotFocus(PasswordFieldKind.NewPassword)
            AddHandler NewPasswordRevealBox.LostFocus, Sub() OnPasswordFieldLostFocus(PasswordFieldKind.NewPassword)
            AddHandler ConfirmNewPasswordBox.GotFocus, Sub() OnPasswordFieldGotFocus(PasswordFieldKind.Confirm)
            AddHandler ConfirmNewPasswordBox.LostFocus, Sub() OnPasswordFieldLostFocus(PasswordFieldKind.Confirm)
        End Sub

        ' ── Lifecycle (watch-item: transient instances are hidden, never closed) ──

        Private Sub OnVisibilityChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            If Me.IsVisible Then
                EnsureStarted()
            Else
                FullStop()
            End If
        End Sub

        Private Sub EnsureStarted()
            If _sceneStarted Then Return
            _sceneStarted = True
            _staticMode = ComputeStaticMode()
            SceneCanvas.Start(_staticMode)
            Avatar.Initialize(_staticMode)
            _isAsleep = False
            UpdateCapsBadge()
            If Not _staticMode Then _sleepTimer.Start()
        End Sub

        Private Sub FullStop()
            _sleepTimer.Stop()
            SceneCanvas.StopAll()
            Avatar.Shutdown()
            _isAsleep = False
            _sceneStarted = False
        End Sub

        Private Sub OnClosedTeardown(sender As Object, e As EventArgs)
            FullStop()
        End Sub

        Private Sub OnWindowActivated(sender As Object, e As EventArgs)
            If _sceneStarted AndAlso Not _staticMode Then
                SceneCanvas.[Resume]()
                Avatar.[Resume]()
                _sleepTimer.Start()
            End If
        End Sub

        Private Sub OnWindowDeactivated(sender As Object, e As EventArgs)
            If _sceneStarted AndAlso Not _staticMode Then
                SceneCanvas.Pause()
                Avatar.Pause()
                _sleepTimer.Stop()
            End If
        End Sub

        Private Shared Function ComputeStaticMode() As Boolean
            ' UX-25 reduced-motion contract + render-tier degrade (Tier < 2 = software rendering).
            Dim motionOn As Boolean = SystemParameters.ClientAreaAnimation
            Dim motionResource As Object = Application.Current.TryFindResource("MotionEnabled")
            If TypeOf motionResource Is Boolean Then
                motionOn = motionOn AndAlso CBool(motionResource)
            End If
            Dim renderTier As Integer = RenderCapability.Tier >> 16
            Return (Not motionOn) OrElse renderTier < 2
        End Function

        ''' <summary>
        ''' Called by Application.HandleLoginSucceeded: plays the mascot's success beat
        ''' (caller caps it at 700ms), then tears the scene down before the window is hidden.
        ''' Static mode completes instantly — the swap behaves exactly as pre-UX-48.
        ''' </summary>
        Public Async Function PlaySuccessBeatAsync() As Task
            _sleepTimer.Stop()
            Try
                Await Avatar.PlaySuccessBeatAsync()
            Finally
                FullStop()
            End Try
        End Function

        ' ── ViewModel signals ──────────────────────────────────────────────────

        Private Sub OnViewModelPropertyChanged(sender As Object, e As PropertyChangedEventArgs)
            If Not _sceneStarted Then Return
            Select Case e.PropertyName
                Case NameOf(LoginViewModel.IsLoggingIn)
                    If _viewModel.IsLoggingIn Then
                        Avatar.GoToPose(AvatarPose.Thinking)
                    Else
                        ApplyContextualPose()
                    End If
                Case NameOf(LoginViewModel.ShowPassword)
                    If _focusedPasswordKind = PasswordFieldKind.Main Then ApplyContextualPose()
                Case NameOf(LoginViewModel.ShowNewPassword)
                    If _focusedPasswordKind = PasswordFieldKind.NewPassword Then ApplyContextualPose()
            End Select
        End Sub

        Private Sub OnLoginAttemptFailed(sender As Object, e As EventArgs)
            If Not _sceneStarted OrElse _staticMode Then Return
            WakeIfAsleep()
            ShakeCard()
            Avatar.PlayRejected(ResolveContextualPose())
        End Sub

        ' ── Pose resolution ────────────────────────────────────────────────────

        Private Function ResolveContextualPose() As AvatarPose
            If _viewModel.IsLoggingIn Then Return AvatarPose.Thinking
            Select Case _focusedPasswordKind
                Case PasswordFieldKind.Main
                    Return If(_viewModel.ShowPassword, AvatarPose.Peeking, AvatarPose.Shy)
                Case PasswordFieldKind.NewPassword
                    Return If(_viewModel.ShowNewPassword, AvatarPose.Peeking, AvatarPose.Shy)
                Case PasswordFieldKind.Confirm
                    Return AvatarPose.Shy
            End Select
            If _usernameFocused Then Return AvatarPose.Watching
            Return AvatarPose.Idle
        End Function

        Private Sub ApplyContextualPose()
            If _isAsleep Then Return
            Avatar.GoToPose(ResolveContextualPose())
        End Sub

        ' ── Username gaze ──────────────────────────────────────────────────────

        Private Sub OnUsernameGotFocus(sender As Object, e As RoutedEventArgs)
            _usernameFocused = True
            ApplyContextualPose()
            UpdateGazeFromCaret()
        End Sub

        Private Sub OnUsernameLostFocus(sender As Object, e As RoutedEventArgs)
            _usernameFocused = False
            Avatar.ResetGaze()
            ApplyContextualPose()
        End Sub

        Private Sub OnUsernameTextChanged(sender As Object, e As Controls.TextChangedEventArgs)
            If _usernameFocused Then UpdateGazeFromCaret()
        End Sub

        Private Sub OnUsernameSelectionChanged(sender As Object, e As RoutedEventArgs)
            If _usernameFocused Then UpdateGazeFromCaret()
        End Sub

        Private Sub UpdateGazeFromCaret()
            If Not _sceneStarted Then Return
            ' Caret position across a nominal 24-character window → 0..1.
            Avatar.SetGaze(UsernameTextBox.CaretIndex / 24.0)
        End Sub

        ' ── Password focus + CapsLock badge ────────────────────────────────────

        Private Sub OnPasswordFieldGotFocus(fieldKind As PasswordFieldKind)
            _focusedPasswordKind = fieldKind
            ApplyContextualPose()
            UpdateCapsBadge()
        End Sub

        Private Sub OnPasswordFieldLostFocus(fieldKind As PasswordFieldKind)
            If _focusedPasswordKind = fieldKind Then
                _focusedPasswordKind = PasswordFieldKind.None
            End If
            ApplyContextualPose()
            UpdateCapsBadge()
        End Sub

        Private Sub UpdateCapsBadge()
            Dim shouldShow As Boolean =
                _focusedPasswordKind <> PasswordFieldKind.None AndAlso
                Keyboard.IsKeyToggled(Key.CapsLock)
            If shouldShow = _capsBadgeShown Then Return
            _capsBadgeShown = shouldShow
            CapsLockBadge.Visibility = If(shouldShow, Visibility.Visible, Visibility.Collapsed)
            Avatar.SetCapsAlert(shouldShow)
        End Sub

        ' ── Global input: caps re-check, sleep timer, wake ─────────────────────

        Private Sub OnWindowPreviewKey(sender As Object, e As KeyEventArgs)
            UpdateCapsBadge()
            RecordActivity()
        End Sub

        Private Sub OnWindowPreviewPointer(sender As Object, e As Input.MouseEventArgs)
            RecordActivity()
        End Sub

        Private Sub RecordActivity()
            If Not _sceneStarted OrElse _staticMode Then Return
            WakeIfAsleep()
            ' Throttle the timer restart — Preview events fire constantly under the cursor.
            Dim tick As Integer = Environment.TickCount
            If tick - _lastActivityTick < 1000 Then Return
            _lastActivityTick = tick
            _sleepTimer.Stop()
            _sleepTimer.Start()
        End Sub

        Private Sub OnSleepTimerTick(sender As Object, e As EventArgs)
            _sleepTimer.Stop()
            If _viewModel.IsLoggingIn Then
                _sleepTimer.Start()
                Return
            End If
            _isAsleep = True
            Avatar.GoToPose(AvatarPose.Sleeping)
            SceneCanvas.SetDimmed(True)
        End Sub

        Private Sub WakeIfAsleep()
            If Not _isAsleep Then Return
            _isAsleep = False
            SceneCanvas.SetDimmed(False)
            ApplyContextualPose()
        End Sub

        ' ── Reject shake (macOS wrong-password gesture) ────────────────────────

        Private Sub ShakeCard()
            Dim shake As New DoubleAnimationUsingKeyFrames With {.FillBehavior = FillBehavior.Stop}
            shake.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)))
            shake.KeyFrames.Add(New LinearDoubleKeyFrame(-12, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))))
            shake.KeyFrames.Add(New LinearDoubleKeyFrame(11, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))))
            shake.KeyFrames.Add(New LinearDoubleKeyFrame(-7, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))))
            shake.KeyFrames.Add(New LinearDoubleKeyFrame(4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))))
            shake.KeyFrames.Add(New LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(380))))
            CardShakeTr.BeginAnimation(TranslateTransform.XProperty, shake)
        End Sub

    End Class

End Namespace
