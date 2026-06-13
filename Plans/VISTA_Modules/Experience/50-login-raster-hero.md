---
module: MerchSys.App
plan-id: UX-50
title: "Login Scene — Raster Cozy-Pixel Hero (CC0 storefront over the kept gradient sky)"
depends-on: [UX-49]
estimated-files: 10
---

# Login Scene — Raster Cozy-Pixel Hero

> **Level 3 (Pro) — item P13b** of [ROADMAP-L3-pro.md](ROADMAP-L3-pro.md). UX-48 (P13) shipped the
> *machinery*; UX-49 (P13a) re-skinned it with hand-coded **vector** artwork and still missed the
> owner's bar (reference: `Expectation.jpg` — a crafted lo-fi/pixel cozy storefront). This plan
> fixes the **medium**, not the tuning: the hero facade becomes a **raster illustration**, while
> every UX-48/49 engineering contract is kept byte-compatible. **Owner verdict (2026-06-13):**
> direction = **cozy pixel set** (licensed CC0 *tindahan* sprite hero + the existing dusk gradient
> sky, dusk-graded). Style lands firmly in "cozy 2D indie", a touch cuter than `Expectation.jpg`'s
> semi-realistic detail — accepted as the clean-rights, 100%-reliable path.

## Status — 2026-06-13: asset lane finalized (generated painterly) + v1 wired (build 0/0)

The asset search walked three lanes: **CC0 pixel** (pixelmateai Market Stalls — verified, but each
stall is ~36×25px and reads too cute as a hero, even composed as a market row); **richer pixel**
(guttykreum *Japanese Corner Store* $7 / LimeZu *Modern Exteriors* $2.50 — detailed but still pixel,
and `Expectation.jpg` is not pixel); and **generated painterly** — chosen, because `Expectation.jpg`
is actually *smooth painterly lo-fi*, which only a generated/painted illustration matches.

- **Generator reality:** the Adobe MCP connector's routing doc states **text-to-image is disabled**
  in this environment (only `image_generative_expand` outpainting works). **Canva `generate-design`**
  *is* available and produced four demo-grade painterly candidates.
- **Pipeline (reusable):** `generate-design` (design_type `desktop_wallpaper`) → pick candidate →
  `create-design-from-candidate` → `export-design` (png, 1600×900) → download the presigned
  `export-download.canva.com` URL (raw thumbnail URLs are 403; the export URL is fetchable).
- **Chosen:** Candidate 1, "Cozy Sari-Sari Store at Dusk" (design `DAHMb96QyQM`) → saved to
  `Assets/Login/login-hero-dusk.png`; `Assets/Login/CREDITS.md` written; alternates kept in
  `Screenshots/Login_Hero_Candidate*.png`.
- **v1 integration (shipped, builds 0/0):** one opaque `<Image x:Name="HeroImage">` (1600×900,
  `BitmapScalingMode=HighQuality`) inserted as the top scene layer **beneath `DimOverlay`**. It fully
  covered the procedural vector scene (kept compiling at that step; removed in the prune below), the
  frosted-glass `VisualBrush` now blurs the painting into real bokeh, and the entrance scene-fade +
  sleep-dim still apply. `.vbproj` gains `<Resource Include="Assets\Login\*.png"/>`.
- **Prune (done — builds 0/0):** the procedural vector scene was removed wholesale.
  `DynamicSceneCanvas.xaml` is now `HeroImage` + `DimOverlay`; `DynamicSceneCanvas.xaml.vb` shrank
  ~700 → ~95 lines (the public surface `Start`/`Pause`/`[Resume]`/`StopAll`/`PlayEntrance`/
  `PulseDoorGlow`/`SetDimmed`/`SceneVisual` is preserved, so `LoginView` is untouched);
  `LoginScenePhase.vb` deleted. The ~40 ambient clocks, cat timer, mood crossfades and cursor
  parallax that were running on hidden layers are gone (CPU reclaimed). The scene is now
  intentionally a **static painterly hero** with a fade-in entrance + sleep-dim.
- **Motion increment (done — builds 0/0):** `HeroLayer` now carries a slow **Ken-Burns** zoom
  (1.07↔1.11, ~55s) + pan and **cursor parallax** (±6px, inside the zoom overscan so no edge shows);
  two warm glows (`LoginHeroGlowBrush`, added to `LoginScene.xaml`) registered to the lit window +
  bulb strip ride the same transform — the bulb glow **flickers**, the window glow **pulses on login
  success** (`PulseDoorGlow`), and the scene **fades in** on entrance. ~6 ambient clocks, all
  Pause/Resume/StopAll-tracked; static mode sets the glows to base and skips all motion.
