---
module: Inventory
audit-date: 2026-05-07
---

# VISTA Module Audit — Inventory

**Audit Date:** 2026-05-07  
**Plans Folder:** `Plans/VISTA_Modules/Inventory/`  
**Progress Folder:** `Progress/VISTA_Modules/Inventory/`

---

## Mirror Check Summary

| Plan ID | Plan Title | Status |
|---------|-----------|--------|
| INV-01 | Inventory Domain Models | ✅ Completed |
| INV-02 | Inventory Data Access | ✅ Completed |
| INV-03 | Stock Management (FIFO) | ✅ Completed |
| INV-04 | Expiry Date Tracking | ✅ Completed |
| INV-05 | Stock Dashboard Service | ✅ Completed |
| INV-06 | Low Stock Alerts | ✅ Completed |
| INV-07 | Shrinkage Recording | ✅ Completed |
| INV-08 | Velocity Classification | ✅ Completed |
| INV-09 | Stockout Estimation | ✅ Completed |
| INV-10 | View — Stock Dashboard | ✅ Completed |
| INV-11 | View — Product Management | ✅ Completed |
| INV-12 | View — Expiry Monitor | ✅ Completed |
| INV-13 | View — Shrinkage | ✅ Completed |

**Total Plans:** 13  
**Completed:** 13 | **In Progress:** 0 | **Blocked:** 0 | **Missing:** 0

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries (unchecked `[ ]` checkboxes only).

### INV-02 — Inventory Data Access

**Status:** Completed

- [ ] INV-03: Inventory Services (FIFO deduction engine, shrinkage recording, alert evaluation)
- [ ] EF Core migration scaffold once all module data-access plans are complete

> Note: The first item (INV-03) is now complete. The EF Core migration scaffold remains pending — no migration plan or progress file exists for this work.

---

### INV-08 — Velocity Classification

**Status:** Completed

- [ ] Register `IVelocityService` → `VelocityService` (Scoped) in the App composition root
- [ ] Add a `StockMovement` log entity to enable precise time-windowed velocity queries (recommended from the plan — current velocity uses lifetime totals, not time-windowed data)

---

## Notable DI Registration Debt (from plain-text "What's Next" sections)

The following DI registrations were flagged in plain-text (not checkbox) "What's Next" items across multiple summaries. They are all deferred to the App composition root / shell integration plan and are not yet tracked in any plan:

| Service | Plan | Note |
|---------|------|------|
| `IExpiryTrackingService` / `ExpiryTrackingService` | INV-04 | Must be Scoped in `MerchSys.App` |
| `IStockDashboardService` / `StockDashboardService` | INV-05 | Must be Scoped in `MerchSys.App` |
| `ILowStockAlertService` / `LowStockAlertService` | INV-06 | Must be Scoped in `MerchSys.App` |
| `ILowStockNotifier` (concrete `Notification.Wpf` impl) | INV-06 | Requires `net10.0-windows` target in App |
| `IShrinkageService` / `ShrinkageService` | INV-07 | Must be Scoped in `MerchSys.App` |
| `IStockoutEstimationService` / `StockoutEstimationService` | INV-09 | Deferred to INFRA-02 pattern |
| `StockDashboardViewModel` + `StockDashboardView` | INV-10 | Deferred to shell navigation plan |
| `ProductManagementViewModel` | INV-11 | Deferred to shell navigation plan |
| `ExpiryMonitorView` + `ExpiryMonitorViewModel` | INV-12 | Deferred to shell/integration plan |
| `ShrinkageView` + `ShrinkageViewModel` | INV-13 | Deferred to shell/integration plan |

Additionally, INV-06 noted that `CheckAndGenerateAlertsAsync` should be called from `SaleCompletedEvent` and `ShrinkageRecordedEvent` handlers — this cross-handler wiring is not yet implemented.

---

## Plans With No Progress File

None. All 13 plans have corresponding progress summaries.

---

## Amendments & Special Files

None. No `*-amendment.md` or non-standard files found in `Progress/VISTA_Modules/Inventory/`.

---

## Summary & Recommendations

- **100% plan coverage** — all 13 Inventory plans have been implemented and marked `completed`. Build status is ✅ across the board (0 errors, 0 warnings on every plan).
- **4 explicit pending `[ ]` tasks** exist across 2 summaries (INV-02 and INV-08). The INV-02 EF Core migration item is a cross-module concern (no dedicated migration plan exists yet). The INV-08 `StockMovement` log entity is a data quality improvement — velocity currently approximates daily sales from lifetime batch totals, which will give imprecise stockout estimates until this is addressed.
- **Major untracked work: DI wiring and shell navigation.** Every Inventory service and ViewModel has been implemented but none are registered in the App composition root or wired into navigation. This block applies to 10+ services/views and is the primary integration risk for the module. A dedicated App-layer plan (or INFRA plan) should capture this.
- **`ILowStockNotifier` concrete implementation is missing.** The `Notification.Wpf` toast is abstracted behind `ILowStockNotifier` in `MerchSys.Inventory` (to avoid WPF target framework issues), but the concrete implementation in `MerchSys.App` has not been written or registered. Low-stock alerts will silently no-op until this is resolved.
- **Top priority next steps:** (1) Create or identify the App-layer DI wiring plan and implement all Inventory service/ViewModel registrations; (2) Implement the `ILowStockNotifier` concrete class in `MerchSys.App`; (3) Wire `CheckAndGenerateAlertsAsync` into `SaleCompletedEvent` / `ShrinkageRecordedEvent` handlers; (4) Schedule the EF Core migration scaffold once all module data-access layers are complete.
