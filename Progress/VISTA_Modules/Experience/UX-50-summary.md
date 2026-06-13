---
module: MerchSys.App
agent: claude-code
date: 2026-06-13
plan-ref: Plans/VISTA_Modules/Experience/50-login-raster-hero.md
status: completed
---

# UX-50 — Login Scene: Painterly Raster Hero (replaces the procedural vector scene)

## Task Summary

The owner rejected UX-49's hand-coded **vector** storefront as unable to reach the reference
`Expectation.jpg`. Root cause was the **medium**: procedural shapes + UX-49's own D5 "silhouettes,
zero interior detail" rule are the opposite of a dense, warm, *smooth painterly* illustration. UX-50
switches the login hero to a **raster painterly illustration** rendered via `<Image>` (WPF draws it
at native fidelity → the login looks exactly as good as the source art), then **removes** the entire
UX-48/49 procedural engine now that it is dead weight.

Art lane was decided after exploring three: CC0 pixel (pixelmateai Market Stalls — verified but too
cute as a hero), richer pixel (guttykreum Japanese Corner Store / LimeZu Modern Exteriors — still
pixel), and **generated painterly** — chosen, because the reference is smooth painterly lo-fi.

## What Was Done

- **Generated the hero via Canva.** Adobe's MCP connector reports text-to-image is disabled in this
  environment; **Canva `generate-design`** (design_type `desktop_wallpaper`) works. Pipeline:
  `generate-design` → `create-design-from-candidate` → `export-design` (png 1600×900) → download the
  presigned `export-download.canva.com` URL. Four demo-grade candidates resulted; the owner chose
  **Candidate 1, "Cozy Sari-Sari Store at Dusk"** (Canva design `DAHMb96QyQM`) — warm bulb-lit
  storefront on the right, blue-hour sky + skyline + crosswalk on the left, clear center for the card.
- **Placed the asset:** `Assets/Login/login-hero-dusk.png` (1600×900) + `Assets/Login/CREDITS.md`
  (Canva Content License, design IDs, prompt). Alternates kept in `Screenshots/Login_Hero_Candidate*.png`.
  `.vbproj` gained `<Resource Include="Assets\Login\*.png"/>` (mirrors the existing Inter-font pattern).
- **Wired it (v1):** inserted one opaque `<Image x:Name="HeroImage">` (`BitmapScalingMode=HighQuality`,
  `UniformToFill`) as the scene, beneath `DimOverlay`. The frosted-glass `VisualBrush` now samples the
  painting (its lit windows blur into real bokeh); the entrance scene-fade and sleep-dim still apply.
- **Pruned the dead engine.** With the painting carrying the scene, the UX-48/49 machinery was
  removed: `DynamicSceneCanvas.xaml` is now `HeroImage` + `DimOverlay`; `DynamicSceneCanvas.xaml.vb`
  shrank **~700 → ~95 lines** — the public surface (`Start`/`Pause`/`[Resume]`/`StopAll`/
  `PlayEntrance`/`PulseDoorGlow`/`SetDimmed`/`SceneVisual`) is byte-preserved so **`LoginView` is
  untouched**; `Views/Login/LoginScenePhase.vb` was deleted. The 60s mood clock, 4s palette
  crossfades, ~40 ambient animation clocks, the cat-tail timer, and the cursor parallax (all of which
  had been running on now-hidden layers) are gone — CPU reclaimed.
- `TinderaAvatar`, `LoginView.xaml(.vb)`, the glass-card stack, DA6, auth, and `Application.xaml.vb`
  are unchanged. The avatar (Aling Vi) and all UX-48/49 mascot/CapsLock/shake/sleep wiring still fire.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds (`dotnet build MerchSys.slnx`) | ✅ 0 errors / 0 warnings |
| Unit tests | N/A (no test projects, per project rules) |
| Manual verification | ⏳ Pending operator run (steps below) |

### Realization-check steps for the operator session
1. Launch the app (`dotnet run --project src/MerchSys.App`, XAMPP/MariaDB up). Confirm the login shows
   the painterly storefront edge-to-edge, the **frosted-glass card blurs the painting** (lit windows
   read as warm bokeh), and the card/avatar sit over the clear center-left.
2. Entrance: the scene fades up once on show; logout→re-login replays it; reduced-motion shows none.
3. Avatar walk (unchanged from UX-49): username gaze, password hands-over-eyes, eye-toggle peek,
   CapsLock badge, wrong-password shake, success bounce, 45s sleep + scene dim, any key wakes.
4. Reduced motion / render Tier < 2: solid card (no blur), static hero, no entrance — instant success.
5. Teardown: login→logout, confirm the hidden LoginView animates nothing (no surviving clocks/timers).

