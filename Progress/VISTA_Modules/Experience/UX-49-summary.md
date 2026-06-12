---
module: MerchSys.App
agent: claude-code
date: 2026-06-12
plan-ref: Plans/VISTA_Modules/Experience/49-login-storefront-hero.md
status: completed
---

# UX-49 — Login Scene Redesign: Dusk Storefront Hero Shot + Tindera Avatar + Frosted-Glass Card

## Task Summary

Replaced every pixel of the UX-48 login presentation after the owner's review ("no wow"; carabao
out, Filipino human avatar in) while keeping the UX-48 engineering contracts byte-compatible. The
login now renders a **dusk storefront hero shot** — the Villon Farm Supply facade at blue hour:
hanging lit bracket sign (swaying ±1.2°), scalloped green/cream awning, a 9-bulb string whose
middle bulbs glow as bokeh through the card, an open doorway and display window spilling warm light
(each with wall gradient, pavement pool, and wet reflection), distant town + coconut-palm
silhouettes, parked tricycle / sack stack / cat / potted plant / walis as rim-lit silhouettes,
fireflies, and a wet-pavement sheen. Real time of day survives as **three lighting moods** (Dusk
hero 05:00–18:59 · Evening 19:00–22:59 · LateNight 23:00–04:59 — game-title-screen art direction:
no clock hour can render a weak palette). **Aling Vi the tindera** replaces Tanod with the
identical pose API — she covers her eyes with both hands during password entry and peeks between
fingers on reveal. The card is now **live frosted glass** (blurred VisualBrush of the scene under a
theme tint), with an **entrance choreography**: the scene brightens, the bulb string lights up
left-to-right, the card rises, the badge pops. Login success additionally pulses the doorway glow
("welcome in"). Auth flow, DA6, `LoginViewModel`, and `Application.xaml.vb` are untouched.

**Plan:** `[[49-login-storefront-hero]]` (owner-locked decisions: scene C / avatar B / glass A)
**Branch:** master (per session instruction)

## What Was Done

- Rewrote `Themes/LoginScene.xaml` — 48 mood tokens (`Scene{Dusk|Evening|LateNight}{Element}Color`,
  16 elements × 3 moods), 18 mood-independent scene brushes (bulb/moon halos, sheen, awning shadow,
  silhouette fills), 24 avatar/badge tokens. **Every hex of the feature lives here.**
- Rewrote `Views/Login/LoginScenePhase.vb` — enum members now `Dusk / Evening / LateNight` with the
  hero-hour bands; doc comment records the art-direction decision. `VISTA_LOGIN_SCENE_HOUR`
  override unchanged (10 → Dusk, 20 → Evening, 1 → LateNight).
- Rewrote `Views/Login/DynamicSceneCanvas.xaml` — full storefront layer tree per the plan's
  composition map (sky → celestial → cloud streaks → town strip → facade → pavement/reflections →
  foreground props → fireflies → dim overlay). Defect-record gates applied: `F1`/`Nonzero` on every
  multi-figure filled path (D1), all readable elements inside the x∈[260,1340] safe area (D2 — the
  sign is fully readable left of the card), every light source ships halo + cast gradient + ground
  pool + reflection (D4), props are silhouettes + ≤2 warm rim strokes (D5).
- Rewrote `Views/Login/DynamicSceneCanvas.xaml.vb` — 14 unfrozen code-built brushes (4-stop sky,
  wall/pavement/glow gradients, pool/spill/halo radials, shared reflection gradient); ~25 color +
  ~24 double crossfade targets per mood change; **base values re-set after starting crossfades** so
  `FillBehavior.Stop` one-shots never snap (new pattern §8); ambient set = 3 cloud oscillations
  (±28px AutoReverse — no wrap), sign sway, 6 bulb twinkles, 6 star twinkles, 2 pavement sheens,
  8 fireflies × 3 clocks (42 loops ≈ UX-48 budget; birds/palay retired); cat tail-flick on a
  12–21s `DispatcherTimer` (suppressed in LateNight where the cat sleeps); 5-layer parallax
  (±2/±2/±4/±7/±13); new public surface: `SceneVisual` (glass sampling), `PlayEntrance()`,
  `PulseDoorGlow()`. Lifecycle (`Start/Pause/[Resume]/StopAll`), 60s phase timer, clock
  bookkeeping, and static mode are UX-48's verbatim.
