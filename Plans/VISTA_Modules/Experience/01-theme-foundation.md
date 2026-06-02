---
module: MerchSys.App
plan-id: UX-01
title: "Theme Foundation — Tokens, Light/Dark Palettes, Inter, Toggle Service"
depends-on: [UX-00]
estimated-files: 9
---

# Theme Foundation — Tokens, Light/Dark Palettes, Inter, Toggle Service

## Context

VISTA has no central theme layer: `Application.xaml`'s `<Application.Resources>` is empty and every
view hardcodes its own hex colors inline. This plan builds the missing foundation — a token system,
two color palettes (light + dark), the Inter typeface, and a runtime theme-toggle service that swaps
palettes **live with no restart** and **persists the choice per laptop**.

Nothing visual changes app-wide yet (views still carry inline hex until UX-04). The deliverable is
the *infrastructure*: after this plan, a developer can flip light/dark and any control that already
references a token re-colors instantly. This is the prerequisite for UX-02, UX-03, and UX-04.

Read `UX-00` first — the token contract (keys + values) and cross-cutting rules are defined there
and are authoritative. This plan implements that contract.

## Prerequisites

- **UX-00** — the design-language token contract and cross-cutting rules.
- Inter font files (SIL OFL, free to bundle): `Inter-Regular.ttf`, `Inter-Medium.ttf`,
  `Inter-SemiBold.ttf` (and `Inter-Bold.ttf` if desired). Obtain from rsms.me/inter (OFL license
  text must be included in the repo alongside the fonts).

## Deliverables

```
MerchSys.App/Fonts/
├── Inter-Regular.ttf            ' New (Build Action = Resource)
├── Inter-Medium.ttf             ' New (Build Action = Resource)
├── Inter-SemiBold.ttf           ' New (Build Action = Resource)
└── OFL.txt                      ' New — Inter's SIL Open Font License

MerchSys.App/Themes/
├── Tokens.xaml                  ' New — theme-agnostic structure tokens (font, radii, spacing, shadow, type scale)
├── Light.xaml                   ' New — light color palette (all color-token keys)
└── Dark.xaml                    ' New — dark color palette (same keys, dark values)

MerchSys.App/Services/Theming/
├── AppTheme.vb                  ' New — Enum { Light, Dark }
├── IThemeService.vb             ' New — interface
└── ThemeService.vb             ' New — swaps merged palette dictionary + persists choice

MerchSys.App/Application.xaml    ' Modified — merge Tokens.xaml + initial palette into Application.Resources
MerchSys.App/Application.xaml.vb ' Modified — apply persisted theme on startup; register IThemeService in DI
```

> `estimated-files: 9` counts new code/markup files (3 fonts + OFL counted as the Fonts group = 1,
> 3 Themes, 3 Services, + the 2 Application edits). Treat as approximate.

## Specification

### 1. Inter as a bundled resource

Place the `.ttf` files under `MerchSys.App/Fonts/` with **Build Action = `Resource`** (not Content,
not Embedded Resource). Reference via the pack URI + family name:

```
AppFontFamily = "pack://application:,,,/Fonts/#Inter"
```

The `#Inter` suffix is the font's internal family name (verify by inspecting the file's name table;
Inter registers as `Inter`). All three weights share the family; weight is selected via
`FontWeight`. Bundling guarantees identical rendering on all four client laptops regardless of what
is installed locally. Include `OFL.txt` to satisfy the license's bundled-license requirement.

### 2. `Themes/Tokens.xaml` — theme-agnostic structure

A `ResourceDictionary` containing the structure tokens from UX-00 (font family, `RadiusSmall/Medium/
Large`, `Spacing*`, `CardShadow`, `FontSize*`). These do **not** vary by theme.

- `AppFontFamily` is a `FontFamily` resource pointing at the pack URI above.
- `CardShadow` is a `DropShadowEffect` whose `Color` is `{DynamicResource ShadowColor}` — the only
  brush-coupled value here, kept `DynamicResource` so it tracks the active palette.
- Corner radii and spacing are plain `CornerRadius`/`Thickness` resources (`StaticResource`-safe).

Root element note (MC3074 trap): if any `xmlns:` clr-namespace mapping is added, it must read
`clr-namespace:MerchSys.App.Themes`, never `clr-namespace:Themes`.

### 3. `Themes/Light.xaml` and `Themes/Dark.xaml` — palettes

Each is a `ResourceDictionary` defining **every color-token key** from the UX-00 table as a
`SolidColorBrush` (plus the `ShadowColor` as a `Color`). The two files share an **identical set of
keys**; only the values differ. Example shape (light):

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Color x:Key="ShadowColor">#000000</Color>
    <SolidColorBrush x:Key="WindowBackgroundBrush" Color="#F5F5F7"/>
    <SolidColorBrush x:Key="SurfaceBrush"          Color="#FFFFFF"/>
    <SolidColorBrush x:Key="SidebarBackgroundBrush" Color="#F2F2F4"/>
    <SolidColorBrush x:Key="TextPrimaryBrush"      Color="#1D1D1F"/>
    <SolidColorBrush x:Key="TextSecondaryBrush"    Color="#6E6E73"/>
    <SolidColorBrush x:Key="AccentBrush"           Color="#007AFF"/>
    <!-- …all remaining keys from the UX-00 table… -->
