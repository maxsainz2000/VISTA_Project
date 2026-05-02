---
type: antipattern
module: Infrastructure
agent: claude-code
date: 2026-05-02
tags: [vb-net, ef-core, dbcontext, build-error, case-insensitive]
---

## Context

When iterating over `ChangeTracker.Entries()` inside a class that inherits from `DbContext`.

## The Trap

Using `entry` as the loop variable name inside a `DbContext` subclass:

```vb
For Each entry In ChangeTracker.Entries().ToList()
    Select Case entry.State   ' BC30516: no accessible 'Entry' accepts this number of arguments
```

VB.NET is case-insensitive, so `entry` resolves to the inherited `DbContext.Entry(entity As Object)` method rather than the local loop variable.

## Why It Fails

`DbContext` exposes a method named `Entry` (multiple overloads). VB.NET's case-insensitive name resolution treats the loop variable `entry` as a call to `Me.Entry(...)`, causing an overload resolution error.

## Rules

- Never use `entry` as a loop variable inside a `DbContext` or its subclasses.
- Use an unambiguous name such as `dbEntry`, `entityEntry`, or `trackedEntry` instead.

```vb
' Correct
For Each dbEntry In ChangeTracker.Entries().ToList()
    Select Case dbEntry.State
```

## Related

- `[[vbnet-rootnamespace-relative-declarations]]`
