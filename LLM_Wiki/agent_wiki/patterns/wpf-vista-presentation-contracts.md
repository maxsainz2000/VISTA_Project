---
type: pattern
module: Infrastructure
agent: claude-code
date: 2026-06-05
tags: [vb-net, architecture, modular-monolith, sharedkernel, mvvm, presentation-contracts, boundaries]
---

# Presentation Contracts Live in SharedKernel (Not in MerchSys.App)

## Context

The UX epic introduced a small set of **presentation-layer** contracts that ViewModels across the app
depend on:

- `IFreshnessAware` — a VM exposes `LastLoadedAt` + a `RefreshCommand` (UX-24).
- `FilterChipItem` — a removable filter-chip display model (UX-28).
- `NotificationAction` — an optional toast action button, e.g. "Undo" (UX-29).
- `ConfirmationRequest` / `IConfirmationPresenter` — the confirm-dialog request + presenter (UX-20).

They currently live in `MerchSys.SharedKernel/Interfaces/`. Because they describe **UI** concerns (chip
text, toast buttons, dialog requests), it is tempting to conclude they are "misplaced" in the domain
kernel and should move to a presentation assembly (`MerchSys.App` or a new `MerchSys.App.Contracts`).
**Do not move them.** This entry records why, so the question isn't re-litigated each UX sweep.

## The Pattern

**Presentation contracts that are consumed by module-library ViewModels belong in `SharedKernel` —
the one assembly every module is permitted to reference.** Keep them there; make them *legible* with a
namespace/folder convention rather than relocating them.

A grep of the consumer graph (2026-06-05) shows all five contracts are referenced by **all four module
libraries**, not only `MerchSys.App`:

| Contract | Module-library consumers (besides App) |
|---|---|
| `IConfirmationPresenter` / `ConfirmationRequest` | Inventory (`ProductManagementViewModel`); Purchasing (`VendorCatalogViewModel`, `VendorListViewModel`, `PurchaseOrderListViewModel`); POS (`TransactionHistoryViewModel`) |
| `FilterChipItem` | Inventory (`StockDashboardViewModel`, `ProductManagementViewModel`); Purchasing (`PurchaseOrderListViewModel`) |
| `IFreshnessAware` | Inventory; Purchasing (`PurchasingDashboardViewModel`); POS; Accounting (`SalesSummaryViewModel`, `FinancialOverviewViewModel`) |
| `NotificationAction` | Purchasing (`VendorListViewModel`, `VendorCatalogViewModel`); Inventory (`ProductManagementViewModel`) |

## Why It Works (and why moving them fails)

The binding constraint is the modular-monolith boundary rule (CLAUDE.md / `[[modular-monolith]]`):

> Each module references `MerchSys.SharedKernel` **only** — no cross-module project references; no
> module → `MerchSys.App` reference.

Module ViewModels live in the **module libraries** (`MerchSys.Inventory`, `.Purchasing`, `.POS`,
`.Accounting`), and those VMs consume these contracts. Therefore:

- Moving the contracts into `MerchSys.App` would force `Inventory/Purchasing/POS/Accounting →
  MerchSys.App` — a **forbidden** reference direction. ❌
- A new `MerchSys.App.Contracts` library that modules reference also breaks the rule as written (a module
  may reference `SharedKernel` *only*). ❌
- Splitting just `FilterChipItem` out fails the same way (Inventory + Purchasing consume it). ❌

`SharedKernel` is **the shared kernel**, not a pure-domain layer. In a modular monolith the shared kernel
legitimately holds whatever contracts **every** module needs — which, here, includes this small set of
presentation contracts. They are already in the only assembly that can host them. The real problem named
by the review (UX review §2.3 / plan UX-35) is **clarity**, not location: they sit indistinguishably
beside domain events/queries.

**Decision (UX-35, 2026-06-05): Option B — keep in SharedKernel.** Chosen variant **B-minimal**
(document the convention now); the optional tidy-up that groups them under a `SharedKernel/Presentation/`
namespace is **folded into UX-31**, which already edits the same consumer VMs.

## Rules

- **Never relocate a presentation contract that a module-library ViewModel consumes out of
  `SharedKernel`.** It would violate the module-boundary rule. This includes "helper" contracts that
  feel UI-only (`FilterChipItem`).
- **New cross-cutting presentation contracts (consumed by ≥1 module VM) go in `SharedKernel`** — same
  as `IFreshnessAware`, `FilterChipItem`, `NotificationAction`, `ConfirmationRequest`,
  `IConfirmationPresenter`.
- **Distinguish, don't move.** When UX-31 lands, these group under a `SharedKernel/Presentation/`
  namespace/folder to separate them visually from domain events/queries. Until then they remain under
  `SharedKernel/Interfaces/` and that is acceptable.
- **App-only presentation types stay in `MerchSys.App`.** If a type is consumed *only* by `MerchSys.App`
  (no module VM references it), it does **not** belong in SharedKernel — keep it in App.

## Related

- `[[modular-monolith]]` — the boundary rule this enforces.
- `[[mariadb-pure-client-server-architecture]]` — current architecture baseline.
- `[[wpf-vista-filter-summary]]`, `[[wpf-vista-notification-undo]]`, `[[wpf-vista-confirmation-presenter]]`,
  `[[wpf-vista-freshness-chip]]` — the patterns that introduced these contracts.