- Created `Views/Login/TinderaAvatar.xaml(.vb)` / **deleted `CarabaoAvatar.xaml(.vb)`** — same
  public API, `AvatarPose` enum, VSM state names, and transform-ownership discipline, so
  `LoginView` wiring compiled unchanged. New geometry: warm-tan face with wide-set eyes +
  **catchlights** (D8), side-parted hair with low bun + sampaguita dots, gold hoop earrings, cream
  blouse + green gingham apron + checkered panuelo, mitten hands (VSM-only, parked below the badge
  clip at Y=78). Pose deltas: Shy = both hands rise over the eyes; Peeking = right hand slides
  down/out 16°; Happy = hands raise beside cheeks + bounce; Thinking = head-bob loop (reuses the
  chew-clock slot); brow-bounce every 3rd blink (replaces ear flick; suppressed while the CapsLock
  alert holds the brows). Badge: warm amber→rose gradient ring + white inner ring + theme outer.
- Modified `Views/LoginView.xaml` — glass card stack (`CardChrome` border 1px
  `LoginGlassHighlightBrush` + `RadiusLarge` + `CardShadow`; inside: `GlassBackdrop` rect with
  `BlurEffect 18/Performance`, `GlassTint` rect, then the form). Form markup, bindings,
  `PasswordBoxHelper`, tab order, CapsLock badge, error LiveSetting, and the DA6 panel are
  byte-identical. Avatar swapped to `TinderaAvatar` (x:Name `Avatar` kept); entrance transforms
  added (`CardEntranceTr`; unnamed `ScaleTransform` on the avatar — MC3093, see Issues).
- Modified `Views/LoginView.xaml.vb` — `SetupGlassCard()` (VisualBrush absolute-viewbox of
  `SceneCanvas.SceneVisual`; static mode collapses the backdrop and swaps tint/border to
  `SurfaceBrush`/`SeparatorBrush` = the old solid card); `UpdateGlassViewbox()` on `CardChrome.
  SizeChanged` (covers the DA6 panel growing the card; never per-frame); `PlayCardEntrance()`
  (keyframed card rise/fade + badge pop, all `FillBehavior.Stop` over final base values);
  `PlaySuccessBeatAsync` now also fires `PulseDoorGlow()`. All UX-48 wiring (gaze, poses, caps,
  shake, sleep, pause/resume, `FullStop()` teardown) untouched.
