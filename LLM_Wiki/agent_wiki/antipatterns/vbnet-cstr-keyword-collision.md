---
name: vbnet-cstr-keyword-collision
type: antipattern
module: MerchSys.App
agent: claude-code
date: 2026-05-15
tags: [vb-net, reserved-keyword, BC30183, build-error]
---

## Context

Applies to any VB.NET file where a local variable or parameter is named `cstr`.

## The Trap

```vb
' WRONG — cstr collides with CStr() built-in
Dim cstr = $"Data Source={path}"
services.AddDbContext(Of MyDbContext)(Sub(o) o.UseSqlite(cstr))
```

VB.NET is case-insensitive. `cstr` is identical to `CStr`, the built-in type-conversion
function (equivalent to `Convert.ToString`). The compiler emits:

```
BC30183: Keyword is not valid as an identifier.
```

The cascading effect breaks every subsequent line that references `cstr`, producing a wave of
BC30199/BC30201/BC30198 parse errors.

## The Fix

```vb
' CORRECT — unambiguous local name
Dim connStr = $"Data Source={path}"
services.AddDbContext(Of MyDbContext)(Sub(o) o.UseSqlite(connStr))
```

## Rules

- Never name a variable `cstr`, `cint`, `cdbl`, `cbool`, `clng`, `csng`, `cobj`, `cdate`,
  `cbyte`, `cchar`, or any other VB.NET intrinsic conversion-function name.
- These are type-conversion keywords, not reserved words, so the IDE may not flag them red —
  but the compiler rejects them with BC30183.

## Related

- Same family as `[[vbnet-err-builtin-shadows-loop-variable]]`
- First encountered: INT-12 harness implementation (2026-05-15)
