---
type: system
title: "Workflow — Lint"
last-updated: 2026-05-02
---

# Workflow: Lint

Execute these steps strictly to prevent wiki rot. Run this workflow when the user says "lint" or "audit".

## Checklist

1. **Broken links** — Scan every `[[wikilink]]` in `wiki/` and `agent_wiki/`. Report and fix any links pointing to non-existent pages.
2. **Orphan pages** — Identify pages in `wiki/` that have zero inbound links from other wiki pages. These are invisible to navigation. Either add links from relevant pages or flag for user review.
3. **Index completeness** — Verify every file in `wiki/sources/`, `wiki/entities/`, `wiki/concepts/`, `wiki/analysis/`, and `wiki/_views/` has a corresponding row in `wiki/index.md`. Add missing entries.
4. **Definition consistency** — Check that key terms (e.g., FIFO, COGS, utang, MediatR) are used consistently across `wiki/`. Flag pages where definitions diverge from `wiki/concepts/`.
5. **Single-source claims** — Flag any entity or concept page where a factual claim cites only one source. These are weak evidence and should be noted (not necessarily wrong, but worth flagging).
6. **Missing stubs** — Find pages that are frequently linked to by other pages but do not yet exist. Create stub pages with a `> [!stub]` callout or report them to the user.
7. **Stale references** — If any source in `Sources/` has been updated or replaced since its last ingest, flag the corresponding `wiki/sources/` page as potentially stale.
8. **Report** — Present findings to the user in priority order:
   - 🔴 **Errors** — broken links, missing index entries (fix immediately)
   - 🟡 **Warnings** — orphan pages, single-source claims (review recommended)
   - 🔵 **Suggestions** — missing stubs, stale references (nice to have)
9. **Log the lint** — append to `wiki/log.md`:
   ```
   ## [YYYY-MM-DD] lint | Summary
   - Errors fixed: <count>
   - Warnings: <count>
   - Suggestions: <count>
   ```

## Agent Wiki Lint (additional)

When linting, also check `agent_wiki/`:
- Every entry in `agent_wiki/errors/`, `patterns/`, `antipatterns/` has a row in `agent_wiki/index.md`.
- Every entry has valid YAML frontmatter with required fields (type, module, agent, date, tags).
- No duplicate entries (same error-code + module combination).
