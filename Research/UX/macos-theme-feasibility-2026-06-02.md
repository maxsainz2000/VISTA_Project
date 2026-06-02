# Feasibility Research — A "MacBook"-Themed WPF UI for VISTA

**Date:** 2026-06-02
**Author:** Claude Code (research only — no code changes)
**Question:** Is it possible to build a "MacBook"-themed WPF application — a combination of *classic* and *premium* feel — for VISTA?
**Status of app:** Tested and fully functional. Next phase is UX/UI improvement only.

---

## 1. Verdict

**Yes — it is fully possible, and WPF is well-suited to it.** WPF's templating engine (Styles, ControlTemplates, ResourceDictionaries) is one of the most powerful UI customization systems on any desktop platform; you can restyle every control pixel-for-pixel. A convincing macOS aesthetic on WPF is a *solved, well-trodden problem*.

There are exactly **three things you cannot replicate 1:1** on Windows/WPF, and all three have acceptable workarounds (§6). Nothing here is a blocker.

The real cost is **not** technical difficulty — it's that VISTA currently has **no centralized theme layer**. Every view hardcodes its own colors and styles inline. So the work is less "can WPF do macOS" (it can) and more "introduce a design-token system and re-skin ~35 views consistently." That's a meaningful but bounded effort (§7).

---

## 2. Decomposing "MacBook — classic + premium"

"macOS feel" is not one thing. It decomposes into concrete, implementable design tokens:

| Trait | macOS specifics | WPF implementation |
|-------|-----------------|--------------------|
| **Typography** | SF Pro / SF Pro Text, tight tracking, optical sizing | Bundle **Inter** (SF Pro is license-locked — §5). `FontFamily` token. |
| **Color** | Muted neutrals, lots of near-white (`#FFFFFF`/`#F5F5F7`), single restrained accent (system blue `#0A84FF` / `#007AFF`) | `Color`/`SolidColorBrush` resources in a theme dictionary |
| **Roundness** | Soft, generous corner radii (6–12px on cards/buttons, ~10–16px on windows) | `CornerRadius` on `Border`/control templates |
| **Depth** | Very subtle, soft, low-opacity drop shadows; layered "cards" on a light canvas | `DropShadowEffect` (soft, low opacity) or shadow `Border` chrome |
| **Spacing** | Generous whitespace, calm density, aligned grids | Margins/padding tokens; restraint |
| **Controls** | Pill toggles, segmented controls, capsule search fields, sidebar with translucent selection, traffic-light window buttons | Custom `ControlTemplate`s |
| **Window** | Rounded title bar, integrated toolbar, "traffic lights" (close/min/max), unified sidebar | `WindowChrome` + custom caption (§6.2) |
| **Materials** | Vibrancy/translucency ("frosted glass") sidebars and panels | Closest Windows equivalent = Acrylic/Mica (§6.1) |
| **Motion** | Gentle ease-in-out, spring-like, short | WPF `Storyboard` animations on triggers |

"Classic + premium" reads as: **restraint, alignment, soft depth, one accent color, excellent type.** That is squarely achievable.

---

## 3. WPF capability map

**Native / easy (90% of the look):**
- Full control re-templating — buttons, lists, tabs, scrollbars, text fields, the activity rail, the sidebar.
- Rounded corners, soft shadows, color tokens, custom fonts — all first-class.
- Segmented controls, pill toggles, capsule search boxes — built from `ToggleButton`/`ListBox` templates.
- Custom window chrome with traffic-light buttons via the `WindowChrome` class.
- Smooth animated hover/selection states via `Storyboard`/`VisualStateManager`.

**Possible with effort:**
- macOS-style **translucent sidebars** — emulate with Windows Acrylic/Mica backdrop (DWM) or a blurred-bitmap behind a semi-transparent panel.
- Sidebar "vibrant" selection highlights — semi-transparent accent fills + subtle inner glow.

**Cannot replicate exactly (with workarounds):**
- True macOS **vibrancy** (content-aware live blur that samples wallpaper/desktop). Windows Acrylic samples behind the *window*, not the live desktop content the way macOS does. *Workaround: Acrylic/Mica or a static frosted panel — visually 80–90% there.* (§6.1)
- **SF Pro** the actual typeface — legally cannot be bundled. *Workaround: Inter.* (§5)
- Pixel-identical traffic-light hover glyphs/physics — easy to approximate, fiddly to perfect. *Workaround: custom-templated buttons; nobody will notice.* (§6.2)

