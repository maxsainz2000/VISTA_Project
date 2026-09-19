---
module: MerchSys.Infrastructure
plan-id: INFRA-33
title: "Post-Pivot MariaDB SQL Compatibility Remediation — Archival Dialect, Audit Materializer, MaxLength Caps"
depends-on: [INFRA-25, INFRA-31]
estimated-files: 6
priority: high
amendment-ref: AMD-2026-05-28-01
origin: audit (db_normalization_compatibility_report.md, verified 2026-05-29)
---

# INFRA-33: Post-Pivot MariaDB SQL Compatibility Remediation

## Context

The `db_normalization_compatibility_report.md` audit surfaced four WPF-vs-MariaDB
compatibility defects left by the 2026-05-28 SQLite→MariaDB pivot. Each was
independently verified against the live `merchsys_central` database and `master`
source on 2026-05-29:

| Audit § | Finding | Verified verdict |
|---|---|---|
| 2.1 | Inherited `RowVersion` crashes on 19 tables | **Already fixed by INFRA-31** — OUT OF SCOPE (see below) |
| 2.2 | `ReceiptArchivalService` raw SQL is SQLite dialect | **Real, live bug** → WI-1 |
| 2.3 | `InventoryAuditService.GetLatestAuditPerProductAsync` full-entity `ToListAsync` | **Real, latent bug** → WI-2 |
| 2.4 | EF `HasMaxLength` caps tighter than DB columns | **Real, low severity** → WI-3 |

This plan covers **only the three live items (WI-1, WI-2, WI-3)**. They are
independent, touch three different modules, and share a single acceptance theme
(the affected operation runs correctly against MariaDB). They are bundled because
they originate from one audit and are individually small.

### Why §2.1 is explicitly out of scope

INFRA-31 (commit `b082961`) added `IgnoreNonTokenRowVersionConvention` to
`BaseDbContext.ConfigureConventions`, which centrally ignores the inherited
`RowVersion` property on every entity that is not a real concurrency token. The
report's Step 3.1 (add 19 per-config `builder.Ignore` calls) is **obsolete and must
not be implemented** — it would duplicate and regress INFRA-31's single mechanism.
Runtime confirmation: the 2026-05-29 goods-receipt expense insert crashed only on
`InputVat`, never on `RowVersion`, proving the convention is active for
`Acc_ExpenseRecords`. **Do not touch the RowVersion mechanism in this plan.**

## Prerequisites

- **INFRA-25** — MariaDB DbContext migration (the pivot that made the SQLite dialect
  invalid).
- **INFRA-31** — RowVersion mapping correction (so this plan does not re-touch that area).

## Confirmations already performed (2026-05-29)

Implementers do not need to re-verify these — they are recorded here as ground truth:

- **Trigger `tr_pos_receipts_no_delete` is already MariaDB-native.** It reads
  `Pos_ArchivalSession` (backtick-quoted `` `key` ``) and fires
  `SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BIR-immutable'` unless a row exists with
  `` `key` `` = `'archival_in_progress'` and `expires_at > CURRENT_TIMESTAMP(6)`.
  **No trigger change is required** — the bug is entirely in the application's raw SQL.
- **`Pos_ArchivalSession` schema:** `` `key` `` `VARCHAR(128)` PK, `` `value` `` `INT`,
  `` `expires_at` `` `DATETIME(6)`. (`key` and `value` are MariaDB reserved-ish words —
  they MUST be backtick-quoted in every statement.)
- **Server `sql_mode` does not include `ANSI_QUOTES`** (`NO_ZERO_IN_DATE,NO_ZERO_DATE,
  NO_ENGINE_SUBSTITUTION`), so the current `""double-quoted""` identifiers are parsed as
  **string literals** — the statements are definitively broken, not merely non-portable.
- Archive target tables (`Pos_OfficialReceiptArchive`, `Pos_ReceiptIntegrityArchive`)
  exist and carry their own no-update/no-delete immutability triggers.

## Wiki References

- `LLM_Wiki/agent_wiki/errors/efcore-vat-ledger-columns-missing-central-schema.md` (same post-pivot regression class)
- `LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` (the materializer bug behind WI-2; post-pivot note uses `MySqlConnection`)
- `LLM_Wiki/agent_wiki/patterns/mariadb-pure-client-server-architecture.md`
- `db_normalization_compatibility_report.md` §2.2 / §2.3 / §2.4 (and its Verification Addendum)

