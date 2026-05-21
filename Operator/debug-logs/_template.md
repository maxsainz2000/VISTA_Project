---
test-id: <CHECKLIST-ID>-Test-<N>
checklist: <CHECKLIST-ID>-verification-checklist.md
branch: debug/<CHECKLIST-ID>-test-<N>
started: <YYYY-MM-DDTHH:mm>
status: in-progress
---

# Debug Session — <CHECKLIST-ID> Test <N>

## Problem Statement
<!-- Paste the exact error message or describe the unexpected behavior -->
<!-- Include the full test description from the verification checklist -->


## Starting State
- **Commit:** `<git rev-parse --short HEAD>`
- **Build status:** clean / N errors
- **Relevant files:**
  - `<path/to/file1>`
  - `<path/to/file2>`

## Allowed Files
<!-- List the files you are permitted to change for this fix -->
<!-- This constrains your scope and prevents collateral damage -->
- `<path/to/file1>` — reason
- `<path/to/file2>` — reason

### Off-limits (do NOT touch)
<!-- List files that are explicitly off-limits for this debugging session -->
- `SharedKernel/Events/*` — shared contracts, not the bug source
- Other modules' services/handlers

---

## Attempt Log

### Attempt 1
- **Hypothesis:**
- **Changed:** `<file>` line <N> — <what you changed>
- **Build result:** clean / error: <message>
- **Runtime result:** <what happened when you ran the test>
- **Verdict:** ✅ fixed / ❌ failed / ⚠️ partial
- **Action:** committed as `<hash>` / reverted via `git checkout -- .`

---

### Attempt 2
- **Hypothesis:**
- **Changed:** `<file>` line <N> — <what you changed>
- **Build result:**
- **Runtime result:**
- **Verdict:** ✅ / ❌ / ⚠️
- **Action:** committed / reverted

---

### Attempt 3
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:** ✅ / ❌ / ⚠️
- **Action:** committed / reverted

---

### Attempt 4
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:** ✅ / ❌ / ⚠️
- **Action:** committed / reverted

---

### Attempt 5
- **Hypothesis:**
- **Changed:**
- **Build result:**
- **Runtime result:**
- **Verdict:** ✅ / ❌ / ⚠️
- **Action:** committed / reverted

> **⛔ HARD STOP** — If all 5 attempts failed, STOP here. Write the resolution section below and escalate to the operator.

---

## Resolution

<!-- Fill this in when the bug is fixed OR when you hit the 5-attempt limit -->

- **Status:** resolved / escalated
- **Root cause:**
- **Fix description:**
- **Final commit:** `<hash>`
- **Agent wiki entry needed?** yes / no — `<entry-name-if-yes>`

### Summary for operator (if escalated)
<!-- If you hit 5 attempts, write a clear summary here so the operator can decide next steps -->
<!-- Include: what you know, what you tried, what you suspect but couldn't prove -->
