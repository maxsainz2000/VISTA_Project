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

## Detector Contract

> Added 2026-05-24 after the agent-wiki audit (`Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md`)
> flagged 8 sites, all false positives — every flagged file imported only
> `Microsoft.Extensions.Logging.Abstractions`, which does not bring the `Console` class into scope.
> See `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md`.

Any audit that detects this rule MUST verify the import scope before flagging.

### Flag (positive patterns)

The file contains BOTH:

1. A `Console.` reference outside comments and string literals, AND
2. An exact import of `Imports Microsoft.Extensions.Logging` (no suffix) OR `Imports Microsoft.Extensions.Logging.Console` (explicit sub-import).

### Do NOT flag (negative patterns)

- `Imports Microsoft.Extensions.Logging.Abstractions` — sibling sub-namespace, does NOT bring `Console` into scope.
- `Imports Microsoft.Extensions.Logging.Configuration` — same reason.
- Files that import any other `Microsoft.Extensions.Logging.<suffix>` namespace that does not itself contain a `Console` member.
- `Console.` text inside `'...` comments or `"..."` string literals.

The corpus that exercises these patterns lives at `Operator/audit-tests/rule-07/` (see INFRA-18).

## Related

- First encountered: INT-12 harness implementation (2026-05-15)