- **Composition (done, 2026-06-13, live-verified):** the hero was re-framed (zoom + pan) to put the
  storefront on the right, and the login card was shifted LEFT (`LoginView` Grid `HorizontalAlignment`
  Center→Left) so the storefront shows unobstructed — the `Expectation.jpg` layout. Glows re-centered;
  all motion + glass re-confirmed in a live run (frame-diff). `Screenshots/Login_vs_Expectation.png`.
- **Time-of-day moods (done, 2026-06-13, live-verified):** a deep-indigo `GradeOverlay` (inside
  `HeroLayer`, *below* the glows so the storefront stays warm) animates its opacity per real-clock
  band — Dusk 0 / Evening 0.32 / LateNight 0.52, 3.5s crossfade; 60s phase timer; the
  `VISTA_LOGIN_SCENE_HOUR` debug override is restored (10/20/1). All three verified — `Screenshots/Login_moods.png`.
- **Token prune (done, 2026-06-13):** the dead UX-48/49 mood-palette colors + vector-scene brushes
  were removed from `LoginScene.xaml` (**86 → 28 tokens** — kept only `LoginHeroGlowBrush`,
  `LoginHeroGradeBrush`, `SceneDimBrush`, and the 25 `Avatar*` tokens, all reference-verified by grep).
  Build 0/0; login + avatar render confirmed live (no missing-`StaticResource` crash).
- **Acceptance gate 8 (verdict vs `Expectation.jpg`) met in still** (with-card mock); in-app
  confirmation pending an operator run.

The CC0/pixel manifest below is **superseded** by this decision; kept for the audit trail.

## Why UX-49 capped out (the root cause — this is the anti-goal)

UX-49's own rule **D5** mandates *"silhouette-first props — near-black shapes + at most 2 warm
rim-light strokes; **zero mid-tone interior detail**."* `Expectation.jpg` is the **opposite**: it is
*all* interior detail — individual product boxes/bottles on depth-shelves, a wooden counter, a
volumetric light cone from hanging bulbs. Procedural vector primitives + soft gradients cannot
manufacture that density or warmth; the result reads muddy/ballooned (see
`Screenshots/{Dust,Evening,LateNight}_Login_Form.png`). **No amount of vector discipline reaches a
crafted illustration — you must *display* the illustration, not reconstruct it from `Path` geometry.**
WPF draws PNG at native fidelity, so a raster hero's quality *equals* the source art's quality —
a deterministic, 100% outcome. Cozy 2D indie title screens are all painted/pixel raster backgrounds
with a few animated overlay layers; never per-shape vector.

## The concept — keep what already works, replace only the failing band

The scene is **already a layered parallax tree (L0–L8)** built to receive exactly this swap. The
sky is the one part the screenshots show reading *well* — it stays. We replace the **structural**
geometry of the facade with a crisp raster storefront, and **keep the light overlays** (the radial
glow ellipses already in L4 read as *light*, not structure, and composite beautifully over pixel
art — the classic "glow-on-top-of-pixels" cozy trick). Mood (Dusk/Evening/LateNight) survives as a
single dusk-grade overlay that crossfades on the *existing* pipeline.

| Layer | UX-49 contents | UX-50 action |
|-------|----------------|--------------|
| L0 Sky (`SkyRect`) | 4-stop dusk gradient, mood-animated | **KEEP** — carries all three moods for free |
| L1 Celestial (`StarsLayer`, `MoonGroup`) | stars + crescent moon | **KEEP** |
| L2 Clouds (`CloudA/B/C`) | soft streaks, ±28px drift | **KEEP** |
| L3 Town (`TownPath`, `PalmLeft/Right`) | distant skyline + palm **silhouettes** | **KEEP** — silhouettes are the one thing vector does fine; this is the left-zone backdrop band |
| **L4 Facade (`FacadeLayer`)** | wall/planks/awning/sign/door/window/bulbs as **vector** | **REPLACE** structure with `<Image>` raster *tindahan*; **KEEP** the light overlays (`DoorSpill`, `DoorGlow`, `WindowGlow`, a few bulb halos), re-anchored over the raster |
| L5 Pavement (`PavementLayer`) | wet pools, reflections, sheen loops | **KEEP**, re-anchor `WindowPool`/`DoorPool`/reflections under the raster's lit openings |
| **L6 Props (`PropsLayer`)** | tricycle/sack/cat **silhouettes** | **REPLACE** with 2–4 CC0 raster props (potted plants, crate/sack) flanking the card per the reference; keep the `CatSitting`/`CatAsleep` swap (optionally a pixel-cat sprite) |
| L7 Fireflies | ≤8 pulse+drift | **KEEP** |
| L8 `DimOverlay` | sleep dim | **KEEP** |
| **(new) Grade** | — | **ADD** `GradeOverlay` `Rectangle` over L4 (under the card zone): code-built gradient brush whose color crossfades per mood — unifies the raster into Dusk/Evening/LateNight |

### Composition (matches `Expectation.jpg` *and* the existing card-occlusion map)

