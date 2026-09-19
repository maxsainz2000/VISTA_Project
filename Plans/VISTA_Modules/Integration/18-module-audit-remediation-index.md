---
module: MerchSys.Integration
plan-id: INT-18
title: "Module audit remediation — triage, verdicts & sequencing"
depends-on: []
estimated-files: 1
priority: high
---

# INT-18: Module audit remediation — triage, verdicts & sequencing

## Context

Six module audit reports were produced at the repository root:

- `accounting_audit_report.md`
- `merchsys_app_audit_report.md`
- `merchsys_inventory_audit_report.md`
- `merchsys_pos_audit_report.md`
- `merchsys_purchasing_audit_report.md`
- `merchsys_sharedkernel_audit_report.md`

Every finding was verified line-by-line against the live source on **2026-06-11**. Each report now carries a **Verification Addendum** (and inline danger-markers on the worst findings) that **governs where it conflicts with the original severities or fixes**. The code quotes in the reports are accurate; many of the **severities, rationales, and recommended fixes are not**.

This plan is the **master index** for acting on the verified findings. It records the disposition of every finding — which batch fixes it, or why it is deferred or must not be implemented — and defines execution order. It writes no product code.

### Three cross-cutting facts established by verification

1. **Do NOT convert raw ADO.NET reader loops to full-entity `ToListAsync()`.** Full-entity `ToListAsync()` (bare `DbSet`, `.Where`, `.Include`, `.AsNoTracking`, `FromSqlRaw(...)`) silently returns an **empty list** in VB.NET + EF Core 10 (`agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`). Several reports recommend exactly this conversion as a "fix"; it would silently disable the feature it touches (tamper-chain validation → no-op; FIFO deduction → every checkout throws). Scalar projections (`.Select(x.Member)`), anonymous projections, `FirstOrDefaultAsync`, `CountAsync`, and `SumAsync` are **not** affected and may be used freely. This is consistent with the existing INT-14/15/16 remediation, which fixes *misused* `ToListAsync` by converting it *to* raw ADO — the opposite direction. Nothing in this program reverses that work.

2. **The "Critical concurrency / class-level state" findings are per-process, not cross-terminal.** The four client laptops are separate OS processes that share nothing in memory; class fields cannot leak between them. Cross-terminal safety is the database's job (`SELECT ... FOR UPDATE` + optimistic tokens), which the code already does. Within one process the affected services are `AddScoped` but resolved from the **root** provider (`MainWindowViewModel:220`), so they are effective singletons — yet serialized by a non-reentrant DbContext that throws on concurrent use before any field race manifests. The field→local refactor is correct hygiene (already proven by `CartService.GetTransactionHistoryPageAsync`) but is **cleanup, not a Critical concurrency fix**.

3. **Several "Critical/High" findings are latent or incorrect.** Synchronous `SaveChanges()` has zero call sites; `AuditInterceptor` is unregistered dead code; `OwnerDashboardViewModel` already disposes its timer on `Unloaded`. These were re-rated in the addenda.

## Prerequisites

- The six verified audit reports (with addenda) at the repository root.
- `agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` — the canonical `ToListAsync` bug + workaround shape.

## Wiki References

- `LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`
- `LLM_Wiki/wiki/concepts/centralized-database-architecture.md` — mandates `SELECT ... FOR UPDATE` for the FIFO decrement.
- `CLAUDE.md` — VB.NET Build Traps; testing-phase rules (do not deep-troubleshoot build failures — document them).

## Deliverables

```
Operator/debug-logs/module-audit-remediation-index.md   ' New — mirror of the disposition table below
```

This is a **read-only / docs-only** plan. No `.vb` files are edited.

## Specification

### Disposition table

Every finding from the six reports, with its verified severity and the batch that fixes it. `Do-not-implement` and `Defer` rows are justified in the sections that follow.

