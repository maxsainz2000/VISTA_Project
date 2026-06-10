---
module: MerchSys.Infrastructure
plan-id: INFRA-34
title: "Server-Side Paging for Unbounded History Grids + Data-Access Resiliency Hardening"
depends-on: [INFRA-25, INFRA-26, UX-37]
estimated-files: 16
priority: medium
note: proactive future-proofing — not a fix for a current defect
---

# INFRA-34: Server-Side Paging for Unbounded History Grids + Data-Access Resiliency Hardening

## Context

A structural performance review (2026-06-10) found the app is, broadly, already built to avoid lag:
the schema is well-indexed (composite indexes match real query patterns), reads use pooled raw
`MySqlConnector` readers off the UI thread, async hygiene is correct (no UI-thread-blocking
sync-over-async), UX-37 enabled container-recycling virtualization on the long grids, and INFRA-26
established the optimistic-concurrency + `FOR UPDATE` FIFO model. The big future-proofing migrations
(MariaDB pivot, EF provider decision, sync-layer decommission) have all landed.

**One deferred lever remains.** UX-37 virtualized the *UI containers* but **explicitly deferred
server-side paging** (its "Out of scope: server-side paging redesign"). So today, several user-facing
history/ledger grids still issue **full-result-set** reads: the entire matching set is materialized
into memory before the UI recycles rows. At ~50 SKUs and current transaction volume this is
imperceptible. Over the **BIR 10-year retention horizon** (`appsettings.json` → `Bir.RetentionYears: 10`),
`Pos_SalesTransactions` and the audit/shrinkage tables grow toward ~10⁶ rows, at which point an
unfiltered "show history" load materializes a very large set — the one place this codebase can develop
lag as data scales.

This plan closes that gap with two cheap, high-leverage moves — (1) a **default date window** so the
*common* case is naturally bounded, and (2) **keyset (seek) pagination** so the "show everything" case
stays O(page) regardless of table depth — plus a small, **careful** data-access resiliency pass. It
changes **no business rule, write path, MediatR contract, or INFRA-26 concurrency semantic**; every
read it touches stays on the raw reader and only gains a `LIMIT` + cursor predicate.

### Confirmed unbounded read paths (evidence)

| # | Read path | Table / order | Bound today? | Index supporting the order |
|---|-----------|---------------|--------------|----------------------------|
| 1 | `MerchSys.POS/Services/CartService.vb:161` (`GetTransactionHistory`) | `Pos_SalesTransactions`, `ORDER BY TransactionDate DESC` | **No** — date filters are *optional*; unfiltered loads all | `IX_Pos_SalesTransactions_TransactionDate` ✅ keyset-ready |
| 2 | `MerchSys.Inventory/Services/ShrinkageService.vb:185` | `Inv_ShrinkageRecords`, `ORDER BY RecordedDate DESC` | **No** | ❌ **no index on `RecordedDate`** — filesort |
| 3 | `MerchSys.Inventory/Services/InventoryAuditService.vb:148` | `Inv_StockAuditRecords`, `ORDER BY AuditedAt DESC` | **No** — filters optional | ❌ only `IX…_ProductId`; **no index on `AuditedAt`** |
| 4 | `MerchSys.POS/ViewModels/CreditManagementViewModel.vb:465` | `Pos_SalesTransactions` per account, `ORDER BY TransactionDate DESC` | **No** | ⚠️ filters `CustomerId` but index is on `CreditAccountId` — **verify column / index** |

### Verify-and-apply candidates (confirm bounding before deciding)

- **AP ledger full history** — `MerchSys.Purchasing/Services/AccountsPayableService.vb`. The *open-items*
  query (`…WHERE IsPaid = 0…`, line ~144) is naturally bounded (you pay them off); confirm whether a
  *full* (paid + unpaid) ledger-history query exists for `APLedgerView` and page it if so.
