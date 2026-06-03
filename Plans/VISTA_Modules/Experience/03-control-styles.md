---
module: MerchSys.App
plan-id: UX-03
title: "Implicit Control Styles — Button, TextBox, ComboBox, ListBox, TabControl, DataGrid, ScrollBar"
depends-on: [UX-01]
estimated-files: 3
---

# Implicit Control Styles — Button, TextBox, ComboBox, ListBox, TabControl, DataGrid, ScrollBar

## Context

This plan authors **implicit** (keyless) `Style`s for the common controls so that most views inherit
the macOS-inspired look **without per-view edits**. It is the multiplier that makes UX-04 (view
migration) mostly a matter of removing inline hex rather than re-templating every control by hand.

Implicit styles are `TargetType="..."` styles with **no `x:Key`**, placed in app-merged dictionaries,
so they apply to every instance of that control type unless a view explicitly overrides. Where a
fully custom template is needed (rounded inputs, flat buttons, slim scrollbars), the style carries a
`ControlTemplate`; where only property defaults are needed (font, padding, colors), the style just
sets properties.

Read `UX-00` (tokens + rules) and `UX-01` (tokens exist) first. UX-03 depends only on UX-01 and may
run in parallel with UX-02.

## Prerequisites

- **UX-01** — tokens (`AccentBrush`, `SurfaceBrush`, `ControlBackgroundBrush`, `TextPrimaryBrush`,
  `SeparatorBrush`, `Radius*`, `Spacing*`, `AppFontFamily`, `FontSize*`, etc.).
- **`HoverBackgroundBrush`** already exists in both palettes (added in UX-02 — Light `#1D1D1F`@6%,
  Dark `#FFFFFF`@8%). **Reuse it for every hover/pressed fill in this plan** (Button, ListBoxItem,
  ComboBoxItem, DataGridRow, TabItem). Do **not** author a new per-control hover brush, and do **not**
  build one from `TextPrimaryBrush` + `Opacity` in markup — see the antipattern note in UX-00.

## Deliverables

```
MerchSys.App/Themes/Controls.xaml          ' New — implicit styles for all common controls
MerchSys.App/Themes/Controls.DataGrid.xaml ' New — DataGrid + row/header/cell styles (kept separate; it's large)
MerchSys.App/Application.xaml              ' Modified — merge Controls dictionaries (after Tokens + palette)
```

Merge order in `Application.xaml`: `Tokens.xaml` → active palette → `Controls.xaml` →
`Controls.DataGrid.xaml`. Control styles reference palette/structure tokens, so they must be merged
after them.

## Specification

All styles reference colors via `DynamicResource` (theme-swappable) and apply `AppFontFamily` +
appropriate `FontSize*`. Target the macOS-tasteful look: rounded, flat, soft, one accent.

### Button (default)

- `ControlBackgroundBrush` (or `SurfaceBrush`) background, `RadiusSmall` corners, `TextPrimaryBrush`
  text, 1px `SeparatorBrush` border, `SpacingS`/`SpacingM` padding.
- Hover: subtle fill lift; Pressed: slightly darker; Disabled: reduced opacity.
- Provide a **keyed** accent variant `AccentButtonStyle` (`AccentBrush` fill, `TextOnAccentBrush`
  text) for primary actions — keyed, not implicit, so it's opt-in.
- Provide a keyed `LinkButtonStyle` (transparent, accent text) for tertiary actions.

### TextBox / PasswordBox

- `ControlBackgroundBrush`, `RadiusSmall`, 1px `SeparatorBrush` border, `TextPrimaryBrush` text,
  comfortable padding. Focused: border → `AccentBrush` (subtle ~1.5px), no harsh glow.
- Placeholder/watermark guidance optional (only if a watermark pattern already exists in the app).

### ComboBox

- Match TextBox chrome (rounded, bordered). Popup uses `SurfaceBrush` with `RadiusMedium` and
  `CardShadow`; items highlight with `SelectionBackgroundBrush`/`SelectionForegroundBrush`.

### ListBox / ListBoxItem

- Transparent list background; items are full-width rounded (`RadiusSmall`) rows. Hover = subtle
  fill; selected = `SelectionBackgroundBrush` + `SelectionForegroundBrush`. Remove the default
  Windows blue highlight.

### TabControl / TabItem

