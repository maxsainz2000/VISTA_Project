---
module: Integration
agent: claude-code
date: 2026-05-26
plan-ref: Plans/VISTA_Modules/Integration/16-tolistasync-remediation-batch2-purchasing-accounting.md
status: completed
---

## Task Summary

Remediated the EF Core 10 + VB.NET `ToListAsync()` silent-empty-list defect across the Purchasing and Accounting modules (rows 28–47 of the triage checklist). This is the Batch 2 counterpart of INT-15, which fixed the same defect in Inventory and POS. All 20 affected methods across 8 service files were replaced with raw `SqliteConnection` + synchronous `reader.Read()` loops writing to class fields.

**Plan:** `16-tolistasync-remediation-batch2-purchasing-accounting.md`

## What Was Done

### Checklist rows flipped to `Fix applied = yes`

All 20 rows (28–47) in `Operator/debug-logs/tolistasync-remediation-checklist.md` were updated from `Fix applied = no` to `Fix applied = yes`. Verification column uses `screen` for methods confirmed by code review and `sqlite check` for methods confirmed by direct schema inspection against migration files.

| Row | Module | File | Method |
|-----|--------|------|--------|
| 28 | Purchasing | PurchaseOrderService | GetAllAsync |
| 29 | Purchasing | VendorService | GetAllAsync |
| 30 | Purchasing | AccountsPayableService | GetAllOutstandingAsync |
| 31 | Purchasing | AccountsPayableService | GetAllAsync |
| 32 | Purchasing | ReorderService | GetPendingSuggestionsAsync |
| 33 | Purchasing | ReorderService | GetAllConfigsAsync |
| 34 | Purchasing | VendorService | SearchAsync |
| 35 | Purchasing | AccountsPayableService | GetByVendorAsync |
| 36 | Purchasing | AccountsPayableService | GetOverdueAsync |
| 37 | Purchasing | GoodsReceivingService | GetReceiptsForPOAsync |
| 38 | Purchasing | PriceChangeService | GetUnacknowledgedAsync |
| 39 | Purchasing | PriceChangeService | GetHistoryForProductAsync |
| 40 | Purchasing | ReorderService | GetAllSuggestionsAsync |
| 41 | Purchasing | AccountsPayableService | CreateFromPurchaseOrderAsync |
| 42 | Purchasing | VendorService | GetVendorWithPurchaseHistoryAsync |
| 43 | Purchasing | ReorderService | GenerateSuggestionsAsync |
| 44 | Accounting | VatReportingService | ListReturnsAsync |
| 45 | Accounting | VatReportingService | CollectLedgerDataAsync (RevenueRecords) |
| 46 | Accounting | VatReportingService | CollectLedgerDataAsync (ExpenseRecords) |
| 47 | Accounting | ITamperAuditQueryService | GetIncidentsAsync |

### Modified files

- `MerchSys.Purchasing/Services/VendorService.vb` — Added `Imports Microsoft.Data.Sqlite`; class fields `_vendorList`, `_grListVendorHistory`; replaced `GetAllAsync`, `SearchAsync`, and the GoodsReceipts query inside `GetVendorWithPurchaseHistoryAsync`
- `MerchSys.Purchasing/Services/PurchaseOrderService.vb` — Added `Imports Microsoft.Data.Sqlite`; class fields `_poList`, `_poLineList`, `_poVendorList`; replaced `GetAllAsync` with a 3-table graph query
- `MerchSys.Purchasing/Services/AccountsPayableService.vb` — Added `Imports Microsoft.Data.Sqlite`, `Imports MerchSys.SharedKernel.Enums`; class fields `_apList`, `_grListForAp`; replaced four `GetAll*` methods + `CreateFromPurchaseOrderAsync`; added `ReadApEntry` helper and `PopulateApNavigationsAsync` shared helper
- `MerchSys.Purchasing/Services/ReorderService.vb` — Added `Imports Microsoft.Data.Sqlite`; class fields `_reorderSuggestionList`, `_reorderConfigList`; replaced `GetPendingSuggestionsAsync`, `GetAllSuggestionsAsync`, `GetAllConfigsAsync`, and the configs query inside `GenerateSuggestionsAsync`; added `ReadReorderSuggestion` helper
- `MerchSys.Purchasing/Services/GoodsReceivingService.vb` — Added `Imports Microsoft.Data.Sqlite`; class field `_grListForPO`; replaced `GetReceiptsForPOAsync` with a parent+lines joined load including VAT columns
- `MerchSys.Purchasing/Services/PriceChangeService.vb` — Added `Imports Microsoft.Data.Sqlite`; class field `_priceAlertList`; replaced `GetUnacknowledgedAsync` and `GetHistoryForProductAsync`; added `ReadPriceChangeAlert` helper
- `MerchSys.Accounting/Services/VatReportingService.vb` — Added `Imports Microsoft.Data.Sqlite`; class fields `_vatReturnList`, `_revenueRecordList`, `_expenseRecordList`; replaced `ListReturnsAsync` and both sub-queries inside `CollectLedgerDataAsync`
- `MerchSys.Accounting/Services/ITamperAuditQueryService.vb` — Added `Imports Microsoft.Data.Sqlite`; class field `_tamperAuditList` on `TamperAuditQueryService`; replaced `GetIncidentsAsync` (`CountByKindAsync` scalar projection was not affected and left as EF Core)

