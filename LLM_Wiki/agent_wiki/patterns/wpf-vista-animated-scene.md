---
type: pattern
module: MerchSys.App
agent: claude-code
date: 2026-06-12
tags: [wpf, xaml, vb-net, animation, visual-state-manager, animation-clock, lifecycle, reduced-motion, login, delight]
---

# WPF Ambient Scene + Reactive Avatar (UX-48 Animated Login)

## Context

UX-48 put a living time-of-day panorama (`Views/Login/DynamicSceneCanvas`) and a reactive mascot
(`Views/Login/CarabaoAvatar`) behind/on the login card. The feature is presentation-only, but it
concentrates four WPF mechanics that any future ambient/animated surface in VISTA will hit again:
animating brushes without tripping the frozen-Freezable rules, VSM on a `UserControl`, animation
lifecycle in windows that are *hidden rather than closed*, and honoring the UX-25 reduced-motion
contract from a self-contained control. This entry records the working recipe.

## The Pattern

### 1. Animate only local, code-built brushes — never resource brushes

Brushes that come out of a `ResourceDictionary` may be shared between consumers, and shared/frozen
`Freezable`s either throw on animation or repaint everything that uses them. The scene therefore
defines **`Color` tokens** (animation seeds) in `Themes/LoginScene.xaml` and builds its animated
brushes in code, holding direct references:

```vb
_skyBrush = New LinearGradientBrush With {.StartPoint = New Point(0.5, 0), .EndPoint = New Point(0.5, 1)}
_skyBrush.GradientStops.Add(New GradientStop(dayPalette.SkyTop, 0.0))   ' code-built ⇒ unfrozen
SkyRect.Fill = _skyBrush
...
' Crossfade: ColorAnimation directly against the stop / brush instance
StartClockTracked(_skyBrush.GradientStops(0), GradientStop.ColorProperty, colorAnim, _crossfadeClocks)
```

One brush instance can be deliberately shared *within the control* (all palay stalks stroke the
same `_foliageBrush`) so a single `ColorAnimation` retints every consumer. Static fills that never
animate stay as `StaticResource` Brush tokens. Never feed a Brush token into a `Color` property
(see `[[wpf-dynamicresource-brush-into-color-property]]`).

### 2. Tracked `AnimationClock`s instead of fire-and-forget storyboards

Every loop (`CreateClock` + `ApplyAnimationClock`) is recorded as `(target, property, clock)` in a
list per concern (ambient / fireflies / birds / crossfade), giving real `Pause()` / `[Resume]()` /
`StopAll()`:

```vb
Private Shared Sub StopClocks(clockList As List(Of ClockEntry))
    For Each ce As ClockEntry In clockList
        ce.Clock.Controller?.Stop()
        ce.Target.ApplyAnimationClock(ce.Prop, Nothing)   ' detach — base value restored
    Next
    clockList.Clear()
End Sub
```

Phase-dependent loops (fireflies at dusk/night, birds by day) start/stop on phase change instead of
hiding behind `Opacity=0` — an invisible animation still ticks. `Timeline.SetDesiredFrameRate(anim, 30)`
on every ambient loop halves the evaluation cost; negative `BeginTime` staggers loop phases so
clones don't move in lockstep.

### 3. UserControl VSM: `GoToElementState` + separated transform ownership

`VisualStateManager.GoToState(control, …)` silently no-ops on a `UserControl` (no template states).
Declare the groups on the root element and call
`VisualStateManager.GoToElementState(AvatarRoot, pose.ToString(), useTransitions)`.

Pose states are **zero-duration storyboards** (`To="…" Duration="0"`) tweened by a shared
`VisualTransition` whose `GeneratedDuration`/easing reference the UX-25 tokens
(`MotionDurationStd`, `MotionEasing`) — so the OS reduced-motion gate zeroes pose tweens for free.
Properties a state doesn't mention revert to their XAML base value on transition.

