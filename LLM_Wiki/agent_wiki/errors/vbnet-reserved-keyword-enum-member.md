---
type: error-fix
title: Reserved Keyword as Enum Member Name
error-codes: [BC31001, BC30201]
module: MerchSys.Inventory
tags: [vb-net, enum, reserved-keyword, build-error]
agent: claude-code
date: 2026-05-09
---

## Problem

Using a VB.NET reserved keyword (`Return`, `End`, `Class`, etc.) as an enum member name causes BC31001 ("Statement cannot appear within an Enum body") and BC30201 ("Expression expected").

```vb
' BROKEN — Return is a reserved keyword
Public Enum MovementType
    Sale = 1
    Return = 4   ' BC31001 / BC30201
End Enum
```

## Fix

Escape the reserved keyword with square brackets:

```vb
Public Enum MovementType
    Sale = 1
    [Return] = 4   ' OK
End Enum
```

## Context

Encountered when defining `MovementType` in `MerchSys.Inventory/Entities/MovementType.vb` (INT-05). The enum member `Return` conflicted with the VB.NET `Return` statement keyword.

## Prevention

Before choosing an enum member name, check whether it matches a VB.NET keyword. Common dangerous names: `Return`, `End`, `Stop`, `Error`, `New`, `With`, `In`, `To`, `Step`, `Get`, `Set`.
