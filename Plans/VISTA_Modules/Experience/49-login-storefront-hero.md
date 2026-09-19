---
module: MerchSys.App
plan-id: UX-49
title: "Login Scene Redesign — Dusk Storefront Hero Shot, Tindera Avatar, Frosted-Glass Card"
depends-on: [UX-48]
estimated-files: 12
---

# Login Scene Redesign — Dusk Storefront Hero Shot + Tindera Avatar + Frosted-Glass Card

> **Level 3 (Pro) — item P13a** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). UX-48 shipped the right
> *machinery* (time-band phases, 4s palette crossfades, tracked animation clocks with hard teardown,
> cursor parallax, reduced-motion/render-tier static fallback, CapsLock badge, reject shake) wearing
> the wrong *artwork*: flat single-color layers, clouds with EvenOdd holes punched through them, a
> storefront sign clipped by the `UniformToFill` crop, an empty flat-green foreground, and a mascot
> that reads "children's app". This plan replaces every pixel of presentation while keeping the
> UX-48 engineering contracts byte-compatible. **Owner verdict (2026-06-12):** scene = **dusk
> storefront hero shot**, avatar = **Filipina tindera**, card = **frosted glass**.

## Why UX-48's artwork failed (defect record — these are the anti-goals)

| # | Defect | Root cause | UX-49 rule that prevents it |
|---|--------|-----------|------------------------------|
| D1 | Clouds render as "four-leaf clovers" | `GeometryGroup` default `FillRule=EvenOdd` cuts holes at every ellipse overlap | **Every multi-figure geometry sets `FillRule="Nonzero"`** |
| D2 | "VILLON FARM SUPPLY" sign clipped mid-word at the window edge | 1600×900 canvas cropped by `UniformToFill` at 900×700; no safe area | **Safe-area rule:** all readable/narrative elements inside canvas x ∈ [260, 1340] |
| D3 | Kindergarten flatness | One solid color per layer; no gradients on large surfaces; no atmospheric depth | **No flat fill on any element > ~15% of canvas**; every depth band is lighter than the band in front of it |
| D4 | Nothing is lit | Sun/sign are pasted shapes; no surface responds to any light source | **Every light source ships 4 artifacts:** halo, the gradient it casts on nearby surfaces, a ground pool, and a wet-pavement reflection |
| D5 | Props look like clipart | Mid-tone filled shapes with outlines | **Silhouette-first props:** near-black shapes + at most 2 warm rim-light strokes; zero mid-tone interior detail |
| D6 | The phase the user demos (Day) was the blandest | Five literal time bands forced a noon palette that vector flatness can't carry | **Hero-hour art direction:** the scene is permanently cinematic (3 dusk/night moods); there is no weak phase to be caught in |
| D7 | Card disconnected from scene | Opaque `SurfaceBrush` card | Frosted-glass card (live blurred scene backdrop) |
| D8 | Avatar reads childish | Gray circle-face carabao | Flat-illustration human tindera with eye catchlights (see proportions spec) |

## The concept

A single, deliberately art-directed **hero shot** — the Villon Farm Supply storefront at blue hour,
just after closing prep, every light warm against a deepening sky. Like a game title screen, the
composition is fixed at its most flattering hour by design; real time of day survives as **three
lighting moods** of the same scene, so *every* login hour is demo-grade:

| Mood | Local time | Character |
|------|-----------|-----------|
| **Dusk** (hero, default) | 05:00–18:59 | Blue-hour sky (indigo → ember horizon), first stars, every store light warm and on |
| **Evening** | 19:00–22:59 | Full night sky, stars at max, interior glow brighter by contrast, fireflies |
| **LateNight** | 23:00–04:59 | Closing time: interior dimmed to a night-light, every other bulb off, cat asleep, moon high |

The 60s phase timer, 4s crossfade pipeline, `VISTA_LOGIN_SCENE_HOUR` debug override, and the
static-mode contract all carry over unchanged. `LoginScenePhase` enum members become
`Dusk / Evening / LateNight` (the enum is consumed only inside `Views/Login/`).

### Composition map (canvas 1600×900)

Visible region at 900×700 after `UniformToFill`: x ∈ [~193, ~1407], full height. The glass card +
badge occlude approximately x ∈ [539, 1061] × y ∈ [122, 776] (blurred, not hidden). Layout puts
every readable element in the left/right clear zones and lets the facade run *behind* the glass:

