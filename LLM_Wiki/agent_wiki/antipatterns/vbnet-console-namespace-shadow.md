---
name: vbnet-console-namespace-shadow
type: antipattern
module: MerchSys.App
agent: claude-code
date: 2026-05-15
tags: [vb-net, namespace, BC30456, build-error, imports]
---

## Context

Applies to any VB.NET file that has `Imports Microsoft.Extensions.Logging` in scope and uses
`Console.WriteLine()`.

## The Trap

```vb
Imports Microsoft.Extensions.Logging   ' brings Console class into scope

...
Console.WriteLine("done")             ' WRONG — resolves to MEL's Console, not System.Console
```

`Microsoft.Extensions.Logging` contains a class named `Console` (the logging provider helper).
With that namespace imported, unqualified `Console` resolves to
`Microsoft.Extensions.Logging.Console` which has no `WriteLine` method. Error:

```
BC30456: 'WriteLine' is not a member of 'Microsoft.Extensions.Logging.Console'.
```

## The Fix

```vb
System.Console.WriteLine("done")   ' fully qualified — unambiguous
```

## Rules

- Whenever `Imports Microsoft.Extensions.Logging` is present, always qualify console output as
  `System.Console.WriteLine()`.
- Same hazard exists for `Imports Microsoft.Extensions.Logging.Console` (explicit sub-import).
- Prefer structured logging (`_logger.LogDebug(...)`) inside harness and service code; reserve
  `System.Console.WriteLine` for top-level diagnostic output that must survive without a logger.

## Related

- First encountered: INT-12 harness implementation (2026-05-15)
