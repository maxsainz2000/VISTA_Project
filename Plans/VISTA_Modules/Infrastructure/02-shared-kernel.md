---
module: Infrastructure
plan-id: INFRA-02
title: "Shared Kernel"
depends-on: [INFRA-01]
estimated-files: 8
---

# Shared Kernel

## Context

The SharedKernel project contains base types, interfaces, and enums shared across all four business modules. Every entity in the system inherits from a base class that provides audit columns and soft-delete behavior. This plan establishes the entity foundation that all module domain models depend on.

## Prerequisites

- **INFRA-01** (Solution Scaffold) — the `MerchSys.SharedKernel` project must exist.

## Wiki References

- `analysis/tech-stack-reference.md` — "Audit columns on every table (CreatedBy, CreatedAt, ModifiedBy, ModifiedAt)" and "Soft deletes (IsDeleted flag)"
- `concepts/modular-monolith.md` — module isolation, no cross-module data access
- `entities/villon-farm-supply.md` — user roles: Manager (full CRUD), Owner (read-only)

## Deliverables

Create the following files in `src/MerchSys.SharedKernel/`:

```
MerchSys.SharedKernel/
├── Entities/
│   ├── BaseEntity.vb              ← Abstract base with ID
│   ├── AuditableEntity.vb         ← Adds audit columns
│   └── SoftDeletableEntity.vb     ← Adds IsDeleted
├── Enums/
│   ├── UserRole.vb                ← Manager, Owner
│   ├── PaymentMethod.vb           ← Cash, GCash, BankTransfer, Credit
│   └── PurchaseOrderStatus.vb     ← Draft, Submitted, Received, Verified, Closed
├── Interfaces/
│   ├── IAuditable.vb              ← Interface for audit columns
│   └── ISoftDeletable.vb          ← Interface for soft delete
```

## Specification

### Base Entity (`BaseEntity.vb`)
```
Public MustInherit Class BaseEntity
    Public Property Id As Integer          ' Primary key, auto-increment
End Class
```

### Auditable Entity (`AuditableEntity.vb`)
```
Public MustInherit Class AuditableEntity
    Inherits BaseEntity
    Implements IAuditable

    Public Property CreatedBy As String        ' Username who created
    Public Property CreatedAt As DateTime      ' UTC timestamp
    Public Property ModifiedBy As String       ' Username who last modified
    Public Property ModifiedAt As DateTime?    ' Nullable — null if never modified
End Class
```

### Soft-Deletable Entity (`SoftDeletableEntity.vb`)
```
Public MustInherit Class SoftDeletableEntity
    Inherits AuditableEntity
    Implements ISoftDeletable

    Public Property IsDeleted As Boolean = False
    Public Property DeletedBy As String        ' Nullable
    Public Property DeletedAt As DateTime?     ' Nullable
End Class
```

### IAuditable Interface
```
Public Interface IAuditable
    Property CreatedBy As String
    Property CreatedAt As DateTime
    Property ModifiedBy As String
    Property ModifiedAt As DateTime?
End Interface
```

### ISoftDeletable Interface
```
Public Interface ISoftDeletable
    Property IsDeleted As Boolean
    Property DeletedBy As String
    Property DeletedAt As DateTime?
End Interface
```

### Enums

**UserRole.vb:**
```
Public Enum UserRole
    Manager = 1     ' Full CRUD on all operational features
    Owner = 2       ' Read-only on dashboards/reports
End Enum
```

**PaymentMethod.vb:**
```
Public Enum PaymentMethod
    Cash = 1
    GCash = 2           ' E-wallet — recorded only, no actual transfer
    BankTransfer = 3    ' Bank payment — recorded only
    Credit = 4          ' Utang — charged to customer credit account
End Enum
```

**PurchaseOrderStatus.vb:**
```
Public Enum PurchaseOrderStatus
    Draft = 1
    Submitted = 2
    Received = 3
    Verified = 4
    Closed = 5
End Enum
```

## Implementation Notes

- All entities use `Integer` for primary keys (auto-increment handled by EF Core)
- All `DateTime` values should be stored as UTC
- `SoftDeletableEntity` is the most common base class — most entities use it
- `AuditableEntity` is for entities that should not support soft-delete (rare)
- `BaseEntity` is for simple lookup/reference entities
- Use proper VB.NET conventions: PascalCase properties, XML doc comments on all public members
- The `Namespace` for all files should follow the folder structure: `MerchSys.SharedKernel.Entities`, `MerchSys.SharedKernel.Enums`, etc.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors
2. All 8 files exist with correct namespaces
3. Inheritance chain: `SoftDeletableEntity` → `AuditableEntity` → `BaseEntity`
4. All enum values have explicit integer assignments
5. XML doc comments on every public class and property

## Output Requirements

### Implementation Summary
After completing all code, create a progress report at:
```
Progress/VISTA_Modules/Infrastructure/INFRA-02-summary.md
```
Using the template structure from `Progress/_template.md`.

### Documentation
- XML doc comments on every public member explaining its purpose
