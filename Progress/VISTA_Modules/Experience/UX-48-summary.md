---
module: MerchSys.App
agent: claude-code
date: 2026-06-12
plan-ref: Plans/VISTA_Modules/Experience/48-animated-login-experience.md
status: completed
---

# UX-48 — Animated Login Experience: Time-of-Day Farm Scene + Reactive Carabao Mascot

## Task Summary

Implemented roadmap item **P13** (`ROADMAP-L3-pro.md`): the login window now renders a pure-vector
Philippine farm panorama that follows the real local time of day (five fixed bands, crossfaded),
with ambient motion (cloud drift, palay sway, star twinkle, fireflies at dusk/night, maya birds by
day, cursor parallax, a storefront sign that glows after dark) behind the existing login card — and
**Tanod the carabao**, a salakot-wearing mascot in a macOS-style circular badge that blinks and
breathes when idle, follows the username caret with its pupils, slides its salakot over its eyes on
password focus (lifts it to peek on show-password), raises its brows on CapsLock, chews while
authenticating, bounces on success, and droops with a macOS-style card shake on rejection, falling
asleep after 45 s of idle. Functional wins shipped alongside: the app's **first CapsLock warning
badge** and the **wrong-password card shake**. Presentation-only: auth flow, DA6, and
`IAuthenticationService` semantics are unchanged; the ViewModel delta is one additive event.

**Plan:** `[[48-animated-login-experience]]`
**Branch:** master (per session instruction)

## What Was Done

- Created `src/MerchSys.App/Themes/LoginScene.xaml` — all 55 per-phase `Color` tokens
  (`Scene{Phase}{Element}Color`, 5 phases × 11 elements), phase-independent colors, and every
  mascot/badge brush. **Every hex of the feature lives here**; the Views/ hex sweep stays at zero.
- Created `src/MerchSys.App/Views/Login/LoginScenePhase.vb` — `LoginScenePhase` enum +
  `LoginScenePhaseProvider.GetPhase(TimeSpan)` pure band lookup (Dawn 05:00–06:29, Day –16:29,
  Golden –17:59, Dusk –19:29, Night otherwise; equatorial PH ⇒ fixed bands, no solar math).
- Created `src/MerchSys.App/Views/Login/DynamicSceneCanvas.xaml(.vb)` — the layered 1600×900
  panorama in a `UniformToFill` viewbox. Animated fills are **local unfrozen brushes built in
  code** (sky `LinearGradientBrush` stops + 8 `SolidColorBrush`es) seeded from the tokens;
  crossfades are 4 s `ColorAnimation`/`DoubleAnimation` clocks. All loops are tracked
  `AnimationClock`s in four lists (ambient / fireflies / birds / crossfade) with
  `Start(staticMode)` / `Pause()` / `[Resume]()` / `StopAll()` lifecycle, 60 s phase
  `DispatcherTimer`, throttled (~30 Hz) cursor parallax on three depth groups (±4/±7/±12 px),
  `Timeline.SetDesiredFrameRate 30` on every ambient loop, and a `#If DEBUG`
  **`VISTA_LOGIN_SCENE_HOUR`** override (mirrors `VISTA_BYPASS_LOGIN`) to force any phase.
  Element caps per plan: 3 cloud groups, 18 stars (8 twinkling), 10 fireflies, 6 sway clusters,
  2 birds.
- Created `src/MerchSys.App/Views/Login/CarabaoAvatar.xaml(.vb)` — badge + bust geometry and the
  eight VSM pose states (Idle, Watching, Shy, Peeking, Thinking, Happy, Rejected, Sleeping) as
  zero-duration pose storyboards tweened by a `VisualTransition` bound to **`MotionDurationStd` +
  `MotionEasing`** (UX-25 tokens). Pose changes go through `GoToElementState` (the
  `[[wpf-vsm-foreground-on-non-control-template-root]]` family of VSM traps; `GoToState` no-ops on
  a UserControl). Code-driven motion (gaze, caps-alert brow raise, blink, ear flick, bounce,
  breath, chew, zzz-float) uses **separate transforms composed with the VSM-driven ones** so the
  two systems never fight over a property; idle loops are code-managed clocks so static mode runs
  zero animations. API: `Initialize/Shutdown/Pause/[Resume]`, `GoToPose`, `SetGaze/ResetGaze`,
  `SetCapsAlert`, `PlayRejected(thenPose)`, `PlaySuccessBeatAsync()`.
