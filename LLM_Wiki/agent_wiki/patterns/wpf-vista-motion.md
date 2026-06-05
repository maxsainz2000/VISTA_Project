---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-05
tags: [wpf, xaml, motion, transitions, micro-interactions, reduced-motion]
---

# WPF Motion, Transitions & Micro-interactions

## Context

Typography and layout spacing provide structure, but animation and motion provide the "premium" feel. However, uncontrolled motion can feel slow, distracting, or cause layout reflows that degrade CPU and GPU performance. In desktop business applications, motion must be fast, subtle, and respect user accessibility settings (specifically OS-level reduced motion).

This pattern establishes centralized animation tokens, a single-gated mechanism for reduced motion, performance-optimized opacity/transform animation rules, and standard view transition templates.

## The Pattern

The pattern uses:
1. **Centralized Motion Tokens (`Themes/Tokens.xaml`)**: Gathers all durations and easing functions in a single theme file instead of hardcoding timing inline.
2. **OS Reduced Motion Gate (`Application.xaml.vb`)**: Resolves the system parameters once on startup and overrides animation durations to zero, automatically turning off motion if the user prefers reduced motion.
3. **Compositor-friendly Animations**: Animate `Opacity` and `RenderTransform` rather than `Width`, `Height`, or `Margin` to prevent triggering layout reflows.
4. **Transition Trigger Binding (`MainWindow.xaml`)**: Uses binding target updates with `NotifyOnTargetUpdated=True` to trigger transition storyboards.

### 1. Motion Tokens XAML (`Themes/Tokens.xaml`)
```xml
    <!-- Motion & Micro-interactions (UX-25) -->
    <sys:Boolean x:Key="MotionEnabled">True</sys:Boolean>
    <Duration x:Key="MotionDurationFast">0:0:0.15</Duration>
    <Duration x:Key="MotionDurationStd">0:0:0.22</Duration>
    <CubicEase x:Key="MotionEasing" EasingMode="EaseOut"/>
```

### 2. Startup Gate VB.NET (`Application.xaml.vb`)
```vb
    Private Sub Application_Startup(sender As Object, e As StartupEventArgs)
        ' Respect reduced motion (UX-25)
        Dim motionEnabled As Boolean = System.Windows.SystemParameters.ClientAreaAnimation
        Application.Current.Resources("MotionEnabled") = motionEnabled
        If Not motionEnabled Then
            Application.Current.Resources("MotionDurationFast") = New System.Windows.Duration(System.TimeSpan.Zero)
            Application.Current.Resources("MotionDurationStd") = New System.Windows.Duration(System.TimeSpan.Zero)
        End If
        
        ' ... proceed with host and window creation
    End Sub
```

### 3. Content View Transition XAML (`MainWindow.xaml`)
```xaml
    <ContentControl Content="{Binding CurrentView, NotifyOnTargetUpdated=True}">
        <ContentControl.RenderTransform>
            <TranslateTransform Y="0"/>
        </ContentControl.RenderTransform>
        <ContentControl.Triggers>
            <EventTrigger RoutedEvent="Binding.TargetUpdated">
                <BeginStoryboard>
                    <Storyboard>
                        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                         From="0.0" To="1.0"
                                         Duration="{StaticResource MotionDurationStd}"/>
                        <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(TranslateTransform.Y)"
                                         From="8.0" To="0.0"
                                         Duration="{StaticResource MotionDurationStd}"
                                         EasingFunction="{StaticResource MotionEasing}"/>
                    </Storyboard>
                </BeginStoryboard>
            </EventTrigger>
        </ContentControl.Triggers>
    </ContentControl>
```

### 4. Control State Easing via VisualStateManager (`Themes/Controls.xaml`)
Implicit templates should define states (e.g. `MouseOver`, `Pressed`) inside `VisualStateManager.VisualStateGroups` and specify transitions referencing the centralized tokens:
```xml
    <Grid>
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="CommonStates">
                <VisualStateGroup.Transitions>
                    <VisualTransition GeneratedDuration="{StaticResource MotionDurationFast}">
                        <VisualTransition.GeneratedEasingFunction>
                            <StaticResource ResourceKey="MotionEasing"/>
                        </VisualTransition.GeneratedEasingFunction>
                    </VisualTransition>
                </VisualStateGroup.Transitions>
                <VisualState x:Name="Normal"/>
                <VisualState x:Name="MouseOver">
                    <Storyboard>
                        <DoubleAnimation Storyboard.TargetName="HoverOverlay"
                                         Storyboard.TargetProperty="Opacity"
                                         To="1" Duration="0"/>
                    </Storyboard>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
        
        <Border Name="Border" Background="{TemplateBinding Background}"/>
        <Border Name="HoverOverlay" Background="{DynamicResource HoverBackgroundBrush}" Opacity="0"/>
    </Grid>
```

## Why It Works

- **Unified Performance**: Animating `Opacity` and `TranslateTransform` uses the WPF composition thread, which is GPU-accelerated. Animating layout variables like margins or widths forces UI layout passes (`Measure` and `Arrange`) that spike CPU utilization and cause visual jank.
- **Centralized timing logic**: When OS-level reduced motion is active, the durations are modified programmatically to zero before WPF XAML templates are parsed. All templates resolving `StaticResource MotionDurationFast` or `StaticResource MotionDurationStd` receive `0` seconds, making all animations instantly complete.
- **No layout reflow**: Selection and hover indicators use overlays whose `Opacity` is animated, avoiding modifications to the parent borders/backgrounds which can trigger redraws.

## Rules

- **Tokens Only**: Never hardcode duration values (`0:0:0.15` or similar) inside a `Storyboard` or a `VisualTransition`. Always reference `{StaticResource MotionDurationFast}` or `{StaticResource MotionDurationStd}`.
- **Opacity/Transforms only**: Avoid animating layout properties (`Margin`, `Width`, `Height`, `Padding`) unless implementing explicit expanding/collapsing lists/regions.
- **Degrade Gracefully**: Make sure visual components remain fully visible and usable when animations are instant (duration = 0).

## Related

- Links to related entries: `[[wpf-vista-theming-conventions]]`, `[[wpf-vista-state-feedback]]`
- Links to Domain Wiki pages: `[[client-server-wpf]]`