Storefront occupies the **right** ~55% (raster `StorefrontImage` ≈ x∈[760,1600], grounded at the
curb y≈778); open **sky + town silhouette** fill the **left** ~45%; the centered 384px glass card
overlaps the seam (blurred). This is the reference's framing and the UX-49 safe-area zones unchanged.

## Asset manifest (CC0 / free-commercial — license-verified 2026-06-13)

> **Acquisition is a manual operator step** (itch.io downloads). Place PNGs under
> `Assets/Login/`. Every asset is embedded in-app only (never redistributed standalone), which all
> licenses below permit. A `CREDITS.md` listing each asset/author/URL/license is a **hard deliverable**.

| Role | Asset | Source | License (verified) |
|------|-------|--------|--------------------|
| **Hero storefront** (the *tindahan*) | Pixel Market Stalls (12 stall sprites, awnings + goods, transparent PNG, <128px) | `https://pixelmateai.itch.io/market-stalls` | Free personal **& commercial**, no attribution required, no standalone redistribution |
| Foreground props | a small CC0 plant/crate/sack set (pick to match the stall palette) | itch.io `assets-cc0` + `pixel-art` tag | CC0 (verify per pack) |
| (optional) richer storefront | a detailed RPG-town shop-exterior sprite, if the stall reads too simple at scale | itch.io `pixel-art`+`shop` | verify per pack |

Sky, skyline, moon, clouds, pavement, fireflies, light glows: **no asset needed** — kept from code.

### Resource pipeline (mirror the existing Inter-font precedent)

- `MerchSys.App.vbproj`: add `<Resource Include="Assets\Login\*.png" />` (the project already ships
  fonts this way — `.vbproj` lines 36–41). PNGs build as **`Resource`**, not `Content`.
- XAML: `<Image Source="/Assets/Login/storefront-dusk.png" .../>` (SDK resolves to the pack URI).
  Code-behind (if any `BitmapImage`): absolute `pack://application:,,,/Assets/Login/<file>.png`,
  `CacheOption=OnLoad`, then `Freeze()` (no file lock, fast, GC-clean on teardown).
- `RenderOptions.BitmapScalingMode="NearestNeighbor"` on the storefront/prop `Image`s → crisp
  pixels when upscaled (authentic pixel-art edges). If the chosen art is hi-res *painted* (not
  pixel), use `HighQuality` instead. Set `UseLayoutRounding="True"` to avoid seams; the window is
  `NoResize`, so the scale factor is fixed and shimmer-free.

## How the engine repoints (every UX-48/49 contract preserved)

- **Frosted-glass card (`SceneVisual` / `VisualBrush` + 18px blur):** *unaffected* — the brush
  samples `SceneRoot` regardless of vector vs raster. Blurring the lit raster + the light overlays
  yields **real bokeh** (the bulbs-through-the-glass payoff improves). Card position is unchanged,
  so the `UpdateGlassViewbox` mapping (Loaded + `CardChrome.SizeChanged`) carries over verbatim.
- **Mood crossfade (`ApplyPhase`/`SetBaseValues`/`CrossfadeColor`/`CrossfadeDouble`):** the facade
  *structure* brushes are gone, so their crossfade targets are **removed**; add the single
  `GradeOverlay` gradient color + light-overlay opacity targets (e.g., LateNight door glow → 0.4,
  every-other-bulb halo → 0). Sky/celestial/cloud/town targets stay. Net: **fewer** crossfade
  targets, same machinery. `GetPalette`/`Col`/`LoginScenePhase` bands unchanged;
  `VISTA_LOGIN_SCENE_HOUR` (10/20/1 → Dusk/Evening/LateNight) unchanged.
- **Entrance (`PlayEntrance`):** "bulbs light left→right" → stagger-fade the **light overlays**
  L→R (same staggered one-shot mechanism, retargeted); scene-brighten + card-rise + badge-pop
  unchanged.
- **Success (`PulseDoorGlow`):** pulse the (kept) `DoorGlow`/`DoorSpill` overlay over the raster.
- **Parallax (`HookParallax`/`*ParallaxTr`):** the new `<Image>` lives inside `FacadeLayer`
  (±7); props ±13. Transforms unchanged — only their children changed.
- **Ambient clocks (`StartAmbientLoops`), cat timer, fireflies, sheen, sign-sway:** kept; the
  bulb-twinkle clocks retarget to the surviving bulb-halo overlays (or are trimmed if the raster
  carries static bulbs). Clock count stays ≤ UX-48 budget; `SetDesiredFrameRate ≤30`.
- **Static mode (reduced motion / Tier < 2):** raster `<Image>` + a *static* grade + base-opacity
  overlays, **no blur, no clocks** — the solid-card fallback logic in `LoginView.xaml.vb` is
  untouched.
