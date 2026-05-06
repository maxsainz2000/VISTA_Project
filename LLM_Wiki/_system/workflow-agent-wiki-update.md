---
type: system
title: "Workflow — Agent Wiki Update"
last-updated: 2026-05-06
---

# Workflow: Agent Wiki Update

**Trigger:** An agent completes a complex debugging session, fixes a significant error, or establishes a new architectural/code pattern.

This workflow is used to document engineering learnings in the `agent_wiki/` so that all agents can reference them in future tasks.

## Prerequisites
- You have successfully resolved an error or established a pattern.
- The solution has been verified to work.

## Steps

### Step 1: Choose the Right Template
- Navigate to `agent_wiki/_templates/`.
- For bug fixes and troubleshooting, copy `error-fix.md`.
- For new code patterns or architectural decisions, copy `pattern.md` or `antipattern.md`.

### Step 2: Create the Entry
- Create a new file in `agent_wiki/errors/` or `agent_wiki/patterns/` (or `antipatterns/`).
- Use a descriptive filename (e.g., `ef-core-lazy-loading-fix.md`).
- Fill out the YAML frontmatter completely:
  - `type`: `error-fix`, `pattern`, or `antipattern`
  - `module`: The module where this occurred (e.g., `MerchSys.Purchasing`)
  - `agent`: Your identifier (e.g., `claude-code`, `antigravity`)
  - `date`: Today's date (YYYY-MM-DD)
  - `tags`: Relevant keywords (e.g., `[ef-core, sqlite, lazy-loading]`)

### Step 3: Write the Content
- **Context/Problem:** Briefly explain what you were trying to do and what went wrong. Include error messages or stack traces if applicable.
- **Root Cause:** Explain *why* the error occurred or why the pattern is needed.
- **Solution:** Provide the code fix or the correct pattern implementation. Use code blocks.
- **Verification:** How did you confirm the fix works?

### Step 4: Update Indices
- Open `agent_wiki/index.md` and add a link to your new entry under the appropriate category.
- Open `agent_wiki/log.md` and append a timestamped entry detailing what you added and why.

## Constraints
- **Be Concise:** Focus on the technical details. Do not write a novel.
- **Tag Correctly:** Proper tagging is crucial for retrieval.
