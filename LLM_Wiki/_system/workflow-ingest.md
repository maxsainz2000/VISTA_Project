---
type: system
title: "Workflow — Ingest"
last-updated: 2026-05-02
---

# Workflow: Ingest

Execute these steps **strictly sequentially** when asked to ingest a source from `Sources/`.

## Checklist

1. **Read** the raw source file in `Sources/`.
2. **Create source summary** in `wiki/sources/` — structured summary with YAML frontmatter, problems table, features table, tech references, key definitions.
3. **Create or update entity pages** in `wiki/entities/` for every person, organization, module, or named system mentioned in the source. Create the page if it does not exist; append new information if it does.
4. **Create or update concept pages** in `wiki/concepts/` for every abstract topic, methodology, technology, or domain concept. Create if missing; merge if existing.
5. **Conflict check:** Test every new factual claim against existing wiki pages. If a contradiction is found, **immediately switch** to `_system/workflow-contradictions.md`, resolve the contradiction, then return here.
6. **Update `wiki/index.md`** — add a row for every new page created. Update summaries for any pages that were modified.
7. **Add cross-references** — ensure all new pages link to related existing pages and vice versa. No orphan pages.
8. **Update analysis pages** in `wiki/analysis/` if the new source adds data relevant to cross-module dependencies, the problems matrix, or the technology stack.
9. **Log the ingest** — append to `wiki/log.md`:
   ```
   ## [YYYY-MM-DD] ingest | <Source Title>
   - Source: `Sources/<filename>`
   - Pages created: <list>
   - Pages updated: <list>
   ```
10. **Mini-lint** — scan all pages touched in this ingest for broken `[[wikilinks]]`. Fix any found.
11. **Report** — summarize what was created and updated for the user.

## Notes

- Ingest one source at a time unless the user explicitly requests batch processing.
- Always read the full source before writing any wiki pages — do not start writing mid-read.
- Source summaries should be 80–120 lines. Entity and concept pages should be 50–150 lines.
