---
module: MerchSys.Integration
plan-id: INT-21
title: "Verified functional fixes (strftime, TIN regex, VAT reconcile, tamper schema, login cast)"
depends-on: [INT-18]
estimated-files: 6
priority: high
---

# INT-21: Verified functional fixes

## Context

This batch fixes the **confirmed, independent functional defects** from the verified audit reports (see **INT-18**). Each is small and self-contained. The grouping theme is "correctness bugs that change observable behaviour," as opposed to the mechanical cleanups in INT-19/21.

Critically, the POS-1 fix here is **not** the EF rewrite the report proposed (that reintroduces the `ToListAsync` empty-result bug) — it is a one-token change to the raw SQL.

## Prerequisites

- **INT-18** — remediation index.

## Wiki References

- `LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`
- `LLM_Wiki/wiki/concepts/bir-compliance.md` — VAT decomposition and TIN format.
- `CLAUDE.md` — EF Core migrations are broken for VB.NET; schema changes are raw SQL at startup.

## Deliverables

```
MerchSys.POS/Services/ReceiptIntegrityService.vb                          ' Modified — strftime -> YEAR (POS-1)
MerchSys.POS/ViewModels/VatSettingsViewModel.vb                           ' Modified — TIN regex (POS-5)
MerchSys.POS/Services/VatAwareReceiptService.vb                           ' Modified — VAT header reconcile (POS-8)
MerchSys.Accounting/Data/Configurations/TamperAuditEntryConfiguration.vb  ' Modified — remove SQLite column types (ACC-2)
MerchSys.App/Data/MariaDbSchemaInitializer.vb                             ' Modified (verify) — Acc_TamperAuditLog DDL alignment (ACC-2)
MerchSys.App/Services/IAuthenticationService.vb                           ' Modified — login date cast + write-path native dates (APP-1)
```

## Specification

### POS-1 — `strftime` on MariaDB (`ReceiptIntegrityService.ValidateChainAsync`)

`strftime('%Y', r.IssueDate)` is SQLite-only and throws on MariaDB. **Keep the raw ADO.NET query** and replace the function with MariaDB's `YEAR()`:

```sql
WHERE YEAR(r.IssueDate) = @year
```

Do **not** apply the report's EF `.Include(...).ThenInclude(...).Where(...).ToListAsync()` rewrite — full-entity `ToListAsync()` returns an empty list in VB.NET + EF Core 10, which would make `ValidateChainAsync` report `TotalChecked = 0` and pass with nothing checked (a silent security no-op). Optionally fold `_integrityChainList` to a local as part of INT-20 (not required here).

### POS-5 — TIN regex mismatch (`VatSettingsViewModel.Tin`)

The client `<RegularExpression>` and the backend `VatConfigurationWriter.TinPattern` disagree (the client rejects `999-999-999` and the legacy 14-digit branch form, and accepts unhyphenated values the backend rejects). Align the client to the backend:

```vb
<RegularExpression("^\d{3}-\d{3}-\d{3}(-\d{3}|-\d{5})?$",
    ErrorMessage:="TIN must match BIR format: 999-999-999, 999-999-999-000, or 999-999-999-00000.")>
```

### POS-8 — VAT header/line centavo reconcile (`VatAwareReceiptService.GenerateReceiptAsync`)

The per-line VAT (`totals.OutputVat`, summed from `VatCalculator`) can differ by centavos from the header `transaction.VatAmount` set at cart checkout, and the header is never reconciled. After `Dim totals = _vatCalculator.AggregateTransaction(...)`, stamp the header to match the line aggregate:

```vb
transaction.VatAmount = totals.OutputVat
```

And after the inner receipt is produced (`Dim receipt = Await _inner.GenerateReceiptAsync(...)`), align the receipt header too, before returning:

```vb
receipt.VatAmount = totals.OutputVat
```

(The line-level decomposition is the BIR-correct basis; the header is synced to the sum of lines.) If `receipt.VatAmount` is set inside the inner service after a `SaveChanges`, ensure the new assignment persists — re-save if the inner service already detached/committed; otherwise the assignment on the tracked entity is flushed by the existing pipeline. Verify by reading `ReceiptService.GenerateReceiptAsync`.

### ACC-2 — TamperAudit SQLite column types

