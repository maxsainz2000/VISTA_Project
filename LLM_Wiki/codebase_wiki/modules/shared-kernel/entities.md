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
| `Entities/AuditableEntity.vb` | `AuditableEntity` | `BaseEntity`, `IAuditable` | `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` |
| `Entities/SoftDeletableEntity.vb` | `SoftDeletableEntity` | `AuditableEntity`, `ISoftDeletable` | `IsDeleted`, `DeletedBy`, `DeletedAt` |
| `Sync/SyncJournal.vb` | `SyncJournal` | `AuditableEntity` | `TableName`, `RowId`, `Operation`, `Payload`, `SyncedAt`. Marked `<NoSync>`. |
| `Persistence/NoSyncAttribute.vb` | `NoSyncAttribute` | `Attribute` | Applied to entities that must never be journalled for sync. |
| `Persistence/SyncJournalDescriptor.vb` | `SyncJournalDescriptor` | N/A | DTO capturing one EF change event for journalling. |
| `Sync/ConflictResolution.vb` | `RemoteRowSnapshot` | N/A | `Exists`, `ModifiedAt` |
| `Sync/ConflictResolution.vb` | `ResolutionDecision` | N/A | `Action`, `Reason` |
| `Sync/SyncSettings.vb` | `SyncSettings` | N/A | `CentralServerHost`, `ProbeIntervalSeconds`, `MariaDbConnection` |
| `Sync/SyncProbeResult.vb` | `SyncProbeResult` | N/A | `NetworkAvailable`, `ServerReachable`, `LatencyMs` |

## Sync Maps
| File Path | Class | Target Module |
|---|---|---|
| `Sync/SyncMaps/PurchasingSyncMap.vb` | `PurchasingSyncMap` | Purchasing |
| `Sync/SyncMaps/PosSyncMap.vb` | `PosSyncMap` | POS |
| `Sync/SyncMaps/InventorySyncMap.vb` | `InventorySyncMap` | Inventory |
| `Sync/SyncMaps/AccountingSyncMap.vb` | `AccountingSyncMap` | Accounting |

## DB Context Base
| File Path | Class | Inherits | Key Members |
|---|---|---|---|
| `Data/BaseDbContext.vb` | `BaseDbContext` | `DbContext` | Handles Auditing and Soft Deletes in `SaveChanges` |
| `Data/AuditInterceptor.vb` | `AuditInterceptor` | `SaveChangesInterceptor` | Intercepts saves to update IAuditable fields |
| `Data/RoleGuardInterceptor.vb` | `RoleGuardInterceptor` | `SaveChangesInterceptor` | SaveChangesInterceptor that enforces OWASP DA5 role-based write rejection. |
| `Data/WriteContextScope.vb` | `WriteContextScope` | `IWriteContextScope` | Default scoped implementation utilizing AsyncLocal to carry write context metadata safely across asynchronous boundaries. |
| `Sync/SyncJournalDbContext.vb` | `SyncJournalDbContext` | `BaseDbContext` | Context for the `Sync_Journal` table. |
| `Sync/MariaDbSyncContext.vb` | `MariaDbSyncContext` | N/A (MySqlConnector wrapper) | Lightweight remote MariaDB connection wrapper. Replaces former Pomelo-backed DbContext to prevent runtime binary incompatibilities. |

## Common Exceptions
| File Path | Class | Inherits | Description |
|---|---|---|---|
| `Exceptions/ImmutableEntityException.vb` | `ImmutableEntityException` | `Exception` | Thrown when attempting to modify/delete BIR-immutable records. |
| `Exceptions/UnauthorizedWriteException.vb` | `UnauthorizedWriteException` | `UnauthorizedAccessException` | Thrown when attempting an unauthorized write (e.g. by an Owner account) to operational databases. |
