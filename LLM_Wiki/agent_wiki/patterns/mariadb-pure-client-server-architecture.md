---
type: pattern
module: Infrastructure
agent: claude-code
date: 2026-05-28
tags: [mariadb, ef-core, vb-net, client-server, concurrency, optimistic-locking, select-for-update, architecture]
---

# Pure Client-Server Against Centralized MariaDB (Post-2026-05-28 Pivot)

## Context

This entry documents the architecture pattern adopted on 2026-05-28 when VISTA pivoted from **offline-first SQLite + push-only sync to MariaDB** to **pure client-server against a single centralized MariaDB 11.4.x LTS instance**.

The trigger was the expansion of the deployment target from a single Manager workstation to **four concurrent client laptops** sharing one MariaDB host on the LAN. In that topology the previous architecture failed correctness under concurrent writes (silent stock divergence under `LastWriteWins`; no cross-client read consistency due to push-only sync).

Authoritative source: `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md`.

## The Pattern

### 1. Topology

```
[Laptop 1 — Client]
[Laptop 2 — Client]  ─┐
[Laptop 3 — Client]   ├──► MariaDB 11.4.x (XAMPP) on Host Laptop
[Laptop 4 — Client]  ─┘     LAN, TCP 3306
```

- One MariaDB instance, four (or N) WPF clients.
- No local DB on any client. No sync. No journal.
- All `DbContext`s in all four modules bind to the same connection string.

### 2. Connection

```vb
' appsettings.json
{
  "ConnectionStrings": {
    "MerchSysCentral": "Server=192.168.1.10;Port=3306;Database=merchsys_central;User Id=vista_app;Password=...;ConnectionTimeout=5;DefaultCommandTimeout=10;"
  }
}
```

Each client reads this from `appsettings.json` on startup. EF Core `DbContext`s are registered scoped (per WPF unit of work).

### 3. Concurrency Control — Two Mechanisms

**Optimistic concurrency tokens** on every mutable financial/inventory row:

```vb
' EF configuration
builder.Property(Function(e) e.RowVersion).
    IsRowVersion().
    HasColumnType("TIMESTAMP(6)").
    ValueGeneratedOnAddOrUpdate()
```

On `DbUpdateConcurrencyException`, the ViewModel surfaces "Data changed elsewhere — refresh and retry" via `Notification.Wpf`, reloads the aggregate, and prompts the user. **Never silently overwrite.**

**Pessimistic `SELECT ... FOR UPDATE`** inside the FIFO decrement transaction:

```vb
Using tx = Await _db.Database.BeginTransactionAsync()
    Dim oldestBatches = Await _db.Database.SqlQueryRaw(Of StockBatch)(
        "SELECT * FROM Inv_StockBatches " &
        "WHERE ProductId = {0} AND QuantityRemaining > 0 AND IsDeleted = 0 " &
        "ORDER BY ReceiptDate, Id " &
        "FOR UPDATE", productId).ToListAsync()

    ' decrement, insert Inv_StockMovements, insert Inv_SaleCogs
    Await _db.SaveChangesAsync()
    Await tx.CommitAsync()
End Using
```

This serializes concurrent sales of the same product across all four clients without app-level coordination.

### 4. Connectivity Loss = Hard Stop

- Connection-status indicator in the shell header (Online / Reconnecting / Offline).
- On `Offline`, all command buttons disable.
- On `Reconnecting`, exponential backoff up to 5 retries; then require manual retry.
- **Do not** attempt to buffer or queue writes on the client. The previous architecture's offline-write path is gone for a reason.

## Why It Works

- **Single source of truth.** Every client reads what the previous transaction wrote. No "I sold the last bag" race.
- **DB-level serialization is cheap on a LAN.** A `FOR UPDATE` on one hot row (the FIFO oldest batch) blocks for milliseconds, not seconds.
- **Standard EF Core semantics.** No custom journal, no custom conflict resolver — code is simpler and more obvious.
- **BIR audit traceability** is preserved in a single DB rather than scattered across N SQLite files plus a central mirror.

## Why The Old Pattern Failed (For Reference)

- Push-only sync → no cross-client reads → Laptop B's dashboard never showed Laptop A's sale.
- `LastWriteWins` on concurrent UPDATEs to `Inv_StockBatches.QuantityRemaining` → one decrement silently overwrote the other, dropping physical-vs-system stock alignment.
- Sync probe at 30s → up to a 60s window of incorrect inventory shown on idle dashboards.
- The very test that motivated the pivot (4-laptop concurrency stress) was untestable under the previous architecture.

## Rules

- **All writes go through `DbContext`.** No raw SQL inserts that bypass concurrency tokens.
- **Mutable rows must have a `RowVersion` / `TIMESTAMP(6) ON UPDATE` column.** Audit columns are not a substitute.
- **FIFO inventory decrement always uses `SELECT ... FOR UPDATE` inside a transaction.** Never read-then-write without locking.
- **`DbUpdateConcurrencyException` is always surfaced to the user**, never caught silently.
- **Never reintroduce SQLite or a sync layer.** If a future requirement demands true offline-write capability, design it as bidirectional with proper inventory reservations — do not resurrect `LastWriteWins`.
- **Host laptop is UPS-backed** — operational hard requirement, not advisory.
- **Nightly `mysqldump`** to a second machine via Task Scheduler — required for DR.

## EF Core ↔ MySQL Provider — Open Question

Pomelo 9.x has a binary incompatibility with EF Core 10 (`MissingMethodException` on `AbstractionsStrings.ArgumentIsEmpty`, see `Operator/debug-logs/INFRA-test-5.md`); Pomelo 10.x is unreleased. INFRA-23 must select between:

- `MySql.EntityFrameworkCore` (Oracle official) — needs validation against EF Core 10
- Stay on raw `MySqlConnector` for writes + thin query helper for reads
- Downgrade EF Core to 9.x

This pattern entry will be updated with the chosen provider after INFRA-23 lands.

## Related

- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` — authoritative concept page
- `LLM_Wiki/wiki/concepts/offline-first-sync.md` — superseded; historical record only
- `LLM_Wiki/Sources/system_plan_amendment_2026-05-28.md` — amendment authority
- `[[efcore10-vbnet-migration-discovery-bug]]` — discovery bug still applies to MariaDB; raw-connection bootstrap pattern carries over (use `MySqlConnection`)
- `[[efcore-vbnet-tolistasync-entity-empty]]` — VB.NET materialization bug; expected to repro against MariaDB
- `[[sync-transmit-delete-no-payload]]` — historical, sync layer being removed
- `[[efcore-temp-key-sync-journal-payload]]` — historical, journal layer being removed
- `[[sqlite-trigger-no-temp-reference]]` — historical, SQLite being removed
