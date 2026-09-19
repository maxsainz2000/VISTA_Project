---
module: MerchSys.Integration
agent: antigravity
date: 2026-06-11
plan-ref: Plans/VISTA_Modules/Integration/19-native-datetime-sql-parameters.md
status: completed
---

## Task Summary

Replaced string-formatted date representations bound to ADO.NET SQL command parameters with native `System.DateTime` values across the database-access services of the monolith. This resolves the potential issue where MariaDB rejects or mismatches the ISO-8601 formatting (with `T`, `Z`, and nanoseconds) produced by `DateTime.ToString("o")`.

**Plan:** `[[19-native-datetime-sql-parameters.md]]`
**Branch:** N/A (Main Workspace)

## What Was Done

Modified 12 primary service implementation files to bind native `System.DateTime` values instead of formatted strings:

1. **`MerchSys.Accounting/Services/VatReportingService.vb`**
   - Converted `@ws` and `@we` parameters in `CollectLedgerDataAsync` to bind native `DateTime` values (`windowStart` and `windowEnd`) directly. Removed the unused `wsStr` and `weStr` string locals.
2. **`MerchSys.Accounting/Services/VatReliefReportService.vb`**
   - Converted `@ws` and `@we` parameters in `BuildSummaryAsync` to bind native `DateTime` values (`windowStart` and `windowEnd`) directly. Removed the unused `wsStr` and `weStr` string locals.
3. **`MerchSys.Accounting/Services/ITamperAuditQueryService.vb`**
   - Converted `@fromUtc` and `@toUtc` parameters in `GetIncidentsAsync` to bind native `DateTime` values (`fromUtc` and `toUtc`).
4. **`MerchSys.Inventory/Services/ExpiryTrackingService.vb`**
   - Converted `@today` and `@threshold` parameters in `GetNearExpiryBatchesAsync` and `@today` in `GetExpiredBatchesAsync` to bind native `DateTime` values (`today` and `thresholdDate`).
5. **`MerchSys.Inventory/Services/ShrinkageService.vb`**
   - Converted `@fromUtc`, `@toUtc`, and `@cursorDate` parameters in `GetShrinkageHistoryPageAsync` to bind native `DateTime` values (`request.FromUtc.Value`, `request.ToUtc.Value`, and `request.CursorDate.Value`).
6. **`MerchSys.Inventory/Services/InventoryAuditService.vb`**
   - Converted `@startDate` and `@endDate` parameters in `GetAuditHistoryAsync` to bind native `DateTime` values (`startDate.Value` and `endDate.Value`).
7. **`MerchSys.Inventory/Services/VelocityService.vb`**
   - Converted `@windowStart` parameter in `GetVelocityHistoryAsync` (or the equivalent velocity analysis query) to bind `windowStart` directly without string formatting.
8. **`MerchSys.POS/Services/CartService.vb`**
   - Converted `@startDate` and `@endDate` in `GetTransactionHistoryAsync`, and `@fromUtc`, `@toUtc`, and `@cursorDate` in `GetTransactionHistoryPageAsync` to bind native `DateTime` values.
9. **`MerchSys.POS/Services/DailySummaryService.vb`**
   - Converted `@start` and `@end` parameters in `BuildDailySummaryAsync` (for both sales and returns queries) and in `BuildPeriodSummaryAsync` (for both sales and returns queries) to bind native `DateTime` values.
10. **`MerchSys.POS/Services/CreditService.vb`**
    - Converted `@cutoff` parameter in `GetOverdueAccountsAsync` to bind the native `DateTime` value `cutoff` directly.
11. **`MerchSys.POS/Services/SalesReturnService.vb`**
    - Converted `@startDate` and `@endOfDay` parameters in `GetReturnHistoryAsync` to bind native `DateTime` values.
12. **`MerchSys.Purchasing/Services/AccountsPayableService.vb`**
    - Converted `@today` parameter in `GetOverdueAsync` to bind the native `DateTime` value `DateTime.UtcNow.Date` directly.

### Verification of Out-of-Scope Sites

The following out-of-scope sites were verified and left intact:
- **`ReceiptIntegrityService.vb:303`** — `receipt.IssueDate.ToString("o")` is used as part of the canonical hash payload. Left intact.
- **Display/Report Formatting** — `.ToString("yyyy-MM-dd HH:mm:ss")` or `.ToString("o")` used for UI/PDF exports in `TamperReportExporter.vb` and `VatReturnExporter.vb` were left intact.
- **`MariaDbSchemaInitializer.vb:208/269`** — DDL/DML string generation remains untouched.
- **`IAuthenticationService.vb:183/227/239/245`** — Left intact (will be addressed in **INT-21**).

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ (0 errors, 0 warnings) |
| Unit tests pass | N/A |
| Manual verification | ✅ (Grep audits confirm correct parameters and out-of-scope exemptions) |

## Issues Encountered

None. The pure parameter substitutions compile cleanly without any library conflicts or compiler errors.

## What's Next

- Implement the next integration batches in sequence (starting with the dependent integrations / remediation plans like `INT-20` and `INT-21`).

## Cross-References

- Domain Wiki pages consulted: `LLM_Wiki/wiki/concepts/centralized-database-architecture.md`
- Agent Wiki entries consulted: `LLM_Wiki/agent_wiki/errors/efcore-vbnet-tolistasync-entity-empty.md`, `CLAUDE.md`
