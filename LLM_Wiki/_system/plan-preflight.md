# VISTA Plan Preflight Check

You are running a pre-generation verification for proposed VISTA plans. The audit report(s) to verify against are in: `Pending_Tasks/`

> **PURPOSE:** This workflow prevents duplicate plan creation by cross-referencing every proposed plan item against existing Progress summaries and Plans before any new plan file is written.

---

## When to Run

Run this workflow **after** audit reports are generated (via `/vista-audit`) and **before** creating any new plan files in `Plans/VISTA_Modules/`. If someone asks you to "update Plans from Pending_Tasks" or "create plans from audit results", run this first.

---

## Step 1 — Collect Proposed Work Items

Read all audit reports in `Pending_Tasks/*-audit-*.md`. For each `- [ ]` item in the `## Pending Tasks` sections, extract:

| Field | Source |
|---|---|
| **Item description** | The `- [ ]` text |
| **Source summary** | Which `<PREFIX>-<NN>-summary.md` it came from |
| **Module** | The module the audit covers |
| **Category** | Code change / Operator verification / Deferred |

Build a flat list of all proposed items across all audit reports.

---

## Step 2 — Verify Each Item Against Progress

For **every** proposed code-change item, check whether the work is already done:

### 2a — Check same-module Progress summaries

Read the `## What Was Done` section of **every** progress summary in the item's module (`Progress/VISTA_Modules/<module>/`). Look for evidence that:
- The exact service/entity/file mentioned in the item was already created or modified
- The DI registration, handler, or wiring described is already in place
- A later plan explicitly scoped and completed this work

### 2b — Check cross-module Progress summaries

If the item references a cross-module concern (e.g., "register in MerchSys.App DI", "Accounting handler for POS event"), also check:
- `Progress/VISTA_Modules/Integration/` — integration plans often wire cross-module concerns
- The target module's progress summaries (e.g., ACC-* for accounting handlers)

### 2c — Check existing Plans

Search `Plans/VISTA_Modules/` for any existing plan that covers the same work. An item is a potential duplicate if:
- An existing plan's `## Specification` section describes the same file modifications
- An existing plan's `## Deliverables` section lists the same output files
- An existing plan's `title` or `plan-id` matches the proposed work

### 2d — Verdict

For each proposed item, assign one of:

| Verdict | Meaning | Action |
|---|---|---|
| ✅ **Already Done** | A Progress summary confirms this work was completed | Do NOT create a plan. Mark item as `*(already completed in <PLAN-ID>)*` in the audit report. |
| ⚠️ **Partially Done** | Some aspects completed, others remain | Create a plan only for the remaining gap. Note what's already done in the plan's Context section. |
| 🔄 **Already Planned** | A plan file exists in `Plans/VISTA_Modules/` but has no matching Progress summary | Do NOT create a duplicate plan. Reference the existing plan. |
| ✅ **Clear** | No evidence of prior completion or existing plan | Safe to create a new plan. |

---

## Step 3 — Duplicate Detection Across Proposed Items

Check for duplicates **within** the proposed set itself:

1. Group items by the files they would modify (e.g., two items both targeting `ReceiptService.vb`).
2. If two items from different audit reports describe the same work, merge them into one plan.
3. If an item from Module A's audit describes work that belongs in Module B, assign it to Module B's plan.

---

## Step 4 — Generate Preflight Report

Before creating any plan files, output a preflight report:

```markdown
# Plan Preflight Report — <YYYY-MM-DD>

## Proposed Plans

| # | Module | Proposed Title | Verdict | Evidence |
|---|--------|---------------|---------|----------|
| 1 | POS | Receipt Integrity DI Wiring | ✅ Already Done | POS-14 registered DI, POS-15 replaced pattern |
| 2 | ACC | VatPayableTile Placement | ✅ Clear | No prior plan or progress covers this |
| 3 | INFRA | SyncOrchestrator Transmission | ⚠️ Partially Done | INFRA-06 replaced stub partially, real transmission still needed |

## Items Removed (Already Done)

| Item | Originally From | Completed By |
|------|----------------|-------------|
| Register IReceiptIntegrityService in DI | POS-13 What's Next | POS-14 |
| Replace Max+1 pattern in ReceiptService | POS-13 What's Next | POS-15 |

## Items Merged

| Merged Into | Original Items |
|-------------|---------------|
| ACC-16 | ACC-12 tile placement + ACC-14 navigation wiring |

## Final Plan List (Safe to Create)

| Plan ID | Module | Title | Priority |
|---------|--------|-------|----------|
| ACC-16 | Accounting | VatPayableTile Placement | 🟡 Medium |
| INFRA-12 | Infrastructure | SyncOrchestrator Transmission | 🟡 Medium |
| ... | ... | ... | ... |
```

---

## Step 5 — Gate

**Do NOT proceed to create plan files until:**

1. The preflight report has been generated and shown to the user.
2. All "✅ Already Done" items have been removed from the plan list.
3. All "⚠️ Partially Done" items have been scoped to only the remaining gap.
4. No duplicates remain in the final plan list.

Only after the user reviews and approves the preflight report should plan files be written.

---

## Quick Reference: Common Duplication Patterns

These are the most common ways duplication occurs in this project:

| Pattern | Example | How to Detect |
|---------|---------|---------------|
| **DI registration done by later plan** | POS-13 says "register in DI" → POS-14 did it | Check `Application.xaml.vb` or `*ServiceRegistration.vb` changes in later summaries |
| **Decorator wiring covers multiple items** | `VatAwareReceiptService` covers hash computation, numbering, and event publishing | Read the decorator's What Was Done — it often resolves 3+ items from earlier plans |
| **Cross-module handler already exists** | POS-14 says "Accounting handler needed" → ACC-10 did it | Check the target module's progress summaries |
| **Integration plan already wired it** | "Wire MediatR" → INT-01 through INT-13 cover this | Check Integration module summaries |
| **DatabaseInitializer already applied** | Migration exists but "not wired" → a later plan wired it | Search for `ApplyIfPending` in later summaries |
