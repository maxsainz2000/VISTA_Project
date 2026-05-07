---
module: Purchasing
audit-date: 2026-05-07
---

# VISTA Module Audit — Purchasing

**Audit Date:** 2026-05-07
**Plans Folder:** `Plans/VISTA_Modules/Purchasing/`
**Progress Folder:** `Progress/VISTA_Modules/Purchasing/`

---

## Mirror Check Summary

| Plan ID | Plan Title | Status |
|---------|-----------|--------|
| PUR-01 | Purchasing Domain Models | ✅ Completed |
| PUR-02 | Purchasing Data Access | ✅ Completed |
| PUR-03 | PO Lifecycle Service | ✅ Completed |
| PUR-04 | Goods Receiving | ✅ Completed |
| PUR-05 | Vendor Directory | ✅ Completed |
| PUR-06 | AP Tracking | ✅ Completed |
| PUR-07 | Reorder Suggestion Engine | ✅ Completed |
| PUR-08 | Price Change Detection | ✅ Completed |
| PUR-09 | View — PO Management | ✅ Completed |
| PUR-10 | View — Goods Receiving | ✅ Completed |
| PUR-11 | View — Vendor Directory | ✅ Completed |
| PUR-12 | View — AP Ledger | ✅ Completed |
| PUR-13 | View — Reorder Suggestions | ✅ Completed |

**Total Plans:** 13
**Completed:** 13 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.
> Items marked *(stale)* refer to plans that have since been completed.

### PUR-02 — Purchasing Data Access

**Status:** Completed

- [ ] PUR-03: Purchasing Services (PO lifecycle, GR recording, AP tracking) *(stale — PUR-03 completed)*

---

### PUR-03 — PO Lifecycle Service

**Status:** Completed

- [ ] PUR-04: Goods Receipt Service (depends on PUR-03; reuses `SequentialNumberGenerator` for GR-YYYY-XXXX) *(stale — PUR-04 completed)*
- [ ] PUR-05: Reorder Engine *(stale — note: PUR-05 is Vendor Directory, not Reorder Engine; PUR-07 is the Reorder Engine; both completed)*

---

### PUR-06 — AP Tracking

**Status:** Completed

- [ ] PUR-07 and beyond (reorder engine, ViewModels) *(stale — PUR-07 through PUR-13 completed)*
- [ ] DI registration of `IAccountsPayableService` / `AccountsPayableService` in `MerchSys.App` (deferred to a consolidation plan) **← ACTIVE**

---

## Notable Deferred Items (unnumbered "What's Next" entries)

These items appeared in What's Next sections without checkboxes but represent real outstanding work:

| Source | Deferred Item |
|--------|--------------|
| PUR-07 | EF Core migration for `Pur_ReorderConfigs` and `Pur_ReorderSuggestions` tables |
| PUR-07 | DI registration of `IReorderService → ReorderService` in `Application.xaml.vb` |
| PUR-08 | EF Core migration for `Pur_PriceChangeAlerts` table |
| PUR-09 | Register `PurchaseOrderListView` + `PurchaseOrderListViewModel` in App DI |
| PUR-09 | Wire PO Management view into MainWindow / navigation shell |
| PUR-10 | Register `GoodsReceivingView` + `GoodsReceivingViewModel` in App DI |
| PUR-10 | Wire `Notification.Wpf` `NotificationManager` for toast feedback (currently uses `StatusMessage` fallback) |
| PUR-11 | Wire `VendorListViewModel` + `VendorDirectoryView` into App DI and navigation shell |
| PUR-12 | Wire `APLedgerView` into MainWindow navigation shell |
| PUR-13 | References "PUR-14 and subsequent Purchasing plans" — **no PUR-14 exists**; this appears to be a stale cross-reference to future infra/app-bootstrap plans |

---

## Plans With No Progress File

None — all 13 plans have a corresponding progress summary.

---

## Amendments & Special Files

| File | Description |
|------|-------------|
| `PUR-03-amendment.md` | Retrofit of `IPurchaseOrderService.CreateDraftAsync` and `UpdateDraftAsync` to accept optional `Notes` and `ExpectedDeliveryDate` parameters. Gap was identified during PUR-09 (PO Management View) where the editor UI collected these fields but had no service path to persist them. Fix used VB.NET `Optional` parameters so all existing call sites compiled without modification. Build: ✅ |

---

## Build Status Summary

All 13 plans report **✅ Solution builds** with 0 errors, 0 warnings.

---

## Summary & Recommendations

- **100% plan coverage** — all 13 Purchasing plans are implemented and marked completed; no missing progress files.
- **All builds green** — no build failures recorded in any summary; the module compiles cleanly.
- **DI wiring is the primary outstanding concern.** Seven views/view-models (PO Management, Goods Receiving, Vendor Directory, AP Ledger, Reorder Suggestions) and two services (`IAccountsPayableService`, `IReorderService`) are not yet registered in `Application.xaml.vb`. This is a systemic gap across all Purchasing views — the App bootstrap / navigation shell plan needs to address this before any UI is reachable at runtime.
- **EF Core migrations are pending** for three tables created in the later plans: `Pur_ReorderConfigs`, `Pur_ReorderSuggestions`, and `Pur_PriceChangeAlerts`. No `dotnet ef migrations add` has been run for these. A migration session is needed before the module can run end-to-end.
- **Stale `[ ]` items in PUR-02 and PUR-03 summaries** reference plans that are now completed. These are benign but could be cleaned up by updating those summaries to checked `[x]` items in a future wiki-sync pass.
