---
module: Infrastructure
agent: claude-code
date: 2026-06-05
plan-ref: Plans/VISTA_Modules/Experience/35-presentation-contract-boundary.md
status: completed
---

## Task Summary

Resolved the SharedKernel presentation-contract boundary question raised in `ux_review_report.md` §2.3.
This is a **decision + documentation** item with **no code change** (the optional relocation is folded
into UX-31). The decision: the five presentation contracts (`IFreshnessAware`, `FilterChipItem`,
`NotificationAction`, `ConfirmationRequest`, `IConfirmationPresenter`) **stay in `SharedKernel`**.

**Plan:** `[[35-presentation-contract-boundary]]`

## What Was Done

- **Assessment (the decision gate):** grepped the consumer graph over `src/`. Result — all five
  contracts are consumed by **all four module libraries** (Inventory, Purchasing, POS, Accounting), not
  only `MerchSys.App`.
- **Decision: Option B (keep in SharedKernel), variant B-minimal (document now).** Options A
  (relocate to `MerchSys.App`) and C (split `FilterChipItem` out) are **rejected** — they would force a
  module → `MerchSys.App` reference, violating the modular-monolith rule ("each module references
  `SharedKernel` only"). `SharedKernel` is the shared kernel, so it legitimately hosts the cross-cutting
  presentation contracts every module's ViewModels need. §2.3 is a *clarity* concern, not a *location*
  defect.
- Created `LLM_Wiki/agent_wiki/patterns/wpf-vista-presentation-contracts.md` — the convention, the
  consumer-graph evidence, the boundary rationale, and the "new presentation contracts go in SharedKernel"
  rule.
- Updated `LLM_Wiki/agent_wiki/index.md` (new row) and `log.md` (new entry).
- Folded the optional **B-tidy** relocation (group the five contracts under a `SharedKernel/Presentation/`
  namespace for legibility) into **UX-31** as Scope item D — UX-31 already edits the same consumer
  ViewModels, so it absorbs the `Imports` change in one pass. Updated UX-31's frontmatter
  (`depends-on` += UX-35; `estimated-files` 14→20), prerequisite note, Scope, watch-items, and
  acceptance criteria accordingly.

## Build & Test Status

| Check | Status |
|---|---|
| Solution builds | N/A (no code change) |
| Unit tests pass | N/A |
| Manual verification | N/A (documentation/decision item) |

## Issues Encountered

- **Issue:** The review (§2.3) framed the contracts as "misplaced UI contracts in the domain kernel."
  - **Resolution:** The framing was partly off — the binding constraint is the module-boundary rule, and
    the contracts are already in the only assembly all modules may reference. Recorded this reasoning in
    the agent-wiki pattern so the location question is not re-litigated each UX sweep.

## What's Next

- [ ] Execute the B-tidy relocation as **UX-31 Scope D** (move to `SharedKernel/Presentation/`, update
      `Imports`, build 0/0). Tracked in UX-31, not here.

## Cross-References

- Domain Wiki pages consulted: `[[modular-monolith]]`, `[[centralized-database-architecture]]`
- Agent Wiki entries created: `[[wpf-vista-presentation-contracts]]`
- Related: `[[wpf-vista-filter-summary]]`, `[[wpf-vista-notification-undo]]`, `[[wpf-vista-confirmation-presenter]]`, `[[wpf-vista-freshness-chip]]`