`TamperAuditEntryConfiguration` maps `Int64` keys as `INTEGER` and dates/strings as `TEXT` (SQLite leftovers). Remove the `HasColumnType(...)` overrides so EF maps to MariaDB defaults (the entity types drive `BIGINT`/`DATETIME(6)`/`VARCHAR`); keep `IsRequired`, `HasMaxLength`, the key, and the index:

```vb
builder.Property(Function(e) e.Id).ValueGeneratedOnAdd()                 ' was HasColumnType("INTEGER")
builder.Property(Function(e) e.DetectedAt).IsRequired()                  ' was HasColumnType("TEXT")
builder.Property(Function(e) e.ReceiptId).IsRequired()                   ' was HasColumnType("INTEGER")
builder.Property(Function(e) e.ReceiptNumber).IsRequired().HasMaxLength(50)
' ...drop HasColumnType("TEXT") from the remaining string/date properties, keeping HasMaxLength.
```

**Schema alignment (verify):** the actual `Acc_TamperAuditLog` table is created by raw SQL at startup, not by EF migrations. Open `MariaDbSchemaInitializer` (and any `Accounting`-module initializer) and confirm the DDL declares `Id BIGINT`, `ReceiptId BIGINT`, `DetectedAt DATETIME(6)`, `CreatedAt DATETIME(6)`, and `VARCHAR(n)` for the bounded strings. Fix the DDL to match if it still uses `TEXT`/`INTEGER`. Note in the summary that an **existing** database needs an `ALTER TABLE` to adopt the corrected types — defer that migration to the schema/troubleshooting session; this plan aligns the source of truth (DDL + EF model).

### APP-1 — login datetime handling (`AuthenticationService`)

Two changes in this file:

1. **Read path (`ReadUserByUsername`, ~lines 211-217):** MySqlConnector returns `DATETIME` columns as `System.DateTime`. Replace the `DateTime.Parse(CStr(rdr(...)))` round-trips with direct casts:

```vb
u.LockedUntil = If(IsDBNull(rdr("LockedUntil")), Nothing, CType(rdr("LockedUntil"), DateTime?))
u.LastPasswordChangeAt = If(IsDBNull(rdr("LastPasswordChangeAt")), Nothing, CType(rdr("LastPasswordChangeAt"), DateTime?))
u.CreatedAt = CType(rdr("CreatedAt"), DateTime)
u.ModifiedAt = If(IsDBNull(rdr("ModifiedAt")), Nothing, CType(rdr("ModifiedAt"), DateTime?))
```

2. **Write path (lines ~183, 227, 239, 245):** these bind `DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")` into `UPDATE` params. Bind the native `DateTime` instead (consistent with INT-19; this file was reserved for INT-21 to avoid two batches editing it):

```vb
cmd.Parameters.AddWithValue("@t", DateTime.UtcNow)
cmd.Parameters.AddWithValue("@now", DateTime.UtcNow)
cmd.Parameters.AddWithValue("@l", DateTime.UtcNow.AddMinutes(15))
```

## Implementation Notes

- Each fix is independent; a failure in one does not block the others.
- POS-8: confirm `OfficialReceipt.VatAmount` is writable and that the assignment is persisted (the receipt is a tracked entity in the same `POSDbContext`). If the inner service already committed, add a `Await _context.SaveChangesAsync()` after the two header assignments.
- Build after each file (`dotnet build MerchSys.slnx`). Per `CLAUDE.md`, document non-trivial build errors in the summary and stop.

## Acceptance Criteria

1. `ReceiptIntegrityService.ValidateChainAsync` uses `YEAR(r.IssueDate)` in raw SQL; no EF `ToListAsync` rewrite was introduced.
2. The `VatSettingsViewModel` TIN regex equals the backend `TinPattern`.
3. `VatAwareReceiptService` sets both `transaction.VatAmount` and `receipt.VatAmount` to `totals.OutputVat`.
4. `TamperAuditEntryConfiguration` has no `HasColumnType("TEXT"|"INTEGER")` overrides; the schema DDL is confirmed/aligned to MariaDB types, with any required `ALTER` noted for the schema session.
5. `AuthenticationService` reads dates via direct cast and binds write-path dates as native `DateTime`.
6. `dotnet build MerchSys.slnx` — target 0 errors, 0 warnings (or documented per protocol).

## Output Requirements

Create a progress report at `Progress/VISTA_Modules/Integration/INT-21-summary.md` using `Progress/_template.md`. Record each fix, the schema-DDL verification result for `Acc_TamperAuditLog`, and whether a follow-up `ALTER` is needed.
