---
type: antipattern
module: Infrastructure
agent: claude-code
date: 2026-05-02
tags: [vb-net, namespace, build-error]
---

## Context

Applies to every VB.NET project in this solution where `<RootNamespace>` is set in the `.vbproj` file (e.g., `MerchSys.SharedKernel`, `MerchSys.Purchasing`, etc.).

## The Trap

Writing fully-qualified `Namespace` declarations inside source files:

```vb
' WRONG — doubles the root namespace segment
Namespace MerchSys.SharedKernel.Interfaces
    Public Interface IAuditable
        ...
    End Interface
End Namespace
```

This looks correct by analogy with C#, but it compiles to `MerchSys.SharedKernel.MerchSys.SharedKernel.Interfaces` — and any `Imports MerchSys.SharedKernel.Interfaces` in a sibling file fails with **BC30002 Type not defined**.

## Why It Fails

The VB.NET compiler prepends `<RootNamespace>` to every `Namespace` statement in source files. Unlike C# (which ignores the root namespace setting entirely), VB.NET stacks them. The result is a doubled prefix that nothing can import by the expected name.

## The Pattern

Use only the **relative suffix** after the root namespace:

```vb
' CORRECT — root namespace provides "MerchSys.SharedKernel", this adds "Interfaces"
Namespace Interfaces
    Public Interface IAuditable
        ...
    End Interface
End Namespace
```

The effective fully-qualified namespace becomes `MerchSys.SharedKernel.Interfaces`, which `Imports MerchSys.SharedKernel.Interfaces` resolves correctly.

## Rules

- **Never** write the full `<RootNamespace>` prefix inside a `Namespace` declaration in VB.NET source files.
- Use only the folder-relative suffix: `Namespace Entities`, `Namespace Enums`, `Namespace Interfaces`, `Namespace Services`, etc.
- This applies to every project in the MerchSys solution (`MerchSys.Purchasing`, `MerchSys.Inventory`, `MerchSys.POS`, `MerchSys.Accounting`, `MerchSys.SharedKernel`).
- Cross-project `Imports` must still use the full qualified name (e.g., `Imports MerchSys.SharedKernel.Interfaces`) — that is correct.

## Related

- First encountered during INFRA-02 SharedKernel implementation (2026-05-02)
