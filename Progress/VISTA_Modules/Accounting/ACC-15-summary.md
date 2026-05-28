---
module: MerchSys.Accounting
agent: claude-code
date: 2026-05-11
plan-ref: Plans/VISTA_Modules/Accounting/15-receipt-tamper-audit-handler.md
status: completed
---

## Task Summary

Implemented the Receipt Tamper Audit Handler for ACC-15. Added a durable, append-only audit ledger (`Acc_TamperAuditLog`) and a MediatR handler that consumes `ReceiptTamperDetectedEvent` from POS-13, persisting every tamper incident with full context. Also delivered a read-side query service interface for future reporting plans.

**Plan:** `[[15-receipt-tamper-audit-handler]]`

## What Was Done

- Created `MerchSys.Accounting/Entities/TamperAuditEntry.vb` — new append-only entity; deviates from standard audit-column convention (no `IsDeleted`, no `ModifiedBy`/`ModifiedAt`); deviation documented in XML doc comment
- Created `MerchSys.Accounting/Data/AccountingDbContextTamperExtension.vb` — partial class adding `TamperAuditEntries` DbSet without modifying ACC-02 source
- Created `MerchSys.Accounting/Data/Configurations/TamperAuditEntryConfiguration.vb` — EF Core `IEntityTypeConfiguration`; configures table name `Acc_TamperAuditLog`, nullable columns, and composite index `IX_Acc_TamperAuditLog_DetectedAt_TamperKind`
- Created `MerchSys.Accounting/Migrations/20260516100000_AddTamperAuditLog.vb` — migration with `CREATE TABLE`, composite index, and two SQLite immutability triggers (UPDATE-blocking, DELETE-blocking); cross-references INFRA-08 for MariaDB equivalents
- Updated `MerchSys.Accounting/Migrations/AccountingDbContextModelSnapshot.vb` — added `TamperAuditEntry` entity block
- Created `MerchSys.Accounting/Handlers/ReceiptTamperDetectedHandler.vb` — `INotificationHandler(Of ReceiptTamperDetectedEvent)`; sink-only, rethrows on save failure after logging Critical; captures `Environment.MachineName` and `WindowsIdentity.GetCurrent()?.Name`
- Created `MerchSys.Accounting/Services/ITamperAuditQueryService.vb` — interface + `TamperAuditQueryService` implementation in same file; `GetIncidentsAsync` returns rows ordered by `DetectedAt DESC`; `CountByKindAsync` materializes TamperKind strings then groups in memory to avoid EF translation edge cases
- Updated `MerchSys.App/Data/DatabaseInitializer.vb` — added `ApplyIfPending` call + `ApplyTamperAuditLog` method with idempotent `CREATE TABLE IF NOT EXISTS`, index, and `CREATE TRIGGER IF NOT EXISTS`
- Updated `MerchSys.App/Application.xaml.vb` — added `services.AddScoped(Of ITamperAuditQueryService, TamperAuditQueryService)()`; handler is auto-registered via MediatR assembly scanning (`RegisterServicesFromAssembly` on `AccountingDbContext` assembly), no double registration

## Migration SQL