- Modified `src/MerchSys.App/Views/LoginView.xaml` — fixed 900×700 window (was 420×auto;
  `NoResize`/`CenterScreen` kept), full-bleed scene + theme scrim + centered 384px card with the
  avatar badge overlapping its top edge. Existing form markup, bindings, `PasswordBoxHelper`,
  tab order, `IsDefault`, KeyBindings and the DA6 panel preserved; added `x:Name`s on the five
  password inputs, the **CapsLock badge** (warning icon + caption, `WarningBrush`,
  `AutomationProperties.LiveSetting="Assertive"`) beside the Password label, the same LiveSetting
  on the error text, and a named `TranslateTransform` for the card shake.
- Modified `src/MerchSys.App/Views/LoginView.xaml.vb` — the full wiring matrix from the plan:
  username focus/caret → `SetGaze` (username **only**; reveal boxes deliberately unwired); password
  focus kind (Main/New/Confirm) → Shy/Peeking; `ShowPassword`/`ShowNewPassword` → peek toggle;
  `IsLoggingIn` → Thinking; `LoginAttemptFailed` → shake + `PlayRejected` (skipped in static mode);
  CapsLock re-checked on focus + window-level PreviewKey events; 45 s sleep timer with throttled
  activity reset and wake; `Activated`/`Deactivated` → pause/resume. **Lifecycle:**
  `IsVisibleChanged(False)`/`Closed`/success-beat completion all funnel into `FullStop()`
  (scene `StopAll` + avatar `Shutdown` + sleep timer stop) — required because LoginView is
  DI-transient and *hidden*, never closed, after success. Static mode =
  `SystemParameters.ClientAreaAnimation`/`MotionEnabled` false **or** `RenderCapability.Tier < 2`.
- Modified `src/MerchSys.App/ViewModels/LoginViewModel.vb` — **additive only**: new
  `LoginAttemptFailed` event raised in the six reject branches (login exception, `Not
  result.Success`, password-change mismatch, lost pending user, change exception, `Not
  changeResult.Success`). The DA6 redirect does **not** raise it. No other VM change.
- Modified `src/MerchSys.App/Application.xaml.vb` — `HandleLoginSucceeded` is now `Async Sub`:
  `Await Task.WhenAny(_loginView.PlaySuccessBeatAsync(), Task.Delay(700))` inside `Try…Catch`
  (Await in the Try body only — BC36943) before `Hide()`. Static mode returns a completed task ⇒
  instant swap exactly as before.
- Modified `Themes/Light.xaml` + `Themes/Dark.xaml` — new token `LoginSceneScrimBrush`
  (Light: opacity 0; Dark: black 12 %) so the dark card keeps contrast over a bright scene; keys
  stay in parity per the UX-00 token contract.

No NuGet packages added; no bitmap/Lottie assets — stock WPF vector + storyboards/clocks only.
Zero new user-facing settings (lean decision record): reduced motion rides the UX-25 OS gate.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors / 0 warnings (first attempt) |
| Unit tests pass | N/A (no test projects, per project rules) |
| Manual verification | ⏳ Pending operator session (steps below) |

### Realization-check steps for the operator session

1. `setx VISTA_LOGIN_SCENE_HOUR 5` (then 10 / 17 / 18 / 22) → launch → verify Dawn / Day / Golden /
   Dusk / Night palettes, sign glow + fireflies after dark, birds by day; clear the variable after.
2. Both themes: toggle Light/Dark before logout → re-login; Dark must show the 12 % scrim, card
   contrast intact.
