---
module: MerchSys.App
plan-id: UX-48
title: "Animated Login Experience — Time-of-Day Farm Scene + Reactive Carabao Mascot"
depends-on: [UX-01, UX-07, UX-25]
estimated-files: 10
---

# Animated Login Experience — Time-of-Day Farm Scene + Reactive Carabao Mascot

> **Level 3 (Pro) — item P13** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). The login window is the
> front door of VISTA — every user sees it every day, and today it is a static card on a flat
> background. This plan turns it into the app's signature moment: a **living Philippine farm
> panorama that follows the real time of day** (the macOS "dynamic desktop" idea, rendered in pure
> vector XAML) behind the existing login card, fronted by **a reactive carabao mascot** that watches
> you type, pulls its salakot over its eyes for the password, thinks while authenticating,
> celebrates success, and shakes off failure. **Presentation-only**: the authentication flow,
> `IAuthenticationService`, and the DA6 first-login password change are byte-for-byte untouched in
> behavior; the ViewModel gains exactly one additive event.

## Context

`Views/LoginView.xaml` is a 420px-wide `Window` holding a `CardStyle` border with the VISTA
wordmark, username/password fields (PasswordBox + reveal-TextBox swap via `PasswordBoxHelper`), an
error TextBlock, and the DA6 password-change panel. `LoginViewModel` already exposes every hook an
animated presentation needs — `Username` (binds `UpdateSourceTrigger=PropertyChanged`),
`IsLoggingIn`, `HasError`/`ErrorMessage`, `ShowPassword`, `ShowPasswordChange`, and the
`LoginSucceeded` event that `Application.xaml.vb → HandleLoginSucceeded` uses to swap windows.

Why this item earns a Pro slot:

- **Emotional design.** Aarron Walter's hierarchy (functional → reliable → usable → *pleasurable*)
  and Don Norman's visceral level both place exactly one delight feature above a finished usability
  floor — and B1–B7, A1–A7, P1–P3, P6–P8, P11–P12 are all shipped. The login is the highest-leverage
  surface for it: first touchpoint of every session (peak-end rule).
- **Prior art.** Darin Senneff's "Yeti" sign-in form (the avatar that follows your typing and covers
  its eyes for the password) is the canonical delightful-login pattern; the macOS *dynamic desktop*
  (Mojave) is the canonical time-of-day environment — both adapted here to VISTA's brand: an
  agricultural supply store in the Philippines, hence a **carabao with a salakot** over a rice-field
  panorama.
- **Functional payoff, not only charm.** The same pass adds a real correctness gap: there is **no
  CapsLock warning anywhere in the app** (verified by grep), and the existing reject feedback is
  text-only; this plan adds the macOS-style card shake + a CapsLock badge.

Per the lean-UX decision record (dropped P4/P5/P10): **zero new user-facing settings.** Reduced
motion rides the existing UX-25 mechanism (`SystemParameters.ClientAreaAnimation` →
`MotionEnabled`), not an in-app toggle.

## Prerequisites

- **UX-01** — token system (`Themes/Tokens.xaml`, `Light.xaml`/`Dark.xaml`), `CardStyle`,
  `AppFontFamily` (Inter), `ThemeService`.
- **UX-07** — `Themes/Icons.xaml` + `IconBase` (CapsLock badge reuses an existing warning glyph; add
  one 24×24 geometry only if none fits).
- **UX-25** — motion tokens `MotionEnabled` / `MotionDurationFast` / `MotionDurationStd` /
  `MotionEasing`, and the live reduced-motion propagation in `Application.xaml.vb`
  (`UpdateMotionSettings`, `SystemParameters.StaticPropertyChanged`).

## The concept

### The environment — "the farm at this hour"

A full-bleed vector panorama fills the login window behind a centered card. It renders **the same
scene at the actual local time of day** — the Philippines is near-equatorial, so five fixed local
time bands are honest (no solar math, by design):

