---
type: antipattern
module: Infrastructure
agent: claude-code
date: 2026-05-03
tags: [vb-net, fluent-api, ef-core, build-error]
---

## Context

Applies to every VB.NET file that uses multi-line fluent method chains (EF Core configurations, LINQ, etc.).

## The Trap

Writing multi-line fluent chains where the continuation line starts with `.`:

```vb
' WRONG — leading dot outside a With block causes BC30157
builder.HasMany(Function(c) c.Products)
       .WithOne(Function(p) p.Category)
       .HasForeignKey(Function(p) p.CategoryId)
       .OnDelete(DeleteBehavior.Restrict)
```

This produces **BC30157: Leading '.' or '!' can only appear inside a 'With' statement.**

## Why It Fails

In VB.NET, a line starting with `.` is only valid inside a `With ... End With` block (where it acts as shorthand for the `With` object). Outside that context, a leading `.` is a syntax error regardless of implicit line continuation.

## The Pattern

Move the `.` to the END of the preceding line instead:

```vb
' CORRECT — dot at end of line triggers implicit continuation
builder.HasMany(Function(c) c.Products).
        WithOne(Function(p) p.Category).
        HasForeignKey(Function(p) p.CategoryId).
        OnDelete(DeleteBehavior.Restrict)
```

Alternatively, use explicit `_` continuation with leading dot:

```vb
builder.HasMany(Function(c) c.Products) _
       .WithOne(Function(p) p.Category) _
       .HasForeignKey(Function(p) p.CategoryId) _
       .OnDelete(DeleteBehavior.Restrict)
```

## Rules

- **Never** start a continuation line with `.` in VB.NET outside a `With` block.
- Place the `.` at the **end** of the preceding line when chaining fluent APIs across multiple lines.
- Applies to all EF Core `IEntityTypeConfiguration` files, LINQ chains, and any other fluent API usage in MerchSys.

## Related

- First encountered in INV-02 entity type configurations (2026-05-03)
- Also note: VB.NET comment-only lines inside multi-line `HasData(...)` calls can cause cascade parse errors — split large HasData calls by category/group to avoid ambiguity.
