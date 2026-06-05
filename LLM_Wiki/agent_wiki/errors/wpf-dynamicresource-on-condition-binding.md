---
type: error-fix
module: Infrastructure
agent: claude-code
date: 2026-06-05
tags: [wpf, xaml, vb-net, triggers, reduced-motion, skeleton-loaders]
error-code: XamlParseException
severity: runtime-error
---

## Problem

Opening any view that hosts a `SkeletonBlock` (UX-26 skeleton loaders) crashed at control
instantiation with:

```
System.Windows.Markup.XamlParseException
  A 'DynamicResourceExtension' cannot be set on the 'Binding' property of type 'Condition'.
  A 'DynamicResourceExtension' can only be set on a DependencyProperty of a DependencyObject.
  at MerchSys.App.Views.Shell.SkeletonBlock.InitializeComponent() ... SkeletonBlock.xaml:line 1
```

Source: `Views/Shell/SkeletonBlock.xaml`, the shimmer `MultiDataTrigger`:

```xml
<Condition Binding="{DynamicResource MotionEnabled}" Value="True"/>
```

**The build compiled clean (0 errors / 0 warnings)** — this is a BAML *load-time* failure, not a
compile failure, so it is invisible to `dotnet build` and only surfaces when the control is actually
rendered.

## Root Cause

`Condition.Binding` is typed `System.Windows.Data.BindingBase`, and it is a **plain CLR property**,
not a `DependencyProperty`. `DynamicResource` (like any markup extension that defers to a DP) can only
be applied to a `DependencyProperty` on a `DependencyObject`. So a resource reference is structurally
illegal there — a `Condition` accepts only a real `{Binding ...}`.

The intent was to gate the shimmer storyboard on the global reduced-motion flag (`MotionEnabled`,
resolved once at startup in `Application.xaml.vb`, UX-25). A resource value cannot be read by a trigger
condition directly; there is no XAML-native "DataTrigger on a resource value."

## Fix

Expose the resource as a CLR property on the control and bind the condition to **that** (a valid
`{Binding}`). Reduced-motion is decided once at startup and never changes at runtime, so a one-time
read is correct (no `INotifyPropertyChanged` needed).

```vb
' SkeletonBlock.xaml.vb — read the global gate once, default-safe
Public ReadOnly Property ShimmerEnabled As Boolean
    Get
        Dim motionFlag As Object = Application.Current?.Resources("MotionEnabled")
        Return motionFlag Is Nothing OrElse CBool(motionFlag)
    End Get
End Property
```

```xml
<!-- Before (broken) -->
<Condition Binding="{DynamicResource MotionEnabled}" Value="True"/>

<!-- After (fixed) -->
<Condition Binding="{Binding ShimmerEnabled,
           RelativeSource={RelativeSource AncestorType=UserControl}}" Value="True"/>
```

## Prevention

- **Never put `{DynamicResource}` / `{StaticResource}` on `Condition.Binding`, `DataTrigger.Binding`,
  or any `BindingBase`-typed CLR property.** Only real `{Binding}` is legal there.
- To gate a trigger on an application/theme **resource** value, surface the resource through a CLR or
  dependency property on the control (read `Application.Current.Resources(key)`) and bind the condition
  to that property.
- A clean `dotnet build` does **not** validate XAML that lives inside `Style`/`ControlTemplate`
  triggers — those are parsed at runtime. Treat "build is 0/0" as necessary-but-not-sufficient for any
  new templated control; it must be instantiated at least once to prove the BAML loads.

## Related

- Links to related Agent Wiki entries: `[[wpf-vista-motion]]`, `[[wpf-vista-skeleton-loaders]]`,
  `[[wpf-datagrid-binds-vm-rowitem-missing-property-blank-cell]]`
- Links to Domain Wiki pages: `[[client-server-wpf]]`
