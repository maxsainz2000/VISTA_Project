---
type: antipattern
module: Infrastructure
agent: claude-code
date: 2026-05-23
tags: [sync, mariadb, sqlite, json, transmitter, delete, payload]
---

## Context

In the sync transmitter (`MariaDbSyncTransmitter.TransmitEntryAsync`), each `SyncJournal` entry
is converted to a Remote POCO via `ToRemoteEntity(entry)` before the operation type is checked.
DELETE journal entries carry **no payload** — the JSON is an empty string — because only the
row ID is needed to delete. Deserializing an empty string throws at runtime.

## The Trap

Calling `ToRemoteEntity` (which deserializes `entry.Payload` as JSON) unconditionally before
branching on `entry.Operation` causes DELETE entries to crash with:

```
Value cannot be null. (Parameter 'json')
```

The misleading reasoning: "I need to know the entity shape before deciding what to do" — but
DELETE only needs `entry.RowId`, which is already on the journal entry itself.

## Why It Fails

`JsonSerializer.Deserialize(Of T)(String, ...)` throws `ArgumentNullException` when the input
string is null or empty. DELETE journal entries are written with `Payload = ""` or `Nothing`
because no entity snapshot is needed.

## Rules

- **Never call `ToRemoteEntity` before checking `entry.Operation`.**
- For `INSERT` and `UPDATE`: deserialize the payload into a Remote POCO, then transmit.
- For `DELETE`: use `entry.RowId` directly — no deserialization needed.

```vb
' Correct pattern:
Select Case entry.Operation.ToUpperInvariant()
    Case "INSERT", "UPDATE"
        Dim remoteEntity = ToRemoteEntity(entry)
        If remoteEntity Is Nothing Then
            _logger.LogWarning(...)
            Return
        End If
        ' ... INSERT / UPDATE logic ...

    Case "DELETE"
        Await _mariaDb.ExecuteDeleteAsync(entry.TableName, entry.RowId, conn, tx, ct)
End Select
```

## Related

- `[[efcore-vbnet-tolistasync-entity-empty]]` — another silent payload/materialization issue
