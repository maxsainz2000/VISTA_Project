---
module: MerchSys.App
plan-id: UX-35
title: "Presentation-Contract Boundary — Resolve SharedKernel UI-Contract Sprawl"
depends-on: [UX-24, UX-28, UX-29]
estimated-files: 7
---

# Presentation-Contract Boundary — Resolve SharedKernel UI-Contract Sprawl

> Architecture decision + refactor closing `ux_review_report.md` §2.3. **Decision-first:** the
> assessment in §1 is the gate — implement §2 only after the direction is chosen. Sequencing: ideally
> run **before UX-31**, which would otherwise add more consumers of the contracts in question.

## Context

UX plans added five presentation-layer contracts to `SharedKernel/Interfaces/`: `IFreshnessAware`,
`FilterChipItem`, `NotificationAction`, `ConfirmationRequest`, `IConfirmationPresenter`. `SharedKernel`
is the **domain** cross-cutting layer (base types, MediatR events, query contracts); these are **UI**
contracts. `FilterChipItem` especially is a pure presentation model (chip display text + removal
callback) sitting in the domain kernel. This blurs the domain/presentation boundary and, left
unaddressed, every adoption sweep (UX-31) deepens the drift.

This plan **decides** the boundary and applies it consistently. It changes **structure/namespace only**
— no behavior, no MediatR contract, no data change.

## Decision (recorded 2026-06-05 — assessment complete)

**Chosen: Option B — keep the contracts in `SharedKernel`, under an explicit `Presentation`
sub-namespace + a documented convention.** Options A and C are **rejected as architecturally
non-viable.**

**Consumer-graph evidence** (grep over `src/`, 2026-06-05): all five contracts are consumed by
**all four module libraries**, not just `MerchSys.App`:

| Contract | Module-library consumers (besides App) |
|---|---|
| `IConfirmationPresenter` / `ConfirmationRequest` | Inventory (`ProductManagementViewModel`), Purchasing (`VendorCatalogViewModel`, `VendorListViewModel`, `PurchaseOrderListViewModel`), POS (`TransactionHistoryViewModel`) |
| `FilterChipItem` | Inventory (`StockDashboardViewModel`, `ProductManagementViewModel`), Purchasing (`PurchaseOrderListViewModel`) |
| `IFreshnessAware` | Inventory, Purchasing (`PurchasingDashboardViewModel`), POS, Accounting (`SalesSummaryViewModel`, `FinancialOverviewViewModel`) |
| `NotificationAction` | Purchasing (`VendorListViewModel`, `VendorCatalogViewModel`), Inventory (`ProductManagementViewModel`) |

**Why A/C are rejected:** the CLAUDE.md rule is *"each module references `SharedKernel` only — no
cross-module project references."* Moving any of these contracts into `MerchSys.App` (Option A) — or
splitting `FilterChipItem` out (Option C) — would force Inventory/Purchasing/POS/Accounting to reference
`MerchSys.App` (or a new shared lib that is *not* `SharedKernel`), which **violates the module-boundary
rule**. Introducing a new `MerchSys.App.Contracts` library that modules reference fails for the same
reason (the rule permits a module to reference `SharedKernel` *only*).

**Why B is correct, not a compromise:** `SharedKernel` is *the shared kernel*, not a pure-domain layer.
In a modular monolith the shared kernel legitimately holds the contracts **every** module needs —
which, since module ViewModels consume them, includes this small set of presentation contracts. They
are already in the only assembly that can host them. The genuine issue §2.3 names is **clarity**, not
location: they sit indistinguishably next to domain events/queries. The fix is a visible separation
(`SharedKernel/Presentation/` namespace) + a documented "where presentation contracts live" convention,
with **zero** reference-graph change and **zero** risk of a boundary violation.

**Implementation shape (B-tidy):** relocate the five files from `SharedKernel/Interfaces/` to
`SharedKernel/Presentation/`, change `Namespace Interfaces` → `Namespace Presentation`, add
`Imports MerchSys.SharedKernel.Presentation` to the ~15 consumers (and update `INotificationService`,
which references `NotificationAction`), then build 0/0. A **B-minimal** variant (leave files in place,
document the convention only — zero code change) is available if a relocation is judged not worth the
import churn; the §2.3 concern is mild enough that B-minimal is defensible.

### Outcome (2026-06-05) — B-minimal shipped; code relocation folded into UX-31