- **Product price history** — `Inv_ProductPriceHistory` already has
  `IX_Inv_ProductPriceHistory_ProductId_ChangedAt (ProductId, ChangedAt DESC)` — **already keyset-ready**;
  page it per product, no new index needed.

### Already-bounded — leave alone (do not touch)

`DailySummaryService` (date-windowed), `OwnerDashboardViewModel` (date-windowed + `GROUP BY`),
`VelocityService` (date-windowed + `GROUP BY`), `TamperAuditQueryService` (`WHERE DetectedAt BETWEEN`),
`CreditService.GetAccounts` (bounded by customer count). These are correct and must not be changed.

## Prerequisites

- **INFRA-25** — module DbContexts on MariaDB; SQLite gone.
- **INFRA-26** — optimistic-concurrency tokens + `FOR UPDATE` FIFO transaction. **Critical for Deliverable E**:
  any `EnableRetryOnFailure` execution strategy interacts with INFRA-26's user-initiated
  `BeginTransactionAsync` and must not break it (see §E).
- **UX-37** — container-recycling virtualization + the `LoadDataAsync`/`IsBusy` async-load standard this
  paging rides on. Paging must **preserve** recycling (no outer `ScrollViewer` that defeats it).

## Wiki References

- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` (data-access + concurrency model)
- `LLM_Wiki/wiki/concepts/client-server-wpf.md` (raw-reader read pattern)
- `LLM_Wiki/agent_wiki/patterns/wpf-vista-performance.md` (UX-37 virtualization + async-load standard)
- `LLM_Wiki/agent_wiki/` EF Core 10 VB.NET `ToListAsync`-on-entity bug (reads stay on raw reader)

## Deliverables

### A. Shared paging contract (SharedKernel)

```
MerchSys.SharedKernel/Querying/PageRequest.vb       ' NEW — keyset cursor + page size + optional date range
MerchSys.SharedKernel/Querying/PagedResult.vb       ' NEW — Items, HasMore, NextCursor (no mandatory COUNT)
```

- `PageRequest`: `PageSize` (default 100), optional keyset cursor (`CursorDate As DateTime?`,
  `CursorId As Long?`), optional `FromUtc`/`ToUtc` date range, optional filter scalar(s).
- `PagedResult(Of T)`: `Items As IReadOnlyList(Of T)`, `HasMore As Boolean`, `NextCursorDate`,
  `NextCursorId`. **No mandatory `TotalCount`** — `COUNT(*)` on an unbounded table is itself costly;
  `HasMore` is derived by fetching `PageSize + 1` rows and trimming. Expose an *optional* `TotalCount`
  only where a total is genuinely useful and the `WHERE` is index-bounded.

### B. Keyset pagination on the confirmed unbounded read paths

Convert paths 1–4 (and the verified candidates) to accept a `PageRequest` and return a `PagedResult`.
Keep the existing raw `MySqlConnector` reader; **add** the cursor predicate + `LIMIT`. Preserve every
existing `WHERE` clause (especially `IsDeleted = 0` soft-delete filters).

Keyset pattern (date-descending lists; `Id` breaks ties so the cursor is stable — InnoDB secondary
indexes implicitly carry the PK, so `(TransactionDate, Id)` is index-covered):

```vb
' First page: no cursor.  Subsequent pages: pass the last row's (date, id).
Dim sql As String =
    "SELECT Id, TransactionDate, ... " &
    "FROM Pos_SalesTransactions " &
    "WHERE IsDeleted = 0 "
If req.FromUtc.HasValue Then sql &= "AND TransactionDate >= @fromUtc "
If req.ToUtc.HasValue Then sql &= "AND TransactionDate <= @toUtc "
If req.CursorDate.HasValue Then
    ' seek past the last row already shown
    sql &= "AND (TransactionDate < @cursorDate " &
           "     OR (TransactionDate = @cursorDate AND Id < @cursorId)) "