---

## 4. Recommended approach for VISTA specifically

Because the app already works and styling is inline, the right move is a **non-invasive, additive theme layer**, not a rewrite:

1. **Introduce a central theme** — add `ResourceDictionary` files (e.g. `Themes/Tokens.xaml`, `Themes/Controls.xaml`) and merge them in `Application.xaml` (currently empty). This is the missing foundation.
2. **Define design tokens** — colors, brushes, font family, corner radii, shadow, spacing as named resources (`{StaticResource AccentBrush}`, `{StaticResource CardCornerRadius}`, etc.).
3. **Author implicit styles** for the common controls (`Button`, `TextBox`, `ListBox`, `TabControl`, `DataGrid`, `ScrollBar`) so most views inherit the look *without per-view edits*.
4. **Re-skin the shell first** — `MainWindow`, `ActivityRail`, `ModuleDetailPanel`. This shell is seen on every screen and delivers the biggest perceived-quality jump for the least work.
5. **Migrate views incrementally** — replace inline hex with token references view-by-view. Functional behavior (bindings, commands, MVVM) is untouched; only chrome changes. Low regression risk.

> **Architectural note:** ViewModels live in module libraries; Views (XAML) live in `MerchSys.App/Views/`. Theming touches **only** `MerchSys.App` (Views + a new `Themes/` folder + `Application.xaml`). No module library, no `DbContext`, no MediatR contract is affected. This keeps the change isolated to the presentation project — exactly where CLAUDE.md says the UI lives.

---

## 5. Fonts — the one legal constraint (important)

- **SF Pro cannot be used.** Apple's license explicitly forbids embedding SF Pro in software for non-Apple OSes and forbids redistribution. Bundling it in a Windows `.exe` would violate the license. ([Apple Developer Fonts](https://developer.apple.com/fonts/), [Apple Developer Forums](https://developer.apple.com/forums/thread/719561))
- **Use Inter instead.** Inter is open-source (SIL OFL), was explicitly designed for screens, and shares SF Pro's optical-sizing behavior and proportions — the closest legal stand-in for the "premium digital product" feel. ([SF Pro alternatives — MaisFontes](https://en.maisfontes.com/blog/apple-typography-sf-pro-free-alternatives))
- Bundle Inter as an app resource (`/Fonts/#Inter`) so it renders identically on all four client laptops regardless of what's installed. Secondary options: **DM Sans**, **Nunito**.

---

## 6. The three "can't do exactly" items + workarounds

### 6.1 Vibrancy / translucency
macOS vibrancy is live, content-aware blur. Windows' nearest equivalents are **Acrylic** and **Mica** (DWM system backdrops), usable from WPF on Windows 11 (.NET 9+ exposes Windows 11 backdrop theming; libraries like WPF UI wrap it). It samples behind the *window* rather than the live desktop, so it's ~80–90% of the effect. For a LAN business app this is more than convincing. A fully static "frosted card" look is also fine and avoids DWM entirely.

### 6.2 Window chrome + traffic lights
Use the `WindowChrome` class to remove the standard Windows title bar and draw a custom caption: rounded top corners, an integrated toolbar, and three custom-templated circular buttons (red/yellow/green) wired to Close/Minimize/Maximize. This is standard practice and well-documented. Caveat: a non-standard title bar is slightly *un-Windows*; some users expect native min/max behavior — worth confirming with the Owner/Manager (§9).

### 6.3 Exact control physics
Spring animations, rubber-band scrolling, and precise traffic-light hover glyphs can be *approximated* but not matched frame-for-frame. In practice nobody comparing a POS app to a Mac will notice. Don't over-invest here.

---

## 7. Effort & risk

**Current state that drives effort:** `Application.xaml` resources are empty; ~35 views hardcode colors inline (e.g. `ActivityRail.xaml` defines its own button template with literal `#1A252F`/`#2980B9`; `MainWindow.xaml` content area is `Background="#F5F6FA"`). There is no token system to lean on, so the foundation must be built first.

