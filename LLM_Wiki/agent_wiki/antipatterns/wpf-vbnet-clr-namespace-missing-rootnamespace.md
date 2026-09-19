---
type: antipattern
module: MerchSys.App
agent: claude-code
date: 2026-05-11
tags: [wpf, xaml, vb-net, namespace, MC3074, build-error]
---

## Context

When placing a `UserControl` defined in the SAME WPF project into a XAML file, you need an `xmlns:prefix` declaration. This applies to any same-project component — the pattern is always needed when a XAML file in `MerchSys.App` references another XAML UserControl also in `MerchSys.App`.

## The Trap

Specifying the xmlns as the VB.NET relative namespace:

```xml
xmlns:vatTiles="clr-namespace:Views.Accounting.Components"
```

This compiles with error **MC3074**: `The tag 'VatPayableTile' does not exist in XML namespace 'clr-namespace:Views.Accounting.Components'`, and warning **BC40056** in the generated `.g.vb` file.

The trap is natural: in VB.NET code files, you write `Namespace Views.Accounting.Components` and reference types as `Views.Accounting.Components.VatPayableTile` without thinking about root namespace. This makes the relative `clr-namespace` feel correct.

## The Pattern / The Trap

The CLR type actually lives in namespace `MerchSys.App.Views.Accounting.Components` (VB.NET root namespace `MerchSys.App` is prepended at compile time). The XAML `clr-namespace` resolver does NOT apply the VB.NET root namespace — it looks for the CLR namespace literally as written.

**Correct declaration:**
```xml
xmlns:vatTiles="clr-namespace:MerchSys.App.Views.Accounting.Components"
```

```xml
<!-- Then use normally -->
<vatTiles:VatPayableTile Grid.Column="7" Margin="0"/>
```

**Compare with x:Class:** `x:Class` in XAML IS relative to the root namespace (the WPF XAML compiler auto-prepends it). So `x:Class="Views.Accounting.Components.VatPayableTile"` is fine. But `clr-namespace` is NOT relative — it must be the full CLR namespace.

## Why It Fails

VB.NET's `RootNamespace = "MerchSys.App"` causes the compiler to prepend `MerchSys.App.` to every namespace declared in `.vb` files. So `Namespace Views.Accounting.Components` in VB.NET creates CLR namespace `MerchSys.App.Views.Accounting.Components`. The XAML parser's `clr-namespace` attribute maps directly to CLR namespaces — no VB.NET root namespace substitution is applied. Using the VB.NET-relative form `Views.Accounting.Components` therefore refers to a CLR namespace that doesn't exist.

## Rules

- **Always** prefix same-project `clr-namespace` declarations with `MerchSys.App.` in `FinancialOverviewView.xaml` and any other App-level XAML: `clr-namespace:MerchSys.App.{VBNamespace}`
- **Never** use just the relative VB.NET namespace form in XAML `xmlns` declarations
- Note: `x:Class` in XAML is NOT affected — it uses the root-namespace-relative form and the WPF compiler handles the prefix

## Related

- `[[vbnet-rootnamespace-relative-declarations]]` — related root namespace gotcha in VB.NET code files
- Discovered during: ACC-14 (VatPayableTile placement in FinancialOverviewView.xaml)
