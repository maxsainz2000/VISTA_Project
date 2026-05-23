---
name: efcore-temp-key-sync-journal-payload
description: EF Core assigns temporary negative PKs to Added entities before SaveChangesAsync; capturing the Payload pre-save bakes the temp key into the JSON, causing duplicate-key errors on re-transmission.
metadata:
  type: error-fix
  module: MerchSys.SharedKernel
  tags: ef-core, sync, journal, temp-key, insert, duplicate-key, idempotency, vb-net
  agent: claude-code
  date: 2026-05-23
---

## Symptom

Sync journal INSERT entries repeatedly fail with:

```
Duplicate entry '-2147482647' for key 'PRIMARY'
```

The `RowId` in `Sync_Journal` is correct (e.g., 7), but the Payload JSON has
`"Id": -2147482647`. The EXISTS check in `TransmitEntryAsync` queries MariaDB
`WHERE Id = 7` (the RowId), finds nothing, and re-INSERTs — but the row was
already inserted in a previous sync cycle with the temp-key Id, causing a duplicate.

## Root Cause

`SyncableRepositoryCore.CollectPreSaveInfo` captures entity property values
**before** `SaveChangesAsync`. EF Core assigns temporary negative integer values
to the `Id` property of `Added` entities while they are tracked in the
`ChangeTracker` (e.g. -2147482647, -2147482646 for consecutive entities in the
same batch). These temp keys are then serialized into the Payload JSON.

After `SaveChangesAsync` returns, EF Core updates the entity's `Id` to the real
DB-assigned auto-increment value. `BuildDescriptors` correctly reads the real PK
for `RowId` — but the snapshot dict was already captured pre-save, so
`RowSnapshotJson` (the Payload) still contains the temp key.

The result: `RowId` = 7 (correct) but `Payload["Id"]` = -2147482647 (EF temp key).

## Fix

In `SyncableRepositoryCore.BuildDescriptors`, for INSERT entries, rebuild the
snapshot from the **live entity entry post-save** instead of the pre-save dict:

```vb
If cap.OperationKind = "Insert" Then
    ' Re-read from the live entry post-save so the DB-generated PK replaces
    ' the EF Core temporary negative key that was present in the pre-save snapshot.
    Dim postSave As New Dictionary(Of String, Object)
    For Each prop In cap.Entry.Properties
        postSave(prop.Metadata.Name) = prop.CurrentValue
    Next
    snapshotJson = JsonSerializer.Serialize(postSave, SerializerOptions)
Else
    snapshotJson = JsonSerializer.Serialize(cap.PropertySnapshot, SerializerOptions)
End If
```

File: `WPF_Applications/MerchSys/src/MerchSys.SharedKernel/Persistence/SyncableRepositoryCore.vb`

## Fixing Pre-Existing Corrupted Entries

Entries already in `Sync_Journal` with the wrong Payload Id cannot be auto-repaired.
The rows exist in MariaDB at the temp-key Id. To stop the duplicate-key cycle:

```sql
-- Update RowId to match the actual MariaDB row Id (from the Payload)
UPDATE Sync_Journal SET RowId = <payload_id>, AttemptCount = 0, LastError = NULL
WHERE Id = <journal_entry_id>;
```

After this, `FetchRemoteRowAsync` queries `WHERE Id = <payload_id>` → finds the
existing MariaDB row → does UPDATE instead of INSERT → no duplicate key.

## Why `RowId` was correct but Payload was not

`BuildDescriptors` reads the PK for `RowId` from the **live** `EntityEntry` after
save (line: `cap.Entry.Property(pkProp.Name).CurrentValue`). But it used the
pre-save `cap.PropertySnapshot` for the full Payload. The asymmetry meant `RowId`
was always correct, but `Payload["Id"]` was sometimes wrong for INSERTs.

## Related

- [[sync-transmit-delete-no-payload]] — earlier fix for NULL Payload on DELETE entries
- `MerchSys.SharedKernel/Persistence/SyncableRepositoryCore.vb` — the fixed file
