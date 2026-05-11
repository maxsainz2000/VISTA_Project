---
type: antipattern
module: MerchSys.POS
agent: claude-code
date: 2026-05-11
tags: [vb-net, reserved-keyword, for-each, BC30068, BC30311, build-error]
error-code: BC30068, BC30311
severity: build-error
---

## Problem

Using `err` as a `For Each` loop variable causes two build errors:

```
BC30068: Expression is a value and therefore cannot be the target of an assignment.
BC30311: Value of type 'ErrObject' cannot be converted to 'String'.
```

Example broken code:
```vb
For Each err In result.ValidationErrors
    _validationErrors.Add(err)   ' BC30068 on assignment, BC30311 on conversion
Next
```

## Root Cause

`Err` is a VB.NET built-in global object (`Microsoft.VisualBasic.ErrObject`). The compiler is case-insensitive (`err` = `Err`), so the loop variable is treated as the global `Err` object, which is read-only and typed as `ErrObject`, not `String`.

## Fix

Rename the loop variable to anything that doesn't clash with the built-in:

```vb
' After (fixed)
For Each errMsg In result.ValidationErrors
    _validationErrors.Add(errMsg)
Next
```

## Prevention

- Never use `err` as a variable name in VB.NET. Use `errMsg`, `errorText`, `msg`, etc.
- Other reserved/built-in names to avoid as variables: `Err`, `Error`, `Now`, `Date`, `Nothing`, `True`, `False`.
- The error code pair BC30068 + BC30311 appearing together in a `For Each` is a reliable signal of this collision.

## Related

- [[vbnet-reserved-keyword-enum-member]] — similar issue with reserved keywords
