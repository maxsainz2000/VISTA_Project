---
name: wpf-vista-ui-settings-persistence
description: Pattern for extending the shared ui-settings.json store to add new per-laptop UI preferences alongside the existing theme key, including off-screen clamp and RestoreBounds maximized handling
metadata:
  type: pattern
  module: MerchSys.App
  tags: [wpf, vb-net, persistence, window-placement, ui-settings, localappdata, json, theming]
  agent: claude-code
  date: 2026-06-05
---

## Context

VISTA persists per-laptop UI preferences to `%LOCALAPPDATA%\MerchSys\ui-settings.json`. The file
started as a single-key `{"theme": "Dark"}` file written by `ThemeService`. UX-23 extended it to
carry window-placement data (size, position, maximized state) alongside the theme key. This entry
documents the multi-key upgrade pattern and the WPF-specific window-placement mechanics that
Pro P3 (personalization) will extend further.

## The Pattern

### 1. Single Typed Store (`UiSettingsStore`)

All read/write of `ui-settings.json` goes through one singleton: `Services/UiSettingsStore.vb`.
It holds every known key as an in-memory property, loads the file once in its constructor (eager),
and exposes a `Save()` method that serializes all keys together.

```vb
' Services/UiSettingsStore.vb  (registered: services.AddSingleton(Of UiSettingsStore)())
Public Class UiSettingsStore
    Public Property Theme As String = "Light"
    Public Property WindowLeft As Double = Double.NaN  ' NaN = no saved placement
    Public Property WindowTop As Double = Double.NaN
    Public Property WindowWidth As Double = 1280.0
    Public Property WindowHeight As Double = 720.0
    Public Property WindowMaximized As Boolean = True

    Public Sub New()
        LoadFromDisk()  ' eager — available before any service reads it
    End Sub
    ' ...
End Class
```

**Why a shared store?** Each service that writes the file must preserve every other service's keys.
Two independent file writers (one for theme, one for placement) inevitably clobber each other on
concurrent or sequential saves. The store serializes all keys in one `File.WriteAllText` call.

### 2. Reading JSON: `System.Text.Json.JsonDocument` (BCL, no NuGet)

`System.Text.Json` is part of the .NET 10 BCL. No NuGet reference needed.

```vb
Imports System.Text.Json

Using doc = JsonDocument.Parse(json)
    Dim root = doc.RootElement
    Dim prop As JsonElement  ' reused across TryGetProperty calls

    If root.TryGetProperty("theme", prop) AndAlso prop.ValueKind = JsonValueKind.String Then
        Theme = If(prop.GetString(), "Light")
    End If

    If root.TryGetProperty("windowLeft", prop) AndAlso prop.ValueKind = JsonValueKind.Number Then
        WindowLeft = prop.GetDouble()
    End If

    ' Boolean keys need an explicit True/False kind check:
    If root.TryGetProperty("windowMaximized", prop) AndAlso
       (prop.ValueKind = JsonValueKind.True OrElse prop.ValueKind = JsonValueKind.False) Then
        WindowMaximized = prop.GetBoolean()
    End If
End Using
```

`TryGetProperty(name As String, ByRef value As JsonElement)` works correctly in VB.NET.
Declare `prop As JsonElement` once and reuse it.

### 3. Writing JSON: hand-rolled string (matching existing approach)

```vb
Dim inv = System.Globalization.CultureInfo.InvariantCulture
Dim leftStr = If(Double.IsNaN(WindowLeft), "null", WindowLeft.ToString("G", inv))
' ...
Dim json = $"{{""theme"":""{themeEsc}"",""windowLeft"":{leftStr},...,""windowMaximized"":{maxStr}}}"
File.WriteAllText(_filePath, json)
```

Use `InvariantCulture` for all floating-point formatting so decimal separators are locale-safe.
Escape theme strings: `.Replace("\", "\\").Replace("""", "\""")`.

### 4. Off-Screen Clamp (`WindowPlacementService.LoadPlacement`)

Before applying a saved window rectangle, validate it against the current virtual screen.
Require at least 100 × 30 px to be visible (title bar reachable); clamp rather than reject
if only partially off-screen.