| Source finding | Verified severity | Disposition |
|---|---|---|
| ACC-1 class-level state | Low (cleanup) | **INT-20** (field→local) |
| ACC-2 SQLite `TEXT`/`INTEGER` + ISO dates | High | **INT-19** (dates) + **INT-21** (column types) |
| ACC-3 manual connection management | — | **Do-not-implement** (read-only SELECTs; rationale incorrect) |
| APP-1 login datetime double-parse | Low–Med | **INT-21** (direct cast) |
| APP-2 OwnerDashboard leak | Low | **Do-not-implement** (already disposed; Singleton fix harmful) |
| APP-3 CTS dispose no-op | Low–Med | **INT-20** |
| APP-4 converter boxing | Low | **Defer** (optional micro-opt) |
| INV-1 class-level state | Low (cleanup) | **INT-20** |
| INV-2 timer/IDisposable leak | High | **INT-23** (incl. `FinancialOverviewViewModel`) |
| INV-3 DB in ProductManagementViewModel | Med–High | **INT-23** |
| INV-4 raw ADO / change-tracker / N+1 | — | **Do-not-implement** the EF rewrite; **INT-20** picks up the `ModifiedBy` sub-point |
| INV-5 expiry threshold inconsistency | Med (minor) | **Defer** (Phase 2) |
| INV-6 date-to-string params | Med | **INT-19** |
| INV-7 stale SQLite comment | Low | **INT-20** |
| POS-1 `strftime` on MariaDB | Critical (bug) | **INT-21** (`YEAR()`, raw ADO retained) |
| POS-2 raw ADO / column-index | Low–Med | **Do-not-implement** the `ToListAsync` conversion (keep raw ADO) |
| POS-3 DB in CreditManagementViewModel | Med–High | **INT-23** |
| POS-4 `CountAsync` txn-number race | Med (real race) | **INT-22** |
| POS-5 TIN regex mismatch | Med | **INT-21** |
| POS-6 SyncLock on SemaphoreSlim | Med | **INT-20** |
| POS-7 archival delete loop | Med (perf) | **Defer** (Phase 2) |
| POS-8 VAT centavo drift | Med | **INT-21** |
| POS-9 hardcoded "12%" label | Low | **Defer** (Phase 2, with the dynamic-rate cluster) |
| POS-10 unused `Items` column | Low | **Defer** (mitigated by `CanonicalPayload`) |
| PUR-1 class-level state | Low (cleanup) | **INT-20** |
| PUR-2 DB in dashboard/PO-list VMs | Med–High | **INT-23** |
| PUR-3 PO-number generation | Med (perf + race) | **INT-22** |
| PUR-4 `DefaultUser="Manager"` clobber | Med | **INT-22** |
| PUR-5 VAT event rounding | Med (minor) | **Defer** (Phase 2, dynamic-rate cluster) |
| PUR-6 date-to-string param | Med | **INT-19** |
| PUR-7 stale SQLite comment | Low | **INT-20** |
| SK-1 sync `SaveChanges` not overridden | Low (latent) | **INT-24** (hardening) |
| SK-2 fail-open role check | Low (good hardening) | **INT-24** (hardening) |
| SK-3 reflection-based self-service | Low–Med | **INT-24** (hardening) |
| SK-4 silent RowVersion ignore | — | **Do-not-implement** throw; **INT-24** (safe warning variant) |
| SK-5 PageRequest bounds | Low | **INT-24** (hardening) |
| SK-6 UserAccount not IAuditable | Low | **INT-24** (hardening) |

### Batch definitions (execution order)