```
x:  260        539                 1061        1340
    │  LEFT     │   BEHIND GLASS    │   RIGHT   │
    │  zone     │   (blurred)       │   zone    │
 ───┼───────────┼───────────────────┼───────────┼───
    │ ☽ moon (380,120)   stars across the top   │
    │           town + palm silhouette strip    │
    │ ╔════════╗                                │
    │ ║VILLON  ║ hanging bracket sign,          │
    │ ║FARM    ║ halo + chains + ±1.2° sway     │
    │ ╚════════╝                                │
    │  awning: scalloped green/cream stripes ───┼──→ full width
    │  bulb string: 9 warm bulbs on a catenary ─┼──→ (bulbs 4–6 glow
    │           │  (through the glass as bokeh) │     behind the card)
    │ display   │   facade wall, plank seams,   │ open doorway,
    │ window:   │   painted band               │ warm light spill,
    │ sacks ·   │                               │ shelf + sack
    │ bottles · │                               │ silhouettes inside;
    │ hanging   │                               │ sack stack + cat;
    │ scale     │                               │ potted plants
    │ TRICYCLE silhouette parked (270–560)      │ broom by the door
 ───┼─ curb ────┼───────────────────────────────┼───
    │  pavement: door pool · window pool · sign │
    │  reflection · bulb streaks · wet sheen    │
```

Layer tree (back → front), each layer's parallax shift on cursor:

| Layer | Contents | Parallax |
|-------|----------|----------|
| L0 Sky | 4-stop vertical gradient `Rectangle` | — |
| L1 Celestial | ≤18 stars (6 twinkling), crescent moon at (380,120) r≈30 | ±2 px |
| L2 Cloud streaks | 3 horizontal soft streak `Path`s (y 160–320), `FillRule=Nonzero`, slow ±30px X oscillation (90–150s, AutoReverse — **no wrap traversal**) | ±2 px |
| L3 Town strip | One path: low rooflines + 3 coconut-palm clusters (y 300–380), full width | ±4 px |
| L4 Facade | Wall (vertical gradient + 4–5 plank seam lines), fascia, awning (14 scalloped stripes with a sag curve + shadow band beneath), bracket sign group, painted decorative band behind the card zone, doorway, display window, bulb string | ±7 px |
| L5 Foreground props | Tricycle, sack stack + cat, potted plants, broom, curb | ±13 px |
| L6 Pavement & reflections | Pavement gradient, light pools, vertical reflection streaks, 2 sheen rects (slow 0.25↔0.4 opacity loops) | — |
| L7 Fireflies | ≤8, lower-left/lower-right pockets (3 clocks each — pulse + drift X/Y) | — |
| L8 DimOverlay | Sleep dim (unchanged from UX-48) | — |

### The hero set-pieces (and their light contracts per D4)

1. **Bracket sign** (left zone, plate ≈ x[300,520] y[350,430]): rounded plate, "VILLON" large /
   "FARM SUPPLY" small, warm halo ellipse behind, two chain lines to a wall bracket, whole group
   swaying ±1.2° (6s). Casts: halo + faint warm gradient on the wall behind + a faint pavement
   reflection. **Readable at all times — never behind the card.**
2. **Open doorway** (right zone, ≈ x[1080,1230] y[500,760]): the brightest light. Interior =
   vertical warm gradient; inside it, dark shelf/sack silhouettes (counter-light). Casts: glow
   gradient up the wall + awning underside, an elliptical pool on the pavement, and the strongest
   reflection streak. Success beat pulses this glow once (+20%, 350ms, AutoReverse).
3. **Display window** (left zone, ≈ x[300,520] y[560,730]): slightly cooler/dimmer amber. Inside,
   silhouettes: a pyramid of feed sacks, a bottle row on a shelf line, a **hanging balance scale**
   (the classic tindahan scale). Casts its own wall gradient + pavement pool + reflection.
4. **Bulb string**: catenary curve across the awning edge from (250,455) to (1350,470), 9 bulbs —
   core + radial halo each. Bulbs 4–6 sit behind the glass card and become warm bokeh in the blur
   (this is the glass payoff). Up to 6 bulbs twinkle gently (±0.12 opacity, 3–5s, frame-capped).
   The string is the star of the **entrance choreography** (below).
