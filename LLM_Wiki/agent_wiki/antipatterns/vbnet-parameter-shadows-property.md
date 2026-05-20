---
type: antipattern
module: MerchSys.Purchasing
agent: claude-code
date: 2026-05-20
tags: [vb-net, case-insensitive, parameter, property, shadowing, logic-bug]
error-code: (none — silent logic bug, no compiler error)
severity: logic-bug
---

# VB.NET parameter name case-insensitively shadows a property

## Problem

`PurchaseOrderEditorViewModel.LoadVendors` accepted a parameter named `vendors`.
The class also declares a property `Vendors As ObservableCollection(Of Vendor)`.
Inside the method, every reference to `Vendors` (including `Vendors.Clear()` and
`Vendors.Add(v)`) resolved to the **parameter**, not the property.

Result: the input `List(Of Vendor)` was cleared immediately, the `For Each` iterated
an empty list, and nothing was ever added to the `ObservableCollection`. The vendor
ComboBox showed no items. No compiler error or warning is raised.

## Root Cause

VB.NET identifiers are **case-insensitive**. A parameter named `vendors` and a property
named `Vendors` are the **same identifier** in VB.NET's name-resolution rules. Within
the method body, the parameter is in the innermost scope and therefore shadows the
instance property, exactly as a local variable would shadow a field.

```vb
' BROKEN — parameter `vendors` shadows property `Vendors` (same name, different case)
Public Property Vendors As ObservableCollection(Of Vendor)

Public Sub LoadVendors(vendors As List(Of Vendor))
    Vendors.Clear()          ' resolves to vendors.Clear() — clears the INPUT list!
    For Each v In vendors    ' iterates the now-empty input list
        Vendors.Add(v)       ' resolves to vendors.Add(v) — adds to the input list
    Next
End Sub
' End result: ObservableCollection is never touched; input list is emptied
```

## Fix

Rename the parameter so it does not case-insensitively match any property or field in
the class.

```vb
' FIXED — parameter renamed to vendorList
Public Sub LoadVendors(vendorList As List(Of Vendor))
    Vendors.Clear()           ' now unambiguously resolves to Me.Vendors (ObservableCollection)
    For Each v In vendorList
        Vendors.Add(v)
    Next
End Sub
```

Apply the same rename to every overload or method that passes the collection through:
`PrepareForNew(vendorList As List(Of Vendor))` and
`LoadFromPO(po As PurchaseOrder, vendorList As List(Of Vendor))`.

## Prevention

- **Never name a parameter the same as a property or field** in the same class (modulo
  case). VB.NET will not warn you; the code compiles and runs silently wrong.
- Prefer the `_camelCase` convention for private fields and `PascalCase` for properties,
  and use clearly distinct names for parameters (`vendorList`, `items`, `input`).
- If a method takes a collection of the same type as an `ObservableCollection` property,
  the parameter name should include a distinguishing word (`List`, `Input`, `Source`).

## Related

- `[[vbnet-lambda-param-shadows-local-variable]]` — same family: VB.NET silently resolves
  two identifiers to the same name when they differ only in case