</ResourceDictionary>
```

The dark file repeats the same keys with the dark column values. **Missing a key in one file is the
classic bug** — a `DynamicResource` lookup will fail silently and that element won't re-color. The
acceptance criteria require key parity between the two files.

### 4. `Application.xaml` — merge order

Merge `Tokens.xaml` first, then exactly **one** palette dictionary (the startup default). The active
palette occupies a known index so the service can hot-swap it:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- [0] structure tokens -->
            <ResourceDictionary Source="pack://application:,,,/Themes/Tokens.xaml"/>
            <!-- [1] active palette — replaced at runtime by ThemeService -->
            <ResourceDictionary Source="pack://application:,,,/Themes/Light.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 5. Theme model + service

```
Public Enum AppTheme
    Light
    Dark
End Enum

Public Interface IThemeService
    ReadOnly Property Current As AppTheme
    Sub Apply(theme As AppTheme)     ' swaps the palette dictionary live
    Sub Toggle()                     ' Light <-> Dark
    Function LoadPersisted() As AppTheme  ' returns saved choice or Light default
End Interface
```

`ThemeService.Apply(theme)`:

1. Build the new palette `ResourceDictionary` from the matching pack URI
   (`/Themes/Light.xaml` or `/Themes/Dark.xaml`).
2. In `Application.Current.Resources.MergedDictionaries`, **remove the previous palette** (the entry
   whose `Source` ends in `Light.xaml`/`Dark.xaml`) and **add** the new one. Do not touch the
   `Tokens.xaml` entry. Because views use `DynamicResource`, every bound brush updates immediately.
3. Set `Current` and persist (step below).

**Persistence (per laptop):** write the choice to a small JSON file at
`%LOCALAPPDATA%\MerchSys\ui-settings.json` (e.g. `{ "theme": "Dark" }`). Do **not** put this in
`appsettings.json` (that file is deployment config). Reuse the existing `%LOCALAPPDATA%\MerchSys\`
directory convention already used by the app. On startup, `Application.xaml.vb` calls
`LoadPersisted()` and `Apply()` before the main window shows, so the saved theme is in effect from
the first frame.

> **VB.NET trap (BC36943):** if persistence I/O is written `Async`, do not `Await` inside a
> `Catch`/`Finally`. Capture any error, then await/log after the block. A synchronous
> `File.WriteAllText` is acceptable here (the payload is tiny) and avoids the trap entirely.

### 6. DI registration

Register `IThemeService` → `ThemeService` as a **singleton** in the app's composition root (the same
`Microsoft.Extensions.DependencyInjection` container used in `Application.xaml.vb` /
`MediatRConfig`). The shell (UX-02) will inject it to drive the toggle UI.

### 7. Temporary verification affordance (dev-only)

So the toggle can be exercised before UX-02 builds the real UI, add a **temporary** toggle entry to
the existing **Developer Tools** panel (`Views/Shell/Modules/DeveloperToolsPanel.xaml`) — a single
"Toggle Theme" button bound to a command that calls `IThemeService.Toggle()`. This is scaffolding;
UX-02 moves the toggle to its permanent home (e.g. the module detail panel footer). Mark it with a
`<!-- UX-01 temp: remove when UX-02 lands the permanent toggle -->` comment.

## Implementation Notes

- **Do not restyle any control in this plan.** No `Style`/`Template` authoring here — that is UX-02
  (shell) and UX-03 (controls). UX-01 is purely tokens + plumbing. The app will look unchanged
  except where the dev toggle flips the (still mostly inline-styled) UI; only elements that already
  happen to inherit defaults will visibly change. That is expected.
- **Key parity is the critical correctness property.** Consider authoring `Light.xaml` first, then
  copying it to `Dark.xaml` and editing only values, to guarantee identical keys.
- **Inter family name:** if the bundled file registers under a different internal name, the
  `#Inter` reference silently falls back to the system default. Verify the family name resolves
  (text should render in Inter, not Segoe) as part of acceptance.
- No new NuGet packages.

## Acceptance Criteria

1. `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` succeeds with **0 errors, 0 warnings**.
2. `Application.xaml` merges `Tokens.xaml` + one palette; resources are no longer empty.
3. `Light.xaml` and `Dark.xaml` define an **identical set of keys** (every key in the UX-00 color
   table present in both); no key exists in one but not the other.
4. Inter renders: a `TextBlock` with `FontFamily="{StaticResource AppFontFamily}"` shows Inter, not
   the system default.
5. The Developer Tools "Toggle Theme" button flips the app between light and dark **without a
   restart**, and any element bound to a token via `DynamicResource` re-colors immediately.
6. The chosen theme is written to `%LOCALAPPDATA%\MerchSys\ui-settings.json` and is **restored on
   next launch** (relaunch in dark → app opens dark).
7. `IThemeService` is registered as a singleton and resolvable from DI.
8. `OFL.txt` (Inter's license) is present in `MerchSys.App/Fonts/`.
9. No view's existing bindings, commands, or behavior changed.

## Output Requirements

### Implementation Summary
Create `Progress/VISTA_Modules/Experience/UX-01-summary.md` (template `Progress/_template.md`).
Include:

- The exact pack URI + verified Inter family name.
- The final list of color-token keys and confirmation of light/dark parity.
- The persistence file path/format and the startup-apply call site.
- The MergedDictionaries swap mechanism (how the active palette is located and replaced).
- Note that the Developer Tools toggle is temporary and owned by UX-02 for relocation.
- Any `codebase_wiki` discrepancies noticed (do not edit `codebase_wiki`).

### Documentation
- XML doc on `IThemeService`/`ThemeService` describing the live-swap mechanism and `DynamicResource`
  requirement.
- Header comment in `Tokens.xaml`, `Light.xaml`, `Dark.xaml` stating the UX-00 token contract is
  authoritative and keys must stay in parity.