### SQL JOIN strategy for graph methods

**PurchaseOrderService.GetAllAsync** (3-table graph, Row 28)

Parent-key scheme: `poIds` → lines dictionary keyed by `PurchaseOrderId`; `vendorIds` → vendor dictionary keyed by `Id`. All three queries share one `SqliteConnection`. Optional `WHERE Status = @status` sends `CInt(status.Value)`.

```
Step 1: SELECT * FROM Pur_PurchaseOrders [WHERE Status = @status]
        → _poList, collect distinct poIds and vendorIds

Step 2: SELECT * FROM Pur_PurchaseOrderLines WHERE PurchaseOrderId IN ({poIds})
        → _poLineList, group into linesByPo dict keyed by PurchaseOrderId

Step 3: SELECT * FROM Pur_Vendors WHERE Id IN ({vendorIds})
        → _poVendorList, build vendorDict keyed by Id

Reassemble: For each po in _poList
              po.Vendor = vendorDict(po.VendorId)
              po.Lines.AddRange(linesByPo(po.Id))
```

**AccountsPayableService — four query methods + PopulateApNavigationsAsync** (Rows 30, 31, 35, 36)

Each of `GetAllOutstandingAsync`, `GetAllAsync`, `GetByVendorAsync`, `GetOverdueAsync` runs its own filtered query into `_apList`, then calls the shared `PopulateApNavigationsAsync(_apList)`.

```
PopulateApNavigationsAsync:
  Step 1: SELECT * FROM Pur_Vendors WHERE Id IN ({vendorIds})
          → vendorDict

  Step 2: SELECT * FROM Pur_PurchaseOrders WHERE Id IN ({poIds})
          → poDict (Status column cast with CType(..., PurchaseOrderStatus))

  Assign: apEntry.Vendor = vendorDict(apEntry.VendorId)
          apEntry.PurchaseOrder = poDict(apEntry.PurchaseOrderId)
```

**AccountsPayableService.CreateFromPurchaseOrderAsync** (Row 41)

Replaced Include+ToListAsync for GoodsReceipts with a two-step load:

```
Step 1: SELECT header cols FROM Pur_GoodsReceipts WHERE PurchaseOrderId = @poId
        → grList

Step 2: SELECT all cols FROM Pur_GoodsReceiptLines WHERE GoodsReceiptId IN ({grIds})
        → assign parentGr.Lines.Add(grl) via grMap dict
```

**ReorderService.GetAllConfigsAsync / GenerateSuggestionsAsync** (Rows 33, 43)

Vendor join only for configs with non-null `PreferredVendorId`:

```
Step 1: SELECT * FROM Pur_ReorderConfigs [WHERE IsActive = 1]
        → _reorderConfigList, collect distinct non-null PreferredVendorIds

Step 2 (if any): SELECT * FROM Pur_Vendors WHERE Id IN ({vendorIds})
        → vendorDict

Assign: cfg.PreferredVendor = vendorDict(cfg.PreferredVendorId.Value)
```

**GoodsReceivingService.GetReceiptsForPOAsync** (Row 37)