5. **Tricycle** (≈ x[270,560] y[690,870]): the identity anchor. Pure near-black silhouette of a
   Philippine tricycle (motorcycle + roofed sidecar), built from circles + paths — plus exactly two
   warm rim-light strokes on the door-facing edges. No interior detail (D5).
6. **Cat** on the sack stack (≈ 1280, 610): small silhouette; tail-flick one-shot every ~18s
   (DispatcherTimer, like the blink pattern); sleeping variant (curled, no flick) in LateNight.

### Starter palettes (tunable; all hex lives in `Themes/LoginScene.xaml`)

Element axis (16): SkyTop, SkyUpper, SkyLower, SkyHorizon, CloudStreak, Town, FacadeWall,
FacadeBase, AwningGreen, AwningCream, SignPlate, GlowCore, GlowWarm, Pavement, PavementFar,
Reflection. Naming: `Scene{Mood}{Element}Color` (48 tokens) + phase-independent brushes
(SignText, BulbCore/BulbHalo, TricycleFill/TricycleRim, PropSilhouette, CatFill, PlantFill,
Star/Moon/Firefly, ChainStroke, CurbLine — ~18) + avatar tokens (~20).

- **Dusk:** sky `#1C2547 → #33305E → #7A4A6B → #C96F5A`; clouds `#3A3458`; town `#221F38`;
  wall `#4A3D52` → base `#3A3046`; awning `#3E6B4F` / `#D8CDB4`; glow core `#FFD98A`, warm
  `#E8923A`; pavement `#2E2A38 → #201D2B`; stars 0.35, moon 0.5, bulbs 0.7, fireflies on.
- **Evening:** sky `#0B1026 → #141B38 → #232B4E → #3A3458`; everything two steps darker;
  glows at full (`#FFDF9E` core, halos 0.85); stars 1.0, moon 1.0; bulbs 0.9; reflections strongest.
- **LateNight:** sky from `#070B1C`; door glow at 0.4 (night-light), window 0.25; every other
  bulb off, the rest 0.5; sign halo 0.5; fireflies capped to ~6 via layer opacity; cat asleep.

### The frosted-glass card

- Card stack: chrome `Border` (existing `CardShadow`, 1px `LoginGlassEdgeBrush`, large radius,
  `CardShakeTr` kept) → inner `Grid` with rounded-rect `Clip` (set in code; **re-set on
  `SizeChanged`** — the DA6 panel changes card height) → layers:
  1. `GlassBackdrop` `Rectangle` — `Fill` = **local, unfrozen** `VisualBrush` built in code-behind:
     `Visual` = the scene's `SceneRoot` (expose `DynamicSceneCanvas.SceneVisual`; **never sample an
     ancestor of the card — VisualBrush self-reference**), `ViewboxUnits=Absolute`, `Viewbox` = the
     card's rect mapped into SceneRoot coordinates via `TransformToVisual` (window is `NoResize`;
     compute on Loaded + on card `SizeChanged`). `Effect` = `BlurEffect` Radius 18,
     `RenderingBias.Performance`.
  2. Tint `Rectangle` — `{DynamicResource LoginGlassTintBrush}` (new token: Light ≈ 75% white,
     Dark ≈ 65% deep slate; tune until `TextPrimaryBrush` on the tint passes the UX-00 4.5:1 floor
     against the worst case behind it — the doorway glow).
  3. 1px top highlight line — `{DynamicResource LoginGlassHighlightBrush}`.
  4. The existing form `StackPanel`, markup/bindings/tab order/DA6 panel untouched.
- The shake animates the whole card while the `Viewbox` mapping stays fixed — at 12px amplitude
  under an 18px blur this is imperceptible; **do not re-map during the shake.**
- **Static mode** (reduced motion or `RenderCapability.Tier < 2`): `GlassBackdrop` collapsed, card
  background set to `SurfaceBrush` via `SetResourceReference` — exactly today's solid card. The
  Dark-theme `LoginSceneScrimBrush` rectangle between scene and card stays.
- Avatar badge chrome: warm gradient ring (`#FFC868 → #C9697E` starter), 2px white inner ring,
  soft drop shadow — replaces the flat blue circle.

### The avatar — "Aling Vi", the tindera