- **Teardown (`StopAll`, avatar `Shutdown`):** unchanged; frozen `BitmapImage`s are GC-clean.

`LoginView.xaml(.vb)`, `LoginViewModel`, `TinderaAvatar`, DA6, auth flow, and `Application.xaml.vb`
are **not modified** (the glass mapping already covers a fixed card). The avatar stays Aling Vi.

## Engineering constraints (carried — binding)

- VB traps: `Namespace Views.Login` (suffix only); any new `clr-namespace` uses the full
  `MerchSys.App.*` root; no `Await` in `Catch`/`Finally`; no lambda/param shadowing; no
  reserved-word locals; `System.Console` if MEL is imported.
- **Zero hex in `Views/`** — grade/light/glow tokens live in `Themes/LoginScene.xaml`; theme-bound
  brushes via `DynamicResource`. Animated fills remain **local unfrozen** brushes built in code;
  resource `Color` tokens are seeds only (never a Brush token into a `Color` property).
- `IsHitTestVisible=False` scene; automation tree, CapsLock badge, error `LiveSetting` preserved.
- WCAG 2.2.2/2.3.3: OS "animation effects" stays the pause/disable switch; glows are slow fades,
  nowhere near flash thresholds.

## Deliverables

| File | Action |
|------|--------|
| `Assets/Login/*.png` | **New** — licensed CC0 *tindahan* hero + props (operator-acquired) |
| `Assets/Login/CREDITS.md` | **New** — per-asset author/source/license record |
| `MerchSys.App.vbproj` | Add `<Resource Include="Assets\Login\*.png" />` |
| `Themes/LoginScene.xaml` | Drop facade-*structure* tokens; add `GradeOverlay` mood tokens + keep light/glow tokens |
| `Views/Login/DynamicSceneCanvas.xaml` | Replace L4 structure with `<Image>`; add `GradeOverlay`; re-anchor L5 pools + L6 props |
| `Views/Login/DynamicSceneCanvas.xaml.vb` | Repoint mood crossfade (remove facade targets, add grade/light), entrance L→R retarget, image load/freeze; clock bookkeeping unchanged |
| `Views/LoginView.xaml(.vb)` | **Likely untouched** — verify glass viewbox still maps (card unchanged); note in summary if a tweak was needed |
| `ROADMAP-L3-pro.md` | P13b entry (done at plan time) |

Plus: progress summary at `Progress/VISTA_Modules/Experience/UX-50-summary.md`; extend agent-wiki
`wpf-vista-animated-scene` with the **raster-hero + glow-overlay** technique and the vector-medium
defect record (why D5 silhouettes can't reach a crafted illustration).

## Acceptance criteria

1. Build: 0 errors / 0 warnings. PNGs build as `Resource`; `Assets/Login/CREDITS.md` present and
   complete.
2. The storefront renders **crisp** at 900×700 (NearestNeighbor, no blur/seams), right-of-center,
   grounded at the curb; sky + town silhouette fill the left; card overlaps the seam.
3. `VISTA_LOGIN_SCENE_HOUR` = 10 / 20 / 1 → Dusk / Evening / LateNight, each demo-grade; the
   `GradeOverlay` crossfade is seamless; LateNight dims the light overlays (night-light) and sleeps
   the cat.
4. Glass card: the lit raster + glows drift **blurred** behind the form as warm bokeh; text
   contrast ≥ 4.5:1 in both themes; DA6 panel growth re-clips/re-maps cleanly; static mode = flat
   raster + static grade, **no blur cost**.
5. Entrance plays once per show (scene-brighten → light overlays bloom L→R → card rise → badge
   pop); success pulses the door glow; reduced motion shows none of it.
6. All UX-49 avatar/CapsLock/shake/sleep wiring still fires (untouched).
7. Teardown: `StopAll` + avatar `Shutdown` confirmed on hide; no clock/timer/VisualBrush survives;
   `BitmapImage`s frozen.
8. **Verdict gate:** side-by-side with `Expectation.jpg`, the result reads as a cozy 2D indie
   storefront at dusk (composition + warmth), not the UX-49 muddy-vector look. Both themes ×
   three moods × reduced-motion screenshotted for the summary.

## Risk register (none threaten feasibility — all are tuning)

- *Stall sprite reads too small/simple at scale* → compose a row of 2–3 stalls + an awning/shelf
  prop, or adopt the optional detailed shop-exterior sprite. (Fallback asset pre-listed above.)
- *NearestNeighbor shimmer under parallax* → window is `NoResize` (fixed scale) so this is unlikely;
  if seen, switch that `Image` to `HighQuality` or snap parallax offsets to integer px.
- *Bright daytime sprite won't grade to blue hour* → raise `GradeOverlay` opacity, or one-time
  offline dusk-grade of the PNG (no code change). Prefer the runtime grade (no art tooling needed).