Two systems animating one property fight (last-writer wins, then a stale hold sticks). Rule:
**VSM owns a transform; code owns a different transform; compose them in a `TransformGroup`.**
E.g. each pupil has `GazePupilL` (code, caret tracking) + `PupilPoseL` (VSM, thinking pose);
the brows have `BrowPoseTr` (VSM) + `BrowAlertTr` (code, CapsLock). The one sanctioned overlap is
the blink: a keyframe animation with `FillBehavior.Stop` on the VSM-owned lid scale, suppressed by
pose bookkeeping while the eyes are covered — on completion the VSM-held value resumes.

### 4. Lifecycle discipline for hidden, transient windows

`LoginView` is DI-**transient** and `HandleLoginSucceeded` calls `Hide()`, not `Close()`; each
logout resolves a *new* instance. Without teardown, every past login leaves a hidden window whose
`DispatcherTimer`s and clocks tick forever. The recipe: route `IsVisibleChanged(False)`, `Closed`,
and the success path into one idempotent `FullStop()` (scene `StopAll()` + avatar `Shutdown()` +
sleep-timer stop). `Window.Activated`/`Deactivated` map to `[Resume]()`/`Pause()`.

### 5. Static mode = the same tree with no clocks started

`staticMode = (Not SystemParameters.ClientAreaAnimation) OrElse (Not MotionEnabled) OrElse (RenderCapability.Tier >> 16) < 2`
is computed once per show. In static mode the control applies the current palette/pose with direct
property sets (`GoToElementState(..., useTransitions:=False)`, `stop.Color = …`) and simply never
starts loops, timers, or parallax — no second code path, zero running animations, functionality
(CapsLock badge, error text) intact. A `#If DEBUG` env override (`VISTA_LOGIN_SCENE_HOUR`)
forces any phase for realization checks, mirroring the `VISTA_BYPASS_LOGIN` precedent.

### 6. Gaze without a render loop

Eye tracking needs no `CompositionTarget.Rendering`: `TextChanged`/`SelectionChanged` map
`CaretIndex / 24.0` → a 120 ms eased `DoubleAnimation` on the gaze transforms. Privacy rule baked
in: gaze reads the **username** caret only; the Shy pose is one fixed value regardless of password
input; the reject animation is identical (shape *and* duration) for every failure reason.

## Why It Works

- Code-built brushes are never frozen, and direct `BeginAnimation`/clock application on them avoids
  the property-path-into-frozen-resource minefield entirely.
- Clock lists make "no animation survives Hide()" a checkable invariant instead of a hope —
  `ApplyAnimationClock(prop, Nothing)` detaches *and* restores the base value.
- Transform-per-owner means VSM and imperative animation compose additively (`TransformGroup`
  multiplies/sums) instead of contending for one DP.
- Zero-duration states + token-driven generated transitions centralize all pose timing in the
  UX-25 tokens — one knob, reduced-motion-aware.

## Rules

- Never animate a brush that lives in a shared `ResourceDictionary`; build it in code from a
  `Color` token, keep the reference, animate that.
- Every loop goes through a tracked clock list with a `StopAll()`; any view that can be hidden
  without being closed must call it on `IsVisibleChanged(False)`.
- `GoToElementState`, never `GoToState`, on UserControl-rooted VSM.
- One property, one animating owner; compose extra motion through additional transforms.
- Ambient loops: transforms/opacity only, `DesiredFrameRate 30`, staggered `BeginTime`, hard
  element caps (UX-48: 3 clouds / 8 twinkling stars / 10 fireflies / 6 sway clusters / 2 birds).
- No animation parameter may derive from credential input (length, characters, or failure reason).

## Related

- Links to related entries: `[[wpf-vista-motion]]` (MotionEnabled contract),
  `[[wpf-vsm-foreground-on-non-control-template-root]]` (VSM targeting),
  `[[wpf-dynamicresource-brush-into-color-property]]`, `[[wpf-vista-theming-conventions]]`
- Links to Domain Wiki pages: `[[client-server-wpf]]`
