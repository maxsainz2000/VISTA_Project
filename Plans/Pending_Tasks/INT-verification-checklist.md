---
module: Integration
source: Integration-audit-2026-05-15.md
generated: 2026-05-16
---

# Operator Verification Checklist — Integration

> Extracted from the 2026-05-15 module audit. Only operator/manual verification tasks are included here.
> INT-12 items are tracked separately in `INT-12-checklist.md` — see cross-reference below.

---

## INT-04 — EF Core Migrations & Data Layer Finalization

- [ ] ⏳ **Deferred:** When EF Core fixes VB.NET migration discovery, run `dotnet ef database update` for all 4 modules to validate the manual migration files apply cleanly
  - **Blocked on:** Upstream EF Core VB.NET bug — monitor `agent_wiki/efcore10-vbnet-migration-discovery-bug.md`

---

## INT-10 — Runtime Verification & Smoke Testing

- [ ] Live GoodsReceived chain: create a PO → receive goods → verify `Inv_StockMovements` row appears with `Type=Receipt` via Python query script or DB browser
- [ ] Live SaleCompleted chain: complete a sale → verify `Inv_StockMovements` row appears with `Type=Sale`
- [ ] ⏳ **Ongoing:** Continue monitoring EF Core VB.NET CLI limitation; no agent action required until upstream fix

---

## INT-11 — IEventBus DI Registration Gap

- [ ] Re-test TransactionHistoryView — XAML fix applied (FieldLabel style on Run element), awaiting operator re-test
- [ ] If all 16 views pass, update `Progress/VISTA_Modules/Integration/INT-10-summary.md`: change the interactive-navigation item from `[/]` → `[x]`

---

## INT-13 — VatReturnView Navigation Wire-up

- [ ] Verify ACC-14's final implementation uses **type-based** `NavigationItem` resolution (not string `"VatReturn"` invocation) as required by the INT-13 pattern
  - **How:** Open `FinancialOverviewView.xaml.vb`, inspect the `NavigateToVatReturnRequested` handler — should resolve `NavigationItem` by type, not by string key

---

## Cross-Reference

> **INT-12 tasks** are tracked in the dedicated [INT-12-checklist.md](INT-12-checklist.md) file.
> Items: TransactionHistoryView re-test, GoodsReceived chain (harness), SaleCompleted chain (harness), INT-10 checkbox flip.
