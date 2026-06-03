---
module: MerchSys.App
plan-id: UX-00
title: "macOS-Inspired Theme — Epic Overview & Index"
depends-on: []
estimated-files: 0
---

# macOS-Inspired Theme — Epic Overview & Index

> **This is an index/overview document, not an implementable batch.** It defines the shared
> design language, the phase DAG, and the cross-cutting rules that all `UX-*` plans inherit.
> Implementers should read this first, then execute `UX-01 → UX-02 → UX-03 → UX-04` in order.

## Context

VISTA is tested and fully functional. This epic is **UX/UI only** — a tasteful, macOS-inspired
re-skin ("classic + premium" feel). It is **additive and cosmetic**: it does not touch data
access, `DbContext`s, MediatR contracts, concurrency, services, or business logic. All work is
confined to the **`MerchSys.App`** presentation project (Views + a new `Themes/` folder +
`Application.xaml`). No module library is modified. No cross-module rule changes.

Decisions confirmed with the product owner (2026-06-02):

1. **Light + Dark theme toggle** — both ship; live swap, no restart; choice persisted per laptop.
2. **Native Windows title bar retained** — no custom `WindowChrome`, no traffic-light buttons.
3. **Tasteful macOS-inspired** — restrained palette + single accent, Inter type, soft corner radii,
   subtle depth, generous whitespace, gentle hover/selection motion. No vibrancy chasing, no
   segmented-controls-everywhere.

Feasibility research backing this epic: `Research/UX/macos-theme-feasibility-2026-06-02.md`.

### Current state (drives the work)

- `Application.xaml` `<Application.Resources>` is **empty** — there is no central theme layer.
- Styling is **inline and hardcoded** across ~35 views (e.g. `ActivityRail.xaml` defines its own
  button template with literal `#1A252F`/`#2980B9`; `MainWindow.xaml` content area is
  `Background="#F5F6FA"`). There is no token system to lean on.

So the foundation (UX-01) must be built before anything else, and the **single most important
discipline** is established there: **all color/brush references use `DynamicResource`** so the
light/dark swap re-colors the live UI without a restart.

## Phase DAG

| Plan | Title | depends-on | Outcome |
|------|-------|-----------|---------|
| **UX-01** | Theme Foundation — tokens, palettes, Inter, toggle service | UX-00 | Central theme layer + working light/dark toggle |
| **UX-02** | Shell Reskin — MainWindow, ActivityRail, ModuleDetailPanel | UX-01 | The always-visible chrome looks macOS-premium |
| **UX-03** | Implicit Control Styles — Button/TextBox/ListBox/TabControl/DataGrid/ScrollBar | UX-01 | Common controls inherit the look app-wide |
| **UX-04** | View Migration — ~35 views: inline hex → tokens | UX-02, UX-03 | Every screen consistent; zero hardcoded color |

UX-02 and UX-03 both depend only on UX-01 and may be executed in either order (or in parallel by
two sessions). UX-04 depends on both.

## Shared Design Language (authoritative token contract)

All four phases reference these **named resource keys**. Keys are identical across light and dark;
only the *values* differ between `Themes/Light.xaml` and `Themes/Dark.xaml`. Layout/structure
tokens (font, radii, spacing, shadow, type scale) live in the theme-agnostic `Themes/Tokens.xaml`
and are shared by both.

### Color tokens (keys — defined in BOTH Light.xaml and Dark.xaml)

| Key | Role | Light value | Dark value |
|-----|------|------------|-----------|
| `WindowBackgroundBrush` | App canvas behind content | `#F5F5F7` | `#1E1E1E` |
| `SurfaceBrush` | Cards / panels / raised content | `#FFFFFF` | `#2C2C2E` |
| `SidebarBackgroundBrush` | Activity rail + module detail panel | `#F2F2F4` | `#232325` |
| `ControlBackgroundBrush` | Inputs, list rows | `#FFFFFF` | `#2C2C2E` |
| `TextPrimaryBrush` | Primary text | `#1D1D1F` | `#F5F5F7` |
| `TextSecondaryBrush` | Secondary / captions | `#6E6E73` | `#98989D` |
| `TextOnAccentBrush` | Text on accent fills | `#FFFFFF` | `#FFFFFF` |
| `AccentBrush` | Single system accent | `#007AFF` | `#0A84FF` |
| `AccentHoverBrush` | Accent hover/pressed | `#0A6CFF` | `#3D9BFF` |
| `SeparatorBrush` | Hairline dividers / borders | `#D2D2D7` | `#38383A` |
| `SelectionBackgroundBrush` | Selected nav/list row fill | `#E8F0FE` | `#0A84FF` (28% alpha) |
| `SelectionForegroundBrush` | Selected row text | `#007AFF` | `#F5F5F7` |
| `DangerBrush` | Errors / overdue / expiry | `#FF3B30` | `#FF453A` |
| `WarningBrush` | Low-stock / caution | `#FF9F0A` | `#FFB340` |
| `SuccessBrush` | Positive / paid | `#34C759` | `#30D158` |
| `HoverBackgroundBrush` | Interaction overlay for hover/pressed rows & buttons | `#1D1D1F` @ 6% | `#FFFFFF` @ 8% |
| `ShadowColor` | Card drop-shadow color (`Color`, not brush) | `#000000` | `#000000` |

