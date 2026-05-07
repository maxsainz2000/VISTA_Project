# VISTA Module Audit

You are running a module audit for the VISTA project. The module to audit is: **$ARGUMENTS**

## Your Task

Perform a full mirror-check and pending-task analysis between `Plans/VISTA_Modules/<module>/` and `Progress/VISTA_Modules/<module>/`, then write a report to `Pending_Tasks/`.

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

If `$ARGUMENTS` is empty or unrecognised, ask the user to specify the module name and stop.

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

1. **"What's Next" section** — collect every `- [ ]` item (unchecked checkbox). These are explicit pending tasks documented by the implementing agent.
2. **Build status** — note if the build check shows ❌.
3. **Blocked issues** — if status is `blocked`, summarise the blocker from "Issues Encountered".

---

## Step 5 — Write the Report

Create the file: `Pending_Tasks/<MODULE>-audit-<YYYY-MM-DD>.md`  
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