New control `Views/Login/TinderaAvatar.xaml(.vb)`; **the public API, pose-enum member names, VSM
state names, and `x:Name="Avatar"` are identical to `CarabaoAvatar`**, so
`LoginView.xaml.vb` compiles unchanged: `Initialize/Shutdown/Pause/[Resume]`, `GoToPose`,
`CurrentPose`, `SetGaze/ResetGaze`, `SetCapsAlert`, `PlayRejected(thenPose)`,
`PlaySuccessBeatAsync()`; `AvatarPose` keeps Idle/Watching/Shy/Peeking/Thinking/Happy/Rejected/
Sleeping. `CarabaoAvatar.xaml(.vb)` is **deleted**.

Bust spec (flat illustration, Duolingo/Headspace quality bar — proportions are binding, they are
the D8 guard):

- Face: warm tan oval (`#C68E6A` starter), eye line at ~55% of face height, eyes wide-set and
  large; **each eye gets a 1px white catchlight dot** (non-negotiable — this is what makes flat
  eyes read alive); soft blush ellipses ~30%; tiny arc nose; no shading on skin.
- Hair: deep brown-black side-parted front + low bun; 3 tiny white dots at the bun (sampaguita);
  small gold hoop earrings (2 thin arcs).
- Dress: cream blouse, green gingham **apron** (base + 2–3 lighter grid lines), folded checkered
  **panuelo** on one shoulder.
