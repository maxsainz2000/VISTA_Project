# Template: Pattern / Antipattern

Copy this template when documenting a proven pattern in `agent_wiki/patterns/` or a trap to avoid in `agent_wiki/antipatterns/`.

---

```yaml
---
type: pattern | antipattern
module: MerchSys.Purchasing | MerchSys.Inventory | MerchSys.POS | MerchSys.Accounting | Infrastructure
agent: antigravity | claude-code | codex | other
date: YYYY-MM-DD
tags: [wpf, ef-core, xaml, mariadb, sqlite, mediatr, vb-net, mvvm, ...]
---
```

## Context

When does this pattern (or antipattern) apply? What situation triggers it?

## The Pattern / The Trap

For **patterns**: Describe the approach that works. Include code snippets if helpful.

For **antipatterns**: Describe what seems right but fails, and **why** it fails. Include the misleading reasoning that leads agents astray.

```vb
' Example code (if applicable)
' ...
```

## Why It Works / Why It Fails

The explanation. Focus on the underlying mechanism — the "because" that makes this knowledge transferable.

## Rules

Concrete, actionable rules for future agents:
- "When doing X, always use Y approach"
- "Never do Z because of W"

## Related

- Links to related entries: `[[other-pattern]]`, `[[related-error]]`
- Links to Domain Wiki pages: `[[modular-monolith]]`, `[[mediatr-mediator]]`
