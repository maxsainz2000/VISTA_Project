---
type: antipattern
module: MerchSys.Inventory
agent: claude-code
date: 2026-05-04
tags: [vb-net, linq, list, build-error, BC32016]
---

## Context

Applies whenever LINQ `.Count(predicate)` is called on a `List(Of T)` variable in VB.NET.

## The Trap

Calling the LINQ `Count` extension method with a predicate on a `List(Of T)`:

```vb
' WRONG — BC32016: 'Count' property has no parameters and its return type cannot be indexed
Dim n As Integer = myList.Count(Function(x) x.IsActive)
```

This compiles to `myList.Count` (the `List(Of T)` integer property), then treats `(Function(x) ...)` as an indexer call on that integer — producing BC32016.

## Why It Fails

`List(Of T)` exposes `Count` as a read-only integer property. VB.NET resolves member access eagerly: it finds the property first and never reaches the LINQ extension method overload. The lambda is then parsed as an attempt to index the returned integer.

## The Pattern

Use the static `Enumerable.Count` method to bypass member resolution:

```vb
' CORRECT — bypasses the List property, calls LINQ extension directly
Dim n As Integer = Enumerable.Count(myList, Function(x) x.IsActive)
```

Alternatively, filter first and use the property:

```vb
' ALSO CORRECT — .Where returns IEnumerable, which has no Count property
Dim n As Integer = myList.Where(Function(x) x.IsActive).ToList().Count
```

## Rules

- **Never** call `.Count(predicate)` directly on a `List(Of T)` variable.
- Use `Enumerable.Count(list, predicate)` for inline predicate counting on lists.
- `Any(predicate)` and `Sum(selector)` are unaffected — they are not `List(Of T)` properties.
- This also applies to `.Sum(selector)` if called on a list that shadows an inherited property — always prefer `Enumerable.Sum(list, selector)` when in doubt.

## Detector Contract

> Added 2026-05-24 after the agent-wiki audit (`Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-report.md`)
> flagged a tuple-array `.Count(predicate)` site as a violation. Arrays have no `Count` property —
> only `List(Of T)` does. See `Operator/debug-logs/archive/2026-05-24-audit-cycle/agent-wiki-verification-improvement-plan.md`.

Any audit that detects this rule MUST verify the receiver's static type.

### Flag (positive patterns)

A call shaped `<receiver>.Count(Function(...) ...)` where the receiver is statically declared as:

- `As List(Of T)`
- `As New List(Of T)`
- Any subtype of `List(Of T)` that does not override the `Count` property to accept arguments

### Do NOT flag (negative patterns)

- Arrays declared as `{ ... }` literals — they expose `Length`, not `Count`.
- Receivers typed `As T()` (any array type).
- Tuple-literal arrays like `{("a", 1), ("b", 2)}` — these are `ValueTuple(Of String, Integer)()`, an array.
- `As IEnumerable(Of T)`, `As IQueryable(Of T)`, `As ICollection(Of T)` — these have no `Count(predicate)` member that conflicts; the LINQ extension is the only candidate.
- Receivers whose static type cannot be resolved from local scope (skip rather than flag).

The corpus that exercises these patterns lives at `Operator/audit-tests/rule-12/` (see INFRA-18).

## Related

- First encountered in INV-05 StockDashboardService (2026-05-04)
- Root cause is the same as BC32016 in general: VB.NET property wins over extension method when names collide