> The four **status** brushes (Danger/Warning/Success/Accent) replace the scattered semantic
> hex values currently inline in views (e.g. the `⚠️ Low-Stock` and `🔴 Expiring` indicators).

> **`HoverBackgroundBrush` is theme-specific and must be authored per palette — do not derive it in
> markup.** It is a translucent ink overlay (dark ink on light, white on dark) painted over a row or
> button on hover/press. It was introduced in UX-02. **Antipattern (validated, see
> `agent_wiki/antipatterns/wpf-dynamicresource-brush-into-color-property.md`):** never define it as
> `<SolidColorBrush Color="{DynamicResource TextPrimaryBrush}" Opacity="0.06"/>` — feeding a brush
> resource into the `Color` property compiles clean (0/0) but throws `InvalidOperationException` at
> dictionary-realization time, and **only in whichever theme is active at boot**, so a clean build +
> working Light proves nothing about Dark. Author the literal translucent color in each palette
> instead (the values above). All consumers reference it via `DynamicResource`.

### Structure tokens (theme-agnostic — `Themes/Tokens.xaml`)

| Key | Type | Value | Notes |
|-----|------|-------|-------|
| `AppFontFamily` | `FontFamily` | `pack://application:,,,/Fonts/#Inter` | Inter bundled as resource (see UX-01) |
| `RadiusSmall` | `CornerRadius` | `6` | Buttons, inputs, list rows |
| `RadiusMedium` | `CornerRadius` | `10` | Cards, panels, dialogs |
| `RadiusLarge` | `CornerRadius` | `14` | Hero/dashboard cards |
| `SpacingXS / S / M / L / XL` | `Thickness` | `4 / 8 / 12 / 16 / 24` | Margins & padding |
| `CardShadow` | `DropShadowEffect` | Blur 16, Depth 2, Dir 270, Opacity 0.12, `Color={DynamicResource ShadowColor}` | Soft macOS depth |
| `FontSizeCaption / Body / Subhead / Title / LargeTitle` | `Double` | `11 / 13 / 15 / 20 / 28` | macOS-ish type scale |

### Type ramp guidance

Inter at the sizes above, with weight via `FontWeight` (Regular 400 body, Medium 500 subheads,
SemiBold 600 titles). Tight line-height; avoid Bold-everything. This single change (Inter + the
ramp) delivers most of the "premium" perception.

## Cross-cutting rules (inherited by every UX-* plan)

1. **`DynamicResource` for all color/brush references.** Never `StaticResource` for anything that
   changes between themes. Structure tokens (radii/spacing/font) may be `StaticResource` since they
   don't vary by theme — but brushes referenced *inside* a shared `DropShadowEffect` (the shadow
   color) must be `DynamicResource`.
2. **No hardcoded hex in views after UX-04.** Every color resolves to a token. UX-04's acceptance
   gate is "zero literal `#RRGGBB` in migrated views except inside the two palette dictionaries."
3. **Behavior is untouchable.** Bindings, commands, `x:Name`s, `DataContext`, MVVM wiring, and
   navigation must not change. Only chrome (`Background`, `Foreground`, `Style`, `Template`,
   `Margin`, `Padding`, `CornerRadius`, `Effect`, `FontFamily`) changes.
4. **XAML `clr-namespace` trap (MC3074).** Any `xmlns:` mapping must carry the full root prefix,
   e.g. `clr-namespace:MerchSys.App.Themes`, never `clr-namespace:Themes`. `x:Class` is unaffected.
5. **Build gate.** `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` must complete with
   **0 errors, 0 warnings**. Per CLAUDE.md: if the build fails, **do not hand-patch** — document
   every error in `Progress/` and stop; troubleshooting is a separate session.
6. **VB.NET trap (BC36943).** Any `Async` code-behind/service must not `Await` inside
   `Catch`/`Finally`; capture error state, await after the block. (Applies mainly to UX-01's
   theme-persistence I/O.)
7. **No new NuGet packages** unless a phase plan explicitly authorizes one. The theme is
   hand-built; no Fluent/commercial library is taken as a dependency.
8. **Accessibility floor.** Light and dark palettes must both clear WCAG AA contrast (4.5:1) for
   `TextPrimaryBrush` on `SurfaceBrush`/`WindowBackgroundBrush`. The values above are chosen to pass;
   any palette tweak must re-verify.

## Output Requirements

Each `UX-*` plan produces its own summary at
`Progress/VISTA_Modules/Experience/UX-0N-summary.md` (template `Progress/_template.md`).

This overview (`UX-00`) has **no code deliverable** and no summary of its own; it is referenced by
the four phase summaries.

## Non-Goals (explicitly out of scope for this epic)

- Custom window chrome / traffic-light buttons (owner declined).
- macOS vibrancy / live desktop blur, Acrylic/Mica backdrops.
- Segmented controls, pill toggles, capsule search as a system-wide mandate (may appear later as
  targeted polish, not part of UX-01..04).
- Any data, service, ViewModel-logic, or navigation-behavior change.
- Bundling SF Pro (license-prohibited — Inter is the chosen typeface).
