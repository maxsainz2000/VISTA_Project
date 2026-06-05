---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-05
tags: [wpf, xaml, skeleton-loaders, loading-states, first-load, shimmer, reduced-motion, vb-net]
---

# WPF Skeleton Loaders

## Context

When a data-heavy page or dashboard is opened for the first time, displaying a blank screen with a central loading spinner can feel slow, disrupt visual flow, and cause sudden layout jumps once the data renders. Skeleton loaders solve this by immediately showing content-shaped silhouettes of the elements (metric cards, lists, tables) with a soft horizontal shimmer animation. This reduces *perceived* latency and guarantees a smooth, jump-free transition when real data arrives.

However, skeleton loaders should only show on **first-load** (empty state -> data). Showing them during a background refresh or reload of already-loaded data would blank out the screen and flash, creating a poor user experience. On refreshes, a lighter indicator (like a top-level busy spinner overlay) should be used instead. Additionally, loaders must support accessibility settings like OS-level reduced motion (no shimmer animation, static shapes only) and adapt dynamically to theme changes (Light/Dark).

## The Pattern

The VISTA skeleton loader pattern is implemented using:
1. **The First-Load Discriminator (`LastLoadedAt Is Nothing`)**: Skeletons only render when the data has not been loaded yet.
2. **SkeletonBlock Control**: A low-level visual shape component displaying a grey background (`SkeletonBaseBrush`) and an overlay gradient shimmer that respects the `{DynamicResource MotionEnabled}` reduced-motion gate.
3. **SkeletonPanel Control**: A container that groups `SkeletonBlock`s into card/dashboard layouts (`Kind="Cards"`) or table row layouts (`Kind="Rows"`).
4. **Conditional Layout Toggling**: Main content collapses, `SkeletonPanel` shows on first-load, and `BusyOverlay` shows on reloads.

### 1. The Low-Level Shape: `SkeletonBlock`

`SkeletonBlock` is a basic placeholder shape that automatically runs a shimmer storyboard when visible, but gates the animation via `MotionEnabled` to allow a static grey fallback for reduced motion:

**SkeletonBlock.xaml**
```xml
<UserControl x:Class="Views.Shell.SkeletonBlock"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid>
        <!-- Base solid grey shape -->
        <Border Background="{DynamicResource SkeletonBaseBrush}"
                CornerRadius="{Binding CornerRadius, RelativeSource={RelativeSource AncestorType=UserControl}}"/>

        <!-- Shimmer overlay (only shown when motion is enabled) -->
        <Border CornerRadius="{Binding CornerRadius, RelativeSource={RelativeSource AncestorType=UserControl}}">
            <Border.Style>
                <Style TargetType="Border">
                    <Setter Property="Visibility" Value="Collapsed"/>
                    <Setter Property="Background">
                        <Setter.Value>
                            <LinearGradientBrush StartPoint="-1.5,0" EndPoint="0,0">
                                <GradientStop Color="Transparent" Offset="0"/>
                                <GradientStop Color="{DynamicResource SkeletonShimmerHighlightColor}" Offset="0.5"/>
                                <GradientStop Color="Transparent" Offset="1"/>
                            </LinearGradientBrush>
                        </Setter.Value>
                    </Setter>
                    <Style.Triggers>
                        <MultiDataTrigger>
                            <MultiDataTrigger.Conditions>
                                <Condition Binding="{DynamicResource MotionEnabled}" Value="True"/>
                                <Condition Binding="{Binding Visibility, RelativeSource={RelativeSource AncestorType=UserControl}}" Value="Visible"/>
                            </MultiDataTrigger.Conditions>
                            <Setter Property="Visibility" Value="Visible"/>
                            <MultiDataTrigger.EnterActions>
                                <BeginStoryboard Name="ShimmerStoryboard">
                                    <Storyboard>
                                        <PointAnimation Storyboard.TargetProperty="(Border.Background).(LinearGradientBrush.StartPoint)"
                                                        From="-1.5,0" To="1.5,0" Duration="0:0:1.8" RepeatBehavior="Forever"/>
                                        <PointAnimation Storyboard.TargetProperty="(Border.Background).(LinearGradientBrush.EndPoint)"
                                                        From="0,0" To="3.0,0" Duration="0:0:1.8" RepeatBehavior="Forever"/>
                                    </Storyboard>
                                </BeginStoryboard>
                            </MultiDataTrigger.EnterActions>
                            <MultiDataTrigger.ExitActions>
                                <StopStoryboard BeginStoryboardName="ShimmerStoryboard"/>
                            </MultiDataTrigger.ExitActions>
                        </MultiDataTrigger>
                    </Style.Triggers>
                </Style>
            </Border.Style>
        </Border>
    </Grid>
</UserControl>
```

**SkeletonBlock.xaml.vb (Code-Behind)**
```vb
Imports System.Windows
Imports System.Windows.Controls

Namespace Views.Shell
    Public Class SkeletonBlock
        Inherits UserControl

        Public Shared ReadOnly CornerRadiusProperty As DependencyProperty =
            DependencyProperty.Register("CornerRadius", GetType(CornerRadius), GetType(SkeletonBlock), New PropertyMetadata(New CornerRadius(4)))

        Public Property CornerRadius As CornerRadius
            Get
                Return CType(GetValue(CornerRadiusProperty), CornerRadius)
            End Get
            Set(value As CornerRadius)
                SetValue(CornerRadiusProperty, value)
            End Set
        End Property

        Public Sub New()
            InitializeComponent()
        End Sub
    End Class
End Namespace
```

