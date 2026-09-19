# UX Implementation Review — Full Scan Report

> **Scope:** All 30 UX implementation summaries (UX-01 → UX-30), their corresponding plans (Plans/VISTA_Modules/Experience/), the actual codebase (WPF_Applications/MerchSys/src/), and affected documentation (codebase_wiki, DI registry, agent wiki).
>
> **Date:** 2026-06-05
>
> **Methodology:** Cross-referenced every implementation summary's "What Was Done" and "What's Next" sections against the plan's deliverables, then verified actual file existence and code adoption via directory listings and grep searches. Documentation drift was checked by comparing the codebase_wiki's layer manifests and DI registry against the actual source tree.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Priority 1 — In-Scope Gaps (Planned but Incomplete)](#priority-1--in-scope-gaps)
3. [Priority 2 — Out-of-Scope Considerations (Not Planned but Needed)](#priority-2--out-of-scope-considerations)
4. [Documentation Drift Inventory](#documentation-drift-inventory)
5. [Verification & Testing Debt](#verification--testing-debt)
6. [Cross-Cutting Consistency Gaps](#cross-cutting-consistency-gaps)
7. [Component Adoption Coverage Matrix](#component-adoption-coverage-matrix)

---

## Executive Summary

| Metric | Count |
|---|---|
| UX plans reviewed | 30 (UX-01 → UX-30) |
| Plans fully delivered (no residual gaps) | 19 |
| Plans with in-scope residual gaps | 11 |
| Out-of-scope items needing consideration | 12 |
| Pending manual verifications (untested) | 6 |
| Stale "What's Next" checkboxes across summaries | 0 remaining (swept 2026-06-05 — see §2.12) |
| Documentation drift entries | 11 genuine (14 inventoried; rows #5, #11, #12 were false positives) |

> [!NOTE]
> There is no `UX-08-summary.md` in `Progress/VISTA_Modules/Experience/`, and this is **correct by design** — not a gap. The plan `08-dashboard-ia-overview.md` is an **index/standards overview** that explicitly states (lines 145–146): *"This overview (UX-08) has no code deliverable and no summary of its own; it is referenced by the five sub-plan summaries."* Its work was delivered and tracked by UX-09 → UX-13. (Corrected 2026-06-05 after the initial scan flagged this as a tracking gap.)

---

## Priority 1 — In-Scope Gaps

These items were explicitly within plan scope but are either partially delivered, have acknowledged "What's Next" items still open, or have code that doesn't fully match the plan's deliverables.

### 1.1 — UX-22: Manual Verification Still Pending

| Plan | UX-22 (Tooltips & Affordance) |
|---|---|
| **Gap:** | Summary explicitly lists manual verification as **Pending** — the only completed UX plan with an unverified manual check. |
| **What's Open:** | `[ ] Manual hover test in both Light and Dark themes` and `[ ] Verify the shared tooltip chrome recolors on theme toggle` are unchecked in the summary's "What's Next". |
| **Impact:** | Tooltip styling may not correctly recolor on theme toggle; broken tooltips would contradict the plan's "done-when" criteria. |
| **Data for Plan:** | Requires a visual realization pass: hover every icon-only control in both themes, verify correct tooltip text, confirm `SurfaceBrush`/`TextPrimaryBrush` swap. |

### 1.2 — UX-23: Window Placement Realization Tests Pending

| Plan | UX-23 (Window-State Persistence) |
|---|---|
| **Gap:** | Five explicit operator realization tests are still unchecked in the summary. |
| **What's Open:** | `[ ] resize/move → close → relaunch`, `[ ] maximize → close → relaunch`, `[ ] simulate off-screen → fallback`, `[ ] delete ui-settings.json → first-run defaults`, `[ ] theme toggle after window-move → both keys survive`. |
| **Impact:** | Window placement restoration is untested in a real multi-monitor/docking scenario. Edge cases (off-screen clamp, settings corruption) are unverified. |
| **Data for Plan:** | Test matrix across the 4 client laptops with different screen configurations. |

### 1.3 — UX-28: FilterSummaryBar Not Adopted on All Filterable Views

| Plan | UX-28 (Search & Filter Maturity) |
|---|---|
| **Gap:** | `FilterSummaryBar` adopted on only **3 views** (StockDashboard, ProductManagement, PurchaseOrderList). Summary's own "What's Next" acknowledges: `[ ] Adopt the FilterSummaryBar on other report lists (e.g. TransactionHistoryView) as a nice-to-have`. |
| **Missing Views:** | `TransactionHistoryView`, `APLedgerView`, `VendorDirectoryView`, `ShrinkageView`, `ExpiryMonitorView`, `TamperAuditReportView`, `CreditManagementView`, `ReorderSuggestionsView` — all have filter bars or filter controls (per UX-11) but no count/chips/clear-all. |
| **Impact:** | Inconsistent filter UX — some views show result counts and filter chips, others don't. Users cannot tell how many items a filter narrowed on the majority of filterable views. |
| **Data for Plan:** | List of 8 views that need `FilterSummaryBar`, the VM properties needed (total count, active chips, clear-all command), and the session-memory pattern already proven in the 3 adopted views. |

### 1.4 — UX-24: FreshnessChip Missing from Some Data Views

| Plan | UX-24 (Data Freshness & Refresh) |
|---|---|
| **Gap:** | `FreshnessChip` is mounted on 8 views per the summary. However, several data-loading views that meet the "data view" criteria are not equipped. |
| **Missing Views:** | `APLedgerView`, `VendorDirectoryView`, `VendorCatalogView`, `ShrinkageView`, `ExpiryMonitorView`, `DailySummaryView`, `CreditManagementView`, `ReorderSuggestionsView`, `GoodsReceivingView`. |
| **Rationale for Gap:** | Plan targeted "8 high-traffic dashboard and list views" — the above are lower-traffic or secondary. The gap is intentional scoping, but the inconsistency means some views offer refresh + staleness indication and some don't. |
| **Data for Plan:** | Per-view assessment of whether `IFreshnessAware` + `FreshnessChip` should be extended. At minimum, `APLedgerView`, `VendorDirectoryView`, and `CreditManagementView` are multi-user concurrency-sensitive and would benefit. |

### 1.5 — UX-26: SkeletonPanel Missing from Some Views with BusyOverlay

| Plan | UX-26 (Skeleton Loaders) |
|---|---|
| **Gap:** | Skeletons integrated on 7 views (4 dashboards + 3 lists per summary). Remaining views still use spinner-only `BusyOverlay` for first-load. |
| **Missing Views:** | `APLedgerView`, `VendorDirectoryView`, `ShrinkageView`, `ExpiryMonitorView`, `DailySummaryView`, `CreditManagementView`, `ReorderSuggestionsView`, `VendorCatalogView`, `GoodsReceivingView`, and all Accounting report views (`IncomeStatement`, `VatReturn`, `VatRelief`, `TamperAudit`, `SalesSummary`). |
| **Impact:** | First-load experience is inconsistent. High-traffic views get content-shaped skeletons; others show a blank panel + spinner. |
| **Data for Plan:** | Views to potentially upgrade, categorized by skeleton `Kind` (Cards vs Rows). Cost-benefit: accounting report views with complex layouts may not benefit from skeleton investment. |

### 1.6 — UX-19: Actionable Empty States on Limited Views

| Plan | UX-19 (Actionable Empty States) |
|---|---|
| **Gap:** | CTA buttons wired on only **4 empty states** (Products, Categories, Vendors, Purchase Orders). Other empty-state panels (StockDashboard, TransactionHistory, Shrinkage, ExpiryMonitor, AP Ledger, Reorder Suggestions) show informational panels with no path forward. |
| **Impact:** | The onboarding win is limited — only 4 of the highest-traffic Manager lists offer "Add your first X" actions. |
| **Data for Plan:** | Inventory of all `EmptyStatePanel` consumers, their current CTA status, and which VM commands could serve as CTAs. |

### 1.7 — UX-29: Undo Coverage is Narrow

| Plan | UX-29 (Notification Actions & Undo) |
|---|---|
| **Gap:** | Undo wired on only **3 delete paths**: vendor delete, vendor catalog entry delete, and product category delete. The plan's vision was "undo for soft-delete/void" — but product soft-delete (`ToggleActiveAsync` in `ProductManagementViewModel`) and PO draft delete don't have undo. |
| **Missing Paths:** | Product deactivation (soft-delete via `IsDeleted`), PO draft deletion, credit account blocking, transaction void. |
| **Impact:** | The "delete with undo" safety net is inconsistent — some destructive actions offer undo, others only offer confirmation. |
| **Data for Plan:** | Map of all `IsDeleted`-based soft-delete paths and void paths, current guard level (confirmation vs undo vs neither), and recommended level. |

### 1.8 — UX-20: Confirmation Dialog Missing on Some Destructive Paths

| Plan | UX-20 (Confirmation Dialogs) |
|---|---|
| **Gap:** | 7 commands routed through `IConfirmationPresenter` per summary. But several destructive paths are not gated: product deactivation (`ToggleActiveAsync`), credit account blocking, shrinkage recording (irreversible inventory reduction). |
| **Impact:** | Inconsistent safety guardrails — some irreversible financial actions have confirmation, others don't. |
| **Data for Plan:** | Complete inventory of write-path commands, their reversibility, financial impact, and current guard level. |

### 1.9 — UX-07: Two Icons Defined but Never Consumed

| Plan | UX-07 (Iconography) |
|---|---|
| **Gap:** | `IconEyeOffGeometry` and `IconChevronLeftGeometry` are defined in `Icons.xaml` but never consumed. Summary acknowledges this as deferred. |
| **What's Next (from summary):** | `[ ] two-state password reveal consuming IconEyeOffGeometry; ActivityRail per-module icon map` — still pending. |
| **Impact:** | Dead resources in the theme dictionary. The two-state password reveal is a noticeable UX gap (user can't tell if password is shown or hidden from the icon state). |
| **Data for Plan:** | The `LoginView` eye buttons need a `DataTrigger` toggling between `IconEyeGeometry` and `IconEyeOffGeometry` based on the reveal state. |

### 1.10 — UX-25: Reduced Motion Fallback Not Fully Tested

| Plan | UX-25 (Motion & Micro-interactions) |
|---|---|
| **Gap:** | Summary says manual verification ✅ but the reduced-motion path checks `SystemParameters.ClientAreaAnimation` — there's no documented test of what happens when animations are disabled at the OS level mid-session (the check runs only at startup per the implementation in `Application_Startup`). |
| **Impact:** | If a user disables OS animations while the app is running, the app won't respond until restart. This is technically correct but may surprise users. |
| **Data for Plan:** | Evaluate whether to add a `SystemParameters.StaticPropertyChanged` listener to react dynamically, or document the "restart required" behavior. |

### 1.11 — UX-08: No Summary File ✅ NOT A GAP (resolved 2026-06-05)

| Plan | UX-08 (Dashboard IA Overview) |
|---|---|
| **Original finding:** | No `UX-08-summary.md` exists, breaking 1:1 plan-to-summary tracking. |
| **Resolution:** | **False positive.** The UX-08 plan is an **index/standards overview**, not an implementable batch. It explicitly declares (lines 145–146): *"This overview (UX-08) has no code deliverable and no summary of its own; it is referenced by the five sub-plan summaries."* Its standards were implemented and tracked by **UX-09 → UX-13**. No summary should exist; creating one would contradict the plan. No action needed. |

### 1.12 — UX-17: SalesCartView Keyboard Coverage Incomplete

| Plan | UX-17 (Keyboard & Focus Foundation) |
|---|---|
| **Gap:** | UX-17 summary shows `SalesCartView.xaml` received `TabIndex` and Enter-to-search, but the deeper keyboard fast path (Ctrl+Q, Ctrl+D, hold/recall, scan-hot focus) was deferred to UX-30. However, UX-30 only covers the POS cart — other UX-17 views (like `DailySummaryView`) have no documented keyboard treatment. |
| **Impact:** | `DailySummaryView` is the only data view with no keyboard navigation entries in any UX summary. |
| **Data for Plan:** | Audit `DailySummaryView.xaml` for tab order and any input fields that need `IsDefault`/`IsCancel`. |

---

## Priority 2 — Out-of-Scope Considerations

These items were not planned in any UX batch but emerged during review as gaps that need consideration for improvement.

### 2.1 — No Unit Tests for Any UX ViewModel Logic

| Category | Testing |
|---|---|
| **Observation:** | Every single UX summary reports `Unit tests pass: N/A`. No unit tests exist for any of the 10 UX-related plans that added or modified ViewModel logic (UX-14, UX-15, UX-18, UX-19, UX-20, UX-21, UX-24, UX-28, UX-29, UX-30). |
| **Impact:** | Filter logic (UX-28 session memory, chip generation), undo time-window logic (UX-29 8-second closure), freshness timestamp management (UX-24), form validation rules (UX-18), and confirmation gating (UX-20) are all untested beyond manual verification. |
| **Data for Plan:** | Priority ranking of which VM logic paths carry the highest regression risk and should get tests first. Focus on: filter session memory, undo idempotency/time-boxing, freshness `LastLoadedAt` stamping, and confirmation gating. |

### 2.2 — `ConfirmationDialog` Window Not Listed in DI Registry

| Category | DI Registry |
|---|---|
| **Observation:** | `ConfirmationDialog` (a WPF `Window`) is instantiated directly by `DefaultConfirmationPresenter` via `New ConfirmationDialog()` rather than resolved from DI. This is technically fine (it's a transient modal window), but it's the only dialog window not listed in the `di-registry.md` Views section. `ShortcutsOverlay` is registered as Singleton, `CommandPalette` is registered as Singleton — but `ConfirmationDialog` follows a different pattern (new per invocation). |
| **Impact:** | Documentation inconsistency. A future developer might try to resolve `ConfirmationDialog` from DI and get confused. |
| **Data for Plan:** | Either document the "direct instantiation" pattern for modal dialogs, or register `ConfirmationDialog` as Transient. |

### 2.3 — Shared Kernel Interface Sprawl from UX Changes

| Category | Architecture |
|---|---|
| **Observation:** | UX plans have added 5 interfaces/models to `SharedKernel/Interfaces/` that are presentation-layer concerns: `IFreshnessAware`, `FilterChipItem`, `NotificationAction`, `ConfirmationRequest`, `IConfirmationPresenter`. SharedKernel is the domain cross-cutting layer — these are UI contracts. |
| **Impact:** | Blurs the boundary between domain and presentation. `FilterChipItem` in particular is a pure UI model (chip display text, removal callback) living in the domain kernel. |
| **Data for Plan:** | Assess whether to introduce a `MerchSys.App.Contracts` or keep them in SharedKernel with a namespace convention. Document the reasoning either way. |

### 2.4 — `DailySummaryView` — Least UX-Treated Data View

| Category | Coverage Gap |
|---|---|
| **Observation:** | `DailySummaryView` has received: ErrorStatePanel (UX-15), formatting sweep (UX-21). It has **not** received: FreshnessChip (UX-24), SkeletonPanel (UX-26), FilterSummaryBar (UX-28), keyboard treatment beyond basic (UX-17), or confirmation/undo (no destructive actions). It's the POS module's only purely read-only summary view. |
| **Impact:** | No functional risk, but it's the only data view without freshness indication in a multi-laptop environment. The user cannot tell how stale the daily summary data is. |
| **Data for Plan:** | Low-effort: add `IFreshnessAware` to `DailySummaryViewModel` + mount `FreshnessChip`. |

### 2.5 — `VendorCatalogView` — Minimal UX Treatment

| Category | Coverage Gap |
|---|---|
| **Observation:** | `VendorCatalogView` has received: ErrorStatePanel (UX-15), formatting sweep (UX-21), confirmation on delete (UX-20), undo on delete (UX-29). It has **not** received: keyboard navigation (UX-17), form field standards (UX-18), FreshnessChip (UX-24), SkeletonPanel (UX-26), FilterSummaryBar (UX-28). |
| **Impact:** | The catalog editor has no `TabIndex` ordering, no `FieldRowStyle`, no keyboard shortcuts. It's a write-heavy view that requires mouse interaction for every operation. |
| **Data for Plan:** | Priority: keyboard navigation and `FieldRowStyle` sweep on catalog editor fields. |

### 2.6 — `GoodsReceivingView` — No Empty State, No Freshness

| Category | Coverage Gap |
|---|---|
| **Observation:** | Per UX-15's own table: `GoodsReceivingView` has `n/a` for EmptyStatePanel ("No EmptyStatePanel for PO list"). It also lacks FreshnessChip and FilterSummaryBar. |
| **Impact:** | When no submitted POs exist, the receiving view shows... nothing? An empty data grid with no explanation is a dead end. |
| **Data for Plan:** | Add an `EmptyStatePanel` to the PO list grid in `GoodsReceivingView` with a message like "No submitted purchase orders awaiting receipt." |

### 2.7 — Codebase Wiki Log Missing UX-19, UX-20 Entries

| Category | Documentation |
|---|---|
| **Observation:** | The [codebase wiki log](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/codebase_wiki/log.md) has entries for UX-17, UX-18, UX-21, UX-22, UX-23, UX-24, UX-25, UX-26, UX-27, UX-28, UX-29, UX-30. **UX-19** and **UX-20** are not listed in the log, even though the codebase wiki content (ui.md, di-registry.md) has been updated to reflect their changes. |
| **Impact:** | Log integrity gap — the log doesn't reflect that UX-19 and UX-20 were synced. |
| **Data for Plan:** | Append entries for UX-19 and UX-20 to the codebase wiki log with the correct dates. |

### 2.8 — `ExpiryMonitorView` — No Keyboard Navigation from UX-17

| Category | Coverage Gap |
|---|---|
| **Observation:** | `ExpiryMonitorView` is not listed in UX-17's "What Was Done" section. It has filter inputs and a threshold setting but no documented `TabIndex`, `PreviewKeyDown`, or mnemonics. |
| **Impact:** | The Expiry Monitor toolbar (threshold days input, filter controls) requires mouse interaction. |
| **Data for Plan:** | Add `TabIndex` sequence and Alt mnemonics to `ExpiryMonitorView` inputs. |

### 2.9 — `ShortcutsOverlay` Not Listed in DI Registry Wiki

| Category | Documentation |
|---|---|
| **Observation:** | `ShortcutsOverlay` and `ShortcutsOverlayViewModel` are registered as Singletons in `Application.xaml.vb` per UX-27 summary, but neither appears in the [di-registry.md](file:///c:/Users/Admin/Documents/VISTA_Project/LLM_Wiki/codebase_wiki/schemas/di-registry.md) Views or Shell Components tables. |
| **Impact:** | DI registry documentation is incomplete for UX-27 deliverables. |
| **Data for Plan:** | Add `ShortcutsOverlay` (Singleton) and `ShortcutsOverlayViewModel` (Singleton) to the DI registry. |

### 2.10 — Agent Wiki Patterns ✅ NOT A GAP (resolved 2026-06-05)

| Category | Documentation |
|---|---|
| **Original finding:** | UX-28/29/30 novel patterns (filter session memory, undo time-window, scan-hot focus) were not logged as agent-wiki patterns. |
| **Resolution:** | **False positive.** All three patterns **exist and are indexed** in `agent_wiki/`: `patterns/wpf-vista-filter-summary.md` (per-user `Shared` session memory + chips + clear-all), `patterns/wpf-vista-notification-undo.md` (time-boxed closures + `hasUndone` idempotency), `patterns/wpf-vista-pos-fast-path.md` (scan-hot focus discipline) — all dated 2026-06-05, all listed in `agent_wiki/index.md` (lines 63–65). The skeleton-first-load discriminator is also documented (`patterns/wpf-vista-skeleton-loaders.md`). No action needed. |

### 2.11 — `ConfirmationDialog` Missing from Shortcuts Overlay

| Category | UX Consistency |
|---|---|
| **Observation:** | UX-27's `ShortcutsOverlay` lists keyboard shortcuts for navigation, command palette, and dialog hotkeys. But the `ConfirmationDialog` (UX-20) has its own keyboard behavior (`IsDefault`, `IsCancel`, typed confirmation match) that is not documented in the shortcuts overlay. |
| **Impact:** | The cheat sheet is incomplete — users won't know that typed-confirmation dialogs require exact text match to enable the confirm button. |
| **Data for Plan:** | Add a "Confirmation Dialogs" group to the shortcuts overlay content explaining the confirm/cancel/type-to-confirm interaction. |

### 2.12 — Stale "What's Next" Checkboxes Across Summaries ✅ RESOLVED (swept 2026-06-05)

| Category | Documentation Hygiene |
|---|---|
| **Resolution:** | Swept 2026-06-05. Genuinely-resolved items marked `[x]` with the resolving plan ID (UX-09/10/11/12/14/15/25 — resolved by later plans or Antigravity wiki sync). Genuinely-open items kept `[ ]` but annotated with their tracking artifact (UX-07/15/22/23 → `Operator/UX-verification-checklist.md`; UX-07/26/28 → plans UX-31/UX-34; UX-28 unit-test item → testing phase). Note: the actual unchecked count was ~22 across 12 summaries, not 12 across 10 — the original scan undercounted. |
| **Observation:** | A systematic grep for `[ ]` (unchecked items) across all 30 UX summaries reveals **12 unchecked items** across 10 summaries. Many of these are resolved (e.g., UX-09's "apply to dashboards" was done by UX-10; UX-14's "UX-15/UX-16" are completed; UX-12/UX-15's "wiki sync" are done per log). However, the checkboxes were never marked `[x]` after completion. |
| **Affected Summaries:** | UX-07, UX-09, UX-10, UX-11, UX-12, UX-14, UX-15, UX-22, UX-23, UX-26, UX-28 |
| **Impact:** | Any audit or scan (like this one) must cross-reference each open checkbox against later summaries and logs to distinguish genuine gaps from stale checkboxes. This adds friction and risk of false positives. |
| **Data for Plan:** | A one-pass sweep to mark resolved items `[x]` and annotate with the resolving plan ID (e.g., `[x] Apply metric hierarchy — resolved by UX-10`). Genuine open items should remain `[ ]`. |

---

## Documentation Drift Inventory

These are specific codebase_wiki entries that are stale or missing based on actual code state.

| # | File | Drift Description | Source UX |
|---|---|---|---|
| 1 | `di-registry.md` | Missing `ShortcutsOverlay` (Singleton) and `ShortcutsOverlayViewModel` (Singleton) | UX-27 |
| 2 | `log.md` | Missing log entries for UX-19 and UX-20 sync | UX-19, UX-20 |
| 3 | `modules/app/ui.md` | `SalesCartView` description updated for UX-30 but does not mention `HoldCartCommand`, `RecallCartCommand`, `SearchAndAddProductCommand`, `TransactionCompleted` event, or `HasHeldCart` property on ViewModel | UX-30 |
| 4 | `modules/app/services.md` | Missing `FreshnessTimer` helper — documented in `ui.md` Helpers section but not in `services.md` | UX-24 |
| 5 | `modules/pos/services.md` | `ICartService` lists `GetCartAsync()` correctly — ✅ no drift | UX-30 |
| 6 | `modules/shared-kernel/` | `FilterChipItem.vb` and `IFreshnessAware.vb` not documented in shared-kernel manifest (if one exists) | UX-24, UX-28 |
| 7 | `di-registry.md` | `NullToVisibilityConverter` (UX-19) not documented anywhere in codebase_wiki | UX-19 |
| 8 | `modules/app/ui.md` | `DailySummaryView` description doesn't mention any UX-17+ treatments | — |
| 9 | `modules/app/ui.md` | `VendorCatalogView` description mentions nothing about UX-20 confirmation or UX-29 undo | UX-20, UX-29 |
| 10 | `modules/app/ui.md` | `GoodsReceivingView` description doesn't mention UX-17 keyboard treatment (`_Confirm Receipt` mnemonic, `TabIndex`) | UX-17 |
| 11 | ~~`Progress/Experience/`~~ | ~~No `UX-08-summary.md` file exists~~ — **not drift**: UX-08 is a summary-less index/standards plan by design (see §1.11) | — |
| 12 | ~~Agent wiki~~ | ~~No documented pattern for filter session memory (UX-28), undo time-window (UX-29), scan-hot focus (UX-30)~~ — **not drift**: all three exist + indexed (`wpf-vista-filter-summary`, `wpf-vista-notification-undo`, `wpf-vista-pos-fast-path`); see §2.10 | UX-28/29/30 |
| 13 | `modules/purchasing/` | `VendorService` and `VendorProductService` `RestoreAsync`/`RestoreCatalogEntryAsync` methods (UX-29) not documented in purchasing services manifest | UX-29 |
| 14 | `modules/inventory/` | `ProductManagementViewModel` category restore (undo) logic from UX-29 not documented in inventory ViewModels | UX-29 |

---

## Verification & Testing Debt

| # | UX | What's Unverified | Risk Level |
|---|---|---|---|
| 1 | UX-22 | Tooltip chrome recoloring on theme toggle | Low |
| 2 | UX-23 | All 5 window placement scenarios (resize/maximize/off-screen/delete/theme) | Medium |
| 3 | UX-07 | Full realization check of all swept icon-only controls in both themes | Low |
| 4 | UX-15 | Force-DB-down error state panel test | Medium |
| 5 | UX-25 | OS-level reduced motion mid-session reaction | Low |
| 6 | All | No automated unit tests for any UX ViewModel logic | High |

---

## Cross-Cutting Consistency Gaps

These are patterns where a UX improvement was applied to *some* views but not all eligible views, creating user-facing inconsistency.

### FreshnessChip Adoption

| View | Has FreshnessChip | Has Data Load | Should Have? |
|---|---|---|---|
| StockDashboardView | ✅ | ✅ | — |
| PurchasingDashboardView | ✅ | ✅ | — |
| OwnerDashboardView | ✅ | ✅ | — |
| FinancialOverviewView | ✅ | ✅ | — |
| SalesSummaryView | ✅ | ✅ | — |
| PurchaseOrderListView | ✅ | ✅ | — |
| ProductManagementView | ✅ | ✅ | — |
| TransactionHistoryView | ✅ | ✅ | — |
| APLedgerView | ❌ | ✅ | **Yes** (concurrency-sensitive) |
| VendorDirectoryView | ❌ | ✅ | **Yes** (concurrency-sensitive) |
| CreditManagementView | ❌ | ✅ | **Yes** (financial data) |
| DailySummaryView | ❌ | ✅ | Consider |
| ExpiryMonitorView | ❌ | ✅ | Consider |
| ShrinkageView | ❌ | ✅ | Consider |
| GoodsReceivingView | ❌ | ✅ | Consider |
| ReorderSuggestionsView | ❌ | ✅ | Consider |
| VendorCatalogView | ❌ | ✅ | Consider |

### FilterSummaryBar Adoption

| Filterable View | Has FilterSummaryBar | Has WrapPanel Filter Bar (UX-11) |
|---|---|---|
| StockDashboardView | ✅ | ✅ |
| ProductManagementView | ✅ | ✅ |
| PurchaseOrderListView | ✅ | ✅ |
| TransactionHistoryView | ❌ | ✅ |
| ShrinkageView | ❌ | ✅ |
| ExpiryMonitorView | ❌ | ✅ |
| TamperAuditReportView | ❌ | ✅ |
| APLedgerView | ❌ | ✅ |
| VendorDirectoryView | ❌ | Has filter controls |
| ReorderSuggestionsView | ❌ | Has filter controls |
| CreditManagementView | ❌ | Has search |

### SkeletonPanel Adoption

| Data View | Has Skeleton | Kind |
|---|---|---|
| StockDashboardView | ✅ | Cards |
| PurchasingDashboardView | ✅ | Cards |
| OwnerDashboardView | ✅ | Cards |
| FinancialOverviewView | ✅ | Cards |
| PurchaseOrderListView | ✅ | Rows |
| ProductManagementView | ✅ | Rows |
| TransactionHistoryView | ✅ | Rows |
| APLedgerView | ❌ | Would be Rows |
| VendorDirectoryView | ❌ | Would be Rows |
| ShrinkageView | ❌ | Would be Rows |
| ExpiryMonitorView | ❌ | Would be Rows |
| CreditManagementView | ❌ | Would be Rows |
| SalesSummaryView | ❌ | Would be Cards |
| IncomeStatementView | ❌ | Would be Cards |

### Keyboard Navigation (UX-17) Coverage

| View | Has TabIndex/PreviewKeyDown | Has Mnemonics |
|---|---|---|
| LoginView | ✅ | ✅ |
| ProductManagementView | ✅ | ✅ |
| ShrinkageView | ✅ | ✅ |
| APLedgerView | ✅ | ✅ |
| PurchaseOrderListView | ✅ | ✅ |
| VendorDirectoryView | ✅ | ✅ |
| ReorderSuggestionsView | ✅ | ✅ |
| GoodsReceivingView | ✅ | ✅ |
| CreditManagementView | ✅ | ✅ |
| TransactionHistoryView | ✅ | ✅ |
| SalesCartView | ✅ | ✅ (UX-30) |
| VatSettingsView | ❌ (UX-18 reformatted but no UX-17 treatment) | ❌ |
| ExpiryMonitorView | ❌ | ❌ |
| DailySummaryView | ❌ | ❌ |
| VendorCatalogView | ❌ | ❌ |

---

## Component Adoption Coverage Matrix

Summary of which shared UX components from the roadmap are adopted across all 24 data views.

| View | ErrorState (UX-15) | FocusVisual (UX-17) | FieldRow (UX-18) | EmptyCTA (UX-19) | Confirm (UX-20) | Formatting (UX-21) | Tooltips (UX-22) | Freshness (UX-24) | Motion (UX-25) | Skeleton (UX-26) | Shortcuts (UX-27) | Filters (UX-28) | Undo (UX-29) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| ProductMgmt | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | — | ✅ | ✅ | ✅ | — | ✅ | ✅ |
| StockDashboard | ✅ | — | — | — | — | ✅ | — | ✅ | ✅ | ✅ | — | ✅ | — |
| PurchaseOrderList | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | — | ✅ | ✅ | ✅ | — | ✅ | — |
| VendorDirectory | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | — | ❌ | ✅ | ❌ | — | ❌ | ✅ |
| APLedger | ✅ | ✅ | ✅ | — | — | ✅ | — | ❌ | ✅ | ❌ | — | ❌ | — |
| TransactionHistory | ✅ | ✅ | — | — | ✅ | ✅ | — | ✅ | ✅ | ✅ | — | ❌ | — |
| SalesCart | ✅ | ✅ | — | — | — | ✅ | ✅ | ❌ | ✅ | ❌ | — | — | — |
| CreditMgmt | ✅ | ✅ | ✅ | — | — | ✅ | — | ❌ | ✅ | ❌ | — | ❌ | — |
| VendorCatalog | ✅ | ❌ | ❌ | — | ✅ | ✅ | — | ❌ | ✅ | ❌ | — | ❌ | ✅ |
| DailySummary | ✅ | ❌ | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | — | — |
| GoodsReceiving | ✅ | ✅ | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | — | — |
| ReorderSuggestions | ✅ | ✅ | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | ❌ | — |
| Shrinkage | ✅ | ✅ | ✅ | — | — | ✅ | — | ❌ | ✅ | ❌ | — | ❌ | — |
| ExpiryMonitor | ✅ | ❌ | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | ❌ | — |
| FinancialOverview | ✅ | — | — | — | — | ✅ | — | ✅ | ✅ | ✅ | — | — | — |
| SalesSummary | ✅ | — | — | — | — | ✅ | — | ✅ | ✅ | ❌ | — | — | — |
| IncomeStatement | ✅ | — | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | — | — |
| VatReturn | ✅ | — | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | — | — |
| VatRelief | ✅ | — | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | — | — |
| TamperAudit | ✅ | — | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | ❌ | — |
| OwnerDashboard | ✅ | — | — | — | — | ✅ | — | ✅ | ✅ | ✅ | — | — | — |
| VatSettings | ✅ | ❌ | ✅ | — | — | ✅ | — | ❌ | ✅ | ❌ | — | — | — |
| PurchasingDashboard | ✅ | — | — | — | — | ✅ | — | ✅ | ✅ | ✅ | — | — | — |
| PriceHistory | ✅ | — | — | — | — | ✅ | — | ❌ | ✅ | ❌ | — | — | — |

> **Legend:** ✅ = adopted, ❌ = eligible but not adopted, — = not applicable (no forms/filters/destructive actions/etc.), `—` in Tooltips column = only icon-only controls get tooltips.

---

> [!NOTE]
> This report is designed to serve as input for generating targeted improvement/optimization plans. Each finding includes specific "Data for Plan" fields that provide the exact scope, affected files, and technical approach needed to draft those plans. No improvement plans are generated in this report.