- Modified `Themes/Light.xaml` + `Themes/Dark.xaml` — `LoginGlassTintBrush` (Light: white 78%;
  Dark: #23252E 74%) and `LoginGlassHighlightBrush` (Light: white 65%; Dark: white 14%), key
  parity per the UX-00 contract. `LoginSceneScrimBrush` kept.
- Registered **P13a** in `ROADMAP-L3-pro.md` (full entry) and the consolidated map in
  `ROADMAP-ui-ux-perfection.md`; extended agent-wiki pattern `wpf-vista-animated-scene` (§7–§10)
  + index/log.

No NuGet packages, no bitmaps, no new user-facing settings. The plan's **optional greeting accent**
("Magandang umaga/hapon/gabi!") was **cut** per the lean rule and the P9 English-only descope —
it is a one-TextBlock add if the owner wants it back.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors / 0 warnings (final) |
| Unit tests pass | N/A (no test projects, per project rules) |
| Manual verification | ⏳ Pending operator session (steps below) |

### Realization-check steps for the operator session

1. `setx VISTA_LOGIN_SCENE_HOUR 10` (then 20 / 1) → launch → verify **Dusk** (blue-hour sky, all
   lights warm) / **Evening** (full stars, brighter interior contrast) / **LateNight** (interior
   night-light, every other bulb off, cat curled asleep); clear the variable after. Each mood must
   look demo-grade on its own — that is the acceptance bar.
2. Glass: watch the scene **drift blurred behind the form** (bulbs 4–6 read as warm bokeh through
   the card); confirm text contrast in **both themes**; trigger DA6 (first-login account) and
   confirm the growing card re-clips/re-maps with no smearing or offset.
3. Entrance: every show of the login window plays scene-brighten → bulbs light left-to-right →
   card rises → badge pops (~1.1s); logout → re-login replays it; reduced motion shows none of it.
4. Avatar walk: username focus + typing (gaze tracks caret), password focus (hands cover eyes),
   eye-toggle (right hand drops to peek), CapsLock on (badge + brow raise; clears on release),
   wrong password (card shake + sad beat, identical for unknown user vs wrong password), correct
   login (door-glow pulse + grin/bounce, MainWindow ≤ 700 ms), idle 45 s (sleep + dim; any key wakes).
5. Reduced motion (OS animation effects off) or render Tier < 2: static current-mood scene, posed
   avatar, **solid** card (SurfaceBrush + SeparatorBrush border, no blur cost), CapsLock badge +
   error text functional, instant success swap, no shake/entrance.
6. Crop check at 900×700: the bracket sign, tricycle, sack stack + cat, and doorway are fully
   visible (defect D2 regression guard).
7. Teardown: login → logout → on the new LoginView confirm only IT animates; optionally breakpoint
   `DynamicSceneCanvas.StopAll` to confirm it ran for the hidden instance. CPU glance in Task
   Manager during idle ambient; knobs if needed: `AmbientFrameRate` (30 → 24/20), blur `Radius`
   (18 → 12), and static mode as the floor.
8. WCAG 2.2.2/2.3.3: the OS "animation effects" setting remains the pause/disable mechanism;
   bulb/firefly pulses are slow fades, nowhere near 2.3.1 flash thresholds.

## Issues Encountered

- **BC30105 — local array shadows method (new wiki note, pattern §10):** `Dim bulbGroups() As
  Canvas = BulbGroups()` parses the initializer as indexing the not-yet-assigned local (VB.NET
  case-insensitivity), not as the method call. Renamed the local. Same family as the known
  parameter/lambda shadowing traps.
- **MC3093 — x:Name inside another control's namescope (new wiki note, pattern §10):** naming the
  `ScaleTransform` placed in `<login:TinderaAvatar.RenderTransform>` from LoginView.xaml is
  illegal. Left it unnamed; `PlayCardEntrance` fetches it via `CType(Avatar.RenderTransform,
  ScaleTransform)`.
- **Caught pre-build:** a duplicate `Data` (attribute + property element) on `PalmLeft`, and
  missing `F1` prefixes on overlapping-figure paths (palm fronds, plant blades, awning, cat ears) —
  the exact EvenOdd defect class this redesign exists to kill.
- **Design judgment call:** static mode also reverts the card border to `SeparatorBrush` so the
  fallback is pixel-faithful to the pre-UX-49 solid card, not a glass border over a solid fill.
- **Mid-entrance Viewbox mapping:** if `SizeChanged` fires while the card-rise transform is
  in flight, the glass sample region bakes a ≤14px vertical offset — invisible under an 18px blur
  and corrected by any later layout pass; deliberately not re-mapped per-frame.

## What's Next

- [ ] Operator realization pass (steps above) — three moods × both themes × reduced-motion, DA6
      re-clip, crop check, teardown breakpoint, CPU glance.
- [ ] If glass contrast reads low on the store laptops, raise `LoginGlassTintBrush` opacity
      (Light 0.78 / Dark 0.74 starters) — the single contrast knob.
- [ ] Optional accents explicitly cut (one-liners if wanted): Filipino greeting caption;
      shake-on-empty-submit (carried open from UX-48).

## Cross-References

- Domain Wiki pages consulted: `[[client-server-wpf]]`, UX-00 token contract via
  `ROADMAP-ui-ux-perfection.md` cross-cutting rules.
- Agent Wiki entries consulted: `[[wpf-vista-animated-scene]]` (UX-48 recipe — extended, not
  replaced), `[[wpf-vista-motion]]`, `[[wpf-vsm-foreground-on-non-control-template-root]]`,
  `[[wpf-dynamicresource-brush-into-color-property]]`, `[[vbnet-lambda-param-shadows-local-variable]]`,
  `[[vbnet-parameter-shadows-property]]`.
- Agent Wiki entry updated: `[[wpf-vista-animated-scene]]` §7 frosted glass, §8 base-after-
  crossfade, §9 vector-art quality gates (defect record D1–D8), §10 MC3093 + BC30105.

### codebase_wiki discrepancies (for Antigravity sync)

- `Views/Login/CarabaoAvatar.xaml(.vb)` **deleted**; `Views/Login/TinderaAvatar.xaml(.vb)` added
  (App file count unchanged at 160).
- `LoginScenePhase` enum members renamed to `Dusk/Evening/LateNight` (3 bands, was 5).
- `DynamicSceneCanvas` public surface grew: `SceneVisual`, `PlayEntrance()`, `PulseDoorGlow()`.
- `LoginView` gained `_glassBrush`/glass members; card structure changed from `CardStyle` Border to
  the `CardChrome` glass stack; `PlaySuccessBeatAsync` now pulses the door glow.
- `Light.xaml`/`Dark.xaml` gained `LoginGlassTintBrush` + `LoginGlassHighlightBrush` (parity).
- `Themes/LoginScene.xaml` token set fully replaced (mood-based naming).