| Phase | Local time | Sky (top → horizon, starter values) | Scene character |
|-------|-----------|--------------------------------------|-----------------|
| **Dawn** | 05:00–06:29 | `#34406B → #E8927C` | low sun, rose horizon, optional mist band over the paddies |
| **Day** | 06:30–16:29 | `#4A8FD4 → #C9E8F5` | bright greens, drifting cumulus, occasional maya-bird flight |
| **Golden** | 16:30–17:59 | `#C2693A → #F5D9A8` | warm amber light, long-shadow tones |
| **Dusk** | 18:00–19:29 | `#3A2D63 → #C76B8E` | first stars, fireflies begin, store sign glow turns on |
| **Night** | 19:30–04:59 | `#0B1230 → #2A3B5E` | crescent moon, twinkling stars, fireflies, lit store sign |

Layers back-to-front (all `Path`/`Ellipse` vector, inside a `Viewbox Stretch="UniformToFill"`):
sky gradient → celestial (sun disc with radial glow / crescent moon + ≤20 stars) → 3 cloud groups
drifting at different speeds (120–240s loops) → distant mountain ridge → rice paddies with ≤6
swaying palay clusters (±2° rotate, 4–6s, staggered) → foreground dirt road, fence posts, and the
**Villon Farm Supply storefront silhouette** whose sign and window glow warm at dusk/night →
critters (2 maya birds, day/golden, an ~18s flight path; ≤10 pulsing fireflies, dusk/night).

Phase changes crossfade over ~4s (`ColorAnimation` on the sky stops and tint brushes). A subtle
**parallax** shifts the cloud/mountain/foreground layers a few pixels toward the cursor
(±4/±7/±12px, 250ms ease-out). The scene is pure ambience: `IsHitTestVisible="False"`, excluded
from the automation tree.

### The mascot — Tanod the carabao