- **Hands** (new geometry, VSM-driven only — never code-animated, so the two systems can't fight):
  mitten-style palms with a thumb notch + 2 short finger grooves; parked below the badge clip at
  idle (translate only — no opacity juggling).

Pose matrix (VSM zero-duration storyboards + `MotionDurationStd`/`MotionEasing` transition, via
`GoToElementState` — the UX-48 mechanism):

| State | Look |
|-------|------|
| Idle | Hands parked, gentle smile; breath loop + randomized blink (ported); every 3rd blink → small brow bounce (replaces the ear flick) |
| Watching | Head turns ~2° toward the card; pupils track the username caret (`SetGaze` ported 1:1) |
| Shy | **Both hands rise and cover her eyes**; lids closed beneath; head dips 2°; smile shrinks |
| Peeking | Left hand stays; right hand slides down + rotates out ~14°; right lid 0.15 squint, right pupil up 1px (UX-48 peek values) |
| Thinking | Pupils up-left, brows up; mouth = small dash; gentle head-bob loop (reuses the chew-clock slot) |
| Happy | Arc-closed happy eyes + open smile; both hands raise outward cheek-level ("yay"); double bounce ported |
| Rejected | Brows angled, sad mouth, head tilt 2°, lids 0.5 droop; auto-recovers (1.2s timer ported) |
| Sleeping | Lids closed, head tilt 4°, floating Zzz (ported); scene dims (existing) |

### Entrance choreography (one-shot, ~1.1s, skipped entirely in static mode)

1. t=0: scene opacity 0.55 → 1 (600ms ease-out).
2. t=120ms: **the bulbs light up left → right** (9 staggered 70ms one-shot opacity pops), sign halo
   last — "the store turns on for you."
3. t=250ms: card rises 14px → 0 + fades in (420ms; a dedicated `CardEntranceTr` composed with the
   shake transform).
4. t=420ms: badge scales 0.86 → 1.0 with slight `BackEase` overshoot.

Success beat = existing avatar bounce + one doorway-glow pulse. Reject, CapsLock, sleep/wake,
pause-on-deactivate: unchanged from UX-48.

### Optional accent (cut freely — lean rule)

One caption line under the wordmark with a real-clock Filipino greeting ("Magandang umaga / hapon /
gabi!") — uses `DateTime.Now`, not the mood (moods no longer track literal hours). Single
`TextBlock`, three string literals. This is brand voice, not localization scope (P9 stays dropped).
Owner may veto at review with zero ripple.

## Engineering constraints (carried from UX-48 — binding)

- Animated fills are **local unfrozen brushes built in code**; resource `Color` tokens are seeds
  only. Never feed a Brush token into a `Color` property.
- All loops are tracked `AnimationClock`s in the existing list/lifecycle
  (`Start(staticMode)/Pause/[Resume]/StopAll`); `FullStop()` on hide/close/success stays mandatory
  (LoginView is DI-transient and hidden, never closed). `[Resume]` keeps its brackets.
- `Timeline.SetDesiredFrameRate` ≤30 on every ambient loop (twinkles may use 24); total concurrent
  ambient clocks ≤ ~45 (UX-48 parity; birds are gone, bulbs/sheen/sign-sway take their budget).
- `AmbientFrameRate` stays the single CPU tuning knob; blur Radius/`RenderingBias` are the two
  glass knobs; the documented degrade ladder is: lower frame caps → blur 18→12 → static fallback.
- VB traps: `Namespace Views.Login` (suffix only); full root prefix in any new `clr-namespace`;
  no `Await` in `Catch`/`Finally`; lambda params must not shadow locals; no reserved-word locals.
- Scene `TextBlock`s use `AppFontFamily`; **zero hex in `Views/`** (everything in
  `LoginScene.xaml` / theme files); `DynamicResource` for theme-dependent brushes.
- `IsHitTestVisible=False` scene; automation tree untouched; CapsLock badge + error LiveSettings
  preserved; auth flow, DA6, `LoginViewModel`, and `Application.xaml.vb` are **not modified**.

## Deliverables

| File | Action |
|------|--------|
| `Themes/LoginScene.xaml` | Rewrite — mood/element tokens + avatar/badge/glass-adjacent statics (~86 tokens) |
| `Views/Login/LoginScenePhase.vb` | Rewrite — enum `Dusk/Evening/LateNight`, bands 05–19 / 19–23 / 23–05; doc comment explains hero-hour art direction |
| `Views/Login/DynamicSceneCanvas.xaml` | Rewrite — full storefront layer tree per composition map |
| `Views/Login/DynamicSceneCanvas.xaml.vb` | Rewrite — mood palette struct, brush wiring, new ambient set (streak drift, sign sway, bulb twinkle, sheen shimmer, fireflies, cat timer), `PlayEntrance()`, `PulseDoorGlow()`, `SceneVisual` property, 4 parallax groups; same clock bookkeeping |
| `Views/Login/TinderaAvatar.xaml` | New — badge + bust + 8 VSM states per pose matrix |
| `Views/Login/TinderaAvatar.xaml.vb` | New — ported lifecycle/blink/gaze/bounce/zzz; chew-slot → head-bob; ear-flick → brow bounce |
| `Views/Login/CarabaoAvatar.xaml` + `.xaml.vb` | **Delete** |
| `Views/LoginView.xaml` | Modify — `TinderaAvatar` swap (x:Name stays `Avatar`), glass card stack, entrance transform; form markup otherwise untouched |
| `Views/LoginView.xaml.vb` | Modify — glass mapping (Loaded/SizeChanged), entrance trigger in `EnsureStarted`, success → door pulse; all UX-48 wiring kept |
| `Themes/Light.xaml` + `Themes/Dark.xaml` | Add `LoginGlassTintBrush`, `LoginGlassHighlightBrush` (keep `LoginSceneScrimBrush`) |
| `ROADMAP-L3-pro.md` | P13a entry (done at plan time) |

Plus: progress summary at `Progress/VISTA_Modules/Experience/UX-49-summary.md`, agent-wiki update
(extend `wpf-vista-animated-scene` with the glass-backdrop technique + the EvenOdd/safe-area
defect record).

## Acceptance criteria

1. Build: 0 errors / 0 warnings.
2. `VISTA_LOGIN_SCENE_HOUR` = 10 / 20 / 1 → Dusk / Evening / LateNight, each individually
   demo-grade; crossfade between moods is seamless.
3. No readable element is cropped at 900×700 (D2) and no geometry shows overlap holes (D1).
4. Glass card: scene visibly drifts blurred behind the form (watch bulbs 4–6); text contrast ≥
   4.5:1 in both themes; DA6 panel growth re-clips and re-maps cleanly; static mode yields the
   solid card with zero blur cost.
5. All 8 tindera poses fire per the UX-48 wiring matrix (gaze, shy hands, peek, caps brows,
   thinking, happy bounce ≤700ms swap, reject + shake, sleep/wake).
6. Entrance choreography plays once per show; never in static mode; never on the hidden instance.
7. Teardown: `StopAll` + avatar `Shutdown` confirmed on hide (UX-48 breakpoint check) — no clock,
   timer, or VisualBrush survives on a hidden LoginView.
8. Idle CPU comparable to UX-48 baseline; degrade ladder documented in the summary.
9. Both themes × three moods × reduced-motion booted and screenshotted for the summary.