End If
sql &= "ORDER BY TransactionDate DESC, Id DESC " &
       "LIMIT @limit"            ' bind @limit = req.PageSize + 1 to detect HasMore
```

- Read `PageSize + 1` rows; if the extra row came back, `HasMore = True` and it is dropped from `Items`;
  `NextCursor` = the last *kept* row's `(TransactionDate, Id)`.
- Parameterize everything (OWASP — no string-concatenated values; only the optional clause *structure*
  is concatenated, never user data). Date params as ISO `"o"` strings to match the existing convention
  in these files.

### B1. Supporting indexes (new migration script)

Keyset/ordered paging is only fast with an index on the ordered column. Add the missing ones via a new
idempotent raw-SQL migration applied by `MariaDbSchemaInitializer` (EF migrations are unusable for
VB.NET — per CLAUDE.md):

```
MerchSys.Infrastructure/Data/Migrations/Central/0006_paging_support_indexes.sql   ' NEW
```

```sql
-- Inv_ShrinkageRecords ordered by RecordedDate DESC (ShrinkageService) — currently a filesort
CREATE INDEX `IX_Inv_ShrinkageRecords_RecordedDate` ON `Inv_ShrinkageRecords` (`RecordedDate`);
-- Inv_StockAuditRecords ordered by AuditedAt DESC (InventoryAuditService)
CREATE INDEX `IX_Inv_StockAuditRecords_AuditedAt` ON `Inv_StockAuditRecords` (`AuditedAt`);
-- (Add IX for the credit-history filter column once §B path 4's column/index is verified.)
```

Use `CREATE INDEX … ` guarded so re-runs are safe (the initializer's migration-ledger already
prevents re-application; if a raw guard is needed, check `information_schema.statistics` first — MariaDB
lacks `CREATE INDEX IF NOT EXISTS` before 10.5, so gate on the `__SchemaMigrations` ledger, not on
inline `IF NOT EXISTS`). Mirror each new index with `HasIndex(...)` in the matching EF
`Data/Configurations/*.vb` so the EF model and the live schema stay in sync.

### C. Default date window on the unbounded-by-default views

For paths 1–3 (transaction history, stock audit, shrinkage), the VM seeds a **default date range**
(e.g. last 90 days — make it a named constant, not a magic literal) on first load so the default fetch
is naturally bounded and instant. The user can widen the range (or pick "All") which then pages via §B.
This is the 80/20 guard: most users look at recent history; "All" stays fast via keyset.

### D. Incremental "Load more" wiring (minimal, no re-skin)

```
MerchSys.POS/ViewModels/TransactionHistoryViewModel.vb        ' MOD — LoadNextPageAsync, HasMore, cursor state
MerchSys.Inventory/ViewModels/ShrinkageViewModel.vb           ' MOD
MerchSys.Inventory/ViewModels/<StockAudit consumer>.vb        ' MOD (verify the actual consumer VM)
MerchSys.POS/ViewModels/CreditManagementViewModel.vb          ' MOD
MerchSys.App/Views/POS/TransactionHistoryView.xaml            ' MOD — "Load more" affordance at list end
MerchSys.App/Views/Inventory/ShrinkageView.xaml               ' MOD
MerchSys.App/Views/POS/CreditManagementView.xaml              ' MOD
```

- Each VM keeps the loaded window in its existing `ObservableCollection` and **appends** the next page
  (don't clear-and-reload). Bind a "Load more" button (visible when `HasMore`) — or scroll-to-end
  trigger — to `LoadNextPageAsync`, gated by the existing UX-15 `IsBusy`.
- **Preserve UX-37 recycling**: the grid stays inside its constrained host; do **not** wrap it in an
  outer `ScrollViewer` (that silently defeats virtualization — UX-37 watch-item).
- **Styling is out of scope** — reuse existing button/component styles from the UX epic; this plan adds
  no new visual design. Owner (read-only) sees the same paged reads; no write affordances added.

### E. Data-access resiliency hardening (careful — read §E watch-items)

```
MerchSys.App/Data/DatabaseConfig.vb        ' MOD — see caveats below
MerchSys.App/appsettings*.json             ' MOD — explicit pooling/timeout knobs
```

1. **Connection pooling — make explicit.** MySqlConnector pools by default, but pin it for clarity and
   LAN tuning: `Pooling=true;Minimum Pool Size=…;Maximum Pool Size=…;ConnectionIdleTimeout=…`.
   **SslMode**: keep `Preferred` (no-TLS LAN) or `Required` (TLS) — **never `None`/`Disabled`** (must be
   accepted by both MySqlConnector *and* the Oracle EF provider — see the standing dual-provider
   constraint). Confirm `ConnectionTimeout`/`DefaultCommandTimeout` are sane for the largest paged read.

2. **`AsNoTracking()` on EF display reads.** The few EF (non-raw-reader) queries that materialize
   entities for display should add `.AsNoTracking()` (no change-tracker overhead on read-only data).
   **Do not flip the global `QueryTrackingBehavior`** — write paths rely on tracking; scope `NoTracking`
   to the specific read queries only.

3. **`EnableRetryOnFailure` — guarded, possibly deferred.** This is the footgun the review flagged:
   - **Verify provider support first.** The Oracle `MySql.EntityFrameworkCore` provider (INFRA-23) does
     not necessarily expose a retrying `IExecutionStrategy` the way Pomelo does. If it doesn't, **defer**
     and document that resilience stays at the app layer (`ConnectionHealthMonitor` + the
     `Connection.RetryBackoffSeconds` ladder already in `appsettings.json`).
   - **If supported, it is INCOMPATIBLE with INFRA-26's user-initiated `BeginTransactionAsync`** (the
     FIFO `FOR UPDATE` path) unless each such transaction is wrapped in
     `strategy.ExecuteAsync(Function() … )`. A naïve blanket `EnableRetryOnFailure` will throw at runtime
     ("the configured execution strategy does not support user-initiated transactions") on the
     concurrency-critical sale path. **Either** wrap every explicit transaction (FIFO decrement, credit,
     AP) in the execution strategy, **or** do not enable retry. **Never enable it blanket.**
   - Recommendation: unless provider support is clean *and* the transaction wrapping is done in the same
     plan, **defer EnableRetryOnFailure** and record the rationale in the summary. App-layer retry
     already covers transient LAN blips.

## Specification

### Watch-items

1. **No semantic change.** Paging changes only *how much* is fetched per round-trip and *in what order* —
   never *what* a query means. Preserve every existing `WHERE` (esp. `IsDeleted = 0`, `IsVoided = 0`).
2. **Keyset cursor must be stable.** Always tie-break the ordered column with `Id` (`ORDER BY col DESC,
   Id DESC`; seek on the `(col, Id)` tuple). A bare `col`-only cursor drops/duplicates rows when two rows
   share a timestamp.
3. **Raw reader for reads.** New/modified read paths use `MySqlConnector` + `reader.Read()`, never
   `ToListAsync` on an entity query (EF Core 10 VB.NET silent-empty bug). Scalar/projection `ToListAsync`
   remains safe where already used.
4. **Virtualization not defeated.** No outer `ScrollViewer` around a paged grid; constrained height
   preserved; recycling (UX-37) and selection/focus (UX-17) and row motion (UX-25) must not regress.
5. **Concurrency untouched.** Do not alter the INFRA-26 `FOR UPDATE` transaction or `RowVersion` checks.
   The only interaction is §E's retry caveat — respect it.
6. **VB.NET / XAML traps.** No `Await` in `Catch`/`Finally` (BC36943) in any new async paging method —
   capture state, await after the block. Reserved-word-safe locals (`limit` is fine; avoid `err`, `date`,
   `now`). Full root prefix on any new `clr-namespace`. `System.Console` if logging.
7. **OWASP.** Parameterize all values; only optional-clause *structure* is concatenated, never user input.
   Role model: Owner read-only — paged reads only, no new write affordance.

### Constraints

- **No new NuGet.** Reuse UX-15 async primitives and the existing raw-reader pattern; add no parallel
  data-access infrastructure beyond the small `PageRequest`/`PagedResult` records.
- **Build gate** — `dotnet build WPF_Applications/MerchSys/MerchSys.slnx` must finish **0 errors,
  0 warnings**. On failure, document the errors in `Progress/` and **stop** (per CLAUDE.md — no blind fixes).
- **Realization check (both themes):** a deliberately large history table pages smoothly — first page is
  instant, "Load more" appends without a full reload, recycling holds, and the default date window bounds
  the initial fetch. Verify in Light **and** Dark.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. Each confirmed unbounded read path (CartService transaction history, ShrinkageService,
   InventoryAuditService, CreditManagement credit history) accepts a `PageRequest` and returns a bounded
   `PagedResult` via keyset; **no display grid issues an unbounded full-table `SELECT`**.
3. A supporting index exists for every ordered column used in a keyset query (new `0006_*` migration +
   matching `HasIndex`); `Inv_ShrinkageRecords.RecordedDate` and `Inv_StockAuditRecords.AuditedAt` are
   indexed; the credit-history filter-column/index discrepancy is resolved.
4. The three unbounded-by-default views seed a default date window; widening to "All" pages via keyset.
5. "Load more" appends the next page incrementally; UX-37 recycling and UX-15 `IsBusy` preserved; no
   selection/focus/motion regression.
6. Resiliency: pooling is explicit (SslMode `Preferred`/`Required`, never `None`/`Disabled`);
   `.AsNoTracking()` applied to EF display reads (global tracking default unchanged); `EnableRetryOnFailure`
   is either correctly wrapping the INFRA-26 `FOR UPDATE` transactions **or** deferred-with-rationale —
   **never** blanket-enabled in a way that breaks the FIFO path.
7. **No** business-rule, write-path, MediatR-contract, concurrency-semantics, or schema-*meaning* change
   (new indexes are additive).

## Out of Scope (Defer)

- Offset/page-number pagination UI — keyset "Load more" is chosen for scale; page-number jumps are not needed.
- Receipt/archival policy changes — `ReceiptArchivalService` already handles the fastest-growing table.
- A caching layer or read-replica — out of scope (see the Owner-remote-access phased plan for replicas).
- Charting/report-export pagination redesign (UX-41/UX-42 own those surfaces).
- Flipping the global `QueryTrackingBehavior` to `NoTracking`.
- Re-skinning any grid — visual design stays with the Experience epic.

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-34-summary.md` (template `Progress/_template.md`). Include:

- Each read path changed, with **before/after SQL** and the keyset cursor design used.
- Indexes added (the `0006_*` script) and the `HasIndex` mirrors; resolution of the credit-history
  column/index discrepancy.
- The default-date-window values chosen per view and why.
- The **`EnableRetryOnFailure` decision** — adopted (with the list of `FOR UPDATE` transactions wrapped in
  the execution strategy) **or** deferred (with provider-support finding + rationale). This is the
  highest-risk item; document it explicitly.
- Both-theme realization notes (instant first page, incremental "Load more", recycling intact).
- Any `codebase_wiki` discrepancies for Antigravity.

### Documentation

Add `LLM_Wiki/agent_wiki/patterns/mariadb-keyset-pagination.md` (the keyset recipe: stable `(col, Id)`
cursor, `PageSize + 1` HasMore trick, no `COUNT(*)` on unbounded tables, index requirement, raw-reader
binding) per `LLM_Wiki/_system/workflow-agent-wiki-update.md`; update `agent_wiki/index.md` + `log.md`.
XML doc on `PageRequest`/`PagedResult` and on each changed service method documenting the cursor contract.
