---
type: antipattern
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-04
tags: [vb-net, lambda, build-error, BC36641]
---

## Context

Applies whenever a LINQ lambda parameter shares a name with any variable declared later in the same method body in VB.NET.

## The Trap

Using a lambda parameter name that matches a local variable declared later in the same method:

```vb
' WRONG — BC36641: Lambda parameter 'po' hides a variable in an enclosing block
Dim numbers = Await _db.PurchaseOrders.Select(Function(po) po.OrderNumber).ToListAsync()
Dim po As New PurchaseOrder With { ... }   ' <-- same name as lambda param above
```

This produces **BC36641: Lambda parameter 'po' hides a variable in an enclosing block, a previously defined range variable, or an implicitly declared variable in a query expression.**

## Why It Fails

Unlike C#, VB.NET scans the entire method body when resolving lambda parameter names. If a local variable with the same name exists anywhere in the method — even after the lambda — the compiler considers it a shadowing conflict and refuses to compile.

## The Pattern

Use a distinct lambda parameter name that does not match any local variable in the method:

```vb
' CORRECT — lambda parameter 'p' does not conflict with local 'po'
Dim numbers = Await _db.PurchaseOrders.Select(Function(p) p.OrderNumber).ToListAsync()
Dim po As New PurchaseOrder With { ... }
```

## Rules

- Never reuse a local variable name as a lambda parameter within the same method in VB.NET.
- Prefer short, unambiguous names for lambda params (`p`, `l`, `v`) when a same-name local exists.
- This applies to all lambda expressions: `Function`, `Sub`, and query comprehensions.