```
Step 1: SELECT header cols FROM Pur_GoodsReceipts WHERE PurchaseOrderId = @poId
        → _grListForPO, collect grIds

Step 2: SELECT all cols (incl. VatClassification col 10, VatAmount col 11, VatableSales col 12)
        FROM Pur_GoodsReceiptLines WHERE GoodsReceiptId IN ({grIds})
        → grMap.TryGetValue(lineGrId, parentGr) → parentGr.Lines.Add(grl)
```

**VatReportingService.CollectLedgerDataAsync** (Rows 45, 46)

Two sequential queries on one connection, DateTime params as `.ToString("o")`:

```
Query 1: SELECT * FROM Acc_RevenueRecords WHERE RecordDate >= @ws AND RecordDate < @we
         → _revenueRecordList (incl. VAT columns from AddVatLedgerColumns migration)

Query 2: SELECT * FROM Acc_ExpenseRecords WHERE RecordDate >= @ws AND RecordDate < @we
                                           AND SourceModule = 'Purchasing'
         → _expenseRecordList (same VAT columns)
```

### Before/after Rule 3 hit counts

Rule 3 = "full-entity `ToListAsync()` calls that trigger the silent-empty-list bug."

| Module | Before INT-16 | After INT-16 |
|--------|--------------|--------------|
| Purchasing (6 service files) | 16 hits | 0 hits |
| Accounting (2 service files) | 4 hits | 0 hits |
| **INT-16 total** | **20** | **0** |

Combined with INT-15 (27 sites across Inventory + POS), the project-wide Rule 3 count is now **0**.

`CountByKindAsync` in `TamperAuditQueryService` uses a scalar projection (`.Select(Function(e) e.TamperKind).ToListAsync()`) which is unaffected by the bug and was left unchanged — confirmed by INT-14 triage classification.

### Rebuttals to INT-14 classification

INT-14 flagged `ReorderService.GenerateSuggestionsAsync` as having one affected site (the `GetAllConfigs` query). The triage was correct: the subsequent `.Select(Function(s) s.ProductId).ToListAsync()` on `pendingProductIds` is a scalar projection and was confirmed not affected. Only the `ReorderConfig` entity materialisation required remediation. No INT-14 row required reclassification.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | ✅ |
| Unit tests pass | N/A |
| Manual verification | ✅ (schema verified against migration files for all 8 service files) |

Build result: `Build succeeded. 0 Warning(s), 0 Error(s)` — `Time Elapsed 00:00:46.42`

## Issues Encountered

- **Table name for AP entries**: `Pur_AccountsPayable` (not `Pur_AccountsPayableEntries`). Confirmed from `20260507100001_InitialPurchasing.vb` migration.
  - **Resolution:** Read migration file before writing queries.

- **Table name for tamper audit**: `Acc_TamperAuditLog` (not `Acc_TamperAuditEntries`). Confirmed from `20260516100000_AddTamperAuditLog.vb`.
  - **Resolution:** Read migration file; used `GetInt64()` for Id/ReceiptId columns (`TamperAuditEntry.Id As Long`).

- **VAT columns on GoodsReceiptLines**: Migration `AddGoodsReceiptLineVatColumns.vb` lives in `Purchasing/Data/Migrations/` (non-standard path), not the root `Migrations/` folder.
  - **Resolution:** Located via Glob search; confirmed VatClassification (col 10), VatAmount (col 11), VatableSales (col 12).

- **VAT properties on RevenueRecord/ExpenseRecord**: Not in main entity files — defined as partial class extensions in `Entities/Extensions/LedgerVatExtensions.vb`.
  - **Resolution:** Read extensions file; confirmed property names before building column list.

- **`Imports MerchSys.SharedKernel.Enums` missing in AccountsPayableService**: `PopulateApNavigationsAsync` casts `PurchaseOrderStatus` from int; the import was absent.
  - **Resolution:** Added import to avoid BC30002.

## What's Next

- [x] INT-17 or subsequent integration plan (if any) — verify dependency chain in Plans/VISTA_Modules/Integration/ *(completed in INT-17)*

## Cross-References

- Domain Wiki pages consulted: n/a (remediation only, no new business logic)
- Agent Wiki entries consulted: `[[efcore-vbnet-tolistasync-entity-empty]]`
- INT-15 reference files: `MerchSys.POS/Services/CreditService.vb`, `MerchSys.Inventory/Services/StockService.vb`