A macOS-login-style **circular avatar badge** (~96px) sits half-overlapping the top edge of the
card, holding a flat-design carabao bust — wide crescent horns, droopy ears, big eyes, and a
**salakot** (woven farmer's hat). "Tanod" (barangay watchman) is the in-repo character name — the
friendly gatekeeper. One mascot by design: the scene's birds/fireflies provide ambient life, but a
single character carries identity (multiple mascots dilute it).

| State | Trigger | Behavior |
|-------|---------|----------|
| **Idle** | default | blinks every 4–7s (randomized), subtle 3s breathing scale (1.00→1.015), occasional ear flick |
| **Watching** | username box focused / text changes | pupils track the caret position across the eye travel (±3px) with a tiny head tilt — *username only, never password* |
| **Shy** | any password-type box focused (incl. DA6 fields) | salakot tips down ~28° over the eyes — fixed pose, independent of anything typed |
| **Peeking** | `ShowPassword=True` while a password box is focused | salakot lifts ~12°, one curious eye visible |
| **Alert** | CapsLock on while a password box is focused | ears perk + raised brows; pairs with the functional CapsLock badge in the card |
| **Thinking** | `IsLoggingIn=True` | eyes drift up-left, slow cud-chewing jaw loop |
| **Happy** | `LoginSucceeded` | eyes arc closed, double bounce, salakot pops — the ~450ms "success beat" played before the window swap |
| **Rejected** | new `LoginAttemptFailed` event | ears droop + brief sad eyes while the **card shakes horizontally** (macOS wrong-password shake); auto-recovers to Idle in ~1.2s |
| **Sleeping** | 45s with no input while visible | eyes close, floating Zzz, scene dims slightly; any key/mouse wakes with a small stretch |

### The form

The existing card markup is preserved (fields, DA6 panel, `PasswordBoxHelper`, tab order,
`IsDefault`, `FocusManager.FocusedElement`) — restyled, not rebuilt: avatar badge on top, "VISTA"
wordmark reduced to `FontSizeTitle` beneath it, and a new **CapsLock badge** (warning icon +
"Caps Lock is on", `WarningBrush`, `AutomationProperties.LiveSetting="Assertive"`) above the
password field.

## Scope

### A. Scene foundation
- **`Themes/LoginScene.xaml`** (new) — all scene `Color` tokens (naming scheme
  `Scene<Phase><Element>`, e.g. `SceneNightSkyTop`, `SceneDayFoliage`, `SceneGoldenMountain`),
  static geometry resources (mountain ridge, storefront, cloud blobs, palay cluster), and shared
  scene brushes. **Every hex in the feature lives here** — the UX-04 "no inline hex in Views/" sweep
  stays at zero. Merged into `LoginView` resources only (not `App.xaml`) so the app-wide resource
  lookup table is unaffected.
- **`Views/Login/LoginScenePhase.vb`** (new) — `LoginScenePhase` enum + `LoginScenePhaseProvider`
  with a pure `GetPhase(timeOfDay)` (band table above) so phase logic is trivially testable later.
- **`Views/Login/DynamicSceneCanvas.xaml` + `.xaml.vb`** (new) — the layered panorama
  `UserControl`. Owns: the 60s `DispatcherTimer` phase clock, the 4s palette crossfade, all ambient
  storyboards, parallax (window-level `PreviewMouseMove`, throttled), and a lifecycle API:
  `Start(staticMode As Boolean)`, `Pause()`, `Resume()`, `StopAll()`.

### B. Mascot control
- **`Views/Login/CarabaoAvatar.xaml` + `.xaml.vb`** (new) — vector carabao on a 120×120 design
  grid (head ellipse ~center 60,68; wide crescent horns; salakot with rotation pivot ~60,30; eyes
  at 46,62 / 74,62 with translatable pupils; muzzle + nostrils; ears on base-pivot rotates).
  States via `VisualStateManager` groups on the root element. API:
  `SetGaze(progress As Double)` (0–1 → pupil/head offset, 120ms eased), `GoToAvatarState(state)`,
  `SetCapsAlert(isOn As Boolean)`, `PlayRejected()`, `PlaySuccessBeatAsync() As Task` (completes on
  storyboard end; completes immediately in static mode).

### C. LoginView recomposition
- **`Views/LoginView.xaml`** (modify) — window becomes fixed ~**900×660** (`ResizeMode="NoResize"`,
  `CenterScreen` kept; verify the DA6 expanded card fits with margin — adjust height once, not
  `SizeToContent`). Root grid: `DynamicSceneCanvas` (full-bleed, hit-test invisible) → in **Dark
  theme only** a 12% scrim over the scene (`DynamicResource`-driven so theme toggle stays live) →
  centered card (existing content, max width ~400) with the avatar badge overlapping its top edge →
  CapsLock badge row above the password grid (Collapsed by default). Add `x:Name` to the password
  boxes (`PasswordBoxHidden`, `PasswordRevealBox`, `NewPasswordBox`, `NewPasswordRevealBox`,
  `ConfirmNewPasswordBox`) for view-side focus wiring. Card gets a named `TranslateTransform` for
  the reject shake (±10px decaying, 4 cycles, ~320ms).

### D. Interaction wiring
- **`ViewModels/LoginViewModel.vb`** (modify, additive only) — add
  `Public Event LoginAttemptFailed As EventHandler`, raised in: `LoginAsync` exception branch,
  `Not result.Success` branch, and both `ChangePasswordAndLoginAsync` reject branches
  (mismatch validation + `Not changeResult.Success` + exception). **Not** raised when
  `ShowPasswordChange` flips true (the DA6 redirect is not a failure). No other VM change.
- **`Views/LoginView.xaml.vb`** (modify) — the wiring matrix:

  | Source (already exists unless noted) | View reaction |
  |---|---|
  | `UsernameTextBox` GotFocus / TextChanged / SelectionChanged | `SetGaze(caretIndex / nominal 24-char window)` → Watching |
  | `UsernameTextBox` LostFocus | recenter gaze → Idle |
  | any password box GotFocus | Shy (or Peeking if `ShowPassword`) |
  | `ShowPassword` change while password focused | Shy ↔ Peeking |
  | password box GotFocus + `PreviewKeyDown` | `SetCapsAlert(Keyboard.IsKeyToggled(Key.CapsLock))` + badge visibility (never set `e.Handled`) |
  | VM `PropertyChanged(IsLoggingIn)` | Thinking on/off |
  | VM `LoginAttemptFailed` *(new)* | card shake + `PlayRejected()` (skip shake in static mode — error text is the feedback) |
  | VM `LoginSucceeded` | handled in `Application.xaml.vb` (below) |
  | window `PreviewKeyDown`/`PreviewMouseMove` | reset 45s sleep timer; wake if Sleeping |
- **`Application.xaml.vb`** (modify, minimal) — `HandleLoginSucceeded` becomes `Async Sub`; after
  removing handlers and **before** `_loginView.Hide()`:
  `Try : Await Task.WhenAny(_loginView.PlaySuccessBeatAsync(), Task.Delay(700)) : Catch : End Try`
  — the happy beat plays during (and is capped against) the natural MainWindow swap; static mode
  returns a completed task, so reduced-motion users get an instant swap exactly as today.

### E. Motion discipline, lifecycle, performance
- **Static mode** (no separate code path — same tree, loops never started): entered when
  `SystemParameters.ClientAreaAnimation` is false (UX-25 semantics) **or**
  `RenderCapability.Tier >> 16 < 2`, **or** in any future caller passing `staticMode:=True`. Renders
  the correct current-phase palette as a still scene with the avatar in a static pose; CapsLock
  badge, error text, and all functionality unaffected; shake and success beat are skipped.
- **Lifecycle** (the hard requirement — see watch-item 3):

  | Event | Action |
  |---|---|
  | `Loaded` / `IsVisibleChanged → True` | detect static mode, `Start()` |
  | `Deactivated` / `Activated` | `Pause()` / `Resume()` ambient storyboards |
  | `IsVisibleChanged → False`, `Closed`, success-beat completion | `StopAll()` — every `DispatcherTimer` stopped + handlers removed, every storyboard stopped/removed |
- **Budgets:** animate **transforms and opacity only** (nothing layout-affecting); element caps —
  3 cloud groups, ≤20 stars (≤8 twinkling), ≤10 fireflies, ≤6 sway clusters, 2 birds; ambient
  storyboards may set `Timeline.DesiredFrameRate="30"` if profiling shows >~3% CPU on a mid-tier
  laptop; parallax throttled to ~30Hz.
- **DEBUG phase override** — `VISTA_LOGIN_SCENE_HOUR` environment variable (integer 0–23) read
  inside `#If DEBUG` only, mirroring the existing `VISTA_BYPASS_LOGIN` pattern, so each phase can be
  verified without waiting for sunset.

> **Out of scope (lean by decision record):**
> - Weather/season modes, sound, per-user or per-role avatars, avatar customization, any new
>   user-facing setting/toggle.
> - Bitmap/GIF/Lottie assets or any new NuGet package — stock WPF vector + storyboards only.
> - Custom window chrome (owner declined in UX-00); the standard title bar stays.
> - Any change to `IAuthenticationService`, Argon2id flow, session handling, DA6 semantics, error
>   *text*, or `MainWindow` startup sequence.
> - Reusing the mascot elsewhere in the app (empty states etc.) — not in this plan; propose
>   separately only if asked.

## Specification

### 0. Watch-items

1. **Frozen/shared-brush animation is the central WPF trap.** A brush from a resource dictionary is
   shared (and often frozen); animating it throws or repaints every consumer. The scene must build
   **local brush instances in code** (or `x:Shared="False"`), seeded from the `Color` tokens in
   `LoginScene.xaml`, hold direct references, and run `ColorAnimation` against those. Never animate
   a `DynamicResource`-assigned brush; never feed a brush token into a `Color` property.
2. **`VisualStateManager.GoToState` silently no-ops on a `UserControl`** without template states —
   use `VisualStateManager.GoToElementState(LayoutRoot, name, useTransitions)` with the state groups
   declared on the root element of `CarabaoAvatar`/`DynamicSceneCanvas`.
3. **Hidden login windows accumulate.** `LoginView` is registered **Transient**;
   `HandleLoginSucceeded` calls `Hide()` (not `Close()`), and each logout resolves a *new* instance.
   Without `StopAll()` on `IsVisibleChanged → False`, every past login leaves a hidden window with
   live `DispatcherTimer`s and animation clocks ticking forever. Teardown is an acceptance
   criterion, not polish.
4. **No credential-derived animation.** Gaze tracks the **username** caret only (visible text);
   the Shy pose is fixed regardless of password length or content; the reject animation is
   identical in shape and duration for *every* failure reason (no user-enumeration or timing tell);
   scene/avatar code never reads `Password`.
5. **DA6 is a redirect, not a failure.** The "set a new password" transition sets `ErrorMessage`
   guidance text — keying the shake off `HasError` would shake at the wrong moment. Shake **only**
   on the new `LoginAttemptFailed` event, raised exactly in the reject branches listed in Scope D.
6. **Async event handler discipline.** The `Async Sub HandleLoginSucceeded` wraps its `Await` in
   `Try…Catch` (await inside `Try` is legal; **no `Await` in `Catch`/`Finally`** — BC36943) and is
   capped by `Task.WhenAny(…, Task.Delay(700))` so a broken storyboard can never wedge login.
   Note: `Closed`-handler removal already precedes the beat, so closing the window during the
   ~450ms beat does not shut the app down — same as today's post-removal window, just longer;
   acceptable.
7. **XAML/VB traps** — full root prefix `clr-namespace:MerchSys.App.Views.Login` (MC3074) while
   `x:Class` stays relative (`Views.Login.CarabaoAvatar`); `Namespace Views.Login` in `.vb` files
   (never repeat the root — BC30002); lambda params must not shadow locals (BC36641); no reserved
   words as variables.
8. **Storyboard hygiene.** Code-started storyboards on named transforms need the names registered
   (`RegisterName`) or element-targeted `Begin(element, True)`; use `HandoffBehavior` deliberately
   on the crossfade; detach `Completed` handlers (they root the view); randomized blink uses **one**
   timer with a re-randomized interval, not a timer per element.
9. **Scene stays out of the way.** `IsHitTestVisible="False"` on the panorama (parallax reads the
   *window's* `PreviewMouseMove`), `AutomationProperties` exclusion for the decorative tree, and the
   card remains an opaque `CardStyle` surface — text contrast (WCAG 1.4.3) never depends on what the
   scene is doing behind it.
10. **WCAG motion conformance.** The ambient scene is auto-playing moving content (WCAG 2.2.2) and
    interaction-triggered motion (2.3.3): honoring the OS "Show animations in Windows" setting via
    the UX-25 mechanism is the pause/disable affordance — document this mapping in the summary. No
    flashing content anywhere near 2.3.1 thresholds (firefly pulses are slow fades).

### 1. Constraints

- **No new NuGet packages** — stock WPF (`Storyboard`, `VisualStateManager`, `Path`) only.
- **Zero new user-facing settings** (lean decision record); reduced motion is OS-driven via UX-25.
- **No business/auth change** — the only VM delta is the additive `LoginAttemptFailed` event; the
  only `Application.xaml.vb` delta is the capped success beat.
- **Hex discipline** — all scene colors are tokens in `Themes/LoginScene.xaml`; `Views/` hex sweep
  stays at zero; theme-dependent UI (card, badge, scrim) uses existing `DynamicResource` tokens.
- **Role model** — pre-auth surface, identical for Manager/Owner/Developer by definition.
- **Build gate** — `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` with **0 errors,
  0 warnings**; on failure, document in `Progress/` and stop (per CLAUDE.md).
- **Realization check** — boot the login in **both themes**, and via `VISTA_LOGIN_SCENE_HOUR` walk
  all five phases; verify: phase crossfade, avatar Watching/Shy/Peeking/Alert/Thinking/
  Rejected/Happy/Sleeping, CapsLock badge, DA6 panel fit at 900×660, reject shake, and a full
  **reduced-motion pass** (toggle "Show animations in Windows" off → static scene, instant swap,
  all functionality intact). Verify teardown: after login, the hidden LoginView has no running
  timers (breakpoint or debug log on `StopAll`).

## Acceptance Criteria

1. Build passes 0 errors / 0 warnings; no new NuGet references; no test projects.
2. The login window shows the five-phase time-of-day panorama with ambient motion (clouds, sway,
   celestial, critters, parallax) matching the local clock, crossfading at phase boundaries, in
   both Light and Dark themes (Dark adds the scrim; card/text contrast unaffected).
3. Tanod the carabao implements all nine states from the state table, wired exactly per the
   Scope D matrix; gaze derives from the username caret only; the Shy pose and reject animation are
   input-independent (watch-item 4).
4. Wrong credentials produce the macOS-style card shake + Rejected beat, identical for every
   failure reason; the DA6 redirect does **not** shake (watch-item 5).
5. CapsLock on, while a password field is focused, shows the warning badge
   (icon + text + `LiveSetting=Assertive`) and the avatar Alert pose; releasing CapsLock clears
   both. This is the app's first CapsLock affordance.
6. Successful login plays the ≤450ms success beat, hard-capped at 700ms, then swaps to MainWindow;
   with `VISTA_BYPASS_LOGIN=1` (DEBUG) nothing regresses; closing the login window pre-auth still
   exits the app.
7. Reduced motion (`SystemParameters.ClientAreaAnimation = False`) or render Tier < 2 yields a
   fully functional **static** login: correct current-phase still scene, posed avatar, badge and
   error feedback intact, zero running storyboards/timers, instant success swap.
8. Lifecycle teardown holds: `StopAll()` on hide/close verified — no timer or storyboard survives
   on a hidden LoginView instance (watch-item 3), including across a logout → re-login cycle.
9. All animation targets are transforms/opacity within the stated element caps; DA6 expanded card
   fits the fixed window; `FocusManager`, tab order, `IsDefault`, and Enter-to-submit all work
   exactly as before.
10. All scene hex lives in `Themes/LoginScene.xaml`; the VM delta is the single additive event;
    `IAuthenticationService` and all auth semantics are untouched.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-48-summary.md` (template `Progress/_template.md`). Include:
the final phase-band table and palette token inventory; the avatar state machine as implemented
(any state added/cut, with reason); the wiring matrix as landed; the lifecycle/teardown verification
(how `StopAll` was confirmed on the hidden instance); the reduced-motion and Tier-degrade behavior;
the WCAG 2.2.2/2.3.3 mapping note (watch-item 10); both-theme × five-phase realization results; CPU
observation on the dev laptop; and `codebase_wiki` discrepancies (new `Views/Login/` folder, new
theme dictionary, the additive VM event, the `Application.xaml.vb` success-beat change).

### Documentation (Agent Wiki)
Add `LLM_Wiki/agent_wiki/patterns/wpf-vista-animated-scene.md` per
`workflow-agent-wiki-update.md`, capturing the reusable mechanics: local-unfrozen-brush palette
crossfade (watch-item 1), `GoToElementState` for UserControl VSM (watch-item 2), lifecycle
discipline for hidden transient windows (watch-item 3), caret-driven gaze without a per-frame
render loop, and consuming the UX-25 `MotionEnabled` contract from a self-contained control. Update
`agent_wiki/index.md` + `agent_wiki/log.md`.