---

## Deliverables

### WI-1 — Convert `ReceiptArchivalService` raw SQL to MariaDB dialect (§2.2)

File: `MerchSys.POS/Services/Archival/ReceiptArchivalService.vb` (MOD)

Three raw statements in `RunBatchAsync` use SQLite syntax and will throw on MariaDB
the first time the archival background loop runs (or `ArchiveEligibleAsync` is invoked):

1. **Session flag write (lines ~235–238).** Replace the SQLite
   `INSERT OR REPLACE … datetime('now', '+5 minutes')` with MariaDB. Preferred form
   (no row churn, no delete semantics):

   ```vb
   Await db.Database.ExecuteSqlRawAsync(
       "INSERT INTO `Pos_ArchivalSession` (`key`, `value`, `expires_at`) " &
       "VALUES ('archival_in_progress', 1, DATE_ADD(NOW(6), INTERVAL 5 MINUTE)) " &
       "ON DUPLICATE KEY UPDATE `value` = 1, `expires_at` = DATE_ADD(NOW(6), INTERVAL 5 MINUTE)",
       cancellationToken)
   ```

   (`REPLACE INTO … NOW(6) + INTERVAL 5 MINUTE` is an acceptable alternative.) The
   resulting `expires_at` must satisfy the trigger's `expires_at > CURRENT_TIMESTAMP(6)`
   check — `NOW(6)`/`CURRENT_TIMESTAMP(6)` are equivalent, so a +5-minute window is valid.

2. **Source-row deletes (lines ~243–253).** Keep the `{0}` parameter placeholders
   (EF translates them to real parameters — correct on MariaDB), but change the
   `""double-quoted""` identifiers to backticks:

   ```vb
   "DELETE FROM `Pos_ReceiptIntegrity` WHERE `ReceiptId` = {0}"
   "DELETE FROM `Pos_OfficialReceipts`  WHERE `Id` = {0}"
   ```

3. **Session flag clear (lines ~257–259).** Backtick the reserved `key` column:

   ```vb
   "DELETE FROM `Pos_ArchivalSession` WHERE `key` = 'archival_in_progress'"
   ```

Constraints:
- **Do not** alter the insert-then-delete ordering, the transaction/connection
  lifecycle, the `Await`-outside-`Catch` rollback pattern, or the trigger.
- Backtick **every** identifier that is a reserved word (`` `key` ``, `` `value` ``);
  backticking all identifiers is fine and safest.
- This is a non-bypassable defect for BIR receipt archival — WI-1 is the highest-value
  item in this plan.

### WI-2 — Rewrite `GetLatestAuditPerProductAsync` with a raw reader (§2.3)

File: `MerchSys.Inventory/Services/InventoryAuditService.vb` (MOD)

`GetLatestAuditPerProductAsync` (lines ~204–210) projects a full entity
(`StockAuditRecord` + included `Product`) straight into `.ToListAsync()`. Under the
EF Core 10 + VB.NET materializer bug this returns an empty list (or fails to
translate the `GroupBy → First()`), silently breaking the "latest audit per product"
dashboard.

Rewrite it using the raw `MySqlConnection` reader pattern. **Reuse the proven pattern
already in this same file** — `GetAuditHistoryAsync` (lines ~140–202) is the template:
it opens a `MySqlConnection` from `_db.Database.GetConnectionString()`, reads
`StockAuditRecord` rows, then hydrates `Product` via `StockService.ReadProduct(reader)`.

Requirements:
- Return one row per `ProductId` — the **latest by `AuditedAt`** (to match the
  original `OrderByDescending(AuditedAt).First()` semantics). A correlated subquery or
  `MAX(AuditedAt)` join is acceptable; do **not** silently substitute `MAX(Id)` unless
  you note that records are never backdated. Prefer matching the original ordering.
- Hydrate the associated `Product` for each row (the dashboard binds product fields).
  Reuse `StockService.ReadProduct(reader)` rather than hand-mapping product columns, so
  the column set stays consistent with `GetAuditHistoryAsync`.
- Respect any soft-delete semantics consistent with `GetAuditHistoryAsync` (mirror its
  filtering — it does not special-case `IsDeleted`, so match that unless the entity is
  `ISoftDeletable`).
- Parameterize all inputs (there are none here, but keep the `MySqlParameter` style if
  any are introduced). No string-concatenated user input (OWASP DA).

