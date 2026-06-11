---
module: MerchSys.Integration
plan-id: INT-19
title: "Native DateTime SQL parameters (replace ISO/string date params)"
depends-on: [INT-18]
estimated-files: 11
priority: high
---

# INT-19: Native DateTime SQL parameters (replace ISO/string date params)

## Context

Verification of the six module audit reports (see **INT-18**) confirmed a **systemic** anti-pattern that each individual report flagged only in part: raw-ADO date range parameters are bound as **formatted strings** rather than native `System.DateTime` values. The two shapes are:

- `New MySqlParameter("@x", someDate.ToString("o"))` — ISO 8601 round-trip format (`2026-06-11T18:27:41.0000000Z`). The `T` separator, `Z` suffix, and 7-digit fraction are **not** accepted by MariaDB's implicit string→`DATETIME` coercion; the comparison can warn and match nothing, silently emptying date-filtered results. **This is the risky shape.**
- `.AddWithValue("@x", someDate.ToString("yyyy-MM-dd HH:mm:ss"))` — MySQL-compatible format; parses, but loses sub-second precision. Lower-risk, converted here for consistency.

MySqlConnector serialises a native `DateTime` parameter in the correct wire format with no string round-trip. The codebase already does this correctly in the FIFO path (`StockService.DeductStockFIFOAsync` binds `@now` as a native `DateTime`), which is the reference shape for this plan.

This batch replaces the string-formatted **command parameters** only. It does not touch raw ADO.NET reader loops (those are the deliberate `ToListAsync`-bug workaround) and does not change query logic.

## Prerequisites

- **INT-18** — the remediation index (scope authority).

## Wiki References

- `LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md` — why the raw-ADO loops stay.
- `CLAUDE.md` — MariaDB CLI; testing-phase rules.

## Deliverables

```
MerchSys.Accounting/Services/VatReportingService.vb            ' Modified — @ws/@we (lines ~378-389, 410-411)
MerchSys.Accounting/Services/VatReliefReportService.vb         ' Modified — @ws/@we (lines ~50-51, 66-67)
MerchSys.Accounting/Services/ITamperAuditQueryService.vb       ' Modified — @fromUtc/@toUtc (lines ~49-50)
MerchSys.Inventory/Services/ExpiryTrackingService.vb           ' Modified — @today/@threshold (lines ~50-51, 115)
MerchSys.Inventory/Services/ShrinkageService.vb                ' Modified — @fromUtc/@toUtc/@cursorDate (lines ~294-297)
MerchSys.Inventory/Services/InventoryAuditService.vb           ' Modified — @startDate/@endDate (lines ~157-158)
MerchSys.Inventory/Services/VelocityService.vb                 ' Modified — @windowStart (line ~135)
MerchSys.POS/Services/CartService.vb                           ' Modified — @startDate/@endDate/@fromUtc/@toUtc/@cursorDate (lines ~169-170, 263-266)
MerchSys.POS/Services/DailySummaryService.vb                   ' Modified — @start/@end (lines ~51-52, 94-95, 163-164, 206-207)
MerchSys.POS/Services/CreditService.vb                         ' Modified — @cutoff (line ~228)
MerchSys.POS/Services/SalesReturnService.vb                    ' Modified — @startDate/@endOfDay (lines ~146-147)
MerchSys.Purchasing/Services/AccountsPayableService.vb         ' Modified — @today (line ~186)
```

(Estimated-files counts the primary service files; line numbers are guides — match by parameter name, not by line.)

## Specification

### The change

For every `MySqlParameter` / `AddWithValue` whose **value** is a `DateTime` rendered through `.ToString("o")` or `.ToString("yyyy-MM-dd HH:mm:ss")`, pass the `DateTime` itself:

```vb
' Before
cmd.Parameters.Add(New MySqlParameter("@fromUtc", fromUtc.ToString("o")))
' After
cmd.Parameters.Add(New MySqlParameter("@fromUtc", fromUtc))
```

```vb
' Before
cmd.Parameters.AddWithValue("@windowStart", windowStart.ToString("yyyy-MM-dd HH:mm:ss"))
' After
cmd.Parameters.AddWithValue("@windowStart", windowStart)
```

Where the code first builds a `Dim wsStr = windowStart.ToString("o")` local and binds the local (e.g. `VatReportingService.CollectLedgerDataAsync`, `VatReliefReportService`), delete the `*Str` local and bind the `DateTime` directly. The values are already `DateTime.UtcNow`-based, so UTC semantics are preserved — do not add any time-zone conversion.

### MUST NOT change (out of scope — would break things)

- **`ReceiptIntegrityService.vb:303`** — `receipt.IssueDate.ToString("o")` is part of the **canonical hash payload**, not a SQL parameter. Changing it breaks the receipt integrity chain. Leave it.
- **Display/report formatting** — `.ToString("yyyy-MM-dd HH:mm")` / `.ToString("o")` used to build user-facing text or CSV/PDF output (`TransactionHistoryViewModel:753`, `BirCompliantReceiptBodyComposer:68`, `TamperReportExporter:91/92/103/233`, `VatReturnExporter:310`). Not SQL parameters. Leave them.
- **`MariaDbSchemaInitializer.vb:208/269`** — `.ToString("yyyy-MM-dd HH:mm:ss.ffffff")` builds seed-row values inside DDL/DML string literals in the correct MySQL format. Out of scope.
- **Raw ADO.NET reader loops / SELECT text** — unchanged. This plan edits only the parameter binding.
- **`IAuthenticationService.vb` date-string params (lines 183/227/239/245)** — these are write-path (`UPDATE`) params on the auth raw-SQL path; they are MySQL-compatible and on a code path that runs before EF is involved. They are folded into **INT-21** (which already edits that file for the login date-cast), not here, to keep one file owned by one batch.

### Reference shape

`StockService.DeductStockFIFOAsync` already binds `New MySqlParameter("@now", now)` with a native `DateTime`. Match that.

## Implementation Notes

- Pure parameter-value substitution. If a diff hunk changes anything other than removing a `.ToString(...)` call from a parameter value, revert it.
- Some methods build a date local once and reuse it across two commands (e.g., `VatReportingService` binds `@ws`/`@we` to both the revenue and expense commands). Bind the `DateTime` to every command that used the string local; then remove the now-unused string local.
- After each file, `dotnet build WPF_Applications/MerchSys/MerchSys.slnx`. Per `CLAUDE.md`, if the build surfaces a non-trivial error, **document it in the summary and stop** — do not deep-troubleshoot in this session.
- No nullable-date trap: where the source guards `If startDate.HasValue Then ... startDate.Value.ToString("o")`, bind `startDate.Value` (still inside the `HasValue` guard).

## Acceptance Criteria

1. Every date-valued `MySqlParameter`/`AddWithValue` in the Deliverables files binds a native `DateTime` (no `.ToString("o")` / `.ToString("yyyy-MM-dd...")` on a parameter value).
2. No raw SELECT text, reader loop, or query logic changed.
3. The four out-of-scope categories above are untouched (hash payload, display formatting, schema initializer, auth write-path).
4. `dotnet build MerchSys.slnx` — target 0 errors, 0 warnings (or documented per protocol).
5. A grep for `New MySqlParameter\(".*", .*\.ToString\("o"\)\)` over `src/` returns zero hits except the documented out-of-scope display/exporter sites.

## Output Requirements

Create a progress report at `Progress/VISTA_Modules/Integration/INT-19-summary.md` using `Progress/_template.md`. List every parameter converted (file + parameter name), the build result, and confirm the out-of-scope sites were left intact.
