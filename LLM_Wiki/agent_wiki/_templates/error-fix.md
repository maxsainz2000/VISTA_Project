# Template: Error Fix

Copy this template when documenting a bug fix in `agent_wiki/errors/`.

---

```yaml
---
type: error-fix
module: MerchSys.Purchasing | MerchSys.Inventory | MerchSys.POS | MerchSys.Accounting | Infrastructure
agent: antigravity | claude-code | codex | other
date: YYYY-MM-DD
tags: [wpf, ef-core, xaml, mariadb, sqlite, mediatr, vb-net, ...]
error-code: CS1061
severity: build-error | runtime-error | logic-bug | performance | warning
---
```

## Problem

What happened. Include the **exact error message** or unexpected behavior. Include the file and line number if applicable.

## Root Cause

**Why** it happened. Focus on the non-obvious explanation — the thing that would save the next agent 30 minutes of debugging.

## Fix

What was changed and why. Include **code snippets** if helpful (keep them minimal — just the relevant lines).

```vb
' Before (broken)
' ...

' After (fixed)
' ...
```

## Prevention

How to avoid this in the future. Concrete rules or checks:
- "Always do X before Y"
- "Never use Z in this context"
- "Check W when you see error code N"

## Related

- Links to related Agent Wiki entries: `[[other-error-fix]]`
- Links to Domain Wiki pages: `[[module-pos]]`, `[[client-server-wpf]]`