3. Avatar walk: focus username & type (gaze tracks caret); focus password (salakot covers eyes);
   toggle the eye (peek); CapsLock on (badge + brow raise; release clears); submit wrong password
   (card shake + sad beat, identical for unknown user vs wrong password); correct login (bounce,
   then MainWindow ≤ 700 ms); idle 45 s (sleep + scene dim; any key wakes).
4. DA6: first-login account → panel must fit the fixed 900×700 window; new-password fields also
   trigger Shy/Peek; rejected change shakes.
5. Reduced motion: Settings → Accessibility → Visual effects → Animation effects **off** → login is
   a static current-phase scene, posed mascot, CapsLock badge + error text functional, instant
   success swap, **no** shake. Same static path covers render Tier < 2.
6. Teardown: log in, then logout → on the new LoginView press nothing for >45 s — only THIS window
   may sleep; optionally breakpoint `DynamicSceneCanvas.StopAll` to confirm it ran for the hidden
   instance (transient-window accumulation guard).
7. WCAG 2.2.2/2.3.3 note: the OS "animation effects" setting is the pause/disable mechanism for the
   auto-playing ambient scene (UX-25 mechanism); firefly pulses are slow fades, nowhere near 2.3.1
   flash thresholds.

## Issues Encountered

- **`Resume` is a VB keyword** — `Pause()/[Resume]()` lifecycle methods needed bracket-escaping at
  declaration *and* call sites (`SceneCanvas.[Resume]()`). Trivial; caught pre-build.
- **Design judgment call:** the plan's wiring matrix lists `LoginAttemptFailed` raises for the
  listed reject branches; the *empty username/password* early-return validation does **not** raise
  (it is input guidance, not a rejected attempt) — consistent with the plan's DA6 rule. Flagged
  here in case the operator prefers macOS's shake-on-empty-submit behavior; a one-line addition.
- **Front-facing salakot:** the plan's "tips down ~28°" reads as rotation, but the bust is drawn
  front-facing, so the shy gesture is implemented as **translate-down 22 px + −4° tilt** (rotation
  alone cannot cover both eyes from the front). Same intent, correct geometry.

## What's Next

- [ ] Operator realization pass (steps above) — both themes × five phases × reduced-motion, DA6 fit
      at 900×700, teardown breakpoint check, and a CPU glance (Task Manager) during idle ambient.
- [ ] If idle CPU on the store laptops exceeds ~3 %, lower `AmbientFrameRate` (30 → 24/20) in
      `DynamicSceneCanvas.xaml.vb` — the single intended tuning knob.
- [ ] Optional polish (explicitly cut from scope): dawn mist band; shake-on-empty-submit (see
      Issues).

## Cross-References

- Domain Wiki pages consulted: `[[client-server-wpf]]` (stack), UX-00 token contract via
  `ROADMAP-ui-ux-perfection.md` cross-cutting rules.
- Agent Wiki entries consulted: `[[wpf-vista-motion]]` (MotionEnabled/duration tokens, reduced-motion
  gate), `[[wpf-vsm-foreground-on-non-control-template-root]]` (VSM targeting discipline),
  `[[wpf-dynamicresource-brush-into-color-property]]` (Color-vs-Brush token hygiene),
  `[[vbnet-nullable-trycast-value-type-compile-error]]` (used `TypeOf … Is Boolean` + `CBool`, not
  `TryCast`, for the `MotionEnabled` resource read).
- New Agent Wiki entry written: `[[wpf-vista-animated-scene]]` (pattern — see below).

### codebase_wiki discrepancies (for Antigravity sync)

- New folder `src/MerchSys.App/Views/Login/` (4 files) + new `Themes/LoginScene.xaml` are not yet in
  the App module file inventory (155 → 160 files).
- `LoginViewModel` gained the `LoginAttemptFailed` event; `LoginView` gained
  `PlaySuccessBeatAsync()`; `Application.HandleLoginSucceeded` is now `Async Sub` with the capped
  success beat.
- `Light.xaml`/`Dark.xaml` gained `LoginSceneScrimBrush` (token contract parity).
- `LoginView` window geometry changed from 420×SizeToContent to fixed 900×700.