| Batch | Title | Concern | Risk |
|---|---|---|---|
| **INT-19** | Native `DateTime` SQL parameters | Replace ISO/string date params with native `DateTime` (~30 sites) | Low, mechanical |
| **INT-20** | Service field→local cleanup + small correctness fixes | Query-buffer fields → locals; `VatConfigurationLoader` lock; `ConnectionHealthMonitor` CTS dispose; stale SQLite comments; FIFO `ModifiedBy` | Low |
| **INT-21** | Verified functional fixes | `strftime`→`YEAR()`; TIN regex; VAT header/line reconcile; TamperAudit column types; login date cast | Medium, targeted |
| **INT-22** | Concurrency-safe sequence numbers + audit-user attribution | POS txn # + PO # via sequence table; `BaseDbContext` honours session user | Medium |
| **INT-23** | ViewModel data-access layering + timer disposal | Move VM raw SQL into services (keep raw ADO/projection); `IDisposable` on the three leaking VMs | Medium |
| **INT-24** | SharedKernel security & robustness hardening | SK-1 sync-`SaveChanges` parity; SK-2 fail-closed role default; SK-3 typed self-service check; SK-4 startup *warning* (not throw); SK-5 `PageSize` clamp; SK-6 `UserAccount : IAuditable` | Low–Med |

INT-19 and INT-20 both touch several of the same service files; sequence INT-19 **before** INT-20 so the date-parameter edits land first and the field→local pass is the last edit per file. INT-21, INT-22, and INT-23 are independent of each other and may proceed in any order after INT-20. INT-24 (hardening) depends on INT-22 because both touch `BaseDbContext`.

### Do-not-implement register

Record these explicitly so a later reader does not "reopen" them:

- **ACC-3** — the manual-connection rewrite. The flagged methods are read-only `SELECT`s with no open transaction; `SaveChanges` interceptors never run on reads. The proposed reuse of the DbContext connection can raise "connection already in use." No change.
- **APP-2** — the Singleton registration. `OwnerDashboardViewModel` already implements `IDisposable` and is disposed on `View.Unloaded`; Singleton would be disposed on first navigation-away and reused broken.
- **INV-4 / POS-1 / POS-2** — the "convert raw ADO.NET to `ToListAsync()`" fixes. They reintroduce the documented empty-materialisation bug. INT-21 fixes POS-1's *actual* bug (`strftime`) inside the raw query instead.
- **SK-4** — the throw-at-startup convention change. Throwing breaks any `ConcurrencyAwareEntity` subclass that intentionally maps no token. **INT-24 implements the safe variant**: audit which entities must carry a token, then emit a startup *warning* — never an exception.

### Deferred / Phase-2 backlog

Lower-priority items, grouped for a future batch. Not scheduled now. (The SharedKernel hardening cluster — SK-1/2/3/5/6 plus the safe SK-4 variant — was promoted to **INT-24**.)

- **Dynamic VAT rate cluster** — POS-9 (receipt label), PUR-5 (GR event rounding), and the systemic hardcoded `1.12D`/12% assumption in `GoodsReceiptVatCalculator`, `GoodsReceivingViewModel`, `GoodsReceiptLine`, and POS display labels. Worth one focused pass once a non-12% rate is actually required.
- **Minor** — INV-5 (expiry threshold harmonisation), POS-7 (batch archival delete), POS-10 (`OfficialReceipt.Items` dead column), APP-4 (converter boxing).

## Implementation Notes

- Read-only plan: the only artifact is `Operator/debug-logs/module-audit-remediation-index.md`, which mirrors the disposition table, batch definitions, and do-not-implement register above.
- Do not silently drop any finding. Every finding from the six reports appears in the disposition table with a batch, `Defer`, or `Do-not-implement`.
- The batch plans (INT-19 … INT-24) depend on this index for scope. They must not re-decide a disposition recorded here.

## Acceptance Criteria

1. `Operator/debug-logs/module-audit-remediation-index.md` exists and mirrors the disposition table, batch definitions, and do-not-implement register.
2. Every one of the 37 findings across the six reports has exactly one disposition.
3. The do-not-implement register names all five protected findings (ACC-3, APP-2, INV-4, POS-1 EF-rewrite, POS-2, SK-4) with one-line justification.
4. No `.vb` files are modified by this plan.

## Output Requirements

Create a progress report at `Progress/VISTA_Modules/Integration/INT-18-summary.md` using `Progress/_template.md`. Include the finding count per disposition (fixed / deferred / do-not-implement) and a pointer to each downstream batch plan.
