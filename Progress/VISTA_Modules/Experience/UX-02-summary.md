# UX-02: Shell Reskin — MainWindow, Activity Rail, Module Detail Panel + Permanent Theme Toggle

---

```yaml
---
module: MerchSys.App
agent: antigravity
date: 2026-06-02
plan-ref: Plans/VISTA_Modules/Experience/02-shell-reskin.md
status: completed
---
```

## Task Summary

This report documents the implementation of the shell visual reskin of the VISTA application (`MerchSys.App`). The VS Code-style dark chrome has been replaced with a macOS-inspired, token-based premium layout utilizing colors, typography, radii, and spacing tokens established in `UX-01`. 

## Design Choices & Specifications

### 1. Selection Styling (Pill vs. Accent Bar; Single-Template vs. Template-Swap)
- **Choice:** Soft selection pill with rounded corners (`RadiusSmall` / `6` corner radius) and a single `ControlTemplate` using opacity-animated border overlays.
- **Rationale:** 
  - The macOS look requires a soft pill rather than a hard vertical accent bar. 
  - To implement subtle transitions and support clean runtime theme-swapping, we rewrote `RailButtonStyle` and `NavItemStyle` into unified `ControlTemplate` structures featuring named overlay elements (`ActiveBg`, `HoverBg`). 
  - By animating the `Opacity` properties of these borders from `0` to `1` over `120ms` in XAML triggers (instead of animating the brush properties directly), we completely avoid WPF rendering engine errors related to animating dynamic resource references. This single-template pattern simplifies the XAML and eliminates duplication of the `ContentPresenter`.

### 2. Permanent Theme Toggle (Control Type & Host ViewModel)
- **Choice:** A custom-templated `ToggleButton` styled as a macOS-style sliding switch, hosted in `MainWindowViewModel`.
- **Details:** 
  - A new boolean property `IsDarkTheme` and command `ToggleThemeCommand` were added to `MainWindowViewModel` via Dependency Injection of the `IThemeService`.
  - The toggle switch is placed in the bottom footer of the `ModuleDetailPanel` next to the "Log Out" button. When clicked, it fires `ToggleThemeCommand` which calls `IThemeService.Toggle()` and raises property change notifications for `IsDarkTheme` to sync the state.
  - The temporary "Toggle Theme" button from `DeveloperToolsPanel.xaml` (and its click handler in code-behind) was completely removed.

### 3. Connection Status Indicator Colors
- **Choice:** The VM's hardcoded color property was bypassed in XAML by implementing `DataTrigger`s on the border's Style mapping VM state properties (`IsReconnecting`, `IsRetryVisible`) directly to token brushes (`SuccessBrush`, `WarningBrush`, `DangerBrush`).
- **Parity:** This ensures that connection state colors automatically adapt to Light and Dark palettes, and removes all hardcoded hex values.

### 4. Layout Spacing
- Mixed margin/padding values (such as `12,6,12,0`) are defined using numerical spacing equivalents (e.g. `12` for `SpacingM`, `16` for `SpacingL`) to ensure compilation success. This is because WPF does not support mixing DynamicResource markup extensions with literal numbers in thickness shorthand syntax, nor does it support DynamicResource in Thickness sub-properties. Uniform thicknesses (like `Padding="{DynamicResource SpacingL}"`) continue to use token references directly.

---

## What Was Done

- **Modified** [MainWindowViewModel.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/ViewModels/MainWindowViewModel.vb) — Injected `IThemeService`, exposed `IsDarkTheme` property and `ToggleThemeCommand` for the permanent switch.
- **Modified** [MainWindow.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/MainWindow.xaml) — Set root `FontFamily` to use the `AppFontFamily` token (Inter) and wrapped the content area in a padded `Border` with `WindowBackgroundBrush` to float the canvas.
- **Modified** [ActivityRail.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ActivityRail.xaml) — Updated background to `SidebarBackgroundBrush`, added a right-side separator hairline, updated typography/spacings, and applied the single-template opacity-animated selection pill button style.
- **Modified** [ModuleDetailPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ModuleDetailPanel.xaml) — Applied `SidebarBackgroundBrush` and right-side separator hairline. Restyled nav list items with soft selection pills. Restyled "Log Out" button. Added the animated macOS-style theme toggle switch to the footer grid.
- **Modified** [ConnectionStatusIndicator.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/ConnectionStatusIndicator.xaml) — Removed hardcoded hex values, bound retry button hover to a transparent white local brush, and mapped states to `SuccessBrush`/`WarningBrush`/`DangerBrush` using XAML `DataTrigger`s.
- **Modified** [DeveloperToolsPanel.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml) — Removed temporary theme toggle button.
- **Modified** [DeveloperToolsPanel.xaml.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Views/Shell/Modules/DeveloperToolsPanel.xaml.vb) — Removed temporary code-behind event handler.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (Build succeeded with 0 errors, 0 warnings) |
| Unit tests pass | ✅ N/A |
| Manual verification | ✅ (All components themed, theme toggle live, shortcuts verify) |

## Issues Encountered

- **Issue:** WPF compilation failed when mixing `DynamicResource` tokens with literal numbers in thickness attributes (e.g. `Margin="{DynamicResource SpacingXS},0,{DynamicResource SpacingXS},0"`).
  - **Resolution:** Replaced mixed Thickness attributes with standard numerical representations of the spacing tokens (e.g. `4,0,4,4` or `12,0,12,0`). Because spacing values are numerical coordinates and contain no color hex representations, they comply with the zero hardcoded hex mandate. Uniform thicknesses use tokens directly.

## What's Next

- **UX-03** — Implement implicit theme-aware styles for common WPF controls (Button, TextBox, ListBox, DataGrid, etc.).
- **UX-04** — Migrate all remaining pages and views to resolve colors exclusively via tokens.

## Codebase Wiki Discrepancies
- Checked [di-registry.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/codebase_wiki/schemas/di-registry.md). `IThemeService` and `ThemeService` are correctly documented. No discrepancies found.
