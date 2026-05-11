---
type: layer-manifest
module: MerchSys.SharedKernel
layer: Entities
last-updated: 2026-05-10
---

# MerchSys.SharedKernel — Entities & Data Types

## Base Entities
| File Path | Class / Interface | Base / Implements | Key Members |
|---|---|---|---|
| `Entities/BaseEntity.vb` | `BaseEntity` | N/A | `Id As Guid` |
| `Entities/AuditableEntity.vb` | `AuditableEntity` | `BaseEntity`, `IAuditable` | `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` |
| `Entities/SoftDeletableEntity.vb` | `SoftDeletableEntity` | `AuditableEntity`, `ISoftDeletable` | `IsDeleted`, `DeletedBy`, `DeletedAt` |
| `Sync/SyncJournal.vb` | `SyncJournal` | `AuditableEntity` | `TableName`, `RowId`, `Operation`, `Payload`, `SyncedAt`. Marked `<NoSync>`. |
| `Persistence/NoSyncAttribute.vb` | `NoSyncAttribute` | `Attribute` | Applied to entities that must never be journalled for sync. |
| `Persistence/SyncJournalDescriptor.vb` | `SyncJournalDescriptor` | N/A | DTO capturing one EF change event for journalling. |
| `Sync/ConflictResolution.vb` | `RemoteRowSnapshot` | N/A | `Exists`, `ModifiedAt` |
| `Sync/ConflictResolution.vb` | `ResolutionDecision` | N/A | `Action`, `Reason` |

## DB Context Base
| File Path | Class | Inherits | Key Members |
|---|---|---|---|
| `Data/BaseDbContext.vb` | `BaseDbContext` | `DbContext` | Handles Auditing and Soft Deletes in `SaveChanges` |
| `Data/AuditInterceptor.vb` | `AuditInterceptor` | `SaveChangesInterceptor` | Intercepts saves to update IAuditable fields |
| `Sync/SyncJournalDbContext.vb` | `SyncJournalDbContext` | `BaseDbContext` | Context for the `Sync_Journal` table. |
| `Sync/MariaDbSyncContext.vb` | `MariaDbSyncContext` | `DbContext` (Pomelo) | Remote MariaDB context for data transmission. |

## Common Exceptions
| File Path | Class | Inherits | Description |
|---|---|---|---|
| `Exceptions/ImmutableEntityException.vb` | `ImmutableEntityException` | `Exception` | Thrown when attempting to modify/delete BIR-immutable records. |
