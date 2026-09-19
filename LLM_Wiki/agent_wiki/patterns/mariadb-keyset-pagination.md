---
type: pattern
module: Infrastructure
agent: claude-code
date: 2026-06-10
tags: [mariadb, mysqlconnector, vb-net, pagination, keyset, performance, raw-reader, viewmodel]
---

# Pattern: MariaDB Keyset (Seek) Pagination for Unbounded History Grids

## Context

INFRA-34. Several history/ledger grids (transaction history, shrinkage history, stock-audit, per-account
credit history) issued **full-result-set** reads. UX-37 virtualized the *UI containers* but the whole
set was still materialized in memory first. As the BIR 10-year-retention tables grow toward ~10⁶ rows,
an unfiltered "show history" load becomes the one place this app can develop lag.

**Keyset (seek) pagination** fixes this: instead of `OFFSET n` (which counts past skipped rows and
degrades with depth), the next page seeks straight to the slice after the last-seen row via the ordered
index — **O(page), not O(table)**, at any depth.

## The Pattern

### 1. The shared contract (SharedKernel/Paging)

```vb
' PageRequest: PageSize (default 100), keyset cursor (CursorDate + CursorId),
'              optional FromUtc/ToUtc date range.
' PagedResult(Of T): Items, HasMore, NextCursorDate, NextCursorId.
```

- The cursor is the **composite `(date, Id)`** of the last row shown. `Id` is the tie-breaker — the
  ordered date column is **not unique**, so a bare date cursor drops/duplicates rows that share a
  timestamp. `CursorId` is `Long?` to fit both INT and BIGINT identity columns.
- **No `COUNT(*)`.** `HasMore` is derived by fetching `PageSize + 1` rows and trimming the extra one —
  a count on an unbounded table is itself costly.

### 2. The keyset read (raw `MySqlConnector` reader — never `ToListAsync` on an entity)

```vb
Dim fetchLimit As Integer = request.PageSize + 1
Dim sql = "SELECT … FROM Pos_SalesTransactions WHERE IsDeleted = 0"
If request.FromUtc.HasValue Then sql &= " AND TransactionDate >= @fromUtc"
If request.ToUtc.HasValue Then sql &= " AND TransactionDate <= @toUtc"
If request.CursorId.HasValue Then
    ' seek past the last row already shown (descending order)
    sql &= " AND (TransactionDate < @cursorDate OR (TransactionDate = @cursorDate AND Id < @cursorId))"
End If
sql &= " ORDER BY TransactionDate DESC, Id DESC LIMIT " & fetchLimit.ToString()
```

- `LIMIT` is the **inlined trusted server-side int** (`PageSize + 1`, set by the VM — never user
  input) to dodge driver placeholder-in-LIMIT quirks. Everything else is parameterized (OWASP).
- After reading: `hasMore = list.Count > PageSize : if hasMore then remove the extra row`.
  `NextCursor = last kept row's (date, Id)`.
- Secondary loads (lines, product/batch decoration) run **only for the page** — `IN (pageIds)` is
  naturally bounded to `PageSize`.
- **An index on the ordered column is mandatory** or `ORDER BY … DESC` is a filesort. INFRA-34's
  `0006_paging_support_indexes.sql` added the missing ones (`Inv_ShrinkageRecords.RecordedDate`,
  `Inv_StockAuditRecords.AuditedAt`, `Pos_SalesTransactions(CustomerId, TransactionDate)`). InnoDB
  secondary indexes implicitly carry the PK, so `KEY (TransactionDate)` covers the `(TransactionDate, Id)`
  cursor at the leaf.

### 3. The "Load more" VM wiring (append, don't reload)

```vb
Private Const PageSize As Integer = 100
Private _nextCursorDate As DateTime?
Private _nextCursorId As Long?
Public Property HasMore As Boolean        ' setter calls LoadMoreCommand.NotifyCanExecuteChanged()
Public ReadOnly Property LoadMoreCommand As AsyncRelayCommand  ' CanExecute = HasMore
```

- First load resets the cursor and loads page 1; `LoadMoreAsync` fetches the next page and **appends**
  to the working set (no clear-and-reload), then re-applies any client-side filters.
- The grid's `ObservableCollection` accumulates the loaded window; existing client filters
  (`ApplyFilters`) re-run over it.

### 4. The "Load more" XAML (don't defeat virtualization)

```xml
<Grid>
    <Grid.RowDefinitions><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
    <DataGrid Grid.Row="0" VirtualizingStackPanel.VirtualizationMode="Recycling" .../>
    <Button Grid.Row="1" Content="Load more" Command="{Binding LoadMoreCommand}"
            Visibility="{Binding HasMore, Converter={StaticResource BoolToVis}}"/>
</Grid>
```

- The button goes in a **sibling `Height="Auto"` row**, never wrapping the grid in a `ScrollViewer`
  (that silently kills UX-37 recycling — see `[[wpf-vista-performance]]`).

## Rules

- Always tie-break the ordered date with `Id`; seek on the `(date, Id)` tuple.
- Fetch `PageSize + 1`; derive `HasMore`; never `COUNT(*)` an unbounded table.
- Add an index on every column used in a keyset `ORDER BY`/seek, or it's a filesort.
- Reads stay on the raw `MySqlConnector` reader (EF Core 10 VB.NET `ToListAsync`-on-entity bug — see
  `[[efcore-vbnet-tolistasync-entity-empty]]`).
- "Load more" **appends**; keep the grid in a constrained row with no outer `ScrollViewer`.
- `DATETIME(6)` cursor round-trips safely via `ToString("o")` (the 7th fractional digit is always 0).
- Per-entity-bounded reads (one customer's history, product-scoped audit) often only need the **index**;
  reserve full paging for genuinely unbounded grids.

## Related

- `[[wpf-vista-performance]]` — UX-37 virtualization + async-load standard this builds on.
- `[[efcore-vbnet-tolistasync-entity-empty]]` — why reads use the raw reader.
- `[[mariadb-pure-client-server-architecture]]` — the centralized-MariaDB data-access model.
- Plan: `Plans/VISTA_Modules/Infrastructure/34-data-access-paging-and-resiliency.md`;
  summary: `Progress/VISTA_Modules/Infrastructure/INFRA-34-summary.md`.