**Chosen variant: B-minimal.** The document-only deliverable is **done**:
`agent_wiki/patterns/wpf-vista-presentation-contracts.md` records the convention ("presentation
contracts consumed by module VMs live in `SharedKernel`, never `MerchSys.App`"), with the consumer
graph and the boundary rationale; `agent_wiki/index.md` + `log.md` updated. **No code change** was made
and none is needed for correctness — the contracts are already in the only assembly that can host them.

The optional **B-tidy** relocation (group the five contracts under a `SharedKernel/Presentation/`
namespace for visual separation from domain events/queries) is **folded into UX-31**, which already
edits the same consumer ViewModels — doing it there avoids a separate ~15-file `Imports` pass. UX-31's
prerequisite note carries this. **This plan (UX-35) is therefore complete as a stand-alone decision +
documentation item; no separate `UX-35-summary.md` code summary is required** (mirror it in the UX-31
summary instead, or write a short note-only UX-35 summary recording the decision).

> **Why this is real but contained:** the architecture rule in CLAUDE.md is "each module references
> `SharedKernel` only; cross-module comms via MediatR." These five types are consumed by
> `MerchSys.App` (and VMs in module libraries). Moving them must not create a new cross-module
> reference that violates that rule — which is exactly why the §1 assessment must check the consumer
> graph before choosing.

## Prerequisites

- The five contracts as shipped by UX-24 (`IFreshnessAware`), UX-28 (`FilterChipItem`), UX-29
  (`NotificationAction`), UX-20 (`ConfirmationRequest`, `IConfirmationPresenter`).
- A read of the architecture rules in `CLAUDE.md` and
  `LLM_Wiki/wiki/concepts/modular-monolith.md` (read-only).
- Existing deps only. **No new NuGet.**

## Scope

### 1. Assessment (deliverable — the decision gate)
Map each of the five contracts to its **consumers** (which projects/VMs reference it). Then choose one
of three documented options:

- **Option A — `MerchSys.App.Contracts`** (or a `Presentation/` namespace inside `MerchSys.App`): move
  the UI contracts out of `SharedKernel`. **Only viable if** no module library needs to reference them
  (i.e., the consuming VMs that live in module libraries don't depend on these types), otherwise this
  creates a forbidden module → App reference. Verify the consumer graph first.
- **Option B — Keep in SharedKernel under an explicit namespace convention** (e.g.
  `SharedKernel/Presentation/`), documenting that SharedKernel intentionally also hosts a small set of
  cross-cutting **presentation** contracts. Lowest risk; zero reference-graph change.
- **Option C — Split:** truly domain-agnostic contracts (`IConfirmationPresenter`, `NotificationAction`)
  stay; pure UI models (`FilterChipItem`) relocate. Only if the graph allows.

The assessment records the consumer graph and the chosen option **with the reasoning**, per the review's
"document the reasoning either way".

### 2. Apply the chosen boundary
Implement the decision: relocate/rename namespaces as chosen, update all consumers and `Imports`, keep
the build green. If Option B, the "refactor" is mostly the namespace/folder move + a documented
convention; if Option A/C, move the types and re-point references **without** introducing any
module-library → `MerchSys.App` dependency.

> **Out of scope:**
> - Any change to MediatR event/query contracts in `SharedKernel` (those are domain and stay).
> - Behavioral change to freshness, filters, undo, or confirmation.
> - Re-opening the module-boundary rule itself.

## Specification

### 0. Watch-items

1. **Structure-only.** Namespaces/folders/`Imports` change; runtime behavior is identical. No new
   public behavior, no contract shape change.
2. **Module-boundary rule is sacred.** The refactor must not add a `MerchSys.<Module>` → `MerchSys.App`
   reference, and must not add a cross-module reference. If Option A/C would require one, fall back to
   Option B and record why.
3. **Namespace doubling trap.** New `Namespace` statements use only the relative suffix (never repeat
   the `<RootNamespace>` prefix) — BC30002.
4. **XAML root prefix.** Any moved type referenced from XAML needs the full `clr-namespace:MerchSys.…`
   prefix updated (MC3074).
5. **One atomic move per type.** Keep edits mechanical and reviewable; build after each contract moves
   rather than all at once.
6. **VB traps.** Reserved-keyword-safe names; `.` continuation at line-end; no parameter/property
   shadowing.

### 1. Constraints

- Structure/namespace only; no behavior, data, or MediatR-contract change.
- Must preserve the module-monolith reference rules (no new cross-module / module→App refs).
- No new NuGet.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — boot the app; freshness chips, filter chips, undo toasts, and confirmation
  dialogs all still work (the moved contracts resolve at runtime) in **both** themes.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. The §1 assessment (consumer graph + chosen option + reasoning) is recorded in the summary.
3. The chosen boundary is applied consistently across all five contracts and their consumers, with **no**
   new cross-module or module→App project reference introduced.
4. Runtime behavior is unchanged — freshness/filters/undo/confirmation all function as before.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-35-summary.md` (template `Progress/_template.md`). Include: the
consumer-graph table for the five contracts, the chosen option and **why** (incl. why the rejected
options were rejected), the final namespace/location of each contract, confirmation no module-boundary
rule was violated, and both-theme realization. Note `codebase_wiki` discrepancies (relocated contracts;
updated `shared-kernel`/`app` manifests) for Antigravity.

### Documentation
Add `patterns/wpf-vista-presentation-contracts.md` per `workflow-agent-wiki-update.md` documenting where
presentation contracts live and the rule for adding new ones (so future UX plans don't re-introduce the
drift). Update `agent_wiki/index.md` + `log.md`.
