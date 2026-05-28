---
type: layer-manifest
module: MerchSys.SharedKernel
layer: Entities
last-updated: 2026-05-26
---

# MerchSys.SharedKernel — Entities & Data Types

## Base Entities
| File Path | Class / Interface | Base / Implements | Key Members |
|---|---|---|---|
| `Entities/BaseEntity.vb` | `BaseEntity` | N/A | `Id As Guid` |
| `Entities/UserAccount.vb` | `UserAccount` | N/A | `Id`, `Username`, `PasswordHash`, `Role`, `LockedUntil` |
| `Entities/ConcurrencyAwareEntity.vb` | `ConcurrencyAwareEntity` | `BaseEntity` | `RowVersion As DateTime` |
| `Entities/AuditableEntity.vb` | `AuditableEntity` | `ConcurrencyAwareEntity`, `IAuditable` | `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt`, `RowVersion` |
| `Entities/SoftDeletableEntity.vb` | `SoftDeletableEntity` | `AuditableEntity`, `ISoftDeletable` | `IsDeleted`, `DeletedBy`, `DeletedAt` |

## DB Context Base
| File Path | Class | Inherits | Key Members |
|---|---|---|---|
| `Data/BaseDbContext.vb` | `BaseDbContext` | `DbContext` | Handles Auditing and Soft Deletes in `SaveChanges` |
| `Data/AuditInterceptor.vb` | `AuditInterceptor` | `SaveChangesInterceptor` | Intercepts saves to update IAuditable fields |
| `Data/RoleGuardInterceptor.vb` | `RoleGuardInterceptor` | `SaveChangesInterceptor` | SaveChangesInterceptor that enforces OWASP DA5 role-based write rejection. |
| `Data/WriteContextScope.vb` | `WriteContextScope` | `IWriteContextScope` | Default scoped implementation utilizing AsyncLocal to carry write context metadata safely across asynchronous boundaries. |

## Common Exceptions
| File Path | Class | Inherits | Description |
|---|---|---|---|
| `Exceptions/ImmutableEntityException.vb` | `ImmutableEntityException` | `Exception` | Thrown when attempting to modify/delete BIR-immutable records. |
| `Exceptions/UnauthorizedWriteException.vb` | `UnauthorizedWriteException` | `UnauthorizedAccessException` | Thrown when attempting an unauthorized write (e.g. by an Owner account) to operational databases. |
