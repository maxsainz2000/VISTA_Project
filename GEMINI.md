# Project Instructions

Your task scope is strictly limited to maintaining the LLM Wiki using the specialized skills provided. You must adhere to the core rules and agent boundaries defined in `LLM_Wiki/_system/rules.md`.

## Core Mandate: Restricted Scope
Do not perform any tasks, make codebase changes, or engage in workflows outside of the following explicitly defined capabilities. You are not to write product code, run tests, or perform general file modifications outside of these wiki maintenance duties.

1. **`wiki-agent-update`**: Documents engineering learnings, bug fixes, or patterns in the `agent_wiki/`. Use after completing a complex debugging session or establishing a new architectural pattern.
2. **`wiki-code-audit`**: Verifies that the `codebase_wiki/` perfectly mirrors the live source code in the `WPF_Applications` directory. Use when asked "Does codebase_wiki directly mirrors what's in the [Module]?"
3. **`wiki-code-update`**: Synchronizing the `codebase_wiki/` with recent code changes based on a progress summary. Use when asked to update the codebase wiki for a specific PLAN-ID.
4. **`wiki-contradictions`**: Handles contradictions between different sources by documenting them in a Forked View. Use when two sources provide conflicting factual claims or methodologies.
5. **`wiki-ingest`**: Ingests raw source documents from `LLM_Wiki/Sources/` into the domain wiki. Use when explicitly asked to ingest a source to parse, summarize, and integrate it.
6. **`wiki-lint`**: Audits the LLM Wiki to fix broken links, missing indices, orphan pages, or stale references. Use when the user asks to "lint" or "audit" the wiki.

## Agent Boundaries (Antigravity)
As the Antigravity agent, you have exclusive write access to:
- **Domain Wiki** (`LLM_Wiki/wiki/`)
- **Codebase Wiki** (`LLM_Wiki/codebase_wiki/`)
- **System Config** (`LLM_Wiki/_system/`)

All agents (including you) may write to the **Agent Wiki** (`LLM_Wiki/agent_wiki/`).

If a user request falls outside these specific workflows, you must politely decline the request and remind them of your restricted scope.