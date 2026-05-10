---
module: MerchSys.POS
plan-id: POS-13
title: "BIR Tamper-Proof Receipt Retention & Sequence"
depends-on: [POS-01, POS-02, POS-06, INFRA-07]
estimated-files: 6
---

# BIR Tamper-Proof Receipt Retention & Sequence

## Context

The Feature Gap Audit (2026-05-10) found that while POS-06 generates `OR-YYYY-XXXX` receipt numbers, the schema-level controls required by NIRC §113 and §235 (10-year preservation, tamper-proof storage) are absent. This plan adds those controls **without modifying** the entity files defined in POS-01 — instead it introduces a sidecar integrity entity, a dedicated sequence table with row-locked generation, an `EF SaveChangesInterceptor` that rejects mutation of persisted receipts, and a long-term archive table. Tamper detection raises `ReceiptTamperDetectedEvent` (defined in INFRA-07) for accounting audit.

## Prerequisites

- **POS-01** (POS Domain Models) — `OfficialReceipt` exists
- **POS-02** (POS Data Access) — `POSDbContext` exists
- **POS-06** (Receipt Generation) — `ReceiptService` produces `OfficialReceipt` rows
- **INFRA-07** (VAT Event Payload) — `ReceiptTamperDetectedEvent` defined

## Wiki References

- `concepts/bir-compliance.md` — NIRC §113 (issuance), §235 (10-year preservation)
- `concepts/owasp-da-top10.md` — structured audit logging on financial state changes

## Deliverables

```
MerchSys.POS/Entities/
├── ReceiptIntegrity.vb
├── ReceiptSequence.vb
└── OfficialReceiptArchive.vb

MerchSys.POS/Services/
├── IReceiptIntegrityService.vb
└── ReceiptIntegrityService.vb

MerchSys.POS/Data/Interceptors/
└── ImmutableReceiptInterceptor.vb
```

## Specification

### ReceiptIntegrity (`Pos_ReceiptIntegrity`)
Sidecar entity, one-to-one with `OfficialReceipt.Id`.
```
Inherits AuditableEntity

Property ReceiptId As Integer                 ' FK + unique
Property IntegrityHash As String              ' SHA-256, 64-char hex
Property PreviousHash As String               ' Hash of previous ReceiptId (chain). Empty for first.
Property RetentionExpiresAt As DateTime       ' IssueDate + 10 years
Property IsImmutable As Boolean               ' Always True; defense-in-depth flag
Property HashAlgorithm As String              ' "SHA-256-v1"
Property CanonicalPayload As String           ' JSON snapshot used to compute hash

' Navigation
Property Receipt As OfficialReceipt
```

Index: `(ReceiptId)` unique, `(RetentionExpiresAt)`.

### ReceiptSequence (`Pos_ReceiptSequence`)
```
Inherits AuditableEntity

Property Year As Integer                      ' Sequence resets per year
Property NextValue As Integer                 ' Next OR number to issue
Property RowVersion As Byte()                 ' Concurrency token
```

Unique index on `(Year)`. One row per year; populated lazily by the generator.

### OfficialReceiptArchive (`Pos_OfficialReceiptArchive`)
Cold-storage table for receipts past `RetentionExpiresAt + ArchivePolicy:GraceDays`. Schema mirrors `OfficialReceipt` exactly plus `ArchivedAt`, `ArchivedHash`. Receipts are **copied** into archive, not deleted from the main table — archival is for partitioning, not removal.

### IReceiptIntegrityService
```
Public Interface IReceiptIntegrityService
    Function ComputeAndPersistAsync(receipt As OfficialReceipt) As Task(Of ReceiptIntegrity)
    Function ValidateAsync(receiptId As Integer) As Task(Of IntegrityValidationResult)
    Function ValidateChainAsync(year As Integer) As Task(Of ChainValidationResult)
    Function GetNextReceiptNumberAsync(year As Integer) As Task(Of String)
End Interface

Public Class IntegrityValidationResult
    Public Property IsValid As Boolean
    Public Property ReceiptId As Integer
    Public Property ExpectedHash As String
    Public Property ActualHash As String
End Class
```

#### Hash computation
```
canonical = JSON({
  ReceiptNumber, IssueDate (UTC ISO-8601), TransactionId,
  BusinessTIN, TotalAmount, VatAmount,
  Items (sorted by ProductId): [{ProductId, Qty, UnitPrice, LineTotal}]
})
hash = SHA256(canonical || PreviousHash)
```

Sorting is mandatory — JSON object key order must be deterministic.