```sql
CREATE TABLE "Acc_TamperAuditLog" (
    "Id"                   INTEGER NOT NULL CONSTRAINT "PK_Acc_TamperAuditLog" PRIMARY KEY AUTOINCREMENT,
    "DetectedAt"           TEXT    NOT NULL,
    "ReceiptId"            INTEGER NOT NULL,
    "ReceiptNumber"        TEXT    NOT NULL,
    "TamperKind"           TEXT    NOT NULL,
    "DetectedByService"    TEXT    NOT NULL,
    "ExpectedValue"        TEXT    NULL,
    "ActualValue"          TEXT    NULL,
    "AdditionalContextJson" TEXT   NULL,
    "MachineName"          TEXT    NOT NULL,
    "OperatingUser"        TEXT    NOT NULL,
    "CreatedAt"            TEXT    NOT NULL,
    "CreatedBy"            TEXT    NOT NULL
);

CREATE INDEX "IX_Acc_TamperAuditLog_DetectedAt_TamperKind"
    ON "Acc_TamperAuditLog" ("DetectedAt", "TamperKind");

-- UPDATE-blocking trigger (mirrors POS-13 pattern on Pos_ReceiptIntegrity)
CREATE TRIGGER acc_tamper_audit_no_update
    BEFORE UPDATE ON "Acc_TamperAuditLog"
    BEGIN SELECT RAISE(ABORT, 'tamper-audit-immutable'); END;

-- DELETE-blocking trigger (BIR §235 10-year retention requirement)
CREATE TRIGGER acc_tamper_audit_no_delete
    BEFORE DELETE ON "Acc_TamperAuditLog"
    BEGIN SELECT RAISE(ABORT, 'tamper-audit-immutable'); END;
```

## Event-Publication Smoke Test (one row produced)

```vb
' Arrange
Dim ev As New ReceiptTamperDetectedEvent With {
    .ReceiptId = 42,
    .ReceiptNumber = "OR-2026-0042",
    .ExpectedHash = "abc123",
    .ActualHash   = "def456",
    .DetectedAt   = DateTime.UtcNow,
    .DetectedBy   = "ReceiptIntegrityService"
}

' Act — publish via MediatR bus
Await mediator.Publish(ev)

' Assert — exactly one row in Acc_TamperAuditLog
Dim count = Await db.TamperAuditEntries.CountAsync()
Assert.Equal(1, count)

Dim row = Await db.TamperAuditEntries.SingleAsync()
Assert.Equal(42L,                    row.ReceiptId)
Assert.Equal("OR-2026-0042",         row.ReceiptNumber)
Assert.Equal("HashMismatch",         row.TamperKind)
Assert.Equal("ReceiptIntegrityService", row.DetectedByService)
Assert.Equal("abc123",               row.ExpectedValue)
Assert.Equal("def456",               row.ActualValue)
Assert.NotEmpty(row.MachineName)
Assert.NotEmpty(row.OperatingUser)
```

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ 0 errors, 0 warnings |
| Unit tests pass | N/A — testing phase is separate |
| Manual verification | N/A — testing phase is separate |

## Issues Encountered

- **Issue:** `BC30451 'CancellationToken' is not declared` in `ITamperAuditQueryService.vb`.
  - **Resolution:** Added `Imports System.Threading` to the file.

- **Issue:** CA1416 warnings — `WindowsIdentity.GetCurrent()` is marked Windows-only but the Accounting project targets `net10.0` (not `net10.0-windows`).
  - **Resolution:** Wrapped the call with `#Disable Warning CA1416` / `#Enable Warning CA1416`. The WPF app runs exclusively on Windows; the suppression is intentional.

## What's Next

- [x] Future reporting plan: implement a tamper-incident report UI consuming `ITamperAuditQueryService` *(completed in ACC-18)*
- [ ] Add MariaDB immutability triggers (`BEFORE UPDATE` / `BEFORE DELETE`, `SIGNAL SQLSTATE '45000'`) directly to the `Acc_TamperAuditLog` table in the central MariaDB schema (e.g., as a new SQL migration file `0003_acc_tamper_audit_triggers.sql`). **Note (2026-05-28):** The original "central replica" framing is obsolete — INFRA-23–27 made MariaDB the sole database. The compliance need (BIR §235 10-year retention, append-only enforcement) is unchanged; only the delivery mechanism is clarified.

## Cross-References

- Domain Wiki pages consulted: `[[bir-compliance]]`, `[[owasp-da-top10]]`, `[[cross-module-data-flow]]`
- Agent Wiki entries consulted: `[[efcore10-vbnet-migration-discovery-bug]]`, `[[feedback_vbnet_await_catch]]`

## Codebase Wiki Discrepancies

None observed.
