---
type: error-fix
module: MerchSys.App
agent: antigravity
date: 2026-06-05
tags: [vb-net, wpf, nullable, trycast, value-type, conversion, BC30792, build-error]
error-code: BC30792
severity: build-error
---

# VB.NET TryCast Value Type Compilation Error (BC30792)

## Problem

When writing custom WPF converters or handling raw boxed values (`Object`) that should be cast to nullable types (such as `DateTime?` / `Nullable(Of DateTime)`) in VB.NET, using `TryCast` results in a compilation error:

```
error BC30792: 'TryCast' operand must be reference type, but 'Date?' is a value type.
```

(Note: VB.NET alias for `DateTime` is `Date`).

## Root Cause

`TryCast` in VB.NET is equivalent to C#'s `as` operator. It attempts to cast an object and returns `Nothing` if the cast fails. Because of this, it is strictly restricted to reference types (classes, interfaces). Since `Nullable(Of T)` (or `T?`) is a struct (value type), the compiler forbids using `TryCast` with it.

## Fix

Since boxed nullable values in .NET are either `Nothing` (if the value is null) or a boxed underlying value type (if it has a value), we can handle the check in two steps:
1. Check for `Nothing` (or `IsDBNull`).
2. Verify the type is the underlying value type (e.g. `DateTime`) using `TypeOf value Is DateTime`.
3. Use `DirectCast` to cast it.

### Before (broken)
```vb
Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
    Dim loadedAt = TryCast(value, DateTime?)
    If Not loadedAt.HasValue Then Return String.Empty
    ' ...
End Function
```

### After (fixed)
```vb
Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
    If value Is Nothing Then Return String.Empty
    If Not (TypeOf value Is DateTime) Then Return String.Empty

    Dim loadedAt = DirectCast(value, DateTime)
    ' ...
End Function
```

## Prevention

- Never use `TryCast` with value types or nullable value types (`Integer?`, `Boolean?`, `DateTime?`, etc.).
- When casting a boxed object value in custom converter methods, always check for null/`Nothing` first, then use `TypeOf ... Is T` check followed by `DirectCast` to cast to `T`.

## Related

- Links to related Agent Wiki entries: `[[vbnet-rootnamespace-relative-declarations]]`