#### Sequence generation (`GetNextReceiptNumberAsync`)
```
1. Open transaction at Serializable isolation
2. Read or insert Pos_ReceiptSequence row for current year
3. Increment NextValue
4. Update with RowVersion check; on concurrency exception retry up to 10 times
5. Commit
6. Format "OR-{Year:D4}-{NextValue:D4}"
```

No in-memory counter, no `Max(ReceiptNumber) + 1` patterns.

#### Tamper detection
On every `ValidateAsync` mismatch: publish `ReceiptTamperDetectedEvent` via `IMediator`, log structured audit entry, mark `ReceiptIntegrity.IsImmutable = True` (already true; serves as detection breadcrumb in journal). Do **not** auto-correct.

### ImmutableReceiptInterceptor
```
Public Class ImmutableReceiptInterceptor
    Inherits SaveChangesInterceptor
End Class
```

Override `SavingChanges` / `SavingChangesAsync`:
1. Iterate `ChangeTracker.Entries(Of OfficialReceipt)()` and `Entries(Of ReceiptIntegrity)()`
2. If `EntityState = Modified` or `Deleted`: throw `ImmutableEntityException`
3. Allow `Added` and `Unchanged`

Register in `POSDbContext.OnConfiguring`:
```
optionsBuilder.AddInterceptors(New ImmutableReceiptInterceptor())
```

`ImmutableEntityException` lives in `SharedKernel/Exceptions/` (create if absent). Message includes entity type, primary key, and the BIR rule reference.

### EF migration `AddBirRetentionConstraints`
- Creates `Pos_ReceiptIntegrity`, `Pos_ReceiptSequence`, `Pos_OfficialReceiptArchive`
- Drops shadow `IsDeleted` column from `Pos_OfficialReceipts` (overrides `SoftDeletableEntity` for this entity only — implemented via `modelBuilder.Entity(Of OfficialReceipt)().Property(Of Boolean)("IsDeleted").HasColumnType("...").Metadata.SetIsShadowProperty(False)` followed by ignore, then migration removes the column)
- Adds SQLite triggers `pos_receipts_no_update` and `pos_receipts_no_delete` that `RAISE(ABORT, 'BIR-immutable')` (defense-in-depth alongside the interceptor)
- For MariaDB the equivalent triggers live in INFRA-06's `mariadb-init.sql`

### appsettings.json additions
```
"Bir": {
  "RetentionYears": 10,
  "HashAlgorithm": "SHA-256-v1",
  "ArchivePolicy": {
    "GraceDays": 90
  }
}
```

## Implementation Notes

- The hash chain is **per-year** (resets with the sequence). First receipt of the year uses `PreviousHash = ""`.
- Sequence row uses an explicit transaction at Serializable level — SQLite serializes writes anyway, but the explicit isolation matches MariaDB behavior when sync writes through.
- Removing `IsDeleted` from receipts is intentional: BIR receipts cannot be soft-deleted, even by the system itself.
- Concurrent sequence access is exercised by 1000-thread insert test mentioned in acceptance criteria — write a short console harness in `MerchSys.POS/Tests/` even though no test project exists yet (it can live as `Pos.SequenceConcurrencyHarness.vb` excluded from production build).
- Do not modify POS-01 or POS-02 source files; all schema changes ship via the new migration.

## Acceptance Criteria

1. `dotnet build` succeeds with 0 errors, 0 warnings
2. EF migration `AddBirRetentionConstraints` applies cleanly to a fresh and existing SQLite database
3. UPDATE of any `OfficialReceipt` field after persistence raises `ImmutableEntityException` at `SaveChangesAsync` time
4. DELETE of any `OfficialReceipt` likewise raises
5. SQLite triggers `pos_receipts_no_update` / `pos_receipts_no_delete` fire even when bypassing EF
6. Concurrency harness (1000 parallel `GetNextReceiptNumberAsync` calls) produces 1000 unique sequential numbers, no gaps
7. `ValidateChainAsync(2026)` returns `IsValid = True` for an untampered set; flipping any byte in a `OfficialReceipt.TotalAmount` makes it return False and publishes `ReceiptTamperDetectedEvent`
8. `RetentionExpiresAt = IssueDate + 10 years` for every persisted `ReceiptIntegrity` row
9. Receipt number format strictly `OR-YYYY-XXXX` (4-digit zero-padded)

## Output Requirements

### Implementation Summary
Create at: `Progress/VISTA_Modules/POS/POS-13-summary.md` using `Progress/_template.md`.

### Documentation
- XML doc comments on `IReceiptIntegrityService`, `ImmutableReceiptInterceptor`, `ReceiptIntegrity`, `ReceiptSequence` citing NIRC §113 / §235
- Header comment in the migration file linking to `concepts/bir-compliance.md`