> The report's §3.3 contains a reference implementation; treat it as a sketch, not
> gospel — align the SELECTed columns to the **actual** `Inv_StockAuditRecords` /
> `Inv_Products` schema and to whatever `StockService.ReadProduct` expects.

### WI-3 — Align EF `HasMaxLength` caps to DB column widths (§2.4)

Three EF caps are tighter than their MariaDB columns. Low severity (they only bite if
a generated value exceeds the cap), but trivially correct. Widen each to match the DB:

| File (MOD) | Property | Change |
|---|---|---|
| `MerchSys.Purchasing/Data/Configurations/PurchaseOrderConfiguration.vb` | `OrderNumber` | `HasMaxLength(20)` → `HasMaxLength(128)` |
| `MerchSys.Purchasing/Data/Configurations/GoodsReceiptConfiguration.vb` | `ReceiptNumber` | `HasMaxLength(20)` → `HasMaxLength(128)` |
| `MerchSys.Purchasing/Data/Configurations/ReorderSuggestionConfiguration.vb` | `ProductName` | `HasMaxLength(200)` → `HasMaxLength(255)` |

Do not change `IsRequired()`, the unique indexes, or any other property.

---

## Acceptance Criteria

1. **WI-1:** `ReceiptArchivalService` contains no SQLite-only syntax — no
   `INSERT OR REPLACE`, no `datetime('now', …)`, no double-quoted identifiers. The
   session-flag insert uses `ON DUPLICATE KEY UPDATE` (or `REPLACE INTO`) with
   `NOW(6) + INTERVAL`/`DATE_ADD`. A manual `ArchiveEligibleAsync(DateTime.UtcNow, n, ct)`
   invocation against MariaDB completes a batch (or cleanly no-ops with zero eligible
   rows) without a SQL syntax error, and the `tr_pos_receipts_no_delete` trigger permits
   the deletes only while the session flag is live.
2. **WI-2:** `GetLatestAuditPerProductAsync` uses a raw `MySqlConnection` reader (no
   full-entity `ToListAsync`) and returns one populated `StockAuditRecord` (with
   `Product` hydrated) per product that has audit history — verified non-empty against a
   DB that has audit rows.
3. **WI-3:** The three `HasMaxLength` values match the DB column widths (128/128/255).
4. **Build: 0 errors / 0 warnings** across the solution.
5. **No regression to INFRA-31:** the RowVersion convention in `BaseDbContext` is
   untouched; no per-config `builder.Ignore(RowVersion)` calls are added.

## Out of Scope (Defer / Reject)

- **§2.1 RowVersion remediation** — already delivered by INFRA-31. Do not implement
  the report's Step 3.1. Do not modify `BaseDbContext` or any RowVersion config.
- **Part 1 (3NF) findings** — descriptive only; all are intentional, documented
  denormalizations (temporal snapshots for BIR, derived-value caches). No schema change.
- Any change to the immutability triggers or the `Pos_ArchivalSession` schema (both are
  already correct on MariaDB).
- Broader audit of other services for SQLite-dialect leakage beyond
  `ReceiptArchivalService` (worth a future sweep, but not this plan; if the implementer
  notices another instance while working, log it in the summary rather than fixing it
  here).

## Output Requirements

### Implementation Summary

`Progress/VISTA_Modules/Infrastructure/INFRA-33-summary.md` per `Progress/_template.md`. Include:

- WI-1: the exact final form of each rewritten statement, and confirmation the manual
  archival invocation ran on MariaDB without syntax error (or cleanly no-op'd).
- WI-2: which "latest per product" strategy was used (AuditedAt subquery vs MAX(Id)) and
  why, plus evidence the method returns a non-empty hydrated list against seeded data.
- WI-3: the three confirmed cap changes.
- Confirmation that the INFRA-31 RowVersion mechanism was not touched.
- Any additional SQLite-dialect or materializer-bug sites noticed but left for a future plan.

### Documentation

- Agent-wiki: if WI-1 establishes the general "SQLite-dialect leakage in raw
  `ExecuteSqlRawAsync` after the pivot" lesson, add or extend an `errors/` entry and
  update `agent_wiki/index.md` + `log.md` per the wiki workflow. WI-2 can reference the
  existing `efcore-vbnet-tolistasync-entity-empty.md` rather than duplicating it.
