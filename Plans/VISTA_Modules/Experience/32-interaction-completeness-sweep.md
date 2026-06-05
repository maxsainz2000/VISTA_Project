---
module: MerchSys.App
plan-id: UX-32
title: "Interaction Completeness Sweep — Empty-State CTAs & Keyboard Navigation for the Remaining Views"
depends-on: [UX-17, UX-19]
estimated-files: 10
---

# Interaction Completeness Sweep — Empty-State CTAs & Keyboard Navigation for the Remaining Views

> **Completion sweep** of shipped roadmap items **B1 (UX-17)** and **B3 (UX-19)** of
> [ROADMAP-ui-ux-perfection.md](ROADMAP-ui-ux-perfection.md). Closes the coverage gaps recorded in
> `ux_review_report.md` §1.6, §1.12, §2.5, §2.6, §2.8.

## Context

Two correctness-floor items shipped but stopped short of full coverage:

- **Actionable empty states (UX-19)** — CTA buttons were wired on only 4 empty states (Products,
  Categories, Vendors, Purchase Orders). Other lists show an informational panel with no path forward,
  and `GoodsReceivingView` has **no** empty state at all (a blank grid when no submitted POs exist).
- **Keyboard & focus (UX-17)** — tab order, mnemonics, and `IsDefault`/`IsCancel` reached 11 views, but
  `DailySummaryView`, `VendorCatalogView`, `ExpiryMonitorView`, and `VatSettingsView` never received
  keyboard treatment and require the mouse for every operation.

This plan brings those laggard views up to the established floor. Empty-state CTAs are **role-aware**
(write CTAs for Manager only; Owner sees the informational state). Keyboard work is presentation-only.
**No business logic, query, or write-path change.**

## Prerequisites

- **UX-19 (B3)** — `EmptyStatePanel` with the optional CTA command + `NullToVisibilityConverter`, and
  the role-gating pattern used on the 4 wired panels.
- **UX-17 (B1)** — the keyboard/focus conventions: `TabIndex` sequencing, Alt mnemonics, focus visuals,
  `IsDefault`/`IsCancel`, Enter-to-submit, and any `PreviewKeyDown` helpers.
- **UX-15** — three-state load model (so empty vs error vs data is already distinguished).
- Existing deps only. **No new NuGet.**

## Scope

### A. Empty-state CTAs on the remaining lists (review §1.6, §2.6)
Add (or, for GoodsReceiving, introduce) an `EmptyStatePanel` with a role-aware CTA to:
`StockDashboardView`, `TransactionHistoryView`, `ShrinkageView`, `ExpiryMonitorView`, `APLedgerView`,
`ReorderSuggestionsView`, and **`GoodsReceivingView`** (message e.g. "No submitted purchase orders
awaiting receipt." — informational; receiving has no "create" CTA, so this one is a no-CTA empty state
that simply explains the dead end). For views where a sensible Manager action exists (e.g. Shrinkage →
"Record shrinkage", Reorder → the existing reorder action), wire that VM command as the CTA; where no
write action fits, ship the informational empty state without a CTA. Owner always sees the
informational form.

### B. Keyboard navigation for the 4 untreated views (review §1.12, §2.5, §2.8)
- `DailySummaryView` — tab order over its controls; `IsDefault`/`IsCancel` on any input/close.
- `VendorCatalogView` — `TabIndex` ordering across the catalog editor fields + Alt mnemonics
  (write-heavy editor; this is the highest-value keyboard target).
- `ExpiryMonitorView` — `TabIndex` over the threshold-days input + filter controls, Alt mnemonics.
- `VatSettingsView` — `TabIndex` + mnemonics over the settings form (UX-18 reformatted it but it never
  got UX-17 focus treatment).

> **Out of scope:**
> - `VendorCatalogView` **form-field standards (UX-18 `FieldRowStyle`)** — tracked separately; this plan
>   does keyboard order/mnemonics only (flag the `FieldRowStyle` follow-up in the summary).
> - The POS cart deep fast-path (already shipped in UX-30).
> - Any new validation rule, query, or write-path change.

## Specification

### 0. Watch-items

1. **CTA = existing VM command only.** Empty-state CTAs invoke commands that already exist; do not add
   new business operations. If no suitable command exists, ship the informational empty state with no
   CTA rather than inventing one.
2. **Role-aware empty states.** Manager sees the actionable CTA; Owner sees the informational message
   with no write affordance. Gate at the same layer UX-19 used.
3. **Keyboard floor parity.** Match UX-17 exactly: logical `TabIndex` order following reading order,
   Alt mnemonics that don't collide within a view, `IsDefault` on the primary action and `IsCancel` on
   close/cancel, visible focus on every focusable control.
4. **No focus traps / no logic in handlers.** Any `PreviewKeyDown` only redirects focus or invokes an
   existing command — never mutates data inline.
5. **Tokens + theme.** New empty-state copy/buttons use tokens; recolor on toggle. No inline hex.
6. **VB traps.** Full `clr-namespace` root prefix (MC3074); no `Await` in `Catch`/`Finally` (BC36943);
   `<Setter>` targets a `DependencyProperty`; no parameter/property shadowing; reserved-keyword-safe
   names.

### 1. Constraints

- Reuse `EmptyStatePanel` (no parallel panel) and the UX-17 keyboard conventions verbatim.
- Presentation-only; no data/query/business/write change.
- No new NuGet. Tokens + theme; no inline hex.
- **Build gate** — 0 errors, 0 warnings. On failure, document in `Progress/` and stop.
- **Realization check** — for each empty-state view, force the empty condition and confirm the panel
  (and CTA, for Manager) appears and the CTA runs; for each keyboard view, tab through end-to-end with
  no mouse and confirm mnemonics + Enter/Esc behave. Verify in **both** themes and as **both** Manager
  and Owner.

## Acceptance Criteria

1. `dotnet build` succeeds with **0 errors, 0 warnings**.
2. The listed lists show an `EmptyStatePanel`; Manager-actionable ones run an existing VM command as the
   CTA, Owner sees the informational form; `GoodsReceivingView` no longer shows a bare empty grid.
3. `DailySummaryView`, `VendorCatalogView`, `ExpiryMonitorView`, and `VatSettingsView` are fully
   keyboard-navigable (tab order + mnemonics + IsDefault/IsCancel) per the UX-17 floor.
4. No business/query/write change; everything recolors on theme toggle; Owner write affordances absent.

## Output Requirements

### Implementation Summary
`Progress/VISTA_Modules/Experience/UX-32-summary.md` (template `Progress/_template.md`). Include: the
final empty-state coverage table (view → CTA command or "informational, no CTA" + role behavior), the
keyboard-coverage table now reading 15/15 data views, the deferred `VendorCatalogView` `FieldRowStyle`
follow-up, and both-theme/both-role realization results. Note `codebase_wiki` discrepancies for
Antigravity (new `EmptyStatePanel` consumers; `_Confirm Receipt`-style mnemonics; `TabIndex` additions).

### Documentation
No new agent-wiki pattern needed (reuses UX-17/UX-19). Update `agent_wiki/log.md` only on a new trap.
