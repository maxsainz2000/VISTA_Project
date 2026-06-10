---
type: pattern
module: MerchSys.App
agent: antigravity
date: 2026-06-10
tags: [wpf, xaml, vb-net, mvvm, theming, design-tokens, wcag, accessibility, severity-banner, shared-kernel]
---

# Severity-Aware Insight Banners (What This Means Callouts)

## Context

Plain-language data interpretations (e.g. margin analysis, overdue credit warnings) need visual callout boxes that reflect their severity (Info, Positive, Warning). Hardcoding colors or using multiple ad-hoc visual layouts results in maintenance overhead, visual inconsistency, and theming problems.

Furthermore, because ViewModels in modular monoliths reside in module libraries that cannot reference the startup `MerchSys.App` layer (module-boundary rule), the severity enum must reside in `SharedKernel` so ViewModels can calculate and expose severity to the UI.

To comply with **WCAG 1.4.1 (Use of Color)**, severity must not be carried by hue alone; it must incorporate distinct icon shapes (Lightbulb for Info, Checkmark for Positive, Warning Triangle for Warning) alongside token-based colors (`AccentBrush`, `SuccessBrush`, `WarningBrush`).

## The Pattern

The pattern uses:
1. **`InsightSeverity` Enum in SharedKernel**: Defined in `MerchSys.SharedKernel.Enums` to cross the module boundary.
2. **Additive Service Methods**: New severity methods added to the interpretation service (e.g. `GetIncomeStatementSeverity`, `GetOverviewSeverity`) returning the enum, computed using the exact same thresholds that produce the text.
3. **`InsightBanner` Reusable Control**: A WPF `UserControl` declaring `Title`, `Text`, and `Severity` dependency properties, styled entirely via declarative `DataTrigger` styles reacting to the enum using theme-safe `DynamicResource` brushes and vectors.

### 1. The Shared Enum
```vb
Namespace Enums
    Public Enum InsightSeverity
        Info = 0
        Positive = 1
        Warning = 2
    End Enum
End Namespace
```

### 2. Service Logic Integration
```vb
Public Function GetIncomeStatementSeverity(data As IncomeStatementDto, Optional previousMargin As Decimal = -1D) As InsightSeverity _
    Implements IWhatThisMeansService.GetIncomeStatementSeverity

    ' Warning conditions (identical thresholds to interpretation text)
    If data.NetIncome < 0 Then Return InsightSeverity.Warning
    If previousMargin >= 0 Then
        Dim diff = Math.Round(previousMargin - data.GrossMarginPercent, 1)
        If diff >= MarginDropWarningThreshold Then Return InsightSeverity.Warning
    End If

    ' Positive conditions
    If data.NetIncome >= 0 Then
        If previousMargin >= 0 Then
            Dim diff = Math.Round(data.GrossMarginPercent - previousMargin, 1)
            If diff > 0 Then Return InsightSeverity.Positive
        ElseIf data.NetIncome > 0 Then
            Return InsightSeverity.Positive
        End If
    End If

    Return InsightSeverity.Info
End Function
```

### 3. Reusable XAML Triggers (WCAG 1.4.1 Compliant)
```xml
<Style x:Key="BannerBorderStyle" TargetType="Border">
    <Setter Property="Background" Value="{DynamicResource SidebarBackgroundBrush}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusSmall}"/>
    <Setter Property="Padding" Value="14,10"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding Severity, ElementName=self}" Value="{x:Static enums:InsightSeverity.Info}">
            <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}"/>
        </DataTrigger>
        <DataTrigger Binding="{Binding Severity, ElementName=self}" Value="{x:Static enums:InsightSeverity.Positive}">
            <Setter Property="BorderBrush" Value="{DynamicResource SuccessBrush}"/>
        </DataTrigger>
        <DataTrigger Binding="{Binding Severity, ElementName=self}" Value="{x:Static enums:InsightSeverity.Warning}">
            <Setter Property="BorderBrush" Value="{DynamicResource WarningBrush}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

### 4. Integration in Views
```xml
<views:InsightBanner DockPanel.Dock="Top" Margin="0,0,0,8"
                     Text="{Binding WhatThisMeansText}"
                     Severity="{Binding WhatThisMeansSeverity}"/>
```

## Why It Works

- **Strict Separation of Concerns**: The control performs no business or interpretation logic. All threshold logic remains inside the service, and ViewModels bind directly to the calculated severity.
- **Theme Safety**: DataTriggers apply `DynamicResource` brushes (`AccentBrush`, `SuccessBrush`, `WarningBrush`), meaning themes swap instantly and live when the user toggles dark mode.
- **WCAG 1.4.1 Compliance**: Shape changes (check mark, warning triangle, lightbulb) convey status even for color-blind users or in grayscale, ensuring accessibility is preserved.
- **Clean Module Boundaries**: Placing the enum in `SharedKernel` avoids circular dependencies between module ViewModels and the UI startup project.

## Related

- `[[wpf-vista-state-feedback]]`
- `[[wpf-vista-theming-conventions]]`
- `[[wpf-vista-accessibility]]`
