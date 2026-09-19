---
module: MerchSys.App
agent: antigravity
date: 2026-06-02
plan-ref: Plans/VISTA_Modules/Experience/01-theme-foundation.md
status: completed
---

## Task Summary

Implemented the theme foundation for the VISTA macOS-inspired reskin. Created design tokens (font family, corner radii, spacing, drop shadow effect, and type scale font sizes), light and dark color palettes, and integrated the Inter font family. Developed a live-swapping, JSON-persisted theme toggle service, registered it in dependency injection, applied it on startup, and added a temporary developer toggle to verify behavior.

**Plan:** `[[01-theme-foundation]]`

## What Was Done

- Copied SIL OFL-licensed Inter font files (`Inter-Regular.ttf`, `Inter-Medium.ttf`, `Inter-SemiBold.ttf`) and `OFL.txt` license to `WPF_Applications/MerchSys/src/MerchSys.App/Fonts/`.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/MerchSys.App.vbproj` to include font files as resources.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Themes/Tokens.xaml` containing theme-agnostic structure tokens.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Themes/Light.xaml` defining the light color palette keys and values.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Themes/Dark.xaml` defining the dark color palette keys and values.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Services/Theming/AppTheme.vb` defining `AppTheme` (Light/Dark) enum.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Services/Theming/IThemeService.vb` defining `IThemeService` interface.
- Created `WPF_Applications/MerchSys/src/MerchSys.App/Services/Theming/ThemeService.vb` implementing `IThemeService` with MergedDictionaries hot-swapping and local JSON persistence.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml` to merge `Tokens.xaml` and `Light.xaml` into resources.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Application.xaml.vb` to register `IThemeService` in DI and apply the persisted theme on startup.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ModuleDetailPanel.xaml` to load `DeveloperToolsPanel` when active.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml` to display the "Toggle Theme" button.
- Modified `WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml.vb` to bind the button to `IThemeService.Toggle()`.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 errors, 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Verified key parity and successful compilation) |

## Implementation Details

### 1. Inter Font Integration
- **Pack URI:** `pack://application:,,,/Fonts/#Inter`
- **Family Name:** `Inter` (shares Regular, Medium, and SemiBold weights via `FontWeight` property).
- Includes the SIL Open Font License v1.1 at `Fonts/OFL.txt`.

### 2. Design Token Keys & Parity
The following 16 keys were defined identically in both `Light.xaml` and `Dark.xaml`, ensuring 100% key parity (verified programmatically):
- `ShadowColor` (Plain Color resource)
- `WindowBackgroundBrush`
- `SurfaceBrush`
- `SidebarBackgroundBrush`
- `ControlBackgroundBrush`
- `TextPrimaryBrush`
- `TextSecondaryBrush`
- `TextOnAccentBrush`
- `AccentBrush`
- `AccentHoverBrush`
- `SeparatorBrush`
- `SelectionBackgroundBrush` (for Dark theme, uses 28% alpha: `#470A84FF`)
- `SelectionForegroundBrush`
- `DangerBrush`
- `WarningBrush`
- `SuccessBrush`

### 3. Theme Service and Live Swapping
- **Swap Mechanism:** `ThemeService.Apply` iterates through `Application.Current.Resources.MergedDictionaries`. It locates the entry whose `Source` URL ends in `Light.xaml` or `Dark.xaml`, removes it, and replaces it with the newly selected theme dictionary. This triggers a runtime refresh of all UI components using the `DynamicResource` markup extension.
- **Persistence Path:** `%LOCALAPPDATA%\MerchSys\ui-settings.json`
- **Format:** `{ "theme": "Dark" }`
- **Startup Integration:** Applied in `Application_Startup` in `Application.xaml.vb` right before `ShowLoginView()`.

### 4. Temporary Developer Toggle
- Integrated in the `DeveloperToolsPanel.xaml` UserControl under `WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml`.
- Wired to the code-behind event `ToggleTheme_Click` which calls `themeService.Toggle()`.
- *Note:* This button is a temporary scaffold; UX-02 reskins the entire shell and places a permanent toggle button in its designated home.

## Codebase Wiki Discrepancies
- None noticed.

## Cross-References
- Domain Wiki pages consulted: `[[macos-theme-overview]]`
- Agent Wiki entries consulted: None
