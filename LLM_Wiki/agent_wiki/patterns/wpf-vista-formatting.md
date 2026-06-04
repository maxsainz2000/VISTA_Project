# WPF Vista Formatting Standards

---

```yaml
---
type: pattern
module: Infrastructure
agent: antigravity
date: 2026-06-04
tags: [wpf, xaml, vb-net, mvvm, formatting]
---
```

## Context

Displaying numbers, currency, dates, and percentages in a consistent way is crucial for a professional look and feel. Previously, formats like `StringFormat='₱{0:N2}'` were repeated inline across dozens of views. This pattern establishes a centralized formatting token and value converter architecture to ensure uniformity and culture safety across the application.

## The Pattern

All formatting literals (e.g., peso currency symbol `₱`, date formats) are centralized in a single dictionary [Formats.xaml](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Themes/Formats.xaml) and custom VB.NET value converters in [FormattingConverters.vb](file:///c:/Users/Admin/Documents/VISTA_Project/WPF_Applications/MerchSys/src/MerchSys.App/Converters/FormattingConverters.vb).

### 1. Simple Binding StringFormat
For standard bindings where `StringFormat` works directly:
```xaml
<TextBlock Text="{Binding TotalValue, StringFormat={StaticResource FormatCurrency}}"/>
<TextBlock Text="{Binding CreatedDate, StringFormat={StaticResource FormatDate}}"/>
```

### 2. Multi-binding, Run Elements, Conditional Signs, or Two-way editing
Where `StringFormat` cannot be used (e.g., inside `Run.Text`), or where culture-safe parse-back is required for two-way inputs (e.g., textbox price entry), use value converters:
```xaml
<!-- Run element format -->
<Run Text="{Binding TotalAmount, Converter={StaticResource PesoConverter}}"/>

<!-- Two-way price entry with safe parse-back -->
<TextBox Text="{Binding EditorRetailPriceText, UpdateSourceTrigger=PropertyChanged, StringFormat={StaticResource FormatQuantity}}"/>
```

### 3. DataGrid Right Alignment
All numeric columns (currency, quantity, count) must be right-aligned. The header must be aligned to match.
- Set column `HeaderStyle` to `{StaticResource RightAlignedHeaderStyle}`.
- Set column `ElementStyle` to `{StaticResource NumericCellStyle}` (or a style based on it).
- For custom/template columns, set `HorizontalAlignment="Right"` and padding.

```xaml
<DataGridTextColumn Header="Retail Price" Width="95"
                    HeaderStyle="{StaticResource RightAlignedHeaderStyle}"
                    Binding="{Binding RetailPrice, StringFormat={StaticResource FormatCurrency}}"
                    ElementStyle="{StaticResource NumericCellStyle}"/>
```

## Central Keys Defined

- `FormatCurrency`: `₱{0:N2}`
- `FormatCurrencyNoDecimal`: `₱{0:N0}`
- `FormatQuantity`: `{0:N2}`
- `FormatQuantityInt`: `{0:N0}`
- `FormatDate`: `MM/dd/yyyy`
- `FormatDateShort`: `MM/dd/yy`
- `FormatDateTime`: `MM/dd/yyyy HH:mm`
- `FormatDateTimeShort`: `MM/dd/yy HH:mm`
- `FormatPercent`: `{}{0:N1}%`
- `FormatPercentSigned`: `{}{0:+0.0;-0.0;0.0}%`

## Rules

- **Never use hardcoded `₱` or format literals in views.** Always reference resource keys (`FormatCurrency`, `FormatDate`) or converters (`PesoConverter`, `DateFormatter`).
- **Always match header alignment to column alignment.** Right-aligned numeric data grid columns must have `HeaderStyle="{StaticResource RightAlignedHeaderStyle}"`.
- **Do not break virtualization.** Avoid using complex `DataGridTemplateColumn` cell templates for plain numeric display; use `DataGridTextColumn` with `ElementStyle="{StaticResource NumericCellStyle}"`.
- **Ensure two-way bindings are safe.** When using textbox inputs for decimals/currency, use `ConvertBack` logic that parses standard culture formatting safely without throwing exceptions.

## Related

- Links to related entries: `[[patterns/wpf-vista-state-feedback.md]]`
- Links to Domain Wiki pages: `[[codebase_wiki/modules/app/services.md]]`