### 2. The High-Level Layout: `SkeletonPanel`

`SkeletonPanel` maps multiple `SkeletonBlock` elements to pre-defined layout kinds (e.g. `Cards` for dashboard layouts, `Rows` for lists) and resolves its own visibility state based on `IsBusy` and `LastLoadedAt`:

**SkeletonPanel.xaml**
```xml
<UserControl x:Class="Views.Shell.SkeletonPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:views="clr-namespace:MerchSys.App.Views.Shell">
    <UserControl.Style>
        <Style TargetType="UserControl">
            <Setter Property="Visibility" Value="Collapsed"/>
            <Style.Triggers>
                <MultiDataTrigger>
                    <MultiDataTrigger.Conditions>
                        <Condition Binding="{Binding IsBusy, RelativeSource={RelativeSource Self}}" Value="True"/>
                        <Condition Binding="{Binding LastLoadedAt, RelativeSource={RelativeSource Self}}" Value="{x:Null}"/>
                    </MultiDataTrigger.Conditions>
                    <Setter Property="Visibility" Value="Visible"/>
                </MultiDataTrigger>
            </Style.Triggers>
        </Style>
    </UserControl.Style>
    <Grid>
        <!-- Cards Template -->
        <DockPanel x:Name="CardsPanel">
            <DockPanel.Style>
                <Style TargetType="DockPanel">
                    <Setter Property="Visibility" Value="Collapsed"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding Kind, RelativeSource={RelativeSource AncestorType=UserControl}}" Value="Cards">
                            <Setter Property="Visibility" Value="Visible"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </DockPanel.Style>
            <!-- Dashboard KPI boxes + Grid representation -->
            ...
        </DockPanel>
        
        <!-- Rows Template -->
        <DockPanel x:Name="RowsPanel">
            <!-- Table grid representation -->
            ...
        </DockPanel>
    </Grid>
</UserControl>
```

### 3. Coexistence and Trigger Setup (The Views)

When adopting the skeleton loaders, views must follow this standard setup:

1. **Hide Main Content Container on First-Load:**
   Bind the main container's `Visibility` via a `MultiDataTrigger` checking if `IsBusy` is true and `LastLoadedAt` is null:
   ```xml
   <DockPanel Margin="10">
       <DockPanel.Style>
           <Style TargetType="DockPanel">
               <Setter Property="Visibility" Value="Visible"/>
               <Style.Triggers>
                   <MultiDataTrigger>
                       <MultiDataTrigger.Conditions>
                           <Condition Binding="{Binding IsBusy}" Value="True"/>
                           <Condition Binding="{Binding LastLoadedAt}" Value="{x:Null}"/>
                       </MultiDataTrigger.Conditions>
                       <Setter Property="Visibility" Value="Collapsed"/>
                   </MultiDataTrigger>
               </Style.Triggers>
           </Style>
       </DockPanel.Style>
       <!-- ... real view content ... -->
   </DockPanel>
   ```

2. **Add `SkeletonPanel` & Update `BusyOverlay`:**
   Add `SkeletonPanel` with `Kind` set to `"Cards"` (dashboards) or `"Rows"` (lists). Bind both the panel and `BusyOverlay` to the VM's `IsBusy` and `LastLoadedAt` properties:
   ```xml
   <views:SkeletonPanel Kind="Cards" IsBusy="{Binding IsBusy}" LastLoadedAt="{Binding LastLoadedAt}" Margin="10"/>
   <views:BusyOverlay Message="Loading..." IsBusy="{Binding IsBusy}" LastLoadedAt="{Binding LastLoadedAt}"/>
   ```

## Why It Works

- **No Layout Jumps:** Skeletons mimic the exact layouts and sizes of the dashboard/grid columns. When the data arrives, the layout is already expanded to the correct size, resulting in a zero-shift transition.
- **Flashing Avoided on Refreshes:** Because of the `LastLoadedAt` null guard, reloads of already-displayed data leave the visible content on screen, and `BusyOverlay` renders a spinner overlay on top, indicating activity without disturbing the scroll position or content.
- **Centralized Accessibility Gate:** Storyboards are triggered only when `MotionEnabled` is `True`. In environments where windows animations are turned off, the linear gradient overlay is collapsed, showing static grey skeleton frames (conforming to WCAG reduced-motion standards).
- **Theme Reactivity:** Colors are tokenized using `SkeletonBaseBrush` and `SkeletonShimmerHighlightColor` defined in theme dictionaries, adjusting correctly on theme toggle (Light vs Dark).

## Rules

1. **Maintain the Discriminator:** Always bind both `IsBusy` and `LastLoadedAt` properties to the `SkeletonPanel` and `BusyOverlay` instances to separate first-load states from reloads.
2. **Never Animate Shared Frozen Brushes:** WPF resources loaded from dictionaries are frozen and cannot be animated directly. Always declare the gradient brush locally inside the element style and animate its properties there.
3. **Opt-In Coexistence:** Non-adopted views retain their old `BusyOverlay` behavior by binding `Visibility="{Binding IsBusy, Converter={StaticResource BoolToVis}}"` directly. Explicitly declared visibility tags take precedence over style-level triggers.

## Related

- `[[wpf-vista-motion]]`
- `[[wpf-vista-freshness-chip]]`
- `[[wpf-vista-state-feedback]]`
- `[[wpf-vista-theming-conventions]]`
