# VISTA Module Audit

You are running a module audit for the VISTA project. The module to audit is: **$ARGUMENTS**

## Your Task

Perform a full mirror-check and pending-task analysis between `Plans/VISTA_Modules/<module>/` and `Progress/VISTA_Modules/<module>/`, then write a report to `Pending_Tasks/`.

> **IMPORTANT:** Step 0 (What's Next Cleanup) is a mandatory pre-step. Skipping it risks reporting stale tasks as pending and may cause duplicate plan generation downstream.

---

## Step 0 — What's Next Cleanup (Mandatory Pre-Step)

Before extracting pending tasks, sweep the `## What's Next` sections of **all** progress summaries in `Progress/VISTA_Modules/<module>/` and resolve stale items.

### Procedure

1. **Read all `## What's Next` sections** in `Progress/VISTA_Modules/<module>/`.
2. For each unchecked `- [ ]` item, determine if the described work has been completed by a **later plan**. Evidence sources:
   - A later progress summary in the **same module** (e.g., POS-13's "What's Next" references DI registration → check if POS-14 or POS-15 did it).
   - A progress summary in a **different module** (e.g., POS-14's "What's Next" references accounting handler → check if ACC-10 did it).
   - The **"What Was Done"** section of any later summary that describes the same work.
3. If the work is confirmed completed, **update the item in-place**:
   ```
   - [x] <original description> *(completed in <PLAN-ID>)*
   ```
4. If the work is **planned** but not yet completed (a plan file exists in `Plans/VISTA_Modules/` but no progress summary), annotate but leave unchecked:
   ```
   - [ ] <original description> *(planned as <PLAN-ID>)*
   ```
5. If the work is neither completed nor planned, leave the item unchanged.

### Cross-Module Lookup

Some What's Next items reference work in other modules. When auditing module X, you must check progress summaries in **all six modules** if an item names a cross-module plan ID (e.g., INT-01, ACC-10).

### Output

For each module audited, record the number of items resolved in Step 0. Include this count in the Step 5 report under a new `## What's Next Cleanup` section:

```markdown
## What's Next Cleanup (Step 0)

**Items resolved this pass:** N

| Summary | Item | Resolved By |
|---------|------|-------------|
| POS-13 | Register IReceiptIntegrityService in DI | POS-14 |
| POS-13 | Replace Max+1 pattern | POS-15 |
| ... | ... | ... |
```

---

## Step 1 — Resolve the Module Name

Map the user's input to the correct folder names. Accepted values (case-insensitive):

| User Input | Plans folder | Progress folder | Plan ID prefix |
|---|---|---|---|
| Infrastructure / INFRA | `Plans/VISTA_Modules/Infrastructure/` | `Progress/VISTA_Modules/Infrastructure/` | `INFRA-` |
| Purchasing / PUR | `Plans/VISTA_Modules/Purchasing/` | `Progress/VISTA_Modules/Purchasing/` | `PUR-` |
| Inventory / INV | `Plans/VISTA_Modules/Inventory/` | `Progress/VISTA_Modules/Inventory/` | `INV-` |
| POS | `Plans/VISTA_Modules/POS/` | `Progress/VISTA_Modules/POS/` | `POS-` |
| Accounting / ACC | `Plans/VISTA_Modules/Accounting/` | `Progress/VISTA_Modules/Accounting/` | `ACC-` |
| Integration / INT | `Plans/VISTA_Modules/Integration/` | `Progress/VISTA_Modules/Integration/` | `INT-` |

If `$ARGUMENTS` is `all` (case-insensitive), run the full audit for **all six modules** in this order: Infrastructure, Purchasing, Inventory, POS, Accounting, Integration. For each module, execute Steps 2–5 independently and write/overwrite its own report file. Then skip to Step 6-ALL.

If `$ARGUMENTS` is empty or unrecognised, ask the user to specify the module name (or `all`) and stop.

---

## Step 2 — Inventory Both Folders

1. **List all plan files** in `Plans/VISTA_Modules/<module>/` (e.g. `01-domain-models.md`, `02-data-access.md`).
   - Extract the plan number from the filename (the leading two-digit number).
   - Read the frontmatter of each plan to get: `plan-id`, `title`, `depends-on`, `estimated-files`.

2. **List all progress files** in `Progress/VISTA_Modules/<module>/` (e.g. `ACC-01-summary.md`).
   - Extract the plan number each summary corresponds to.
   - Read the frontmatter of each summary to get: `status` (`in-progress | completed | blocked`).
   - Exclude amendment files (e.g. `*-amendment.md`) from the mirror check — note them separately.

---

## Step 3 — Mirror Check

For each plan file, determine its coverage status:

- **Covered** — a matching `<PREFIX>-<NN>-summary.md` exists in Progress with `status: completed`.
- **In Progress** — a matching summary exists with `status: in-progress`.
- **Blocked** — a matching summary exists with `status: blocked`.
- **Missing** — no matching summary exists at all.

A plan file `NN-<name>.md` maps to progress file `<PREFIX>-<NN>-summary.md`.
Example: `Plans/…/Accounting/07-view-financial-overview.md` → `Progress/…/Accounting/ACC-07-summary.md`.

---

## Step 4 — Pending Task Extraction

For every progress summary that exists (regardless of status), read the full file and extract:

1. **"What's Next" section** — collect every `- [ ]` item (unchecked checkbox). These are explicit pending tasks documented by the implementing agent. **Only unchecked items remain after Step 0 cleanup.**
2. **Build status** — note if the build check shows ❌.
3. **Blocked issues** — if status is `blocked`, summarise the blocker from "Issues Encountered".

If a `- [ ]` item was resolved during Step 0 (now `- [x]`), **do not** include it in the Pending Tasks section. Annotate it with `*(now completed)*` in the report only if the reader benefits from seeing what was cleaned up.

---

## Step 5 — Write the Report

Look for an existing file matching `Pending_Tasks/<MODULE>-audit-*.md` (any date). If one exists, **overwrite it** — update both the filename date and all `audit-date` / **Audit Date:** fields inside to today's date. If none exists, create `Pending_Tasks/<MODULE>-audit-<YYYY-MM-DD>.md`.  
(Use today's date. Use the canonical module name from the table in Step 1.)

The report must contain:

```markdown
---
module: <Module Name>
audit-date: <YYYY-MM-DD>
---

# VISTA Module Audit — <Module Name>

**Audit Date:** <YYYY-MM-DD>  
**Plans Folder:** `Plans/VISTA_Modules/<module>/`  
**Progress Folder:** `Progress/VISTA_Modules/<module>/`

---

## Mirror Check Summary

| Plan ID | Plan Title | Status |
|---------|-----------|--------|
| ACC-01 | Accounting Domain Models | ✅ Completed |
| ACC-02 | ... | ⚠️ In Progress |
| ACC-03 | ... | 🚫 Blocked |
| ACC-04 | ... | ❌ Missing |

**Total Plans:** N  
**Completed:** N | **In Progress:** N | **Blocked:** N | **Missing:** N

---

## Pending Tasks

> Extracted from "What's Next" sections in existing progress summaries.

### <PLAN-ID> — <Plan Title>

**Status:** <Completed / In Progress / Blocked>

- [ ] <task from What's Next>
- [ ] <task from What's Next>

*(Repeat for each plan that has pending items)*

---

## Plans With No Progress File

| Plan ID | Plan Title | Depends On |
|---------|-----------|------------|
| ACC-XX | ... | ACC-YY |

---

## Amendments & Special Files

List any `*-amendment.md` or non-standard files found in the Progress folder:

- `PUR-03-amendment.md` — *describe what it contains*

---

## Summary & Recommendations

Write 3–5 bullet points summarising:
- Overall completion percentage
- Which plans are blocking others (check `depends-on` chains)
- Any build failures noted in summaries
- Top-priority next steps to unblock the module
```

---

## Step 6 — Report Back

After writing the file, tell the user:
- The path of the generated report
- The mirror check counts (Completed / In Progress / Blocked / Missing)
- The total number of pending `[ ]` tasks found across all summaries

---

## Step 6-ALL — Report Back (all-modules mode only)

After writing all six report files, tell the user:
- The paths of all six updated/created files
- A single combined summary table:

| Module | Completed | In Progress | Blocked | Missing | Pending Tasks |
|--------|-----------|-------------|---------|---------|---------------|
| Infrastructure | N | N | N | N | N |
| Purchasing | N | N | N | N | N |
| Inventory | N | N | N | N | N |
| POS | N | N | N | N | N |
| Accounting | N | N | N | N | N |
| Integration | N | N | N | N | N |
| **TOTAL** | N | N | N | N | N |
