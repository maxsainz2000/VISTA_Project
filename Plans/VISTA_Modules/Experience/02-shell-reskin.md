---
module: MerchSys.App
plan-id: UX-02
title: "Shell Reskin — MainWindow, Activity Rail, Module Detail Panel + Permanent Theme Toggle"
depends-on: [UX-01]
estimated-files: 5
---

# Shell Reskin — MainWindow, Activity Rail, Module Detail Panel + Permanent Theme Toggle

## Context

The shell (activity rail + module detail panel + content frame) is visible on **every screen**, so
re-skinning it delivers the largest perceived-quality jump for the least work. This plan converts the
shell from its current dark, hardcoded "VS Code"-style chrome to the macOS-inspired token-based look,
and gives the light/dark toggle its **permanent home** (replacing the temporary Developer Tools
button from UX-01).

Behavior is untouched: the three-column layout, `Ctrl+1..4`/`Ctrl+0` shortcuts, DI-filled
`ActivityRailSlot`/`ModuleDetailSlot`, and `CurrentView` content binding all stay exactly as-is.
Only chrome changes.

Read `UX-00` (token contract + cross-cutting rules) and `UX-01` (the tokens now exist) first.

## Prerequisites

- **UX-01** — `Tokens.xaml`, `Light.xaml`, `Dark.xaml`, `IThemeService` (singleton in DI), Inter.

## Current shell facts (verified)

- `MainWindow.xaml` — 3-col `Grid` (60 / 220 / *). Content cell hardcodes `Background="#F5F6FA"`.
  Native title bar (kept — owner declined custom chrome).
- `Views/Shell/ActivityRail.xaml` — `Background="#1A252F"`; defines `RailButtonStyle` inline with
  literal `#7F8C8D`/`#2C3E50`/`#2980B9`/`#243342`; active-item `DataTrigger` swaps the template.
- `Views/Shell/ModuleDetailPanel.xaml` — module nav items, session info, Log Out, connection status.
- `Views/Shell/ConnectionStatusIndicator.xaml` — small status chip in the detail panel.

## Deliverables

```
MerchSys.App/Views/MainWindow.xaml                       ' Modified — token backgrounds, content padding
MerchSys.App/Views/Shell/ActivityRail.xaml               ' Modified — token-based rail + selection styling
MerchSys.App/Views/Shell/ModuleDetailPanel.xaml          ' Modified — sidebar surface, nav rows, footer toggle
MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml  ' Modified — status colors → Success/Danger/Warning tokens
MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml ' Modified — remove UX-01 temporary toggle
```

(Code-behind `.xaml.vb` files are touched only if a small command for the permanent toggle is needed
and no existing shell command fits — prefer binding to an existing shell ViewModel command.)

## Specification

### Visual target (macOS-inspired, tasteful)

- **Activity rail** — `SidebarBackgroundBrush` background; icon glyphs/labels in `TextSecondaryBrush`,
  rising to `TextPrimaryBrush` on hover; the active item uses a soft `SelectionBackgroundBrush` pill
  (rounded `RadiusSmall`) rather than a hard left accent bar — closer to macOS sidebar selection.
  Keep the existing 3px accent only if it reads better after trying the pill; document the choice.
- **Module detail panel** — `SidebarBackgroundBrush` (very slightly different from the rail to imply
  layering); nav rows are full-width rounded (`RadiusSmall`) rows that highlight with
  `SelectionBackgroundBrush`/`SelectionForegroundBrush` when active and a subtle hover fill
  otherwise; `SeparatorBrush` hairlines; session info and Log Out in `TextSecondaryBrush`.
- **Content frame** — `WindowBackgroundBrush` (replaces `#F5F6FA`); add comfortable padding
  (`SpacingL`) so content "floats" with macOS-style breathing room.
- **Type** — apply `AppFontFamily` at the window root so the whole tree inherits Inter; use the
  `FontSize*` ramp for the app title and nav labels.
- **Motion** — gentle hover/selection transitions on rail + nav rows (short `ColorAnimation` /
  opacity, ~120ms ease). Keep it subtle; no bouncing.

### Token discipline

Every color in the three shell files becomes a `DynamicResource` token reference. **Zero literal hex
remains** in `MainWindow.xaml`, `ActivityRail.xaml`, `ModuleDetailPanel.xaml`,
`ConnectionStatusIndicator.xaml` after this plan. Rounded corners use `RadiusSmall`/`RadiusMedium`;
spacing uses the `Spacing*` thicknesses.

The `ActivityRail` `RailButtonStyle` and its active-state `DataTrigger`/template are rewritten to use
tokens. Preserve the `RailItem` `DataTemplate`, `SelectModuleCommand` binding, `ToolTipText`, and
`Abbreviation` content exactly.

### Permanent theme toggle

Place the light/dark toggle in the **module detail panel footer**, near Log Out / connection status —
a small control (e.g. a templated `ToggleButton` styled as a macOS-style switch, or a simple
sun/moon icon button). Bind it to a command that calls `IThemeService.Toggle()` and reflects
`IThemeService.Current`. Inject `IThemeService` into the relevant shell ViewModel (it is already a
DI singleton from UX-01); if the shell ViewModel construction is in the composition root, add the
dependency there.

Then **remove the temporary "Toggle Theme" button** from `DeveloperToolsPanel.xaml` (the UX-01
scaffolding marked with the `UX-01 temp` comment).

### Native title bar

Unchanged — no `WindowChrome`, no traffic lights. The window keeps the standard Windows caption.
(If desired later, the app icon/title text can be refined, but that is out of scope here.)

## Implementation Notes

- **Behavior is frozen.** Do not alter `Window.InputBindings`, the `Grid` column structure, the
  `ContentControl` slot names (`ActivityRailSlot`, `ModuleDetailSlot`), or the `CurrentView`
  binding. Only chrome attributes change.
- Selection styling: the existing active-item logic is a `DataTrigger` on `IsActive` that *replaces
  the whole template*. Prefer instead to keep one template and animate/swap a named background
  `Border`'s fill via the trigger — simpler and avoids duplicating the content presenter. Document
  whichever approach is used.
- Verify both themes: the rail/sidebar must look right in light **and** dark (selection contrast,
  hairline visibility). This is the first real test of palette parity from UX-01.
- MC3074: keep any `clr-namespace` mappings on the full `MerchSys.App...` root prefix.
- No new NuGet packages.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. The activity rail, module detail panel, content frame, and connection status indicator render
   with token colors (no literal hex remains in those four XAML files).
3. The app uses Inter throughout the shell.
4. Active module/nav item shows a macOS-style soft selection (rounded fill), legible in both themes.
5. The permanent theme toggle lives in the module detail panel footer, flips light/dark live, and
   reflects the current theme; the temporary Developer Tools toggle is removed.
6. All shell behavior is unchanged: `Ctrl+1..4`/`Ctrl+0` switch modules, DI slots fill, content
   navigation works, Log Out works, connection status updates.
7. The look is correct in **both** light and dark (verified by toggling at runtime).

## Output Requirements

### Implementation Summary
Create `Progress/VISTA_Modules/Experience/UX-02-summary.md` (template `Progress/_template.md`).
Include: the selection-styling approach chosen (pill vs. accent bar; single-template vs.
template-swap), the permanent toggle's control type and host ViewModel, confirmation the UX-01 temp
toggle was removed, and before/after notes for light + dark. Note any `codebase_wiki` discrepancies.

### Documentation
- Inline comment in `ActivityRail.xaml` explaining the token mapping and active-state mechanism.
- XML doc / comment on the shell ViewModel theme-toggle command.