```vb
Dim vsLeft = SystemParameters.VirtualScreenLeft
Dim vsTop = SystemParameters.VirtualScreenTop
Dim vsRight = vsLeft + SystemParameters.VirtualScreenWidth
Dim vsBottom = vsTop + SystemParameters.VirtualScreenHeight

Dim visibleH = Math.Min(windowRight, vsRight) - Math.Max(savedLeft, vsLeft)
Dim visibleV = Math.Min(windowBottom, vsBottom) - Math.Max(savedTop, vsTop)

If visibleH < 100.0 OrElse visibleV < 30.0 Then
    Return Nothing  ' off-screen → caller uses first-run defaults (CenterScreen + Maximized)
End If

' Clamp
Dim clampedLeft = Math.Max(vsLeft, Math.Min(savedLeft, vsRight - 100.0))
Dim clampedTop  = Math.Max(vsTop,  Math.Min(savedTop,  vsBottom - 30.0))
```

Use `SystemParameters.VirtualScreen*` — these aggregate all connected monitors (not just the
primary). `SystemParameters.WorkArea` only covers the primary screen.

### 5. RestoreBounds Handling — Save

When saving, always persist the **restore bounds** (normal window size), not the maximized extent:

```vb
Dim isMax = (window.WindowState = WindowState.Maximized)
Dim bounds As Rect = If(isMax, window.RestoreBounds,
                         New Rect(window.Left, window.Top, window.Width, window.Height))
```

`window.RestoreBounds` is only valid when `WindowState` is `Maximized` or `Minimized`. It holds
the normal size/position the window had before it was maximized — the data the user actually
wants restored when they un-maximize.

### 6. RestoreBounds Handling — Restore (MainWindow constructor + Loaded)

The correct two-step sequence to restore a previously-maximized window without losing the restore
bounds:

```vb
' Step 1 — Constructor: apply normal bounds before Show()
WindowStartupLocation = WindowStartupLocation.Manual
Left   = placement.Left   ' saved restore bounds (NOT full-screen extent)
Top    = placement.Top
Width  = placement.Width
Height = placement.Height
_pendingMaximize = placement.IsMaximized

' Step 2 — Loaded event: maximize AFTER the visual tree is ready
If _pendingMaximize Then
    WindowState = WindowState.Maximized
End If
```

**Why defer to `Loaded`?** Setting `WindowState = Maximized` in the constructor (before `Show()`)
causes WPF to record whatever `Left/Top/Width/Height` the window had at that moment as
`RestoreBounds` — which might be 0/0/1280/720 (XAML defaults). Deferring to `Loaded` lets WPF
set `RestoreBounds` from the explicit bounds you assigned in the constructor, so un-maximizing
returns to the user's last normal position/size.

### 7. First-Run Defaults

When `LoadPlacement()` returns `Nothing` (no saved state or off-screen):

```vb
WindowStartupLocation = WindowStartupLocation.CenterScreen
_pendingMaximize = True  ' maximized in Loaded
```

Remove `WindowState="Maximized"` and `WindowStartupLocation="CenterScreen"` from XAML — set them
exclusively from code so first-run vs. restored paths are controlled in one place.

### 8. Adding a New Preference to the Store (future Pro P3 reference)

1. Add a property to `UiSettingsStore` with a sensible default.
2. Add a `TryGetProperty` read in `LoadFromDisk`.
3. Add the key to the hand-rolled JSON string in `Save()`.
4. Create a new service (or extend an existing one) that reads/writes the property and calls
   `_store.Save()`.
5. Register the new service as a singleton if it has no per-request state.

No file-path or folder changes needed — the store already handles directory creation.

## Rules

- **Never read or write `ui-settings.json` directly from a service.** All reads go through
  `UiSettingsStore` properties (set at construction); all writes call `_store.Save()`.
- **Always persist `RestoreBounds` when maximized**, not `Left/Top/Width/Height`. Those are the
  screen-filling extent, not the user's window size.
- **Apply bounds in the constructor, `WindowState.Maximized` in `Loaded`** so WPF records the
  correct `RestoreBounds`.
- **Use `SystemParameters.VirtualScreen*` for off-screen checks** — `WorkArea` misses secondary
  monitors.
- **Validate with both axes ≥ threshold before clamping** — a window that's entirely off
  horizontally (disconnected monitor) needs the fallback, not a clamp.
- **Wrap all disk I/O in Try/Catch** — corrupt file must never crash startup or close.
- **No new NuGet.** `System.Text.Json` is BCL in .NET 10.

## Related

- `[[wpf-mainwindow-not-shell-window]]` — `Application.Current.MainWindow` is the LoginView, not
  the shell; iterate `Application.Current.Windows` to find the shell by DataContext type.
- `[[wpf-vista-theming-conventions]]` — the broader theme / design-token conventions.
