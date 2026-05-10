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
| `Sync/SyncJournal.vb` | `SyncJournal` | `AuditableEntity` | `TableName`, `RowId`, `Operation`, `Payload`, `SyncedAt` |

## DB Context Base
| File Path | Class | Inherits | Key Members |
|---|---|---|---|
| `Data/BaseDbContext.vb` | `BaseDbContext` | `DbContext` | Handles Auditing and Soft Deletes in `SaveChanges` |
| `Data/AuditInterceptor.vb` | `AuditInterceptor` | `SaveChangesInterceptor` | Intercepts saves to update IAuditable fields |
| `Sync/SyncJournalDbContext.vb` | `SyncJournalDbContext` | `BaseDbContext` | Context for the `Sync_Journal` table. |
