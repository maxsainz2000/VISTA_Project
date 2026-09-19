---
module: MerchSys.App
agent: antigravity
date: 2026-06-03
plan-ref: Plans/VISTA_Modules/Experience/03-control-styles.md
status: completed
---

## Task Summary

Implemented implicit, theme-aware WPF styles and templates for all common controls to achieve a macOS-inspired aesthetic app-wide without requiring per-view edits. Integrated the custom styles into the central application resources.

**Plan:** `[[03-control-styles]]`

## What Was Done

- Created `WPF_Applications/MerchSys/src/MerchSys.App/Themes/Controls.xaml` defining implicit styles for:
  - `ScrollBar` (Custom template: 8px slim scrollbars with overlay rounded thumbs, no arrow buttons).
  - `Button` (Custom template: rounded corner flat style, interaction overlay support for hover and pressed states).
  - `TextBox` / `PasswordBox` (Custom template: comfortable padding, rounded borders, and active accent border on focus).
  - `CheckBox` / `RadioButton` (Custom templates: rounded checkbox, dot radio button, utilizing system accent colors).
  - `ListBox` / `ListBoxItem` (Custom template: full-width rounded rows with subtle selection/hover overlays).
  - `ComboBox` / `ComboBoxItem` (Custom template: clean dropdown chevrons, popup container with medium corner radius and drop shadow).
  - `TabControl` / `TabItem` (Custom template: Safari-style flat tabs with thin accent underline active indicator).
- Created keyed button styles:
  - `AccentButtonStyle` (Keyed style for primary actions utilizing `AccentBrush` and `AccentHoverBrush`).
  - `LinkButtonStyle` (Keyed flat style for tertiary actions).
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Themes/Controls.DataGrid.xaml` definingimplicit styles for:
  - `DataGrid` (Horizontal hairlines, surface backgrounds, alternating row canvas backgrounds).
  - `DataGridColumnHeader` (Left-aligned, flat headers, semi-bold text, bottom border line).
  - `DataGridRow` (Hover overlay, selection highlights, comfortable spacing).
  - `DataGridCell` (Padding adjustments, no focus rectangle border harshness).
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml` to merge the new control dictionaries in the correct order after design tokens and active theme palettes.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Succeeded with 0 errors, 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ⚠️ Initial claim was inaccurate — the app **crashed at view load** (see Issues). Corrected by claude-code 2026-06-03; rebuilds 0/0 and the original `XamlParseException` is resolved. A full runtime smoke-test (open Login, an input form, a DataGrid view, toggle light/dark) is still recommended before UX-04. |

## Issues Encountered

- **Issue:** WPF compilation failed in `ScrollBar` style with error `MC3088: Property elements cannot be in the middle of an element's content.` because `<Style.Triggers>` was placed before the `<Setter Property="Template">` element.
  - **Resolution:** Re-ordered the style elements so that the `<Style.Triggers>` block is the last element inside the `<Style>` block, satisfying WPF's XAML parser.
- **Issue (runtime crash, fixed by claude-code 2026-06-03):** App threw `XamlParseException` → inner `ArgumentNullException (Parameter 'property')` at `Setter.Property`, `Controls.xaml` line 82, the moment the first `TextBox` realized (Login view). The implicit `ScrollBar` template's `Orientation=Horizontal` trigger tried to retarget the `Track`'s `DecreaseRepeatButton`/`IncreaseRepeatButton`/`Thumb` via `<Setter TargetName="PART_Track" Property="...">`. Those three are **plain CLR properties, not `DependencyProperty`s**, so a `Setter` cannot target them — the lookup returns null and throws when the style is sealed at runtime. This is why the build was clean (0/0) yet the app crashed: setter-target validity is not a compile-time check. The crash surfaced on a `TextBox` because its template hosts a `ScrollViewer` → `ScrollBar`.
  - **Resolution:** Replaced the single template + illegal triggers with **two complete `ControlTemplate`s** (`VerticalScrollBarTemplate` default, `HorizontalScrollBarTemplate`) and swap the whole `Template` (a real DP) in the `Style.Trigger` for `Orientation=Horizontal`. Logged as `agent_wiki/antipatterns/wpf-setter-targets-clr-property-not-dependencyproperty.md`. Rebuild 0/0.

## What's Next

- **UX-04** — Migrate all remaining pages and views to resolve colors exclusively via tokens (removing inline hex values).

## Cross-References

- Domain Wiki pages consulted: `[[macos-theme-overview]]`
- Agent Wiki entries consulted: `[[wpf-dynamicresource-brush-into-color-property]]`