- Flat tabs, no 3D border. Inactive tab text `TextSecondaryBrush`; active tab `TextPrimaryBrush`
  with a thin `AccentBrush` underline (macOS/Safari-ish). Tab content area `SurfaceBrush`.

### DataGrid (in `Controls.DataGrid.xaml`)

VISTA's reports/ledgers use grids heavily; this is the highest-impact control after the shell.

- Grid background `SurfaceBrush`; remove heavy gridlines — use `SeparatorBrush` **horizontal**
  hairlines only (`GridLinesVisibility=Horizontal`), no vertical lines.
- Column headers: `SurfaceBrush`/subtle tint, `TextSecondaryBrush`, SemiBold, bottom `SeparatorBrush`
  border, left-aligned text, no raised 3D look.
- Rows: comfortable row height; hover subtle fill; selected `SelectionBackgroundBrush` /
  `SelectionForegroundBrush`. Optional very-subtle alternating row tint (`AlternatingRowBackground`)
  if it reads well — otherwise plain.
- Cells: `TextPrimaryBrush`, `SpacingS` padding, remove focus rectangle harshness.

### ScrollBar

- Slim, overlay-style, rounded thumb in a muted `TextSecondaryBrush` (reduced opacity), transparent
  track, no arrow buttons (or minimal). Thumb darkens slightly on hover. This single control does a
  lot for the "macOS" read.

### CheckBox / RadioButton (if used in views)

- Rounded check/dot, `AccentBrush` when checked, `SeparatorBrush` border when unchecked. Only style
  these if the app actually uses them (verify; otherwise skip to keep scope tight).

## Implementation Notes

- **Implicit, not keyed, for the base styles.** No `x:Key` on the per-type base style so every
  instance inherits. Keyed variants (`AccentButtonStyle`, `LinkButtonStyle`) are opt-in extras.
- **Don't break existing keyed styles.** Some views define their own keyed styles inline (e.g. the
  rail's `RailButtonStyle`). Implicit styles do **not** override keyed styles, so those keep working;
  UX-04 reconciles them. Verify no view relies on an unstyled default that this plan would alter in a
  way that breaks layout (e.g. a Button whose size assumed zero padding). Where a regression is
  found, document it for UX-04 rather than special-casing here.
- **These implicit styles take effect app-wide the moment they merge — including in the ~35 views
  UX-04 has not migrated yet.** That is intended (UX-04 then mostly just strips inline hex), but
  expect transient visual collisions where a not-yet-migrated view still carries inline
  `Background`/`Foreground` hex that fights the new implicit chrome. Do **not** chase those here —
  note any that look broken and leave them for UX-04. The gate for UX-03 is that controls render
  correctly where they inherit cleanly, not that every legacy view is already perfect.
- **Test both themes and a few representative views at runtime** (a DataGrid-heavy report, an input
  form like Login, a list view) to confirm the implicit styles read well before UX-04 starts.
- Templates must keep `ContentPresenter`/`ScrollViewer`/`ItemsPresenter` wiring intact so controls
  remain functional — re-template chrome, not behavior.
- MC3074: full root prefix on any `clr-namespace`. No new NuGet packages.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. `Controls.xaml` + `Controls.DataGrid.xaml` are merged after tokens/palette in `Application.xaml`.
3. Buttons, text inputs, combo boxes, lists, tabs, data grids, and scrollbars render in the
   macOS-inspired style **app-wide without per-view edits**, in both light and dark.
4. Keyed `AccentButtonStyle` and `LinkButtonStyle` exist and work for opt-in use.
5. Existing keyed styles (e.g. `RailButtonStyle`) still function; no control loses behavior
   (combo popups open, grids sort/scroll, lists select, tabs switch).
6. No literal hex in the two control dictionaries — all colors are tokens.
7. All colors are `DynamicResource` so controls re-color on theme toggle.

## Output Requirements

### Implementation Summary
Create `Progress/VISTA_Modules/Experience/UX-03-summary.md` (template `Progress/_template.md`).
Include: the list of controls styled (and any skipped because unused), which got full templates vs.
property-only styles, the keyed variants provided, and any regressions found that UX-04 must handle.
Note `codebase_wiki` discrepancies.

### Documentation
- Header comment in each control dictionary listing the controls covered and the tokens consumed.
- XML/inline notes on any non-obvious template (e.g. the overlay scrollbar, the DataGrid header).