## Issues Encountered

- **Adobe gen disabled:** the connector's routing doc lists text-to-image as unavailable in this
  environment (only `image_generative_expand`). Canva was the available generator — recorded so the
  next agent doesn't retry Adobe for image generation.
- **Canva thumbnail URLs 403:** raw `design.canva.ai` thumbnails aren't fetchable; the
  `export-design` presigned URL is. Materialize → export → download is the reliable preview path.
- **No build traps hit** — the prune was additive-then-subtractive XAML + a small code-behind rewrite
  that preserved the public surface, so no VB antipattern (namespace/shadowing/reserved-word) applied.

## What's Next

- [ ] Operator realization pass (steps above) — first true in-app confirmation.
- [x] Motion increment (2026-06-13, builds 0/0): Ken-Burns zoom/pan + cursor parallax on `HeroLayer`;
      warm window + bulb glows (`LoginHeroGlowBrush`, registered, ride the transform) with bulb
      flicker, login-success pulse, and entrance fade-in; ~6 Pause/Resume/StopAll-tracked clocks;
      static mode skips all motion.
- [x] Composition + live verification (2026-06-13, builds 0/0): re-framed the hero (zoom + pan) so the
      storefront is prominent on the right; shifted the login card LEFT (`LoginView` Grid Center→Left)
      so the storefront shows unobstructed — the Expectation.jpg layout; re-centered the glows. All
      ambient motion (cursor parallax, Ken-Burns drift, bulb-glow flicker) confirmed ACTIVE via a live
      frame-diff capture (scene regions move, card region at noise floor). Glass blur + alignment also
      re-confirmed live (refuting an audit that claimed they were broken). Side-by-side vs the
      reference: `Screenshots/Login_vs_Expectation.png`.
- [x] Time-of-day moods (2026-06-13, builds 0/0): a deep-indigo `GradeOverlay` darkens the same hero
      per real-clock band (Dusk 0 / Evening 0.32 / LateNight 0.52, 3.5s crossfade, 60s phase timer);
      the glows ride above the grade so the storefront stays lit; `VISTA_LOGIN_SCENE_HOUR` override
      restored. All three verified live — `Screenshots/Login_moods.png`.
- [x] Time-of-day moods (done — see the checked item above): grade overlay per real-clock band.
- [x] Token prune (2026-06-13): `Themes/LoginScene.xaml` trimmed 86 → 28 tokens — dead mood-palette
      colors + vector-scene brushes removed; kept the hero glow/grade, `SceneDimBrush`, and the 25
      avatar tokens. Build 0/0; avatar + scene render confirmed live.

## Cross-References

- Plan: `[[50-login-raster-hero]]` (P13b). Supersedes the vector direction of `[[49-login-storefront-hero]]`.
- Roadmap: P13b registered in `ROADMAP-L3-pro.md` + `ROADMAP-ui-ux-perfection.md`.

### codebase_wiki discrepancies (for Antigravity sync)
- `Views/Login/DynamicSceneCanvas.xaml(.vb)` rewritten — now a raster hero (`HeroImage`) + `DimOverlay`
  only; code-behind ~95 lines; public surface unchanged.
- `Views/Login/LoginScenePhase.vb` **deleted** (the `LoginScenePhase` enum / `LoginScenePhaseProvider`
  are gone); UX-50 later re-introduced a lighter `MoodPhase` enum + the `VISTA_LOGIN_SCENE_HOUR`
  override *inline* in `DynamicSceneCanvas.xaml.vb` to drive the raster grade moods.
- New: `Assets/Login/login-hero-dusk.png`, `Assets/Login/CREDITS.md`; `.vbproj` Resource include.
- `Themes/LoginScene.xaml` **pruned 86 → 28 tokens** (dead mood-palette colors + vector-scene brushes
  removed); live keys = `LoginHeroGlowBrush`, `LoginHeroGradeBrush`, `SceneDimBrush`, + 25 `Avatar*`.
- `LoginView.xaml`: the card + avatar Grid is now `HorizontalAlignment=Left` (was Center) so the card
  sits over the open dusk sky and the storefront shows unobstructed. `login-hero-dusk.png` is a
  reframed crop (storefront-right); `DynamicSceneCanvas` gained `HeroLayer` (Ken-Burns + parallax
  transform) + `GlowWindow`/`GlowBulbs` ellipses + a `GradeOverlay` rectangle.
- `DynamicSceneCanvas` re-gained a light mood system (the heavy UX-49 one stays gone): a `MoodPhase`
  enum, a 60s phase timer, the `VISTA_LOGIN_SCENE_HOUR` debug override, and a per-band grade crossfade;
  `Themes/LoginScene.xaml` gained `LoginHeroGradeBrush`.