| Phase | Scope | Risk |
|-------|-------|------|
| **0 — Foundation** | Theme dictionaries, tokens, Inter font, merge into `Application.xaml` | Low |
| **1 — Shell reskin** | `MainWindow`, `ActivityRail`, `ModuleDetailPanel`, window chrome + traffic lights | Low–Med (chrome is the only fiddly part) |
| **2 — Implicit control styles** | Button/TextBox/ListBox/TabControl/DataGrid/ScrollBar | Low |
| **3 — View migration** | Replace inline hex → tokens, view by view (~35 views) | Low but repetitive |
| **4 — Materials & polish** | Acrylic/Mica sidebar, soft shadows, hover/selection animations | Med (DWM interop) |

**Risk profile:** Low overall. This is **cosmetic and additive** — it does not touch data access, MediatR contracts, concurrency, or business logic. Worst case for any single view is a visual glitch, not a functional regression. Build constraint from CLAUDE.md still applies: **0 errors, 0 warnings**, and XAML `clr-namespace` must carry the full `MerchSys.App...` root prefix (MC3074 trap).

---

## 8. Library options

You can either **build the theme by hand** (maximum control over the macOS look, no dependency) or **start from a Fluent library and bend it macOS-ward**:

- **WPF UI (lepoco)** — free, MIT, modern Fluent controls + Win11 backdrop support. Good *base* for rounded/modern controls; you'd override colors/radii toward macOS. ([github.com/lepoco/wpfui](https://github.com/lepoco/wpfui))
- **WPF native Fluent theme** (.NET 9+) — built-in, supports `CornerRadius` palette tweaks and Win11 theming. ([Thomas Claudius Huber](https://www.thomasclaudiushuber.com/2025/02/21/wpf-in-net-9-0-windows-11-theming/))
- **Commercial** (Telerik, Syncfusion Theme Studio, Xceed Pro Themes) — polished but licensed; overkill for a ~50-SKU shop and they're Fluent/Windows-flavored, not macOS.

> **Recommendation:** Hand-build a small token + style set (Phase 0–2). The macOS look is specific enough that a Fluent library would be fought as much as leveraged, and VISTA's control surface is modest. Optionally lift WPF UI *only* for its Win11 Acrylic/Mica backdrop helper in Phase 4.

---

## 9. Open questions for the user (before any build)

1. **Light or dark?** macOS "premium" is most associated with the **light** aesthetic (white cards, soft shadows). VISTA's shell is currently dark. Pick a primary (or support both via swappable dictionaries).
2. **Custom window chrome + traffic lights — yes or no?** It's the most "Mac" signal but the least "Windows-native." For a Windows business app run by a Manager/Owner, confirm they want a non-standard title bar.
3. **Scope of fidelity** — "tasteful macOS-inspired" (calm palette, Inter, rounded cards, soft depth, one accent) vs. "pixel homage" (traffic lights, vibrancy, segmented controls everywhere). The former is ~70% of the effort for ~95% of the perceived premium feel.

---

## 10. Bottom line

- **Feasible:** yes, comfortably. WPF's templating makes this one of its strengths.
- **Legal constraint:** don't ship SF Pro — ship **Inter**.
- **Real cost:** building the centralized theme layer VISTA never had, then re-skinning ~35 inline-styled views. Cosmetic, additive, low-risk; touches only `MerchSys.App`.
- **Recommended start:** Phase 0 (tokens + Inter) → Phase 1 (shell). That alone transforms the perceived quality, and you can stop or continue from there.

---

### Sources
- [Apple Developer — Fonts](https://developer.apple.com/fonts/)
- [Apple Developer Forums — Can we use SF Pro fonts in apps?](https://developer.apple.com/forums/thread/719561)
- [SF Pro typography and free alternatives — MaisFontes](https://en.maisfontes.com/blog/apple-typography-sf-pro-free-alternatives)
- [WPF UI (lepoco) — Fluent experience for WPF](https://github.com/lepoco/wpfui)
- [WPF in .NET 9.0 — Windows 11 Theming, Thomas Claudius Huber](https://www.thomasclaudiushuber.com/2025/02/21/wpf-in-net-9-0-windows-11-theming/)
- [How to Get Fluent Design Theme in Your WPF Application — Telerik](https://www.telerik.com/blogs/how-to-get-fluent-design-theme-wpf-application)
